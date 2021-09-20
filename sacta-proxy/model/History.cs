using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using sacta_proxy.helpers;

namespace sacta_proxy.model
{
    public enum HistoryItems 
    {
        ServiceStarted = 1,                     // USER = "", DEP = "", STATE = "", MAP="", CAUSE=""
        ServiceEnded = 2,                       // USER = "", DEP = "", STATE = "", MAP="", CAUSE=""
        ServiceFatalError = 3,                  // USER = "", DEP = "", STATE = "", MAP="", CAUSE="cause"
        ServiceInMode = 4,                      // USER = "", DEP = "", STATE = "Simple/Master/Standby", MAP="", CAUSE="cause"
        ServiceWarning = 5,                     // USER = "", DEP = "", STATE = "", MAP="", CAUSE="cause"

        UserLogin = 10,                         // USER = "user", DEP = "", STATE = "", MAP="", CAUSE=""
        UserErrorAccess = 11,                   // USER = "user", DEP = "", STATE = "", MAP="", CAUSE="error"
        UserConfigChange = 12,                  // USER = "user", DEP = "", STATE = "changes", MAP="", CAUSE=""
        UserLogout = 13,                        // USER = "user", DEP = "", STATE = "", MAP="", CAUSE=""

        DepActivityEvent = 20,                  // USER = "", DEP = "dep/scv", STATE = "on/off", MAP="", CAUSE=""
        DepTxstateChange = 21,                  // USER = "", DEP = "dep/scv", STATE = "on/off", MAP="", CAUSE=""
        DepSectorizationReceivedEvent = 22,     // USER = "", DEP = "dep", STATE = "", MAP="map", CAUSE=""
        DepSectorizationRejectedEvent = 23,     // USER = "", DEP = "dep", STATE = "", MAP="map", CAUSE="cause"
        ScvSectorizationSendedEvent = 25,       // USER = "", DEP = "scv", STATE = "", MAP="map", CAUSE=""
        ScvSectorizationAskEvent = 26,          // USER = "", DEP = "scv", STATE = "", MAP="map", CAUSE=""
    };
    public class History : IDisposable
    {
        const string FileName = "history.json";
        class HistoryItemDesciption
        {
            public HistoryItems Code { get; set; }
            public string FormatString { get; set; }
        }
        class HistoryItem
        {
            public DateTime Date { get; set; }
            public HistoryItems Code { get; set; }
            public string User { get; set; }
            public string Dep { get; set; }
            public string State { get; set; }
            public string Map { get; set; }
            public string Cause { get; set; }
            public override string ToString()
            {
                var Description = HistoryItemsDesc.Where(i => i.Code == Code).FirstOrDefault();
                if (Description != null)
                {
                    return String.Format(Description.FormatString, Dep, State, Map, Cause);
                }
                return $"ERROR: Codigo de Historico {Code} no encontrado en la descripcion.";
            }
            static readonly List<HistoryItemDesciption> HistoryItemsDesc = new List<HistoryItemDesciption>()
            {
                new HistoryItemDesciption(){ Code= HistoryItems.ServiceStarted, FormatString="Servicio Iniciado {1}"},
                new HistoryItemDesciption(){ Code= HistoryItems.ServiceEnded, FormatString="Servicio Detenido"},
                new HistoryItemDesciption(){ Code= HistoryItems.ServiceFatalError, FormatString="Error Grave en el Servicio {3}"},
                new HistoryItemDesciption(){ Code= HistoryItems.ServiceInMode, FormatString="Entrando en Modo {1}"},
                new HistoryItemDesciption(){ Code= HistoryItems.ServiceWarning, FormatString="Aviso en el Servicio {3}"},

                new HistoryItemDesciption(){ Code= HistoryItems.UserLogin, FormatString="Acceso de Usuario"},
                new HistoryItemDesciption(){ Code= HistoryItems.UserErrorAccess, FormatString="Error de Acceso de Usurario: {3}"},
                new HistoryItemDesciption(){ Code= HistoryItems.UserConfigChange, FormatString="Cambio de Configuracion efectuada"},
                new HistoryItemDesciption(){ Code= HistoryItems.UserLogout, FormatString="Salida de Usuario"},

                new HistoryItemDesciption(){ Code= HistoryItems.DepActivityEvent, FormatString="Cambio en Estado Dependencia {0} => {1}"},
                new HistoryItemDesciption(){ Code= HistoryItems.DepTxstateChange, FormatString="Cambio en Estado TX de Dependencia {0} => {1}"},
                new HistoryItemDesciption(){ Code= HistoryItems.DepSectorizationReceivedEvent, FormatString="Sectorizacion ## {2} ## Recibida para Dependencia {0}"},
                new HistoryItemDesciption(){ Code= HistoryItems.DepSectorizationRejectedEvent, FormatString="Sectorizacion ## {2} ## Rechazada para Dependencia {0}. Cause: {3}"},
                new HistoryItemDesciption(){ Code= HistoryItems.ScvSectorizationSendedEvent, FormatString="Sectorizacion ## {2} ## Enviada al SCV. Motivo: {3}"},
                new HistoryItemDesciption(){ Code= HistoryItems.ScvSectorizationAskEvent, FormatString="{0} Peticion de Sectorizacion"},
            };
        }

