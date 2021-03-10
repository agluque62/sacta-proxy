using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net;

using sacta_proxy.model;
using sacta_proxy.helpers;

namespace sacta_proxy.Managers
{
    public abstract class BaseManager
    {
        public event EventHandler<ActivityOnLanArgs> EventActivity;
        protected enum SactaState { WaitingSactaActivity, WaitingSectorization, SendingPresences, Stopped }
        public enum WhatLanItems { Lan1, Lan2, Global }
        public abstract void Start(/*int ProtocolVersion, Configuration.DependecyConfig cfg*/);
        public abstract void Stop();
        public virtual bool TxEnabled { get; set; }
        public abstract object Status { get; }
        public virtual void SafeLaunchEvent<T>(EventHandler<T> handler, T args)
        {
            handler?.Invoke(this, args);
        }
        public virtual void LaunchEventActivity(WhatLanItems item, bool status)
        {
            SafeLaunchEvent<ActivityOnLanArgs>(EventActivity, new ActivityOnLanArgs()
            {
                ScvId = Cfg.Id,
                What = item,
                ActivityOnLan = status
            });
        }
        #region Rutinas comunes
        protected bool ActivityOnLan1
        {
            get
            {
                var currentActivity = (DateTime.Now - LastActivityOfLan1 < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive));
                if (currentActivity != lastStateOfLan1)
                {
                    LaunchEventActivity(WhatLanItems.Lan1, currentActivity);
                }
                lastStateOfLan1 = currentActivity;
                return currentActivity;
            }
        }
        protected bool ActivityOnLan2
        {
            get
            {
                var currentActivity = (DateTime.Now - LastActivityOfLan2 < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive));
                if (currentActivity != lastStateOfLan2)
                {
                    LaunchEventActivity(WhatLanItems.Lan2, currentActivity);
                }
                lastStateOfLan2 = currentActivity;
                return currentActivity;
            }
        }
        protected bool IsThereLanActivity
        {
            get
            {
                var lan1 = ActivityOnLan1;
                var lan2 = ActivityOnLan2;
                return lan1 || lan2;
            }
        }
        protected IPEndPoint Lan1Listen { get; set; }
        protected IPEndPoint Lan1Sendto { get; set; }
        protected IPEndPoint Lan2Listen { get; set; }
        protected IPEndPoint Lan2Sendto { get; set; }
        #endregion

        #region Datos 
        protected object Locker { get; set; }
        protected string Id { get; set; }
        protected int Version { get; set; }
        protected Configuration.DependecyConfig Cfg { get; set; }
        protected Timer TickTimer { get; set; }
        protected DateTime LastActivityOfLan1 { get; set; }
        protected DateTime LastActivityOfLan2 { get; set; }
        protected DateTime LastPresenceSended { get; set; }
        protected int Sequence { get; set; }
        protected ProcessStatusControl PS = new ProcessStatusControl();
        private bool lastStateOfLan1 = false;
        private bool lastStateOfLan2 = false;
        #endregion
    }
    public class ManagerEventArgs : EventArgs
    {
        public string ScvId { get; set; }
    }
    public class ActivityOnLanArgs : ManagerEventArgs
    {
        public BaseManager.WhatLanItems What { get; set; }
        public bool ActivityOnLan { get; set; }
    }
    public class SectorizationReceivedArgs : ManagerEventArgs
    {
        public bool Accepted { get; set; }
        public string ReceivedMap { get; set; }
        public Dictionary<string, int> SectorMap { get; set; }
        public string RejectCause { get; set; }
        public Action<bool> Acknowledge { get; set; }
    }
    public class SectorizationRequestArgs : ManagerEventArgs 
    { 
    }
    
}
