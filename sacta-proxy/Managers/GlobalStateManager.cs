using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.helpers;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{
    public class GlobalStateManager
    {
        public static bool MainStandbyCheck(Action<bool, bool> notify=null)
        {
#if !DEBUG
            var dualMode = Properties.Settings.Default.ServerType == 1;
            var virtualIpIsLocal = IpHelper.IsLocalIpV4Address(Properties.Settings.Default.ScvServerIp);
            notify?.Invoke(dualMode, virtualIpIsLocal);
            return virtualIpIsLocal;
#else
            notify?.Invoke(Mode, Master);
            return Master;
#endif
        }
#if DEBUG
        public static void DebugMainStandbyModeSet(bool dual, bool master)
        {
            Mode = dual;
            Master = master;
        }
        static bool Mode { get; set; } = false;
        static bool Master { get; set; } = false;
#endif
        static DateTime LastDbCheckTime = DateTime.MinValue;
        static bool LastDbStatus = false;
        public static bool DbIsPresent
        {
            get
            {
                var elapsed = DateTime.Now - LastDbCheckTime;
                if (elapsed > TimeSpan.FromMinutes(2))
                {
                    LastDbCheckTime = DateTime.Now;
                    LastDbStatus = DbControl.IsPresent();
                }
                return LastDbStatus;
            }
        }
        public static object Info 
        { 
            get
            {
                var settings = Properties.Settings.Default;
                return new
                {
                    server = settings.ServerType == 0 ? "Simple" : "Dual",
                    scv = settings.ServerType == 0 ? "CD30" : "ULISES",
                    db = settings.DbConn == 0 ? "NO" : settings.DbConn == 1 ? "MySQL" : "Otra",
                    main = MainStandbyCheck(),
                    dbconn = DbIsPresent
                };
            } 
        }
    }
}
