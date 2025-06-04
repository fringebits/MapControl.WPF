
// This just exists to add reference to some of the default "tile providers"

namespace MapControl
{
    public enum TileProviderType
    {
        OpenStreetMap = 0,
        RideWithGps,
        BingMaps,
        UsgsTopo,
        JuicyTrails,
        Seamark,
        MaxProviderTypes
    }

    public class TileProvider
    {
        static TileProvider()
        {
            // BingMapsTileLayer.ApiKey = Constants.BingApiKey;
        }

        public static T CreateTileLayer<T>(TileProviderType providerType) 
            where T : ITileLayer, new()
        {
            T result = default(T);

            switch (providerType)
            {
                case TileProviderType.OpenStreetMap:
                    result = new T() {
                        SourceName = "OpenStreetMap",
                        Description="Maps © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)",
                        TileSource = new TileSource { UriFormat = "http://{c}.tile.openstreetmap.org/{z}/{x}/{y}.png" },
                        MaxZoomLevel = 19
                    };
                    break;

                case TileProviderType.RideWithGps:
                    result = new T
                    {
                        SourceName = "RideWithGps",
                        Description = "Maps © [RideWithGps Contributors]",
                        TileSource = new TileSource { UriFormat = "http://tile-{c}.ridewithgps.com/rwgps/{z}/{x}/{y}.png" },
                        MaxZoomLevel = 19
                    };
                    break;

                //case TileProviderType.BingMaps:
                //    result = new BingMapsTileLayer {
                //        SourceName = "BingMaps",
                //        Description = "© [Microsoft Corporation](http://www.bing.com/maps/)",
                //        Mode = BingMapsTileLayer.MapMode.Road,
                //        MaxZoomLevel = 19
                //    };
                //    break;

                case TileProviderType.UsgsTopo:
                    result = new T {
                        SourceName = "UsgsTopo",
                        Description = "USGS Topo Maps",
                        TileSource = new TileSource { UriFormat = @"http://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}" },
                        MaxZoomLevel = 19
                    };
                    break;

                case TileProviderType.JuicyTrails:
                    // note: {c} -> uses 'abc', but juicy can use 'abcd'
                    result = new T
                    {
                        Description = "Juicy Trails",
                        TileSource = new TileSource { UriFormat = @"http://{c}.tile.juicytrails.com/juicy/{z}/{x}/{y}.png" },
                        MaxZoomLevel = 19
                    };
                    break;

                case TileProviderType.Seamark:
                    result = new T 
                    {
                        Description = "Seamarks",
                        TileSource = new TileSource { UriFormat = @"https://tiles.openseamap.org/seamark/{z}/{x}/{y}.png" },
                        MaxZoomLevel = 18
                    };
                    break;
            }

            if (result != null)
            {
                result.SourceName = providerType.ToString();
            }

            return result;
        }

    }
}


/**
 * 
 #	Result	Protocol	Host	URL	Body	Caching	Content-Type	Process	Comments	Custom	
345	200	HTTP	c.tile.juicytrails.com	/juicy/13/1661/3135.png	35,780	max-age=604800; Expires: Sun, 06 Sep 2015 17:16:01 GMT	image/png	chrome:4464			
* 

       <!--
        TileLayers with OpenStreetMap data.
        -->
                <map:TileLayer x:Key="OpenStreetMap" SourceName="OpenStreetMap"
                       Description="Maps © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://{c}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                       MaxZoomLevel="19"/>
 * 
                <map:TileLayer x:Key="OpenCycleMap" SourceName="Thunderforest OpenCycleMap"
                       Description="Maps © [Thunderforest](http://www.thunderforest.com/), Data © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://{c}.tile.thunderforest.com/cycle/{z}/{x}/{y}.png"/>
                <map:TileLayer x:Key="Landscape" SourceName="Thunderforest Landscape"
                       Description="Maps © [Thunderforest](http://www.thunderforest.com/), Data © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://{c}.tile.thunderforest.com/landscape/{z}/{x}/{y}.png"/>
                <map:TileLayer x:Key="Outdoors" SourceName="Thunderforest Outdoors"
                       Description="Maps © [Thunderforest](http://www.thunderforest.com/), Data © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://{c}.tile.thunderforest.com/outdoors/{z}/{x}/{y}.png"/>
                <map:TileLayer x:Key="Transport" SourceName="Thunderforest Transport"
                       Description="Maps © [Thunderforest](http://www.thunderforest.com/), Data © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://{c}.tile.thunderforest.com/transport/{z}/{x}/{y}.png"/>
                <map:TileLayer x:Key="TransportDark" SourceName="Thunderforest Transport Dark"
                       Description="Maps © [Thunderforest](http://www.thunderforest.com/), Data © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://{c}.tile.thunderforest.com/transport-dark/{z}/{x}/{y}.png"
                       Foreground="White" Background="Black"/>
                <map:TileLayer x:Key="MapQuest" SourceName="MapQuest OpenStreetMap"
                       Description="Maps © [MapQuest](http://www.mapquest.com/), Data © [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                       TileSource="http://otile{n}.mqcdn.com/tiles/1.0.0/osm/{z}/{x}/{y}.png"
                       MaxZoomLevel="19"/>
                <map:TileLayer x:Key="Seamarks" SourceName="Seamarks"
                       TileSource="http://tiles.openseamap.org/seamark/{z}/{x}/{y}.png"
                       MinZoomLevel="9" MaxZoomLevel="18"/>

                <!--
        Bing Maps TileLayers with tile URLs retrieved from the Imagery Metadata Service
        (see http://msdn.microsoft.com/en-us/library/ff701716.aspx).
        A Bing Maps API Key (see http://msdn.microsoft.com/en-us/library/ff428642.aspx) is required
        for using these layers and must be assigned to the static BingMapsTileLayer.ApiKey property.
        -->
                <map:BingMapsTileLayer x:Key="BingAerial" SourceName="Bing Maps Aerial"
                               Description="© [Microsoft Corporation](http://www.bing.com/maps/)"
                               FileMode="Aerial" MaxZoomLevel="19" Foreground="White" Background="Black"/>
                <map:BingMapsTileLayer x:Key="BingHybrid" SourceName="Bing Maps Hybrid"
                               Description="© [Microsoft Corporation](http://www.bing.com/maps/)"
                               FileMode="AerialWithLabels" MaxZoomLevel="19" Foreground="White" Background="Black"/>

/**/