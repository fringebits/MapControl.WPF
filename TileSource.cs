// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Globalization;

    using MapCore;

    /// <summary>
    /// Provides the URI of a map tile.
    /// </summary>
    public partial class TileSource
    {
        public const int TileSize = 256;
        public const double MetersPerDegree = 6378137d * Math.PI / 180d; // WGS 84 semi major axis

        private Func<int, int, int, Uri> getUri;
        private string uriFormat = string.Empty;

        public TileSource()
        {
        }

        protected TileSource(string uriFormat)
        {
            this.uriFormat = uriFormat;
        }

        public string UriFormat
        {
            get => this.uriFormat;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("The value of the UriFormat property must not be null or empty.");
                }

                this.uriFormat = value;

                if (this.uriFormat.Contains("{x}") && this.uriFormat.Contains("{y}") && this.uriFormat.Contains("{z}"))
                {
                    if (this.uriFormat.Contains("{c}"))
                    {
                        this.getUri = this.GetOpenStreetMapUri;
                    }
                    else if (this.uriFormat.Contains("{i}"))
                    {
                        this.getUri = this.GetGoogleMapsUri;
                    }
                    else if (this.uriFormat.Contains("{n}"))
                    {
                        this.getUri = this.GetMapQuestUri;
                    }
                    else
                    {
                        this.getUri = this.GetBasicUri;
                    }
                }
                else if (this.uriFormat.Contains("{q}")) // {i} is optional
                {
                    this.getUri = this.GetQuadKeyUri;
                }
                else if (this.uriFormat.Contains("{W}") && this.uriFormat.Contains("{S}") && this.uriFormat.Contains("{E}") && this.uriFormat.Contains("{N}"))
                {
                    this.getUri = this.GetBoundingBoxUri;
                }
                else if (this.uriFormat.Contains("{w}") && this.uriFormat.Contains("{s}") && this.uriFormat.Contains("{e}") && this.uriFormat.Contains("{n}"))
                {
                    this.getUri = this.GetLatLonBoundingBoxUri;
                }
                else if (this.uriFormat.Contains("{x}") && this.uriFormat.Contains("{v}") && this.uriFormat.Contains("{z}"))
                {
                    this.getUri = this.GetTmsUri;
                }
            }
        }

        public virtual Uri GetUri(int x, int y, int zoomLevel)
        {
            return this.getUri != null ? this.getUri(x, y, zoomLevel) : null;
        }

        internal Uri GetBasicUri(int x, int y, int zoomLevel)
        {
            return new Uri(this.uriFormat.
                Replace("{x}", x.ToString()).
                Replace("{y}", y.ToString()).
                Replace("{z}", zoomLevel.ToString()),
                UriKind.RelativeOrAbsolute);
        }

        private Uri GetOpenStreetMapUri(int x, int y, int zoomLevel)
        {
            var hostIndex = (x + y) % 3;

            return new Uri(this.uriFormat.
                Replace("{c}", "abc".Substring(hostIndex, 1)).
                Replace("{x}", x.ToString()).
                Replace("{y}", y.ToString()).
                Replace("{z}", zoomLevel.ToString()),
                UriKind.RelativeOrAbsolute);
        }

        private Uri GetGoogleMapsUri(int x, int y, int zoomLevel)
        {
            var hostIndex = (x + y) % 4;

            return new Uri(this.uriFormat.
                Replace("{i}", hostIndex.ToString()).
                Replace("{x}", x.ToString()).
                Replace("{y}", y.ToString()).
                Replace("{z}", zoomLevel.ToString()),
                UriKind.RelativeOrAbsolute);
        }

        private Uri GetMapQuestUri(int x, int y, int zoomLevel)
        {
            var hostIndex = (x + y) % 4 + 1;

            return new Uri(this.uriFormat.
                Replace("{n}", hostIndex.ToString()).
                Replace("{x}", x.ToString()).
                Replace("{y}", y.ToString()).
                Replace("{z}", zoomLevel.ToString()),
                UriKind.RelativeOrAbsolute);
        }

        private Uri GetTmsUri(int x, int y, int zoomLevel)
        {
            y = (1 << zoomLevel) - 1 - y;

            return new Uri(this.uriFormat.
                Replace("{x}", x.ToString()).
                Replace("{v}", y.ToString()).
                Replace("{z}", zoomLevel.ToString()),
                UriKind.RelativeOrAbsolute);
        }

        private Uri GetQuadKeyUri(int x, int y, int zoomLevel)
        {
            if (zoomLevel < 1)
            {
                return null;
            }

            var quadkey = new char[zoomLevel];

            for (var z = zoomLevel - 1; z >= 0; z--, x /= 2, y /= 2)
            {
                quadkey[z] = (char)('0' + 2 * (y % 2) + (x % 2));
            }

            return new Uri(this.uriFormat.
                Replace("{i}", new string(quadkey[zoomLevel - 1], 1)).
                Replace("{q}", new string(quadkey)),
                UriKind.RelativeOrAbsolute);
        }

        private Uri GetBoundingBoxUri(int x, int y, int zoomLevel)
        {
            var tileSize = 360d / (1 << zoomLevel); // tile width in degrees
            var west = MetersPerDegree * (x * tileSize - 180d);
            var east = MetersPerDegree * ((x + 1) * tileSize - 180d);
            var south = MetersPerDegree * (180d - (y + 1) * tileSize);
            var north = MetersPerDegree * (180d - y * tileSize);

            return new Uri(this.uriFormat.
                Replace("{W}", west.ToString(CultureInfo.InvariantCulture)).
                Replace("{S}", south.ToString(CultureInfo.InvariantCulture)).
                Replace("{E}", east.ToString(CultureInfo.InvariantCulture)).
                Replace("{N}", north.ToString(CultureInfo.InvariantCulture)));
        }

        private Uri GetLatLonBoundingBoxUri(int x, int y, int zoomLevel)
        {
            var tileSize = 360d / (1 << zoomLevel); // tile width in degrees
            var west = x * tileSize - 180d;
            var east = (x + 1) * tileSize - 180d;
            var south = MercatorTransform.YToLatitude(180d - (y + 1) * tileSize);
            var north = MercatorTransform.YToLatitude(180d - y * tileSize);

            return new Uri(this.uriFormat.
                Replace("{w}", west.ToString(CultureInfo.InvariantCulture)).
                Replace("{s}", south.ToString(CultureInfo.InvariantCulture)).
                Replace("{e}", east.ToString(CultureInfo.InvariantCulture)).
                Replace("{n}", north.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
