using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Utilities;
using ClusterLib;
using helpers;

namespace ClusterNodeSimul
{
    class ClusterNode
    {
        public string ListenEndp { get => $"{Config.Ip}:{Config.Port}"; }
        public string RemoteEndp { get => $"{Config.EpIp}:{Config.EpPort}"; }
        public NodeState State { get; set; }
        public string LocalStatus { get => ClusterModule.State.LocalNode.ToString(); }
        public string RemoteStatus { get => ClusterModule.State.RemoteNode.ToString(); }
        public ClusterSettings Config { get; set; } = new ClusterSettings()
        {
            NodeId = "SimulatedNode",
            Ip = "192.168.0.1",
            Port = 6001,
            EpIp = "192.168.0.1",
            EpPort = 6000,
            MaintenanceServerForTraps = "127.0.0.1",
            VirtualIps = new List<ClusterIpSetting>()
            {
                new ClusterIpSetting(){ AdapterIp = "10.12.90.1", ClusterIp = "10.12.90.88", ClusterMsk = "255.255.255.0", AdapterMask=1, AdapterIndex=-1, VirtualIpContext=-1  },
                new ClusterIpSetting(){ AdapterIp = "10.20.90.1", ClusterIp = "10.20.90.88", ClusterMsk = "255.255.255.0", AdapterMask=2, AdapterIndex=-1, VirtualIpContext=-1  },
                new ClusterIpSetting(){ AdapterIp = "10.21.94.1", ClusterIp = "10.21.94.88", ClusterMsk = "255.255.255.0", AdapterMask=4, AdapterIndex=-1, VirtualIpContext=-1  },
            },
            TimeToStart = 5000,
            Tick = 1000,
            RemoteTimeout = 5000
        };
        public ClusterNode()
        {
            State = NodeState.NoValid;
        }
        public void Go()
        {
            ClusterModule = new Cluster(Config)/* { SimulatedAdapters = Config.VirtualIps.Count }*/;
            ClusterModule.Run();
        }
        public void Stop()
        {
            ClusterModule?.Dispose();
        }

        public void SendRemoteStateAsk()
        {

        }

        public void ClusterStateSwitch()
        {
            if (ClusterModule?.State.LocalNode.State == NodeState.NoActive)
            {
                ClusterModule?.Activate();
            }
            else
            {
                ClusterModule?.Deactivate();
            }
        }

        Cluster ClusterModule { get; set; }
    }
}
