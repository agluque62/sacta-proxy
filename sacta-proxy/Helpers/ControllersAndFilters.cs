using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sacta_proxy.helpers
{
    public class TemporaryEventsFilter
    {
        public TemporaryEventsFilter(int maxvalue)
        {
            Max = maxvalue;
            Activated = false;
        }
        public void BootDuring(TimeSpan time)
        {
            Counts = Counts.Select(c => 0).ToList();
            Activated = true;
            Task.Run(() =>
            {
                Task.Delay(time).Wait();
                Activated = false;
            });
        }

        public bool ProcessEvent(int eventid)
        {
            if (!Activated)
                return true;
            else
            {
                if (eventid >= Counts.Count)
                    return true;

                Counts[eventid]++;
                return Counts[eventid] > Max ? false : true;
            }
        }

        int Max { get; set; }
        bool Activated { get; set; }
        List<int> Counts = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 };  
    }
}
