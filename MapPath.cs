// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// © 2015 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Windows.Media;

namespace MapControl
{
    /// <summary>
    /// Base class for map shapes. The shape geometry is given by the Data property,
    /// which must contain a Geometry defined in cartesian (projected) map coordinates.
    /// The Stretch property is meaningless for MapPath, it will be reset to None.
    /// </summary>
    public partial class MapPath : IMapElement
    {
        private MapBase parentMap;

        public MapBase ParentMap
        {
            get 
            { 
                return this.parentMap; 
            }

            set
            {
                this.parentMap = value;
                this.UpdateData();
            }
        }

        protected virtual void UpdateData()
        {
            if (this.Data != null)
            {
                if (this.parentMap != null)
                {
                    this.Data.Transform = this.ParentMap.ViewportTransform;
                }
                else
                {
                    this.Data.ClearValue(Geometry.TransformProperty);
                }
            }
        }
    }
}
