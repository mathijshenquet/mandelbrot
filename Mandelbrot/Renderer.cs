using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mandelbrot
{
    public class Renderer
    {
        public FractalBox fractalBox;

        protected volatile bool shouldAbort = false;
        protected Thread[] renderThreads;
        protected ManualResetEvent[] threadSuspenders;
        protected volatile bool[] threadRendering;
        public volatile int renderID;

        protected Color[,] pixelmap;
        protected PixelState[,] state;
        protected Double[,] escapeCounts;

        protected Queue<Rectangle> segments = new Queue<Rectangle>();
        protected Object stateLock = new Object();
        protected int maxIterations;

        protected enum PixelState : byte
        {
            None = 0x0,
            Seen = 0x1,
            Calculated = 0x2,
            NeedsPainting = 0x4,
            SeedPoint = 0x8
        }

        protected int threads;
        protected int segmentRows;
        protected int segmentColumns;

        public Renderer(int maxIterations, int segmentColumns, int segmentRows, int threads)
        {
            this.maxIterations = maxIterations;
            this.segmentColumns = segmentColumns;
            this.segmentRows = segmentRows;
            this.threads = threads;

            renderThreads = new Thread[threads];
            threadSuspenders = new ManualResetEvent[threads];
            threadRendering = new bool[threads];
            for (int i = 0; i < threads; i++)
            {
                threadRendering[i] = false;

                var suspender = new ManualResetEvent(false);
                threadSuspenders[i] = suspender;

                int threadID = i;
                var renderThread = new Thread(() => renderLoop(threadID));
                renderThreads[i] = renderThread;

                renderThread.IsBackground = false; //TODO why?
                renderThread.Priority = threads == 1 ? ThreadPriority.BelowNormal : ThreadPriority.Lowest;
                renderThread.Start();
            }
        }

        public bool IsRendering()
        {
            for (int i = 0; i < threads; i++)
            {
                if (threadRendering[i])
                    return true;
            }
            return false;
        }

        public void Render()
        {
            Stop();

            renderID += 1;

            lock (stateLock)
            {
                fractalBox.FractalView.Fractal.MaxIterations = maxIterations;

                pixelmap = new Color[fractalBox.SampleWidth, fractalBox.SampleHeight];
                state = new PixelState[fractalBox.SampleWidth, fractalBox.SampleHeight];
                escapeCounts = new double[fractalBox.SampleWidth, fractalBox.SampleHeight];

                var w = fractalBox.SampleWidth / segmentColumns;
                var h = fractalBox.SampleHeight / segmentRows;

                var map = new byte[segmentColumns, segmentRows];
                var rect = new Rectangle(0, 0, segmentColumns, segmentRows);

                int x = segmentColumns / 2 - 1;
                int y = segmentRows / 2;

                var dx = 1;
                var dy = 0;
                segments.Clear();

                while(true){
                    if (!rect.Contains(x, y))
                        break;

                    enqueueSegment(x, y);
                    map[x, y] = 1;
                    x += dx;
                    y += dy;

                    if(!rect.Contains(x + dx, y + dy))
                        break;

                    if (map[x + dy, y - dx] == 0)
                    {
                        var tmp = dx;
                        dx = dy;
                        dy = -tmp;
                    }
                }

                x = 0;
                y = 0;
                dx = 1;
                dy = 0;

                while(true)
                {
                    if (!rect.Contains(x, y) || map[x, y] == 2)
                        break;

                    if (map[x, y] == 0)
                        enqueueSegment(x, y);
                        map[x, y] = 2;

                    if (!rect.Contains(x + dx, y + dy) || map[x + dx, y + dy] == 2)
                    {
                        var tmp = dx;
                        dx = -dy;
                        dy = tmp;
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (int i = 0; i < threads; i++)
            {
                threadSuspenders[i].Set();
                threadRendering[i] = true;
            }
        }

        private void enqueueSegment(int x, int y)
        {
            var width = fractalBox.SampleWidth / segmentColumns;
            var height = fractalBox.SampleHeight / segmentRows;

            int segWidth;
            if (x == segmentColumns - 1)
                segWidth = fractalBox.SampleWidth - x * width;
            else
                segWidth = width;

            int segHeight;
            if (y == segmentRows - 1)
                segHeight = fractalBox.SampleHeight - y * height;
            else
                segHeight = height;

            segments.Enqueue(new Rectangle(x * width, y * height, segWidth, segHeight));
        }

        public void DrawOnto(Bitmap bitmap)
        {
            if (state == null || fractalBox.SampleWidth != bitmap.Width || fractalBox.SampleHeight != bitmap.Height)
                return;

            var lockedBits = bitmap.LockBits(
                        new Rectangle(0, 0, fractalBox.SampleWidth, fractalBox.SampleHeight),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite,
                        bitmap.PixelFormat);

            for (int x = 0; x < fractalBox.SampleWidth; x++)
            {
                for (int y = 0; y < fractalBox.SampleHeight; y++)
                {
                    if (!checkBits(state[x, y], PixelState.NeedsPainting))
                        continue;

                    unsafe
                    {
                        byte* pixel = (byte*)lockedBits.Scan0 + (y * lockedBits.Stride) + (x * 4);
                        setPixel(pixel, pixelmap[x, y]);
                    }

                    state[x, y] ^= PixelState.NeedsPainting;
                }
            }

            bitmap.UnlockBits(lockedBits);
        }

        public void Stop(bool wait = false)
        {
            foreach (var suspender in threadSuspenders)
                suspender.Reset();

            if (wait)
                for (int id = 0; id < threads; id++)
                {
                    while (renderThreads[id].ThreadState != ThreadState.WaitSleepJoin)
                        Thread.Sleep(10);

                    threadRendering[id] = false;
                }
        }

        private void renderLoop(int threadID)
        {
            while (!shouldAbort)
            {
                threadSuspenders[threadID].WaitOne(Timeout.Infinite);

                Rectangle? segment = null;
                lock (stateLock){
                    if (segments.Count > 0)
                        segment = segments.Dequeue();
                }

                if (segment.HasValue)
                {
                    RenderSegment(segment.Value);
                }
                else
                {
                    threadRendering[threadID] = false;
                    threadSuspenders[threadID].Reset();
                }
            }
        }

        public void Recolor()
        {
            lock (stateLock)
            {
                for (int x = 0; x < fractalBox.SampleWidth; x++)
                {
                    for (int y = 0; y < fractalBox.SampleHeight; y++)
                    {
                        pixelmap[x, y] = fractalBox.FractalView.Coloring.Color(escapeCounts[x, y]);
                        state[x, y] |= PixelState.NeedsPainting;
                    }
                }
            }
        }

        protected void RenderSegment(Rectangle segmentBox)
        {
            var localRenderID = renderID;
            var fractal = fractalBox.FractalView.Fractal;
            var coloring = fractalBox.FractalView.Coloring;

            var activePoints = new List<Point>();

            for (int y = 1; y < segmentBox.Height; y += 5)
            {
                activePoints.Add(new Point(segmentBox.X, segmentBox.Y + y));
                activePoints.Add(new Point(segmentBox.X + segmentBox.Width - 1, segmentBox.Y + y));
            }

            for (int x = 1; x < segmentBox.Width; x += 5)
            {
                activePoints.Add(new Point(segmentBox.X + x, segmentBox.Y));
                activePoints.Add(new Point(segmentBox.X + x, segmentBox.Y + segmentBox.Height - 1));
            }

            foreach (var pnt in activePoints)
                state[pnt.X, pnt.Y] |= PixelState.SeedPoint;

            var newActivePoints = new List<Point>();
            var edgePoints = new List<Point>();
            var fuzzyBorders = true;// fractal.Mode == FractalMode.Julia;
            var i = 0;
            while (i++ < 1000)
            {
                if (localRenderID != renderID)
                    break;

                foreach (Point p in activePoints)
                {
                    var parameter = fractalBox.FromBitmapCoords(p);
                    var escapeCount = fractal.EscapeCount(parameter, true);

                    if (Double.IsNaN(escapeCount))
                        break;

                    var color = (Color)coloring.Color(escapeCount);
                    escapeCounts[p.X, p.Y] = escapeCount;
                    pixelmap[p.X, p.Y] = color;
                    state[p.X, p.Y] |= PixelState.NeedsPainting | PixelState.Seen;

                    if (Double.IsPositiveInfinity(escapeCount))
                    {
                        if (fuzzyBorders && !checkBits(state[p.X, p.Y], PixelState.SeedPoint))
                            edgePoints.Add(p);

                        continue;
                    }

                    Point[] neighbors = {
                        new Point(p.X + 1, p.Y),
                        new Point(p.X - 1, p.Y),
                        new Point(p.X, p.Y + 1),
                        new Point(p.X, p.Y - 1),
                    };

                    foreach (var neighbor in neighbors)
                    {
                        var x = neighbor.X;
                        var y = neighbor.Y;

                        if (segmentBox.Contains(neighbor) && !checkBits(state[x, y], PixelState.Seen))
                        {
                            state[x, y] |= PixelState.Seen;
                            newActivePoints.Add(neighbor);
                        }
                    }
                }

                if (newActivePoints.Count == 0)
                {
                    if (!fuzzyBorders || edgePoints.Count == 0)
                        break;

                    var s = 5;
                    foreach (var p in edgePoints)
                    {
                        Point[] neighbors = {
                            new Point(p.X + s, p.Y),
                            new Point(p.X - s, p.Y),
                            new Point(p.X, p.Y + s),
                            new Point(p.X, p.Y - s),
                            new Point(p.X + s, p.Y + s),
                            new Point(p.X + s, p.Y - s),
                            new Point(p.X - s, p.Y + s),
                            new Point(p.X - s, p.Y - s),
                        };

                        foreach (var neighbor in neighbors)
                        {
                            var x = neighbor.X;
                            var y = neighbor.Y;

                            if (segmentBox.Contains(neighbor) && !checkBits(state[x, y], PixelState.Seen))
                            {
                                state[x, y] |= PixelState.SeedPoint;
                                state[x, y] |= PixelState.Seen;
                                newActivePoints.Add(neighbor);
                            }
                        }
                    }
                    edgePoints.Clear();
                }

                var tmp = activePoints;

                activePoints = newActivePoints;
                newActivePoints = tmp;
                newActivePoints.Clear();
            }

            if (renderID != localRenderID)
                for (int x = segmentBox.X; x < segmentBox.X + segmentBox.Width; x++)
                    for (int y = segmentBox.Y; y < segmentBox.Y + segmentBox.Height; y++)
                        state[x, y] = PixelState.None;
            
            else
                for (int x = segmentBox.X; x < segmentBox.X + segmentBox.Width; x++)
                    for (int y = segmentBox.Y; y < segmentBox.Y + segmentBox.Height; y++)
                        if (!checkBits(state[x, y], PixelState.Seen)) {
                            pixelmap[x, y] = Color.Black;
                            state[x, y] |= PixelState.NeedsPainting;
                        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool checkBits(int s, int bits)
        {
            return (s & bits) == bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool checkBits(PixelState s, PixelState bits)
        {
            return (s & bits) == bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe void setPixel(byte* pixel, Color c)
        {
            pixel[0] = (byte)c.B;
            pixel[1] = (byte)c.G;
            pixel[2] = (byte)c.R;
            pixel[3] = (byte)c.A;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe void setPixel(byte* pixel, byte r, byte g, byte b)
        {
            pixel[0] = b;
            pixel[1] = g;
            pixel[2] = r;
            pixel[3] = 255;
        }
    }
}
