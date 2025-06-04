
namespace MapControl
{
    public static class PointExtensions
    {
        public static Helix.CoreTypes.Point ToCorePoint(this System.Windows.Point p)
            => new Helix.CoreTypes.Point(p.X, p.Y);

        public static System.Windows.Point ToSystemPoint(this Helix.CoreTypes.Point p)
            => new System.Windows.Point(p.X, p.Y);
    }
}
