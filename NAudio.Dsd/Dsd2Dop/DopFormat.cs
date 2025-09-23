namespace NAudio.Dsd.Dsd2Dop
{
    public enum DopFormat
    {
        /// <summary>
        /// Represents the PCM 176.4 kHz that contain DSD64.
        /// </summary>
        DoP176_4 = 176400,
        /// <summary>
        /// Represents the PCM 352.8 kHz that contain DSD128.
        /// </summary>
        DoP352_8 = 352800,
        /// <summary>
        /// Represents the PCM 705.6 kHz that contain DSD256.
        /// </summary>
        DoP705_6 = 705600,
        /// <summary>
        /// Represents the PCM 1411.2 kHz that contain DSD512.
        /// </summary>
        DoP1411_2 = 1411200,
    }

    public static class DopFormatExtensions
    {
        /// <summary>
        /// Gets the sampling frequency in Hz for the specified DoP format.
        /// </summary>
        /// <param name="format">The DoP format.</param>
        /// <returns>The sampling frequency in Hz.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetSamplingFrequency(this DopFormat format)
        {
            return (int)format;
        }
    }
}
