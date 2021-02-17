﻿using System;
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
                    cfg.Psi.Sectorization.Positions.AddRange(dep.Sectorization.Positions);
                    cfg.Psi.Sectorization.Sectors.AddRange(dep.Sectorization.Sectors);

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
                    EventThread.Start();
                    MainManager.Manager.Start(cfg.Psi);
                    DepManager.Values.ToList().ForEach(dependency =>
                    {
                        dependency.Manager.Start(dependency.Cfg);
                    });
                    webCallbacks.Add("/config", OnConfig);
                    webCallbacks.Add("/status", OnState);
                    SactaProxyWebApp?.Start(cfg.General.WebPort, cfg.General.WebActivityMinTimeout, webCallbacks);
                    Cfg = cfg;
                    PS.Set(ProcessStates.Running);
                });
            }));
        }
        protected override void OnStop()
        {
            // Se ejecuta al finalizar el programa.
            SactaProxyWebApp?.Stop();
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
            EventThread.Stop();
            PS.Set(ProcessStates.Stopped);
        }

        #region Callbacks Web
        protected void OnState(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new { res = "ok", Status }, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnConfig(HttpListenerContext context, StringBuilder sb)
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
                        global = MainManager.MapOfSectors,
                        deps = DepManager.Select(dep => new { id = dep.Key, sect = dep.Value.MapOfSectors }).ToList()
                    }
                };
            }
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