        public History(int maxDays=30, int maxItems = 2000)
        {
            WorkingThread = new EventQueue((sender, x) => Logger.Exception<History>(x));
            MaxDays = maxDays;
            MaxItems = maxItems;
            Read();
            Sanitize();
            WorkingThread.Start();
        }
        public void Dispose()
        {
            WorkingThread.ControlledStop();
        }
        public void Add(HistoryItems item, string user="", string dep="", string state="", string map="", string cause = "")
        {
            WorkingThread.Enqueue("", () =>
            {
                AddItem(new HistoryItem()
                {
                    Date = DateTime.Now,
                    Code = item,
                    User = user,
                    Dep = dep,
                    State = state,
                    Map = map,
                    Cause = cause
                });
                Logger.Info<History>($"HIST {item}, user: {user}, dep: {dep}, state:{state}, map: {map}, cause: {cause}");
            });
        }
        public Object Get { get => history; }
        public void Configure(int maxDays, int maxItems)
        {
            MaxDays = maxDays;
            MaxItems = maxItems;
            Sanitize();
            Write();
        }
        void AddItem(HistoryItem item)
        {
            try
            {
                // Salvandolo en el Historico Local...
                history.Add(item);
                Sanitize();
                Write();
                // Enviandolo a la Base de datos.
                WriteToDb(item);
            }
            catch (Exception x)
            {
                Logger.Exception<History>(x);
                Logger.Trace<History>(x.ToString());
            }
        }

        void Write()
        {
            var data = JsonHelper.ToString(history);
            File.WriteAllText(FileName, data);
        }
        void Read()
        {
            if (File.Exists(FileName))
            {
                var data = File.ReadAllText(FileName);
                var items = JsonHelper.Parse<List<HistoryItem>>(data);
                history = items;
            }
            else
            {
                history = new List<HistoryItem>();
            }
        }
        void Sanitize()
        {
            var Days = TimeSpan.FromDays(MaxDays);
            history = history.Where(i => (DateTime.Now - i.Date) <= Days).OrderByDescending(i => i.Date).ToList();
            history = history.Count() > MaxItems ? history.Take(MaxItems).ToList() : history;
        }
        void WriteToDb(HistoryItem item)
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
                            String.Format("INSERT INTO tbnewhistorico (idfechahora, idequipo, idincidencia, descripcion, grupo) " +
                                "VALUES ('{0}', {1}, {2}, '{3}', {4})",
                                String.Format("{0:yyyy-MM-dd HH:mm:ss}", item.Date),
                                4,
                                9999,
                                NormalizeBdtFieldLen($"{Environment.MachineName}: {item} {item.User}", 255),
                                99) :
                            string.Format("INSERT INTO historicoincidencias (IdSistema, Scv, IdIncidencia, IdHw, TipoHw, FechaHora, Usuario, Descripcion) " +
                                "VALUES (\"{0}\",{1},{2},\"{3}\",{4},\"{5}\",\"{6}\",\"{7}\")",
                                "departamento", 0, 50, "ProxySacta", 4,
                                String.Format("{0:yyyy-MM-dd HH:mm:ss}", item.Date),
                                NormalizeBdtFieldLen(item.User, 32),
                                NormalizeBdtFieldLen($"{Environment.MachineName.ToString()}: {item}", 200));
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    });
                }
            }
        }
        private string NormalizeBdtFieldLen(string field, int maxlen)
        {
            return field.Length > maxlen ? field.Substring(0, maxlen) : field;
        }

        int MaxDays { get; set; }
        int MaxItems { get; set; }
        List<HistoryItem> history = new List<HistoryItem>();
        //object Locker { get; set; }
        EventQueue WorkingThread { get; set; }
    }
}
