namespace NAudio.Dsd.Dsd2Pcm
{
    public class MultiStageResampler
    {
        private readonly List<SampleFloat64Resampler> _stages = [];

        /// <summary>
        /// A list of tuples representing intermediate rate pairs. Each tuple contains:<br/>
        /// - Item1: The input rate for the current step<br/>
        /// - Item2: The output rate for the current step<br/>
        /// </summary>
        public List<(int, int)> ConversionSteps { get; }

        /// <summary>
        /// Initializes a new multi-stage resampler for converting between sample rates.
        /// </summary>
        /// <param name="conversionSteps">A list of tuples representing intermediate rate pairs (intputRate, outputRate) for each stage</param>
        /// <remarks>
        /// Automatically generates optimal resampling stages between the input and output rates.
        /// Each stage is configured with appropriate buffer sizes based on the rate conversion ratio.
        /// </remarks>
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

        /// <summary>
        /// Initializes a new multi-stage resampler for converting between sample rates.
        /// </summary>
        /// <param name="inputRate">Source sample rate (must be positive)</param>
        /// <param name="outputRate">Target sample rate (must be positive)</param>
        /// <exception cref="ArgumentException">Thrown when inputRate or outputRate are not positive</exception>
        /// <remarks>
        /// Automatically generates optimal resampling stages between the input and output rates.
        /// Each stage is configured with appropriate buffer sizes based on the rate conversion ratio.
        /// </remarks>
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

        /// <summary>
        /// Resample to target sample rate
        /// </summary>
        /// <param name="inputSamples">Input samples</param>
        /// <returns>A array of output samples</returns>
        public double[] Resample(double[] inputSamples)
        {
            double[] buffer = inputSamples;
            foreach (var stage in _stages)
                buffer = stage.Resample(buffer);
            return buffer;
        }

        /// <summary>
        /// Reset all internal stages
        /// </summary>
        public void Reset()
        {
            foreach (var stage in _stages) stage.Reset();
        }

        /// <summary>
        /// Create multiple Multi-stage resampler instances for multichannel processing
        /// </summary>
        /// <param name="conversionSteps">A list of tuples representing intermediate rate pairs</param>
        /// <param name="channels">Number of channels</param>
        /// <returns>A array of Multi-stage resampler instances</returns>
        public static MultiStageResampler[] CreateMultiStageResamplers(List<(int, int)> conversionSteps, int channels)
        {
            MultiStageResampler[] resamplers = new MultiStageResampler[channels];
            for (int i = 0; i < channels; ++i)
            {
                resamplers[i] = new MultiStageResampler(conversionSteps);
            }
            return resamplers;
        }

        /// <summary>
        /// Create multiple Multi-stage resampler instances for multichannel processing
        /// </summary>
        /// <param name="inputRate">The starting sample rate</param>
        /// <param name="outputRate">The target sample rate</param>
        /// <param name="channels">Number of channels</param>
        /// <returns>A array of Multi-stage resampler instances</returns>
        public static MultiStageResampler[] CreateMultiStageResamplers(int inputRate, int outputRate, int channels)
        {
            MultiStageResampler[] resamplers = new MultiStageResampler[channels];
            for (int i = 0; i < channels; ++i)
            {
                resamplers[i] = new MultiStageResampler(inputRate, outputRate);
            }
            return resamplers;
        }

        /// <summary>
        /// Reset multiple Multi-stage resampler instances for multichannel processing
        /// </summary>
        /// <param name="resamplers">Array of MultiStageResampler</param>
        public static void Reset(MultiStageResampler[] resamplers)
        {
            foreach (var item in resamplers) item.Reset();
        }

        /// <summary>
        /// Generates a sequence of intermediate rate pairs between input and output rates.
        /// </summary>
        /// <param name="inputRate">The starting sample rate</param>
        /// <param name="outputRate">The target sample rate</param>
        /// <param name="stepFactor">The factor by which to change rates at each step (default: 2)</param>
        /// <returns>
        /// A list of tuples representing intermediate rate pairs. Each tuple contains:<br/>
        /// - Item1: The input rate for the current step<br/>
        /// - Item2: The output rate for the current step<br/>
        /// The list progresses from (inputRate, intermediate) to (intermediate, outputRate)<br/>
        /// </returns>
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
