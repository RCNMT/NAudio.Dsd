namespace NAudio.Dsd.Dsd2Pcm
{
    public static class Filter
    {
        /// <summary>
        /// Designs a symmetric FIR low-pass filter using Hamming window.
        /// </summary>
        /// <param name="dsdRate">Input DSD sample rate (Hz)</param>
        /// <param name="pcmRate">Output PCM sample rate (Hz)</param>
        /// <param name="taps">Number of FIR taps</param>
        /// <param name="cutoffFactor">Normalized cutoff factor (e.g. 0.40, 0.45, 0.49)</param>
        /// <returns>Array of FIR coefficients (double[])</returns>
        public static double[] DesignHammingFir(int dsdRate, int pcmRate, int taps, double cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            double[] h = new double[taps];

            // Normalized cutoff frequency
            double fc = cutoffFactor * pcmRate / dsdRate;

            for (int n = 0; n < M; n++)
            {
                double x = n - mid;

                // Sinc function (ideal low-pass)
                double sinc = (x == 0.0) ? 2.0f * fc : Math.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                double window = HammingWindow(n, M);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            double sum = 0;
            for (int n = 0; n < taps; n++) sum += h[n];
            for (int n = 0; n < taps; n++) h[n] /= sum;

            return h;
        }

        /// <summary>
        /// Designs a symmetric FIR low-pass filter using Kaiser window.
        /// </summary>
        /// <param name="dsdRate">Input DSD sample rate (Hz)</param>
        /// <param name="pcmRate">Output PCM sample rate (Hz)</param>
        /// <param name="taps">Number of FIR taps</param>
        /// <param name="attenuationDb">Stopband attenuation in dB (e.g. 60, 80, 100)</param>
        /// <param name="cutoffFactor">Normalized cutoff factor (e.g. 0.40, 0.45, 0.49)</param>
        /// <returns>Array of FIR coefficients (double[])</returns>
        public static double[] DesignKaiserFir(int dsdRate, int pcmRate, int taps, double attenuationDb = 80, double cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            double[] h = new double[taps];

            // Normalized cutoff frequency
            double fc = cutoffFactor * pcmRate / dsdRate;

            // Approximate Kaiser beta
            double beta = ComputeKaiserBeta(attenuationDb);

            for (int n = 0; n < M; n++)
            {
                int x = n - mid;

                // Sinc function (ideal low-pass)
                double sinc = (x == 0) ? 2.0f * fc : Math.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                double window = KaiserWindow(n, M, beta);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            double sum = 0;
            foreach (var v in h) sum += v;
            for (int i = 0; i < taps; i++) h[i] /= sum;

            return h;
        }

        /// <summary>
        /// Designs a symmetric FIR low-pass filter using Blackman window.
        /// </summary>
        /// <param name="dsdRate">Input DSD sample rate (Hz)</param>
        /// <param name="pcmRate">Output PCM sample rate (Hz)</param>
        /// <param name="taps">Number of FIR taps</param>
        /// <param name="cutoffFactor">Normalized cutoff factor (e.g. 0.40, 0.45, 0.49)</param>
        /// <returns>Array of FIR coefficients (double[])</returns>
        public static double[] DesignBlackmanFir(int dsdRate, int pcmRate, int taps, double cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            double[] h = new double[taps];

            // Normalized cutoff frequency
            double fc = cutoffFactor * pcmRate / dsdRate;

            for (int n = 0; n < M; n++)
            {
                int x = n - mid;

                // Sinc function (ideal low-pass)
                double sinc = (x == 0) ? 2.0f * fc : Math.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                double window = BlackmanWindow(n, M);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            double sum = 0;
            foreach (var v in h) sum += v;
            for (int i = 0; i < taps; i++) h[i] /= sum;

            return h;
        }

        /// <summary>
        /// Designs a symmetric FIR low-pass filter using Chebyshev window.
        /// </summary>
        /// <param name="dsdRate">Input DSD sample rate (Hz)</param>
        /// <param name="pcmRate">Output PCM sample rate (Hz)</param>
        /// <param name="taps">Number of FIR taps</param>
        /// <param name="attenuationDb">Stopband attenuation in dB (e.g. 60, 80, 100)</param>
        /// <param name="cutoffFactor">Normalized cutoff factor (e.g. 0.40, 0.45, 0.49)</param>
        /// <returns>Array of FIR coefficients (double[])</returns>
        public static double[] DesignChebyshevFir(int dsdRate, int pcmRate, int taps, double attenuationDb = 80, double cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            double[] h = new double[taps];

            // Normalized cutoff frequency
            double fc = cutoffFactor * pcmRate / dsdRate;

            // Window function
            double[] window = ChebyshevWindow(M, attenuationDb);

            for (int n = 0; n < M; n++)
            {
                int x = n - mid;

                // Sinc function (ideal low-pass)
                double sinc = (x == 0) ? 2.0f * fc : Math.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Apply Chebyshev window
                h[n] = sinc * window[n];
            }

            // Normalize to unity gain
            double sum = 0;
            foreach (var v in h) sum += v;
            for (int i = 0; i < taps; i++) h[i] /= sum;

            return h;
        }

        /// <summary>
        /// Designs a symmetric FIR low-pass filter using Blackman-Harris window.
        /// </summary>
        /// <param name="dsdRate">Input DSD sample rate (Hz)</param>
        /// <param name="pcmRate">Output PCM sample rate (Hz)</param>
        /// <param name="taps">Number of FIR taps</param>
        /// <param name="cutoffFactor">Normalized cutoff factor (default 0.45)</param>
        /// <returns>Array of FIR coefficients (double[])</returns>
        public static double[] DesignBlackmanHarrisFir(int dsdRate, int pcmRate, int taps, double cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            double[] h = new double[taps];

            // Normalized cutoff frequency
            double fc = cutoffFactor * 0.5f * pcmRate / dsdRate;

            for (int n = 0; n <= M; n++)
            {
                double x = n - mid;

                // Sinc function (ideal low-pass)
                double sinc = (x == 0) ? 2.0f * fc : Math.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                double window = BlackmanHarrisWindow(n, M);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            double sum = 0;
            foreach (var v in h) sum += v;
            for (int i = 0; i < taps; i++) h[i] /= sum;

            return h;
        }

        #region Helpers
        private static double Acosh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x - 1.0f));
        }

        private static double AcoshSafe(double x)
        {
            if (x < 1.0) x = 1.0f;
            return Math.Log(x + Math.Sqrt(x * x - 1.0f));
        }

        private static double[] ChebyshevWindow(int N, double attenuationDb)
        {
            if (N < 1) throw new ArgumentException("N must be >= 1", nameof(N));
            if (N == 1) return [1.0f];

            int M = N - 1;

            double rippleLinear = Math.Pow(10.0f, attenuationDb / 20.0f);
            double tg = Math.Cosh(AcoshSafe(rippleLinear) / M);

            double[] s = new double[N];
            for (int m = 0; m <= M; ++m)
            {
                double x = tg * MathF.Cos(MathF.PI * m / N);
                double val;
                if (Math.Abs(x) > 1.0)
                {
                    val = Math.Cosh(M * Acosh(Math.Abs(x)));
                    if (x < 0 && (M % 2 == 1)) val = -val;
                }
                else
                {
                    val = Math.Cos(M * Math.Acos(x));
                }
                s[m] = val;
            }

            double[] w = new double[N];
            for (int n = 0; n < N; ++n)
            {
                double sum = s[0];
                for (int m = 1; m <= M; ++m)
                {
                    sum += 2.0f * s[m] * MathF.Cos(2.0f * MathF.PI * m * n / N);
                }
                w[n] = sum / N;
            }

            double max = 0.0f;
            for (int i = 0; i < N; ++i)
            {
                double av = Math.Abs(w[i]);
                if (av > max) max = av;
            }
            if (max <= 0.0) max = 1.0f;
            for (int i = 0; i < N; ++i) w[i] /= max;

            return w;
        }

        private static double HammingWindow(int n, int N)
        {
            return 0.54f - 0.46f * MathF.Cos(2 * MathF.PI * n / (N - 1));
        }

        private static double KaiserWindow(int n, int N, double beta)
        {
            double ratio = (2.0f * n) / (N - 1) - 1.0f;
            double arg = beta * Math.Sqrt(1.0f - ratio * ratio);
            return BesselI0(arg) / BesselI0(beta);
        }

        private static double BlackmanWindow(int n, int N)
        {
            const double a0 = 7938.0f / 18608.0f;
            const double a1 = 9240.0f / 18608.0f;
            const double a2 = 1430.0f / 18608.0f;

            double term1 = 2.0f * MathF.PI * n / (N - 1);
            double term2 = 4.0f * MathF.PI * n / (N - 1);

            return a0 - a1 * Math.Cos(term1) + a2 * Math.Cos(term2);
        }

        private static double BlackmanHarrisWindow(int n, int N)
        {
            const double a0 = 0.35875f;
            const double a1 = 0.48829f;
            const double a2 = 0.14128f;
            const double a3 = 0.01168f;

            return a0 - a1 * MathF.Cos(2.0f * MathF.PI * n / N)
                      + a2 * MathF.Cos(4.0f * MathF.PI * n / N)
                      - a3 * MathF.Cos(6.0f * MathF.PI * n / N);
        }

        private static double ComputeKaiserBeta(double attenuationDb)
        {
            if (attenuationDb > 50) return 0.1102f * (attenuationDb - 8.7f);
            if (attenuationDb >= 21) return 0.5842f * Math.Pow(attenuationDb - 21, 0.4f) + 0.07886f * (attenuationDb - 21);
            return 0.0f;
        }

        private static double BesselI0(double x)
        {
            double sum = 1.0f;
            double y = x * x / 4.0f;
            double t = y;
            int k = 1;

            while (t > 1e-10)
            {
                sum += t;
                k++;
                t *= y / (k * k);
            }

            return sum;
        }
        #endregion
    }
}
