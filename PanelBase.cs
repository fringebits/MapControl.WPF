// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Windows;
using System.Windows.Controls;

namespace MapControl
{
    /// <summary>
    /// Common base class for MapPanel and TileLayer.
    /// </summary>
    public class PanelBase : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (UIElement element in Children)
            {
                element.Measure(availableSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement element in Children)
            {
                element.Arrange(new Rect(new Point(), finalSize));
            }

            return finalSize;
        }
    }
}
