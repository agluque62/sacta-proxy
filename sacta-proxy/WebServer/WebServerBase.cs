﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using sacta_proxy.Helpers;

namespace sacta_proxy.WebServer
{
    class WebServerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public delegate void wasRestCallBack(HttpListenerContext context, StringBuilder sb);

        #region Public
        public class CfgServer
        {
            public string DefaultUrl { get; set; }
            public string DefaultDir { get; set; }
            public string LoginUrl { get; set; }
            public string LogoutUrl { get; set; }
            public bool HtmlEncode { get; set; }
            public int SessionDuration { get; set; }
            public Dictionary<string, wasRestCallBack> CfgRest { get; set; }
            public string LoginErrorTag { get; set; }
            public List<string> SecureUris { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public WebServerBase()
        {
            SetRequestRootDirectory();
            Enable = true;
            DisableCause = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="cfg"></param>
        public void Start(int port, CfgServer cfg)
        {
            lock (locker)
            {
                if (Listener != null)
                    Stop();

                Config = cfg;
                Listener = new HttpListener();
                Listener.Prefixes.Add("http://*:" + port.ToString() + "/");

                ///** Configurar la Autentificacion */
                //Listener.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous;
                //Listener.AuthenticationSchemeSelectorDelegate = request =>
                //{
                //    /** Todas las operaciones No GET de Usuarios no ulises se consideran inseguras... Habra que autentificarse */
                //    // return request.HttpMethod == "GET" || request.Headers["UlisesClient"] == "MTTO" ? AuthenticationSchemes.Anonymous : AuthenticationSchemes.Basic;
                //    return AuthenticationSchemes.Anonymous;
                //};

                Listener.Start();
                Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Stop()
        {
            lock (locker)
            {
                if (Listener != null)
                {
                    Listener.Close();
                    Listener = null;
                    Config = null;
                }
            }
        }

        #endregion

        #region Protected

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        void GetContextCallback(IAsyncResult result)
        {
            lock (locker)
            {
                if (Listener == null || Listener.IsListening == false)
                    return;

                HttpListenerContext context = Listener.EndGetContext(result);
                logrequest(context);

                try
                {
                    if (IsAuthenticated(context))
                    {
                        string url = context.Request.Url.LocalPath;
                        if (Enable)
                        {
                            if (url == "/") context.Response.Redirect(Config?.DefaultUrl);
                            else
                            {
                                wasRestCallBack cb = FindRest(url);
                                if (cb != null)
                                {
                                    StringBuilder sb = new System.Text.StringBuilder();
                                    cb(context, sb);
                                    context.Response.ContentType = FileContentType(".json");
                                    Render(Encode(sb.ToString()), context.Response);
                                }
                                else
                                {
                                    url = Config?.DefaultDir + url;
                                    if (url.Length > 1 && File.Exists(url.Substring(1)))
                                    {
                                        /** Es un fichero lo envio... */
                                        string file = url.Substring(1);
                                        string ext = Path.GetExtension(file).ToLowerInvariant();

                                        context.Response.ContentType = FileContentType(ext);
                                        ProcessFile(context.Response, file);
                                    }
                                    else
                                    {
                                        context.Response.StatusCode = 404;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Render(Encode(DisableCause), context.Response);
                            // context.Response.StatusCode = 503;
                            // context.Response.Redirect("/noserver.html");
                            context.Response.ContentType = FileContentType(".html");
                            ProcessFile(context.Response, (Config?.DefaultDir + "/disabled.html").Substring(1), "{{cause}}", DisableCause);
                        }
                    }
                }
                catch (Exception x)
                {
                    Logger.Exception<WebServerBase>(x);
                    context.Response.StatusCode = 500;
                    // Todo. Render(Encode(x.Message), context.Response);
                }
                finally
                {
                    context.Response.Close();
                    if (Listener != null && Listener.IsListening)
                        Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="file"></param>
        protected void ProcessFile(HttpListenerResponse response, string file, string tag = "", string valor = "")
        {
            if (tag != "")
            {
                string str = File.ReadAllText(file).Replace(tag, valor);
                byte[] content = Encoding.ASCII.GetBytes(str);
                response.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                byte[] content = File.ReadAllBytes(file);
                response.OutputStream.Write(content, 0, content.Length);
            }
            response.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="res"></param>
        protected void Render(string msg, HttpListenerResponse res)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
            res.ContentLength64 = buffer.Length;

            using (System.IO.Stream outputStream = res.OutputStream)
            {
                outputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entrada"></param>
        /// <returns></returns>
        protected string Encode(string entrada)
        {
            if (Config?.HtmlEncode == true)
            {
                char[] chars = entrada.ToCharArray();
                StringBuilder result = new StringBuilder(entrada.Length + (int)(entrada.Length * 0.1));

                foreach (char c in chars)
                {
                    int value = Convert.ToInt32(c);
                    if (value > 127)
                        result.AppendFormat("&#{0};", value);
                    else
                        result.Append(c);
                }

                return result.ToString();
            }
            return entrada;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void SetRequestRootDirectory()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string rootDirectory = Path.GetDirectoryName(exePath);
            Directory.SetCurrentDirectory(rootDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected wasRestCallBack FindRest(string url)
        {
            if (Config?.CfgRest == null)
                return null;

            if (Config.CfgRest.ContainsKey(url))
                return Config?.CfgRest[url];

            string[] urlComp = url.Split('/');
            foreach (KeyValuePair<string, wasRestCallBack> item in Config?.CfgRest)
            {
                string[] keyComp = item.Key.Split('/');
                if (keyComp.Count() != urlComp.Count())
                    continue;

                bool encontrado = true;
                for (int index = 0; index < urlComp.Count(); index++)
                {
                    if (urlComp[index] != keyComp[index] && keyComp[index] != "*")
                        encontrado = false;
                }

                if (encontrado == true)
                    return item.Value;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        Dictionary<string, string> _filetypes = new Dictionary<string, string>()
        {
            {".css","text/css"},
            {".jpeg","image/jpg"},
            {".jpg","image/jpg"},
            {".htm","text/html"},
            {".html","text/html"},
            {".ico","image/ico"},
            {".js","text/json"},
            {".json","text/json"},
            {".txt","text/text"},
            {".bmp","image/bmp"}
        };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        private string FileContentType(string ext)
        {
            if (_filetypes.ContainsKey(ext))
                return _filetypes[ext];
            return "text/text";
        }

        #endregion

        #region Autenticacion
        //bool MustAuthenticate = true;
        protected DateTime SessionExpiredAt = DateTime.Now;
        //private bool Authenticated(HttpListenerContext context)
        //{
        //    /** Es una peticion que el Selector ha determinado como 'segura' y no requiere autentificarse */
        //    if (context.User == null)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        if (/*!MustAuthenticate*/ DateTime.Now < SessionExpiredAt)
        //        {
        //            HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
        //            if (AuthenticateUser?.Invoke(identity?.Name, identity?.Password) == true)
        //            {
        //                MustAuthenticate = true;
        //                SessionExpiredAt = DateTime.Now;
        //                return true;
        //            }
        //        }

        //        MustAuthenticate = false;
        //        SessionExpiredAt = DateTime.Now + TimeSpan.FromMinutes(1);
        //        /** Operador no autentificado. Presenta peticion de Login / Password. */
        //        /** Para presentar la pantalla de peticion LOGIN / PASSWORD... */
        //        context.Response.StatusCode = 401;
        //        context.Response.AddHeader("WWW-Authenticate",
        //            "Basic Realm=\"My WebDAV Server\""); // show login dialog
        //        byte[] message = new UTF8Encoding().GetBytes("Access denied");
        //        context.Response.ContentLength64 = message.Length;
        //        context.Response.OutputStream.Write(message, 0, message.Length);
        //        context.Response.Close();

        //        return false;
        //    }
        //}

        private bool IsAuthenticated(HttpListenerContext context)
        {
            // Control de los Post de Login
            if (context.Request.RawUrl.ToLower().Contains(Config?.LoginUrl.ToLower()))
            {
                if (context.Request.HttpMethod == "POST")
                {
                    // Autenticar.
                    if (!context.Request.HasEntityBody)
                    {
                        context.Response.Redirect(Config?.LoginUrl);
                        return false;
                    }
                    /** Leer los datos asociados */
                    using (System.IO.Stream body = context.Request.InputStream) // here we have data
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(body, context.Request.ContentEncoding))
                        {
                            var data = reader.ReadToEnd();
                            // Llamar a la rutina de AUT de la aplicacion.
                            AuthenticateUser?.Invoke(WebUtility.UrlDecode(data), (accepted, cause) => 
                            { 
                                if (accepted)
                                    SessionExpiredAt = DateTime.Now + TimeSpan.FromSeconds(Config.SessionDuration);
                                else
                                {
                                    //context.Response.Redirect(Config?.LoginUrl);
                                    ProcessFile(context.Response, (Config?.DefaultDir + Config?.LoginUrl).Substring(1),
                                        Config.LoginErrorTag, Config.LoginErrorTag + cause);
                                }
                            });
                        }
                    }
                    if (SessionExpiredAt > DateTime.Now)                    
                        context.Response.Redirect(Config?.DefaultUrl);
                    return false;
                }
                return true;
            }

            // Control de lo que tengo que dejar pasar
            if (SessionExpiredAt > DateTime.Now || Config.SecureUris.Contains(context.Request.RawUrl)) 
            {
                return true;
            }

            // Redireccionar.
            context.Response.Redirect(Config?.LoginUrl);
            return false;
        }
        protected Action<string, Action<bool, string>> AuthenticateUser = null;
        #endregion Autentificacion

        #region Testing
        private void logrequest(HttpListenerContext context)
        {
#if DEBUG
            Logger.Info<Logger>($"HTTP Request: {context.Request.HttpMethod} {context.Request.Url.OriginalString}");
            //Logger.Info<Logger>($"Raw URL: {context.Request.RawUrl}");
            if (context.Request.QueryString.Count > 0)
            {
                var array = (from key in context.Request.QueryString.AllKeys
                             from value in context.Request.QueryString.GetValues(key)
                             select string.Format("{0}={1}", key, value)).ToArray();

                Logger.Info<Logger>($"Query: {String.Join("##", array)}");
            }
#endif
        }
        #endregion

        #region Private

        HttpListener Listener = null;
        bool Enable { get; set; }
        string DisableCause { get; set; }
        Object locker = new Object();
        CfgServer Config { get; set; }
        #endregion
    }
}