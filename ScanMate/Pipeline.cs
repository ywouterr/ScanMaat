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

        }

        

        public Pipeline()
        {
            InitializeComponent();
        }

        private void Pipeline_Load(object sender, EventArgs e)
        {
            string inputDir;
            //if (currentInputFolder.Text != "")
            //{
            //    inputDir = currentInputFolder.Text;
            //}
            //else
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
            

            var fileSystemWatcher = new FileSystemWatcher(@inputDir)
            {
                Filter = "*.jpg",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            fileSystemWatcher.Created += new FileSystemEventHandler(OnFileCreated);
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            sw.Restart();
            //Console.WriteLine("This one works");
            //Console.WriteLine(e.Name);
            //Console.WriteLine(e.ChangeType);
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
                    while (IsFileLocked(e.FullPath))
                    {
                        Thread.Sleep(100);
                    }

                    lock (locker)
                    {
                        Bitmap incoming = new Bitmap(inputFiles.Process());
                        Console.WriteLine("{0} is being processed.", e.FullPath);


                        //string file = e.Name;
                        //imageFileName.Text = file;
                        if (currentScan.Image != null) currentScan.Image = null;

                        if (incoming.Size.Height <= 0 || incoming.Size.Width <= 0 ||
                                incoming.Size.Height > 3000 || incoming.Size.Width > 3000) // dimension check
                            MessageBox.Show("Error in image dimensions (have to be > 0 and <= 3000)");
                        else
                            currentScan.Invoke((Action)
                            delegate ()
                            {
                                currentScan.Image = (Image)new Bitmap(e.FullPath);
                                currPathText.Text = imagePath;
                            });

                        // processing step
                        Bitmap resized = new Bitmap(incoming, new Size(incoming.Width / 2, incoming.Height / 2));

                        var stampsAndCoord = Apply.apply(resized);
                        List<Color[,]> processedStamps = stampsAndCoord.Item1;
                        List<Point> topLefts = stampsAndCoord.Item2;

                        Console.WriteLine("{0} is done processing.", e.FullPath);

                        string outputDir = "";

                        string directory = Directory.GetCurrentDirectory();

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


                        // check for largest height and largest width

                        int width = 0;
                        int height = 0;

                        foreach(Color[,] stamp in processedStamps)
                        {
                            if (stamp.GetLength(0) > width) width = stamp.GetLength(0);
                            if (stamp.GetLength(1) > height) height = stamp.GetLength(1);
                        }

                        directory = outputDir;// Directory.GetCurrentDirectory();

                        //Bitmap outputImage;
                        
                        Bitmap totalScan = new Bitmap(incoming.Size.Width, incoming.Size.Height);
                        //Bitmap postBeeld = new Bitmap(Resource1.PBsmall);
                        //int dimX = width + width + 20 - (width % postBeeld.Width);
                        //int dimY = height + height + 20 - (height % postBeeld.Height);
                        //Bitmap maxBackGround = new Bitmap(dimX, dimY);



                        //pictureBox2.Invoke((Action)
                        //    delegate ()
                        //    {
                        //        pictureBox2.Image = (Image)maxBackGround;
                        //    });

                        //Thread.Sleep(5000);

                        //using (TextureBrush brush = new TextureBrush(postBeeld, WrapMode.Tile))
                        //using (Graphics g = Graphics.FromImage(maxBackGround))
                        //{
                        //    // Do your painting in here
                        //    g.FillRectangle(brush, 0, 0, maxBackGround.Width, maxBackGround.Height);
                        //}

                        //RectangleF cloneRect = new RectangleF(0, 0, postBeeld.Width, postBeeld.Height);
                        //System.Drawing.Imaging.PixelFormat format =
                        //    postBeeld.PixelFormat;

                        //ImageLayout.Tile;

                        //for(int y = 0; y < dimY; y += postBeeld.Height)
                        //{
                        //    for(int x = 0; x < dimX; x += postBeeld.Width)
                        //    {

                        //        using (Graphics g = Graphics.FromImage(maxBackGround))
                        //        {
                        //            g.DrawImage(postBeeld, x, y);//, x + postBeeld.Width, y + postBeeld.Height);
                        //        }

                        //    }    
                        //}

                        //pictureBox2.Invoke((Action)
                        //    delegate ()
                        //    {
                        //        pictureBox2.Image = (Image)maxBackGround;
                        //    });

                        //string fileLocation = string.Format(directory + "\\backgrond.jpg");
                        //backGround.Save(fileLocation, ImageFormat.Jpeg);
                        //Bitmap cloneBitmap = postBeeld.Clone(cloneRect, format);

                        //// Draw the cloned portion of the Bitmap object.
                        //e.Graphics.DrawImage(cloneBitmap, 0, 0);


                        for (int i = 0; i < processedStamps.Count; i++)
                        {
                            //Color[,] displayImage = processedStamps[i];
                            int w = processedStamps[i].GetLength(0) + 20;
                            int h = processedStamps[i].GetLength(1) + 20;
                            //if (pictureBox2.Image != null) pictureBox2.Image.Dispose();
                            //outputImage = new Bitmap(w, h); // create new output image
                            Bitmap saveOutput = new Bitmap(w, h);//CropImage(maxBackGround, new Rectangle(new Point(0, 0), new Size(w, h)));

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
                            string fileLocation = string.Format(directory + "\\{0}-{1}.jpg", scanNr, i);
                            saveOutput.Save(fileLocation, ImageFormat.Jpeg);
                            //outputImage.Dispose();
                            saveOutput.Dispose();




                        }
                        
                        //maxBackGround.Dispose();
                        //postBeeld.Dispose();

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
                        //    pictureBox2.Invoke((Action)
                        //        delegate ()
                        //        {
                        //            pictureBox2.Image.Dispose();
                        //            pictureBox2.Image = null;
                        //            pictureBox2.Image = (Image)totalScan;
                        //        });
                        //}
                        //else pictureBox2.Image = (Image)totalScan;
                        //totalScan.Dispose();
                        //BackgroundImage.Dispose();
                        //postBeeld.Dispose();
                        //inputFiles.Dequeue();
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
                stream = new FileInfo(file).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
    }

    
}
