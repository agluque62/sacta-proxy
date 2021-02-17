using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using sacta_proxy.Helpers;

namespace sacta_proxy.model
{
    /// <summary>
    /// 
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Items de configuracion
        /// </summary>
        public class GeneralConfig
        {
            public int WebPort { get; set; }
            public GeneralConfig()
            {
                WebPort = 8091;
            }
        }
        public class CommItem
        {
            public int Port { get; set; }
            public string Ip1 { get; set; }
            public string Ip2 { get; set; }
            public string NetwotkIf { get; set; }
        }
        public class CommConfig
        {
            public CommItem Listen { get; set; }
            public CommItem SendTo { get; set; }
        }
        public class SactaProtocolSacta
        {
            public int Domain { get; set; }
            public int Center { get; set; }
            public int PsiGroup { get; set; }
            public int SpvGrup { get; set; }
            public List<int> Psis { get; set; }
            public List<int> Spvs { get; set; }
        }
        public class SactaProtocolScv
        {
            public int Domain { get; set; }
            public int Center { get; set; }
            public int Scv { get; set; }
        }
        public class SactaProtocolConfig
        {
            public int TickAlive { get; set; }
            public int TimeoutAlive { get; set; }
            public SactaProtocolSacta Sacta { get; set; }
            public SactaProtocolScv Scv { get; set; }
        }
        public class SectorizationDataConfig
        {
            public List<int> Sectors { get; set; }
            public List<int> Positions { get; set; }
            public List<int> Virtuals { get; set; }
        }
        public class DependecyConfig
        {
            public string Id { get; set; }
            public CommConfig Comm { get; set; }
            public SactaProtocolConfig SactaProtocol { get; set; }
            public SectorizationDataConfig Sectorization { get; set; }
        }
        /// <summary>
        /// Datos de Configuracion,
        /// </summary>
        public GeneralConfig General { get; set; }
        public DependecyConfig Proxy { get; set; }
        public List<DependecyConfig> Dependencies { get; set; }
    }
    public class ConfigurationManager
    {
        const string FileName = "sacta-proxy-config.json";
        public void Get(Action<Configuration> deliver)
        {
            try
            {
                if (File.Exists(FileName))
                {
                    var data = File.ReadAllText(FileName);
                    var cfg = JsonHelper.Parse<Configuration>(data);
                    deliver(cfg);
                }
                else
                {
                    var cfg = new Configuration();
                    var data = JsonHelper.ToString(cfg);
                    File.WriteAllText(FileName, data);
                    deliver(cfg);
                }
            }
            catch(Exception x)
            {
                Logger.Exception<ConfigurationManager>(x);
            }

        }
        public bool Set(string ConfigurationData)
        {
            try
            {
                var cfg = new Configuration();
                var data = JsonHelper.ToString(cfg);
                File.WriteAllText(FileName, data);
                return true;
            }
            catch (Exception x)
            {
                Logger.Exception<ConfigurationManager>(x);
            }
            return false;
        }

    }
}
