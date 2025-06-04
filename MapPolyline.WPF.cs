// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    public partial class MapPolyline
    {
        public static readonly DependencyProperty FillRuleProperty = StreamGeometry.FillRuleProperty.AddOwner(
            typeof(MapPolyline), new FrameworkPropertyMetadata(
                (o, e) => ((StreamGeometry)((MapPolyline)o).Data).FillRule = (FillRule)e.NewValue));

        public MapPolyline()
        {
            this.Data = new StreamGeometry();
        }

        protected override void UpdateData()
        {
            var geometry = (StreamGeometry) this.Data;

            if (this.ParentMap != null && this.Locations != null && this.Locations.Any())
            {
                using (var context = geometry.Open())
                {
                    var points = this.Locations.Select(l => this.ParentMap.MapTransform.Transform(l)).Select(p => new Point(p.X, p.Y));

                    context.BeginFigure(points.First(), this.IsClosed, this.IsClosed);
                    context.PolyLineTo(points.Skip(1).ToList(), true, true);
                }

                geometry.Transform = this.ParentMap.ViewportTransform;
            }
            else
            {
                geometry.Clear();
                geometry.ClearValue(Geometry.TransformProperty);
            }
        }
    }
}
