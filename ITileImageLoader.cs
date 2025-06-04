// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;

namespace MapControl
{
    public interface ITileImageLoader
    {
        int NumPendingTiles { get; }

        void BeginLoadTiles(ITileLayer tileLayer, IEnumerable<Tile> tiles);

        void CancelLoadTiles(ITileLayer tileLayer);
    }
}
