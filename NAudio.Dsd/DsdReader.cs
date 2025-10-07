namespace NAudio.Dsd
{
    /// <summary>
    /// Provides functionality to read DSD (Direct Stream Digital) audio files, such as DSF files, and exposes the audio data as a WaveStream.
    /// </summary>
    public class DsdReader : WaveStream, IDisposable
    {
        private readonly bool _own;
        private readonly object _obj = new();
        private readonly Stream _stream;
        private readonly DsdHeader _header;

        public DsdHeader Header
        {
            get => _header;
        }

        private readonly WaveFormat _waveFormat;
        public override WaveFormat WaveFormat
        {
            get => _waveFormat;
        }

        public override long Length
        {
            get => _header.DataLength;
        }

        public override long Position
        {
            get => _stream.Position - _header.DataOffset;
            set
            {
                lock (_obj)
                {
                    value = Math.Min(value, Length);
                    if (_header.Interleaved)
                    {
                        value -= value % _header.ChannelCount;
                    }
                    else
                    {
                        value -= value % (_header.FrameSize);
                    }
                    _stream.Position = value + _header.DataOffset;
                }
            }
        }

        private readonly TimeSpan _totalTime;
        public override TimeSpan TotalTime
        {
            get => _totalTime;
        }

        public override TimeSpan CurrentTime
        {
            get => TimeSpan.FromSeconds(Position * 8 / (float)(_header.SamplingFrequency * _header.ChannelCount));
            set => Position = (long)(value.TotalSeconds * _header.SamplingFrequency * _header.ChannelCount / 8);
        }

        public bool IsLSBF { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DsdReader"/> class that reads from the specified file path.
        /// </summary>
        /// <param name="path">The path to the DSD file to read.</param>
        public DsdReader(string path) : this(File.OpenRead(path), true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DsdReader"/> class that reads from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read the DSD file from. The stream must support reading and seeking.</param>
        public DsdReader(Stream stream) : this(stream, false)
        {
        }

        private DsdReader(Stream stream, bool own)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            _own = own;
            _stream = stream;
            _header = DsdHeader.GetHeader(_stream);
            _totalTime = TimeSpan.FromSeconds((_header.DataLength * 8) / (float)(_header.SamplingFrequency * _header.ChannelCount));
            _waveFormat = new WaveFormat(_header.SamplingFrequency, _header.BitsPerSample, _header.ChannelCount);
            IsLSBF = _header.IsLittleEndian;
            Position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count % _header.BlockSizePerChannel != 0)
            {
                throw new ArgumentException($"Must read complete blocks: requested {count}, block align is {Header.BlockSizePerChannel}");
            }
            lock (_obj)
            {
                if (Position + count > Length)
                {
                    count = (int)(Length - Position);
                }
                return _stream.Read(buffer, offset, count);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _own && _stream != null)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
