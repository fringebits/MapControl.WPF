// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Xml;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MapControl
{
    using SimpleLogger;

    public class BingMapsTileLayer : TileLayer
    {
        public enum MapMode
        {
            Road,
            Aerial,
            AerialWithLabels
        }

        public BingMapsTileLayer()
        {
            this.MinZoomLevel = 1;
            this.MaxZoomLevel = 21;
            this.Loaded += this.OnLoaded;
        }

        public static string ApiKey { get; set; }
        public MapMode Mode { get; set; }
        public string Culture { get; set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.OnLoaded;

            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new InvalidOperationException("A Bing Maps API Key must be assigned to the ApiKey property.");
            }

            var uri = string.Format("http://dev.virtualearth.net/REST/V1/Imagery/Metadata/{0}?output=xml&key={1}", this.Mode, ApiKey);
            var request = WebRequest.CreateHttp(uri);

            request.BeginGetResponse(this.HandleImageryMetadataResponse, request);
        }

        private void HandleImageryMetadataResponse(IAsyncResult asyncResult)
        {
            try
            {
                var request = (HttpWebRequest)asyncResult.AsyncState;

                using (var response = request.EndGetResponse(asyncResult))
                using (var xmlReader = XmlReader.Create(response.GetResponseStream()))
                {
                    this.ReadImageryMetadataResponse(xmlReader);
                }
            }
            catch (System.Net.WebException ex)
            {
                Logger.Log(ex);
                this.TileSource = new BingMapsTileSource();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void ReadImageryMetadataResponse(XmlReader xmlReader)
        {
            string logoUri = null;
            string imageUrl = null;
            string[] imageUrlSubdomains = null;
            int? zoomMin = null;
            int? zoomMax = null;

            do
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "BrandLogoUri":
                            logoUri = xmlReader.ReadElementContentAsString();
                            break;
                        case "ImageUrl":
                            imageUrl = xmlReader.ReadElementContentAsString();
                            break;
                        case "ImageUrlSubdomains":
                            imageUrlSubdomains = ReadStrings(xmlReader.ReadSubtree());
                            break;
                        case "ZoomMin":
                            zoomMin = xmlReader.ReadElementContentAsInt();
                            break;
                        case "ZoomMax":
                            zoomMax = xmlReader.ReadElementContentAsInt();
                            break;
                        default:
                            xmlReader.Read();
                            break;
                    }
                }
                else
                {
                    xmlReader.Read();
                }
            }
            while (xmlReader.NodeType != XmlNodeType.None);

            if (!string.IsNullOrEmpty(imageUrl) && imageUrlSubdomains != null && imageUrlSubdomains.Length > 0)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (string.IsNullOrEmpty(this.Culture))
                    {
                        this.Culture = CultureInfo.CurrentUICulture.Name;
                    }

                    // THIS IS THE TILESOURCE SETUP ###
                    this.TileSource = new BingMapsTileSource(imageUrl.Replace("{culture}", this.Culture), imageUrlSubdomains);

                    if (zoomMin.HasValue && zoomMin.Value > this.MinZoomLevel)
                    {
                        this.MinZoomLevel = zoomMin.Value;
                    }

                    if (zoomMax.HasValue && zoomMax.Value < this.MaxZoomLevel)
                    {
                        this.MaxZoomLevel = zoomMax.Value;
                    }

                    if (!string.IsNullOrEmpty(logoUri))
                    {
                        this.LogoImage = new BitmapImage(new Uri(logoUri));
                    }
                }));
            }
        }

        private static string[] ReadStrings(XmlReader xmlReader)
        {
            var strings = new List<string>();

            do
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "string")
                {
                    strings.Add(xmlReader.ReadElementContentAsString());
                }
                else
                {
                    xmlReader.Read();
                }
            }
            while (xmlReader.NodeType != XmlNodeType.None);

            return strings.ToArray();
        }
    }
}
