using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace sacta_proxy
{
    [RunInstaller(true)]
    public partial class SactaProxyInstaller : System.Configuration.Install.Installer
    {
        public SactaProxyInstaller()
        {
            InitializeComponent();
        }
    }
}
