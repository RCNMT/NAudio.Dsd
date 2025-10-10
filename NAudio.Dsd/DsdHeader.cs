using System.Text;

namespace NAudio.Dsd
{
    /// <summary>
    /// Represents the header information of a DSD (Direct Stream Digital) audio file, such as a DSF file.
    /// </summary>
    /// References: https://dsd-guide.com/sites/default/files/white-papers/DSFFileFormatSpec_E.pdf
    /// References: https://www.sonicstudio.com/pdf/dsd/DSDIFF_1.5_Spec.pdf
    public struct DsdHeader
    {
        /// <summary>
        /// DSD chuck identifier, should be "DSD " (0x44, 0x53, 0x44, 0x20) for DSF files. This is a 4-byte ASCII string.
        /// </summary>
        public int ChunkID;

        /// <summary>
        /// Size of the DSD chunk in bytes, including the header. This is a 8-byte unsigned integer.
        /// </summary>
        public long ChunkSize;

        /// <summary>
        /// Total file size in bytes. This is a 8-byte unsigned integer.
        /// </summary>
        public long TotalFileSize;

        /// <summary>
        /// Metadata offset in bytes from the beginning of the file to the start of the metadata section. This is a 8-byte unsigned integer.
        /// </summary>
        public long MetadataOffset;

        /// <summary>
        /// Metadata length in bytes from the beginning of metadata to the end of the metadata section. This is a 8-byte unsigned integer.
        /// </summary>
        public long MetadataLength;

        /// <summary>
        /// Format chunk identifier, should be "fmt " (0x66, 0x6D, 0x74, 0x20) for DSF files. This is a 4-byte ASCII string.
        /// </summary>
        public int FormatID;

        /// <summary>
        /// Size of the Format chunk in bytes, including the header. This is a 8-byte unsigned integer.
        /// </summary>
        public long FormatSize;

        /// <summary>
        /// Format version, should be 1 for the current version of the DSF format. This is a 4-byte unsigned integer.
        /// </summary>
        public int FormatVersion;

        /// <summary>
        /// Format, should be 0 for DSD raw. This is a 4-byte unsigned integer.
        /// </summary>
        public int Format;

        /// <summary>
        /// Number of channels in the audio data. This is a 4-byte unsigned integer.
        /// </summary>
        /// <remarks>
        /// Common values are 1 for mono, 2 for stereo, 3 for 3 channels, 4 for quadraphonic, 5 for 4 channels, 6 for 5 channels, and 7 for 5.1 channels.
        /// </remarks>
        public int ChannelType;

        /// <summary>
        /// Channel count, should match the number of channels in the audio data. This is a 4-byte unsigned integer.
        /// </summary>
        public int ChannelCount;

        /// <summary>
        /// Sampling frequency in Hz.
        /// Common values are 2822400 for DSD64, 5644800 for DSD128, 11289600 for DSD256, 22579200 for DSD512, and 45158400 for DSD1024. This is a 4-byte unsigned integer.
        /// </summary>
        public int SamplingFrequency;

        /// <summary>
        /// Bits per sample, should be 1 for DSD audio data. This is a 4-byte unsigned integer.
        /// </summary>
        public int BitsPerSample;

        /// <summary>
        /// Sample count, total number of samples per channel in the audio data. This is a 8-byte unsigned integer.
        /// </summary>
        public long SampleCount;

        /// <summary>
        /// Block size per channel in bytes, should be 4096 for DSF files. This is a 4-byte unsigned integer.
        /// </summary>
        public int BlockSizePerChannel;

        /// <summary>
        /// Reserved, should be 0. This is a 4-byte unsigned integer.
        /// </summary>
        public int Reserved;

        /// <summary>
        /// Data chunk identifier, should be "data" (0x64, 0x61, 0x74, 0x61) for DSF files. This is a 4-byte ASCII string.
        /// </summary>
        public int DataID;

        /// <summary>
        /// Length of the Data chunk in bytes. This is a 8-byte signed integer.
        /// </summary>
        public long DataLength;

        /// <summary>
        /// Offset in bytes from the beginning of the file to the start of the audio data.
        /// </summary>
        public long DataOffset;

        /// <summary>
        /// Interleaved of audio data.
        /// </summary>
        public bool Interleaved;

        /// <summary>
        /// Indicates the "endianness" of the audio data.
        /// </summary>
        public bool IsLittleEndian;

        /// <summary>
        /// Frame size of audio data for all channels.
        /// </summary>
        public int FrameSize;

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
            stream.Position = 0;
            using BinaryReader reader = new(stream, Encoding.ASCII, true);
            var fileType = reader.ReadString(4);
            if (fileType == "DSD " || fileType == " DSD")
            {
                return GetDsfHeader(stream);
            }
            else if (fileType == "FRM8")
            {
                return GetDsdiffHeader(stream);
            }
            throw new InvalidDataException("Not a valid file format");
        }

