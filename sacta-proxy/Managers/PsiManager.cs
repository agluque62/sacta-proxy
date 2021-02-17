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
    class PsiManager : BaseManager
    {
        #region Events
        public EventHandler<PsiManagerActivityArgs> EventActivity;
        #endregion Events
        public override void Start(Configuration.DependecyConfig cfg)
        {
            Logger.Info<PsiManager>($"Starting PsiManager...");

            EnableTx = false;

        }
        public override void Stop()
        {
            Logger.Info<PsiManager>($"Stopping PsiManager...");

        }

        public override object Status 
        { 
            get
            {
                return new { res = "En implementacion" };
            }
        }

        public override bool EnableTx { get; set; }
    }
}
