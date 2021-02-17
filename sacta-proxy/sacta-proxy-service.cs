using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.Helpers;
using sacta_proxy.WebServer;

namespace sacta_proxy
{
    public partial class SactaProxy : ServiceBase
    {
        private readonly SactaProxyWebApp sactaProxyWebApp = null;
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
            sactaProxyWebApp.Start(8091);           // TODO. Poner el puerto configurable.
        }
        protected override void OnStop()
        {
            // Se ejecuta al finalizar el programa.
            sactaProxyWebApp.Stop();
        }


    }
}
