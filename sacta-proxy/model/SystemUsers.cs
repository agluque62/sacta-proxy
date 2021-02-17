using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sacta_proxy.model
{
    class SystemUserInfo
    {
        public string id { get; set; }
        public string pwd { get; set; }
        public int prf { get; set; }
    }
    class SystemUsers
    {
        static public bool Authenticate(string user, string pwd)
        {
            bool res = false;
            int profile = 0;
            if (user == "root" && pwd == "#ncc#")
            {
                res = true;
                profile = 1;
            }
            else
            {
                try
                {
                    // TODO. Obtener los usuarios, y comprobar si tienen acceso
                    res = false;
                }
                catch (Exception)
                {
                    res = false;
                }
            }
            SetCurrentUser(res, user, profile);
            return res;
        }
        public static string CurrentUserId { get => CurrentUser?.id; }
        static void SetCurrentUser(bool login, string user, int profile)
        {
            CurrentUser = new SystemUserInfo() { id = login ? user : "", prf = login ? profile : 0 };
        }
        static  SystemUserInfo CurrentUser { get; set; }

    }
}
