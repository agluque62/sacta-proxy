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
#if !DEBUG1
            var dualMode = Properties.Settings.Default.ServerType == 1;
            var virtualIpIsLocal = IpHelper.IsLocalIpV4Address(Properties.Settings.Default.ScvServerIp);
            notify?.Invoke(dualMode, dualMode ? virtualIpIsLocal : true);
            return dualMode ? virtualIpIsLocal : true;
#else
            notify?.Invoke(Mode, Mode ? Master : true);
            return Mode ? Master : true;
#endif
        }
#if DEBUG
        public static void DebugMainStandbyModeSet(bool dual, bool master)
        {
            Mode = dual;
            Master = master;
        }
        static bool Mode { get; set; } = Properties.Settings.Default.ServerType == 1;
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
                object ret = null;
                MainStandbyCheck((isdual, main) =>
                {
                    ret = new
                    {
                        server = isdual==false ? "Simple" : "Dual",
                        scv = settings.ScvType == 0 ? "CD30" : "ULISES",
                        db = settings.DbConn == 0 ? "NO" : settings.DbConn == 1 ? "MySQL" : "Otra",
                        main,
                        dbconn = DbIsPresent
                    };
                });
                return ret;
            } 
        }
    }
}
