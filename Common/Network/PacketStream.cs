using System.IO;
using System.Text;
using System;
using Common.Security;
using Common.Structures;
using System.Linq;

namespace Common.Network
{
    /// <summary>
    /// A Packet
    /// </summary>
    public class PacketStream : MemoryStream
    {
        private XRC4Cipher rc4Cipher;

        /// <summary>
        /// Packet header length
        /// (4 Bytes)Size
        /// (2 Bytes)ID
        /// (1 Byte) Checksum
        /// </summary>
        public const short HeaderLength = 62;
        public const short MaxBuffer = 2048;

        /// <summary>
        /// Packet ID
        /// </summary>
        public ushort Id { get; private set; }

        private MemoryStream inner;

        protected internal KeyIV keyIV;

        #region Constructors
        /// <summary>
        /// Creates an empty packet
        /// </summary>
        public PacketStream()
        {
            this.inner = new MemoryStream();
        }

        /// <summary>
        /// Creates a packet and set its ID
        /// </summary>
        /// <param name="PacketId"></param>
        public PacketStream(ushort PacketId)
        {
            keyIV = Encryption.GenerateKeyIV();
            int keyLen = keyIV.Key.Length;
            int ivLen = keyIV.IV.Length;

            this.inner = new MemoryStream();

            // Write Blank space to have packet size assigned to later
            this.inner.Write(new byte[4], 0, 4);

            // Write the packetId
            this.inner.Write(BitConverter.GetBytes(PacketId), 0, 2);

            Random rand = new Random();

            // Packet Header Structure
            // (int) Packet Size
            // (short) Packet ID
            // (short) KeyIV Key Length
            // (byte) Confusion Data
            // (byte) Confusion Data
            // (byte[]) KeyIV Key 
            // (short) Confusion Data
            // (short) KeyIV IV Length
            // (byte[]) KeyIV Iv

            // Write the keyIV
            this.inner.Write(BitConverter.GetBytes((short)keyLen), 0, 2);
            this.inner.Write(BitConverter.GetBytes((byte)rand.Next(1, 255)), 0, 1);
            this.inner.Write(BitConverter.GetBytes((byte)rand.Next(1, 255)), 0, 1);
            this.inner.Write(keyIV.Key, 0, keyLen);
            this.inner.Write(BitConverter.GetBytes((short)rand.Next(1, 32767)), 0, 2);
            this.inner.Write(BitConverter.GetBytes((short)ivLen), 0, 2);
            this.inner.Write(keyIV.IV, 0, ivLen);
        }
        #endregion

        /// <summary>
        /// Encrypts and assembles packet for transmission
        /// </summary>
        /// <returns></returns>
        public byte[] Finalize()
        {
            // Start at the beginning of the stream
            this.inner.Seek(0, SeekOrigin.Begin);

            // Define a header buff and write it
            byte[] header = new byte[62];
            this.inner.Read(header, 0, header.Length);

            // Encrypt the information section of the packet 
            byte[] encryptedData = encryptData();

            // Set the packet size (based on the size of encryptedData buffer)
            byte[] packetSizeBytes = BitConverter.GetBytes(encryptedData.Length);
            for (int i = 0; i < 4; i++) { header[i] = packetSizeBytes[i]; }

            // Encrypt the header
            header = encryptHeader(ref header);
            byte[] headerLenBytes = BitConverter.GetBytes(header.Length);

            // Create a buffer to store the aseembled packet
            byte[] encryptedPacket = new byte[headerLenBytes.Length + header.Length + encryptedData.Length];

            // Copy the header and encryptedData to the final output buffer
            Array.Copy(headerLenBytes, 0, encryptedPacket, 0, headerLenBytes.Length);
            Array.Copy(header, 0, encryptedPacket, headerLenBytes.Length, header.Length);
            Array.Copy(encryptedData, 0, encryptedPacket, headerLenBytes.Length + header.Length, encryptedData.Length);

            // Return the finalized packet
            return encryptedPacket;
        }

        #region Write Methods
        /// <summary>
        /// Writes count bytes of given buffer at given offset to the packet
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.inner.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes count bytes of giver buffer to the packet at position offset
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="count"></param>
        public void WriteAt(byte[] buffer, int position, int count)
        {
            this.inner.Seek(position, SeekOrigin.Begin);
            this.inner.Write(buffer, 0, count);
        }

        /// <summary>
        /// Writes a 16-bits integer to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteInt16(Int16 val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 2);
        }

