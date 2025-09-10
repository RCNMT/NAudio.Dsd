namespace NAudio.Dsd.Dsd2Pcm
{
    public enum DitherType
    {
        /// <summary>
        /// No dithering applied.
        /// </summary>
        None,
        /// <summary>
        /// Second-order PDF dithering algorithm.
        /// </summary>
        SoPDF,
        /// <summary>
        /// F-weighted dithering algorithm.
        /// </summary>
        FWeighted,
        /// <summary>
        /// Rectangular dithering algorithm.
        /// </summary>
        Rectangular,
        /// <summary>
        /// Triangular Probability Density Function dithering algorithm.
        /// </summary>
        TriangularPDF,
        /// <summary>
        /// Vanderkooy-Lipshitz dithering algorithm.
        /// </summary>
        VanderkooyLipshitz,
        /// <summary>
        /// High-pass Triangular PDF dithering algorithm.
        /// </summary>
        HighPassTriangularPDF,
        /// <summary>
        /// Lipshitz's Minimally Audible dithering algorithm.
        /// </summary>
        LipshitzMinimallyAudible,
        /// <summary>
        /// Modified High-pass Triangular PDF dithering algorithm.
        /// </summary>
        ModifiedHighPassTriangularPDF,
    }
}
