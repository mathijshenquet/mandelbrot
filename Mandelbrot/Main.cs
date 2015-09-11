using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Globalization;

namespace Mandelbrot
{
    public partial class Main : Form
    {
        public Main()
        {
            Text = "Mandelbrot";
            Width = Screen.PrimaryScreen.Bounds.Width;
            Height = Screen.PrimaryScreen.Bounds.Height;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var mainContainer = new SplitContainer();
            mainContainer.Dock = DockStyle.Fill;
            mainContainer.IsSplitterFixed = true;
            mainContainer.FixedPanel = FixedPanel.Panel2;
            mainContainer.Size = new System.Drawing.Size(500, 500);
            mainContainer.SplitterWidth = 1;
            mainContainer.SplitterDistance = mainContainer.Size.Width - 300;
            Controls.Add(mainContainer);

            var controlTable = new TableLayoutPanel();
            controlTable.Dock = DockStyle.Fill;
            controlTable.ColumnCount = 1;
            controlTable.RowCount = 8;
            controlTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));

            mainContainer.Panel2.Controls.Add(controlTable);

            var controls = controlTable.Controls;

            var coloring = new Coloring();
            coloring.Hue = 0.11;
            var center = new Complex(-0.75, 0.001); //new Complex(-0.1666, -0.6485);

            var mandelbrotFractalView = new FractalView(new Complex(-0.75, 0), 4, new Fractal(FractalMode.Mandelbrot), coloring);
            var juliaFractalView = new FractalView(new Complex(0, 0), 4, new Fractal(FractalMode.Julia, new Complex(0, 1)), coloring);

            var primaryBox = new FractalBox(mandelbrotFractalView, new Renderer(10000, 16, 10, 8));
            var secondaryBox = new FractalBox(juliaFractalView, new Renderer(1000, 1, 1, 1));

            mandelbrotFractalView.CenterMoved += juliaFractalView.SetJuliaConstant;

            MouseWheel += (object sender, MouseEventArgs e) =>
            {
                FractalBox targetBox;
                if(primaryBox.Hovered)
                    targetBox = primaryBox;
                else if(secondaryBox.Hovered)
                    targetBox = secondaryBox;
                else
                    return;
                targetBox.ScaleZoom(1.0 + (double)e.Delta / 1000.0);
            };
            mainContainer.Panel1.Controls.Add(primaryBox);
            controls.Add(secondaryBox);

            var PreviewControls = new FlowLayoutPanel();
            controls.Add(PreviewControls);

            Button btn;
            var tooltips = new ToolTip();

            btn = new Button();
            btn.Text = "swap";
            tooltips.SetToolTip(btn, "Verwissel de voorbeeld en hoofd fractal afbeelder");
            btn.Click += (object o, EventArgs e) =>
            {
                var primaryFractalView = primaryBox.FractalView;
                var secondaryFractalView = secondaryBox.FractalView;

                primaryBox.FractalView = secondaryFractalView;
                secondaryBox.FractalView = primaryFractalView;
            };
            PreviewControls.Controls.Add(btn);
            
            var grp = new GroupBox();
            grp.Dock = DockStyle.Top;
            grp.Text = "Mouse";
            grp.Height = 200;
            controls.Add(grp);

            var tbl = new TableLayoutPanel();
            tbl.RowCount = 4;
            tbl.ColumnCount = 2;
            tbl.Dock = DockStyle.Fill;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            grp.Controls.Add(tbl);

            Func<String, TextBox> mkField = (String labelText) =>
            {
                var label = new Label();
                label.Text = labelText;
                label.AutoSize = true;
                label.Anchor = AnchorStyles.Left;
                label.TextAlign = ContentAlignment.MiddleLeft;

                var field = new TextBox();
                field.Dock = DockStyle.Fill;

                tbl.Controls.Add(label);
                tbl.Controls.Add(field);

                return field;
            };
            var mouseReal = mkField("real");
            var mouseImag = mkField("imag");
            var mouseZoom = mkField("zoom");
            var mouseCount = mkField("count");

            var coords = mkField("coord");

            Func<FractalBox, FractalBox.ComplexEventHandler> hoverHandler = (FractalBox fractalBox) => (Complex z) =>
            {
                var fractalView = fractalBox.FractalView;

                mouseZoom.Text = Math.Round(Math.Log(1 + 1 / fractalView.Zoom), 2).ToString();
                mouseReal.Text = z.Real.ToString();
                mouseImag.Text = z.Imaginary.ToString();
                mouseCount.Text = Math.Round(fractalView.Fractal.EscapeCount(z, false), 1).ToString();
            };

            mandelbrotFractalView.CenterMoved += (Complex z) =>
            {
                coords.Text = String.Format("{0}:{1}:{2}", 
                        z.Real.ToString("R"), 
                        z.Imaginary.ToString("R"), 
                        Math.Round(Math.Log(1 + 1 / primaryBox.FractalView.Zoom), 2).ToString()
                    );
            };


            coords.KeyUp += (object o, KeyEventArgs e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                var parts = coords.Text.Split(':');

                if (parts.Length != 3)
                    return;

                double real, imag, zoom;

                try
                {
                    real = Double.Parse(parts[0]);
                    imag = Double.Parse(parts[1]);
                    zoom = 1 / (Math.Exp(Double.Parse(parts[2])) - 1);
                }
                catch
                {
                    return;
                }

                primaryBox.SetView(new Complex(real, imag), zoom);
            };

            primaryBox.Hover   += hoverHandler(primaryBox);
            secondaryBox.Hover += hoverHandler(secondaryBox);
        }
    }
}
