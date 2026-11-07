using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Game.Network
{
    /// <summary>
    /// One TCP connection carrying length-prefixed frames. A reader thread decodes incoming messages into a queue;
    /// a writer thread sends queued frames, so the game loop never blocks on a slow peer. Too long a send queue
    /// closes the connection.
    /// </summary>
    public sealed class NetConnection : IDisposable
    {
        private const int HeaderBytes = 4;
        private const int MaxQueuedFrames = 1024;

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly ConcurrentQueue<object> _incoming = new ConcurrentQueue<object>();
        private readonly BlockingCollection<byte[]> _outgoing = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
        private volatile bool _closed;
        private volatile string _flushReason;

        public NetConnection(TcpClient client)
        {
            _client = client;
            _client.NoDelay = true;
            _stream = client.GetStream();
            RemoteAddress = client.Client.RemoteEndPoint?.ToString() ?? string.Empty;
            new Thread(ReadLoop) { IsBackground = true, Name = "NetReader" }.Start();
            new Thread(WriteLoop) { IsBackground = true, Name = "NetWriter" }.Start();
        }

        public string RemoteAddress { get; }

        public bool IsClosed => _closed;

        /// <summary>Why the connection closed (exception text), for logs.</summary>
        public string CloseReason { get; private set; }

        private long _bytesSent;
        private long _bytesReceived;

        /// <summary>Traffic counters (module 3 metrics): frame bytes including the length header.</summary>
        public long BytesSent => Interlocked.Read(ref _bytesSent);
        public long BytesReceived => Interlocked.Read(ref _bytesReceived);

        public bool TryReceive(out object message) => _incoming.TryDequeue(out message);

        /// <summary>Sends an already encoded message (per-peer encodings such as ProtocolCodec.EncodeWorld).</summary>
        public void SendEncoded(byte[] payload) => Enqueue(payload);

        public void Send(object message) => Enqueue(ProtocolCodec.Encode(message));

        private void Enqueue(byte[] payload)
        {
            if (_closed)
            {
                return;
            }
            if (_outgoing.Count >= MaxQueuedFrames)
            {
                Close("send queue overflow");
                return;
            }
            var frame = new byte[HeaderBytes + payload.Length];
            WriteLength(frame, payload.Length);
            Buffer.BlockCopy(payload, 0, frame, HeaderBytes, payload.Length);
            try
            {
                _outgoing.Add(frame);
            }
            catch (InvalidOperationException)
            {
                // Added after close: ignore.
            }
        }

        public void Close(string reason)
        {
            if (_closed)
            {
                return;
            }
            _closed = true;
            CloseReason = reason;
            _outgoing.CompleteAdding();
            try
            {
                _client.Close();
            }
            catch (Exception)
            {
                // Already closed.
            }
        }

        /// <summary>Sends everything already queued, then closes (used to deliver a refusal before hanging up).</summary>
        public void CloseAfterFlush(string reason)
        {
            if (_closed)
            {
                return;
            }
            _flushReason = reason;
            _outgoing.CompleteAdding();
        }

        public void Dispose() => Close("disposed");

        private void ReadLoop()
        {
            var header = new byte[HeaderBytes];
            try
            {
                while (!_closed)
                {
                    if (!ReadExact(header, HeaderBytes))
                    {
                        Close("remote closed");
                        return;
                    }
                    int length = ReadLength(header);
                    if (length <= 0 || length > NetworkConstants.MaxFrameBytes)
                    {
                        Close($"bad frame length {length}");
                        return;
                    }
                    var payload = new byte[length];
                    if (!ReadExact(payload, length))
                    {
                        Close("remote closed");
                        return;
                    }
                    Interlocked.Add(ref _bytesReceived, HeaderBytes + length);
                    _incoming.Enqueue(ProtocolCodec.Decode(payload));
                }
            }
            catch (Exception exception)
            {
                Close(exception.Message);
            }
        }

        private void WriteLoop()
        {
            try
            {
                foreach (byte[] frame in _outgoing.GetConsumingEnumerable())
                {
                    _stream.Write(frame, 0, frame.Length);
                    Interlocked.Add(ref _bytesSent, frame.Length);
                }
                if (_flushReason != null)
                {
                    _stream.Flush();
                    Close(_flushReason);
                }
            }
            catch (Exception exception)
            {
                Close(exception.Message);
            }
        }

        private bool ReadExact(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    return false;
                }
                offset += read;
            }
            return true;
        }

        private static void WriteLength(byte[] buffer, int length)
        {
            buffer[0] = (byte)length;
            buffer[1] = (byte)(length >> 8);
            buffer[2] = (byte)(length >> 16);
            buffer[3] = (byte)(length >> 24);
        }

        private static int ReadLength(byte[] buffer) => buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
    }
}
