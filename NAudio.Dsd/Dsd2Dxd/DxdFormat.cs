namespace NAudio.Dsd.Dsd2Dxd
{
    public enum DxdFormat
    {
        /// <summary>
        /// Represents the DXD44.1 audio format, a high-resolution digital audio format with a sampling rate of 44.1 kHz.
        /// </summary>
        DXD44_1 = 1,
        /// <summary>
        /// Represents the DXD88.2 audio format, a high-resolution digital audio format with a sampling rate of 88.2 kHz.
        /// </summary>
        DXD88_2 = 2,
        /// <summary>
        /// Represents the DXD176.4 audio format, a high-resolution digital audio format with a sampling rate of 176.4 kHz.
        /// </summary>
        DXD176_4 = 4,
        /// <summary>
        /// Represnts the DXD352.8 audio format, a high-resolution digital audio format with a sampling rate of 352.8 kHz.
        /// </summary>
        DXD352_8 = 8,
        /// <summary>
        /// Represents the DXD705.6 audio format, a high-resolution digital audio format with a sampling rate of 705.6 kHz.
        /// </summary>
        DXD705_6 = 16,
    }

    public static class DxdFormatExtensions
    {
        /// <summary>
        /// Gets the sampling frequency in Hz for the specified DXD format.
        /// </summary>
        /// <param name="format">The DXD format.</param>
        /// <returns>The sampling frequency in Hz.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetSamplingFrequency(this DxdFormat format)
        {
            return format switch
            {
                DxdFormat.DXD44_1 => 44100,
                DxdFormat.DXD88_2 => 88200,
                DxdFormat.DXD176_4 => 176400,
                DxdFormat.DXD352_8 => 352800,
                DxdFormat.DXD705_6 => 705600,
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported DXD format"),
            };
        }
    }
}
