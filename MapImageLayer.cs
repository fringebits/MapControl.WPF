// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using MapCore;
    using Point = System.Windows.Point;

    /// <summary>
    /// Map image overlay. Fills the entire viewport with map images provided by a web service,
    /// e.g. a Web Map Service (WMS). The image request Uri is specified by the UriFormat property.
    /// </summary>
    public partial class MapImageLayer : MapPanel
    {
        public static readonly DependencyProperty UriFormatProperty = DependencyProperty.Register(
            "UriFormat", typeof(string), typeof(MapImageLayer),
            new PropertyMetadata(null, (o, e) => ((MapImageLayer)o).UpdateImage()));

        public static readonly DependencyProperty RelativeImageSizeProperty = DependencyProperty.Register(
            "RelativeImageSize", typeof(double), typeof(MapImageLayer), new PropertyMetadata(1d));

        private readonly DispatcherTimer updateTimer;
        private int currentImageIndex;
        private bool updateInProgress;

        public MapImageLayer()
        {
            this.Children.Add(new MapImage { Opacity = 0d });
            this.Children.Add(new MapImage { Opacity = 0d });

            this.updateTimer = new DispatcherTimer { Interval = MapBase.TileUpdateInterval };
            this.updateTimer.Tick += (s, e) => this.UpdateImage();
        }

        /// <summary>
        /// The format string of the image request Uri. The format must contain
        /// {X} and {Y} format specifiers for the map width and height in pixels and either
        /// {w},{s},{e},{n} for the bounding box in lat/lon (like EPSG:4326) or
        /// {W},{S},{E},{N} for the bounding box in meters (like EPSG:3857).
        /// </summary>
        public string UriFormat
        {
            get { return (string) this.GetValue(UriFormatProperty); }
            set { this.SetValue(UriFormatProperty, value); }
        }

        /// <summary>
        /// Relative size of the map images in relation to the current viewport size.
        /// Setting a value greater than one will let MapImageLayer request images that
        /// are larger than the viewport, in order to support smooth panning.
        /// </summary>
        public double RelativeImageSize
        {
            get { return (double) this.GetValue(RelativeImageSizeProperty); }
            set { this.SetValue(RelativeImageSizeProperty, value); }
        }

        protected override void OnViewportChanged()
        {
            base.OnViewportChanged();

            this.updateTimer.Stop();
            this.updateTimer.Start();
        }

        protected void UpdateImage()
        {
            this.updateTimer.Stop();

            if (this.updateInProgress)
            {
                this.updateTimer.Start(); // update image on next timer tick
            }
            else if (this.ParentMap != null && this.RenderSize.Width > 0 && this.RenderSize.Height > 0)
            {
                this.updateInProgress = true;

                var relativeSize = Math.Max(this.RelativeImageSize, 1d);
                var width = this.RenderSize.Width * relativeSize;
                var height = this.RenderSize.Height * relativeSize;
                var dx = (this.RenderSize.Width - width) / 2d;
                var dy = (this.RenderSize.Height - height) / 2d;

                var loc1 = this.ParentMap.ViewportPointToLocation(new Point(dx, dy));
                var loc2 = this.ParentMap.ViewportPointToLocation(new Point(dx + width, dy));
                var loc3 = this.ParentMap.ViewportPointToLocation(new Point(dx, dy + height));
                var loc4 = this.ParentMap.ViewportPointToLocation(new Point(dx + width, dy + height));

                var west = Math.Min(loc1.Longitude, Math.Min(loc2.Longitude, Math.Min(loc3.Longitude, loc4.Longitude)));
                var east = Math.Max(loc1.Longitude, Math.Max(loc2.Longitude, Math.Max(loc3.Longitude, loc4.Longitude)));
                var south = Math.Min(loc1.Latitude, Math.Min(loc2.Latitude, Math.Min(loc3.Latitude, loc4.Latitude)));
                var north = Math.Max(loc1.Latitude, Math.Max(loc2.Latitude, Math.Max(loc3.Latitude, loc4.Latitude)));

                var p1 = this.ParentMap.MapTransform.Transform(new Location(south, west));
                var p2 = this.ParentMap.MapTransform.Transform(new Location(north, east));

                width = Math.Round((p2.X - p1.X) * this.ParentMap.ViewportScale);
                height = Math.Round((p2.Y - p1.Y) * this.ParentMap.ViewportScale);

                this.UpdateImage(west, east, south, north, (int)width, (int)height);
            }
        }

        protected virtual void UpdateImage(double west, double east, double south, double north, int width, int height)
        {
            if (this.UriFormat != null && width > 0 && height > 0)
            {
                var uri = this.UriFormat.Replace("{X}", width.ToString()).Replace("{Y}", height.ToString());

                if (uri.Contains("{W}") && uri.Contains("{S}") && uri.Contains("{E}") && uri.Contains("{N}"))
                {
                    var p1 = this.ParentMap.MapTransform.Transform(new Location(south, west));
                    var p2 = this.ParentMap.MapTransform.Transform(new Location(north, east));

                    uri = uri.
                        Replace("{W}", (TileSource.MetersPerDegree * p1.X).ToString(CultureInfo.InvariantCulture)).
                        Replace("{S}", (TileSource.MetersPerDegree * p1.Y).ToString(CultureInfo.InvariantCulture)).
                        Replace("{E}", (TileSource.MetersPerDegree * p2.X).ToString(CultureInfo.InvariantCulture)).
                        Replace("{N}", (TileSource.MetersPerDegree * p2.Y).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    uri = uri.
                        Replace("{w}", west.ToString(CultureInfo.InvariantCulture)).
                        Replace("{s}", south.ToString(CultureInfo.InvariantCulture)).
                        Replace("{e}", east.ToString(CultureInfo.InvariantCulture)).
                        Replace("{n}", north.ToString(CultureInfo.InvariantCulture));
                }

                this.UpdateImage(west, east, south, north, new Uri(uri));
            }
            else
            {
                this.UpdateImage(west, east, south, north, (BitmapSource)null);
            }
        }

        protected virtual void UpdateImage(double west, double east, double south, double north, Uri uri)
        {
            this.UpdateImage(west, east, south, north, new BitmapImage(uri));
        }

        protected void UpdateImage(double west, double east, double south, double north, BitmapSource bitmap)
        {
            this.currentImageIndex = (this.currentImageIndex + 1) % 2;
            var mapImage = (MapImage) this.Children[this.currentImageIndex];

            mapImage.SetBoundingBox(west, east, south, north);
            mapImage.Source = bitmap;

            this.ImageUpdated(bitmap);
        }

        private void SwapImages()
        {
            var topImage = (MapImage) this.Children[this.currentImageIndex];
            var bottomImage = (MapImage) this.Children[(this.currentImageIndex + 1) % 2];

            SetZIndex(topImage, 1);
            SetZIndex(bottomImage, 0);

            if (topImage.Source != null)
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0d,
                    To = 1d,
                    Duration = Tile.OpacityAnimationDuration,
                    FillBehavior = FillBehavior.Stop
                };

                fadeAnimation.Completed += (s, e) => bottomImage.Opacity = 0d;

                topImage.BeginAnimation(OpacityProperty, fadeAnimation);
                topImage.Opacity = 1d;
            }
            else
            {
                topImage.Opacity = 0d;
                bottomImage.Opacity = 0d;
            }

            this.updateInProgress = false;
        }
    }
}
