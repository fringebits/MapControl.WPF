// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Windows.Controls;

    public partial class Tile
    {
        public static TimeSpan OpacityAnimationDuration = TimeSpan.FromSeconds(0.3);

        public readonly int ZoomLevel;
        public readonly int X;
        public readonly int Y;
        public readonly Image Image = new Image { Opacity = 0d };

        public Tile(int zoomLevel, int x, int y)
        {
            this.ZoomLevel = zoomLevel;
            this.X = x;
            this.Y = y;
            this.Pending = true;
        }

        public bool Pending { get; private set; }

        public int XIndex
        {
            get
            {
                var numTiles = 1 << this.ZoomLevel;
                return ((this.X % numTiles) + numTiles) % numTiles;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y}, Z={ZoomLevel})";
        }
    }
}
