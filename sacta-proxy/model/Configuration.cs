using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using sacta_proxy.helpers;

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
            public int HistoryMaxDays { get; set; }
            public int HistoryMaxItems { get; set; }
            public GeneralConfig()
            {
                WebPort = 8091;
                ActivateSactaLogic = "AND";
                WebActivityMinTimeout = 30;
                HistoryMaxDays = 7;
                HistoryMaxItems = 1000;
            }
        }
        public class ItfItem
        {
            public string Ip { get; set; }
            public string IpTo { get; set; }
            public string FromMask { get; set; }
            public string McastGroup { get; set; }
            //public string McastIf { get; set; }
            public ItfItem()
            {
                Ip = "127.0.0.1";
                IpTo = "127.0.0.1";
                FromMask = "127.0.0.1/24";
                McastGroup = "225.12.101.1";
                //McastIf = "127.0.0.1";
            }
        }
        public class CommItem
        {
            public int Port { get; set; }
            public ItfItem Lan1 { get; set; }
            public ItfItem Lan2 { get; set; }
            public CommItem()
            {
                Port = 9000;
                Lan1 = new ItfItem();
                Lan2 = new ItfItem();
            }
        }
        public class CommConfig
        {
            public int ListenPort { get; set; }
            public int SendingPort { get; set; }
            //public CommItem Listen { get; set; }
            //public CommItem SendTo { get; set; }
            public ItfItem If1 { get; set; }
            public ItfItem If2 { get; set; }
            public CommConfig()
            {
                //Listen = new CommItem();
                //SendTo = new CommItem();
                If1 = new ItfItem();
                If2 = new ItfItem();
                ListenPort = 15100;
                SendingPort = 19204;
            }
        }
        public class SactaProtocolSacta
        {
            public int Domain { get; set; }
            public int Center { get; set; }
            public int PsiGroup { get; set; }
            public int SpvGrup { get; set; }
            //public List<int> Psis { get; set; }
            public string Psis { get; set; }
            //public List<int> Spvs { get; set; }
            public string Spvs { get; set; }
            public SactaProtocolSacta(bool bGenerate = false)
            {
                Domain = 1;
                Center = 107;
                PsiGroup = 110;
                SpvGrup = 85;
                if (bGenerate)
                {
                    //Psis = new List<int>() { 111, 112, 113, 114, 7286, 7287, 7288, 7289 };
                    //Spvs = new List<int>() { 86, 87, 88, 89, 7266, 7267, 7268, 7269 };
                    Psis = "111, 112, 113, 114, 7286, 7287, 7288, 7289";
                    Spvs = "86, 87, 88, 89, 7266, 7267, 7268, 7269";
                }
            }
            public List<int> PsisList()
            {
                return Configuration.String2ListInt(Psis);
            }
            public List<int> SpvsList()
            {
                return Configuration.String2ListInt(Spvs);
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
            public string Sectors { get; set; }
            public string Positions { get; set; }
            public string Virtuals { get; set; }
            public string SectorsMap { get; set; }        // Sector Dependencia => Sector SCV.
            public string PositionsMap { get; set; }      // Posicion Dependencia => Posicion SCV.
            public SectorizationDataConfig(bool bGenerate = false)
            {
                Sectors = "";
                Positions = "";
                Virtuals = "";
                SectorsMap = "";
                PositionsMap = "";
                if (bGenerate)
                {
                    Sectors = "0";
                    Positions = "0";
                }
            }
            public List<int> SectorsList()
            {
                return String2ListInt(Sectors);
            }
            public List<int> PositionsList()
            {
                return String2ListInt(Positions);
            }
            public List<int> VirtualsList()
            {
                return String2ListInt(Virtuals);
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
                Sectorization = new SectorizationDataConfig(bGenerate);
            }
        }
        /// <summary>
        /// Datos de Configuracion,
        /// </summary>
        public int ProtocolVersion { get; set; }
        public int InCluster { get; set; }
        public GeneralConfig General { get; set; }
        public DependecyConfig Psi { get; set; }
        public List<DependecyConfig> Dependencies { get; set; }
        public Configuration(bool bGenerate = false)
        {
            ProtocolVersion = 1;
            InCluster = 0;
            General = new GeneralConfig();
            Psi = new DependecyConfig(bGenerate) { Id = "SCV" };
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
        public static string AgreggateString(string input, string val)
        {
            if (input != "") input += ",";
            return input + val;
        }

        public static string ListString2String(List<string> input)
        {
            if (input.Count() == 0) return "";
            return input.Aggregate((i, j) => i + "," + j.ToString());
        }
        public static List<int> String2ListInt(string input)
        {
            if (input == "") return new List<int>();
            var l1 = input.Split(',').ToList();
            var l2 = l1.Select(i => int.Parse(i)).ToList();
            return l2;
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
                if (cfg != null)
                {
                    Write(cfg);
                    return true;
                }
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
