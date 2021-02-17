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
            public int WebActivityMinTimeout { get; set; }
            public string ActivateSactaLogic { get; set; }
            public GeneralConfig()
            {
                WebPort = 8091;
                ActivateSactaLogic = "AND";
                WebActivityMinTimeout = 30;
            }
        }
        public class LanItem
        {
            public string Ip { get; set; }
            public string FromMask { get; set; }
            public string McastGroup { get; set; }
            public string McastIf { get; set; }
            public LanItem()
            {
                Ip = "127.0.0.1";
                FromMask = "127.0.0.1/24";
                McastGroup = "225.12.101.1";
                McastIf = "127.0.0.1";
            }
        }
        public class CommItem
        {
            public int Port { get; set; }
            public LanItem Lan1 { get; set; }
            public LanItem Lan2 { get; set; }
            public CommItem()
            {
                Port = 9000;
                Lan1 = new LanItem();
                Lan2 = new LanItem();
            }
        }
        public class CommConfig
        {
            public CommItem Listen { get; set; }
            public CommItem SendTo { get; set; }
            public CommConfig()
            {
                Listen = new CommItem();
                SendTo = new CommItem();
            }
        }
        public class SactaProtocolSacta
        {
            public int Domain { get; set; }
            public int Center { get; set; }
            public int PsiGroup { get; set; }
            public int SpvGrup { get; set; }
            public List<int> Psis { get; set; }
            public List<int> Spvs { get; set; }
            public SactaProtocolSacta(bool bGenerate = false)
            {
                Domain = 1;
                Center = 107;
                PsiGroup = 110;
                SpvGrup = 85;
                if (bGenerate)
                {
                    Psis = new List<int>() { 111, 112, 113, 114, 7286, 7287, 7288, 7289 };
                    Spvs = new List<int>() { 86, 87, 88, 89, 7266, 7267, 7268, 7269 };
                }
            }
        }
        public class SactaProtocolScv
        {
            public int Domain { get; set; }
            public int Center { get; set; }
            public int Scv { get; set; }
            public SactaProtocolScv()
            {
                Domain = 1;
                Center = 107;
                Scv = 10;
            }
        }
        public class SactaProtocolConfig
        {
            public int TickAlive { get; set; }
            public int TimeoutAlive { get; set; }
            public int SectorizationTimeout { get; set; }
            public SactaProtocolSacta Sacta { get; set; }
            public SactaProtocolScv Scv { get; set; }
            public SactaProtocolConfig(bool bGenerate = false)
            {
                TickAlive = 5;
                TimeoutAlive = 30;
                SectorizationTimeout = 60;
                Sacta = new SactaProtocolSacta(bGenerate);
                Scv = new SactaProtocolScv();
            }
        }
        public class SectorizationDataConfig
        {
            public List<int> Sectors { get; set; }
            public List<int> Positions { get; set; }
            public List<int> Virtuals { get; set; }
            public string SectorsMap { get; set; }        // Sector Dependencia => Sector SCV.
            public string PositionsMap { get; set; }      // Posicion Dependencia => Posicion SCV.
            public SectorizationDataConfig()
            {
                Sectors = new List<int>();
                Positions = new List<int>();
                Virtuals = new List<int>();

                SectorsMap = "";
                PositionsMap = "";
            }
        }
        public class DependecyConfig
        {
            public string Id { get; set; }
            public CommConfig Comm { get; set; }
            public SactaProtocolConfig SactaProtocol { get; set; }
            public SectorizationDataConfig Sectorization { get; set; }
            public DependecyConfig(bool bGenerate = false)
            {
                Id = "IdDep";
                Comm = new CommConfig();
                SactaProtocol = new SactaProtocolConfig(bGenerate);
                Sectorization = new SectorizationDataConfig();
            }
        }
        /// <summary>
        /// Datos de Configuracion,
        /// </summary>
        public GeneralConfig General { get; set; }
        public DependecyConfig Psi { get; set; }
        public List<DependecyConfig> Dependencies { get; set; }
        public Configuration(bool bGenerate = false)
        {
            General = new GeneralConfig();
            Psi = new DependecyConfig(bGenerate);
            Dependencies = new List<DependecyConfig>();
            if (bGenerate)
            {
                Dependencies.Add(new DependecyConfig(bGenerate) { Id = "TWR" });
                Dependencies.Add(new DependecyConfig(bGenerate) { Id = "APP" });
            }
        }

        public static bool MapOfSectorsEntryValid(string input)
        {
            if (input.Contains(":") == false)
                return false;
            var pair = input.Split(':');
            if (pair.Count() != 2)
                return false;
            return int.TryParse(pair[0], out _) && int.TryParse(pair[1], out _);
        }
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
                    var cfg = new Configuration(true);
                    Write(cfg);
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
                var cfg = JsonHelper.Parse<Configuration>(ConfigurationData);
                Write(cfg);
                return true;
            }
            catch (Exception x)
            {
                Logger.Exception<ConfigurationManager>(x);
            }
            return false;
        }
        public void Write(Configuration cfg)
        {
            var data = JsonHelper.ToString(cfg);
            File.WriteAllText(FileName, data);
        }

    }
}
