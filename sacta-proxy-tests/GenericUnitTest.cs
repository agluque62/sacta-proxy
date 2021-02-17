using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Linq;
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
    }
}
