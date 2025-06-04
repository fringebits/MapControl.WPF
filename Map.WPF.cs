// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Windows;
using System.Windows.Input;

namespace MapControl
{
    using SimpleLogger;

    /// <summary>
    /// Default input event handling.
    /// </summary>
    public class Map : MapBase
    {
        public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.Register(
            "ManipulationMode", typeof(ManipulationModes), typeof(Map), new PropertyMetadata(ManipulationModes.All));

        public static readonly DependencyProperty MouseWheelZoomDeltaProperty = DependencyProperty.Register(
            "MouseWheelZoomDelta", typeof(double), typeof(Map), new PropertyMetadata(.35d));

        public static MouseButton MousePanMode { get; set; } = MouseButton.Left;

        private Point? mousePosition;

        static Map()
        {
            IsManipulationEnabledProperty.OverrideMetadata(typeof(Map), new FrameworkPropertyMetadata(true));
        }

        public Map()
        {
            this.EnableDefaultMouseTranslation = true;
        }

        /// <summary>
        /// Gets or sets a value that specifies how the map control handles manipulations.
        /// </summary>
        public ManipulationModes ManipulationMode
        {
            get { return (ManipulationModes)this.GetValue(ManipulationModeProperty); }
            set { this.SetValue(ManipulationModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the amount by which the ZoomLevel property changes during a MouseWheel event.
        /// </summary>
        public double MouseWheelZoomDelta
        {
            get { return (double)this.GetValue(MouseWheelZoomDeltaProperty); }
            set { this.SetValue(MouseWheelZoomDeltaProperty, value); }
        }

        public bool EnableDefaultMouseTranslation
        {
            get; set;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            var zoomDelta = this.MouseWheelZoomDelta * e.Delta / 120d;
            this.ZoomMap(e.GetPosition(this), this.TargetZoomLevel + zoomDelta);
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            base.OnStylusDown(e);

            Logger.Log($"StylusDevice: {e.StylusDevice.Name}");
        }

        public void MousePanBegin(MouseEventArgs e)
        {
            if (this.CaptureMouse())
            {
                this.mousePosition = e.GetPosition(this);
            }
        }

        public void MousePanEnd(MouseEventArgs e)
        {
            if (this.mousePosition.HasValue)
            {
                this.mousePosition = null;
                this.ReleaseMouseCapture();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (MousePanMode == MouseButton.Left)
            {
                this.MousePanBegin(e);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (MousePanMode == MouseButton.Left)
            {
                this.MousePanEnd(e);
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            if (MousePanMode == MouseButton.Right)
            {
                this.MousePanBegin(e);
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);

            if (MousePanMode == MouseButton.Right)
            {
                this.MousePanEnd(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.mousePosition.HasValue && this.EnableDefaultMouseTranslation)
            {
                var position = e.GetPosition(this);
                this.TranslateMap((Point)(position - this.mousePosition.Value));
                this.mousePosition = position;
            }
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);

            Manipulation.SetManipulationMode(this, this.ManipulationMode);
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);

            this.TransformMap(e.ManipulationOrigin,
                (Point)e.DeltaManipulation.Translation, e.DeltaManipulation.Rotation,
                (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2d);
        }
    }
}
