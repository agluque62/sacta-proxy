using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
        public static void Get(string id, Action<SectMap> deliver)
        {
            ReadData();
            var item = PersistenceItems.Where(i => i.Id == id).FirstOrDefault();
            if (item != null)
            {
                deliver(item.Map);
            }
            else
            {
                deliver(new SectMap());
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
}
