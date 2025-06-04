// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace MapControl
{
    /// <summary>
    /// Fills a rectangular area with map tiles from a TileSource.
    /// </summary>
    [ContentProperty("TileSource")]
    public partial class TileLayer : PanelBase, IMapElement, ITileLayer
    {
        public static TileLayer Default
        {
            get
            {
                return new TileLayer
                {
                    SourceName = "OpenStreetMap",
                    Description = "© [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)",
                    TileSource = new TileSource { UriFormat = "http://{c}.tile.openstreetmap.org/{z}/{x}/{y}.png" }
                };
            }
        }

        public static readonly DependencyProperty TileSourceProperty = DependencyProperty.Register(
            "TileSource", typeof(TileSource), typeof(TileLayer),
            new PropertyMetadata(null, (o, e) => ((TileLayer)o).UpdateTiles(true)));

        public static readonly DependencyProperty SourceNameProperty = DependencyProperty.Register(
            "SourceName", typeof(string), typeof(TileLayer), new PropertyMetadata(null));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description", typeof(string), typeof(TileLayer), new PropertyMetadata(null));

        public static readonly DependencyProperty LogoImageProperty = DependencyProperty.Register(
            "LogoImage", typeof(ImageSource), typeof(TileLayer), new PropertyMetadata(null));

        public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty.Register(
            "MinZoomLevel", typeof(int), typeof(TileLayer), new PropertyMetadata(0));

        public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty.Register(
            "MaxZoomLevel", typeof(int), typeof(TileLayer), new PropertyMetadata(18));

        public static readonly DependencyProperty MaxParallelDownloadsProperty = DependencyProperty.Register(
            "MaxParallelDownloads", typeof(int), typeof(TileLayer), new PropertyMetadata(4));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof(Brush), typeof(TileLayer), new PropertyMetadata(null));

        public static readonly new DependencyProperty BackgroundProperty = DependencyProperty.Register(
            "Background", typeof(Brush), typeof(TileLayer), new PropertyMetadata(null));

        private readonly ITileImageLoader tileImageLoader;
        private List<Tile> tiles = new List<Tile>();
        private MapBase parentMap;

        public TileLayer()
            : this(new TileImageLoader())
        {
        }

        public TileLayer(ITileImageLoader tileImageLoader)
        {
            this.tileImageLoader = tileImageLoader;
            this.Initialize();
        }

        partial void Initialize();

        /// <summary>
        /// Provides map tile URIs or images.
        /// </summary>
        public TileSource TileSource
        {
            get { return (TileSource) this.GetValue(TileSourceProperty); }
            set { this.SetValue(TileSourceProperty, value); }
        }

        /// <summary>
        /// Name of the TileSource. Used as key in a TileLayerCollection and as component of a tile cache key.
        /// </summary>
        public string SourceName
        {
            get { return (string) this.GetValue(SourceNameProperty); }
            set { this.SetValue(SourceNameProperty, value); }
        }

        /// <summary>
        /// Description of the TileLayer. Used to display copyright information on top of the map.
        /// </summary>
        public string Description
        {
            get { return (string) this.GetValue(DescriptionProperty); }
            set { this.SetValue(DescriptionProperty, value); }
        }

        /// <summary>
        /// Logo image. Used to display a provider brand logo on top of the map.
        /// </summary>
        public ImageSource LogoImage
        {
            get { return (ImageSource) this.GetValue(LogoImageProperty); }
            set { this.SetValue(LogoImageProperty, value); }
        }

        /// <summary>
        /// Minimum zoom level supported by the TileLayer.
        /// </summary>
        public int MinZoomLevel
        {
            get { return (int) this.GetValue(MinZoomLevelProperty); }
            set { this.SetValue(MinZoomLevelProperty, value); }
        }

        /// <summary>
        /// Maximum zoom level supported by the TileLayer.
        /// </summary>
        public int MaxZoomLevel
        {
            get { return (int) this.GetValue(MaxZoomLevelProperty); }
            set { this.SetValue(MaxZoomLevelProperty, value); }
        }

        /// <summary>
        /// Maximum number of parallel downloads that may be performed by the TileLayer's ITileImageLoader.
        /// </summary>
        public int MaxParallelDownloads
        {
            get { return (int) this.GetValue(MaxParallelDownloadsProperty); }
            set { this.SetValue(MaxParallelDownloadsProperty, value); }
        }

        /// <summary>
        /// Optional foreground brush. Sets MapBase.Foreground, if not null.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush) this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Optional background brush. Sets MapBase.Background, if not null.
        /// New property prevents filling of RenderTransformed TileLayer with Panel.Background.
        /// </summary>
        public new Brush Background
        {
            get { return (Brush) this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public MapBase ParentMap
        {
            get { return this.parentMap; }
            set
            {
                if (this.parentMap != null)
                {
                    this.parentMap.TileGridChanged -= this.UpdateTiles;
                    this.ClearValue(RenderTransformProperty);
                }

                this.parentMap = value;

                if (this.parentMap != null)
                {
                    this.parentMap.TileGridChanged += this.UpdateTiles;
                    this.RenderTransform = this.parentMap.TileLayerTransform;
                }

                this.UpdateTiles();
            }
        }

        protected virtual void UpdateTiles(bool clearTiles = false)
        {
            if (this.tiles.Count > 0)
            {
                this.tileImageLoader.CancelLoadTiles(this);
            }

            if (clearTiles)
            {
                this.tiles.Clear();
            }

            this.SelectTiles();

            this.Children.Clear();

            if (this.tiles.Count > 0)
            {
                foreach (var tile in this.tiles)
                {
                    this.Children.Add(tile.Image);
                }

                this.tileImageLoader.BeginLoadTiles(this, this.tiles.Where(t => t.Pending));
            }
        }

        private void UpdateTiles(object sender, EventArgs e)
        {
            this.UpdateTiles();
        }

        private void SelectTiles()
        {
            var newTiles = new List<Tile>();

            if (this.parentMap != null && this.TileSource != null)
            {
                var grid = this.parentMap.TileGrid;
                var zoomLevel = this.parentMap.TileZoomLevel;
                var maxZoomLevel = Math.Min(zoomLevel, this.MaxZoomLevel);
                var minZoomLevel = this.MinZoomLevel;

                if (minZoomLevel < maxZoomLevel && this != this.parentMap.TileLayers.FirstOrDefault())
                {
                    // do not load background tiles if this is not the base layer
                    minZoomLevel = maxZoomLevel;
                }

                for (var z = minZoomLevel; z <= maxZoomLevel; z++)
                {
                    var tileSize = 1 << (zoomLevel - z);
                    var x1 = (int)Math.Floor((double)grid.X / tileSize); // may be negative
                    var x2 = (grid.X + grid.Width - 1) / tileSize;
                    var y1 = Math.Max(grid.Y / tileSize, 0);
                    var y2 = Math.Min((grid.Y + grid.Height - 1) / tileSize, (1 << z) - 1);

                    for (var y = y1; y <= y2; y++)
                    {
                        for (var x = x1; x <= x2; x++)
                        {
                            var tile = this.tiles.FirstOrDefault(t => t.ZoomLevel == z && t.X == x && t.Y == y);

                            if (tile == null)
                            {
                                tile = new Tile(z, x, y);

                                var equivalentTile = this.tiles.FirstOrDefault(
                                    t => t.ZoomLevel == z && t.XIndex == tile.XIndex && t.Y == y && t.Image.Source != null);

                                if (equivalentTile != null)
                                {
                                    // do not animate to avoid flicker when crossing 180°
                                    tile.SetImage(equivalentTile.Image.Source, false);
                                }
                            }

                            newTiles.Add(tile);
                        }
                    }
                }
            }

            this.tiles = newTiles;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.parentMap != null)
            {
                foreach (var tile in this.tiles)
                {
                    var tileSize = (double)(256 << (this.parentMap.TileZoomLevel - tile.ZoomLevel));
                    var x = tileSize * tile.X - 256 * this.parentMap.TileGrid.X;
                    var y = tileSize * tile.Y - 256 * this.parentMap.TileGrid.Y;

                    tile.Image.Width = tileSize;
                    tile.Image.Height = tileSize;
                    tile.Image.Arrange(new Rect(x, y, tileSize, tileSize));
                }
            }

            return finalSize;
        }
    }
}
