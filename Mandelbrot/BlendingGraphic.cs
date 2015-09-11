using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot
{
    class AdditiveBlender
    {
        public int Width;
        public int Height;
        public Rectangle Rectangle;
        public PixelFormat PixelFormat;

        Bitmap product;
        Bitmap queue;

        Graphics productGraphic;
        Graphics queueGraphic;

        public AdditiveBlender(int width, int height){
            Width = width;
            Height = height;
            Rectangle = new Rectangle(0, 0, Width, Height);
            PixelFormat = PixelFormat.Format32bppArgb;

            product = new Bitmap(Width, Height, PixelFormat);
            queue = new Bitmap(Width, Height, PixelFormat);

            productGraphic = Graphics.FromImage(product);
            queueGraphic = Graphics.FromImage(queue);

            productGraphic.Clear(Color.Transparent);
            queueGraphic.Clear(Color.Transparent);
        }

        public Graphics GetGraphics()
        {
            return queueGraphic;
        }

        public void DrawOnto(Graphics g)
        {
            g.DrawImageUnscaled(product, Rectangle);
        }

        public void Blend()
        {
            // Assumes all bitmaps are the same size and same pixel format
            BitmapData productData = product.LockBits(Rectangle, ImageLockMode.ReadWrite, PixelFormat);
            BitmapData queueData = queue.LockBits(Rectangle, ImageLockMode.ReadWrite, PixelFormat);

            unsafe
            {
                byte* productPtr = (byte*) productData.Scan0.ToPointer();
                byte* queuePtr = (byte*) queueData.Scan0.ToPointer();
                int bytesPerPix = productData.Stride / Width;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++, productPtr += bytesPerPix, queuePtr += bytesPerPix)
                    {
                        if (*(queuePtr + 3) == 0)
                            continue;

                        *(productPtr + 0) += *(queuePtr + 0);
                        *(productPtr + 1) += *(queuePtr + 1);
                        *(productPtr + 2) += *(queuePtr + 2);
                        *(productPtr + 3) += *(queuePtr + 3);

                        *(byte*)(queuePtr + 0) = 0;
                        *(byte*)(queuePtr + 1) = 0;
                        *(byte*)(queuePtr + 2) = 0;
                        *(byte*)(queuePtr + 3) = 0;
                    }
                }
            }
            product.UnlockBits(productData);
            queue.UnlockBits(queueData);
        }
    }
}
