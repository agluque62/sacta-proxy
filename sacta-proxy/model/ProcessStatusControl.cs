using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sacta_proxy.Helpers;

namespace sacta_proxy.model
{
    public enum ProcessStates { Stopped, Running, Error }
    public class ProcessStatusControl
    {
        ProcessStates State { get; set; }
        private string LastError { get; set; }

        public ProcessStatusControl()
        {
            State = ProcessStates.Stopped;
            LastError = "";
        }
        public void Set(ProcessStates state, string strError="")
        {
            State = state;
            LastError = strError;
        }
        public override string ToString()
        {
            return $"State: {State}, LastMessage: {LastError}";
        }
        public void SignalFatal<T>(string cause)
        {
            Set(ProcessStates.Error, cause);
            Logger.Fatal<T>(cause);
        }
        public object Status
        {
            get
            {
                return new { std = State, str = ToString() };
            }
        }
    }
}
