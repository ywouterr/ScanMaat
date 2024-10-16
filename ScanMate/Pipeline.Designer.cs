
namespace ScanMate
{
    partial class Pipeline
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
            this.currentScan = new System.Windows.Forms.PictureBox();
            this.currentScanLoc = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.currPathText = new System.Windows.Forms.TextBox();
            this.setCluster = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.inputFolderButton = new System.Windows.Forms.Button();
            this.currentInputFolder = new System.Windows.Forms.TextBox();
            this.outputFolderButton = new System.Windows.Forms.Button();
            this.currentOutputFolder = new System.Windows.Forms.TextBox();
            this.okPixels = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.currentScan)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // currentScan
            // 
            this.currentScan.Location = new System.Drawing.Point(42, 44);
            this.currentScan.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.currentScan.Name = "currentScan";
            this.currentScan.Size = new System.Drawing.Size(559, 595);
            this.currentScan.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.currentScan.TabIndex = 0;
            this.currentScan.TabStop = false;
            // 
            // currentScanLoc
            // 
            this.currentScanLoc.AutoSize = true;
            this.currentScanLoc.Location = new System.Drawing.Point(38, 659);
            this.currentScanLoc.Name = "currentScanLoc";
            this.currentScanLoc.Size = new System.Drawing.Size(161, 20);
            this.currentScanLoc.TabIndex = 1;
            this.currentScanLoc.Text = "Currently processing: ";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(626, 44);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(559, 595);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 2;
            this.pictureBox2.TabStop = false;
            // 
            // currPathText
            // 
            this.currPathText.Location = new System.Drawing.Point(209, 655);
            this.currPathText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.currPathText.Name = "currPathText";
            this.currPathText.Size = new System.Drawing.Size(391, 26);
            this.currPathText.TabIndex = 4;
            // 
            // setCluster
            // 
            this.setCluster.Location = new System.Drawing.Point(209, 702);
            this.setCluster.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.setCluster.MaxLength = 3;
            this.setCluster.Name = "setCluster";
            this.setCluster.Size = new System.Drawing.Size(112, 26);
            this.setCluster.TabIndex = 5;
            this.setCluster.Text = "40";
            this.setCluster.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(38, 706);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(151, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Pixels for Clustering:";
            // 
            // inputFolderButton
            // 
            this.inputFolderButton.Location = new System.Drawing.Point(626, 670);
            this.inputFolderButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.inputFolderButton.Name = "inputFolderButton";
            this.inputFolderButton.Size = new System.Drawing.Size(162, 38);
            this.inputFolderButton.TabIndex = 7;
            this.inputFolderButton.Text = "Select input folder";
            this.inputFolderButton.UseVisualStyleBackColor = true;
            this.inputFolderButton.Click += new System.EventHandler(this.inputFolderButton_Click);
            // 
            // currentInputFolder
            // 
            this.currentInputFolder.Location = new System.Drawing.Point(626, 715);
            this.currentInputFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.currentInputFolder.Name = "currentInputFolder";
            this.currentInputFolder.Size = new System.Drawing.Size(559, 26);
            this.currentInputFolder.TabIndex = 8;
            this.currentInputFolder.Text = "";
            this.currentInputFolder.TextChanged += new System.EventHandler(this.currentInputFolder_TextChanged);
            // 
            // outputFolderButton
            // 
            this.outputFolderButton.Location = new System.Drawing.Point(626, 772);
            this.outputFolderButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.outputFolderButton.Name = "outputFolderButton";
            this.outputFolderButton.Size = new System.Drawing.Size(162, 35);
            this.outputFolderButton.TabIndex = 9;
            this.outputFolderButton.Text = "Select output folder";
            this.outputFolderButton.UseVisualStyleBackColor = true;
            this.outputFolderButton.Click += new System.EventHandler(this.outputFolderButton_Click);
            // 
            // currentOutputFolder
            // 
            this.currentOutputFolder.Location = new System.Drawing.Point(626, 815);
            this.currentOutputFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.currentOutputFolder.Name = "currentOutputFolder";
            this.currentOutputFolder.Size = new System.Drawing.Size(559, 26);
            this.currentOutputFolder.TabIndex = 10;
            this.currentOutputFolder.Text = "C:\\Users\\Yannick\\Documents\\Werk\\Scan programma\\ScanMate\\lab\\processed";
            // 
            // okPixels
            // 
            this.okPixels.Location = new System.Drawing.Point(328, 699);
            this.okPixels.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.okPixels.Name = "okPixels";
            this.okPixels.Size = new System.Drawing.Size(86, 36);
            this.okPixels.TabIndex = 11;
            this.okPixels.Text = "Ok";
            this.okPixels.UseVisualStyleBackColor = true;
            this.okPixels.Click += new System.EventHandler(this.okPixels_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(209, 747);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(158, 30);
            this.checkBox1.TabIndex = 21;
            this.checkBox1.Text = "Detect dark stamps";
            this.checkBox1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // Pipeline
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1221, 876);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.okPixels);
            this.Controls.Add(this.currentOutputFolder);
            this.Controls.Add(this.outputFolderButton);
            this.Controls.Add(this.currentInputFolder);
            this.Controls.Add(this.inputFolderButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.setCluster);
            this.Controls.Add(this.currPathText);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.currentScanLoc);
            this.Controls.Add(this.currentScan);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Pipeline";
            this.Text = "ScanMate v0";
            this.Load += new System.EventHandler(this.Pipeline_Load);
            ((System.ComponentModel.ISupportInitialize)(this.currentScan)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox currentScan;
        private System.Windows.Forms.Label currentScanLoc;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.TextBox currPathText;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox setCluster;
        private System.Windows.Forms.Button inputFolderButton;
        private System.Windows.Forms.TextBox currentInputFolder;
        private System.Windows.Forms.Button outputFolderButton;
        private System.Windows.Forms.TextBox currentOutputFolder;
        private System.Windows.Forms.Button okPixels;
        private System.Windows.Forms.CheckBox checkBox1;
    }
}

