using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.helpers;

namespace sacta_proxy.Managers
{
    public class MainStandbyManager
    {
        public static void Check(Action<bool, bool> notify)
        {
#if !DEBUG
            var dualMode = Properties.Settings.Default.ServerType == 1;
            var virtualIpIsLocal = IpHelper.IsLocalIpV4Address(Properties.Settings.Default.ScvServerIp);
            notify(dualMode, virtualIpIsLocal);
#else
            notify(Mode, Master);
#endif
        }
#if DEBUG
        public static void SetMode(bool dual, bool master)
        {
            Mode = dual;
            Master = master;
        }
        static bool Mode { get; set; } = false;
        static bool Master { get; set; } = false;
#endif
    }
}
