using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    }
}
