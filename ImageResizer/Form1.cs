using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageResizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            label4.Text = "";
            label5.Text = "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBox1.Text))
                    throw new Exception("Select source folder");

                if (string.IsNullOrWhiteSpace(textBox2.Text))
                    throw new Exception("Select destination folder");

                if (textBox1.Text.ToLower() == textBox2.Text.ToLower())
                    throw new Exception("Destination should not be source folder");

                if (string.IsNullOrWhiteSpace(textBox3.Text))
                    throw new Exception("Size required");

                if (Directory.Exists(textBox2.Text))
                {
                    var files = Directory.GetFiles(textBox2.Text);
                    var folders = Directory.GetDirectories(textBox2.Text);

                    if (files.Length != 0 || folders.Length != 0)
                    {
                        if (MessageBox.Show("Destination folder not empty\r\n\r\nDo you want to process?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
                            return;
                    }
                }

                panel1.Visible = true;
                statusMsg = "";
                statusMsg2 = "";
                Progress = 0;
                progressBar1.Value = 0;
                label4.Text = "";
                label5.Text = "";
                imageList.Clear();

                var size = textBox3.Text;
                var sizes = size.Split('x');

                int width = Convert.ToInt32(sizes[0]);
                int height = Convert.ToInt32(sizes[1]);

                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;

                button1.Enabled = false;
                button2.Enabled = false;

                button3.Enabled = false;

                timer1.Start();
                backgroundWorker1.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = fbd.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = fbd.SelectedPath;
            }
        }

        public string statusMsg { get; set; }
        public string statusMsg2 { get; set; }
        public int Progress { get; set; }
        public long totalSize { get; set; }
        public long doneSize { get; set; }
        public long totalFiles { get; set; }
        public long doneFiles { get; set; }
        public List<ImageFile> imageList = new List<ImageFile>();

        void ScanFolder(string path, string rootPath)
        {
            var files = Directory.GetFiles(path, "*.jpg");

            foreach (var file in files)
            {
                FileInfo fi = new FileInfo(file);

                ImageFile f = new ImageFile();
                f.Path = file;
                f.RelativePath = file.Replace(rootPath, "");
                f.Size = fi.Length;

                imageList.Add(f);
            }

            var folders = Directory.GetDirectories(path);

            foreach (var folder in folders)
            {
                ScanFolder(folder, rootPath);
            }
        }

        private void SafeCreateDirectory(string folderName)
        {
            DirectoryInfo di = new DirectoryInfo(folderName);

            if (!di.Exists)
            {
                if (!di.Parent.Exists)
                {
                    SafeCreateDirectory(di.Parent.Name);
                }

                Directory.CreateDirectory(folderName);
            }
        }

        private string GetFolderName(string f)
        {
            FileInfo fi = new FileInfo(f);
            return fi.DirectoryName;
        }

        public Size GetNewSize(Size OldSize, Size NewSize)
        {
            double OldRatio = (double)OldSize.Width / (double)OldSize.Height;

            double NewRatio = (double)NewSize.Width / (double)NewSize.Height;

            if (NewRatio == OldRatio)
            {
                return NewSize;
            }
            if (NewRatio > OldRatio)
            {
                NewSize = new Size((int)(NewSize.Height * OldRatio), NewSize.Height);
            }
            else
            {
                NewSize = new Size(NewSize.Width, (int)(NewSize.Width / OldRatio));
            }

            return NewSize;
        }

        private int ImageConvert(string sourcePath, string destinationPath, int width, int height)
        {
            Image img = new Bitmap(sourcePath);
            Size OldSize = new Size((int)img.PhysicalDimension.Width, (int)img.PhysicalDimension.Height);
            Size newSize = GetNewSize(OldSize, new Size(width, height));

            if (OldSize.Width != newSize.Width || OldSize.Height != newSize.Height)
            {
                SafeCreateDirectory(GetFolderName(destinationPath));
                try
                {
                    using (var m = new MemoryStream())
                    {
                        Bitmap newImage = new Bitmap(img, newSize);
                        newImage.Save(m, System.Drawing.Imaging.ImageFormat.Jpeg);
                        newImage.Dispose();
                        img.Dispose();
                        img = null;

                        //destinationPath

                        using (FileStream fs = new FileStream(destinationPath, FileMode.Create, FileAccess.ReadWrite))
                        {
                            byte[] bytes = m.ToArray();
                            fs.Write(bytes, 0, bytes.Length);
                        }
                    }

                    return 1;
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                try
                {
                    SafeCreateDirectory(GetFolderName(destinationPath));
                    File.Copy(sourcePath, destinationPath);
                }
                catch
                {
                    return -1;
                }
            }

            if (img != null)
                img.Dispose();

            return 0;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            statusMsg = "Scanning folder...";
            var destinationPath = textBox2.Text;
            ScanFolder(textBox1.Text, textBox1.Text);

            totalSize = imageList.Select(x => x.Size).Sum();
            totalFiles = imageList.Count();
            doneFiles = 0;
            doneSize = 0;

            var size = textBox3.Text;
            var sizes = size.Split('x');

            int width = Convert.ToInt32(sizes[0]);
            int height = Convert.ToInt32(sizes[1]);

            statusMsg = "Converting images...";
            foreach (var f in imageList)
            {
                //System.Threading.Thread.Sleep(1000);
                var sourceFile = f.Path;
                var destinationFile = destinationPath + f.RelativePath;

                f.SuccessStatus = ImageConvert(sourceFile, destinationFile, width, height);

                f.IsDone = true;
                doneSize += f.Size;
                doneFiles++;

                statusMsg = "Converting images (" + doneFiles + "/" + totalFiles + ") ...";
                statusMsg2 = f.RelativePath;
                Progress = (int)((doneSize * 100) / totalSize);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                FormStatus fs = new FormStatus(this.imageList);

                fs.ShowDialog();

                //MessageBox.Show("Done");

                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
            }

            panel1.Visible = false;

            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;

            button1.Enabled = true;
            button2.Enabled = true;

            button3.Enabled = true;

            timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label4.Text = statusMsg;
            label5.Text = statusMsg2;

            progressBar1.Value = Math.Max(0, Math.Min(100, Progress));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //textBox1.Text = @"D:\test";
            //textBox2.Text = @"D:\test2";
            //textBox3.Text = "3000x2000";
        }


    }

    public class ImageFile
    {
        public string Path { get; set; }
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public bool IsDone { get; set; }
        public int SuccessStatus { get; set; }
    }


}
