using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.helpers;

namespace sacta_proxy.model
{
    public enum ProcessStates { Stopped, Running, Error }
    public class ProcessStatusControl
    {
        class ProcessMessage
        {
            public DateTime When { get; set; }
            public string Msg { get; set; }
        };
        ProcessStates State { get; set; }
        private List<ProcessMessage> LastErrors { get; set; }

        public ProcessStatusControl()
        {
            State = ProcessStates.Stopped;
            LastErrors = new List<ProcessMessage>();
        }
        public void Set(ProcessStates state, string strError="")
        {
            State = state;
            if (LastErrors.Count >= 8)
                LastErrors.RemoveAt(0);
            LastErrors.Add(new ProcessMessage() { When = DateTime.Now, Msg = strError });
        }
        public override string ToString()
        {
                return $"{String.Join(" ## ", LastErrors)}";
        }
        public void SignalFatal<T>(string cause, History history)
        {
            Set(ProcessStates.Error, cause);
            history?.Add(HistoryItems.ServiceFatalError, "", "", "", "", cause);
            Logger.Fatal<T>(cause);
        }
        public void SignalWarning<T>(string cause, History history)
        {
            Set(State, cause);
            // history?.Add(HistoryItems.ServiceFatalError, "", "", "", "", cause);
            Logger.Warn<T>(cause);
        }
        public object Status
        {
            get
            {
                return new { std = State, str = ToString(), lst=LastErrors.Where(i=>i.Msg!=string.Empty).ToList() };
            }
        }
        //public History History { get; set; }
        public bool IsStarted { get => State != ProcessStates.Stopped; }
    }
}
