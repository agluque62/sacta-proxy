using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using helpers;
namespace ClusterLib2.model
{
    public class IpCluster
    {
        public string Id { get; set; }
        public string IpF { get; set; }
        public string Msk { get; set; }
        public string IpV { get; set; }
    }
    public class ClusterConfig
    {
        public string IpInterna { get; set; }
        public int PuertoInterno { get; set; }
        public int PuertoWeb { get; set; }
        public List<IpCluster> IpsCluster { get; set; }
        public ClusterConfig(bool generate = false)
        {
            if (generate)
            {
                IpInterna = "127.0.0.1";
                PuertoInterno = 0;
                PuertoWeb = 8092;
                IpsCluster = new List<IpCluster>();
                for (int itf = 0; itf < 3; itf++)
                {
                    var Item = new IpCluster()
                    {
                        Id = $"VLan-{itf}",
                        IpF = "127.0.0.1",
                        Msk = "255.255.255.0",
                        IpV = "127.0.0.2"
                    };
                    IpsCluster.Add(Item);
                }
            }
        }
    }
    public class ClusterConfigManager
    {
        const string FileName = "cluster-config.json";
        public static void Get(Action<ClusterConfig> deliver)
        {
            try
            {
                if (File.Exists(FileName))
                {
                    var data = File.ReadAllText(FileName);
                    var cfg = JsonHelper.Parse<ClusterConfig>(data);
                    deliver(cfg);
                }
                else
                {
                    var cfg = new ClusterConfig(true);
                    Write(cfg);
                    deliver(cfg);
                }
            }
            catch (Exception x)
            {
                Logger.Exception<ClusterConfigManager>(x);
            }
        }
        public static bool Set(string ConfigurationData)
        {
            try
            {
                var cfg = JsonHelper.Parse<ClusterConfig>(ConfigurationData);
                Write(cfg);
                return true;
            }
            catch (Exception x)
            {
                Logger.Exception<ClusterConfigManager>(x);
            }
            return false;
        }
        public static void Write(ClusterConfig cfg)
        {
            var data = JsonHelper.ToString(cfg);
            File.WriteAllText(FileName, data);
        }
    }
}
