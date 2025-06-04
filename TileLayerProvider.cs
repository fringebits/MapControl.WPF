// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using MapCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Threading;    

    /// <summary>
    /// Fills a rectangular area with map tiles from a TileSource.
    /// </summary>
    public partial class TileLayerProvider : ITileLayer
    {
        private readonly ITileImageLoader tileImageLoader;

        private List<Tile> tiles = new List<Tile>();

        public TileLayerProvider()
            : this(new SimpleTileImageLoader())
        {
        }

        public TileLayerProvider(ITileImageLoader tileImageLoader)
        {
            this.tileImageLoader = tileImageLoader;
            this.Initialize();
        }

        partial void Initialize();

        /// <summary>
        /// Provides map tile URIs or images.
        /// </summary>
        public TileSource TileSource { get; set; }

        /// <summary>
        /// Name of the TileSource. Used as key in a TileLayerCollection and as component of a tile cache key.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Description of the TileLayer. Used to display copyright information on top of the map.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Minimum zoom level supported by the TileLayer.
        /// </summary>
        public int MinZoomLevel { get; set; }

        /// <summary>
        /// Maximum zoom level supported by the TileLayer.
        /// </summary>
        public int MaxZoomLevel { get; set; }

        /// <summary>
        /// Maximum number of parallel downloads that may be performed by the TileLayer's ITileImageLoader.
        /// </summary>
        public int MaxParallelDownloads { get; set; }

        public Dispatcher Dispatcher => System.Windows.Threading.Dispatcher.CurrentDispatcher;

        public int NumPendingTiles => this.tileImageLoader.NumPendingTiles;

        // #TODO: create a version of this that replaces ZOOM with an output dimension (and chooses an appropriate zoom)
        public void UpdateTiles(Rectangle rect, int zoom)
        {
            var newTiles = this.SelectTiles(rect, zoom);

            this.LoadTiles(newTiles);
        }

        public void LoadTiles(List<Tile> tilesToLoad)
        {
            if (this.tiles.Count > 0)
            {
                this.tileImageLoader.CancelLoadTiles(this);
            }

            this.tiles.Clear();

            this.tiles.AddRange(tilesToLoad);

            if (this.tiles.Count > 0)
            {
                this.tileImageLoader.BeginLoadTiles(this, this.tiles.Where(t => t.Pending));
            }
        }

        public List<Tile> SelectTiles(Rectangle rect, int zoom = 0)
        {
            var result = new List<Tile>();

            // given a rect + zoom, get the tiles
            var tl = TileMath.PositionToTileXY(rect.TopLeft, zoom, TileSource.TileSize);
            var br = TileMath.PositionToTileXY(rect.BottomRight, zoom, TileSource.TileSize);

            for (var x = tl.X; x <= br.X; x++)
            {
                for (var y = tl.Y; y <= br.Y; y++)
                {
                    var tile = new Tile(zoom, x, y);
                    result.Add(tile);
                }
            }

            return result;
        }
    }
}
