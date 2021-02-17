namespace SimulSACTA
{
   partial class MainForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ContextMenuStrip _InfoCM;
            System.Windows.Forms.MenuStrip _MainMS;
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._InfoLB = new System.Windows.Forms.ListBox();
            this._SectorizeBT = new System.Windows.Forms.Button();
            this._SectorsTB = new System.Windows.Forms.TextBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.lan1Info = new System.Windows.Forms.Label();
            this.lan2Info = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            _InfoCM = new System.Windows.Forms.ContextMenuStrip(this.components);
            _MainMS = new System.Windows.Forms.MenuStrip();
            _InfoCM.SuspendLayout();
            _MainMS.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _InfoCM
            // 
            _InfoCM.ImageScalingSize = new System.Drawing.Size(20, 20);
            _InfoCM.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearToolStripMenuItem});
            _InfoCM.Name = "_InfoCM";
            _InfoCM.Size = new System.Drawing.Size(113, 28);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(112, 24);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // _MainMS
            // 
            _MainMS.ImageScalingSize = new System.Drawing.Size(20, 20);
            _MainMS.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            _MainMS.Location = new System.Drawing.Point(0, 0);
            _MainMS.Name = "_MainMS";
            _MainMS.Size = new System.Drawing.Size(696, 28);
            _MainMS.TabIndex = 8;
            _MainMS.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(106, 24);
            this.settingsToolStripMenuItem.Text = "Propiedades";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // _InfoLB
            // 
            this._InfoLB.ContextMenuStrip = _InfoCM;
            this._InfoLB.FormattingEnabled = true;
            this._InfoLB.ItemHeight = 16;
            this._InfoLB.Location = new System.Drawing.Point(4, 181);
            this._InfoLB.Margin = new System.Windows.Forms.Padding(4);
            this._InfoLB.Name = "_InfoLB";
            this._InfoLB.Size = new System.Drawing.Size(641, 420);
            this._InfoLB.TabIndex = 5;
            // 
            // _SectorizeBT
            // 
            this._SectorizeBT.Location = new System.Drawing.Point(545, 145);
            this._SectorizeBT.Margin = new System.Windows.Forms.Padding(4);
            this._SectorizeBT.Name = "_SectorizeBT";
            this._SectorizeBT.Size = new System.Drawing.Size(100, 28);
            this._SectorizeBT.TabIndex = 4;
            this._SectorizeBT.Text = "Enviar";
            this._SectorizeBT.UseVisualStyleBackColor = true;
            this._SectorizeBT.Click += new System.EventHandler(this._SectorizeBT_Click);
            // 
            // _SectorsTB
            // 
            this._SectorsTB.Location = new System.Drawing.Point(20, 115);
            this._SectorsTB.Margin = new System.Windows.Forms.Padding(4);
            this._SectorsTB.Name = "_SectorsTB";
            this._SectorsTB.Size = new System.Drawing.Size(625, 22);
            this._SectorsTB.TabIndex = 3;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(252, 82);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(393, 24);
            this.comboBox1.TabIndex = 9;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.OnPresectChange);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 82);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(186, 17);
            this.label1.TabIndex = 10;
            this.label1.Text = "Sectorizaciones Preparadas";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.lan2Info);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.lan1Info);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.comboBox1);
            this.panel1.Controls.Add(this._SectorizeBT);
            this.panel1.Controls.Add(this._InfoLB);
            this.panel1.Controls.Add(this._SectorsTB);
            this.panel1.Location = new System.Drawing.Point(23, 33);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(657, 607);
            this.panel1.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 17);
            this.label2.TabIndex = 11;
            this.label2.Text = "Lan1";
            // 
            // lan1Info
            // 
            this.lan1Info.AutoSize = true;
            this.lan1Info.Location = new System.Drawing.Point(66, 23);
            this.lan1Info.Name = "lan1Info";
            this.lan1Info.Size = new System.Drawing.Size(46, 17);
            this.lan1Info.TabIndex = 12;
            this.lan1Info.Text = "label3";
            // 
            // lan2Info
            // 
            this.lan2Info.AutoSize = true;
            this.lan2Info.Location = new System.Drawing.Point(66, 51);
            this.lan2Info.Name = "lan2Info";
            this.lan2Info.Size = new System.Drawing.Size(46, 17);
            this.lan2Info.TabIndex = 14;
            this.lan2Info.Text = "label3";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 51);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 17);
            this.label4.TabIndex = 13;
            this.label4.Text = "Lan2";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(696, 653);
            this.Controls.Add(_MainMS);
            this.Controls.Add(this.panel1);
            this.MainMenuStrip = _MainMS;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "SimulSACTA";
            _InfoCM.ResumeLayout(false);
            _MainMS.ResumeLayout(false);
            _MainMS.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.ListBox _InfoLB;
      private System.Windows.Forms.Button _SectorizeBT;
      private System.Windows.Forms.TextBox _SectorsTB;
      private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
      private System.Windows.Forms.ComboBox comboBox1;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lan2Info;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lan1Info;
        private System.Windows.Forms.Label label2;
    }
}

