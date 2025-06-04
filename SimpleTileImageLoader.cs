// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using SimpleLogger;

    /// <summary>
    /// Loads map tile images and optionally caches them in a System.Runtime.Caching.ObjectCache.
    /// </summary>
    public class SimpleTileImageLoader : ITileImageLoader
    {
        /// <summary>
        /// Default name of an ObjectCache instance that is assigned to the Cache property.
        /// </summary>
        public const string DefaultCacheName = "TileCache";

        /// <summary>
        /// Default expiration time for cached tile images. Used when no expiration time
        /// was transmitted on download. The default and recommended minimum value is seven days.
        /// See OpenStreetMap tile usage policy: http://wiki.openstreetmap.org/wiki/Tile_usage_policy
        /// </summary>
        public static TimeSpan DefaultCacheExpiration = TimeSpan.FromDays(14);

        /// <summary>
        /// The ObjectCache used to cache tile images. The default is MemoryCache.Default.
        /// </summary>
        public static ObjectCache Cache = MemoryCache.Default;

        /// <summary>
        /// Optional value to be used for the HttpWebRequest.UserAgent property. The default is null.
        /// </summary>
        public static string HttpUserAgent;

        private class PendingTile
        {
            public readonly Tile Tile;
            public readonly ImageSource CachedImage;

            public PendingTile(Tile tile, ImageSource cachedImage)
            {
                this.Tile = tile;
                this.CachedImage = cachedImage;
            }
        }

        private readonly ConcurrentQueue<PendingTile> pendingTiles = new ConcurrentQueue<PendingTile>();

        private int taskCount;

        public int NumPendingTiles => 0;

        public void BeginLoadTiles(ITileLayer tileLayer, IEnumerable<Tile> tiles)
        {
            if (tiles.Any())
            {
                // get current TileLayer property values in UI thread
                var tileSource = tileLayer.TileSource;
                var imageTileSource = tileSource as ImageTileSource;

                foreach (var tile in tiles)
                {
                    var image = LoadImage(imageTileSource, tile);
                    tile.SetImage(image);
                }
            }
        }

        public void CancelLoadTiles(ITileLayer tileLayer)
        {
        }

        private void GetTiles(IEnumerable<Tile> tiles, Dispatcher dispatcher, TileSource tileSource, string sourceName, int maxDownloads)
        {
            var useCache = Cache != null
                && !string.IsNullOrEmpty(sourceName)
                && !(tileSource is ImageTileSource)
                && !tileSource.UriFormat.StartsWith("file:");

            foreach (var tile in tiles)
            {
                BitmapSource cachedImage = null;

                bool requestUpdatedTile = false;

                if (useCache)
                {
                    var ret = GetCachedImage(CacheKey(sourceName, tile), out cachedImage);
                    requestUpdatedTile = !ret;
                    if (cachedImage != null)
                    {
                        dispatcher.BeginInvoke(new Action<Tile, ImageSource>((t, i) => t.SetImage(i)), tile, cachedImage);
                    }
                }

                if (requestUpdatedTile)
                {
                    Logger.Log($"Requesting updated tile {tile}");
                    this.pendingTiles.Enqueue(new PendingTile(tile, cachedImage));
                }
            }

            var newTaskCount = Math.Min(this.pendingTiles.Count, maxDownloads) - this.taskCount;

            while (newTaskCount-- > 0)
            {
                Interlocked.Increment(ref this.taskCount);

                this.LoadPendingTiles(dispatcher, tileSource, sourceName);

                //Task.Run(() => this.LoadPendingTiles(dispatcher, tileSource, sourceName));
            }
        }

        private void LoadPendingTiles(Dispatcher dispatcher, TileSource tileSource, string sourceName)
        {
            var imageTileSource = tileSource as ImageTileSource;
            PendingTile pendingTile;

            while (this.pendingTiles.TryDequeue(out pendingTile))
            {
                var tile = pendingTile.Tile;
                var cacheKey = CacheKey(sourceName, tile);
                ImageSource image = null;

                Logger.Log($"LoadPendingTile: {tile}, Key={cacheKey}");

                if (imageTileSource != null)
                {
                    image = LoadImage(imageTileSource, tile);
                }
                else
                {
                    var uri = tileSource.GetUri(tile.XIndex, tile.Y, tile.ZoomLevel);

                    if (uri != null)
                    {
                        if (!uri.IsAbsoluteUri)
                        {
                            image = LoadImage(uri.OriginalString);
                        }
                        else if (uri.Scheme == "file")
                        {
                            image = LoadImage(uri.LocalPath);
                        }
                        else
                        {
                            image = DownloadImage(uri, cacheKey);

                            if (image == null)
                            {
                                Logger.Log($"Using CachedImage for tile {cacheKey}.");
                                image = pendingTile.CachedImage;
                            }
                        }
                    }
                }

                if (image != null)
                {
                    dispatcher.BeginInvoke(new Action<Tile, ImageSource>((t, i) => t.SetImage(i)), tile, image);
                }
                else
                {
                    tile.SetImage(null);
                }
            }

            Interlocked.Decrement(ref this.taskCount);
        }

        private static ImageSource LoadImage(ImageTileSource tileSource, Tile tile)
        {
            try
            {
                return tileSource.LoadImage(tile.XIndex, tile.Y, tile.ZoomLevel);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(Logger.Level.Error, $"Loading tile image failed {tile}");
            }

            return null;
        }

        private static ImageSource LoadImage(string path)
        {
            ImageSource image = null;

            if (File.Exists(path))
            {
                try
                {
                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        image = BitmapFrame.Create(fileStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    Logger.Log(Logger.Level.Error, $"Creating tile image failed path={path}");
                }
            }

            return image;
        }

        private static ImageSource DownloadImage(Uri uri, string cacheKey)
        {
            BitmapSource image = null;

            try
            {
                var request = WebRequest.CreateHttp(uri);
                request.UserAgent = HttpUserAgent;

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        responseStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        image = BitmapFrame.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }

                    Logger.Log($"DownloadImage: {cacheKey} from {uri}");
                    if (cacheKey != null)
                    {
                        SetCachedImage(cacheKey, image, GetExpiration(response.Headers));
                    }
                }
            }
            catch (WebException ex)
            {
                Logger.Log(ex);
                Logger.Log(Logger.Level.Error, $"Downloading {uri} failed: {ex.Status}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(Logger.Level.Error, $"Downloading {uri} failed: {ex.Message}");
            }

            return image;
        }

        private static string TileKey(TileSource tileSource, Tile tile)
        {
            return $"{tileSource.GetHashCode():X}/{tile.ZoomLevel:X}/{tile.XIndex:X}/{tile.Y:X}";
        }

        private static string CacheKey(string sourceName, Tile tile)
        {
            if (!string.IsNullOrEmpty(sourceName))
            {
                return $"{sourceName}/{tile.ZoomLevel}/{tile.XIndex}/{tile.Y}";
            }

            return null;
        }

        /// <summary>
        /// Try to get cached image with specified cacheKey. 
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="image"></param>
        /// <returns>Returns true if cached image was successuflly loaded, false otherwise.</returns>
        private static bool GetCachedImage(string cacheKey, out BitmapSource image)
        {
            image = Cache.Get(cacheKey) as BitmapSource;

            if (image == null)
            {
                Logger.Log($"CachedImage {cacheKey} is null.");
                return false;
            }

            var metadata = (BitmapMetadata)image.Metadata;

            // get cache expiration date from BitmapMetadata.DateTaken, must be parsed with CurrentCulture
            if (metadata == null)
            {
                // No metadata, ok to use cached image
                Logger.Log($"CachedImage {cacheKey} has no metadata.");
                return true;
            }

            if (metadata.DateTaken == null)
            {
                Logger.Log($"CachedImage {cacheKey} has no DateTaken field.");
                return true;
            }

            if (!DateTime.TryParse(
                metadata.DateTaken, CultureInfo.CurrentCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var expiration))
            {
                Logger.Log($"CachedImage {cacheKey} could not parse DateTaken field.");
                return true;
            }

            if (expiration > DateTime.UtcNow)
            {
                return true;
            }

            Logger.Log($"CachedImage {cacheKey} has expired on {expiration}.");
            return false;
        }

        private static void SetCachedImage(string cacheKey, BitmapSource image, DateTime expiration)
        {
            Logger.Log($"SetCachedImage: {cacheKey}, Expires {expiration}");

            var bitmap = BitmapFrame.Create(image);
            var metadata = (BitmapMetadata)bitmap.Metadata;

            // store cache expiration date in BitmapMetadata.DateTaken
            metadata.DateTaken = expiration.ToString(CultureInfo.InvariantCulture);
            metadata.Freeze();
            bitmap.Freeze();
            
            Cache.Set(cacheKey, bitmap, new CacheItemPolicy { AbsoluteExpiration = expiration });
        }

        private static DateTime GetExpiration(WebHeaderCollection headers)
        {
            return DateTime.UtcNow.Add(DefaultCacheExpiration);

            /**
            var maxExpiration = DateTime.UtcNow.Add(DefaultCacheExpiration);

            var cacheControl = headers["Cache-Control"];
            int maxAge;
            DateTime expiration;

            // FIXME: use REGEX parser for something like .*?max-age=/d*  (was seeing: public, max-age=XYZ)
            if (cacheControl != null &&
                cacheControl.StartsWith("max-age=") &&
                int.TryParse(cacheControl.Substring(8), out maxAge))
            {
                maxAge = Math.Min(maxAge, (int)DefaultCacheExpiration.TotalSeconds);
                expiration = DateTime.UtcNow.AddSeconds(maxAge);
            }
            else
            {
                var expires = headers["Expires"];
                var maxExpiration = DateTime.UtcNow.Add(DefaultCacheExpiration);

                if (expires == null ||
                    !DateTime.TryParse(expires, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out expiration) ||
                    expiration > maxExpiration)
                {
                    expiration = maxExpiration;
                }
            }

            return expiration;
            /**/
        }
    }
}
