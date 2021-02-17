using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Net;
using System.Threading.Tasks;

using sacta_proxy.Helpers;

namespace sacta_proxy_tests
{
    [TestClass]
    public class IpUnitTest
    {
        [TestMethod]
        public void TestingCIDRnotation()
        {
            string mask1 = "225.12.101.1/32";
            IPAddress add = IPAddress.Parse("225.12.101.1");
            
            var res = IpHelper.IsInSubnet(mask1, add);
        }
        [TestMethod]
        public void IpMaskingTest()
        {
            var ip1 = "10.75.12.121";
            var ip2 = "10.75.130.121";
            var mask1 = "10.75.128.0/20";
            var mask2 = "10.75.254.0/23";
            var res1 = IpHelper.IsInSubnet(mask1, ip1);
            var res2 = IpHelper.IsInSubnet(mask1, ip2);
        }
        [TestMethod]
        public void TestingMcastSendingAndReceive()
        {
            var Listener = new UdpSocket("192.168.90.50", 9000);
            //Listener.Base.MulticastLoopback = false;
            Listener.Base.JoinMulticastGroup(IPAddress.Parse("225.12.101.1"), IPAddress.Parse("192.168.90.50"));
            Listener.Base.JoinMulticastGroup(IPAddress.Parse("225.212.101.1"), IPAddress.Parse("192.168.90.50"));
            Listener.NewDataEvent += (s, d) =>
            {
                var from = d.Client.Address;
               
            };
            Listener.BeginReceive();

            var Sender = new UdpSocket(9001);
            Sender.Base.JoinMulticastGroup(IPAddress.Parse("225.12.101.1"), IPAddress.Parse("192.168.90.50"));
            IPEndPoint to1 = new IPEndPoint(IPAddress.Parse("225.12.101.1"), 9000);
            IPEndPoint to2 = new IPEndPoint(IPAddress.Parse("225.212.101.1"), 9000);
            Task.Delay(1000).Wait();
            Sender.Send(to1, new byte[] { 1, 2, 3 });
            Task.Delay(1000).Wait();
            Sender.Send(to2, new byte[] { 1, 2, 3 });

            Task.Delay(1000).Wait();
        }
    }
}
