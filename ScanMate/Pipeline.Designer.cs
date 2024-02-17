
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
            this.label2 = new System.Windows.Forms.Label();
            this.setSpacing = new System.Windows.Forms.TextBox();
            this.okSpacing = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.currentScan)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // currentScan
            // 
            this.currentScan.Location = new System.Drawing.Point(37, 35);
            this.currentScan.Name = "currentScan";
            this.currentScan.Size = new System.Drawing.Size(497, 476);
            this.currentScan.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.currentScan.TabIndex = 0;
            this.currentScan.TabStop = false;
            // 
            // currentScanLoc
            // 
            this.currentScanLoc.AutoSize = true;
            this.currentScanLoc.Location = new System.Drawing.Point(34, 527);
            this.currentScanLoc.Name = "currentScanLoc";
            this.currentScanLoc.Size = new System.Drawing.Size(146, 17);
            this.currentScanLoc.TabIndex = 1;
            this.currentScanLoc.Text = "Currently processing: ";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(556, 35);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(497, 476);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 2;
            this.pictureBox2.TabStop = false;
            // 
            // currPathText
            // 
            this.currPathText.Location = new System.Drawing.Point(186, 524);
            this.currPathText.Name = "currPathText";
            this.currPathText.Size = new System.Drawing.Size(348, 22);
            this.currPathText.TabIndex = 4;
            // 
            // setCluster
            // 
            this.setCluster.Location = new System.Drawing.Point(186, 562);
            this.setCluster.MaxLength = 3;
            this.setCluster.Name = "setCluster";
            this.setCluster.Size = new System.Drawing.Size(100, 22);
            this.setCluster.TabIndex = 5;
            this.setCluster.Text = "20";
            this.setCluster.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 565);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "Pixels for Clustering:";
            // 
            // inputFolderButton
            // 
            this.inputFolderButton.Location = new System.Drawing.Point(556, 536);
            this.inputFolderButton.Name = "inputFolderButton";
            this.inputFolderButton.Size = new System.Drawing.Size(144, 30);
            this.inputFolderButton.TabIndex = 7;
            this.inputFolderButton.Text = "Select input folder";
            this.inputFolderButton.UseVisualStyleBackColor = true;
            this.inputFolderButton.Click += new System.EventHandler(this.inputFolderButton_Click);
            // 
            // currentInputFolder
            // 
            this.currentInputFolder.Location = new System.Drawing.Point(556, 572);
            this.currentInputFolder.Name = "currentInputFolder";
            this.currentInputFolder.Size = new System.Drawing.Size(497, 22);
            this.currentInputFolder.TabIndex = 8;
            this.currentInputFolder.Text = "C:\\Users\\Yannick\\Documents\\Werk\\Scan programma\\ScanMate\\lab\\input";
            // 
            // outputFolderButton
            // 
            this.outputFolderButton.Location = new System.Drawing.Point(556, 618);
            this.outputFolderButton.Name = "outputFolderButton";
            this.outputFolderButton.Size = new System.Drawing.Size(144, 28);
            this.outputFolderButton.TabIndex = 9;
            this.outputFolderButton.Text = "Select output folder";
            this.outputFolderButton.UseVisualStyleBackColor = true;
            this.outputFolderButton.Click += new System.EventHandler(this.outputFolderButton_Click);
            // 
            // currentOutputFolder
            // 
            this.currentOutputFolder.Location = new System.Drawing.Point(556, 652);
            this.currentOutputFolder.Name = "currentOutputFolder";
            this.currentOutputFolder.Size = new System.Drawing.Size(497, 22);
            this.currentOutputFolder.TabIndex = 10;
            this.currentOutputFolder.Text = "C:\\Users\\Yannick\\Documents\\Werk\\Scan programma\\ScanMate\\lab\\processed";
            // 
            // okPixels
            // 
            this.okPixels.Location = new System.Drawing.Point(292, 559);
            this.okPixels.Name = "okPixels";
            this.okPixels.Size = new System.Drawing.Size(76, 29);
            this.okPixels.TabIndex = 11;
            this.okPixels.Text = "Ok";
            this.okPixels.UseVisualStyleBackColor = true;
            this.okPixels.Click += new System.EventHandler(this.okPixels_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 599);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(155, 17);
            this.label2.TabIndex = 12;
            this.label2.Text = "Spacing within clusters:";
            // 
            // setSpacing
            // 
            this.setSpacing.Location = new System.Drawing.Point(186, 596);
            this.setSpacing.Name = "setSpacing";
            this.setSpacing.Size = new System.Drawing.Size(100, 22);
            this.setSpacing.TabIndex = 13;
            this.setSpacing.Text = "20";
            this.setSpacing.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // okSpacing
            // 
            this.okSpacing.Location = new System.Drawing.Point(293, 594);
            this.okSpacing.Name = "okSpacing";
            this.okSpacing.Size = new System.Drawing.Size(75, 26);
            this.okSpacing.TabIndex = 14;
            this.okSpacing.Text = "Ok";
            this.okSpacing.UseVisualStyleBackColor = true;
            this.okSpacing.Click += new System.EventHandler(this.okSpacing_Click);
            // 
            // Pipeline
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1085, 701);
            this.Controls.Add(this.okSpacing);
            this.Controls.Add(this.setSpacing);
            this.Controls.Add(this.label2);
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
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox setSpacing;
        private System.Windows.Forms.Button okSpacing;
    }
}

