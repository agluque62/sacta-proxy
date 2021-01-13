using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Net;
using System.Threading.Tasks;

using sacta_proxy.helpers;

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
            var ip1 = "10.20.90.1";
            var ip2 = "10.20.91.1";
            var mask1 = "10.20.90.0/25";
            var mask2 = "10.20.91.0/25";
            var res1 = IpHelper.IsInSubnet(mask1, ip1);
            var res2 = IpHelper.IsInSubnet(mask1, ip2);
            var res3 = IpHelper.IsInSubnet(mask2, ip1);
            var res4 = IpHelper.IsInSubnet(mask2, ip2);
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
        [TestMethod]
        public void TestingAddIp()
        {
            IpHelper.EthIfDelIpv4("10.12.60.35", "10.12.62.35");
        }
        [TestMethod]
        public void TestingForVirtualIp()
        {
            var EmptyIp = "";
            var BadIp = "10b.no es una ip";
            var LoopbackIp = "127.0.0.1";
            var LocalIp = "10.12.60.34";
            var ExternIp = "10.168.168.1";

            Assert.IsFalse(IpHelper.IsLocalIpV4Address(EmptyIp));
            Assert.IsFalse(IpHelper.IsLocalIpV4Address(BadIp));
            Assert.IsTrue(IpHelper.IsLocalIpV4Address(LoopbackIp));
            Assert.IsTrue(IpHelper.IsLocalIpV4Address(LocalIp));
            Assert.IsFalse(IpHelper.IsLocalIpV4Address(ExternIp));
        }
    }
}
