using System.Drawing;

namespace Halftonify
{
    public static class Extensions
    {
        public static void DrawCircle(this Graphics g, Brush brush, float x, float y, float radius)
        {
            g.FillEllipse(brush, x - radius, y - radius, 2.0f * radius, 2.0f * radius);
        }
    }
}
