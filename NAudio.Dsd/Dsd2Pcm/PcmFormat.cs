namespace NAudio.Dsd.Dsd2Pcm
{
    public enum PcmFormat
    {
        /// <summary>
        /// Represents the PCM44.1 audio format, a standard digital audio format with a sampling rate of 44.1 kHz.
        /// </summary>
        PCM44_1 = 1,
        /// <summary>
        /// Represents the PCM88.2 audio format, a high-resolution digital audio format with a sampling rate of 88.2 kHz.
        /// </summary>
        PCM88_2 = 2,
        /// <summary>
        /// Represents the PCM176.4 audio format, a high-resolution digital audio format with a sampling rate of 176.4 kHz.
        /// </summary>
        PCM176_4 = 4,
        /// <summary>
        /// Represnts the PCM352.8 audio format, a high-resolution digital audio format with a sampling rate of 352.8 kHz.
        /// </summary>
        PCM352_8 = 8,
        /// <summary>
        /// Represents the PCM705.6 audio format, a high-resolution digital audio format with a sampling rate of 705.6 kHz.
        /// </summary>
        PCM705_6 = 16,
    }

    public static class PcmFormatExtensions
    {
        /// <summary>
        /// Gets the sampling frequency in Hz for the specified PCM format.
        /// </summary>
        /// <param name="format">The PCM format.</param>
        /// <returns>The sampling frequency in Hz.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetSamplingFrequency(this PcmFormat format)
        {
            return format switch
            {
                PcmFormat.PCM44_1 => 44100,
                PcmFormat.PCM88_2 => 88200,
                PcmFormat.PCM176_4 => 176400,
                PcmFormat.PCM352_8 => 352800,
                PcmFormat.PCM705_6 => 705600,
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported PCM format"),
            };
        }
    }
}
