using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Management;

namespace sacta_proxy.helpers
{
    public class IpHelper
    {
        /// <summary>
        /// Determina si una IP está en una subred. 
        /// El formato de la máscara debe ser CIDR notation (IPAddress/PrefixLength => xxx.xxx.xxx.xxx/yyy)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="subnetMask"></param>
        /// <returns></returns>
        public static bool IsInSubnet(string subnetMask, IPAddress address)
        {
            var slashIdx = subnetMask.IndexOf("/");
            if (slashIdx == -1)
            { // We only handle netmasks in format "IP/PrefixLength".
                throw new NotSupportedException("Only SubNetMasks with a given prefix length are supported.");
            }

            // First parse the address of the netmask before the prefix length.
            var maskAddress = IPAddress.Parse(subnetMask.Substring(0, slashIdx));
            if (maskAddress.AddressFamily != address.AddressFamily)
            { // We got something like an IPV4-Address for an IPv6-Mask. This is not valid.
                return false;
            }

            // Now find out how long the prefix is.
            int maskLength = int.Parse(subnetMask.Substring(slashIdx + 1));
            if (maskAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // Convert the mask address to an unsigned integer.
                var maskAddressBits = BitConverter.ToUInt32(maskAddress.GetAddressBytes().Reverse().ToArray(), 0);

                // And convert the IpAddress to an unsigned integer.
                var ipAddressBits = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

                // Get the mask/network address as unsigned integer.
                uint mask = uint.MaxValue << (32 - maskLength);

                // https://stackoverflow.com/a/1499284/3085985
                // Bitwise AND mask and MaskAddress, this should be the same as mask and IpAddress
                // as the end of the mask is 0000 which leads to both addresses to end with 0000
                // and to start with the prefix.
                return (maskAddressBits & mask) == (ipAddressBits & mask);
            }

            if (maskAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Convert the mask address to a BitArray.
                var maskAddressBits = new BitArray(maskAddress.GetAddressBytes());

                // And convert the IpAddress to a BitArray.
                var ipAddressBits = new BitArray(address.GetAddressBytes());

                if (maskAddressBits.Length != ipAddressBits.Length)
                {
                    throw new ArgumentException("Length of IP Address and Subnet Mask do not match.");
                }

                // Compare the prefix bits.
                for (int maskIndex = 0; maskIndex < maskLength; maskIndex++)
                {
                    if (ipAddressBits[maskIndex] != maskAddressBits[maskIndex])
                    {
                        return false;
                    }
                }

                return true;
            }

            throw new NotSupportedException("Only InterNetworkV6 or InterNetwork address families are supported.");
        }
        public static bool IsInSubnet(string subnetMask, string ip)
        {
            return IsInSubnet(subnetMask, IPAddress.Parse(ip));
        }
        public static bool IsIpv4(string ip)
        {
            const string pattern = "^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            Regex check = new Regex(pattern);
            return check.IsMatch(ip);
        }
        public static string GhostIp(string ethip)
        {
            return "127.0.0.1";
        }
        public static void EthIfInfIpv4(string ethif, Action<Object, List<Tuple<string,string>>> delivery)
        {
            var adapter = (new ManagementClass("Win32_NetworkAdapterConfiguration"))
                .GetInstances()
                .Cast<ManagementObject>()
                .Where(a => (a["IpAddress"] as string[]) != null && (a["IpAddress"] as string[]).ToList().Contains(ethif))
                .FirstOrDefault();
            if (adapter != null)
            {
                var ips = (adapter["IPAddress"] as string[]).ToList().Where(i => IsIpv4(i)).Select((val,index)=>new { index, val }).ToList();
                var mscs = (adapter["IpSubnet"] as string[]).ToList().Where(i => IsIpv4(i)).Select((val, index) => new { index, val }).ToList().ToList();
                var data = ips.Join(mscs, i => i.index, m => m.index, (i, m) => new Tuple<string, string>(i.val, m.val)).ToList();
                delivery(adapter, data);
            }
        }
        public static void EthIfAddIpv4(string ethif, string newIp, string newMask)
        {
            EthIfInfIpv4(ethif, (adapter, ipdata) =>
            {
                var exist = ipdata.Where(i => i.Item1 == newIp).FirstOrDefault();
                if (exist == null)
                {
                    var newIps = ipdata.Select(i => i.Item1).Union(new List<string>() { newIp }).ToArray();
                    var newMcs = ipdata.Select(i => i.Item2).Union(new List<string>() { newMask }).ToArray();
                    try
                    {
                        var newAddress = (adapter as ManagementObject).GetMethodParameters("EnableStatic");
                        newAddress["IPAddress"] = newIps.ToArray();
                        newAddress["SubnetMask"] = newMcs.ToArray();

                        var res = (adapter as ManagementObject).InvokeMethod("EnableStatic", newAddress, null);
                    }
                    catch (Exception x)
                    {
                        // todo.
                    }
                }
            });
        }
        public static void EthIfDelIpv4(string ethif, string oldIp)
        {
            EthIfInfIpv4(ethif, (adapter, ipdata) =>
            {
                var exist = ipdata.Where(i => i.Item1 == oldIp).FirstOrDefault();
                if (exist != null)
                {
                    var newIps = ipdata.Where(i => i.Item1 != oldIp).Select(i => i.Item1).ToList();
                    var newMcs = ipdata.Where(i => i.Item1 != oldIp).Select(i => i.Item2).ToList();
                    try
                    {
                        var newAddress = (adapter as ManagementObject).GetMethodParameters("EnableStatic");
                        newAddress["IPAddress"] = newIps.ToArray();
                        newAddress["SubnetMask"] = newMcs.ToArray();

                        var res = (adapter as ManagementObject).InvokeMethod("EnableStatic", newAddress, null);
                    }
                    catch (Exception x)
                    {
                        // todo.
                    }
                }
            });
        }
        public static bool IsLocalIpV4Address(string host)
        {
            try
            {
                if (!IsIpv4(host)) return false;
                // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
