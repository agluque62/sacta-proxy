using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using sacta_proxy.helpers;
using sacta_proxy.Managers;

namespace sacta_proxy
{
    static class Program
    {
        static readonly string AppGuid = "{18750FE3-20CF-4205-9AE9-58AAAB552009}";
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main(string[] args)
        {
            using (Mutex mutex = new Mutex(false, "Global\\" + AppGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    Logger.Fatal<SactaProxyApp>("Instance Already running...");
                    return;
                }
                (new SactaProxyApp()).Run(args);
            }

        }
    }

    class SactaProxyApp
    {
        public void Run(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyGlobalExceptionHandler);

            if (args.Contains("-console"))
            {
                Logger.Info<SactaProxyApp>("Arrancando en Modo Consola. Pulsa 'q' para salir...");
                var app = new SactaProxy();
                app.StartOnConsole(args);

                char key;
                while ((key = Console.ReadKey(true).KeyChar) != 'q')
                {
                    // Por si se quieren simular acciones mediante el teclado...
#if DEBUG
                    switch (key)
                    {
                        case 's':
                            GlobalStateManager.DebugMainStandbyModeSet(false, false);
                            break;
                        case 'p':
                            GlobalStateManager.DebugMainStandbyModeSet(true, true);
                            break;
                        case 'r':
                            GlobalStateManager.DebugMainStandbyModeSet(true, false);
                            break;
                        case '0':
                            app.Reset();
                            break;
                    }
#endif
                }

                app.StopOnConsole();
                Logger.Info<SactaProxyApp>("Fin del Programa.");
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new SactaProxy()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
        void MyGlobalExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception x = (Exception)args.ExceptionObject;
            Logger.Exception<SactaProxyApp>(x);
        }

    }
}
