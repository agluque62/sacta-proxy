using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using NLog;

using SimulSACTA.Properties;
using Utilities;

namespace SimulSACTA
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 
        /// </summary>
        static Logger _Logger = LogManager.GetCurrentClassLogger();
        static MainForm _This;
        public SimulSacta _Sacta { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public MainForm()
        {
            _Sacta = new SimulSacta();
            InitializeComponent();
            LoadPresect();

            _This = this;
            var stts = Properties.Settings.Default;
            lan1Info.Text = $"{stts.SactaIpA}:{stts.ListenPortA} <= {stts.SactaMcastA}, {stts.SactaIpA}:{stts.ListenPortA} => {stts.ScvIpA}:{stts.ScvPortA}";
            lan2Info.Text = $"{stts.SactaIpB}:{stts.ListenPortB} <= {stts.SactaMcastB}, {stts.SactaIpB}:{stts.ListenPortB} => {stts.ScvIpB}:{stts.ScvPortB}";
            chkLan1.Checked = _Sacta.Lan1EnableTx;
            chkLan2.Checked = _Sacta.Lan2EnableTx;
            //Settings stts = Settings.Default;
            //_Sacta.SectorUcs = stts.LastSectorization.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            //_SectorsTB.Text = stts.LastSectorization;

            _Sacta.SectorUcs = _SectorsTB.Text.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            _Sacta.Run();

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public static void LogMethod(/*string level*/LogLevel level, string message)
        {
            _This.Invoke(new GenericEventHandler<string>(_This.OnNewInfoEventNew), new object[] { /*_This*/level, message });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        void OnNewInfoEvent(object sender, string info)
        {
            _InfoLB.BeginUpdate();

            if (_InfoLB.Items.Count > 2000)
            {
                for (int i = _InfoLB.Items.Count - 1; i > 1500; i--)
                {
                    _InfoLB.Items.RemoveAt(i);
                }
            }
            _Logger.Info(info);
            _InfoLB.Items.Insert(0, info);
            _InfoLB.EndUpdate();
        }

        void OnNewInfoEventNew(object sender, string info)
        {
            _InfoLB.BeginUpdate();

            if (_InfoLB.Items.Count > 2000)
            {
                //for (int i = _InfoLB.Items.Count - 1; i > 1500; i--)
                //{
                //    _InfoLB.Items.RemoveAt(i);
                //}
                for (int i = 0; i < 500; i++)
                    _InfoLB.Items.RemoveAt(0);
            }
            _Logger.Log(sender as LogLevel, info);
            _InfoLB.Items.Add($"{DateTime.Now.ToLongTimeString()}: {info}");
            _InfoLB.EndUpdate();
            _InfoLB.SelectedIndex = _InfoLB.Items.Count - 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SectorizeBT_Click(object sender, EventArgs e)
        {
            string[] sectorUcs = _SectorsTB.Text.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (sectorUcs.Length % 2 != 0)
            {
                MessageBox.Show("Formato Erroneo en el STRING de Sectorization");
                return;
            }

            //Settings.Default.LastSectorization = _SectorsTB.Text;
            //Settings.Default.Save();

            _Sacta.RunSectorization(sectorUcs);
            _Sacta.SectorUcs = sectorUcs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _InfoLB.Items.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm dlg = new SettingsForm();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show("Es necesario reiniciar la aplicación");
            }
        }

        private Dictionary<string, string> preSectDic = new Dictionary<string, string>();
        void LoadPresect()
        {
            preSectDic.Clear();
            foreach (string strsect in Properties.Settings.Default.PreSectorizaciones)
            {
                string[] keyval = strsect.Split(new string[] { "##" }, StringSplitOptions.None);
                if (keyval.Length == 2)
                {
                    preSectDic[keyval[0]] = keyval[1];
                }
            }

            //
            comboBox1.DataSource = new BindingSource(preSectDic, null);
            comboBox1.DisplayMember = "Key";
            comboBox1.ValueMember = "Value";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPresectChange(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            int selectedIndex = cmb.SelectedIndex;
            KeyValuePair<string, string> item = (KeyValuePair<string, string>)cmb.SelectedItem;

            _SectorsTB.Text = item.Value;
        }

        private void OnLan1Changed(object sender, EventArgs e)
        {
            _Sacta.Lan1EnableTx = chkLan1.Checked;
        }

        private void chkLan2_CheckedChanged(object sender, EventArgs e)
        {
            _Sacta.Lan2EnableTx = chkLan2.Checked;

        }
    }
}