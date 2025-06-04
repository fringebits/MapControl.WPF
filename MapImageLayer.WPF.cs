// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapControl
{
    public partial class MapImageLayer
    {
        private void ImageUpdated(BitmapSource bitmap)
        {
            if (bitmap != null && !bitmap.IsFrozen && bitmap.IsDownloading)
            {
                bitmap.DownloadCompleted += this.BitmapDownloadCompleted;
                bitmap.DownloadFailed += this.BitmapDownloadFailed;
            }
            else
            {
                this.SwapImages();
            }
        }

        private void BitmapDownloadCompleted(object sender, EventArgs e)
        {
            var bitmap = (BitmapSource)sender;
            bitmap.DownloadCompleted -= this.BitmapDownloadCompleted;
            bitmap.DownloadFailed -= this.BitmapDownloadFailed;

            this.SwapImages();
        }

        private void BitmapDownloadFailed(object sender, ExceptionEventArgs e)
        {
            var bitmap = (BitmapSource)sender;
            bitmap.DownloadCompleted -= this.BitmapDownloadCompleted;
            bitmap.DownloadFailed -= this.BitmapDownloadFailed;

            var mapImage = (MapImage) this.Children[this.currentImageIndex];
            mapImage.Source = null;

            this.SwapImages();
        }
    }
}
