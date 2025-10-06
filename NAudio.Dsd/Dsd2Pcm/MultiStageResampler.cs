namespace NAudio.Dsd.Dsd2Pcm
{
    public class MultiStageResampler
    {
        private readonly List<SampleFloat64Resampler> _stages = [];

        public List<(int, int)> Steps { get; }

        public MultiStageResampler(int inputRate, int outputRate)
        {
            if (inputRate < 44100 || outputRate < 44100)
            {
                throw new AggregateException("Invalid sample rates.");
            }

            Steps = GetIntermediates(inputRate, outputRate);
            foreach (var item in Steps)
            {
                _stages.Add(new SampleFloat64Resampler(item.Item1, item.Item2, 64 * 2, 1024 * 2));
            }
        }

        public double[] Resample(double[] input)
        {
            double[] buffer = input;
            foreach (var stage in _stages)
                buffer = stage.Resample(buffer);
            return buffer;
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

        public static List<(int, int)> GetIntermediates(int inputRate, int outputRate)
        {
            List<(int, int)> intermediate = [];
            int max, min;
            bool downsampling = inputRate >= outputRate;
            (max, min) = downsampling ? (inputRate, outputRate) : (outputRate, inputRate);

            if (max % min == 0)
            {
                if (downsampling)
                {
                    while (max != min)
                    {
                        intermediate.Add((max ,max /= 2));
                    }
                }
                else
                {
                    while (max != min)
                    {
                        intermediate.Add((min, min *= 2));
                    }
                }
            }
            else
            {
                int tmp = max % 44100 == 0 ? 44100 : 48000;
                while (tmp < min) 
                {
                    tmp *= 2;
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
