using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

using sacta_proxy.Helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;
using sacta_proxy.Managers;


namespace sacta_proxy
{
    using SectMap = Dictionary<string, int>;

    public class DependencyControl
    {
        public Configuration.DependecyConfig Cfg { get; set; }
        public BaseManager Manager { get; set; }
        public bool Activity { get; set; }
        public Dictionary<string,int> MapOfSectors { get; set; }
        public DependencyControl()
        {
            Cfg = null;
            Manager = null;
            Activity = false;
            MapOfSectors = new Dictionary<string, int>();
        }
        public List<SectorizationItem> Sectorization
        {
            get
            {
                var sect = MapOfSectors.Select(m => new SectorizationItem() { Sector = m.Key, Position = m.Value }).ToList();
                return sect;
            }
        }
        public static SectMap CopyMap(SectMap src, SectMap dst)
        {
            foreach(var item in src)
            {
                dst[item.Key] = item.Value;
            }
            return dst;
        }
    }
    public partial class SactaProxy : ServiceBase
    {
        public SactaProxy(bool WebEnabled=true)
        {
            InitializeComponent();
            if (WebEnabled)
            {
                SactaProxyWebApp = new SactaProxyWebApp();
            }
            else
            {
                SactaProxyWebApp = null;
            }
        }
        public void StartOnConsole(string[] args)
        {
            OnStart(args);
        }
        public void StopOnConsole()
        {
            OnStop();
            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
        }
        protected override void OnStart(string[] args)
        {
            // Se ejecuta al arrancar el programa.
            EventThread.Start();
            StartService();

        }
        protected override void OnStop()
        {
            StopService();
            EventThread.Stop();
        }

        protected void StartService()
        {
            cfgManager.Get((cfg =>
            {
                DepManager.Clear();
                cfg.Psi.Sectorization.Positions.Clear();
                cfg.Psi.Sectorization.Sectors.Clear();

                var manager = new PsiManager();
                manager.EventActivity += OnPsiEventActivity;
                manager.EventSectRequest += OnPsiEventSectorizationAsk;

                MainManager = new DependencyControl()
                {
                    Cfg = cfg.Psi,
                    Manager = manager
                };

                cfg.Dependencies.ForEach(dep =>
                {
                    var dependency = new ScvManager();
                    dependency.EventActivity += OnScvEventActivity;
                    dependency.EventSectorization += OnScvEventSectorization;

                    /** Construyendo la configuracion de Sectorizacion general */
                    var sectorsMap = dep.Sectorization.SectorsMap.Split(',')
                        .Where(i => Configuration.MapOfSectorsEntryValid(i))
                        .ToDictionary(k => int.Parse(k.Split(':')[0]), v => int.Parse(v.Split(':')[1]));
                    var positionsMap = dep.Sectorization.PositionsMap.Split(',')
                        .Where(i => Configuration.MapOfSectorsEntryValid(i))
                        .ToDictionary(k => int.Parse(k.Split(':')[0]), v => int.Parse(v.Split(':')[1]));
                    var virtuals = dep.Sectorization.Virtuals
                        .Select(v => sectorsMap.Keys.Contains(v) ? sectorsMap[v] : v)
                        .ToList();
                    var reals = dep.Sectorization.Sectors
                        .Select(r => sectorsMap.Keys.Contains(r) ? sectorsMap[r] : r)
                        .ToList();
                    var positions = dep.Sectorization.Positions
                        .Select(p => positionsMap.Keys.Contains(p) ? positionsMap[p] : p)
                        .ToList();
                    
                    cfg.Psi.Sectorization.Positions.AddRange(positions);
                    cfg.Psi.Sectorization.Virtuals.AddRange(virtuals);
                    cfg.Psi.Sectorization.Sectors.AddRange(reals);

                    //dependency.Start(dep);
                    DepManager[dep.Id] = new DependencyControl()
                    {
                        Cfg = dep,
                        Manager = dependency
                    };
                });
                /** Chequear que no haya sectores o posiciones repetidas */
                var duplicatedSec = cfg.Psi.Sectorization.Sectors.GroupBy(s => s)
                    .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
                var duplicatedPos = cfg.Psi.Sectorization.Positions.GroupBy(s => s)
                    .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
                TestDuplicated(duplicatedPos, duplicatedSec, () =>
                {
                    // Solo arranca el programa cuando no hay duplicados.
                    MainManager.Manager.Start(cfg.Psi);
                    DepManager.Values.ToList().ForEach(dependency =>
                    {
                        dependency.Manager.Start(dependency.Cfg);
                    });
                    webCallbacks.Add("/config", OnWebRequestConfig);
                    webCallbacks.Add("/status", OnWebRequestState);
                    webCallbacks.Add("/version", OnWebRequestVersion);
#if DEBUG
                    webCallbacks.Add("/testing/*", OnWebRequestTesting);
#endif
                    SactaProxyWebApp?.Start(cfg.General.WebPort, cfg.General.WebActivityMinTimeout, webCallbacks);
                    Cfg = cfg;
#if DEBUG
                    DepManager.Where(d => d.Key == "TWR").First().Value.MapOfSectors = new SectMap()
                    {
                        {"0001", 1 },
                        {"0002", 1 },
                        {"0003", 2 },
                        {"0004", 2 },
                    };
                    DepManager.Where(d => d.Key == "APP").First().Value.MapOfSectors = new SectMap()
                    {
                        {"0011", 11 },
                        {"0012", 11 },
                        {"0013", 12 },
                        {"0014", 12 },
                    };
                    DependencyControl.CopyMap(DepManager.Where(d => d.Key == "TWR").First().Value.MapOfSectors, MainManager.MapOfSectors);
                    DependencyControl.CopyMap(DepManager.Where(d => d.Key == "APP").First().Value.MapOfSectors, MainManager.MapOfSectors);
#endif
                    PS.Set(ProcessStates.Running);
                });
            }));
        }
        protected void StopService() 
        {
            // Se ejecuta al finalizar el programa.
            SactaProxyWebApp?.Stop();
            webCallbacks.Clear();

            DepManager.Values.ToList().ForEach(dependency =>
            {
                (dependency.Manager as ScvManager).EventActivity -= OnScvEventActivity;
                (dependency.Manager as ScvManager).EventSectorization -= OnScvEventSectorization;
                dependency.Manager.Stop();
            });
            DepManager.Clear();
            (MainManager.Manager as PsiManager).EventActivity -= OnPsiEventActivity;
            (MainManager.Manager as PsiManager).EventSectRequest -= OnPsiEventSectorizationAsk;
            MainManager.Manager.Stop();
            PS.Set(ProcessStates.Stopped);
        }

