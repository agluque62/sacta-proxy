using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;


using sacta_proxy;
using sacta_proxy.helpers;
using sacta_proxy.model;
using sacta_proxy.Managers;

namespace sacta_proxy_tests
{
    [TestClass]
    public class GenericUnitTest
    {
        [TestMethod]
        public void BuildingMsgsTest()
        {
            ushort sUser = 34;
            byte[] user = BitConverter.GetBytes(UInt16.Parse(sUser.ToString()));
            Array.Reverse(user);

            ConfigurationManager cfgMan = new ConfigurationManager();
            cfgMan.Get((cfg) => {
                // Enviar una trama Ini a TWR
                var twr = cfg.Dependencies.Where(d => d.Id == "TWR").FirstOrDefault();
                var msg = SactaMsg.MsgToScv(twr, SactaMsg.MsgType.Init, 0, 0).Serialize();

                //var to = new IPEndPoint(IPAddress.Parse(twr.Comm.Listen.Lan1.Ip), twr.Comm.Listen.Port);
                //var Sender = new UdpSocket(9000);
                //Sender.Send(to, msg);
            });
        }

        [TestMethod]
        public void StringSplitTest()
        {
            var inputs = new List<string>()
            {
                "",",","120:","1:1,kkk,32:0","1:1,kk:mm,2:kk"
            };

            inputs.ForEach(input =>
            {
                var splitted = input.Split(',')
                    .Where(i => Configuration.MapOfSectorsEntryValid(i))
                    .ToDictionary(k => int.Parse(k.Split(':')[0]), v => int.Parse(v.Split(':')[1]));
            });
        }

        [TestMethod]
        public void EventQueueTest()
        {
            var eq = new EventQueue();
            eq.Start();
            eq.Enqueue("1", () => { Debug.WriteLine("Evento 1"); });
            eq.Enqueue("2", () => { Debug.WriteLine("Evento 2"); });
            eq.Enqueue("3", () => { Debug.WriteLine("Evento 3"); });
            eq.Enqueue("4", () => { Debug.WriteLine("Evento 4"); });
            eq.ControlledStop();
        }
        [TestMethod]
        public void LastErrorsTests()
        {
            var PS = new ProcessStatusControl();

            PS.SignalWarning<GenericUnitTest>("Warning #001", null);
            PS.SignalFatal<GenericUnitTest>("Fatal #001", null);
            PS.SignalWarning<GenericUnitTest>("Warning #002", null);
            PS.SignalFatal<GenericUnitTest>("Fatal #002", null);
            PS.SignalWarning<GenericUnitTest>("Warning #003", null);
            PS.SignalFatal<GenericUnitTest>("Fatal #003", null);
            PS.SignalWarning<GenericUnitTest>("Warning #004", null);
            PS.SignalFatal<GenericUnitTest>("Fatal #004", null);
            PS.SignalWarning<GenericUnitTest>("Warning #005", null);
            PS.SignalFatal<GenericUnitTest>("Fatal #005", null);
        }
        [TestMethod]
        public void TestModifyHmiConfig()
        {
            Assert.IsTrue(HMIConfigHelper.CambioModoNocturno("hmi.exe.config", "False", "True"));
        }
        [TestMethod]
        public void TestAllIdsOnMap()
        {
            int maxIdSect = 9999;
            int minIdSect = 1;
            int maxIdPos = 255;
            int minIdPos = 1;

            var dep = new Configuration.DependecyConfig();

            dep.Sectorization.Sectors = "1,2,3,4";
            dep.Sectorization.Virtuals = "5";
            dep.Sectorization.Positions = "11,12,13,14,15,300";
            dep.Sectorization.PositionsMap = "11:1,12:2,77777777777777777777777777777777:9";
            dep.Sectorization.SectorsMap = "1:99,2:98,3:97,4:96,5:12987";

            var sectIds = GenericHelper.Split(dep.Sectorization.Sectors, ',').Select(s => GenericHelper.ToInt(s))
                .Union(GenericHelper.Split(dep.Sectorization.Virtuals, ',').Select(s => GenericHelper.ToInt(s)))
                .ToList();
            var posIds = GenericHelper.Split(dep.Sectorization.Positions, ',')
                .Select(p => GenericHelper.ToInt(p))
                .ToList();

            var badSectIds = sectIds
                .Union(GenericHelper.Split(dep.Sectorization.SectorsMap, ':', ',').Select(item => GenericHelper.ToInt(item)))
                .Where(s => s < minIdSect || s > maxIdSect)
                .ToList();
            var badPosIds = posIds
                .Union(GenericHelper.Split(dep.Sectorization.PositionsMap,':', ',').Select(item => GenericHelper.ToInt(item)))
                .Where(p => p < minIdPos || p > maxIdPos)
                .ToList();

            var invsect = GenericHelper.Split(dep.Sectorization.SectorsMap, ',')
                .Select(p => GenericHelper.Split(p, ':').FirstOrDefault())
                .Select(s => GenericHelper.ToInt(s))
                .Where(s => !sectIds.Contains(s))
                .ToList();
            var invpos = GenericHelper.Split(dep.Sectorization.PositionsMap, ',')
                .Select(p => GenericHelper.Split(p, ':').FirstOrDefault())
                .Select(s => GenericHelper.ToInt(s))
                .Where(s => !posIds.Contains(s))
                .ToList();

        }

    }

    public class HMIConfigHelper
    {
        public static bool CambioModoNocturno(string filename, string actualvalue, string newvalue)
        {
            if (File.Exists(filename))
            {
                var find = $"      <setting name=\"ModoNocturno\" serializeAs=\"String\">\r\n        <value>{actualvalue}</value>\r\n      </setting>";
                var repl = $"      <setting name=\"ModoNocturno\" serializeAs=\"String\">\r\n        <value>{newvalue}</value>\r\n      </setting>";

                File.WriteAllText(filename + ".old", File.ReadAllText(filename)); // backup

                var data = File.ReadAllText(filename);
                if (data.IndexOf(find) == -1) return false;

                File.WriteAllText(filename, data.Replace(find, repl)); // 
                return true;
            }
            return false;
        }
    }
}
