namespace NAudio.Dsd
{
    public class DsdHeader
    {
        /// <summary>
        /// DSD chuck identifier, should be "DSD " (0x44, 0x53, 0x44, 0x20) for DSF files. This is a 4-byte ASCII string.
        /// </summary>
        public int DsdID;

        /// <summary>
        /// Size of the DSD chunk in bytes, including the header. This is a 8-byte unsigned integer.
        /// </summary>
        public ulong DsdSize;

        /// <summary>
        /// Total file size in bytes. This is a 8-byte unsigned integer.
        /// </summary>
        public ulong TotalFileSize;

        /// <summary>
        /// Metadata offset in bytes from the beginning of the file to the start of the metadata section. This is a 8-byte unsigned integer.
        /// </summary>
        public ulong MetadataOffset;

        /// <summary>
        /// Format chunk identifier, should be "fmt " (0x66, 0x6D, 0x74, 0x20) for DSF files. This is a 4-byte ASCII string.
        /// </summary>
        public int FormatID;

        /// <summary>
        /// Size of the Format chunk in bytes, including the header. This is a 8-byte unsigned integer.
        /// </summary>
        public ulong FormatSize;

        /// <summary>
        /// Format version, should be 1 for the current version of the DSF format. This is a 4-byte unsigned integer.
        /// </summary>
        public uint FormatVersion;

        /// <summary>
        /// Format, should be 0 for DSD raw. This is a 4-byte unsigned integer.
        /// </summary>
        public uint Format;

        /// <summary>
        /// Number of channels in the audio data. This is a 4-byte unsigned integer.
        /// </summary>
        /// <remarks>
        /// Common values are 1 for mono, 2 for stereo, 3 for 3 channels, 4 for quadraphonic, 5 for 4 channels, 6 for 5 channels, and 7 for 5.1 channels.
        /// </remarks>
        public uint ChannelType;

        /// <summary>
        /// Channel count, should match the number of channels in the audio data. This is a 4-byte unsigned integer.
        /// </summary>
        public uint ChannelCount;

        /// <summary>
        /// Sampling frequency in Hz.
        /// Common values are 2822400 for DSD64, 5644800 for DSD128, 11289600 for DSD256, 22579200 for DSD512, and 45158400 for DSD1024. This is a 4-byte unsigned integer.
        /// </summary>
        public uint SamplingFrequency;

        /// <summary>
        /// Bits per sample, should be 1 for DSD audio data. This is a 4-byte unsigned integer.
        /// </summary>
        public uint BitsPerSample;

        /// <summary>
        /// Sample count, total number of samples per channel in the audio data. This is a 8-byte unsigned integer.
        /// </summary>
        public ulong SampleCount;

        /// <summary>
        /// Block size per channel in bytes, should be 4096 for DSF files. This is a 4-byte unsigned integer.
        /// </summary>
        public uint BlockSizePerChannel;

        /// <summary>
        /// Reserved, should be 0. This is a 4-byte unsigned integer.
        /// </summary>
        public uint Reserved;

        /// <summary>
        /// Data chunk identifier, should be "data" (0x64, 0x61, 0x74, 0x61) for DSF files. This is a 4-byte ASCII string.
        /// </summary>
        public uint DataID;

        /// <summary>
        /// Size of the Data chunk in bytes. This is a 8-byte signed integer.
        /// </summary>
        public long DataSize;

        /// <summary>
        /// Offset in bytes from the beginning of the file to the start of the audio data.
        /// </summary>
        public long DataOffset;

        /// <summary>
        /// Reads and parses the DSD header from the given stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to read the DSD header from. The stream must support reading and seeking, and its position should be at the beginning of the DSD file.
        /// </param>
        /// <returns>
        /// A <see cref="DsdHeader"/> object containing the parsed header information.
        /// </returns>
        public static DsdHeader GetHeader(Stream stream)
        {
            using BinaryReader reader = new(stream, System.Text.Encoding.ASCII, true);
            return new DsdHeader()
            {
                DsdID = reader.ReadInt32(),
                DsdSize = reader.ReadUInt64(),
                TotalFileSize = reader.ReadUInt64(),
                MetadataOffset = reader.ReadUInt64(),
                FormatID = reader.ReadInt32(),
                FormatSize = reader.ReadUInt64(),
                FormatVersion = reader.ReadUInt32(),
                Format = reader.ReadUInt32(),
                ChannelType = reader.ReadUInt32(),
                ChannelCount = reader.ReadUInt32(),
                SamplingFrequency = reader.ReadUInt32(),
                BitsPerSample = reader.ReadUInt32(),
                SampleCount = reader.ReadUInt64(),
                BlockSizePerChannel = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32(),
                DataID = reader.ReadUInt32(),
                DataSize = reader.ReadInt64(),
                DataOffset = stream.Position
            };
        }
    }
}
