﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Este código fue generado por una herramienta.
//     Versión de runtime:4.0.30319.42000
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SimulSACTA.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5000")]
        public ushort PresenceInterval {
            get {
                return ((ushort)(this["PresenceInterval"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30000")]
        public ushort ActivityTimeOut {
            get {
                return ((ushort)(this["ActivityTimeOut"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.20.90.1")]
        public string SactaIpA {
            get {
                return ((string)(this["SactaIpA"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("19204")]
        public int ListenPortA {
            get {
                return ((int)(this["ListenPortA"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.20.91.1")]
        public string SactaIpB {
            get {
                return ((string)(this["SactaIpB"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("19204")]
        public int ListenPortB {
            get {
                return ((int)(this["ListenPortB"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.20.94.1")]
        public string ScvIpA {
            get {
                return ((string)(this["ScvIpA"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15100")]
        public int ScvPortA {
            get {
                return ((int)(this["ScvPortA"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.20.94.1")]
        public string ScvIpB {
            get {
                return ((string)(this["ScvIpB"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15100")]
        public int ScvPortB {
            get {
                return ((int)(this["ScvPortB"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public byte SactaDomain {
            get {
                return ((byte)(this["SactaDomain"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("107")]
        public byte SactaCenter {
            get {
                return ((byte)(this["SactaCenter"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("110")]
        public ushort SactaGroupUser {
            get {
                return ((ushort)(this["SactaGroupUser"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public byte ScvDomain {
            get {
                return ((byte)(this["ScvDomain"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("107")]
        public byte ScvCenter {
            get {
                return ((byte)(this["ScvCenter"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public ushort ProcessorNumber {
            get {
                return ((ushort)(this["ProcessorNumber"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("111")]
        public ushort SactaSPSIUser {
            get {
                return ((ushort)(this["SactaSPSIUser"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("86")]
        public ushort SactaSPVUser {
            get {
                return ((ushort)(this["SactaSPVUser"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>10</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection ScvUsers {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ScvUsers"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1,1;2,2;3,3;4,4")]
        public string LastSectorization {
            get {
                return ((string)(this["LastSectorization"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool EnableMulticast {
            get {
                return ((bool)(this["EnableMulticast"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>Sectores Desplegados##1,1;2,2;3,3;4,4</string>
  <string>Agrupacion 01 (De 2 en 2)##1,1;2,1;3,2;4</string>
  <string>Agrupacion 02 (De 3 en 3)##1,1;2,1;3,1;4,2</string>
  <string>Agrupacion 03 (De 4 en 4)##1,1;2,1;3,1;4,1</string>
  <string>Sectorizacion Erronea 01 (Faltan Sectores Reales)##1,1;2,2;3,3</string>
  <string>Sectorizacion Erronea 02 (Sectores Repetidos)##1,1;2,2;3,3;3,4;4,4</string>
  <string>Sectorizacion Erronea 03 (Con Sectores Virtuales)##1,1;2,2;3,3;4,4</string>
  <string>Sectorizacion Erronea 04 (Con Sectores Inexistentes)##1,1;2,2;3,3;4,4;5,4</string>
  <string>Sectorizacion Erronea 05 (Con UCS Inexistentes)##1,1;2,2;3,3;4,5</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection PreSectorizaciones {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["PreSectorizaciones"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("225.12.101.1")]
        public string SactaMcastA {
            get {
                return ((string)(this["SactaMcastA"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("225.212.101.1")]
        public string SactaMcastB {
            get {
                return ((string)(this["SactaMcastB"]));
            }
        }
    }
}
