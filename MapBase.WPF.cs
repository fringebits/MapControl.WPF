// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using MapCore;
    using Point = System.Windows.Point;

    public partial class MapBase
    {
        public static readonly DependencyProperty ForegroundProperty =
            Control.ForegroundProperty.AddOwner(typeof(MapBase));

        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(
            "Center", typeof(Location), typeof(MapBase), new FrameworkPropertyMetadata(
                new Location(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).CenterPropertyChanged((Location)e.NewValue)));

        public static readonly DependencyProperty TargetCenterProperty = DependencyProperty.Register(
            "TargetCenter", typeof(Location), typeof(MapBase), new FrameworkPropertyMetadata(
                new Location(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).TargetCenterPropertyChanged((Location)e.NewValue)));

        public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register(
            "ZoomLevel", typeof(double), typeof(MapBase), new FrameworkPropertyMetadata(
                1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).ZoomLevelPropertyChanged((double)e.NewValue)));

        public static readonly DependencyProperty TargetZoomLevelProperty = DependencyProperty.Register(
            "TargetZoomLevel", typeof(double), typeof(MapBase), new FrameworkPropertyMetadata(
                1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).TargetZoomLevelPropertyChanged((double)e.NewValue)));

        public static readonly DependencyProperty HeadingProperty = DependencyProperty.Register(
            "Heading", typeof(double), typeof(MapBase), new FrameworkPropertyMetadata(
                0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).HeadingPropertyChanged((double)e.NewValue)));

        public static readonly DependencyProperty TargetHeadingProperty = DependencyProperty.Register(
            "TargetHeading", typeof(double), typeof(MapBase), new FrameworkPropertyMetadata(
                0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).TargetHeadingPropertyChanged((double)e.NewValue)));

        static MapBase()
        {
            ClipToBoundsProperty.OverrideMetadata(
                typeof(MapBase), new FrameworkPropertyMetadata(true));

            BackgroundProperty.OverrideMetadata(
                typeof(MapBase), new FrameworkPropertyMetadata(Brushes.Transparent));
        }

        partial void RemoveAnimation(DependencyProperty property)
        {
            this.BeginAnimation(property, null);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.ResetTransformOrigin();
            this.UpdateTransform();
        }

        private void SetViewportTransform(Point mapOrigin)
        {
            var transform = Matrix.Identity;
            transform.Translate(-mapOrigin.X, -mapOrigin.Y);
            transform.Scale(this.ViewportScale, -this.ViewportScale);
            transform.Rotate(this.Heading);
            transform.Translate(this.viewportOrigin.X, this.viewportOrigin.Y);

            this.viewportTransform.Matrix = transform;
        }

        private void SetTileLayerTransform()
        {
            var scale = Math.Pow(2d, this.ZoomLevel - this.TileZoomLevel);
            var transform = Matrix.Identity;
            transform.Translate(this.TileGrid.X * TileSource.TileSize, this.TileGrid.Y * TileSource.TileSize);
            transform.Scale(scale, scale);
            transform.Translate(this.tileLayerOffset.X, this.tileLayerOffset.Y);
            transform.RotateAt(this.Heading, this.viewportOrigin.X, this.viewportOrigin.Y);

            this.tileLayerTransform.Matrix = transform;
        }

        private void SetTransformMatrixes()
        {
            var rotateMatrix = Matrix.Identity;
            rotateMatrix.Rotate(this.Heading);
            this.rotateTransform.Matrix = rotateMatrix;

            var scaleMatrix = Matrix.Identity;
            scaleMatrix.Scale(this.CenterScale, this.CenterScale);
            this.scaleTransform.Matrix = scaleMatrix;

            this.scaleRotateTransform.Matrix = scaleMatrix * rotateMatrix;
        }

        private Matrix GetTileIndexMatrix(double scale)
        {
            var transform = this.viewportTransform.Matrix;
            transform.Invert(); // view to map coordinates
            transform.Translate(180d, -180d);
            transform.Scale(scale, -scale); // map coordinates to tile indices

            return transform;
        }
    }
}
