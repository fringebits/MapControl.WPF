// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapControl
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
    using MapCore;

    /// <summary>
    /// The map control. Draws map content provided by the TileLayers or the TileLayer property.
    /// The visible map area is defined by the Center and ZoomLevel properties. The map can be rotated
    /// by an angle that is given by the Heading property.
    /// MapBase is a MapPanel and hence can contain map overlays like other MapPanels or MapItemsControls.
    /// </summary>
    public partial class MapBase : MapPanel
    {
        private const double MaximumZoomLevel = 22d;

        public static double ZoomLevelSwitchDelta = 0d;
        public static bool UpdateTilesWhileViewportChanging = true;
        public static TimeSpan TileUpdateInterval = TimeSpan.FromSeconds(0.5);
        public static TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.3);
        public static EasingFunctionBase AnimationEasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        public static readonly DependencyProperty TileLayerProperty = DependencyProperty.Register(
            "TileLayer", typeof(TileLayer), typeof(MapBase), new PropertyMetadata(null,
                (o, e) => ((MapBase)o).TileLayerPropertyChanged((TileLayer)e.NewValue)));

        public static readonly DependencyProperty TileLayersProperty = DependencyProperty.Register(
            "TileLayers", typeof(IList<TileLayer>), typeof(MapBase), new PropertyMetadata(null,
                (o, e) => ((MapBase)o).TileLayersPropertyChanged((IList<TileLayer>)e.OldValue, (IList<TileLayer>)e.NewValue)));

        public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty.Register(
            "MinZoomLevel", typeof(double), typeof(MapBase), new PropertyMetadata(1d,
                (o, e) => ((MapBase)o).MinZoomLevelPropertyChanged((double)e.NewValue)));

        public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty.Register(
            "MaxZoomLevel", typeof(double), typeof(MapBase), new PropertyMetadata(18d,
                (o, e) => ((MapBase)o).MaxZoomLevelPropertyChanged((double)e.NewValue)));

        internal static readonly DependencyProperty CenterPointProperty = DependencyProperty.Register(
            "CenterPoint", typeof(Point), typeof(MapBase), new PropertyMetadata(new Point(),
                (o, e) => ((MapBase)o).CenterPointPropertyChanged((Point)e.NewValue)));

        private readonly PanelBase tileLayerPanel = new PanelBase();
        private readonly DispatcherTimer tileUpdateTimer = new DispatcherTimer { Interval = TileUpdateInterval };
        private readonly MapTransform mapTransform = new MercatorTransform();
        private readonly MatrixTransform viewportTransform = new MatrixTransform();
        private readonly MatrixTransform tileLayerTransform = new MatrixTransform();
        private readonly MatrixTransform scaleTransform = new MatrixTransform();
        private readonly MatrixTransform rotateTransform = new MatrixTransform();
        private readonly MatrixTransform scaleRotateTransform = new MatrixTransform();

        private Location transformOrigin;
        private Point viewportOrigin;
        private Point tileLayerOffset;
        private PointAnimation centerAnimation;
        private DoubleAnimation zoomLevelAnimation;
        private DoubleAnimation headingAnimation;
        private bool internalPropertyChange;

        public MapBase()
        {
            this.Children.Add(this.tileLayerPanel);
            this.TileLayers = new ObservableCollection<TileLayer>();

            this.tileUpdateTimer.Tick += this.UpdateTiles;
            this.Loaded += this.MapLoaded;

            this.Initialize();
        }

        partial void Initialize();
        partial void RemoveAnimation(DependencyProperty property);

        /// <summary>
        /// Raised when the current viewport has changed.
        /// </summary>
        public event EventHandler ViewportChanged;

        /// <summary>
        /// Raised when the TileZoomLevel or TileGrid properties have changed.
        /// </summary>
        public event EventHandler TileGridChanged;

        /// <summary>
        /// Gets or sets the map foreground Brush.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush) this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the base TileLayer used by the Map control.
        /// </summary>
        public TileLayer TileLayer
        {
            get { return (TileLayer) this.GetValue(TileLayerProperty); }
            set { this.SetValue(TileLayerProperty, value); }
        }

        /// <summary>
        /// Gets or sets optional multiple TileLayers that are used simultaneously.
        /// The first element in the collection is equal to the value of the TileLayer property.
        /// The additional TileLayers usually have transparent backgrounds and their IsOverlay
        /// property is set to true.
        /// </summary>
        public IList<TileLayer> TileLayers
        {
            get { return (IList<TileLayer>) this.GetValue(TileLayersProperty); }
            set { this.SetValue(TileLayersProperty, value); }
        }

        /// <summary>
        /// Gets or sets the location of the center point of the Map.
        /// </summary>
        public Location Center
        {
            get { return (Location) this.GetValue(CenterProperty); }
            set { this.SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the target value of a Center animation.
        /// </summary>
        public Location TargetCenter
        {
            get { return (Location) this.GetValue(TargetCenterProperty); }
            set { this.SetValue(TargetCenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum value of the ZoomLevel and TargetZommLevel properties.
        /// Must be greater than or equal to zero and less than or equal to MaxZoomLevel.
        /// </summary>
        public double MinZoomLevel
        {
            get { return (double) this.GetValue(MinZoomLevelProperty); }
            set { this.SetValue(MinZoomLevelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum value of the ZoomLevel and TargetZommLevel properties.
        /// Must be greater than or equal to MinZoomLevel and less than or equal to 20.
        /// </summary>
        public double MaxZoomLevel
        {
            get { return (double) this.GetValue(MaxZoomLevelProperty); }
            set { this.SetValue(MaxZoomLevelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the map zoom level.
        /// </summary>
        public double ZoomLevel
        {
            get { return (double) this.GetValue(ZoomLevelProperty); }
            set { this.SetValue(ZoomLevelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the target value of a ZoomLevel animation.
        /// </summary>
        public double TargetZoomLevel
        {
            get { return (double) this.GetValue(TargetZoomLevelProperty); }
            set { this.SetValue(TargetZoomLevelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the map heading, or clockwise rotation angle in degrees.
        /// </summary>
        public double Heading
        {
            get { return (double) this.GetValue(HeadingProperty); }
            set { this.SetValue(HeadingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the target value of a Heading animation.
        /// </summary>
        public double TargetHeading
        {
            get { return (double) this.GetValue(TargetHeadingProperty); }
            set { this.SetValue(TargetHeadingProperty, value); }
        }

        /// <summary>
        /// Gets the transformation from geographic coordinates to cartesian map coordinates.
        /// </summary>
        public MapTransform MapTransform
        {
            get { return this.mapTransform; }
        }

        /// <summary>
        /// Gets the transformation from cartesian map coordinates to viewport coordinates.
        /// </summary>
        public Transform ViewportTransform
        {
            get { return this.viewportTransform; }
        }

        /// <summary>
        /// Gets the RenderTransform to be used by TileLayers, with origin at TileGrid.X and TileGrid.Y.
        /// </summary>
        public Transform TileLayerTransform
        {
            get { return this.tileLayerTransform; }
        }

        /// <summary>
        /// Gets the scaling transformation from meters to viewport coordinate units (pixels) at the Center location.
        /// </summary>
        public Transform ScaleTransform
        {
            get { return this.scaleTransform; }
        }

        /// <summary>
        /// Gets the transformation that rotates by the value of the Heading property.
        /// </summary>
        public Transform RotateTransform
        {
            get { return this.rotateTransform; }
        }

        /// <summary>
        /// Gets the combination of ScaleTransform and RotateTransform
        /// </summary>
        public Transform ScaleRotateTransform
        {
            get { return this.scaleRotateTransform; }
        }

        /// <summary>
        /// Gets the scaling factor from cartesian map coordinates to viewport coordinates.
        /// </summary>
        public double ViewportScale { get; private set; }

        /// <summary>
        /// Gets the scaling factor from meters to viewport coordinate units (pixels) at the Center location.
        /// </summary>
        public double CenterScale { get; private set; }

        /// <summary>
        /// Gets the zoom level to be used by TileLayers.
        /// </summary>
        public int TileZoomLevel { get; private set; }

        /// <summary>
        /// Gets the tile grid to be used by TileLayers.
        /// </summary>
        public Int32Rect TileGrid { get; private set; }

        /// <summary>
        /// Gets the map scale at the specified location as viewport coordinate units (pixels) per meter.
        /// </summary>
        public double GetMapScale(Location location)
        {
            return this.mapTransform.RelativeScale(location) * Math.Pow(2d, this.ZoomLevel) * TileSource.TileSize / (TileSource.MetersPerDegree * 360d);
        }

        /// <summary>
        /// Transforms a geographic location to a viewport coordinates point.
        /// </summary>
        public Point LocationToViewportPoint(Location location)
        {
            var p = this.mapTransform.Transform(location);
            return this.viewportTransform.Transform(new Point(p.X, p.Y));
        }

        /// <summary>
        /// Transforms a viewport coordinates point to a geographic location.
        /// </summary>
        public Location ViewportPointToLocation(Point point)
        {
            var p = this.viewportTransform.Inverse.Transform(point);
            return this.mapTransform.Transform(new Nutron.CoreTypes.Point(p.X, p.Y));
        }

        /// <summary>
        /// Transforms a viewport coordinates point to a geographic location.
        /// </summary>
        public Location ViewportPointToLocation(Nutron.CoreTypes.Point point)
        {
            return ViewportPointToLocation(point.ToSystemPoint());
        }

        /// <summary>
        /// Sets a temporary origin location in geographic coordinates for scaling and rotation transformations.
        /// This origin location is automatically removed when the Center property is set by application code.
        /// </summary>
        public void SetTransformOrigin(Location origin)
        {
            this.transformOrigin = origin;
            this.viewportOrigin = this.LocationToViewportPoint(origin);
        }

        /// <summary>
        /// Sets a temporary origin point in viewport coordinates for scaling and rotation transformations.
        /// This origin point is automatically removed when the Center property is set by application code.
        /// </summary>
        public void SetTransformOrigin(Point origin)
        {
            this.viewportOrigin.X = Math.Min(Math.Max(origin.X, 0d), this.RenderSize.Width);
            this.viewportOrigin.Y = Math.Min(Math.Max(origin.Y, 0d), this.RenderSize.Height);
            this.transformOrigin = this.ViewportPointToLocation(this.viewportOrigin);
        }

        /// <summary>
        /// Removes the temporary transform origin point set by SetTransformOrigin.
        /// </summary>
        public void ResetTransformOrigin()
        {
            this.transformOrigin = null;
            this.viewportOrigin = new Point(this.RenderSize.Width / 2d, this.RenderSize.Height / 2d);
        }

        /// <summary>
        /// Changes the Center property according to the specified translation in viewport coordinates.
        /// </summary>
        public void TranslateMap(Point translation)
        {
            if (this.transformOrigin != null)
            {
                this.ResetTransformOrigin();
            }

            if (translation.X != 0d || translation.Y != 0d)
            {
                this.Center = this.ViewportPointToLocation(new Point(this.viewportOrigin.X - translation.X, this.viewportOrigin.Y - translation.Y));
            }
        }

        /// <summary>
        /// Changes the Center, Heading and ZoomLevel properties according to the specified
        /// viewport coordinate translation, rotation and scale delta values. Rotation and scaling
        /// is performed relative to the specified origin point in viewport coordinates.
        /// </summary>
        public void TransformMap(Point origin, Point translation, double rotation, double scale)
        {
            this.SetTransformOrigin(origin);

            this.viewportOrigin.X += translation.X;
            this.viewportOrigin.Y += translation.Y;

            if (rotation != 0d)
            {
                var heading = (((this.Heading + rotation) % 360d) + 360d) % 360d;
                this.InternalSetValue(HeadingProperty, heading);
                this.InternalSetValue(TargetHeadingProperty, heading);
            }

            if (scale != 1d)
            {
                var zoomLevel = Math.Min(Math.Max(this.ZoomLevel + Math.Log(scale, 2d), this.MinZoomLevel), this.MaxZoomLevel);
                this.InternalSetValue(ZoomLevelProperty, zoomLevel);
                this.InternalSetValue(TargetZoomLevelProperty, zoomLevel);
            }

            this.UpdateTransform(true);
        }

        /// <summary>
        /// Sets the value of the TargetZoomLevel property while retaining the specified origin point
        /// in viewport coordinates.
        /// </summary>
        public void ZoomMap(Point origin, double zoomLevel)
        {
            if (zoomLevel >= this.MinZoomLevel && zoomLevel <= this.MaxZoomLevel)
            {
                this.SetTransformOrigin(origin);
                this.TargetZoomLevel = zoomLevel;
            }
        }

        /// <summary>
        /// Sets the TargetZoomLevel and TargetCenter properties such that the specified bounding box
        /// fits into the current viewport. The TargetHeading property is set to zero.
        /// </summary>
        public void ZoomToBounds(Location southWest, Location northEast)
        {
            if (southWest.Latitude < northEast.Latitude && southWest.Longitude < northEast.Longitude)
            {
                var p1 = this.mapTransform.Transform(southWest);
                var p2 = this.mapTransform.Transform(northEast);
                var lonScale = this.RenderSize.Width / (p2.X - p1.X) * 360d / TileSource.TileSize;
                var latScale = this.RenderSize.Height / (p2.Y - p1.Y) * 360d / TileSource.TileSize;
                var lonZoom = Math.Log(lonScale, 2d);
                var latZoom = Math.Log(latScale, 2d);

                this.TargetZoomLevel = Math.Min(lonZoom, latZoom);
                this.TargetCenter = this.mapTransform.Transform(new Nutron.CoreTypes.Point((p1.X + p2.X) / 2d, (p1.Y + p2.Y) / 2d));
                this.TargetHeading = 0d;
            }
        }

        protected override void OnViewportChanged()
        {
            base.OnViewportChanged();

            var viewportChanged = this.ViewportChanged;

            if (viewportChanged != null)
            {
                viewportChanged(this, EventArgs.Empty);
            }
        }

        private void MapLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.MapLoaded;

            if (this.tileLayerPanel.Children.Count == 0 && !this.Children.OfType<TileLayer>().Any())
            {
                this.TileLayer = TileLayer.Default;
            }
        }

        private void TileLayerPropertyChanged(TileLayer tileLayer)
        {
            if (tileLayer != null)
            {
                if (this.TileLayers == null)
                {
                    this.TileLayers = new ObservableCollection<TileLayer>(new TileLayer[] { tileLayer });
                }
                else if (this.TileLayers.Count == 0)
                {
                    this.TileLayers.Add(tileLayer);
                }
                else if (this.TileLayers[0] != tileLayer)
                {
                    this.TileLayers[0] = tileLayer;
                }
            }
        }

        private void TileLayersPropertyChanged(IList<TileLayer> oldTileLayers, IList<TileLayer> newTileLayers)
        {
            if (oldTileLayers != null)
            {
                var oldCollection = oldTileLayers as INotifyCollectionChanged;
                if (oldCollection != null)
                {
                    oldCollection.CollectionChanged -= this.TileLayerCollectionChanged;
                }

                this.TileLayer = null;
                this.ClearTileLayers();
            }

            if (newTileLayers != null)
            {
                this.TileLayer = newTileLayers.FirstOrDefault();
                this.AddTileLayers(0, newTileLayers);

                var newCollection = newTileLayers as INotifyCollectionChanged;
                if (newCollection != null)
                {
                    newCollection.CollectionChanged += this.TileLayerCollectionChanged;
                }
            }
        }

        private void TileLayerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.AddTileLayers(e.NewStartingIndex, e.NewItems.Cast<TileLayer>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.RemoveTileLayers(e.OldStartingIndex, e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    this.RemoveTileLayers(e.NewStartingIndex, e.OldItems.Count);
                    this.AddTileLayers(e.NewStartingIndex, e.NewItems.Cast<TileLayer>());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.ClearTileLayers();
                    if (e.NewItems != null)
                    {
                        this.AddTileLayers(0, e.NewItems.Cast<TileLayer>());
                    }
                    break;

                default:
                    break;
            }

            var tileLayer = this.TileLayers.FirstOrDefault();

            if (this.TileLayer != tileLayer)
            {
                this.TileLayer = tileLayer;
            }
        }

        private void AddTileLayers(int index, IEnumerable<TileLayer> tileLayers)
        {
            foreach (var tileLayer in tileLayers)
            {
                if (index == 0)
                {
                    if (tileLayer.Background != null)
                    {
                        this.Background = tileLayer.Background;
                    }

                    if (tileLayer.Foreground != null)
                    {
                        this.Foreground = tileLayer.Foreground;
                    }
                }

                this.tileLayerPanel.Children.Insert(index++, tileLayer);
            }
        }

        private void RemoveTileLayers(int index, int count)
        {
            while (count-- > 0)
            {
                this.tileLayerPanel.Children.RemoveAt(index + count);
            }

            if (index == 0)
            {
                this.ClearValue(BackgroundProperty);
                this.ClearValue(ForegroundProperty);
            }
        }

        private void ClearTileLayers()
        {
            this.tileLayerPanel.Children.Clear();
            this.ClearValue(BackgroundProperty);
            this.ClearValue(ForegroundProperty);
        }

        private void InternalSetValue(DependencyProperty property, object value)
        {
            {
                if (value is Nutron.CoreTypes.Point)
                {
                    value = ((Nutron.CoreTypes.Point)value).ToSystemPoint();
                }
            }

            this.internalPropertyChange = true;
            this.SetValue(property, value);
            this.internalPropertyChange = false;
        }

        private void AdjustCenterProperty(DependencyProperty property, ref Location center)
        {
            if (center == null)
            {
                center = new Location();
                this.InternalSetValue(property, center);
            }
            else if (center.Longitude < -180d || center.Longitude > 180d ||
                center.Latitude < -this.mapTransform.MaxLatitude || center.Latitude > this.mapTransform.MaxLatitude)
            {
                center = new Location(
                    Math.Min(Math.Max(center.Latitude, -this.mapTransform.MaxLatitude), this.mapTransform.MaxLatitude),
                    Location.NormalizeLongitude(center.Longitude));
                this.InternalSetValue(property, center);
            }
        }

        private void CenterPropertyChanged(Location center)
        {
            if (!this.internalPropertyChange)
            {
                this.AdjustCenterProperty(CenterProperty, ref center);
                this.ResetTransformOrigin();
                this.UpdateTransform();

                if (this.centerAnimation == null)
                {
                    this.InternalSetValue(TargetCenterProperty, center);
                    this.InternalSetValue(CenterPointProperty, this.mapTransform.Transform(center));
                }
            }
        }

        private void TargetCenterPropertyChanged(Location targetCenter)
        {
            if (!this.internalPropertyChange)
            {
                this.AdjustCenterProperty(TargetCenterProperty, ref targetCenter);

                if (!targetCenter.Equals(this.Center))
                {
                    if (this.centerAnimation != null)
                    {
                        this.centerAnimation.Completed -= this.CenterAnimationCompleted;
                    }

                    // animate private CenterPoint property by PointAnimation
                    this.centerAnimation = new PointAnimation
                    {
                        From = this.mapTransform.Transform(this.Center).ToSystemPoint(),
                        To = this.mapTransform.Transform(targetCenter, this.Center.Longitude).ToSystemPoint(),
                        Duration = AnimationDuration,
                        EasingFunction = AnimationEasingFunction
                    };

                    this.centerAnimation.Completed += this.CenterAnimationCompleted;
                    this.BeginAnimation(CenterPointProperty, this.centerAnimation);
                }
            }
        }

        private void CenterAnimationCompleted(object sender, object e)
        {
            if (this.centerAnimation != null)
            {
                this.centerAnimation.Completed -= this.CenterAnimationCompleted;
                this.centerAnimation = null;

                this.InternalSetValue(CenterProperty, this.TargetCenter);
                this.InternalSetValue(CenterPointProperty, this.mapTransform.Transform(this.TargetCenter));
                this.RemoveAnimation(CenterPointProperty); // remove holding animation in WPF

                this.ResetTransformOrigin();
                this.UpdateTransform();
            }
        }

        private void CenterPointPropertyChanged(Point centerPoint)
        {
            if (!this.internalPropertyChange)
            {
                centerPoint.X = Location.NormalizeLongitude(centerPoint.X);
                this.InternalSetValue(CenterProperty, this.mapTransform.Transform(centerPoint.ToCorePoint()));
                this.ResetTransformOrigin();
                this.UpdateTransform();
            }
        }

        private void MinZoomLevelPropertyChanged(double minZoomLevel)
        {
            if (minZoomLevel < 0d || minZoomLevel > this.MaxZoomLevel)
            {
                minZoomLevel = Math.Min(Math.Max(minZoomLevel, 0d), this.MaxZoomLevel);
                this.InternalSetValue(MinZoomLevelProperty, minZoomLevel);
            }

            if (this.ZoomLevel < minZoomLevel)
            {
                this.ZoomLevel = minZoomLevel;
            }
        }

        private void MaxZoomLevelPropertyChanged(double maxZoomLevel)
        {
            if (maxZoomLevel < this.MinZoomLevel || maxZoomLevel > MaximumZoomLevel)
            {
                maxZoomLevel = Math.Min(Math.Max(maxZoomLevel, this.MinZoomLevel), MaximumZoomLevel);
                this.InternalSetValue(MaxZoomLevelProperty, maxZoomLevel);
            }

            if (this.ZoomLevel > maxZoomLevel)
            {
                this.ZoomLevel = maxZoomLevel;
            }
        }

        private void AdjustZoomLevelProperty(DependencyProperty property, ref double zoomLevel)
        {
            if (zoomLevel < this.MinZoomLevel || zoomLevel > this.MaxZoomLevel)
            {
                zoomLevel = Math.Min(Math.Max(zoomLevel, this.MinZoomLevel), this.MaxZoomLevel);
                this.InternalSetValue(property, zoomLevel);
            }
        }

        private void ZoomLevelPropertyChanged(double zoomLevel)
        {
            if (!this.internalPropertyChange)
            {
                this.AdjustZoomLevelProperty(ZoomLevelProperty, ref zoomLevel);
                this.UpdateTransform();

                if (this.zoomLevelAnimation == null)
                {
                    this.InternalSetValue(TargetZoomLevelProperty, zoomLevel);
                }
            }
        }

        private void TargetZoomLevelPropertyChanged(double targetZoomLevel)
        {
            if (!this.internalPropertyChange)
            {
                this.AdjustZoomLevelProperty(TargetZoomLevelProperty, ref targetZoomLevel);

                if (targetZoomLevel != this.ZoomLevel)
                {
                    if (this.zoomLevelAnimation != null)
                    {
                        this.zoomLevelAnimation.Completed -= this.ZoomLevelAnimationCompleted;
                    }

                    this.zoomLevelAnimation = new DoubleAnimation
                    {
                        To = targetZoomLevel,
                        Duration = AnimationDuration,
                        EasingFunction = AnimationEasingFunction
                    };

                    this.zoomLevelAnimation.Completed += this.ZoomLevelAnimationCompleted;
                    this.BeginAnimation(ZoomLevelProperty, this.zoomLevelAnimation);
                }
            }
        }

        private void ZoomLevelAnimationCompleted(object sender, object e)
        {
            if (this.zoomLevelAnimation != null)
            {
                this.zoomLevelAnimation.Completed -= this.ZoomLevelAnimationCompleted;
                this.zoomLevelAnimation = null;

                this.InternalSetValue(ZoomLevelProperty, this.TargetZoomLevel);
                this.RemoveAnimation(ZoomLevelProperty); // remove holding animation in WPF

                this.UpdateTransform(true);
            }
        }

        private void AdjustHeadingProperty(DependencyProperty property, ref double heading)
        {
            if (heading < 0d || heading > 360d)
            {
                heading = ((heading % 360d) + 360d) % 360d;
                this.InternalSetValue(property, heading);
            }
        }

        private void HeadingPropertyChanged(double heading)
        {
            if (!this.internalPropertyChange)
            {
                this.AdjustHeadingProperty(HeadingProperty, ref heading);
                this.UpdateTransform();

                if (this.headingAnimation == null)
                {
                    this.InternalSetValue(TargetHeadingProperty, heading);
                }
            }
        }

        private void TargetHeadingPropertyChanged(double targetHeading)
        {
            if (!this.internalPropertyChange)
            {
                this.AdjustHeadingProperty(TargetHeadingProperty, ref targetHeading);

                if (targetHeading != this.Heading)
                {
                    var delta = targetHeading - this.Heading;

                    if (delta > 180d)
                    {
                        delta -= 360d;
                    }
                    else if (delta < -180d)
                    {
                        delta += 360d;
                    }

                    if (this.headingAnimation != null)
                    {
                        this.headingAnimation.Completed -= this.HeadingAnimationCompleted;
                    }

                    this.headingAnimation = new DoubleAnimation
                    {
                        By = delta,
                        Duration = AnimationDuration,
                        EasingFunction = AnimationEasingFunction
                    };

                    this.headingAnimation.Completed += this.HeadingAnimationCompleted;
                    this.BeginAnimation(HeadingProperty, this.headingAnimation);
                }
            }
        }

        private void HeadingAnimationCompleted(object sender, object e)
        {
            if (this.headingAnimation != null)
            {
                this.headingAnimation.Completed -= this.HeadingAnimationCompleted;
                this.headingAnimation = null;

                this.InternalSetValue(HeadingProperty, this.TargetHeading);
                this.RemoveAnimation(HeadingProperty); // remove holding animation in WPF

                this.UpdateTransform();
            }
        }

        private void UpdateTransform(bool resetTransformOrigin = false)
        {
            Location center;

            if (this.transformOrigin == null)
            {
                center = this.Center;
                this.SetViewportTransform(center);
            }
            else
            {
                this.SetViewportTransform(this.transformOrigin);

                center = this.ViewportPointToLocation(new Point(this.RenderSize.Width / 2d, this.RenderSize.Height / 2d));
                center.Longitude = Location.NormalizeLongitude(center.Longitude);

                if (center.Latitude < -this.mapTransform.MaxLatitude || center.Latitude > this.mapTransform.MaxLatitude)
                {
                    center.Latitude = Math.Min(Math.Max(center.Latitude, -this.mapTransform.MaxLatitude), this.mapTransform.MaxLatitude);
                    resetTransformOrigin = true;
                }

                this.InternalSetValue(CenterProperty, center);

                if (this.centerAnimation == null)
                {
                    this.InternalSetValue(TargetCenterProperty, center);
                    this.InternalSetValue(CenterPointProperty, this.mapTransform.Transform(center));
                }

                if (resetTransformOrigin)
                {
                    this.ResetTransformOrigin();
                    this.SetViewportTransform(center);
                }
            }

            this.CenterScale = this.ViewportScale * this.mapTransform.RelativeScale(center) / TileSource.MetersPerDegree; // Pixels per meter at center latitude

            this.SetTransformMatrixes();
            this.OnViewportChanged();
        }

        private void SetViewportTransform(Location origin)
        {
            var oldMapOriginX = (this.viewportOrigin.X - this.tileLayerOffset.X) / this.ViewportScale - 180d;
            var mapOrigin = this.mapTransform.Transform(origin);

            this.ViewportScale = Math.Pow(2d, this.ZoomLevel) * TileSource.TileSize / 360d;
            this.SetViewportTransform(mapOrigin.ToSystemPoint());

            this.tileLayerOffset.X = this.viewportOrigin.X - (180d + mapOrigin.X) * this.ViewportScale;
            this.tileLayerOffset.Y = this.viewportOrigin.Y - (180d - mapOrigin.Y) * this.ViewportScale;

            if (Math.Abs(mapOrigin.X - oldMapOriginX) > 180d)
            {
                // immediately handle map origin leap when map center moves across 180° longitude
                this.UpdateTiles(this, EventArgs.Empty);
            }
            else
            {
                this.SetTileLayerTransform();

                if (!UpdateTilesWhileViewportChanging)
                {
                    this.tileUpdateTimer.Stop();
                }

                this.tileUpdateTimer.Start();
            }
        }

        private void UpdateTiles(object sender, object e)
        {
            this.tileUpdateTimer.Stop();

            var zoomLevel = (int)Math.Round(this.ZoomLevel + ZoomLevelSwitchDelta);
            var transform = this.GetTileIndexMatrix((double)(1 << zoomLevel) / 360d);

            // tile indices of visible rectangle
            var p1 = transform.Transform(new Point(0d, 0d));
            var p2 = transform.Transform(new Point(this.RenderSize.Width, 0d));
            var p3 = transform.Transform(new Point(0d, this.RenderSize.Height));
            var p4 = transform.Transform(new Point(this.RenderSize.Width, this.RenderSize.Height));

            // index ranges of visible tiles
            var x1 = (int)Math.Floor(Math.Min(p1.X, Math.Min(p2.X, Math.Min(p3.X, p4.X))));
            var y1 = (int)Math.Floor(Math.Min(p1.Y, Math.Min(p2.Y, Math.Min(p3.Y, p4.Y))));
            var x2 = (int)Math.Floor(Math.Max(p1.X, Math.Max(p2.X, Math.Max(p3.X, p4.X))));
            var y2 = (int)Math.Floor(Math.Max(p1.Y, Math.Max(p2.Y, Math.Max(p3.Y, p4.Y))));
            var grid = new Int32Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1);

            if (this.TileZoomLevel != zoomLevel || this.TileGrid != grid)
            {
                this.TileZoomLevel = zoomLevel;
                this.TileGrid = grid;

                this.SetTileLayerTransform();

                var tileGridChanged = this.TileGridChanged;

                if (tileGridChanged != null)
                {
                    tileGridChanged(this, EventArgs.Empty);
                }
            }
        }
    }
}
