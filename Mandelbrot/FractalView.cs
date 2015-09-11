using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mandelbrot
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class FractalView
    {
        private Complex center;
        public Complex Center
        {
            get { return center; }
            private set
            {
                center = value;
                CenterMoved.Invoke(value);
            }
        }
        public double Zoom { get; private set; }
        
        public delegate void ComplexEventHandler(Complex z);
        public event ComplexEventHandler Hover = delegate { };
        public event ComplexEventHandler CenterMoved = delegate { };
        public event EventHandler StateChanged = delegate { };

        public Fractal Fractal;
        public Coloring Coloring;
        public Renderer Renderer;

        public FractalView(Complex center, double zoom, Fractal fractal, Coloring coloring)
            : base()
        {
            Center = center;
            Zoom = zoom;
            Fractal = fractal;
            Coloring = coloring;
        }

        public void ModifyHue(double delta)
        {
            Coloring.Hue += delta;
        }
        
        public void ScaleZoom(double factor)
        {
            Zoom /= factor;
        }

        public void SetCenter(Complex center)
        {
            Center = center;

            CenterMoved.Invoke(Center);
        }

        public void SetView(Complex center, double zoom)
        {
            Center = center;
            Zoom = zoom;

            CenterMoved.Invoke(Center);
        }

        public void SetJuliaConstant(Complex c)
        {
            if (Fractal.Mode == FractalMode.Julia)
            {
                StateChanged.Invoke(this, EventArgs.Empty);
                Fractal.JuliaConstant = c;
            }
        }
    }
}
