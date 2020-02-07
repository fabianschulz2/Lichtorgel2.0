using Accord.Math;
using Accord.Math.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lichtorgel2._0
{
    class FFT
    {
        public double[] Run(double[] data)
        {
            double[] fft = new double[data.Length];

            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];

            for (int i = 0; i < data.Length; i++)

                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);

            FourierTransform2.FFT(fftComplex, FourierTransform.Direction.Forward);

            for (int i = 0; i < data.Length; i++)

                fft[i] = fftComplex[i].Magnitude;

            return fft;

        }
    }
}
