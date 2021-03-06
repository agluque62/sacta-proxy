﻿using System;
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

        public abstract void Start(/*int ProtocolVersion, Configuration.DependecyConfig cfg*/);
        public abstract void Stop();
        public virtual bool TxEnabled { get; set; }
        public abstract object Status { get; }
        public virtual void SafeLaunchEvent<T>(EventHandler<T> handler, T args)
        {
            handler?.Invoke(this, args);
        }
        #region Rutinas comunes
        protected bool ActivityOnLan1
        {
            get { return (DateTime.Now - LastActivityOnLan1 < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive)); }
        }
        protected bool ActivityOnLan2
        {
            get { return (DateTime.Now - LastActivityOnLan2 < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive)); }
        }
        protected bool IsThereLanActivity
        {
            get
            {
                return ActivityOnLan1 || ActivityOnLan2;
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
        protected DateTime LastActivityOnLan1 { get; set; }
        protected DateTime LastActivityOnLan2 { get; set; }
        protected DateTime LastPresenceSended { get; set; }
        protected int Sequence { get; set; }
        protected ProcessStatusControl PS = new ProcessStatusControl();
        #endregion
    }
    public class ManagerEventArgs : EventArgs
    {
        public string ScvId { get; set; }
    }
    public class ActivityOnLanArgs : ManagerEventArgs
    {
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
