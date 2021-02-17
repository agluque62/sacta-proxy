﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using sacta_proxy.Helpers;

namespace sacta_proxy.WebServer
{
    class SactaProxyWebApp : WebServerBase
    {
        class SystemUsers
        {
            class SystemUserInfo
            {
                public string id { get; set; }
                public string pwd { get; set; }
                public int prf { get; set; }
            }
            static public bool Authenticate(string user, string pwd)
            {
                if (user == "root" && pwd == "#ncc#")
                    return true;
                try
                {
                    // Obtener los usuarios, y comprobar si tienen acceso
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public SactaProxyWebApp() : base()
        {
        }
        public void Start(int port)
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
                    response(res, "Usuario o password incorrecta");
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
                "/styles/uv5ki-styles.css",
                "/scripts/jquery/jquery-2.1.3.min.js",
                "/images/corporativo-a.png",
                "/favicon.ico"
            };
            try
            {
                base.Start(port, new CfgServer() 
                {
                    DefaultDir = "/webclient",
                    DefaultUrl = "/index.html",
                    LoginUrl = "/login.html",
                    LogoutUrl="/logout",
                    LoginErrorTag= "<div id='result'>",
                    HtmlEncode = false,
                    SessionDuration = 60,
                    SecureUris = SecureUris,
                    CfgRest = cfg
                });
            }
            catch (Exception x)
            {
                Logger.Exception<SactaProxyWebApp>(x);
            }
        }
        public new void Stop()
        {
            try
            {
                base.Stop();
            }
            catch (Exception x)
            {
                Logger.Exception<SactaProxyWebApp>(x);
            }
        }

        #region Manejadores REST
        protected void RestLogout(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "POST")
            {
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
        
        #endregion Manejadores REST
    }
}