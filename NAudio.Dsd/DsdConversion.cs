namespace NAudio.Dsd
{
    public class DsdConversion
    {
        /// <summary>
        /// Converts DSD2x audio data to DSD1x using a simple 2nd-order FIR noise shaping filter. <br/>
        /// <code>
        /// Input      Output
        /// DSD1024 => DSD512
        /// DSD512  => DSD256
        /// DSD256  => DSD128
        /// DSD128  => DSD64
        /// </code>
        /// </summary>
        /// <param name="dsd2x">DSD audio data byte array. must be in DSD128, DSD256, DSD512, or DSD1024 format.</param>
        /// <returns>Byte array containing the downsampled DSD1x audio data. The length of the output array is half the length of the input byte array.</returns>
        [Obsolete("This method is deprecated due to quality issue.")]
        public static unsafe byte[] DSD2xToDSD1x_FIR2nd(byte[] dsd2x)
        {
            byte[] dsd1x = new byte[dsd2x.Length / 2];

            if ((dsd1x.Length & 1) != 0)
            {
                throw new ArgumentException($"{nameof(dsd2x)}.Length / 2 must be even.", nameof(dsd2x));
            }

            float prevError1 = 0;
            float prevError2 = 0;

            fixed (byte* srcPtr = dsd2x, dstPtr = dsd1x)
            {
                byte* src = srcPtr;
                byte* dst = dstPtr;

                for (int i = 0; i < dsd1x.Length; i++, src += 2, dst++)
                {
                    byte b1 = src[0], b2 = src[1];
                    byte downsampledByte = 0;

                    for (int bit = 0; bit < 8; bit++)
                    {
                        // Convert each bit to +1/-1
                        float s1 = (b1 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        float s2 = (b2 & (1 << bit)) != 0 ? 1.0f : -1.0f;

                        // Average
                        float avg = (s1 + s2) * 0.5f;

                        // Apply simple 2nd-order noise shaping filter
                        float shaped = avg + 1.5f * prevError1 - 0.5f * prevError2;

                        // Quantize to 1-bit
                        float quantized = shaped > 0 ? 1.0f : -1.0f;

                        // Update error feedback (FIR)
                        float error = shaped - quantized;
                        prevError2 = prevError1;
                        prevError1 = error;

                        // Set bit
                        if (quantized > 0)
                            downsampledByte |= (byte)(1 << bit);
                    }

                    *dst = downsampledByte;
                }
            }

            return dsd1x;
        }

        /// <summary>
        /// Converts DSD4x audio data to DSD1x using a simple 2nd-order FIR noise shaping filter. <br/>
        /// <code>
        /// Input      Output
        /// DSD1024 => DSD256
        /// DSD512  => DSD128
        /// DSD256  => DSD64
        /// </code>
        /// </summary>
        /// <param name="dsd4x">DSD audio data byte array. must be in DSD256, DSD512, or DSD1024 format.</param>
        /// <returns>Byte array containing the downsampled DSD1x audio data. The length of the output array is one-fourth the length of the input byte array.</returns>
        [Obsolete("This method is deprecated due to quality issue.")]
        public static unsafe byte[] DSD4xToDSD1x_FIR2nd(byte[] dsd4x)
        {
            byte[] dsd1x = new byte[dsd4x.Length / 4];

            if ((dsd1x.Length & 1) != 0)
            {
                throw new ArgumentException($"{nameof(dsd4x)}.Length / 4 must be even.", nameof(dsd4x));
            }

            double prevError1 = 0;
            double prevError2 = 0;

            fixed (byte* srcPtr = dsd4x, dstPtr = dsd1x)
            {
                byte* src = srcPtr;
                byte* dst = dstPtr;

                for (int i = 0; i < dsd1x.Length; i++, src += 4, dst++)
                {
                    byte b1 = src[0], b2 = src[1], b3 = src[2], b4 = src[3];
                    byte downsampledByte = 0;

                    for (int bit = 0; bit < 8; bit++)
                    {
                        // Convert each bit to +1/-1
                        double s1 = (b1 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s2 = (b2 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s3 = (b3 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s4 = (b4 & (1 << bit)) != 0 ? 1.0f : -1.0f;

                        // Average
                        double avg = 0.25f * (s1 + s2 + s3 + s4);

                        // Apply 2nd-order noise shaping
                        double shaped = avg + 1.5f * prevError1 - 0.5f * prevError2;

                        // Quantize to 1-bit
                        double quantized = shaped > 0 ? 1.0f : -1.0f;

                        // Update error feedback (FIR)
                        double error = shaped - quantized;
                        prevError2 = prevError1;
                        prevError1 = error;

                        // Set bit
                        if (quantized > 0)
                            downsampledByte |= (byte)(1 << bit);
                    }

                    *dst = downsampledByte;
                }
            }

            return dsd1x;
        }

        /// <summary>
        /// Converts DSD8x audio data to DSD1x using a simple 2nd-order FIR noise shaping filter. <br/>
        /// <code>
        /// Input      Output
        /// DSD1024 => DSD128
        /// DSD512  => DSD64
        /// </code>
        /// </summary>
        /// <param name="dsd8x">DSD audio data byte array. must be in DSD512 or DSD1024 format.</param>
        /// <returns>Byte array containing the downsampled DSD1x audio data. The length of the output array is one-eighth the length of the input byte array.</returns>
        [Obsolete("This method is deprecated due to performance and quality issues.")]
        public static unsafe byte[] DSD8xToDSD1x_FIR2nd(byte[] dsd8x)
        {
            byte[] dsd1x = new byte[dsd8x.Length / 8];

            if ((dsd1x.Length & 1) != 0)
            {
                throw new ArgumentException($"{nameof(dsd8x)}.Length / 8 must be even.", nameof(dsd8x));
            }

            double prevError1 = 0;
            double prevError2 = 0;

            fixed (byte* srcPtr = dsd8x, dstPtr = dsd1x)
            {
                byte* src = srcPtr;
                byte* dst = dstPtr;

                for (int i = 0; i < dsd1x.Length; i++, src += 8, dst++)
                {
                    byte b1 = src[0], b2 = src[1], b3 = src[2], b4 = src[3], b5 = src[4], b6 = src[5], b7 = src[6], b8 = src[7];
                    byte downsampledByte = 0;

                    for (int bit = 0; bit < 8; bit++)
                    {
                        // Convert each bit to +1/-1
                        double s1 = (b1 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s2 = (b2 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s3 = (b3 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s4 = (b4 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s5 = (b5 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s6 = (b6 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s7 = (b7 & (1 << bit)) != 0 ? 1.0f : -1.0f;
                        double s8 = (b8 & (1 << bit)) != 0 ? 1.0f : -1.0f;

                        // Average
                        double avg = 0.125f * (s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8);

                        // Apply 2nd-order noise shaping
                        double shaped = avg + 1.5f * prevError1 - 0.5f * prevError2;

                        // Quantize to 1-bit
                        double quantized = shaped > 0 ? 1.0f : -1.0f;

                        // Update error feedback (FIR)
                        double error = shaped - quantized;
                        prevError2 = prevError1;
                        prevError1 = error;

                        // Set bit
                        if (quantized > 0)
                            downsampledByte |= (byte)(1 << bit);
                    }

                    *dst = downsampledByte;
                }
            }

            return dsd1x;
        }

        /// <summary>
        /// Converts DSD16x audio data to DSD1x using a combination of DSD8xToDSD1x_FIR2nd and DSD2xToDSD1x_FIR2nd methods. <br/>
        /// <code>
        /// Input      Output
        /// DSD1024 => DSD64
        /// </code>
        /// </summary>
        /// <param name="dsd16x">DSD audio data byte array. must be in DSD1024 format.</param>
        /// <returns>Byte array containing the downsampled DSD1x audio data. The length of the output array is one-sixteenth the length of the input byte array.</returns>
        [Obsolete("This method is deprecated due to performance and quality issues.")]
        public static byte[] DSD16xToDSD1x(byte[] dsd16x)
        {
            return DSD2xToDSD1x_FIR2nd(DSD8xToDSD1x_FIR2nd(dsd16x));
        }
    }
}
