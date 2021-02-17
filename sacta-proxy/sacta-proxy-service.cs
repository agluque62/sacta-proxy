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
    public partial class SactaProxy : ServiceBase
    {
        private readonly SactaProxyWebApp sactaProxyWebApp = null;
        private readonly ConfigurationManager cfgManager = new ConfigurationManager();
        private readonly PsiManager psiManager = new PsiManager();
        private readonly Dictionary<string, ScvManager> dependenciesManager = new Dictionary<string, ScvManager>();
        private readonly Dictionary<string, wasRestCallBack> webCallbacks = new Dictionary<string, wasRestCallBack>();

        public SactaProxy()
        {
            InitializeComponent();
            sactaProxyWebApp = new SactaProxyWebApp();
        }
        public void StartOnConsole(string[] args)
        {
            OnStart(args);
        }
        public void StopOnConsole()
        {
            OnStop();
        }
        protected override void OnStart(string[] args)
        {
            // Se ejecuta al arrancar el programa.
            cfgManager.Get((cfg =>
            {
                psiManager.Start(cfg.Proxy);
                dependenciesManager.Clear();
                cfg.Dependencies.ForEach(dep =>
                {
                    var dependency = new ScvManager();
                    dependency.Start(dep);
                    dependenciesManager[dep.Id] = dependency;
                });

                webCallbacks.Add("/config", OnConfig);
                webCallbacks.Add("/status", OnState);
                sactaProxyWebApp.Start(cfg.General.WebPort, webCallbacks);
            }));
        }
        protected override void OnStop()
        {
            // Se ejecuta al finalizar el programa.
            sactaProxyWebApp.Stop();
            dependenciesManager.Values.ToList().ForEach(mng =>
            {
                mng.Stop();
            });
            dependenciesManager.Clear();
            psiManager.Stop();
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
                    psiManager = psiManager.Status,
                    dependencies = dependenciesManager.Values.Select(dep => dep.Status).ToList()
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
    }
}
