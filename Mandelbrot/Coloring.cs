using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot
{
    public class Coloring
    {
        public double Hue = 0.1;

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = (value * 255);
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
            
            if (hi == 0)
                return System.Drawing.Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return System.Drawing.Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return System.Drawing.Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return System.Drawing.Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return System.Drawing.Color.FromArgb(255, t, p, v);
            else
                return System.Drawing.Color.FromArgb(255, v, p, q);
        }

        public Color Color(double normalizedEscapeCount)
        {
            if (normalizedEscapeCount < 0 || Double.IsPositiveInfinity(normalizedEscapeCount)) return System.Drawing.Color.Black;

            var v = Cosine(Math.Sqrt(normalizedEscapeCount) / 10);

            var color = ColorFromHSV(((Hue*360) % 360 + 360) % 360, v * 2.0 / 3.0, (1 - v));
            return color;
        }

        private static double Zigzag(double n)
        {
            return Math.Abs((n % 2) - 1);
        }

        private static double Cosine(double n)
        {
            var x = ((n - 0.5) % 2) - 1;
            var res = Math.Pow(4 * x * (1 - Math.Abs(x)), 2);

            if (res < 0)
                return 0;
            else if(1 < res)
                return 1;
            else
                return res;
        }
    }
}

class QuadraticBezierCurve
{
    double x0, y0, x1, y1, x2, y2;

    public QuadraticBezierCurve(double x0, double y0, double x1, double y1, double x2, double y2){
        this.x0 = x0;
        this.y0 = y0;
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
    }

    public Tuple<Double, Double> At(double t)
    {
        var d = (1 - t);
            
        var x = d * (d * x0 + t * x1) + t * ( d * x1 + t * x2);
        var y = d * (d * y0 + t * y1) + t * ( d * y1 + t * y2);

        return new Tuple<double, double>(x, y);
    }
}