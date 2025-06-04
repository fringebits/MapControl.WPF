// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    public partial class Tile
    {
        public void SetImage(ImageSource image, bool animateOpacity = true)
        {
            this.Pending = false;

            if (image != null)
            {
                if (animateOpacity && OpacityAnimationDuration > TimeSpan.Zero)
                {
                    var bitmap = image as BitmapSource;

                    if (bitmap != null && !bitmap.IsFrozen && bitmap.IsDownloading)
                    {
                        bitmap.DownloadCompleted += this.BitmapDownloadCompleted;
                        bitmap.DownloadFailed += this.BitmapDownloadFailed;
                    }
                    else
                    {
                        this.Image.BeginAnimation(UIElement.OpacityProperty,
                            new DoubleAnimation(0d, 1d, OpacityAnimationDuration));
                    }
                }
                else
                {
                    this.Image.Opacity = 1d;
                }

                this.Image.Source = image;
            }
        }

        private void BitmapDownloadCompleted(object sender, EventArgs e)
        {
            var bitmap = (BitmapSource)sender;

            bitmap.DownloadCompleted -= this.BitmapDownloadCompleted;
            bitmap.DownloadFailed -= this.BitmapDownloadFailed;

            this.Image.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0d, 1d, OpacityAnimationDuration));
        }

        private void BitmapDownloadFailed(object sender, ExceptionEventArgs e)
        {
            var bitmap = (BitmapSource)sender;

            bitmap.DownloadCompleted -= this.BitmapDownloadCompleted;
            bitmap.DownloadFailed -= this.BitmapDownloadFailed;

            this.Image.Source = null;
        }
    }
}
