using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnbufferedMemoryStream
{
    public unsafe class UnbufferedMemoryStream : Stream
    {
        private readonly int _blobSize;
        private readonly ManualResetEventSlim _canDispose;
        private readonly AutoResetEvent _dataWritten;

        private volatile bool _disposed;
        private volatile MemoryBlob* _head;
        private volatile int _headPosition;
        private int _operationsInProgress;
        private volatile MemoryBlob* _tail;
        private int _tailPosition;

        public UnbufferedMemoryStream() : this(1024)
        {
        }

        public UnbufferedMemoryStream(int blobSize)
        {
            _head = _tail = CreateBlob(blobSize);
            _blobSize = blobSize;
            _headPosition = 0;
            _tailPosition = 0;
            _dataWritten = new AutoResetEvent(false);
            _canDispose = new ManualResetEventSlim();
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length => throw new InvalidOperationException();

        public override long Position
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        private static MemoryBlob* CreateBlob(int blobSize)
        {
            var blob = new MemoryBlob(blobSize);
            var pBlob = Marshal.AllocHGlobal(sizeof(MemoryBlob));
            Marshal.StructureToPtr(blob, pBlob, false);
            return (MemoryBlob*) pBlob;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnbufferedMemoryStream));

            if (Interlocked.Increment(ref _operationsInProgress) == 1)
                _canDispose.Reset();

            if (_head == _tail && _headPosition == _tailPosition)
                _dataWritten.WaitOne();

            var blobEnd = _head == _tail ? _headPosition : _blobSize;

            var bytesToBeRead = Math.Min(count, blobEnd - _tailPosition);

            Marshal.Copy(_tail->Data + _tailPosition, buffer, offset, bytesToBeRead);

            _tailPosition += bytesToBeRead;
            if (_tailPosition >= _blobSize)
            {
                _tailPosition = 0;
                var oldTail = _tail;
                _tail = _tail->Next;

                Marshal.FreeHGlobal(_tail->Data);
                Marshal.FreeHGlobal((IntPtr) oldTail);
            }


            if (Interlocked.Decrement(ref _operationsInProgress) == 0)
                _canDispose.Set();

            return bytesToBeRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnbufferedMemoryStream));

            if (Interlocked.Increment(ref _operationsInProgress) == 1)
                _canDispose.Reset();

            do
            {
                var spaceInCurrentBlob = _blobSize - _headPosition;
                var bytesToBeWritten = Math.Min(count, spaceInCurrentBlob);

                Marshal.Copy(buffer, offset, _head->Data + _headPosition, bytesToBeWritten);

                count -= bytesToBeWritten;
                offset += bytesToBeWritten;
                _headPosition += bytesToBeWritten;

                if (_headPosition < _blobSize) continue;

                var nextHead = CreateBlob(_blobSize);
                _head = _head->Next = nextHead;
                _headPosition = 0;
            } while (count > 0);

            _dataWritten.Set();

            if (Interlocked.Decrement(ref _operationsInProgress) == 0)
                _canDispose.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _disposed = true;
                _canDispose.Wait();
                _dataWritten.Dispose();
                _canDispose.Dispose();
            }


            while (_tail != _head)
            {
                var oldTail = _tail;
                _tail = _tail->Next;
                Marshal.FreeHGlobal((IntPtr) oldTail);
            }

            Marshal.FreeHGlobal((IntPtr) _tail);
            base.Dispose(disposing);
        }

        private struct MemoryBlob
        {
            public readonly IntPtr Data;

            public MemoryBlob(int blobSize)
            {
                Data = Marshal.AllocHGlobal(blobSize);
                Next = null;
            }

            public MemoryBlob* Next;
        }
    }
}