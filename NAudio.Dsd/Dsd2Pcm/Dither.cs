namespace NAudio.Dsd.Dsd2Pcm
{
    public class Dither
    {
        private readonly Random _random;
        private readonly float _quantizationStep;
        private float _previousError1;
        private float _previousError2;

        public delegate float DitherFunction(float input);
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
            _previousError1 = 0.0f;
            _previousError2 = 0.0f;
            _quantizationStep = 1.0f / (MathF.Pow(2, bits) - 1);

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
            _previousError1 = 0.0f;
            _previousError2 = 0.0f;
        }

        #region Dithering Algorithms
        private float ApplyRectangularDither(float input)
        {
            // RPDF (Rectangular Probability Density Function)
            float noise = (_random.NextSingle() - 0.5f) * _quantizationStep;
            return input + noise;
        }

        private float ApplyTriangularPdfDither(float input)
        {
            // TPDF (Triangular PDF) - sum of two RPDF noises
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            return input + r1 + r2;
        }

        private float ApplyHighPassTriangularPdfDither(float input)
        {
            // High-pass filtered TPDF
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r3 = (_random.NextSingle() - 0.5f) * _quantizationStep;

            return input + r1 + r2 - r3;
        }

        private float ApplyModifiedHighPassTriangularPdfDither(float input)
        {
            // Modified high-pass TPDF with different coefficients
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r3 = (_random.NextSingle() - 0.5f) * _quantizationStep;

            return input + 0.75f * r1 + 0.75f * r2 - 0.5f * r3;
        }

        private float ApplyLipshitzMinimallyAudibleDither(float input)
        {
            // Lipshitz's minimally audible noise shaping
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;

            // Simple noise shaping
            float shapedNoise = r1 + r2 - _previousError1;
            _previousError1 = r1;

            return input + shapedNoise;
        }

        private float ApplyFWeightedDither(float input)
        {
            // F-weighted noise shaping (approximation)
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;

            // First-order high-pass filter
            float shapedNoise = r1 + r2 - 0.5f * _previousError1;
            _previousError1 = shapedNoise;

            return input + shapedNoise;
        }

        private float ApplyVanderkooyLipshitzDither(float input)
        {
            // Vanderkooy-Lipshitz noise shaping
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;

            // Second-order noise shaping
            float shapedNoise = r1 + r2 - 1.6f * _previousError1 + 0.64f * _previousError2;
            _previousError2 = _previousError1;
            _previousError1 = shapedNoise;

            return input + shapedNoise;
        }

        private float ApplySoPdfDither(float input)
        {
            // Second-order PDF dither
            float r1 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r2 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r3 = (_random.NextSingle() - 0.5f) * _quantizationStep;
            float r4 = (_random.NextSingle() - 0.5f) * _quantizationStep;

            return input + r1 + r2 + r3 + r4;
        }
        #endregion
    }
}
