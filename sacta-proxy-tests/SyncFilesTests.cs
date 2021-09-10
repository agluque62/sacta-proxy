using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
    public class SyncFilesTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arranque y parada en modo 'no dual'
            using (serv02 = new DataSyncManager(false, "192.168.168.2", "224.100.10.1:1030")
            {
                MaxJitter=3,
                SyncListenerSpvPeriod=5,
                SyncSendingPeriod=10
            })
            {
                serv02.FileSyncEvent += OnSyncEvent02;
                Task.Delay(TimeSpan.FromSeconds(20)).Wait();
            }
        }
        [TestMethod]
        public void TestMethod2()
        {
            // Arranque y parada en modo 'Dual'. Supervision Listener...
            using (serv02 = new DataSyncManager(true, "192.168.168.2", "224.100.10.1:1030")
            {
                MaxJitter = 3,
                SyncListenerSpvPeriod = 5,
                SyncSendingPeriod = 11
            })
            {
                var cfg1 = Cfg1;
                serv02.FileSyncEvent += OnSyncEvent02;
                serv02.MonitorsFile(SupervisedFilename, cfg1.LastModification, JsonHelper.ToString(cfg1, false));

                Task.Delay(TimeSpan.FromSeconds(120)).Wait();
            }
        }
        [TestMethod]
        public void SyncMethodTest03()
        {
            var cfg1 = Cfg1;
            var cfg2 = Cfg2;
            // Arranque diferido.
            using (serv01 = new DataSyncManager(true, "192.168.168.1", "224.100.10.1:1030"))
            {
                serv01.FileSyncEvent += OnSyncEvent01;

                serv01.MonitorsFile(SupervisedFilename, cfg1.LastModification, JsonHelper.ToString(cfg1, false));
                Task.Delay(TimeSpan.FromSeconds(17)).Wait();
                using (serv02 = new DataSyncManager(true, "192.168.168.2", "224.100.10.1:1030"))
                {
                    serv02.FileSyncEvent += OnSyncEvent02;
                    serv02.InternalDelay = 1;

                    serv02.MonitorsFile(SupervisedFilename, cfg2.LastModification, JsonHelper.ToString(cfg2, false));
                    Task.Delay(TimeSpan.FromSeconds(60)).Wait();
                }
            }
        }
        [TestMethod]
        public void SyncMethodTest04()
        {
            var cfg1 = Cfg1;
            var cfg2 = Cfg2;

            cfg1.LastModification += TimeSpan.FromMinutes(0);
            cfg2.LastModification += TimeSpan.FromMinutes(0);
            // Arranque diferido.
            using (serv01 = new DataSyncManager(true, "192.168.168.1", "224.100.10.1:1030"))
            {
                serv01.FileSyncEvent += OnSyncEvent01;

                serv01.MonitorsFile(SupervisedFilename, cfg1.LastModification, JsonHelper.ToString(cfg1, false));
                Task.Delay(TimeSpan.FromSeconds(17)).Wait();
                using (serv02 = new DataSyncManager(true, "192.168.168.2", "224.100.10.1:1030"))
                {
                    serv02.FileSyncEvent += OnSyncEvent02;
                    serv02.InternalDelay = 1;

                    serv02.MonitorsFile(SupervisedFilename, cfg2.LastModification, JsonHelper.ToString(cfg2, false));
                    Task.Delay(TimeSpan.FromSeconds(30)).Wait();

                    cfg1.LastModification += TimeSpan.FromMinutes(10);
                    serv01.MonitorsFile(SupervisedFilename, cfg1.LastModification, JsonHelper.ToString(cfg1, false));
                    Task.Delay(TimeSpan.FromSeconds(30)).Wait();

                    cfg2.LastModification += TimeSpan.FromMinutes(20);
                    serv02.MonitorsFile(SupervisedFilename, cfg2.LastModification, JsonHelper.ToString(cfg2, false));
                    Task.Delay(TimeSpan.FromSeconds(30)).Wait();
                }
            }
        }
        protected Configuration Cfg1 => JsonHelper.Parse<Configuration>(System.IO.File.ReadAllText(Filename1));
        protected Configuration Cfg2 => JsonHelper.Parse<Configuration>(System.IO.File.ReadAllText(Filename2));
        protected void OnSyncEvent01(object sender, FilesSyncManagerEventArgs data)
        {
            Debug.WriteLine($"ON SVR01 Event");
            if (data.Error != default)
            {
                Debug.WriteLine($"ON SVR01 Event Error => {data.Error}");
            }
            else
            {
                Debug.WriteLine($"ON SVR01 Event Actualize {data.Item.Name} ({data.Item.Date}) Received Data => {data.Item.Data.Substring(0, 24)}...");
                var cfg = JsonHelper.Parse<Configuration>(data.Item.Data);
                serv01.MonitorsFile(SupervisedFilename, data.Item.Date, data.Item.Data);
            }
        }
        protected void OnSyncEvent02(object sender, FilesSyncManagerEventArgs data)
        {
            Debug.WriteLine($"ON SVR02 Event");
            if (data.Error != default)
            {
                Debug.WriteLine($"ON SVR02 Event Error => {data.Error}");
            }
            else
            {
                Debug.WriteLine($"ON SVR02 Event Actualize {data.Item.Name} ({data.Item.Date}) Received Data => {data.Item.Data.Substring(0, 24)}...");
                var cfg = JsonHelper.Parse<Configuration>(data.Item.Data);
                serv02.MonitorsFile(SupervisedFilename, data.Item.Date, data.Item.Data);
            }
        }
        protected string SupervisedFilename => "sacta-proxy-config.json";
        protected string Filename1 => "sacta-proxy-config-1.json";
        protected string Filename2 => "sacta-proxy-config-2.json";
        protected DataSyncManager serv01 = null;
        protected DataSyncManager serv02 = null;
    }
}
