using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.model;

namespace sacta_proxy.Managers
{
    public abstract class BaseManager
    {
        public abstract void Start(Configuration.DependecyConfig cfg);
        public abstract void Stop();
        public abstract bool EnableTx { get; set; }
        public abstract object Status { get; }
        public virtual void SafeLaunchEvent<T>(EventHandler<T> handler, T args)
        {
            handler?.Invoke(this, args);
        }
    }
    public class ScvManagerEventArgs : EventArgs
    {
        public string ScvId { get; set; }
    }
    public class ScvManagerActivityArgs : ScvManagerEventArgs
    {
        public bool Activity { get; set; }
    }
    public class ScvManagerSectorizationArgs : ScvManagerEventArgs
    {
        public Dictionary<string, int> SectorMap { get; set; }
    }
    public class PsiManagerActivityArgs : EventArgs
    {
        public bool Activity { get; set; }
    }
}
