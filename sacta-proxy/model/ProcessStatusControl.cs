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
        ProcessStates State { get; set; }
        private List<string> LastErrors { get; set; }

        public ProcessStatusControl()
        {
            State = ProcessStates.Stopped;
            LastErrors = new List<string>();
        }
        public void Set(ProcessStates state, string strError="")
        {
            State = state;
            if (LastErrors.Count >= 8)
                LastErrors.RemoveAt(0);
            LastErrors.Add(strError);
        }
        public override string ToString()
        {
            return $"{LastErrors.Aggregate((i, j) => i + " ##\n" + j)}";
        }
        public void SignalFatal<T>(string cause, History history)
        {
            Set(ProcessStates.Error, cause);
            history?.Add(HistoryItems.ServiceFatalError, "", "", "", "", cause);
            Logger.Fatal<T>(cause);
        }
        public void SignalWarning<T>(string cause, History history)
        {
            Set(ProcessStates.Error, cause);
            history?.Add(HistoryItems.ServiceFatalError, "", "", "", "", cause);
            Logger.Warn<T>(cause);
        }
        public object Status
        {
            get
            {
                return new { std = State, str = ToString() };
            }
        }
        //public History History { get; set; }
        public bool IsStarted { get => State != ProcessStates.Stopped; }
    }
}
