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

        /// <summary>
        /// Converts the DsdFormat to DSD rate multiplier (1 for DSD64, 2 for DSD128, etc.).
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetDsdRate(this DsdFormat format)
        {
            return format switch
            {
                DsdFormat.DSD64 => 1,
                DsdFormat.DSD128 => 2,
                DsdFormat.DSD256 => 4,
                DsdFormat.DSD512 => 8,
                DsdFormat.DSD1024 => 16,
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported DSD format"),
            };
        }

        /// <summary>
        /// Converts the DsdFormat to a user-friendly string representation.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="short"></param>
        /// <returns></returns>
        public static string ToFriendlyString(this DsdFormat format, bool @short = true)
        {
            return format switch
            {
                DsdFormat.DSD64 => "DSD64" + (@short ? "" : " (2.8224 MHz)"),
                DsdFormat.DSD128 => "DSD128" + (@short ? "" : " (5.6448 MHz)"),
                DsdFormat.DSD256 => "DSD256" + (@short ? "" : " (11.2896 MHz)"),
                DsdFormat.DSD512 => "DSD512" + (@short ? "" : " (22.5792 MHz)"),
                DsdFormat.DSD1024 => "DSD1024" + (@short ? "" : " (45.1584 MHz)"),
                _ => "Unknown DSD Format",
            };
        }
    }
}
