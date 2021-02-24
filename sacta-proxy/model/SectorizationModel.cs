using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MySql.Data.MySqlClient;

using sacta_proxy.helpers;

namespace sacta_proxy.model
{
    using SectMap = Dictionary<string, int>;

    public class SectorizationItem
    {
        public string Sector { get; set; }
        public int Position { get; set; }
    }
    public class SectorizationPersistence
    {
        const string FileName = "sacta-proxy-sectorizations.json";
        public static void Get(string id, Action<DateTime, SectMap> deliver)
        {
            ReadData();
            var item = PersistenceItems.Where(i => i.Id == id).FirstOrDefault();
            if (item != null)
            {
                deliver(item.Date, item.Map);
            }
            else
            {
                deliver(DateTime.Now, new SectMap());
            }
        }
        public static void Set(string id, SectMap map)
        {
            var item = PersistenceItems.Where(i => i.Id == id).FirstOrDefault();
            if (item != null)
            {
                item.Date = DateTime.Now;
                item.Map = map;
            }
            else
            {
                PersistenceItems.Add(new PersistenceItem()
                {
                    Date = DateTime.Now,
                    Id = id,
                    Map = map
                });
            }
            WriteData();
        }
        public static void Sanitize(List<string> deps)
        {
            var items = PersistenceItems.Where(item => deps.Contains(item.Id)).ToList();
            PersistenceItems = items;
            WriteData();
        }
        static void WriteData()
        {
            var data = JsonHelper.ToString(PersistenceItems);
            File.WriteAllText(FileName, data);
        }
        static void ReadData()
        {
            if (File.Exists(FileName))
            {
                var data = File.ReadAllText(FileName);
                var items = JsonHelper.Parse<List<PersistenceItem>>(data);
                PersistenceItems = items;
            }
            else
            {
                PersistenceItems = new List<PersistenceItem>();
            }
        }
        class PersistenceItem
        {
            public DateTime Date { get; set; }
            public string Id { get; set; }
            public SectMap Map { get; set; }
        }
        static List<PersistenceItem> PersistenceItems = new List<PersistenceItem>();
    }
    public class SectorizationHelper
    {
        public static string MapToString(SectMap map)
        {
            var mapstr = string.Empty;
            foreach(var entry in map)
            {
                mapstr += $"{entry.Key}:{entry.Value},";
            }
            return mapstr;
        }

        public static void CompareWithDb(string PositionsList, string SectorsList, string VirtualSectorsList, Action<string> notifyError)
        {
            var PosInCfg = PositionsList.Count()==0 ? new List<string>() : PositionsList.Split(',').OrderBy(i => i).ToList();
            var SecInCfg = SectorsList.Count() == 0 ? new List<string>() : SectorsList.Split(',').OrderBy(i => i).ToList();
            var VirInCfg = VirtualSectorsList.Count() == 0 ? new List<string>() : VirtualSectorsList.Split(',').OrderBy(i => i).ToList();
            SectorizationsItemsInDB((PosInDb, SecInDb, VirInDb) =>
            {
                var PosEquals = PosInCfg.Except(PosInDb).Count() == 0 && PosInDb.Except(PosInCfg).Count()==0;
                if (PosEquals == false)
                {
                    notifyError($"Conjunto Posiciones Diferente: CFG: {PositionsList}; DB: {String.Join(",",PosInDb)}");
                }
                var SecEquals = SecInCfg.Except(SecInDb).Count() == 0 && SecInDb.Except(SecInCfg).Count() == 0;
                if (SecEquals == false)
                {
                    notifyError($"Conjunto Sectores Diferente: CFG: {SectorsList}; DB: {String.Join(",", SecInDb)}");
                }
                var VirEquals = VirInCfg.Except(VirInDb).Count() == 0 && VirInDb.Except(VirInCfg).Count() == 0;
                if (VirEquals == false)
                {
                    notifyError($"Conjunto Sectores Virtuales Diferente: CFG: {VirtualSectorsList}; DB: {String.Join(",", VirInDb)}");
                }
            });
        }

        public static void SectorizationsItemsInDB(Action<List<string>, List<string>, List<string>> notify)
        {
            var settings = Properties.Settings.Default;
            if (settings.DbConn == 1)
            {
                // Solo efectuamos el acceso para MySql
                using (var connection = new MySqlConnection(DbControl.StrConn))
                {
                    try
                    {
                        DbControl.ControlledOpen(connection, () =>
                        {
                            using (var PositionsCommand = new MySqlCommand(DbControl.SqlQueryForPositions, connection))
                            using (var SectorsCommand = new MySqlCommand(DbControl.SqlQueryForSectors, connection))
                            using (var VirtualsCommand = new MySqlCommand(DbControl.SqlQueryForVirtuals, connection))
                            {
                                var positions = new List<string>();
                                var sectors = new List<string>();
                                var virtuals = new List<string>();
                                using (var PositionsReader = PositionsCommand.ExecuteReader())
                                {
                                    while (PositionsReader.Read())
                                        positions.Add(PositionsReader[0].ToString());
                                }
                                using (var SectorsReader = SectorsCommand.ExecuteReader())
                                {
                                    while (SectorsReader.Read())
                                        sectors.Add(SectorsReader[0].ToString());
                                }
                                using (var VirtualsReader = VirtualsCommand.ExecuteReader())
                                {
                                    while (VirtualsReader.Read())
                                        virtuals.Add(VirtualsReader[0].ToString());
                                }
                                //positions = positions.Count == 0 ? new List<string>() { "-1" } : positions;
                                //sectors = sectors.Count == 0 ? new List<string>() { "-1" } : sectors;
                                //virtuals = virtuals.Count == 0 ? new List<string>() { "-1" } : virtuals;
                                notify(positions, sectors, virtuals);
                            }
                        });
                    }
                    catch (Exception x)
                    {
                        Logger.Exception<SectorizationHelper>(x);
                        notify(new List<string>() { "-3" }, new List<string>() { "-3" }, new List<string>() { "-3" });
                    }
                }
            }
            else
            {
                notify(new List<string>() { "-2" }, new List<string>() { "-2" }, new List<string>() { "-2" });
            }

        }
    }
}