        /// <summary>
        /// Writes a 16-bits unsigned integer to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteUInt16(UInt16 val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 2);
        }

        /// <summary>
        /// Writes a 32-bits integer to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteInt32(Int32 val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 4);
        }

        /// <summary>
        /// Writes a 32-bits unsigned integer to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteUInt32(UInt32 val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 4);
        }

        /// <summary>
        /// Writes a 64-bits integer to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteInt64(Int64 val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 8);
        }

        /// <summary>
        /// Writes a 64-bits unsigned integer to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteUInt64(UInt64 val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 8);
        }

        /// <summary>
        /// Writes a floating-point number to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteSingle(Single val)
        {
            this.inner.Write(BitConverter.GetBytes(val), 0, 4);
        }

        /// <summary>
        /// Writes a byte to the packet
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            this.inner.WriteByte(value);
        }

        /// <summary>
        /// Writes an array of bytes to the packet
        /// </summary>
        /// <param name="value"></param>
        public void WriteBytes(byte[] value)
        {
            this.inner.Write(value, 0, value.Length);
        }

        /// <summary>
        /// Writes a string to the packet, filling with zeros
        /// </summary>
        /// <param name="val"></param>
        /// <param name="size"></param>
        public void WriteString(String val, int size)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(val);
            string str64 = Convert.ToBase64String(tmp);
            byte[] tmp2 = Encoding.UTF8.GetBytes(str64);
            this.inner.Write(BitConverter.GetBytes(tmp2.Length), 0, 4);
            this.inner.Write(tmp2, 0, tmp2.Length);
            this.inner.Write(new byte[str64.Length - tmp2.Length], 0, str64.Length - tmp2.Length);
        }

        /// <summary>
        /// Writes a string to the packet, without filling with zeros
        /// </summary>
        /// <param name="value"></param>
        internal void WriteString(String value)
        {
            this.WriteString(value, value.Length);
        }

        /// <summary>
        /// Writes a boolean to the packet
        /// </summary>
        /// <param name="val"></param>
        public void WriteBool(bool val)
        {
            this.inner.WriteByte((byte)(val ? 1 : 0));
        }
        #endregion

        #region Read Methods
        /// <summary>
        /// Reads count bytes from the packet, starting at the offset to the buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = this.inner.Read(buffer, offset, count);

            return result;
        }

        /// <summary>
        /// Read count bytes
        /// </summary>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public byte[] ReadBytes(int count, int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[count];
            inner.Read(buffer, 0, count);
            return buffer;
        }

        /// <summary>
        /// Reads a signed 16-bits Integer from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public Int16 ReadInt16(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[2];
            inner.Read(buffer, 0, 2);
            return BitConverter.ToInt16(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned 16-bits integer from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public UInt16 ReadUInt16(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[2];
            inner.Read(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a signed 32-bits integer from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public Int32 ReadInt32(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[4];
            inner.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned 32-bits integer from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public UInt32 ReadUInt32(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[4];
            inner.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a signed 64-bits integer from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public Int64 ReadInt64(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[8];
            inner.Read(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Reads an unsigned 64-bits integer from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public UInt64 ReadUInt64(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[8];
            inner.Read(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        /// <summary>
        /// Reads a floating-point number from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public Single ReadSingle(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[4];
            inner.Read(buffer, 0, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        /// <summary>
        /// Reads a string of given size from the packet
        /// </summary>
        /// <param name="size"></param>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public String ReadString(int size, int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            byte[] buffer = new byte[size];
            int i = 0;

            for (i = 0; i < size; i++)
            {
                int rByte = inner.ReadByte();

                // 0 : End of string / -1 : End of Stream
                if (rByte == 0x00 || rByte == -1)
                    break;

                buffer[i] = (byte)rByte;
            }

            // Advances the reading cursor
            inner.Seek((size - i - 1), SeekOrigin.Current);

            return Encoding.ASCII.GetString(buffer).Trim('\0');
        }

        internal string ReadString(int offset = -1)
        {
            if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            // Buffer the string length
            byte[] buffer = new byte[4];
            inner.Read(buffer, 0, 4);

            // Buffer the string (still encoded to base64)
            int strLen = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[strLen];
            inner.Read(buffer, 0, strLen); 

            // Convert to normal string
            string str64 = Encoding.UTF8.GetString(buffer);
            byte[] tmp = Convert.FromBase64String(str64);
            return Encoding.UTF8.GetString(tmp);

            //StringBuilder builder = new StringBuilder();

            //while (true)
            //{
            //    int rByte = inner.ReadByte();

            //    // 0 : End of string / -1 : End of Stream
            //    if (rByte == 0x00 || rByte == -1)
            //        break;

            //    builder.Append(Convert.ToChar(rByte));
            //}
        }

        /// <summary>
        /// Reads a boolean value from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        public bool ReadBool(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            return (inner.ReadByte() == 0 ? false : true);
        }

        /// <summary>
        /// Reads a byte from the packet
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="countHeader"></param>
        /// <returns></returns>
        internal byte ReadByte(int offset = -1, bool countHeader = false)
        {
            if (countHeader)
                inner.Seek(offset, SeekOrigin.Begin);
            else if (offset == -1 && inner.Position < HeaderLength)
                inner.Seek(HeaderLength, SeekOrigin.Begin);
            else if (offset >= 0)
                inner.Seek(offset + HeaderLength, SeekOrigin.Begin);

            return (byte)inner.ReadByte();
        }
        #endregion

        /// <summary>
        /// Gets the packet ID
        /// </summary>
        /// <returns></returns>
        public UInt16 GetId()
        {
            if (this.Id > 0) return this.Id;

            inner.Seek(4, SeekOrigin.Begin);
            byte[] res = new byte[2];
            inner.Read(res, 0, 2);
            this.Id = BitConverter.ToUInt16(res, 0);
            return this.Id;
        }

        protected byte[] encryptHeader(ref byte[] header)
        {
            string dtKey = DateTime.UtcNow.ToString("yyyyMMddHH");
            rc4Cipher = new XRC4Cipher(dtKey);
            return rc4Cipher.DoCipher(ref header);
        }

        protected byte[] encryptData()
        {
            byte[] res = new byte[inner.Length - 62];
            byte[] enc = null;
            inner.Seek(62, SeekOrigin.Begin);
            inner.Read(res, 0, res.Length);
            enc = Encryption.EncryptBytes(ref res, keyIV.Key, keyIV.IV);

            return  (enc.Length > 0) ? enc : new byte[1];
        }

        /// <summary>
        /// Sets Packet ID
        /// </summary>
        /// <param name="pPacketId"></param>
        public void SetId(UInt16 pPacketId)
        {
            inner.Seek(4, SeekOrigin.Begin);
            inner.Write(BitConverter.GetBytes(pPacketId), 0, 2);
        }
    }
}
