using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Runtime.InteropServices;

namespace Mandelbrot
{
    public enum FractalMode{
        Mandelbrot,
        Julia
    }

    public class Fractal
    {
        public const double EscapeRadiusSquared = 100;
        public int MaxIterations = 1000;

        public FractalMode Mode = FractalMode.Mandelbrot;
        public Complex JuliaConstant = new Complex(0, 0);

        public Fractal(FractalMode mode)
        {
            if (mode != FractalMode.Mandelbrot)
                throw new ArgumentException("Can't instantiate a julia fractal without specifying a juliaConstant");

            Mode = mode;
        }

        public Fractal(FractalMode mode, Complex juliaConstant)
        {
            Mode = mode;
            JuliaConstant = juliaConstant;
        }

        public List<Complex> EscapeSequence(Complex parameter, int length)
        {
            Complex current = parameter;

            Complex constant;
            if (Mode == FractalMode.Julia)
                constant = JuliaConstant;
            else
                constant = parameter;

            Complex next;
            double R, I;

            var sequence = new List<Complex>();
            sequence.Add(current);

            for (int i = 0; i < length; i++)
            {
                R = current.Real * current.Real;
                I = current.Imaginary * current.Imaginary;

                if (R + I > EscapeRadiusSquared)
                    break;

                next = new Complex(
                    R - I + constant.Real,
                    2 * current.Real * current.Imaginary + constant.Imaginary);

                current = next;

                sequence.Add(current);
            }

            return sequence;
        }

        [DllImport("EscapeCount.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern double escapeCount(double param_real, double param_imag, double const_real, double const_imag, int maxIterations);

        public double EscapeCount(Complex parameter, bool normalizeResult = true)
        {
            Complex constant;
            if (Mode == FractalMode.Julia)
                constant = JuliaConstant;
            else
                constant = parameter;

            double normalizedEscapeCount = escapeCount(parameter.Real, parameter.Imaginary, constant.Real, constant.Imaginary, MaxIterations);

            if (normalizedEscapeCount == -1)
                return Double.PositiveInfinity;
            else
                return normalizedEscapeCount;
        }

        /*public double EscapeCount(Complex parameter, bool normalizeResult = true)
        {
            Complex constant;
            if (Mode == FractalMode.Julia)
                constant = JuliaConstant;
            else
                constant = parameter;

            double real = parameter.Real;
            double imag = parameter.Imaginary;

            double R = 0;
            double I = 0;
            double ri = 0;

            int iterationCount;

            for (iterationCount = 0; iterationCount < MaxIterations; iterationCount++)
            {
                R = real * real;
                I = imag * imag;
                ri = real * imag;

                var distSquared = R + I;

                if (distSquared > EscapeRadiusSquared)
                {
                    break;
                }

                real = R - I + constant.Real;
                imag = 2 * ri + constant.Imaginary;
            }

            if (iterationCount == MaxIterations)
            {
                return Double.PositiveInfinity;
            }

            if (normalizeResult)
            {
                var normalizedIterationCount = iterationCount - Math.Log(Math.Log(R + I), 2) + 1;
                return normalizedIterationCount;
            }
            else
            {
                return iterationCount;
            }
        }

        /*public double EscapeCount(Complex parameter, bool normalizeResult = true)
        {
            var m = 4;
            var p = 32;

            var s = p - m; // Scaling factor

            Complex constant;
            if (Mode == FractalMode.Julia)
                constant = JuliaConstant;
            else
                constant = parameter;

            long cr = (long)Math.Round(constant.Real * Math.Pow(2, s));
            long ci = (long)Math.Round(constant.Imaginary * Math.Pow(2, s));

            long pr = (long)Math.Round(parameter.Real * Math.Pow(2, s));
            long pi = (long)Math.Round(parameter.Imaginary * Math.Pow(2, s));

            long qr = 0;
            long qi = 0;

            int iterationCount;
            for (iterationCount = 0; iterationCount < MaxIterations; iterationCount++)
            {
                var rr = pr * pr;
                var ii = pi * pi;

                qr = ((rr - ii) >> s) + cr;
                qi = ((2 * pr * pi) >> s) + ci;

                //rr = ((double)qr) / Math.Pow(2, s);
                //ri = ((double)qi) / Math.Pow(2, s);

                if (((rr + ii) >> (2 * s)) > 9)
                    break;

                pr = qr;
                pi = qi;
            }

            if (iterationCount == MaxIterations)
            {
                return Double.PositiveInfinity;
            }

            if (normalizeResult)
            {
                var normalizedIterationCount = iterationCount - Math.Log(Math.Log((double)((pr * pr + pi * pi) >> s) / Math.Pow(2, s), 2)) + 1;
                return normalizedIterationCount;
            }
            else
            {
                return iterationCount;
            }
        }

        public double EscapeCount(Complex parameter, bool normalizeResult = true)
        {
            var m = 4;
            var p = 32;

            var s = p - m; // Scaling factor

            Complex constant;
            if (Mode == FractalMode.Julia)
                constant = JuliaConstant;
            else
                constant = parameter;

            decimal cr = (decimal)constant.Real;
            decimal ci = (decimal)constant.Imaginary;

            decimal pr = (decimal)parameter.Real;
            decimal pi = (decimal)parameter.Imaginary;

            decimal qr = 0;
            decimal qi = 0;

            int iterationCount;
            for (iterationCount = 0; iterationCount < MaxIterations; iterationCount++)
            {
                decimal rr = pr * pr;
                decimal ii = pi * pi;

                qr = (rr - ii) + cr;
                qi = (2 * pr * pi) + ci;

                //rr = ((double)qr) / Math.Pow(2, s);
                //ri = ((double)qi) / Math.Pow(2, s);

                if ((rr + ii) > 16)
                    break;

                pr = qr;
                pi = qi;
            }

            if (iterationCount == MaxIterations)
            {
                return Double.PositiveInfinity;
            }

            if (normalizeResult)
            {
                var normalizedIterationCount = iterationCount - Math.Log(Math.Log((double)(pr * pr + pi * pi), 2)) + 1;
                return normalizedIterationCount;
            }
            else
            {
                return iterationCount;
            }
        }*/
    }
}
