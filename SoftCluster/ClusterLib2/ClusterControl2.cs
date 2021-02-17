using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;

using ClusterLib;
using Utilities;

using ClusterLib2.model;
using helpers;
using WebServer;

namespace ClusterLib2
{

    public class ClusterControl2 
    {
        #region Public Methods
        public void Run()
        {
            ClusterConfigManager.Get((cfg) =>
            {
                Cfg = cfg;
                Worker = new EventQueue();
                Worker.Start();

                Listener = new UdpSocket(cfg.IpInterna, cfg.PuertoInterno);
                Listener.NewDataEvent += OnNewData;
                Listener.BeginReceive();

                StartWebService();
            });
        }

        public void Stop()
        {
            Worker.Enqueue("Stopping", () =>
            {
                Listener.NewDataEvent -= OnNewData;

                StopWebService();
            });
            Worker.ControlledStop();
        }

        public void Activate()
        {
            Worker.Enqueue("Public Activate", () =>
            {

            });
        }

        public void Deactivate()
        {
            Worker.Enqueue("Public Deactivate", () =>
            {

            });
        }
        #endregion Public Methods

        void OnNewData(object sender, DataGram dg)
        {
            Worker.Enqueue("Cluster OnNewData", () =>
            {
                try
                {
                    if (dg.Data[0] == '{')
                    {
                        // Debug.WriteLine("Llega un JSON...");
                    }
                    else
                    {
                        using (var ms = new MemoryStream(dg.Data))
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            object msg = bf.Deserialize(ms);

                            if (msg is MsgType type)
                            {
                                switch (type)
                                {
                                    case MsgType.Activate:
                                        //Debug.WriteLine("Activate(true)");
                                        break;
                                    case MsgType.Deactivate:
                                        //Debug.WriteLine("Deactivate()");
                                        break;
                                    case MsgType.GetState:
                                        //Debug.WriteLine("SendState()");
                                        break;
                                    default:
                                        //Debug.WriteLine("Error 1");
                                        break;
                                }
                            }
                            else if (msg is NodeInfo)
                            {
                                //Debug.WriteLine("NodeInfo...");
                            }
                            else
                            {
                                throw new Exception("Mensaje Desconocido");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"Excepcion: {ex.Message}");
                }
            });
        }
        void OnTick(object sender, ElapsedEventArgs e)
        {
            Worker.Enqueue("Cluster OnTick", () =>
            {

            });
        }

        #region WEB Control
        void StartWebService()
        {
            WebApp = new WebServerApp(null, null, (s, ev) => { Logger.Debug<WebServerApp>(ev.Message); }, (s, ev) => { Logger.Exception<WebServerApp>(ev.X); });

            webCallbacks.Clear();
            webCallbacks.Add("/config", OnWebRequestConfig);
            webCallbacks.Add("/status", OnWebRequestState);

            WebApp?.Start(Cfg.PuertoWeb, 0, webCallbacks);

            Logger.Info<ClusterControl2>("Servidor WEB Arrancado.");
        }
        protected void OnWebRequestState(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new
                {
                    res = "ok",
                    //user = SystemUsers.CurrentUserId,
                    //version = GenericHelper.VersionManagement.AssemblyVersion,
                    //global = GlobalStateManager.Info,
                    //Status
                }, false));
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
                ClusterConfigManager.Get((cfg) =>
                {
                    data = JsonHelper.ToString(new { res = "ok", cfg }, false);
                    context.Response.StatusCode = 200;
                });
                sb.Append(data);
            }
            else if (context.Request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string strData = reader.ReadToEnd();
                    if (ClusterConfigManager.Set(strData))
                    {
                        ///** Reiniciar el Servicio */
                        //Reset();
                        ///** Historico */
                        //History.Add(HistoryItems.UserConfigChange, SystemUsers.CurrentUserId);
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

        void StopWebService() 
        {
            WebApp?.Stop();
            webCallbacks.Clear();
            Logger.Info<ClusterControl2>("Servidor Web Detenido.");
        }
        #endregion Web Control

        #region Private Methods

        #endregion Private Methods

        ClusterConfig Cfg { get; set; }
        private WebServerApp WebApp { get; set; }
        private readonly Dictionary<string, wasRestCallBack> webCallbacks = new Dictionary<string, wasRestCallBack>();
        EventQueue Worker { get; set; }
        UdpSocket Listener { get; set; }
    }
}