        #region Callbacks Web
        protected void OnWebRequestState(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new { res = "ok", user=SystemUsers.CurrentUserId, version=GenericHelper.VersionManagement.AssemblyVersion, Status }, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestConfig(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                string data = JsonHelper.ToString(new { res = "Error al obtener configuracion" }, false);
                context.Response.StatusCode = 500;
                cfgManager.Get((cfg) =>
                {
                    data = JsonHelper.ToString(new { res = "ok", cfg}, false);
                    context.Response.StatusCode = 200;
                });
                sb.Append(data);
            }
            else if (context.Request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string strData = reader.ReadToEnd();
                    if (cfgManager.Set(strData))
                    {
                        /** Reiniciar el Servicio */
                        Reset();
                        context.Response.StatusCode = 200;
                        sb.Append(JsonHelper.ToString(new { res = "ok" }, false));
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonHelper.ToString(new { res = "Error actualizando la configuracion" }, false));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestVersion(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString((new GenericHelper.VersionManagement("versiones.json")).Version, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestTesting(HttpListenerContext context, StringBuilder sb)
        {
            string cmd = context.Request.Url.LocalPath.Split('/')[2];
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (cmd == "reset")
                {
                    Reset();
                }
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new { res = "ok"}, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        #endregion Callbacks Web

        #region EventManagers
        private readonly EventQueue EventThread = new EventQueue();
        protected void OnScvEventActivity(Object sender, ActivityOnLanArgs data)
        {
            EventThread.Enqueue("OnScvEventActivity", () =>
            {
                var controlDep = DepManager.Where(d => d.Key == data.ScvId).Select(d => d.Value).ToList();
                if (controlDep.Count == 1)
                {
                    if (controlDep[0].Activity != data.ActivityOnLan)
                    {
                        controlDep[0].Activity = data.ActivityOnLan;
                    }
                    var actives = DepManager.Select(s => s.Value).Where(d => d.Activity == true).ToList();
                    MainManager.Manager.EnableTx =
                        Cfg.General.ActivateSactaLogic == "OR" ? actives.Count() > 0 :
                        Cfg.General.ActivateSactaLogic == "AND" ? actives.Count() == DepManager.Count() :
                        actives.Count() == DepManager.Count();
                }
                else
                {
                    // Algo no va bien.
                    Logger.Fatal<SactaProxy>($"OnScvEventActivity. {data.ScvId} Dependency is missing or duplicated");
                }
            });
        }
        protected void OnScvEventSectorization(Object sender, SectorizationReceivedArgs data)
        {
            /** Se ha recibido una sectorizacion correcta de una dependencia */ 
            EventThread.Enqueue("OnScvEventSectorization", () =>
            {
                var controlDep = DepManager.Where(d => d.Key == data.ScvId).Select(d => d.Value).ToList();
                if (controlDep.Count == 1)
                {
                    // Actualizo la Sectorizacion en la Dependencia.
                    controlDep[0].MapOfSectors = data.SectorMap;

                    // Actualizo la Sectorizacion en el Manager.
                    MainManager.MapOfSectors = DependencyControl.CopyMap(controlDep[0].MapOfSectors, MainManager.MapOfSectors);

                    // Propagar la Sectorizacion al SCV real si todas las dependencias han recibido sectorizacion.
                    var DepWithSectInfo = DepManager.Where(d => d.Value.MapOfSectors.Count() > 0).ToList();
                    if (DepWithSectInfo.Count == DepManager.Count)
                    {
                        (MainManager.Manager as PsiManager).SendSectorization(MainManager.MapOfSectors);
                    }
                    else
                    {
                        Logger.Warn<SactaProxy>($"OnScvEventSectorization. IGNORED. No all Sectorization Info Present.");
                    }
                }
                else
                {
                    Logger.Fatal<SactaProxy>($"OnScvEventSectorization. {data.ScvId} Dependency is missing or duplicated");
                }
            });
        }
        protected void OnPsiEventActivity(Object sender, ActivityOnLanArgs data)
        {
            EventThread.Enqueue("OnPsiEventActivity", () =>
            {
                if (data.ActivityOnLan != MainManager.Activity)
                {
                    MainManager.Activity = data.ActivityOnLan;
                    // Si se pierde la conectividad con el SCV real, se simula 'inactividad' en la interfaz sacta.
                    DepManager.Values.ToList().ForEach(dependency =>
                    {
                        dependency.Manager.EnableTx = MainManager.Activity;
                    });
                }
            });
        }
        protected void OnPsiEventSectorizationAsk(Object sender, SectorizationRequestArgs data)
        {
            EventThread.Enqueue("OnPsiEventActivity", () =>
            {
                var DepWithSectInfo = DepManager.Where(d => d.Value.MapOfSectors.Count() > 0).ToList();
                if (DepWithSectInfo.Count == DepManager.Count)
                {
                    (MainManager.Manager as PsiManager).SendSectorization(MainManager.MapOfSectors);
                }
                else
                {
                    Logger.Warn<SactaProxy>($"OnPsiEventSectorizationAsk. IGNORED. No all Sectorization Info Present.");
                }
            });
        }

        #endregion EventManagers

        void TestDuplicated(List<string> pos, List<string> sec, Action continues)
        {
            if (pos.Count() > 0)
                PS.SignalFatal<SactaProxy>($"There are duplicate positions in configuration => {pos.Aggregate((i, j) => i + "," + j)}");
            if (sec.Count() > 0)
                PS.SignalFatal<SactaProxy>($"There are duplicate sectors in configuration => {sec.Aggregate((i, j) => i + "," + j)}");
            if (pos.Count() <= 0 && sec.Count() <= 0)
                continues();
        }
        Object Status 
        { 
            get
            {
                return new
                {
                    service = PS.Status,
                    web = 0,
                    psi_em = MainManager.Manager.Status,
                    scv_em = DepManager.Select(dep => new { id=dep.Key, std=dep.Value.Manager.Status}).ToList(),
                    sectorizations = new
                    {
                        global = MainManager.Sectorization,
                        deps = DepManager.Select(dep => new { id = dep.Key, sect = dep.Value.Sectorization }).ToList()
                    }
                };
            }
        }
        void Reset()
        {
            EventThread.Enqueue("Reset", () =>
            {
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                StopService();
                Task.Delay(TimeSpan.FromMilliseconds(1000)).Wait();
                StartService();
            });
        }

        #region Data
        private SactaProxyWebApp SactaProxyWebApp { get; set; }
        private readonly Dictionary<string, wasRestCallBack> webCallbacks = new Dictionary<string, wasRestCallBack>();

        private readonly ConfigurationManager cfgManager = new ConfigurationManager();
        private Configuration Cfg { get; set; }

        private DependencyControl MainManager { get; set; }
        private readonly Dictionary<string, DependencyControl> DepManager = new Dictionary<string, DependencyControl>();
        private readonly ProcessStatusControl PS = new ProcessStatusControl();
        #endregion
    }

}
