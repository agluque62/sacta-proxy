using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using sacta_proxy.helpers;

namespace sacta_proxy.model
{
    class UserInfo
    {
        public enum AccessProfiles { Operador = 0, Tecnico1 = 1, Tecnico2 = 2, Tecnico3 = 3 }
        public string Id { get; set; }
        public string Clave { get; set; }
        public AccessProfiles Perfil { get; set; }
    }
    public class SystemUsers
    {
        /// <summary>
        /// Control de Acceso. 
        /// Solo los Tecnico 2 (2) / Tecnico 3 (3)
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        static public bool Authenticate(string user, string pwd)
        {
            bool res = false;
            UserInfo.AccessProfiles profile = UserInfo.AccessProfiles.Operador;
            if (user == "root" && pwd == "#scpx#")
            {
                res = true;
                profile = UserInfo.AccessProfiles.Tecnico3;
                /** Al entrar con la clave maestra se resetean contadores de error operativos */
                DbControl.ConsecutiveErrors = 0;
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
            return profile == UserInfo.AccessProfiles.Tecnico2 || profile == UserInfo.AccessProfiles.Tecnico3;
        }
        public static string CurrentUserId { get => CurrentUser?.Id; }
        public static string CurrentUserIdAndProfile { get => $"{CurrentUser?.Id} / {CurrentUser?.Perfil}"; }
        static void SetCurrentUser(bool login, string user, UserInfo.AccessProfiles profile)
        {
            CurrentUser = new UserInfo() { Id = login ? user : "", Perfil = login ? profile : 0 };
        }
        static  UserInfo CurrentUser { get; set; }
        static void UsersInDb(Action<List<UserInfo>> delivery)
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
                            var users = new List<UserInfo>();
                            while (reader.Read())
                            {
                                var perfil = settings.ScvType == 0 ? (uint)(long)reader[2] : (uint)reader[2];
                                users.Add(new UserInfo()
                                {
                                    Id = reader[0] as string,
                                    Clave = reader[1] as string,
                                    Perfil = (UserInfo.AccessProfiles)perfil
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
