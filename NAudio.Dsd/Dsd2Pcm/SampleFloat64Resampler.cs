namespace NAudio.Dsd.Dsd2Pcm
{
    public class SampleFloat64Resampler
    {
        private readonly object _lock;
        private readonly double _ratio;                 // SourceRate / TargetRate
        private readonly int _filterLength;             // FIR length
        private readonly int _numPhases;                // Polyphase table size
        private readonly double[][] _filterBank;        // [phase][tap]
        private readonly List<double> _inputBuffer;

        /// <summary>
        /// Initializes a new sample float-64 resampler for converting between sample rates.
        /// </summary>
        /// <param name="sourceRate">Source sample rate</param>
        /// <param name="targetRate">Tatget sample rate</param>
        /// <param name="filterLength">FIR length (default: 64)</param>
        /// <param name="numPhases">Num phases (default: 1024)</param>
        /// <exception cref="ArgumentException"></exception>
        public SampleFloat64Resampler(int sourceRate, int targetRate, int filterLength = 64, int numPhases = 1024)
        {
            if (sourceRate <= 0 || targetRate <= 0)
                throw new ArgumentException("Invalid sample rates.");

            _lock = new object();
            _ratio = (double)sourceRate / targetRate;
            _filterLength = filterLength;   // Quality vs. speed trade-off
            _numPhases = numPhases;         // More phases = smoother fractional steps

            double cutoff = 0.45f * MathF.Min(sourceRate, targetRate) / sourceRate;
            _filterBank = BuildPolyphaseBank(_filterLength, _numPhases, cutoff);
            _inputBuffer = [];
        }

        /// <summary>
        /// Resample to target sample rate
        /// </summary>
        /// <param name="inputSamples">Input samples</param>
        /// <returns>A array of output samples</returns>
        public double[] Resample(double[] inputSamples)
        {
            lock (_lock)
            {
                _inputBuffer.AddRange(inputSamples);
                int inputCount = _inputBuffer.Count;
                int estimatedOutput = (int)Math.Ceiling(inputCount / _ratio);
                int outputIndex = 0;
                double inputIndex = 0.0f;
                double[] output = new double[estimatedOutput];

                while (true)
                {
                    int baseIndex = (int)Math.Floor(inputIndex);
                    if (baseIndex + _filterLength >= inputCount)
                        break; // Not enough samples left to process

                    double frac = inputIndex - baseIndex;
                    int phaseIndex = (int)(frac * _numPhases) % _numPhases;
                    double[] taps = _filterBank[phaseIndex];

                    double sum = 0f;
                    for (int i = 0; i < _filterLength; i++)
                    {
                        sum += _inputBuffer[baseIndex + i] * taps[i];
                    }

                    output[outputIndex++] = sum;
                    inputIndex += _ratio;
                }

                // Remove processed samples from buffers
                _inputBuffer.RemoveRange(0, Math.Min((int)Math.Floor(inputIndex), inputCount));

                // Trim output to actual size
                if (outputIndex < output.Length) Array.Resize(ref output, outputIndex);

                return output;
            }
        }

        /// <summary>
        /// Clears the input buffer and discards any unprocessed data
        /// </summary>
        public void Reset()
        {
            lock (_lock) _inputBuffer.Clear();
        }

        private static double[][] BuildPolyphaseBank(int length, int phases, double cutoff)
        {
            double[][] bank = new double[phases][];
            for (int p = 0; p < phases; p++)
            {
                double frac = (double)p / phases;
                bank[p] = new double[length];

                double sum = 0;
                for (int i = 0; i < length; i++)
                {
                    double x = i - (length - 1) / 2.0f - frac;
                    double v = (x == 0)
                        ? 2 * cutoff
                        : Math.Sin(2 * Math.PI * cutoff * x) / (Math.PI * x);

                    // Hamming window
                    v *= 0.54f - 0.46f * MathF.Cos(2 * MathF.PI * i / (length - 1));
                    bank[p][i] = v;
                    sum += v;
                }

                // Normalize DC gain
                for (int i = 0; i < length; i++)
                    bank[p][i] /= sum;
            }
            return bank;
        }
    }
}
