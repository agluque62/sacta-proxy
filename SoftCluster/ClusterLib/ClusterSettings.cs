using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterLib
{
    public class ClusterIpSetting
    {
        public string AdapterIp { get; set; }
        public int AdapterMask { get; set; }
        public string ClusterIp { get; set; }
        public string ClusterMsk { get; set; }

        public int AdapterIndex { get; set; }
        public int VirtualIpContext { get; set; }
    }
    public class ClusterSettings
    {
        public string NodeId;
        public string Ip;
        public int Port;
        public string EpIp;
        public int EpPort;
        //public string AdapterIp1;
        //public string AdapterIp2;
        //public string ClusterIp1;
        //public string ClusterIp2;
        //public string ClusterMask1;
        //public string ClusterMask2;
        public string MaintenanceServerForTraps;
        public int TimeToStart;
        public List<ClusterIpSetting> VirtualIps;
        public int Tick;
        public int RemoteTimeout;
    }
}
