
namespace MapControl
{
    public static class PointExtensions
    {
        public static Nutron.CoreTypes.Point ToCorePoint(this System.Windows.Point p)
            => new Nutron.CoreTypes.Point(p.X, p.Y);

        public static System.Windows.Point ToSystemPoint(this Nutron.CoreTypes.Point p)
            => new System.Windows.Point(p.X, p.Y);
    }
}
