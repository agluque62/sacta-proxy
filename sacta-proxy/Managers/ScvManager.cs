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
    class ScvManager
    {
        public void Start(Configuration.DependecyConfig cfg)
        {
            Id = cfg.Id;
            Logger.Info<ScvManager>($"Starting ScvManager for {Id}...");

        }
        public void Stop()
        {
            Logger.Info<ScvManager>($"Stopping ScvManager for {Id}...");

        }
        public object Status
        {
            get
            {
                return new { Id, res = "En implementacion" };
            }
        }
        string Id { get; set; }
    }
}
