using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Wayland
{
    public sealed class WaylandWireConnection : IDisposable
    {
        Socket socket;
        Stream networkStream;
        Stream bufferedStream;
        BinaryWriter binaryWriter;
        BinaryReader binaryReader;

        public WaylandWireConnection(Socket socket)
        {
            this.socket = socket;
            networkStream = new NetworkStream(this.socket);
            bufferedStream = new BufferedStream(networkStream);
            binaryReader = new BinaryReader(bufferedStream);
            binaryWriter = new BinaryWriter(bufferedStream);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                binaryReader.Dispose();
                binaryWriter.Dispose();
                bufferedStream.Dispose();
                networkStream.Dispose();
                socket.Dispose();
            }
        }

        public void Flush()
        {
            bufferedStream.Flush();
        }

        public void Write(int i)
        {
            binaryWriter.Write(i);
        }

        public void Write(uint u)
        {
            binaryWriter.Write(u);
        }

        public void Write(double f)
        {
            binaryWriter.Write(Convert.ToInt32(f * 256));
        }

        public void Write(string s)
        {
            if (s == null)
            {
                Write((byte[])null);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                byte[] withNull = new byte[bytes.Length + 1];
                bytes.CopyTo(withNull, 0);
                Write(bytes);
            }
        }

        public void Write(byte[] a)
        {
            if (a == null)
            {
                binaryWriter.Write((uint)0);
            }
            else
            {
                int paddedLength = a.Length + 3 / 4 * 4;
                binaryWriter.Write((uint)a.Length);
                binaryWriter.Write(a);
                for (int i = 0; i <= paddedLength - a.Length; i++)
                    binaryWriter.Write((byte)0);
            }
        }

        public void Write(IntPtr h)
        {
            throw new NotSupportedException("File descriptors are currently not supported");
        }

        public int ReadInt32()
        {
            throw new Exception("unimplemented");
        }

        public uint ReadUInt32()
        {
            throw new Exception("unimplemented");
        }

        public double ReadDouble()
        {
            throw new Exception("unimplemented");
        }

        public string ReadString()
        {
            throw new Exception("unimplemented");
        }

        public byte[] ReadByteArray()
        {
            throw new Exception("unimplemented");
        }

        public IntPtr ReadFileDescriptor()
        {
            throw new NotSupportedException("File descriptors are currently not supported");
        }
    }
}
