namespace NAudio.Dsd.Dsd2Pcm
{
    public class SampleFloat64Resampler
    {
        private readonly double _ratio;                // SourceRate / TargetRate
        private readonly int _filterLength;            // FIR length
        private readonly int _numPhases;               // Polyphase table size
        private readonly double[][] _filterBank;       // [phase][tap]
        private readonly List<double> _inputBuffer;

        public SampleFloat64Resampler(int sourceRate, int targetRate, int filterLength = 64 * 2, int numPhases = 1024 * 2)
        {
            if (sourceRate <= 0 || targetRate <= 0)
                throw new ArgumentException("Invalid sample rates.");

            _ratio = (double)sourceRate / targetRate;
            _filterLength = filterLength;   // Quality vs. speed trade-off
            _numPhases = numPhases;         // More phases = smoother fractional steps

            double cutoff = 0.45 * Math.Min(sourceRate, targetRate) / sourceRate;
            _filterBank = BuildPolyphaseBank(_filterLength, _numPhases, cutoff);
            _inputBuffer = [];
        }

        public double[] Resample(double[] inputSamples)
        {
            _inputBuffer.AddRange(inputSamples);
            int inputCount = _inputBuffer.Count;
            int estimatedOutput = (int)Math.Ceiling(inputCount / _ratio);
            int outputIndex = 0;
            double inputIndex = 0.0;
            double[] output = new double[estimatedOutput];

            while (true)
            {
                int baseIndex = (int)Math.Floor(inputIndex);
                if (baseIndex + _filterLength >= inputCount)
                    break; // Not enough samples left to process

                double frac = inputIndex - baseIndex;
                int phaseIndex = (int)(frac * _numPhases) % _numPhases;
                double[] taps = _filterBank[phaseIndex];

                double sum = 0d;
                for (int i = 0; i < _filterLength; i++)
                {
                    sum += _inputBuffer[baseIndex +i] * taps[i];
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
                    double x = i - (length - 1) / 2.0 - frac;
                    double v = (x == 0)
                        ? 2 * cutoff
                        : Math.Sin(2 * Math.PI * cutoff * x) / (Math.PI * x);

                    // Hamming window
                    v *= 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (length - 1));
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
