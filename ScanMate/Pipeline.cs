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

        public class Variables
        {
            private static int clusterDist = 20;
            private static int clusterSpac = 20;
            private static int scalingFact = 1;
            private static bool cropping = false;
            
            public static int ClustDist
            {
                get { return clusterDist; }
                set { clusterDist = value; }
            }
            public static int ClustSpac
            {
                get { return clusterSpac; }
                set { clusterSpac = value; }
            }
            public static int ScalingFact
            {
                get { return scalingFact; }
                set { scalingFact = value; }
            }
            public static bool Cropping
            {
                get { return cropping; }
                set { cropping = value; }
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

        private static string CreateLocalCopy(string filePath)
        {
            try
            {
                string localCopyPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(filePath));

                // Copy the file to a local directory
                File.Copy(filePath, localCopyPath);

                Console.WriteLine("Local copy created: " + localCopyPath);

                return localCopyPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating local copy: " + ex.Message);
                return null;
            }
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

        //public class BackGround
        //{
        //    Bitmap backGround;
        //    public Bitmap maxBackGround
        //    {
        //        get { return backGround; }
        //        set { backGround = value; }
        //    }
        //}

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
                        if (Variables.Cropping)
                        {
                            int left = (int)((incoming.Width / 9.1) * 1.6);
                            int right = incoming.Width - left;
                            int top = (right - left) / 30;
                            int bottom = top * 23;
                            Rectangle cropArea = new Rectangle(left, top, right, bottom);
                            incoming = cropImage(incoming, cropArea);
                        }
                        Console.WriteLine("{0} is being processed.", e.FullPath);// localCopyOfPath);
                        if (currentScan.Image != null) currentScan.Image = null;

                        //if (incoming.Size.Height <= 0 || incoming.Size.Width <= 0 ||
                        //        incoming.Size.Height > 3000 || incoming.Size.Width > 4000) // dimension check
                        //    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 4000w/3000h)");
                        //else
                        currentScan.Invoke((Action) delegate ()
                        {
                            currentScan.Image = (Image)new Bitmap(e.FullPath);
                            currPathText.Text = imagePath;
                        });
                        Console.WriteLine("incom W factor {0}", incoming.Width);
                        Console.WriteLine("incom H factor {0}", incoming.Height);
                        Console.WriteLine("Scaling factor {0}", Variables.ScalingFact);
                        // processing step
                        Bitmap resized = new Bitmap(incoming, new Size(Convert.ToInt32(incoming.Width / Variables.ScalingFact), Convert.ToInt32(incoming.Height / Variables.ScalingFact)));

                        var stampsAndCoord = Apply.apply(resized);
                        List<Color[,]> processedStamps = stampsAndCoord.Item1;
                        List<Point> topLefts = stampsAndCoord.Item2;
                        Console.WriteLine("{0} is done processing.", imagePath);//localCopyOfPath);

                        string outputDirectory = setOutputFolder();
                        Bitmap totalScan = new Bitmap(incoming.Size.Width, incoming.Size.Height);
                        
                        // check for largest height and largest width

                        //int width = 0;
                        //int height = 0;

                        //foreach(Color[,] stamp in processedStamps)
                        //{
                        //    if (stamp.GetLength(0) > width) width = stamp.GetLength(0);
                        //    if (stamp.GetLength(1) > height) height = stamp.GetLength(1);
                        //}


                        //Bitmap postBeeld = new Bitmap(Resource1.PBsmall);
                        //int dimX = width + width + 20 - (width % postBeeld.Width);
                        //int dimY = height + height + 20 - (height % postBeeld.Height);
                        //BackGround bg = new BackGround() { maxBackGround = new System.Drawing.Bitmap(dimX, dimY) };

                        //Bitmap maxBackGround = new Bitmap(dimX, dimY);

                        //using (TextureBrush brush = new TextureBrush(postBeeld, WrapMode.Tile))
                        //using (Graphics g = Graphics.FromImage(bg.maxBackGround))
                        //{
                        //    // Do your painting in here
                        //    g.FillRectangle(brush, 0, 0, bg.maxBackGround.Width, bg.maxBackGround.Height);
                        //}


                        //pictureBox2.Invoke((Action)
                        //    delegate ()
                        //    {
                        //        pictureBox2.Image = (Image)bg.maxBackGround;
                        //    });

                        for (int i = 0; i < processedStamps.Count; i++)
                        {
                            int w = processedStamps[i].GetLength(0) + 20;
                            int h = processedStamps[i].GetLength(1) + 20;
                            Bitmap saveOutput = new Bitmap(w, h);
                            //CopyRegionIntoStamp(bg.maxBackGround, new Rectangle(0, 0, w - 20, h - 20), ref saveOutput, new Rectangle(0, 0, w, h));//new Bitmap(w, h);//CropImage(maxBackGround, new Rectangle(new Point(0, 0), new Size(w, h)));

                            // copy array to output Bitmap
                            for (int x = 0; x < w - 20; x++)             // loop over columns
                                for (int y = 0; y < h - 20; y++)         // loop over rows
                                {
                                    Color newColor = Color.FromArgb(processedStamps[i][x, y].R, processedStamps[i][x, y].G, processedStamps[i][x, y].B);
                                    //only modify below row for gray/color
                                    //Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                                    //outputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                                    saveOutput.SetPixel(x + 10, y + 10, newColor);
                                    lock (locker)
                                    {
                                        totalScan.SetPixel(x + topLefts[i].X, y + topLefts[i].Y, newColor);
                                    }
                                }

                            //                         // display output image
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
        //public static T Clone<T>(T source)
        //{
        //    if (!typeof(T).IsSerializable)
        //    {
        //        throw new ArgumentException("The type must be serializable.", nameof(source));
        //    }

        //    // Don't serialize a null object, simply return the default for that object
        //    if (ReferenceEquals(source, null)) return default;

        //    using var Stream stream = new MemoryStream();
        //    IFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(stream, source);
        //    stream.Seek(0, SeekOrigin.Begin);
        //    return (T)formatter.Deserialize(stream);
        //}

        public static void CopyRegionIntoStamp(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
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

        private static Bitmap CropImage(Bitmap img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return img.Clone(cropArea, bmpImage.PixelFormat);
        }

        private static bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = new FileInfo(file).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch (FileNotFoundException err)
            {
                throw err;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:  
                //still being written to  
                //or being processed by another thread  
                //or does not exist (has already been processed)  
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked  
            return false;
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

        private void okSpacing_Click(object sender, EventArgs e)
        {
            if (setSpacing.Text != "")
            {
                int number;
                if (int.TryParse(setSpacing.Text, out number))
                {
                    Variables.ClustSpac = int.Parse(setCluster.Text);
                }
                else MessageBox.Show("Please specify a number using { 0123456789 } only.");
            }
            else Variables.ClustSpac = 20;
        }

        private void okScaling_Click(object sender, EventArgs e)
        {
            if (setScaling.Text != "")
            {
                int number;
                if (int.TryParse(setScaling.Text, out number))
                {
                    Variables.ScalingFact = int.Parse(setScaling.Text);
                }
                else MessageBox.Show("Please specify a number using { 0123456789 } only.");
            }
            else Variables.ScalingFact = 4;
        }

        private void okCropping_Click(object sender, EventArgs e)
        {
            Variables.Cropping = (cropComboBox.Items[cropComboBox.SelectedIndex].ToString() == "A4");
        }
    }

    
}
