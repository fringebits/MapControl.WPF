// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Windows;
using System.Windows.Media;

namespace MapControl
{
    /// <summary>
    /// Base class for map overlays with font, background, foreground and stroke properties.
    /// </summary>
    public partial class MapOverlay : MapPanel
    {
        public double FontSize
        {
            get { return (double) this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
        }

        public FontFamily FontFamily
        {
            get { return (FontFamily) this.GetValue(FontFamilyProperty); }
            set { this.SetValue(FontFamilyProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle) this.GetValue(FontStyleProperty); }
            set { this.SetValue(FontStyleProperty, value); }
        }

        public FontStretch FontStretch
        {
            get { return (FontStretch) this.GetValue(FontStretchProperty); }
            set { this.SetValue(FontStretchProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return (FontWeight) this.GetValue(FontWeightProperty); }
            set { this.SetValue(FontWeightProperty, value); }
        }

        public Brush Foreground
        {
            get { return (Brush) this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush) this.GetValue(StrokeProperty); }
            set { this.SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double) this.GetValue(StrokeThicknessProperty); }
            set { this.SetValue(StrokeThicknessProperty, value); }
        }

        public DoubleCollection StrokeDashArray
        {
            get { return (DoubleCollection) this.GetValue(StrokeDashArrayProperty); }
            set { this.SetValue(StrokeDashArrayProperty, value); }
        }

        public double StrokeDashOffset
        {
            get { return (double) this.GetValue(StrokeDashOffsetProperty); }
            set { this.SetValue(StrokeDashOffsetProperty, value); }
        }

        public PenLineCap StrokeDashCap
        {
            get { return (PenLineCap) this.GetValue(StrokeDashCapProperty); }
            set { this.SetValue(StrokeDashCapProperty, value); }
        }

        public PenLineCap StrokeStartLineCap
        {
            get { return (PenLineCap) this.GetValue(StrokeStartLineCapProperty); }
            set { this.SetValue(StrokeStartLineCapProperty, value); }
        }

        public PenLineCap StrokeEndLineCap
        {
            get { return (PenLineCap) this.GetValue(StrokeEndLineCapProperty); }
            set { this.SetValue(StrokeEndLineCapProperty, value); }
        }

        public PenLineJoin StrokeLineJoin
        {
            get { return (PenLineJoin) this.GetValue(StrokeLineJoinProperty); }
            set { this.SetValue(StrokeLineJoinProperty, value); }
        }

        public double StrokeMiterLimit
        {
            get { return (double) this.GetValue(StrokeMiterLimitProperty); }
            set { this.SetValue(StrokeMiterLimitProperty, value); }
        }
    }
}
