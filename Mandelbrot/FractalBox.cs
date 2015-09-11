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
    public class FractalBox : PictureBox
    {
        private FractalView fractalView;
        public FractalView FractalView
        {
            set
            {
                if(FractalView != null)
                    FractalView.StateChanged -= NeedsRedraw;

                fractalView = value;

                FractalView.StateChanged += NeedsRedraw;
                NeedsRedraw();
            }
            get
            {
                return fractalView;
            }
        }

        public int SampleWidth
        {
            get
            {
                return (int) (Width * sampleRate);
            }
        }

        public int SampleHeight
        {
            get
            {
                return (int) (Height * sampleRate);
            }
        }

        public Renderer Renderer;
        public Bitmap Bitmap;
        private double sampleRate = 1.0;

        private double scale{ get; set; }
        private double translateX{ get; set; }
        private double translateY{ get; set; }

        public delegate void ComplexEventHandler(Complex z);
        public event ComplexEventHandler Hover = delegate { };

        private Point mousePosition;
        public bool Hovered{get; private set;}

        private bool painting = false;
        bool panning = false;
        Complex panCenterStart = new Complex(0, 0);
        Point panStart = new Point(0, 0);
        Point panCurrent = new Point(0, 0);

        public FractalBox(FractalView fractalView, Renderer renderer)
            : base()
        {
            FractalView = fractalView;
            Renderer = renderer;

            renderer.fractalBox = this;

            ResizeRedraw = false;
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            Margin = new Padding(0);

            var renderLoop = new System.Windows.Forms.Timer();
            renderLoop.Tick += (object o, EventArgs e) => Invalidate();
            renderLoop.Interval = 1000 / 30;
            renderLoop.Start();
            
            Bitmap = new Bitmap(SampleWidth, SampleHeight);

            Paint += paint;
            Resize += resizeControl;
            ControlAdded += resizeControl;

            Hover += delegate { };

            MouseDown += (object sender, MouseEventArgs e) => PanStart(e.Location);

            MouseMove += (object sender, MouseEventArgs e) =>
            {
                if (panning)
                    PanUpdate(e.Location);
                else
                    mousePosition = e.Location;
                    Hover.Invoke(FromScreenCoords(e.Location));
            };

            MouseEnter += (object sender, EventArgs e) => Hovered = true;

            MouseLeave += (object sender, EventArgs e) => Hovered = false;

            MouseUp += (object sender, MouseEventArgs e) => PanEnd(e.Location);

            MouseDoubleClick += (object sender, MouseEventArgs e) => FractalView.SetCenter(FromScreenCoords(e.Location));
        }

        void paint(object sender, PaintEventArgs e)
        {
            var delta = panning ? new Point(panCurrent.X - panStart.X, panCurrent.Y - panStart.Y) : new Point(0, 0);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            e.Graphics.DrawImage(Bitmap, delta.X, delta.Y, Width, Height);

            Pen pen;

            pen = new Pen(Color.Gray);
            e.Graphics.DrawLine(pen, Width / 2 - 10, Height / 2, Width / 2 - 5, Height / 2);
            e.Graphics.DrawLine(pen, Width / 2 + 10, Height / 2, Width / 2 + 5, Height / 2);
            e.Graphics.DrawLine(pen, Width / 2, Height / 2 - 10, Width / 2, Height / 2 - 5);
            e.Graphics.DrawLine(pen, Width / 2, Height / 2 + 10, Width / 2, Height / 2 + 5);
            pen.Dispose();

            Complex orbitStart = Hovered ? FromScreenCoords(mousePosition) : new Complex(0,0);

            var orbit = FractalView.Fractal.EscapeSequence(orbitStart, 255);
            var g = e.Graphics;
            pen = new Pen(Color.FromArgb(64, 255, 255, 255), 2);
            var lastPoint = ToScreenCoords(orbit[0]);
            lastPoint.Offset(delta);

            var rect = new Rectangle(-1 * Width, -1 * Height, 3 * Width, 3 * Height);

            foreach (var complex in orbit.Skip(1))
            {
                var point = ToScreenCoords(complex);
                if(!rect.Contains(point)){
                    break;
                }
                point.Offset(delta);
                g.DrawLine(pen, lastPoint, point);
                lastPoint = point;
            }

            if (painting)
            {
                if (!Renderer.IsRendering())
                {
                    //Thread.Sleep(1);
                    painting = false;
                }

                Renderer.DrawOnto(Bitmap);
            }
        }

        public void NeedsRedraw(object sender, EventArgs e)
        {
            NeedsRedraw();
        }

        public void NeedsRedraw()
        {
            if (Renderer == null)
                return;

            Renderer.Stop();
            preRender();
            Renderer.Render();
        }

        public void ModifyHue(double delta)
        {
            FractalView.ModifyHue(delta);
            Renderer.Recolor();
            painting = true;
        }

        public void ScaleZoom(double factor)
        {
            Renderer.Stop();
            FractalView.ScaleZoom(factor);
            preRender();

            var cx = SampleWidth / 2;
            var cy = SampleHeight / 2;
            var sw = (int) (((double)SampleWidth) * factor);
            var sh = (int) (((double)SampleHeight) * factor);

            Graphics.FromImage(Bitmap).DrawImage(Bitmap, cx - sw/2, cy - sh/2, sw, sh);
            Renderer.Render();
        }

        public void SetCenter(Complex center)
        {
            Renderer.Stop();
            FractalView.SetCenter(center);
            preRender();
            Renderer.Render();
        }

        public void SetView(Complex center, double zoom)
        {
            FractalView.SetView(center, zoom);
            Renderer.Render();
        }

        public void resizeControl(object sender, EventArgs e)
        {
            Renderer.Stop(true);

            Bitmap = new Bitmap(SampleWidth, SampleHeight);

            //CenterMoved.Invoke(Center);
            preRender();
            Renderer.Render();
        }

        public void PanStart(Point startPos)
        {
            panCenterStart = FractalView.Center;
            panStart = startPos;
            panCurrent = startPos;
            panning = true;
        }

        public void PanUpdate(Point currentPos)
        {
            panCurrent = currentPos;
            var delta = new Point(currentPos.X - panStart.X, currentPos.Y - panStart.Y);
            FractalView.SetCenter(panCenterStart - new Complex(((double)delta.X / scale), ((double)delta.Y / scale)));
        }

        public void PanEnd(Point endPos)
        {
            if (!panning)
                return;

            var delta = new Point(endPos.X - panStart.X, endPos.Y - panStart.Y);

            lock (Bitmap)
            {
                var tmp = (Bitmap)Bitmap.Clone();
                Graphics g = Graphics.FromImage(Bitmap);
                g.Clear(BackColor);
                g.DrawImage(tmp, (int) (delta.X * sampleRate), (int) (delta.Y * sampleRate), SampleWidth, SampleHeight);
            }

            FractalView.SetCenter(panCenterStart - new Complex(((double)delta.X / scale), ((double)delta.Y / scale)));

            panning = false;

            preRender();
            Renderer.Render();
        }

        private void preRender()
        {
            scale = Math.Max(Width, Height) / FractalView.Zoom;
            translateX = -(Width / 2) + scale * FractalView.Center.Real;
            translateY = -(Height / 2) + scale * FractalView.Center.Imaginary;
            painting = true;

            if(Hovered)
                Hover.Invoke(FromScreenCoords(mousePosition));
        }

        public Point ToScreenCoords(Complex z)
        {
            return new Point(
                (int) (Math.Round((z.Real * scale)      - translateX)),
                (int) (Math.Round((z.Imaginary * scale) - translateY))
            );
        }

        public Complex FromScreenCoords(Point p)
        {
            return new Complex(
                (p.X + translateX) / (scale), 
                (p.Y + translateY) / (scale)
            );
        }

        public Complex FromBitmapCoords(Point p)
        {
            return new Complex(
                (p.X/sampleRate + translateX) / scale,
                (p.Y/sampleRate + translateY) / scale
            );
        }
    }
}
