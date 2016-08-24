using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Halftonify
{
    public partial class MainWindow : Window
    {
        private Bitmap inputBitmap = null;
        private Bitmap outputBitmap = null;

        // TODO: Make these configurable in the window
        private const int TILE_SIZE = 18;
        private const float RADIUS_MAX_FACTOR = 0.75f;
        private const float RADIUS_MIN_FACTOR = 0.15f;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "PNG Image Files (*.png)|*.png";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == true)
            {
                FilepathLabel.Content = dialog.FileName;
                inputBitmap = new Bitmap(dialog.FileName);
                outputBitmap = new Bitmap(dialog.FileName);
                InputImage.Source = LoadBitmap(inputBitmap);
            }
            else
            {
                inputBitmap = null;
                outputBitmap = null;
                InputImage.Source = null;
            }
        }

        private void Halftonify_Click(object sender, RoutedEventArgs e)
        {
            if (inputBitmap != null &&
                outputBitmap != null)
            {
                outputBitmap = CreateNonIndexedImage(outputBitmap);

                ClearOutput();

                int tilesX = inputBitmap.Width / TILE_SIZE + 1;
                int tilesY = inputBitmap.Height / TILE_SIZE + 1;

                Graphics outputGraphics = Graphics.FromImage(outputBitmap);
                Font arial = new Font("Arial", 8);

                for (int tileY = 0; tileY < tilesY; tileY++)
                {
                    int startPixelY = tileY * TILE_SIZE;
                    int tileSizeY = Math.Min(TILE_SIZE, inputBitmap.Height - startPixelY);

                    outputGraphics.DrawLine(Pens.Gray, 0.0f, startPixelY + tileSizeY / 2.0f, outputBitmap.Width, startPixelY + tileSizeY / 2.0f);
                    outputGraphics.DrawString((tilesY - (tileY + 1)).ToString(), arial, Brushes.Gray, 0.0f, startPixelY);
                    outputGraphics.DrawString((tilesY - (tileY + 1)).ToString(), arial, Brushes.Gray, outputBitmap.Width - tileSizeY, startPixelY); // TODO: Fix hax
                }

                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    int startPixelX = tileX * TILE_SIZE;
                    int tileSizeX = Math.Min(TILE_SIZE, inputBitmap.Width - startPixelX);

                    outputGraphics.DrawLine(Pens.Gray, startPixelX + tileSizeX / 2.0f, 0.0f, startPixelX + tileSizeX / 2.0f, outputBitmap.Height);
                    outputGraphics.DrawString((tileX + 0).ToString(), arial, Brushes.Gray, startPixelX, 0.0f);
                    outputGraphics.DrawString((tileX + 0).ToString(), arial, Brushes.Gray, startPixelX, outputBitmap.Height - tileSizeX); // TODO: Fix hax

                    for (int tileY = 0; tileY < tilesY; tileY++)
                    {
                        int startPixelY = tileY * TILE_SIZE;
                        int tileSizeY = Math.Min(TILE_SIZE, inputBitmap.Height - startPixelY);
                        float darkness = GetTileDarkness(startPixelX, startPixelY, tileSizeX, tileSizeY);

                        float radiusInitial = RADIUS_MAX_FACTOR * darkness * TILE_SIZE / 2.0f;
                        RadiusInfo radiusFinal = GetSteppedRadius(radiusInitial, RADIUS_MIN_FACTOR * TILE_SIZE / 2.0f, RADIUS_MAX_FACTOR * TILE_SIZE / 2.0f);

                        // TODO: Change size based on darkness.
                        //workingGraphics.FillEllipse(Brushes.Black, startPixelX, startPixelY, tileSizeX, tileSizeY);
                        outputGraphics.DrawCircle(radiusFinal.BrushColor, startPixelX + tileSizeX / 2.0f, startPixelY + tileSizeY / 2.0f, radiusFinal.Radius);
                    }
                }

                OutputImage.Source = LoadBitmap(outputBitmap);
            }
        }

        private struct RadiusInfo
        {
            public float Radius;
            public Brush BrushColor;

            public RadiusInfo(float radius, Brush brushColor)
            {
                Radius = radius;
                BrushColor = brushColor;
            }
        }

        private RadiusInfo GetSteppedRadius(float value, float min, float max)
        {
            // TODO: Support configurable number of steps.
            float step1 = min + ((max - min) / 5);
            float step2 = min + (2 * (max - min) / 5);
            float step3 = min + (3 * (max - min) / 5);
            float step4 = min + (4 * (max - min) / 5);

            if (value < min)
            {
                return new RadiusInfo(0.0f, Brushes.White);
            }
            else if (value >= min && value < step1)
            {
                return new RadiusInfo(0.15f * max, Brushes.Red);
            }
            else if (value >= step1 && value < step2)
            {
                return new RadiusInfo(0.3f * max, Brushes.Green);
            }
            else if (value >= step2 && value < step3)
            {
                return new RadiusInfo(0.45f * max, Brushes.Blue);
            }
            else if (value >= step3 && value < step4)
            {
                return new RadiusInfo(0.6f * max, Brushes.Yellow);
            }
            else // if (value > step4)
            {
                return new RadiusInfo(0.75f * max, Brushes.Black);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (outputBitmap != null)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.FileName = "Image";
                dialog.DefaultExt = ".png";
                dialog.Filter = "PNG Image Files (*.png)|*.png";

                if (dialog.ShowDialog() == true)
                {
                    outputBitmap.Save(dialog.FileName);
                }
            }
        }

        private void ClearOutput()
        {
            for (int x = 0; x < outputBitmap.Width; x++)
            {
                for (int y = 0; y < outputBitmap.Height; y++)
                {
                    outputBitmap.SetPixel(x, y, Color.White);
                }
            }
        }

        private float GetTileDarkness(int startX, int startY, int sizeX, int sizeY)
        {
            int channelCount = 0;
            int totalBrightness = 0;

            for (int x = startX; x < startX + sizeX; x++)
            {
                for (int y = startY; y < startY + sizeY; y++)
                {
                    Color pixel = inputBitmap.GetPixel(x, y);
                    totalBrightness += pixel.R;
                    totalBrightness += pixel.G;
                    totalBrightness += pixel.B;
                    channelCount += 3;
                }
            }

            float averageBrightness = (float)totalBrightness / channelCount;
            averageBrightness /= 255.0f;

            return 1.0f - averageBrightness;
        }

        public Bitmap CreateNonIndexedImage(Image sourceImage)
        {
            Bitmap nonIndexedBitmap = new Bitmap(sourceImage.Width, sourceImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(nonIndexedBitmap))
            {
                graphics.DrawImage(sourceImage, 0, 0);
            }

            return nonIndexedBitmap;
        }

        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        private static BitmapSource LoadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr hBitmap = source.GetHbitmap();
            BitmapSource bitmapSource = null;
            try
            {
                bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return bitmapSource;
        }
    }
}
