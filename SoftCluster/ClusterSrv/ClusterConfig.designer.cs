namespace ClusterSrv
{
   partial class ClusterConfig
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
            System.Windows.Forms.GroupBox groupBox1;
            System.Windows.Forms.TextBox textBox2;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.TextBox textBox1;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.TextBox textBox3;
            System.Windows.Forms.GroupBox groupBox2;
            System.Windows.Forms.TextBox textBox5;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.TextBox textBox6;
            System.Windows.Forms.TextBox textBox4;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.GroupBox groupBox3;
            System.Windows.Forms.Label label10;
            System.Windows.Forms.TextBox textBox10;
            System.Windows.Forms.Label label11;
            System.Windows.Forms.TextBox textBox7;
            System.Windows.Forms.Label label7;
            System.Windows.Forms.GroupBox groupBox4;
            System.Windows.Forms.Label label8;
            System.Windows.Forms.TextBox textBox8;
            System.Windows.Forms.Label label9;
            this._NodePortTB = new System.Windows.Forms.TextBox();
            this._RNodePortTB = new System.Windows.Forms.TextBox();
            this._OkBT = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            textBox2 = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            textBox1 = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            textBox3 = new System.Windows.Forms.TextBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            textBox5 = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            textBox6 = new System.Windows.Forms.TextBox();
            textBox4 = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            groupBox3 = new System.Windows.Forms.GroupBox();
            label10 = new System.Windows.Forms.Label();
            textBox10 = new System.Windows.Forms.TextBox();
            label11 = new System.Windows.Forms.Label();
            textBox7 = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            groupBox4 = new System.Windows.Forms.GroupBox();
            label8 = new System.Windows.Forms.Label();
            textBox8 = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox2);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(textBox1);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new System.Drawing.Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(438, 64);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Adaptadores";
            // 
            // textBox2
            // 
            textBox2.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "AdapterIp2", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox2.Location = new System.Drawing.Point(323, 27);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(100, 20);
            textBox2.TabIndex = 3;
            textBox2.Text = global::ClusterSrv.Properties.Settings.Default.AdapterIp2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(238, 30);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(68, 13);
            label2.TabIndex = 2;
            label2.Text = "Adaptador 2:";
            // 
            // textBox1
            // 
            textBox1.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "AdapterIp1", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox1.Location = new System.Drawing.Point(97, 27);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(100, 20);
            textBox1.TabIndex = 1;
            textBox1.Text = global::ClusterSrv.Properties.Settings.Default.AdapterIp1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(15, 30);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(68, 13);
            label1.TabIndex = 0;
            label1.Text = "Adaptador 1:";
            // 
            // textBox3
            // 
            textBox3.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "ClusterIp2", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox3.Location = new System.Drawing.Point(323, 27);
            textBox3.Name = "textBox3";
            textBox3.Size = new System.Drawing.Size(100, 20);
            textBox3.TabIndex = 3;
            textBox3.Text = global::ClusterSrv.Properties.Settings.Default.ClusterIp2;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBox5);
            groupBox2.Controls.Add(textBox3);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(textBox6);
            groupBox2.Controls.Add(textBox4);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(label4);
            groupBox2.Location = new System.Drawing.Point(12, 82);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(438, 88);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Cluster";
            // 
            // textBox5
            // 
            textBox5.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "ClusterMask2", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox5.Location = new System.Drawing.Point(323, 53);
            textBox5.Name = "textBox5";
            textBox5.Size = new System.Drawing.Size(100, 20);
            textBox5.TabIndex = 7;
            textBox5.Text = global::ClusterSrv.Properties.Settings.Default.ClusterMask2;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(238, 56);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(73, 13);
            label5.TabIndex = 6;
            label5.Text = "Máscara IP 2:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(238, 30);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(77, 13);
            label3.TabIndex = 2;
            label3.Text = "Dirección IP 2:";
            // 
            // textBox6
            // 
            textBox6.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "ClusterMask1", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox6.Location = new System.Drawing.Point(97, 53);
            textBox6.Name = "textBox6";
            textBox6.Size = new System.Drawing.Size(100, 20);
            textBox6.TabIndex = 5;
            textBox6.Text = global::ClusterSrv.Properties.Settings.Default.ClusterMask1;
            // 
            // textBox4
            // 
            textBox4.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "ClusterIp1", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox4.Location = new System.Drawing.Point(97, 27);
            textBox4.Name = "textBox4";
            textBox4.Size = new System.Drawing.Size(100, 20);
            textBox4.TabIndex = 1;
            textBox4.Text = global::ClusterSrv.Properties.Settings.Default.ClusterIp1;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(15, 56);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(73, 13);
            label6.TabIndex = 4;
            label6.Text = "Máscara IP 1:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(15, 30);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(77, 13);
            label4.TabIndex = 0;
            label4.Text = "Dirección IP 1:";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(this._NodePortTB);
            groupBox3.Controls.Add(label10);
            groupBox3.Controls.Add(textBox10);
            groupBox3.Controls.Add(label11);
            groupBox3.Controls.Add(textBox7);
            groupBox3.Controls.Add(label7);
            groupBox3.Location = new System.Drawing.Point(12, 246);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(438, 88);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "Nodo Local";
            // 
            // _NodePortTB
            // 
            this._NodePortTB.Location = new System.Drawing.Point(323, 52);
            this._NodePortTB.Name = "_NodePortTB";
            this._NodePortTB.Size = new System.Drawing.Size(99, 20);
            this._NodePortTB.TabIndex = 7;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(238, 55);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(41, 13);
            label10.TabIndex = 6;
            label10.Text = "Puerto:";
            // 
            // textBox10
            // 
            textBox10.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "Ip", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox10.Location = new System.Drawing.Point(102, 52);
            textBox10.Name = "textBox10";
            textBox10.Size = new System.Drawing.Size(100, 20);
            textBox10.TabIndex = 5;
            textBox10.Text = global::ClusterSrv.Properties.Settings.Default.Ip;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(20, 55);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(68, 13);
            label11.TabIndex = 4;
            label11.Text = "Dirección IP:";
            // 
            // textBox7
            // 
            textBox7.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "NodeId", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox7.Enabled = false;
            textBox7.Location = new System.Drawing.Point(102, 26);
            textBox7.Name = "textBox7";
            textBox7.Size = new System.Drawing.Size(100, 20);
            textBox7.TabIndex = 1;
            textBox7.Text = global::ClusterSrv.Properties.Settings.Default.NodeId;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(20, 33);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(47, 13);
            label7.TabIndex = 0;
            label7.Text = "Nombre:";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(this._RNodePortTB);
            groupBox4.Controls.Add(label8);
            groupBox4.Controls.Add(textBox8);
            groupBox4.Controls.Add(label9);
            groupBox4.Location = new System.Drawing.Point(12, 176);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(438, 64);
            groupBox4.TabIndex = 3;
            groupBox4.TabStop = false;
            groupBox4.Text = "Nodo Remoto";
            // 
            // _RNodePortTB
            // 
            this._RNodePortTB.Location = new System.Drawing.Point(323, 31);
            this._RNodePortTB.Name = "_RNodePortTB";
            this._RNodePortTB.Size = new System.Drawing.Size(99, 20);
            this._RNodePortTB.TabIndex = 3;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(238, 34);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(41, 13);
            label8.TabIndex = 2;
            label8.Text = "Puerto:";
            // 
            // textBox8
            // 
            textBox8.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::ClusterSrv.Properties.Settings.Default, "EpIp", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            textBox8.Location = new System.Drawing.Point(102, 31);
            textBox8.Name = "textBox8";
            textBox8.Size = new System.Drawing.Size(100, 20);
            textBox8.TabIndex = 1;
            textBox8.Text = global::ClusterSrv.Properties.Settings.Default.EpIp;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(20, 34);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(68, 13);
            label9.TabIndex = 0;
            label9.Text = "Dirección IP:";
            // 
            // _OkBT
            // 
            this._OkBT.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._OkBT.Location = new System.Drawing.Point(196, 340);
            this._OkBT.Name = "_OkBT";
            this._OkBT.Size = new System.Drawing.Size(75, 23);
            this._OkBT.TabIndex = 4;
            this._OkBT.Text = "OK";
            this._OkBT.UseVisualStyleBackColor = true;
            this._OkBT.Click += new System.EventHandler(this._OkBT_Click);
            // 
            // ClusterConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 369);
            this.Controls.Add(this._OkBT);
            this.Controls.Add(groupBox4);
            this.Controls.Add(groupBox3);
            this.Controls.Add(groupBox2);
            this.Controls.Add(groupBox1);
            this.Name = "ClusterConfig";
            this.Text = "ClusterConfig";
            this.Load += new System.EventHandler(this.ClusterIpConfig_Load);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.TextBox _RNodePortTB;
      private System.Windows.Forms.TextBox _NodePortTB;
      private System.Windows.Forms.Button _OkBT;

   }
}