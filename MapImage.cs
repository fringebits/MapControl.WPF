// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Windows;
using System.Windows.Media;

namespace MapControl
{
    /// <summary>
    /// Fills a rectangular area with an ImageBrush from the Source property.
    /// </summary>
    public class MapImage : MapRectangle
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(ImageSource), typeof(MapImage),
            new PropertyMetadata(null, (o, e) => ((ImageBrush)((MapImage)o).Fill).ImageSource = (ImageSource)e.NewValue));

        public MapImage()
        {
            this.Fill = new ImageBrush
            {
                RelativeTransform = new MatrixTransform
                {
                    Matrix = new Matrix(1d, 0d, 0d, -1d, 0d, 1d)
                }
            };
        }

        public ImageSource Source
        {
            get { return (ImageSource) this.GetValue(SourceProperty); }
            set { this.SetValue(SourceProperty, value); }
        }
    }
}
