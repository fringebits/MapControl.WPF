// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

namespace MapControl
{
    using System.Windows.Media;

    internal class BingMapsTileSource : TileSource
    {
        private readonly string[] subdomains;

        private readonly bool offline;

        public BingMapsTileSource(string uriFormat, string[] subdomains)
        {
            this.UriFormat = uriFormat;
            this.subdomains = subdomains;
        }

        public BingMapsTileSource()
            : base("http://tiles.openseamap.org/seamark/{z}/{x}/{y}.png")
        {
            this.subdomains = new[] { "1" };
            this.offline = true;
        }

        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            if (this.offline)
            {
                return null;
            }

            if (zoomLevel < 1)
            {
                return null;
            }

            var subdomain = this.subdomains[(x + y) % this.subdomains.Length];
            var quadkey = new char[zoomLevel];

            for (var z = zoomLevel - 1; z >= 0; z--, x /= 2, y /= 2)
            {
                quadkey[z] = (char)('0' + 2 * (y % 2) + (x % 2));
            }

            return new Uri(this.UriFormat.
                Replace("{subdomain}", subdomain).
                Replace("{quadkey}", new string(quadkey)));
        }
    }
}
