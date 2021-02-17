using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.helpers;

namespace sacta_proxy.model
{
    public enum HistoryItems 
    {
        ServiceStarted = 1,                     // USER = "", DEP = "", STATE = "", MAP="", CAUSE=""
        ServiceFatalError = 2,                  // USER = "", DEP = "", STATE = "", MAP="", CAUSE="cause"
        ServiceEnded = 2,                       // USER = "", DEP = "", STATE = "", MAP="", CAUSE=""

        UserLogin = 10,                         // USER = "user", DEP = "", STATE = "", MAP="", CAUSE=""
        UserErrorAccess = 11,                   // USER = "user", DEP = "", STATE = "", MAP="", CAUSE="error"
        UserConfigChange = 12,                  // USER = "user", DEP = "", STATE = "changes", MAP="", CAUSE=""
        UserLogout = 13,                        // USER = "user", DEP = "", STATE = "", MAP="", CAUSE=""

        DepActivityEvent = 20,                  // USER = "", DEP = "dep/scv", STATE = "on/off", MAP="", CAUSE=""
        DepTxstateChange = 21,                  // USER = "", DEP = "dep/scv", STATE = "on/off", MAP="", CAUSE=""
        DepSectorizationReceivedEvent = 22,     // USER = "", DEP = "dep", STATE = "", MAP="map", CAUSE=""
        DepSectorizationRejectedEvent = 23,     // USER = "", DEP = "dep", STATE = "", MAP="map", CAUSE="cause"
        ScvSectorizationSendedEvent = 25,       // USER = "", DEP = "scv", STATE = "", MAP="map", CAUSE=""

    };
    public class History : IDisposable
    {
        const string FileName = "history.json";
        class HistoryItem
        {
            public DateTime Date { get; set; }
            public HistoryItems Code { get; set; }
            public string User { get; set; }
            public string Dep { get; set; }
            public string State { get; set; }
            public string Map { get; set; }
            public string Cause { get; set; }
        }

        public History(int maxDays=30, int maxItems = 2000)
        {
            //Locker = new object();
            WorkingThread = new EventQueue();
            MaxDays = maxDays;
            MaxItems = maxItems;
            Read();
            Sanitize();
            WorkingThread.Start();
        }
        public void Dispose()
        {
            WorkingThread.Stop();
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

        void AddItem(HistoryItem item)
        {
            history.Add(item);
            Sanitize();
            Write();
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
            var Days = TimeSpan.FromDays(MaxItems);
            history = history.Where(i => (DateTime.Now - i.Date) <= Days).OrderBy(i => i.Date).ToList();
            history = history.Count() > MaxItems ? history.Take(MaxItems).ToList() : history;
        }



        int MaxDays { get; set; }
        int MaxItems { get; set; }
        List<HistoryItem> history = new List<HistoryItem>();
        //object Locker { get; set; }
        EventQueue WorkingThread { get; set; }
    }
}
