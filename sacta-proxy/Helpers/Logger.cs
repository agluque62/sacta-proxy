using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

namespace sacta_proxy.Helpers
{
    public class Logger
    {
        protected static void Log<T>(LogLevel level, string msg, string caller, int line)
        {
            var logger = LogManager.GetLogger(typeof(T).Name);
            logger.Log(level, $"[{caller}: {line}]: {msg}");
        }
        public static void Trace<T>(String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            Log<T>(LogLevel.Trace, msg, caller, lineNumber);
        }
        public static void Debug<T>(String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            Log<T>(LogLevel.Debug, msg, caller, lineNumber);
        }
        public static void Info<T>(String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            Log<T>(LogLevel.Info, msg, caller, lineNumber);
        }
        public static void Warn<T>(String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            Log<T>(LogLevel.Warn, msg, caller, lineNumber);
        }
        public static void Error<T>(String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            Log<T>(LogLevel.Error, msg, caller, lineNumber);
        }
        public static void Fatal<T>(String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            Log<T>(LogLevel.Fatal, msg, caller, lineNumber);
        }
        public static void Exception<T>( Exception x, string msg1 = "",
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)

        {
            var msg = msg1 + "[Exception]: " + x.Message + (x.InnerException != null ? (" [Inner Exception]: " + x.InnerException.Message) : "");
            Log<T>(LogLevel.Error, msg, caller, lineNumber);
            NLog.LogManager.GetCurrentClassLogger().Trace(x);
        }

    }
}
