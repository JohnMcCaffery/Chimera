namespace Chimera.OpenSim.GUI.Controls.Plugins {
    partial class KeyPresserPanel {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.startButton = new System.Windows.Forms.Button();
            this.intervalS = new System.Windows.Forms.NumericUpDown();
            this.key = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.stopM = new System.Windows.Forms.NumericUpDown();
            this.shutdownBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.intervalS)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stopM)).BeginInit();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(4, 4);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(195, 23);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // intervalS
            // 
            this.intervalS.DecimalPlaces = 3;
            this.intervalS.Location = new System.Drawing.Point(100, 33);
            this.intervalS.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.intervalS.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.intervalS.Name = "intervalS";
            this.intervalS.Size = new System.Drawing.Size(99, 20);
            this.intervalS.TabIndex = 1;
            this.intervalS.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.intervalS.ValueChanged += new System.EventHandler(this.intervalS_ValueChanged);
            // 
            // key
            // 
            this.key.Location = new System.Drawing.Point(100, 59);
            this.key.Name = "key";
            this.key.Size = new System.Drawing.Size(99, 20);
            this.key.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "IntervalS";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(25, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Key";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1, 87);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "StopM";
            // 
            // stopM
            // 
            this.stopM.DecimalPlaces = 1;
            this.stopM.Location = new System.Drawing.Point(100, 85);
            this.stopM.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
            this.stopM.Name = "stopM";
            this.stopM.Size = new System.Drawing.Size(99, 20);
            this.stopM.TabIndex = 6;
            this.stopM.ValueChanged += new System.EventHandler(this.shutdownM_ValueChanged);
            // 
            // shutdownBox
            // 
            this.shutdownBox.AutoSize = true;
            this.shutdownBox.Location = new System.Drawing.Point(0, 111);
            this.shutdownBox.Name = "shutdownBox";
            this.shutdownBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.shutdownBox.Size = new System.Drawing.Size(114, 17);
            this.shutdownBox.TabIndex = 7;
            this.shutdownBox.Text = "Shutdown on Stop";
            this.shutdownBox.UseVisualStyleBackColor = true;
            this.shutdownBox.CheckedChanged += new System.EventHandler(this.shutdownBox_CheckedChanged);
            // 
            // KeyPresserPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.shutdownBox);
            this.Controls.Add(this.stopM);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.key);
            this.Controls.Add(this.intervalS);
            this.Controls.Add(this.startButton);
            this.Name = "KeyPresserPanel";
            this.Size = new System.Drawing.Size(626, 478);
            ((System.ComponentModel.ISupportInitialize)(this.intervalS)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stopM)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.NumericUpDown intervalS;
        private System.Windows.Forms.TextBox key;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown stopM;
        private System.Windows.Forms.CheckBox shutdownBox;
    }
}
