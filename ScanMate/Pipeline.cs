using System;
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
        private FileSystemWatcher fSW;


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
            SetInputFolderWatcher(currentInputFolder.Text);
            setOutputFolder();
        }

        // Method to initialize the FileSystemWatcher
        private void SetInputFolderWatcher(string inputDir)
        {
            // Ensure the directory exists
            if (!System.IO.Directory.Exists(inputDir))
            {
                MessageBox.Show("Input directory does not exist. Please select a valid folder.");
                return;
            }

            // If the watcher is already set up, stop it
            if (fSW != null)
            {
                fSW.EnableRaisingEvents = false;
                fSW.Dispose();
            }

            // Create a new FileSystemWatcher
            fSW = new FileSystemWatcher(@inputDir)
            {
                Filter = "*.*", // Use general filter for now, modify as per your needs
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            fSW.Created += new FileSystemEventHandler(OnFileCreated);
            fSW.EnableRaisingEvents = true;
        }


        //private void Pipeline_Load(object sender, EventArgs e)
        //{
        //    string inputDir;
        //    inputDir = "C:\\Users\\Yannick\\Documents\\Werk\\Scan programma\\ScanMate\\lab\\input";
        //    if(!System.IO.Directory.Exists(inputDir))
        //    {
        //        if (!System.IO.Directory.Exists("C:\\Users\\Rob\\Pictures"))
        //        {
        //            if (!System.IO.Directory.Exists("C:\\Gebruikers\\Rob\\Afbeeldingen"))
        //            {
        //                if (currentInputFolder.Text != "")
        //                {
        //                    inputDir = currentInputFolder.Text;
        //                }
        //                MessageBox.Show("Try specifying input folder by hand");
        //            }
        //            else inputDir = "C:\\Gebruikers\\Rob\\Afbeeldingen";
        //        }
        //        else inputDir = "C:\\Users\\Rob\\Pictures";
        //    }
        //    currentInputFolder.Text = inputDir;

        //    setOutputFolder();

        //    string[] filters = { "*.jpg", "*.jpeg", "*.bmp", "*.png" };

        //    foreach (string f in filters)
        //    {
        //        var fSW = new FileSystemWatcher(@inputDir)
        //        {
        //            Filter = f,
        //            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        //            EnableRaisingEvents = true
        //        };
        //        fSW.Created += new FileSystemEventHandler(OnFileCreated);
        //    }
        //}

        //private Task AttemptToOpenAsync(string filepath)
        //{
        //    return Task.Factory.StartNew(() =>
        //    {
        //        bool available = false;

        //        while (!available)
        //        {
        //            available = IsAvailable(filepath);
        //            Console.WriteLine($"IsAvailable: {available}");

        //            if (!available)
        //            {
        //                Thread.Sleep(100);
        //            }
        //        }

        //        return available;
        //    });
        //}
        private async Task AttemptToOpenAsync(string filepath)
        {
            int maxAttempts = 10;  // Set a maximum number of attempts to avoid infinite loops.
            int attempt = 0;
            bool available = false;

            while (!available && attempt < maxAttempts)
            {
                available = IsAvailable(filepath);
                Console.WriteLine($"IsAvailable: {available}");

                if (!available)
                {
                    attempt++;
                    await Task.Delay(500);  // Increase delay to give more time for writing completion.
                }
            }

            if (!available)
            {
                throw new IOException($"File '{filepath}' is still unavailable after multiple attempts.");
            }
        }

        private bool IsAvailable(string filepath)
        {
            try
            {
                // Attempt to open with FileShare.ReadWrite to avoid locking issues
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return true;
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

            return false;
        }


        private static Bitmap cropImage(Bitmap img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        private async Task WaitUntilFileIsAvailableAsync(string filepath)
        {
            int delay = 200; // Start with a 200ms delay
            int maxAttempts = 50;  // Maximum number of attempts
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return; // File is available
                    }
                }
                catch (IOException ex)
                {
                    // Log and continue retrying
                    Console.WriteLine($"File access attempt {attempt + 1} failed: {ex.Message}");
                }

                // Increase the delay exponentially to provide more time with each failed attempt
                await Task.Delay(delay);
                delay = Math.Min(delay * 2, 5000); // Cap the delay at 5 seconds
                attempt++;
            }

            throw new IOException($"File '{filepath}' is still unavailable after multiple attempts.");
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
                // Delay before attempting to access the file, to give time for writing completion
                await Task.Delay(1000); // Add a 1-second delay

                try
                {
                    // Ensure that the file is fully available before attempting to use it
                    await WaitUntilFileIsAvailableAsync(imagePath);

                    lock (locker)
                    {
                        using (Bitmap incoming = new Bitmap(inputFiles.Process()))
                        {
                            int left = 0;
                            int right = incoming.Width;
                            int top = incoming.Height / 30;
                            int bottom = incoming.Height - incoming.Height / 30 - 1;
                            Rectangle cropArea = new Rectangle(left, top, right, bottom);
                            Bitmap cropped = cropImage(incoming, cropArea);

                            Console.WriteLine("{0} is being processed.", e.FullPath);

                            if (currentScan.Image != null)
                            {
                                currentScan.Image = null;
                            }

                            currentScan.Invoke((Action)delegate ()
                            {
                                currentScan.Image = cropped;
                                currPathText.Text = imagePath;
                            });

                            Console.WriteLine("incom W factor {0}", incoming.Width);
                            Console.WriteLine("incom H factor {0}", incoming.Height);

                            var stampsAndCoord = ImageToCutouts.process(incoming);
                            Console.WriteLine("{0} is done processing.", imagePath);

                            // Saving output image
                            string outputDirectory = Directory.GetCurrentDirectory();
                            Bitmap totalScan = new Bitmap(incoming.Size.Width, incoming.Size.Height);

                            for (int i = 0; i < stampsAndCoord.Count; i++)
                            {
                                Color[,] processedStamp = stampsAndCoord[i].Item1;
                                Point topLeft = stampsAndCoord[i].Item2;
                                int w = processedStamp.GetLength(0) + 20;
                                int h = processedStamp.GetLength(1) + 20;
                                Bitmap saveOutput = new Bitmap(w, h);

                                for (int x = 0; x < w - 20; x++)
                                {
                                    for (int y = 0; y < h - 20; y++)
                                    {
                                        Color newColor = Color.FromArgb(processedStamp[x, y].R, processedStamp[x, y].G, processedStamp[x, y].B);
                                        saveOutput.SetPixel(x + 10, y + 10, newColor);
                                        lock (locker)
                                        {
                                            totalScan.SetPixel(x + topLeft.X, y + topLeft.Y, newColor);
                                        }
                                    }
                                }

                                string fileLocation = string.Format(outputDirectory + "\\{0}-{1}.jpg", scanNr, i);
                                saveOutput.Save(fileLocation, ImageFormat.Jpeg);
                                saveOutput.Dispose();
                            }

                            scanNr++;

                            sw.Stop();
                            Console.WriteLine("Runtime: {0} seconds", sw.Elapsed.TotalSeconds.ToString());

                            if (pictureBox2.Image != null && totalScan != null)
                            {
                                pictureBox2.Image.Dispose();
                                pictureBox2.Image = null;
                            }
                            pictureBox2.Image = totalScan;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("Something went wrong while accessing the file.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ERROR: {ex.Message}");
                }
            }
        }



        //private void setOutputFolder()
        //{
        //    string outputDir = currentOutputFolder.Text;
        //    if (currentOutputFolder.Text != "" && System.IO.Directory.Exists(outputDir))
        //    {
        //        Directory.SetCurrentDirectory(@outputDir);
        //    }
        //    else if (System.IO.Directory.Exists("C:\\Users\\Rob\\Pictures\\uitgesneden"))
        //    {
        //        Console.WriteLine("It exists");
        //        outputDir = "C:\\Users\\Rob\\Pictures\\uitgesneden";
        //        Directory.SetCurrentDirectory(outputDir);
        //        currentOutputFolder.Text = outputDir;
        //    }
        //    else if (System.IO.Directory.Exists("C:\\Gebruikers\\Rob\\Afbeeldingen\\uitgesneden"))
        //    {
        //        outputDir = "C:\\Gebruikers\\Rob\\Afbeeldingen\\uitgesneden";
        //        Directory.SetCurrentDirectory(outputDir);
        //        currentOutputFolder.Text = outputDir;
        //    }
        //    else MessageBox.Show("No access to tried paths\n\'C:\\Users\\Rob\\Pictures\\uitgesneden\' and \'C:\\Gebruikers\\Rob\\Afbeeldingen\\uitgesneden\'");

        //}

        private void setOutputFolder()
        {
            string outputDir = currentOutputFolder.Text;
            if (!string.IsNullOrEmpty(outputDir) && System.IO.Directory.Exists(outputDir))
            {
                Directory.SetCurrentDirectory(outputDir);
            }
            else
            {
                MessageBox.Show("Invalid output folder. Please select a valid path.");
            }
        }

        private void currentOutputFolder_TextChanged(object sender, EventArgs e)
        {
            setOutputFolder();
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
            string newInputDir = currentInputFolder.Text;
            if (!string.IsNullOrEmpty(newInputDir) && System.IO.Directory.Exists(newInputDir))
            {
                SetInputFolderWatcher(newInputDir);
            }
            else
            {
                MessageBox.Show("Invalid input folder. Please select a valid path.");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Variables.ContrastCorrection = !Variables.ContrastCorrection;
        }
    }

    
}
