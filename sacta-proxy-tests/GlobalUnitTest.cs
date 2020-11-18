using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;

using System.Net;
using System.Threading.Tasks;

using sacta_proxy;
using sacta_proxy.Helpers;
using sacta_proxy.model;
using sacta_proxy.Managers;

namespace sacta_proxy_tests
{
    [TestClass]
    public class GlobalUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var app = new SactaProxy(false);
            app.StartOnConsole(null);

            ConfigurationManager cfgMan = new ConfigurationManager();
            cfgMan.Get((cfg) => {
                // Enviar una trama Ini a TWR
                var twr = cfg.Dependencies.Where(d => d.Id == "TWR").FirstOrDefault();
                var msg = SactaMsg.MsgToScv(twr, SactaMsg.MsgType.Init, 0, 0).Serialize();
                var to = new IPEndPoint(IPAddress.Parse(twr.Comm.Listen.Lan1.Ip), twr.Comm.Listen.Port);
                var Sender = new UdpSocket(9000);

                Sender.Send(to, msg);
            });

            Task.Delay(TimeSpan.FromSeconds(1000)).Wait();
            app.StopOnConsole();
        }
    }
}
