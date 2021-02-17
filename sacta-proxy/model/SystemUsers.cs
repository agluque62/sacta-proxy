using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

namespace sacta_proxy.model
{
    class SystemUserInfo
    {
        public string Id { get; set; }
        public string Clave { get; set; }
        public uint Perfil { get; set; }
    }
    public class SystemUsers
    {
        static public bool Authenticate(string user, string pwd)
        {
            bool res = false;
            uint profile = 0;
            if (user == "root" && pwd == "#ncc#")
            {
                res = true;
                profile = 3;
            }
            else
            {
                try
                {
                    // Obtener los usuarios, y comprobar si tienen acceso
                    UsersInDb((users) =>
                    {
                        var founds = users.Where(u => u.Id == user && u.Clave == pwd);
                        res = founds.Count() > 0;
                        profile = res ? founds.First().Perfil : 0;
                    });
                }
                catch 
                {
                }
            }
            SetCurrentUser(res, user, profile);
            return res;
        }
        public static string CurrentUserId { get => CurrentUser?.Id; }
        static void SetCurrentUser(bool login, string user, uint profile)
        {
            CurrentUser = new SystemUserInfo() { Id = login ? user : "", Perfil = login ? profile : 0 };
        }
        static  SystemUserInfo CurrentUser { get; set; }
        static void UsersInDb(Action<List<SystemUserInfo>> delivery)
        {
            var settings = Properties.Settings.Default;
            using (var connection = new MySqlConnection($"Server={settings.HistoricServer};User ID=root;Password=cd40;Database=new_cd40;;Connect Timeout={settings.DbConnTimeout}"))
            {
                connection.Open();

                using (var command = new MySqlCommand("SELECT IdOperador,Clave,NivelAcceso FROM operadores WHERE IdSistema='departamento';", connection))
                using (var reader = command.ExecuteReader())
                {
                    var users = new List<SystemUserInfo>();
                    while (reader.Read())
                    {
                        users.Add(new SystemUserInfo()
                        {
                            Id = reader[0] as string,
                            Clave = reader[1] as string,
                            Perfil = (uint)reader[2]
                        });
                    }
                    delivery(users);
                }
            }
        }
    }
}
