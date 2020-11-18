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
    public class DependencyControl
    {
        public Configuration.DependecyConfig Cfg { get; set; }
        public BaseManager Manager { get; set; }
        public bool Activity { get; set; }
        public Dictionary<string, int> SectorMap { get; set; }
        public DependencyControl()
        {
            Cfg = null;
            Manager = null;
            Activity = false;
            SectorMap = new Dictionary<string, int>();
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
                    SactaProxyWebApp?.Start(cfg.General.WebPort, webCallbacks);
                    Cfg = cfg;
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
            MainManager.Manager.Stop();
            EventThread.Stop();
        }

        #region Callbacks Web
        protected void OnState(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                var status = new
                {
                    psiManager = MainManager.Manager.Status,
                    dependencies = DepManager.Values.Select(dep => dep.Manager.Status).ToList()
                };
                sb.Append(JsonHelper.ToString(new { res = "ok", status }, false));
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
                    // Algo no va bien---
                }
            });
        }
        protected void OnScvEventSectorization(Object sender, SectorizationArgs data)
        {
            /** Se ha recibido una sectorizacion correcta de una dependencia */ 
            EventThread.Enqueue("OnScvEventSectorization", () =>
            {
                // TODO

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

        #endregion EventManagers

        void TestDuplicated(List<string> pos, List<string> sec, Action continues)
        {
            if (pos.Count() > 0)
                Logger.Fatal<SactaProxy>($"There are duplicate positions in configuration => {pos.Aggregate((i, j) => i + "," + j)}");
            if (sec.Count() > 0)
                Logger.Fatal<SactaProxy>($"There are duplicate sectors in configuration => {sec.Aggregate((i, j) => i + "," + j)}");
            if (pos.Count() <= 0 && sec.Count() <= 0)
                continues();
        }

        #region Data
        private SactaProxyWebApp SactaProxyWebApp { get; set; }
        private readonly Dictionary<string, wasRestCallBack> webCallbacks = new Dictionary<string, wasRestCallBack>();

        private readonly ConfigurationManager cfgManager = new ConfigurationManager();
        private Configuration Cfg { get; set; }

        private DependencyControl MainManager { get; set; }
        private readonly Dictionary<string, DependencyControl> DepManager = new Dictionary<string, DependencyControl>();
        #endregion
    }

}
