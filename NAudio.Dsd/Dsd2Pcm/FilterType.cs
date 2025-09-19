namespace NAudio.Dsd.Dsd2Pcm
{
    public enum FilterType
    {
        /// <summary>
        /// A custom FIR filter. Use this when you want to provide your own coefficients
        /// for DSD-to-PCM conversion. Allows full control over cutoff, ripple, and windowing.
        /// </summary>
        Custom,

        /// <summary>
        /// Kaiser window FIR filter. Flexible filter with adjustable stopband attenuation
        /// via the beta parameter. Good balance between transition width and ripple.
        /// Common choice for high-quality DSD-to-PCM decimation.
        /// Recommended for high-quality DSD-to-PCM at all PCM rates, especially if you want
        /// tunable trade-offs between filter length and alias suppression.
        /// </summary>
        Kaiser,

        /// <summary>
        /// Hamming window FIR filter. Moderate stopband attenuation (~53 dB) and smooth passband.
        /// Simple and efficient, suitable for general-purpose DSD-to-PCM conversion where extreme
        /// aliasing suppression is not critical.
        /// </summary>
        Hamming,

        /// <summary>
        /// Blackman window FIR filter. Higher stopband attenuation (~74 dB) than Hamming,
        /// slightly wider transition band. Useful when you need better aliasing suppression
        /// without going to high-order filters.
        /// Recommended for PCM rates up to 192 kHz where a good balance between quality and performance is desired.
        /// </summary>
        Blackman,

        /// <summary>
        /// Chebyshev FIR filter. Equiripple in the frequency domain, giving very sharp transitions.
        /// Can achieve minimal filter length for a given stopband requirement, but may introduce
        /// ringing. Use when low-latency DSD-to-PCM conversion is desired.
        /// Recommended when low-latency DSD-to-PCM conversion is needed or CPU efficiency is critical.
        /// </summary>
        Chebyshev,

        /// <summary>
        /// Blackman-Harris window FIR filter. Very high stopband attenuation (~92–100 dB)
        /// with minimal side lobes. Excellent for high-fidelity DSD-to-PCM conversion, especially
        /// when suppressing aliasing and preserving ultrasonic content.
        /// Recommended for high PCM rates (192–768 kHz or higher) or professional audio playback.
        /// </summary>
        BlackmanHarris,
    }
}
