namespace MapControl
{
    using System.Windows.Threading;

    public interface ITileLayer
    {
        /// <summary>
        /// Provides map tile URIs or images.
        /// </summary>
        TileSource TileSource { get; set; }

        /// <summary>
        /// Name of the TileSource. Used as key in a TileLayerCollection and as component of a tile cache key.
        /// </summary>
        string SourceName { get; set; }

        /// <summary>
        /// Description of the TileLayer. Used to display copyright information on top of the map.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Minimum zoom level supported by the TileLayer.
        /// </summary>
        int MinZoomLevel { get; set; }

        /// <summary>
        /// Maximum zoom level supported by the TileLayer.
        /// </summary>
        int MaxZoomLevel { get; set; }

        /// <summary>
        /// Maximum number of parallel downloads that may be performed by the TileLayer's ITileImageLoader.
        /// </summary>
        int MaxParallelDownloads { get; set; }

        Dispatcher Dispatcher { get; }
    }
}
