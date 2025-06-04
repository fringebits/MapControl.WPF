// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using MapCore;

    using Rect = System.Windows.Rect;

    public partial class MapGraticule
    {
        private class Label
        {
            public readonly double Position;
            public readonly string Text;

            public Label(double position, string text)
            {
                this.Position = position;
                this.Text = text;
            }
        }

        private readonly Dictionary<string, GlyphRun> glyphRuns = new Dictionary<string, GlyphRun>();

        static MapGraticule()
        {
            IsHitTestVisibleProperty.OverrideMetadata(
                typeof(MapGraticule), new FrameworkPropertyMetadata(false));

            StrokeThicknessProperty.OverrideMetadata(
                typeof(MapGraticule), new FrameworkPropertyMetadata(0.5, (o, e) => ((MapGraticule)o).glyphRuns.Clear()));
        }

        protected override void OnViewportChanged()
        {
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.ParentMap != null)
            {
                var bounds = this.ParentMap.ViewportTransform.Inverse.TransformBounds(new Rect(this.ParentMap.RenderSize));
                var start = this.ParentMap.MapTransform.Transform(new Helix.CoreTypes.Point(bounds.X, bounds.Y));
                var end = this.ParentMap.MapTransform.Transform(new Helix.CoreTypes.Point(bounds.X + bounds.Width, bounds.Y + bounds.Height));
                var minSpacing = this.MinLineSpacing * 360d / (Math.Pow(2d, this.ParentMap.ZoomLevel) * TileSource.TileSize);
                var spacing = LineSpacings[LineSpacings.Length - 1];

                if (spacing >= minSpacing)
                {
                    spacing = LineSpacings.FirstOrDefault(s => s >= minSpacing);
                }

                var labelFormat = spacing < 1d ? "{0} {1}°{2:00}'" : "{0} {1}°";
                var labelStart = new Location(
                    Math.Ceiling(start.Latitude / spacing) * spacing,
                    Math.Ceiling(start.Longitude / spacing) * spacing);

                var latLabels = new List<Label>((int)((end.Latitude - labelStart.Latitude) / spacing) + 1);
                var lonLabels = new List<Label>((int)((end.Longitude - labelStart.Longitude) / spacing) + 1);

                for (var lat = labelStart.Latitude; lat <= end.Latitude; lat += spacing)
                {
                    latLabels.Add(new Label(lat, CoordinateString(lat, labelFormat, "NS")));

                    drawingContext.DrawLine(this.Pen, this.ParentMap.LocationToViewportPoint(new Location(lat, start.Longitude)), this.ParentMap.LocationToViewportPoint(new Location(lat, end.Longitude)));
                }

                for (var lon = labelStart.Longitude; lon <= end.Longitude; lon += spacing)
                {
                    lonLabels.Add(new Label(lon, CoordinateString(Location.NormalizeLongitude(lon), labelFormat, "EW")));

                    drawingContext.DrawLine(this.Pen, this.ParentMap.LocationToViewportPoint(new Location(start.Latitude, lon)), this.ParentMap.LocationToViewportPoint(new Location(end.Latitude, lon)));
                }

                if (this.Foreground != null && this.Foreground != Brushes.Transparent && latLabels.Count > 0 && lonLabels.Count > 0)
                {
                    var latLabelOrigin = new Point(this.StrokeThickness / 2d + 2d, -this.StrokeThickness / 2d - this.FontSize / 4d);
                    var lonLabelOrigin = new Point(this.StrokeThickness / 2d + 2d, this.StrokeThickness / 2d + this.FontSize);
                    var transform = Matrix.Identity;
                    transform.Rotate(this.ParentMap.Heading);

                    foreach (var latLabel in latLabels)
                    {
                        foreach (var lonLabel in lonLabels)
                        {
                            GlyphRun latGlyphRun;
                            GlyphRun lonGlyphRun;

                            if (!this.glyphRuns.TryGetValue(latLabel.Text, out latGlyphRun))
                            {
                                latGlyphRun = GlyphRunText.Create(latLabel.Text, this.Typeface, this.FontSize, latLabelOrigin);
                                this.glyphRuns.Add(latLabel.Text, latGlyphRun);
                            }

                            if (!this.glyphRuns.TryGetValue(lonLabel.Text, out lonGlyphRun))
                            {
                                lonGlyphRun = GlyphRunText.Create(lonLabel.Text, this.Typeface, this.FontSize, lonLabelOrigin);
                                this.glyphRuns.Add(lonLabel.Text, lonGlyphRun);
                            }

                            var position = this.ParentMap.LocationToViewportPoint(new Location(latLabel.Position, lonLabel.Position));

                            drawingContext.PushTransform(new MatrixTransform(
                                transform.M11, transform.M12, transform.M21, transform.M22, position.X, position.Y));

                            drawingContext.DrawGlyphRun(this.Foreground, latGlyphRun);
                            drawingContext.DrawGlyphRun(this.Foreground, lonGlyphRun);
                            drawingContext.Pop();
                        }
                    }

                    var removeKeys = this.glyphRuns.Keys.Where(k => !latLabels.Any(l => l.Text == k) && !lonLabels.Any(l => l.Text == k));

                    foreach (var key in removeKeys.ToList())
                    {
                        this.glyphRuns.Remove(key);
                    }
                }
                else
                {
                    this.glyphRuns.Clear();
                }
            }
        }
    }
}
