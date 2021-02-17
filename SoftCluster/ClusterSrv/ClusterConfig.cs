using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using ClusterSrv.Properties;

namespace ClusterSrv
{
   public partial class ClusterConfig : Form
   {
      public ClusterConfig()
      {
         InitializeComponent();
      }

      private void ClusterIpConfig_Load(object sender, EventArgs e)
      {
         Settings stts = Settings.Default;

         // Get Node Id by Operating System
         stts.NodeId = System.Environment.MachineName;

          _RNodePortTB.Text = stts.EpPort.ToString();
         _NodePortTB.Text = stts.Port.ToString();
      }

      private void _OkBT_Click(object sender, EventArgs e)
      {
         Settings stts = Settings.Default;

         stts.EpPort = Int32.Parse(_RNodePortTB.Text);
         stts.Port = Int32.Parse(_NodePortTB.Text);

         stts.Save();
         Close();
      }
   }
}