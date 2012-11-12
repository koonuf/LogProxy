using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace LogProxy.Lib
{
    public class SocketStream : Stream
    {
        private Socket socket;

        public SocketStream(Socket socket)
        {
            this.socket = socket;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return -1; }
        }

        public override long Position
        {
            get
            {
                return -1;
            }
            set
            {
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.socket != null)
            {
                Utils.DisposeSocket(this.socket);
            }
        }
    }
}
