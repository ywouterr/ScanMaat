﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;
using ScanMate.Methods;
using ScanMate.Methods.Processing;
using ScanMate.Methods.Processing.Processing;
using System.Diagnostics;
using ScanMate.Domain;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace ScanMate
{
    public partial class Pipeline : Form
    {
        private static Object locker = new Object();

        public inputQueue inputFiles = new inputQueue();
        Stopwatch sw = new Stopwatch();
        int scanNr = 0;
        //static readonly object locker = new object();

        public class Variables
        {
            private static int clusterDist = 40;
            private static bool darkMode = false;

            public static int ClustDist
            {
                get { return clusterDist; }
                set { clusterDist = value; }
            }
            public static bool ContrastCorrection
            {
                get { return darkMode; }
                set { darkMode = value; }
            }
        }

        

        public Pipeline()
        {
            InitializeComponent();
        }

        private void Pipeline_Load(object sender, EventArgs e)
        {
            string inputDir;
            inputDir = "C:\\Users\\Yannick\\Documents\\Werk\\Scan programma\\ScanMate\\lab\\input";
            if(!System.IO.Directory.Exists(inputDir))
            {
                if (!System.IO.Directory.Exists("C:\\Users\\Rob\\Pictures"))
                {
                    if (!System.IO.Directory.Exists("C:\\Gebruikers\\Rob\\Afbeeldingen"))
                    {
                        if (currentInputFolder.Text != "")
                        {
                            inputDir = currentInputFolder.Text;
                        }
                        MessageBox.Show("Try specifying input folder by hand");
                    }
                    else inputDir = "C:\\Gebruikers\\Rob\\Afbeeldingen";
                }
                else inputDir = "C:\\Users\\Rob\\Pictures";
            }
            currentInputFolder.Text = inputDir;
            

            var fileSystemWatcher = new FileSystemWatcher(@inputDir)
            {
                Filter = "*.jpg",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            fileSystemWatcher.Created += new FileSystemEventHandler(OnFileCreated);
        }

        private Task AttemptToOpenAsync(string filepath)
        {
            return Task.Factory.StartNew(() =>
            {
                bool available = false;

                while (!available)
                {
                    available = IsAvailable(filepath);
                    Console.WriteLine($"IsAvailable: {available}");

                    if (!available)
                    {
                        Thread.Sleep(100);
                    }
                }

                return available;
            });
        }

        private bool IsAvailable(string filepath)
        {
            bool result = false;

            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    result = true;
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        private static Bitmap cropImage(Bitmap img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            sw.Restart();
            string imagePath = e.FullPath;
            inputFiles.AddToQ(imagePath);
            Console.WriteLine("{0} is queued.", e.FullPath);

            int number = 0;
            if (!int.TryParse(setCluster.Text.Trim(), out number))
            {
                MessageBox.Show("Please enter a valid number for Clustering.");
                setCluster.Text = "20";
            }
            else
            {
                while (inputFiles.Inspect() != imagePath) Thread.Sleep(100);
                try
                {
                    //string localCopyOfPath = CreateLocalCopy(e.FullPath);
                    await AttemptToOpenAsync(imagePath);
                    lock (locker)
                    {
                        Bitmap incoming = new Bitmap(inputFiles.Process());
                        int left = 0;
                        int right = incoming.Width;
                        int top = incoming.Height/30;
                        int bottom = incoming.Height - incoming.Height / 30 - 1;
                        Rectangle cropArea = new Rectangle(left, top, right, bottom);
                        incoming = cropImage(incoming, cropArea);
                        Console.WriteLine("{0} is being processed.", e.FullPath);// localCopyOfPath);
                        if (currentScan.Image != null) currentScan.Image = null;

                        currentScan.Invoke((Action) delegate ()
                        {
                            currentScan.Image = (Image)new Bitmap(e.FullPath);
                            currPathText.Text = imagePath;
                        });
                        Console.WriteLine("incom W factor {0}", incoming.Width);
                        Console.WriteLine("incom H factor {0}", incoming.Height);

                        var stampsAndCoord = ImageToCutouts.process(incoming);
                        Console.WriteLine("{0} is done processing.", imagePath);

                        string outputDirectory = setOutputFolder();
                        Bitmap totalScan = new Bitmap(incoming.Size.Width, incoming.Size.Height);
                        Bitmap debuggingOutput = new Bitmap(incoming.Size.Width, incoming.Size.Height);

                        for (int i = 0; i < stampsAndCoord.Count; i++)
                        {
                            Color[,] processedStamp = stampsAndCoord[i].Item1;
                            Point topLeft = stampsAndCoord[i].Item2;
                            int w = processedStamp.GetLength(0) + 20;
                            int h = processedStamp.GetLength(1) + 20;
                            Bitmap saveOutput = new Bitmap(w, h);
                            Bitmap houghImage = new Bitmap(incoming.Size.Width, incoming.Size.Height);

                            // copy array to output Bitmap
                            for (int x = 0; x < w - 20; x++)
                                for (int y = 0; y < h - 20; y++) 
                                {
                                    Color newColor = Color.FromArgb(processedStamp[x, y].R, processedStamp[x, y].G, processedStamp[x, y].B);
                                    saveOutput.SetPixel(x + 10, y + 10, newColor);
                                    lock (locker)
                                    {
                                        totalScan.SetPixel(x + topLeft.X, y + topLeft.Y, newColor);
                                    }
                                }

                            //save output image
                            string fileLocation = string.Format(outputDirectory + "\\{0}-{1}.jpg", scanNr, i);
                            saveOutput.Save(fileLocation, ImageFormat.Jpeg);
                            saveOutput.Dispose();

                            //debugging
                            Color[,] debuggingContour = stampsAndCoord[i].Item3;
                            int ww = debuggingContour.GetLength(0);
                            int hh = debuggingContour.GetLength(1);

                            // copy array to output Bitmap
                            for (int x = 0; x < ww; x++)
                                for (int y = 0; y < hh; y++)
                                {
                                    if (debuggingContour[x, y].R == 255) {
                                        Color newColor = Color.FromArgb(debuggingContour[x, y].R, 0, 0);
                                        lock (locker)
                                        {
                                            debuggingOutput.SetPixel(x, y, newColor);
                                        }
                                    }
                                }

                            byte[,] houghByteArray = stampsAndCoord[i].Item4;
                            int www = houghByteArray.GetLength(0);
                            int hhh = houghByteArray.GetLength(1);
                            byte pixelvalue;

                            // copy array to output Bitmap
                            for (int x = 0; x < www; x++)
                                for (int y = 0; y < hhh; y++)
                                {
                                    pixelvalue = houghByteArray[x, y];
                                        Color newColor = Color.FromArgb(pixelvalue, pixelvalue, pixelvalue);
                                        lock (locker)
                                        {
                                            houghImage.SetPixel(x, y, newColor);
                                        }
                                }
                            //save output image
                            string houghLocation = string.Format(outputDirectory + "\\{0}-{1}-ht.jpg", scanNr, i);
                            houghImage.Save(houghLocation, ImageFormat.Jpeg);
                            houghImage.Dispose();

                        }

                        //save output image
                        string debuggingLocation = string.Format(outputDirectory + "\\{0}-debug.jpg", scanNr);
                        debuggingOutput.Save(debuggingLocation, ImageFormat.Jpeg);
                        debuggingOutput.Dispose();

                        scanNr++;

                        sw.Stop();
                        Console.WriteLine("Runtime: {0} seconds", sw.Elapsed.TotalSeconds.ToString());

                        if (pictureBox2.Image != null && totalScan != null)
                        {
                            pictureBox2.Image.Dispose();
                            pictureBox2.Image = null;
                            pictureBox2.Image = totalScan;
                        }
                        else pictureBox2.Image = totalScan;
                    }
                }
                catch(FileNotFoundException)
                {
                    MessageBox.Show("Something went wrong while accessing the file.");
                }
                catch(Exception ex)
                {
                    MessageBox.Show("ERROR: {0}", ex.Message);
                }
                
            }
        }

        private string setOutputFolder()
        {
            string outputDir = "";
            if (currentOutputFolder.Text != "")
            {
                outputDir = currentOutputFolder.Text;
                if (System.IO.Directory.Exists(outputDir))
                {
                    Directory.SetCurrentDirectory(@outputDir);
                }
                else MessageBox.Show("Folder specified not valid");
            }
            else if (System.IO.Directory.Exists("C:\\Users\\Rob\\Pictures\\uitgesneden"))
            {
                outputDir = "C:\\Users\\Rob\\Pictures\\uitgesneden";
                Directory.SetCurrentDirectory(@outputDir);
                currentOutputFolder.Text = outputDir;
            }
            else if (System.IO.Directory.Exists("C:\\Gebruikers\\Rob\\Afbeeldingen\\uitgesneden"))
            {
                outputDir = "C:\\Gebruikers\\Rob\\Afbeeldingen\\uitgesneden";
                Directory.SetCurrentDirectory(outputDir);
                currentOutputFolder.Text = outputDir;
            }
            else MessageBox.Show("No access to tried paths\n\'C:\\Users\\Rob\\Pictures\\uitgesneden\' and \'C:\\Gebruikers\\Rob\\Afbeeldingen\\uitgesneden\'");
            return outputDir;
        }

        private void inputFolderButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentInputFolder.Text = dialog.SelectedPath;
            }
            else
            {
                currentInputFolder.Text = "";
            }
        }

        private void outputFolderButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentOutputFolder.Text = dialog.SelectedPath;
            }
            else
            {
                currentOutputFolder.Text = "";
            }
        }

        private void okPixels_Click(object sender, EventArgs e)
        {
            if (setCluster.Text != "")
            {
                int number;
                if (int.TryParse(setCluster.Text, out number))
                {
                    Variables.ClustDist = int.Parse(setCluster.Text);
                }
                else MessageBox.Show("Please specify a number using { 0123456789 } only.");
            }
            else Variables.ClustDist = 20;
        }

        private void currentInputFolder_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Variables.ContrastCorrection = !Variables.ContrastCorrection;
        }
    }

    
}
