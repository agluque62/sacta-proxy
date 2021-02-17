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

using sacta_proxy.helpers;
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
        public Dictionary<string,int> MapOfSectors 
        {
            get
            {
                return sectorization;
            }
            set
            {
                sectorization = value;
                SectorizationPersistence.Set(Cfg.Id, sectorization);
            }
        }
        public DependencyControl(string id)
        {
            Cfg = null;
            Manager = null;
            Activity = false;
            SectorizationPersistence.Get(id, (data) =>
            {
                sectorization = data;
            });
        }
        public List<SectorizationItem> Sectorization
        {
            get
            {
                var sect = MapOfSectors.Select(m => new SectorizationItem() { Sector = m.Key, Position = m.Value }).ToList();
                return sect;
            }
        }
        public void MergeSectorization(SectMap map)
        {
            foreach(var item in map)
            {
                sectorization[item.Key] = item.Value;
            }
            SectorizationPersistence.Set(Cfg.Id, sectorization);
        }
        private SectMap sectorization = new SectMap();
    }
    public partial class SactaProxy : ServiceBase
    {
        public SactaProxy(bool WebEnabled=true)
        {
            InitializeComponent();
            if (WebEnabled)
            {
                SactaProxyWebApp = new SactaProxyWebApp();
                SactaProxyWebApp.UserActivityEvent += (sender, args) =>
                {
                    EventThread?.Enqueue("OnUserActivityEvent", () =>
                    {
                        var idh = args.InOut ? HistoryItems.UserLogin : args.Cause == "" ? HistoryItems.UserLogout : HistoryItems.UserErrorAccess;
                        PS.History?.Add(idh, args.User, "", "", "", args.Cause);
                    });
                };
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
                PS.History = new History(cfg.General.HistoryMaxDays, cfg.General.HistoryMaxItems);
                ManagersDescription.Clear();
                cfg.Psi.Sectorization.Positions.Clear();
                cfg.Psi.Sectorization.Sectors.Clear();

                var manager = new PsiManager();
                manager.EventActivity += OnPsiEventActivity;
                manager.EventSectRequest += OnPsiEventSectorizationAsk;
                ManagersDescription.Add(new DependencyControlEntry() { Id = cfg.Psi.Id, Dep = new DependencyControl(cfg.Psi.Id) { Cfg = cfg.Psi, Manager = manager }, IsMain = true });

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

                    ManagersDescription.Add(new DependencyControlEntry() { Id = dep.Id, Dep = new DependencyControl(dep.Id) { Cfg = dep, Manager = dependency }, IsMain = false });
                });
                /** */
                var ids = cfg.Dependencies.Select(d => d.Id).ToList();
                ids.Add(cfg.Psi.Id);
                SectorizationPersistence.Sanitize(ids);

                /** Chequear que no haya sectores o posiciones repetidas */
                var duplicatedSec = cfg.Psi.Sectorization.Sectors.GroupBy(s => s)
                    .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
                var duplicatedPos = cfg.Psi.Sectorization.Positions.GroupBy(s => s)
                    .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
                TestDuplicated(duplicatedPos, duplicatedSec, () =>
                {
                    // Solo arranca el programa cuando no hay duplicados.
                    ManagersDescription.ForEach(dependency =>
                    {
                        dependency.Dep.Manager.Start(dependency.Dep.Cfg);
                    });
                    webCallbacks.Add("/config", OnWebRequestConfig);
                    webCallbacks.Add("/status", OnWebRequestState);
                    webCallbacks.Add("/version", OnWebRequestVersion);
#if DEBUG
                    webCallbacks.Add("/testing/*", OnWebRequestTesting);
#endif
                    SactaProxyWebApp?.Start(cfg.General.WebPort, cfg.General.WebActivityMinTimeout, webCallbacks);
                    Cfg = cfg;
#if DEBUG1
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
                    MainManager.MergeSectorization(DepManager.Where(d => d.Key == "TWR").First().Value.MapOfSectors);
                    MainManager.MergeSectorization(DepManager.Where(d => d.Key == "APP").First().Value.MapOfSectors);
#endif
                    PS.Set(ProcessStates.Running);
                    PS.History.Add(HistoryItems.ServiceStarted);
                });
            }));
        }
        protected void StopService() 
        {
            // Se ejecuta al finalizar el programa.
            SactaProxyWebApp?.Stop();
            webCallbacks.Clear();

            ManagersDescription.ForEach(depEntry =>
            {
                if (depEntry.IsMain)
                {
                    (depEntry.Dep.Manager as PsiManager).EventActivity -= OnPsiEventActivity;
                    (depEntry.Dep.Manager as PsiManager).EventSectRequest -= OnPsiEventSectorizationAsk;
                }
                else
                {
                    (depEntry.Dep.Manager as ScvManager).EventActivity -= OnScvEventActivity;
                    (depEntry.Dep.Manager as ScvManager).EventSectorization -= OnScvEventSectorization;
                }
                depEntry.Dep.Manager.Stop();
            });
            ManagersDescription.Clear();

            PS.Set(ProcessStates.Stopped);
            PS.History.Add(HistoryItems.ServiceEnded);
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
                        /** Historico */
                        PS.History.Add(HistoryItems.UserConfigChange, SystemUsers.CurrentUserId);
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
                //var controlDep = DepManager.Where(d => d.Key == data.ScvId).Select(d => d.Value).ToList();
                var ctrldep = DepManagers.Where(d => d.Cfg.Id == data.ScvId).FirstOrDefault();
                if (ctrldep != null)
                {
                    if (ctrldep.Activity != data.ActivityOnLan)
                    {
                        /** Actualiza el estado de la dependencia y Genera el Historico */
                        ctrldep.Activity = data.ActivityOnLan;
                        PS.History.Add(HistoryItems.DepActivityEvent, "", ctrldep.Cfg.Id, data.ActivityOnLan ? "ON" : "OFF");
                        /** Actualiza el Tx del SCV */
                        var oldEnableTx = MainManager.Manager.EnableTx;
                        var actives = DepManagers.Where(d => d.Activity == true).ToList();
                        MainManager.Manager.EnableTx =
                            Cfg.General.ActivateSactaLogic == "OR" ? actives.Count() > 0 :
                            Cfg.General.ActivateSactaLogic == "AND" ? actives.Count() == DepManagers.Count() :
                            actives.Count() == DepManagers.Count();
                        /** Se genera el historico si corresponde */
                        if (oldEnableTx != MainManager.Manager.EnableTx)
                        {
                            PS.History.Add(HistoryItems.DepTxstateChange, "", MainManager.Cfg.Id, MainManager.Manager.EnableTx ? "ON" : "OFF");
                        }
                    }
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
                var ctrldep = DepManagers.Where(d => d.Cfg.Id == data.ScvId).FirstOrDefault();
                if (ctrldep != null)
                {
                    if (data.Accepted)
                    {
                        // Actualizo la Sectorizacion en la Dependencia.
                        ctrldep.MapOfSectors = data.SectorMap;

                        // Actualizo la Sectorizacion en el Manager.
                        MainManager.MergeSectorization(ctrldep.MapOfSectors);

                        // Historico.
                        PS.History.Add(HistoryItems.DepSectorizationReceivedEvent, "", ctrldep.Cfg.Id, "", SectorizationHelper.MapToString(data.SectorMap));

                        // Propagar la Sectorizacion al SCV real si todas las dependencias han recibido sectorizacion.
                        var DepWithSectInfo = DepManagers.Where(d => d.MapOfSectors.Count > 0).ToList();
                        if (DepWithSectInfo.Count == DepManagers.Count)
                        {
                            (MainManager.Manager as PsiManager).SendSectorization(MainManager.MapOfSectors);
                            // Historico
                            PS.History.Add(HistoryItems.ScvSectorizationSendedEvent, "", MainManager.Cfg.Id, "", 
                                SectorizationHelper.MapToString(MainManager.MapOfSectors), "Recibida de SACTA");
                        }
                        else
                        {
                            Logger.Warn<SactaProxy>($"OnScvEventSectorization. IGNORED. No all Sectorization Info Present.");
                        }
                    }
                    else
                    {
                        // Evento de Sectorizacion Rechazada. Historico
                        PS.History.Add(HistoryItems.DepSectorizationRejectedEvent, "", ctrldep.Cfg.Id,
                            "", SectorizationHelper.MapToString(data.SectorMap), data.RejectCause);
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
                    /** Historico del Cambio */
                    PS.History.Add(HistoryItems.DepActivityEvent, "", MainManager.Cfg.Id, MainManager.Activity ? "ON" : "OFF");
                    // Si se pierde la conectividad con el SCV real, se simula 'inactividad' en la interfaz sacta.
                    DepManagers.ForEach(dependency =>
                        {
                            dependency.Manager.EnableTx = MainManager.Activity;
                          /** Historico del Cambio */
                            PS.History.Add(HistoryItems.DepTxstateChange, "", dependency.Cfg.Id, MainManager.Activity ? "ON" : "OFF");

                        });
                }
            });
        }
        protected void OnPsiEventSectorizationAsk(Object sender, SectorizationRequestArgs data)
        {
            EventThread.Enqueue("OnPsiEventActivity", () =>
            {
                //var DepWithSectInfo = DepManager.Where(d => d.Value.MapOfSectors.Count() > 0).ToList();
                var DepWithSectInfo = DepManagers.Where(d => d.MapOfSectors.Count > 0).ToList();
                if (DepWithSectInfo.Count == DepManagers.Count)
                {
                    (MainManager.Manager as PsiManager).SendSectorization(MainManager.MapOfSectors);
                    /** Historico */
                    PS.History.Add(HistoryItems.ScvSectorizationSendedEvent, "", MainManager.Cfg.Id, "", SectorizationHelper.MapToString(MainManager.MapOfSectors), "Peticion SCV");
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
                    web = SactaProxyWebApp?.Status,
                    ems = ManagersDescription.Select(d => new { id = d.Id, status = d.Dep.Manager.Status, sect=d.Dep.Sectorization }).ToList()
                };
            }
        }
        void Reset()
        {
            EventThread.Enqueue("Reset", () =>
            {
                Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
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
        private readonly ProcessStatusControl PS = new ProcessStatusControl();
        class DependencyControlEntry
        {
            public string Id { get; set; }
            public DependencyControl Dep { get; set; }
            public bool IsMain { get; set; }
        }
        private readonly List<DependencyControlEntry> ManagersDescription = new List<DependencyControlEntry>();
        private DependencyControl MainManager
        {
            get
            {
                var dep = ManagersDescription.Where(d => d.IsMain).FirstOrDefault();
                return dep?.Dep;
            }
        }
        private List<DependencyControl> DepManagers
        {
            get
            {
                var deps = ManagersDescription.Where(d => d.IsMain == false).Select(d => d.Dep).ToList();
                return deps;
            }
        }
        #endregion
    }

}