        private static DsdHeader GetDsfHeader(Stream stream)
        {
            DsdHeader header = new();
            stream.Position = 0;
            using BinaryReader reader = new(stream, Encoding.ASCII, true);

            header.ChunkID = reader.ReadInt32();
            header.ChunkSize = reader.ReadInt64();
            header.TotalFileSize = reader.ReadInt64();
            header.MetadataOffset = reader.ReadInt64();
            header.FormatID = reader.ReadInt32();
            header.FormatSize = reader.ReadInt64();
            header.FormatVersion = reader.ReadInt32();
            header.Format = reader.ReadInt32();
            header.ChannelType = reader.ReadInt32();
            header.ChannelCount = reader.ReadInt32();
            header.SamplingFrequency = reader.ReadInt32();
            header.BitsPerSample = reader.ReadInt32();
            header.SampleCount = reader.ReadInt64();
            header.BlockSizePerChannel = reader.ReadInt32();
            header.Reserved = reader.ReadInt32();
            header.DataID = reader.ReadInt32();
            header.DataLength = reader.ReadInt64();
            header.DataOffset = stream.Position;
            header.Interleaved = false;
            header.IsLittleEndian = header.BitsPerSample == 1;
            header.MetadataOffset = header.DataLength;
            header.MetadataLength = stream.Length;
            header.FrameSize = header.BlockSizePerChannel * header.ChannelCount;
            return header;
        }

        private static DsdHeader GetDsdiffHeader(Stream stream)
        {
            DsdHeader header = new();
            stream.Position = 0;
            using BinaryReader reader = new(stream, Encoding.ASCII, true);

            var fileId = reader.ReadString(4);
            if (fileId != "FRM8")
            {
                throw new InvalidDataException("Not a valid DSDIFF file");
            }

            var fileSize = reader.ReadInt64BE();
            var formType = reader.ReadString(4);

            while (stream.Position < fileSize + 12) // 12 = header size of FRM8
            {
                var chunkId = reader.ReadString(4);
                var chunkSize = reader.ReadInt64BE();

                if (chunkId == "FVER")
                {
                    string version = string.Concat(reader.ReadBytes(4));
                    header.FormatVersion = int.Parse(version);
                }
                else if (chunkId == "PROP")
                {
                    var propType = reader.ReadString(4);
                    var propEnd = stream.Position + chunkSize - 4;

                    while (stream.Position < propEnd)
                    {
                        var subId = reader.ReadString(4);
                        var subSize = reader.ReadInt64BE();

                        if (subId == "FS  ") // Sample rate
                        {
                            header.SamplingFrequency = reader.ReadInt32BE();
                        }
                        else if (subId == "CHNL")
                        {
                            header.ChannelCount = reader.ReadInt16BE();
                            var channelIds = new string[header.ChannelCount];
                            for (int i = 0; i < channelIds.Length; i++)
                            {
                                channelIds[i] = reader.ReadString(4);
                            }
                        }
                        else if (subId == "CMPR")
                        {
                            var compressionType = reader.ReadString(4);
                            int size = reader.ReadByte();
                            if (sizeof(int) + sizeof(byte) + size != subSize)
                            {
                                size = (int)subSize - sizeof(int) - sizeof(byte);
                            }
                            var compressionName = reader.ReadString(size);
                        }
                        else
                        {
                            // Skip prop if not handled.
                            stream.Position += subSize;
                        }
                    }
                }
                else if (chunkId == "DSD ")
                {
                    var start = stream.Position;
                    var possibleBlockSize = reader.ReadInt32BE();

                    if (possibleBlockSize > 0 && possibleBlockSize % 8 == 0 && possibleBlockSize < 1_000_000)
                    {
                        // Non-interleaved
                        header.Interleaved = false;
                        header.BlockSizePerChannel = possibleBlockSize;
                        header.DataOffset = stream.Position;
                        header.DataLength = chunkSize - sizeof(int);
                    }
                    else
                    {
                        // Interleaved
                        header.Interleaved = true;
                        header.BlockSizePerChannel = 1024 * 2;
                        header.DataOffset = start;
                        header.DataLength = chunkSize;
                    }

                    // Skip audio data to next chunk.
                    stream.Position = start + chunkSize;
                }
                else if (chunkId == "ID3 ")
                {
                    header.MetadataOffset = stream.Position;
                    header.MetadataLength = chunkSize;

                    // Handle ID3 metadata here.
                    stream.Position += chunkSize;
                }
                else
                {
                    // Skip chunk if not handled.
                    stream.Position += chunkSize;
                }
            }
            header.Interleaved = true;
            header.SampleCount = header.DataLength * 8 / header.ChannelCount;
            header.BitsPerSample = 1;
            header.FrameSize = header.BlockSizePerChannel * header.ChannelCount;
            header.IsLittleEndian = false;

            return header;
        }
    }
}
