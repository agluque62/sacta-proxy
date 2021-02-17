using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.Helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{
    class PsiManager
    {
        public void Start(Configuration.DependecyConfig cfg)
        {
            Logger.Info<PsiManager>($"Starting PsiManager...");


        }
        public void Stop()
        {
            Logger.Info<PsiManager>($"Stopping PsiManager...");

        }

        public object Status 
        { 
            get
            {
                return new { res = "En implementacion" };
            }
        }
    }
}
