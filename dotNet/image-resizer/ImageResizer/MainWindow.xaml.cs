using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ImageResizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Variables

        private int scale;
        private string root;
        private string target;

        #endregion

        #region Event Handler

        private void btnRoot_Click(object sender, RoutedEventArgs e)
        {
            var diag = new FolderBrowserDialog();
            diag.RootFolder = Environment.SpecialFolder.MyComputer;
            diag.ShowNewFolderButton = false;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtRoot.Text = diag.SelectedPath;
            }
        }

        private void btnTarget_Click(object sender, RoutedEventArgs e)
        {
            var diag = new FolderBrowserDialog();
            diag.RootFolder = Environment.SpecialFolder.MyComputer;
            diag.ShowNewFolderButton = true;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtTarget.Text = diag.SelectedPath;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            // Bildschirm-Eingaben prüfen
            if (!checkInputs())
                return;

            lstOutput.Items.Clear();

            // Werte setzen
            this.scale = Int32.Parse(txtScale.Text);
            this.root = txtRoot.Text;
            this.target = txtTarget.Text;

            // Skalierung durchführen
            Thread t = new Thread(resizeAllImages);
            t.Start();
        }

        #endregion

        #region Private Methods

        private void addOutput(string message)
        {
            if (!CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, (MethodInvoker)delegate()
                {
                    addOutput(message);
                });
                return;
            }

            lstOutput.Items.Insert(0, message);
        }

        private bool checkInputs()
        {
            StringBuilder sb = new StringBuilder();

            // Textbox Quelle
            if (String.IsNullOrWhiteSpace(txtRoot.Text))
            {
                sb.AppendLine("     - Source must be provided!");
            }
            else if (!Directory.Exists(txtRoot.Text))
            {
                sb.AppendLine("     - Source '" + txtRoot.Text + "' invalid!");
            }

            // Textbox Ziel
            if (String.IsNullOrWhiteSpace(txtTarget.Text))
            {
                sb.AppendLine("     - Target must be provided!");
            }
            else if (!Directory.Exists(txtTarget.Text))
            {
                sb.AppendLine("     - Target '" + txtTarget.Text + "' invalid!");
            }

            // Skalierung
            int scale = 0;
            if (String.IsNullOrWhiteSpace(txtScale.Text))
            {
                sb.AppendLine("     - Scaling must be provided!");
            }
            else if (!Int32.TryParse(txtScale.Text, out scale))
            {
                sb.AppendLine("     - Scaling must be an integer numeric value!");
            }

            if (sb.Length > 0)
            {
                System.Windows.MessageBox.Show(sb.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private void resizeAllImages()
        {
            addOutput("Starting...");

            var supportedFormats = new List<string>() { ".jpg", ".jpeg", ".bmp", ".png", ".gif", ".tiff" };
            int fileCounter = 0;

            var files = Directory.GetFiles(this.root, "*", SearchOption.AllDirectories);

            Parallel.ForEach(files, file =>
                {
                    if (supportedFormats.Contains(Path.GetExtension(file.ToLower())))
                    {
                        loadAndConvertImage(file, this.scale);

                        Interlocked.Increment(ref fileCounter);
                    }
                });

            addOutput("Successfully done (" + fileCounter + " files scaled)");
        }

        private void loadAndConvertImage(string filePath, int scale)
        {
            addOutput("       " + filePath);

            // get file size
            var fInfo = new FileInfo(filePath);
            var fileSize = (Int32)fInfo.Length;

            using (var fs = File.OpenRead(filePath))
            {
                using (var img = Image.FromStream(fs))
                {
                    var imgResized = resizeImage(img, scale);

                    ImageFormat format;
                    var extension = Path.GetExtension(filePath.ToLower());

                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            format = ImageFormat.Jpeg;
                            break;

                        case ".bmp":
                            format = ImageFormat.Bmp;
                            break;

                        case ".png":
                            format = ImageFormat.Png;
                            break;

                        case ".gif":
                            format = ImageFormat.Gif;
                            break;

                        case ".tiff":
                            format = ImageFormat.Tiff;
                            break;

                        default:
                            return;
                    }

                    string filename = Path.GetFileName(filePath);
                    string targetPath = Path.Combine(this.target, filename);

                    saveImage(targetPath, imgResized, format);

                    imgResized = null;
                }
            }
        }

        private Image resizeImage(Image image, int scale)
        {
            int newHeight = (int)Math.Round(image.Height * (scale * .01));
            int newWidth = (int)Math.Round(image.Width * (scale * .01));

            Image newImage = new Bitmap(newWidth, newHeight);

            using (Graphics graphicsHandle = Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

        private void saveImage(string path, Image image, ImageFormat format)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                image.Save(fs, format);
            }
        }

        #endregion
    }
}
