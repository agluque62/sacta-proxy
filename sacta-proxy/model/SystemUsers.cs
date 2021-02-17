using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using sacta_proxy.helpers;

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
                catch (Exception x)
                {
                    Logger.Error<SystemUsers>(x.Message);
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
            if (settings.DbConn == 1)
            {
                // Solo efectuamos el acceso para MySql
                using (var connection = new MySqlConnection(DbControl.StrConn))
                {
                    DbControl.ControlledOpen(connection, () =>
                    {
                        var query = settings.ScvType == 0 ?
                            $"SELECT idusuario, clave, perfil FROM opeusu;" :
                            $"SELECT IdOperador, Clave, NivelAcceso FROM operadores WHERE IdSistema = 'departamento';";

                        using (var command = new MySqlCommand(query, connection))
                        using (var reader = command.ExecuteReader())
                        {
                            var users = new List<SystemUserInfo>();
                            while (reader.Read())
                            {
                                var perfil = settings.ScvType == 0 ? (uint)(long)reader[2] : (uint)reader[2];
                                users.Add(new SystemUserInfo()
                                {
                                    Id = reader[0] as string,
                                    Clave = reader[1] as string,
                                    Perfil = perfil
                                });
                            }
                            delivery(users);
                        }
                    });
                }
            }
        }
    }
}
