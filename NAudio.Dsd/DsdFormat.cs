namespace NAudio.Dsd
{
    /// <summary>
    /// DSD audio format enumeration.
    /// </summary>
    public enum DsdFormat
    {
        /// <summary>
        /// Represents the DSD64 audio format, a high-resolution digital audio format with a sampling rate of 2.8224 MHz.
        /// </summary>
        DSD64 = 0,
        /// <summary>
        /// Represents the DSD128 audio format, a high-resolution digital audio format with a sampling rate of 5.6448 MHz.
        /// </summary>
        DSD128 = 1,
        /// <summary>
        /// Represents the DSD256 audio format, a high-resolution digital audio format with a sampling rate of 11.2896 MHz.
        /// </summary>
        DSD256 = 2,
        /// <summary>
        /// Represents the DSD512 audio format, a high-resolution digital audio format with a sampling rate of 22.5792 MHz.
        /// </summary>
        DSD512 = 3,
        /// <summary>
        /// Represents the DSD1024 audio format, a high-resolution digital audio format with a sampling rate of 45.1584 MHz.
        /// This format is less common and may not be supported by all hardware or software.
        /// </summary>
        DSD1024 = 4
    }

    public static class DsdFormatExtensions
    {
        /// <summary>
        /// Gets the sampling frequency in Hz for the specified DSD format.
        /// </summary>
        /// <param name="format">The DSD format.</param>
        /// <returns>The sampling frequency in Hz.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetSamplingFrequency(this DsdFormat format)
        {
            return format switch
            {
                DsdFormat.DSD64 => 2822400,
                DsdFormat.DSD128 => 5644800,
                DsdFormat.DSD256 => 11289600,
                DsdFormat.DSD512 => 22579200,
                DsdFormat.DSD1024 => 45158400,
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported DSD format"),
            };
        }

        /// <summary>
        /// Creates a DsdFormat from the given sampling frequency in Hz.
        /// </summary>
        /// <param name="samplingFrequency">sampling frequency in Hz.</param>
        /// <returns>The corresponding DsdFormat.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static DsdFormat FromSamplingFrequency(int samplingFrequency)
        {
            return samplingFrequency switch
            {
                2822400 => DsdFormat.DSD64,
                5644800 => DsdFormat.DSD128,
                11289600 => DsdFormat.DSD256,
                22579200 => DsdFormat.DSD512,
                45158400 => DsdFormat.DSD1024,
                _ => throw new ArgumentOutOfRangeException(nameof(samplingFrequency), "Unsupported sampling frequency"),
            };
        }
    }
}
