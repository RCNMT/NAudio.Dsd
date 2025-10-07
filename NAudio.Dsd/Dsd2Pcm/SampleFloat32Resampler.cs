namespace NAudio.Dsd.Dsd2Pcm
{
    public class SampleFloat32Resampler
    {
        private readonly object _lock;
        private readonly float _ratio;                  // SourceRate / TargetRate
        private readonly int _filterLength;             // FIR length
        private readonly int _numPhases;                // Polyphase table size
        private readonly float[][] _filterBank;         // [phase][tap]
        private readonly List<float> _inputBuffer;

        public SampleFloat32Resampler(int sourceRate, int targetRate, int filterLength = 64, int numPhases = 1024)
        {
            if (sourceRate <= 0 || targetRate <= 0)
                throw new ArgumentException("Invalid sample rates.");

            _lock = new object();
            _ratio = (float)sourceRate / targetRate;
            _filterLength = filterLength;   // Quality vs. speed trade-off
            _numPhases = numPhases;         // More phases = smoother fractional steps

            float cutoff = 0.45f * MathF.Min(sourceRate, targetRate) / sourceRate;
            _filterBank = BuildPolyphaseBank(_filterLength, _numPhases, cutoff);
            _inputBuffer = [];
        }

        public float[] Resample(float[] inputSamples)
        {
            lock (_lock)
            {
                _inputBuffer.AddRange(inputSamples);
                int inputCount = _inputBuffer.Count;
                int estimatedOutput = (int)MathF.Ceiling(inputCount / _ratio);
                int outputIndex = 0;
                float inputIndex = 0.0f;
                float[] output = new float[estimatedOutput];

                while (true)
                {
                    int baseIndex = (int)MathF.Floor(inputIndex);
                    if (baseIndex + _filterLength >= inputCount)
                        break; // Not enough samples left to process

                    float frac = inputIndex - baseIndex;
                    int phaseIndex = (int)(frac * _numPhases) % _numPhases;
                    float[] taps = _filterBank[phaseIndex];

                    float sum = 0f;
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

        public void Reset()
        {
            lock (_lock) _inputBuffer.Clear();
        }

        private static float[][] BuildPolyphaseBank(int length, int phases, float cutoff)
        {
            float[][] bank = new float[phases][];
            for (int p = 0; p < phases; p++)
            {
                float frac = (float)p / phases;
                bank[p] = new float[length];

                float sum = 0;
                for (int i = 0; i < length; i++)
                {
                    float x = i - (length - 1) / 2.0f - frac;
                    float v = (x == 0)
                        ? 2 * cutoff
                        : MathF.Sin(2 * MathF.PI * cutoff * x) / (MathF.PI * x);

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
