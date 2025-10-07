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
        /// <returns>Array of FIR coefficients (float[])</returns>
        public static float[] DesignHammingFir(int dsdRate, int pcmRate, int taps, float cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            float[] h = new float[taps];

            // Normalized cutoff frequency
            float fc = cutoffFactor * pcmRate / dsdRate;

            for (int n = 0; n < M; n++)
            {
                float x = n - mid;

                // Sinc function (ideal low-pass)
                float sinc = (x == 0.0) ? 2.0f * fc : MathF.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                float window = HammingWindow(n, M);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            float sum = 0;
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
        /// <returns>Array of FIR coefficients (float[])</returns>
        public static float[] DesignKaiserFir(int dsdRate, int pcmRate, int taps, float attenuationDb = 80, float cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            float[] h = new float[taps];

            // Normalized cutoff frequency
            float fc = cutoffFactor * pcmRate / dsdRate;

            // Approximate Kaiser beta
            float beta = ComputeKaiserBeta(attenuationDb);

            for (int n = 0; n < M; n++)
            {
                int x = n - mid;

                // Sinc function (ideal low-pass)
                float sinc = (x == 0) ? 2.0f * fc : MathF.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                float window = KaiserWindow(n, M, beta);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            float sum = 0;
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
        /// <returns>Array of FIR coefficients (float[])</returns>
        public static float[] DesignBlackmanFir(int dsdRate, int pcmRate, int taps, float cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            float[] h = new float[taps];

            // Normalized cutoff frequency
            float fc = cutoffFactor * pcmRate / dsdRate;

            for (int n = 0; n < M; n++)
            {
                int x = n - mid;

                // Sinc function (ideal low-pass)
                float sinc = (x == 0) ? 2.0f * fc : MathF.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                float window = BlackmanWindow(n, M);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            float sum = 0;
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
        /// <returns>Array of FIR coefficients (float[])</returns>
        public static float[] DesignChebyshevFir(int dsdRate, int pcmRate, int taps, float attenuationDb = 80, float cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            float[] h = new float[taps];

            // Normalized cutoff frequency
            float fc = cutoffFactor * pcmRate / dsdRate;

            // Window function
            float[] window = ChebyshevWindow(M, attenuationDb);

            for (int n = 0; n < M; n++)
            {
                int x = n - mid;

                // Sinc function (ideal low-pass)
                float sinc = (x == 0) ? 2.0f * fc : MathF.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Apply Chebyshev window
                h[n] = sinc * window[n];
            }

            // Normalize to unity gain
            float sum = 0;
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
        /// <returns>Array of FIR coefficients (float[])</returns>
        public static float[] DesignBlackmanHarrisFir(int dsdRate, int pcmRate, int taps, float cutoffFactor = 0.45f)
        {
            if (taps % 2 == 0) taps++;
            int M = taps - 1;
            int mid = M / 2;
            float[] h = new float[taps];

            // Normalized cutoff frequency
            float fc = cutoffFactor * 0.5f * pcmRate / dsdRate;

            for (int n = 0; n <= M; n++)
            {
                float x = n - mid;

                // Sinc function (ideal low-pass)
                float sinc = (x == 0) ? 2.0f * fc : MathF.Sin(2.0f * MathF.PI * fc * x) / (MathF.PI * x);

                // Window function
                float window = BlackmanHarrisWindow(n, M);
                h[n] = sinc * window;
            }

            // Normalize to unity gain
            float sum = 0;
            foreach (var v in h) sum += v;
            for (int i = 0; i < taps; i++) h[i] /= sum;

            return h;
        }

        #region Helpers
        private static float Acosh(float x)
        {
            return MathF.Log(x + MathF.Sqrt(x * x - 1.0f));
        }

        private static float AcoshSafe(float x)
        {
            if (x < 1.0) x = 1.0f;
            return MathF.Log(x + MathF.Sqrt(x * x - 1.0f));
        }

        private static float[] ChebyshevWindow(int N, float attenuationDb)
        {
            if (N < 1) throw new ArgumentException("N must be >= 1", nameof(N));
            if (N == 1) return [1.0f];

            int M = N - 1;

            float rippleLinear = MathF.Pow(10.0f, attenuationDb / 20.0f);
            float tg = MathF.Cosh(AcoshSafe(rippleLinear) / M);

            float[] s = new float[N];
            for (int m = 0; m <= M; ++m)
            {
                float x = tg * MathF.Cos(MathF.PI * m / N);
                float val;
                if (Math.Abs(x) > 1.0)
                {
                    val = MathF.Cosh(M * Acosh(MathF.Abs(x)));
                    if (x < 0 && (M % 2 == 1)) val = -val;
                }
                else
                {
                    val = MathF.Cos(M * MathF.Acos(x));
                }
                s[m] = val;
            }

            float[] w = new float[N];
            for (int n = 0; n < N; ++n)
            {
                float sum = s[0];
                for (int m = 1; m <= M; ++m)
                {
                    sum += 2.0f * s[m] * MathF.Cos(2.0f * MathF.PI * m * n / N);
                }
                w[n] = sum / N;
            }

            float max = 0.0f;
            for (int i = 0; i < N; ++i)
            {
                float av = Math.Abs(w[i]);
                if (av > max) max = av;
            }
            if (max <= 0.0) max = 1.0f;
            for (int i = 0; i < N; ++i) w[i] /= max;

            return w;
        }

        private static float HammingWindow(int n, int N)
        {
            return 0.54f - 0.46f * MathF.Cos(2 * MathF.PI * n / (N - 1));
        }

        private static float KaiserWindow(int n, int N, float beta)
        {
            float ratio = (2.0f * n) / (N - 1) - 1.0f;
            float arg = beta * MathF.Sqrt(1.0f - ratio * ratio);
            return BesselI0(arg) / BesselI0(beta);
        }

        private static float BlackmanWindow(int n, int N)
        {
            const float a0 = 7938.0f / 18608.0f;
            const float a1 = 9240.0f / 18608.0f;
            const float a2 = 1430.0f / 18608.0f;

            float term1 = 2.0f * MathF.PI * n / (N - 1);
            float term2 = 4.0f * MathF.PI * n / (N - 1);

            return a0 - a1 * MathF.Cos(term1) + a2 * MathF.Cos(term2);
        }

        private static float BlackmanHarrisWindow(int n, int N)
        {
            const float a0 = 0.35875f;
            const float a1 = 0.48829f;
            const float a2 = 0.14128f;
            const float a3 = 0.01168f;

            return a0 - a1 * MathF.Cos(2.0f * MathF.PI * n / N)
                      + a2 * MathF.Cos(4.0f * MathF.PI * n / N)
                      - a3 * MathF.Cos(6.0f * MathF.PI * n / N);
        }

        private static float ComputeKaiserBeta(float attenuationDb)
        {
            if (attenuationDb > 50) return 0.1102f * (attenuationDb - 8.7f);
            if (attenuationDb >= 21) return 0.5842f * MathF.Pow(attenuationDb - 21, 0.4f) + 0.07886f * (attenuationDb - 21);
            return 0.0f;
        }

        private static float BesselI0(float x)
        {
            float sum = 1.0f;
            float y = x * x / 4.0f;
            float t = y;
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
