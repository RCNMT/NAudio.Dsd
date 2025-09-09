namespace NAudio.Dsd.Dsd2Dxd
{
    public class Dither
    {
        private readonly Random _random;
        private readonly double _quantizationStep;
        private double _previousError1;
        private double _previousError2;

        public delegate double DitherFunction(double input);
        public readonly DitherFunction ApplyDither;

        /// <summary>
        /// Initializes a new instance of the Dither class with the specified dithering algorithm, bit depth, and optional random seed.
        /// </summary>
        /// <param name="ditherType">The type of dithering algorithm to apply.</param>
        /// <param name="bits">The target bit depth for quantization (e.g., 16 for 16-bit audio).</param>
        /// <param name="seed">Optional seed for the random number generator to ensure reproducibility.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported dithering type is specified.</exception>
        public Dither(DitherType ditherType, int bits, int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _previousError1 = 0.0;
            _previousError2 = 0.0;
            _quantizationStep = 1.0 / (Math.Pow(2, bits) - 1);

            ApplyDither = ditherType switch
            {
                DitherType.None => (input) => input,
                DitherType.SoPDF => ApplySoPdfDither,
                DitherType.FWeighted => ApplyFWeightedDither,
                DitherType.Rectangular => ApplyRectangularDither,
                DitherType.TriangularPDF => ApplyTriangularPdfDither,
                DitherType.VanderkooyLipshitz => ApplyVanderkooyLipshitzDither,
                DitherType.HighPassTriangularPDF => ApplyHighPassTriangularPdfDither,
                DitherType.LipshitzMinimallyAudible => ApplyLipshitzMinimallyAudibleDither,
                DitherType.ModifiedHighPassTriangularPDF => ApplyModifiedHighPassTriangularPdfDither,
                _ => throw new ArgumentOutOfRangeException(nameof(ditherType), "Unsupported dither type"),
            };
        }

        public void Reset()
        {
            _previousError1 = 0.0;
            _previousError2 = 0.0;
        }

        #region Dithering Algorithms
        private double ApplyRectangularDither(double input)
        {
            // RPDF (Rectangular Probability Density Function)
            double noise = (_random.NextDouble() - 0.5) * _quantizationStep;
            return input + noise;
        }

        private double ApplyTriangularPdfDither(double input)
        {
            // TPDF (Triangular PDF) - sum of two RPDF noises
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;
            return input + r1 + r2;
        }

        private double ApplyHighPassTriangularPdfDither(double input)
        {
            // High-pass filtered TPDF
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r3 = (_random.NextDouble() - 0.5) * _quantizationStep;

            return input + r1 + r2 - r3;
        }

        private double ApplyModifiedHighPassTriangularPdfDither(double input)
        {
            // Modified high-pass TPDF with different coefficients
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r3 = (_random.NextDouble() - 0.5) * _quantizationStep;

            return input + 0.75 * r1 + 0.75 * r2 - 0.5 * r3;
        }

        private double ApplyLipshitzMinimallyAudibleDither(double input)
        {
            // Lipshitz's minimally audible noise shaping
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;

            // Simple noise shaping
            double shapedNoise = r1 + r2 - _previousError1;
            _previousError1 = r1;

            return input + shapedNoise;
        }

        private double ApplyFWeightedDither(double input)
        {
            // F-weighted noise shaping (approximation)
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;

            // First-order high-pass filter
            double shapedNoise = r1 + r2 - 0.5 * _previousError1;
            _previousError1 = shapedNoise;

            return input + shapedNoise;
        }

        private double ApplyVanderkooyLipshitzDither(double input)
        {
            // Vanderkooy-Lipshitz noise shaping
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;

            // Second-order noise shaping
            double shapedNoise = r1 + r2 - 1.6 * _previousError1 + 0.64 * _previousError2;
            _previousError2 = _previousError1;
            _previousError1 = shapedNoise;

            return input + shapedNoise;
        }

        private double ApplySoPdfDither(double input)
        {
            // Second-order PDF dither
            double r1 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r2 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r3 = (_random.NextDouble() - 0.5) * _quantizationStep;
            double r4 = (_random.NextDouble() - 0.5) * _quantizationStep;

            return input + r1 + r2 + r3 + r4;
        }
        #endregion
    }
}
