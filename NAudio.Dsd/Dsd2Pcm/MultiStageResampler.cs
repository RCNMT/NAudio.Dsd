namespace NAudio.Dsd.Dsd2Pcm
{
    public class MultiStageResampler
    {
        private readonly List<SampleFloat64Resampler> _stages = [];

        public List<(int, int)> ConversionSteps { get; }

        public MultiStageResampler(List<(int, int)> conversionSteps)
        {
            ConversionSteps = conversionSteps;
            foreach (var item in ConversionSteps)
            {
                (int max, int min) = item.Item1 >= item.Item2 ? (item.Item1, item.Item2) : (item.Item2, item.Item1);
                int n = 64 * Math.Max(1, (max / min) - 1);
                _stages.Add(new SampleFloat64Resampler(item.Item1, item.Item2, n, 1024));
            }
        }

        public MultiStageResampler(int inputRate, int outputRate)
        {
            if (inputRate <= 0 || outputRate <= 0)
            {
                throw new AggregateException("Invalid sample rates");
            }

            ConversionSteps = GetIntermediates(inputRate, outputRate, 2);
            foreach (var item in ConversionSteps)
            {
                (int max, int min) = item.Item1 >= item.Item2 ? (item.Item1, item.Item2) : (item.Item2, item.Item1);
                int n = 64 * Math.Max(1, (max / min) - 1);
                _stages.Add(new SampleFloat64Resampler(item.Item1, item.Item2, n, 1024));
            }
        }

        public double[] Resample(double[] input)
        {
            double[] buffer = input;
            foreach (var stage in _stages)
                buffer = stage.Resample(buffer);
            return buffer;
        }

        public void Reset()
        {
            foreach (var stage in _stages) stage.Reset();
        }

        public static MultiStageResampler[] CreateMultiStageResamplers(List<(int, int)> conversionSteps, int channels)
        {
            MultiStageResampler[] resamplers = new MultiStageResampler[channels];
            for (int i = 0; i < channels; ++i)
            {
                resamplers[i] = new MultiStageResampler(conversionSteps);
            }
            return resamplers;
        }

        public static MultiStageResampler[] CreateMultiStageResamplers(int inputRate, int outputRate, int channels)
        {
            MultiStageResampler[] resamplers = new MultiStageResampler[channels];
            for (int i = 0; i < channels; ++i)
            {
                resamplers[i] = new MultiStageResampler(inputRate, outputRate);
            }
            return resamplers;
        }

        public static void Reset(MultiStageResampler[] resamplers)
        {
            foreach (var item in resamplers) item.Reset();
        }

        public static List<(int, int)> GetIntermediates(int inputRate, int outputRate, int stepFactor = 2)
        {
            if (stepFactor < 2) stepFactor = 2;

            List<(int, int)> intermediate = [];
            int max, min;
            bool downsampling = inputRate >= outputRate;
            (max, min) = downsampling ? (inputRate, outputRate) : (outputRate, inputRate);

            if (max % min == 0)
            {
                if (downsampling)
                {
                    while (max > min)
                    {
                        intermediate.Add((max, Math.Max(max /= stepFactor, min)));
                    }
                }
                else
                {
                    while (max > min)
                    {
                        intermediate.Add((min, Math.Min(min *= stepFactor, max)));
                    }
                }
            }
            else
            {
                int tmp = max % 11025 == 0 ? 44100 : 48000;
                while (tmp < min)
                {
                    tmp *= stepFactor;
                }
                if (downsampling)
                {
                    intermediate = GetIntermediates(max, tmp);
                    intermediate.Add((tmp, min));
                }
                else
                {
                    intermediate = GetIntermediates(tmp, max);
                    intermediate.Insert(0, (min, tmp));
                }
            }

            return intermediate;
        }
    }
}
