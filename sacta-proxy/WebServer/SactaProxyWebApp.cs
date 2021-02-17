using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using sacta_proxy.model;
using sacta_proxy.helpers;

namespace sacta_proxy.WebServer
{
    class WebUserActivityArgs : EventArgs
    {
        public string User { get; set; }
        public bool InOut { get; set; }
        public string Cause { get; set; }
    }
    class SactaProxyWebApp : WebServerBase
    {
        public event EventHandler<WebUserActivityArgs> UserActivityEvent;
        public SactaProxyWebApp(Func<History> hist) : base()
        {
            History = hist;
        }
        public void Start(int port, int SessionDuration = 15, Dictionary<string, wasRestCallBack> cbs=null)
        {
            /** Rutina a la que llama el servidor base para autentificar un usuario */
            AuthenticateUser = (data, response) =>
            {
                /** 'namecontroluser'=user&'namecontrolpwd'=pwd */
                var items = data.Split('&')
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => x.Split('='))
                        .ToDictionary(x => x[0], x => x[1]);

                if (items.Keys.Contains("username") && items.Keys.Contains("password"))
                {
                    var res = SystemUsers.Authenticate(items["username"], items["password"]);
                    var cause = res ? "" : "Usuario o password incorrecta";
                    response(res, cause);
                    UserActivityEvent?.Invoke(this, new WebUserActivityArgs() { User = items["username"], InOut = res, Cause = cause });
                }
                else
                {
                    response(false, "No ha introducido usuario o password");
                }
            };

            /** Configura las rutinas a las que llama el servidor base cuando recibe peticiones REST */
            Dictionary<string, wasRestCallBack> cfg = new Dictionary<string, wasRestCallBack>()
                {
                    {"/alive", RestAlive },
                    {"/logs", RestLogs },
                    {"/logout", RestLogout }
                };
            var SecureUris = new List<string>()
            {
                "/styles/bootstrap/bootstrap.min.css",
                "/styles/ncc-styles.css",
                "/scripts/jquery/jquery-2.1.3.min.js",
                "/images/corporativo-a.png",
                "/favicon.ico"
            };
            try
            {
                /** Añado los callback 'exteriores' */
                if (cbs != null)
                {
                    foreach(var item in cbs)
                    {
                        if (cfg.Keys.Contains(item.Key) == false)
                        {
                            cfg[item.Key] = item.Value;
                        }
                    }
                }

                base.Start(port, new CfgServer() 
                {
                    DefaultDir = "/webclient",
                    DefaultUrl = "/index.html",
                    LoginUrl = "/login.html",
                    LogoutUrl="/logout",
                    LoginErrorTag= "<div id='result'>",
                    HtmlEncode = false,
                    SessionDuration=SessionDuration,
                    SecureUris = SecureUris,
                    CfgRest = cfg
                });
                stdcontrol.Set(ProcessStates.Running);
            }
            catch (Exception x)
            {
                Logger.Exception<SactaProxyWebApp>(x);
                stdcontrol.SignalFatal<SactaProxyWebApp>($"Exception on starting => {x.Message}", History());
            }
        }
        public new void Stop()
        {
            try
            {
                base.Stop();
                stdcontrol.Set(ProcessStates.Stopped);
            }
            catch (Exception x)
            {
                Logger.Exception<SactaProxyWebApp>(x);
                stdcontrol.SignalFatal<SactaProxyWebApp>($"Exception on ending => {x.Message}", History());
            }
        }

        public object Status { get => stdcontrol.Status; }

        #region Manejadores REST
        protected void RestLogout(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "POST")
            {
                UserActivityEvent?.Invoke(this, new WebUserActivityArgs() { User = SystemUsers.CurrentUserId, InOut = false, Cause="" });
                SessionExpiredAt = DateTime.Now;
                context.Response.Redirect("/login.html");
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void RestAlive(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new { res = "OK" }, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void RestLogs(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                // TODO. Leer los Logs y Añadir 
                // ReadLog((logs) => { sb.Append(JsonConvert.SerializeObject(logs, Formatting.Indented)); });
                sb.Append(JsonHelper.ToString(new { res = "Contenido del Logs" }, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }

        private readonly ProcessStatusControl stdcontrol = new ProcessStatusControl();
        private Func<History> History { get; set; }
        #endregion Manejadores REST
    }
}
