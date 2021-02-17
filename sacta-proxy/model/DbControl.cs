using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using sacta_proxy.helpers;

namespace sacta_proxy.model
{
    public class DbControl
    {
        public static string StrConn
        {
            get
            {
                var settings = Properties.Settings.Default;
                if (settings.DbConn == 1)
                {
                    var schema = settings.ScvType == 0 ? "cd30" : "new_cd40";
                    var strconn = $"Server={settings.ScvServerIp};User ID={settings.DbRootUser};Password={settings.DbRootPwd};Database={schema};Connect Timeout={settings.DbConnTimeout}";
                    return strconn;
                }
                return string.Empty;
            }
        }

        public static bool IsPresent(Action<bool> delivery=null)
        {
            bool retorno = false;
            using (var connection = new MySqlConnection(StrConn))
            {
                ControlledOpen(connection, () =>
                {
                    retorno = true;
                });
                delivery?.Invoke(retorno);
                return retorno;
            }
        }

        public static string SqlQueryForPositions
        {
            get
            {
                var settings = Properties.Settings.Default;
                return settings.ScvType == 1 ? "SELECT PosicionSacta FROM top WHERE IdSistema='departamento';"
                    : "SELECT DISTINCT NumUcs from Ucs;";
            }
        }
        public static string SqlQueryForSectors
        {
            get
            {
                var settings = Properties.Settings.Default;
                return settings.ScvType == 1 ? "SELECT NumSacta FROM sectores WHERE IdSistema='departamento' AND sectorsimple=1 AND (tipo='R');"
                    : "SELECT idSActa FROM SectorPosicion WHERE (IdNucleo != 'ACTIVA' AND tipo='R');";
            }
        }
        public static string SqlQueryForVirtuals
        {
            get
            {
                var settings = Properties.Settings.Default;
                return settings.ScvType == 1 ? "SELECT NumSacta FROM sectores WHERE IdSistema='departamento' AND sectorsimple=1 AND (tipo='V')"
                    : "SELECT idSActa FROM SectorPosicion WHERE(IdNucleo != 'ACTIVA' AND tipo = 'V');";
            }
        }

        public static void ControlledOpen(MySqlConnection connection, Action Continue)
        {
            if (ConsecutiveErrors < Properties.Settings.Default.DbMaxConsecutiveErrors)
            {
                try
                {
                    connection.Open();
                    ConsecutiveErrors = 0;
                    Continue();
                    return;
                }
                catch(Exception x)
                {
                    ConsecutiveErrors += 1;
                    throw x;
                }
            }
            throw new Exception("No Hay conexion con Base de Datos...");
        }

        private static int ConsecutiveErrors = 0;
    }

}
