using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlickZip
{
    class LhaExtractor
    {
        private static readonly byte[] HEADER_BYTES = { 0x2D, 0x6C, 0x68 }; // "-lh"
        private static readonly int HEADER_SIZE = 21;

        private Stream _stream;

        public LhaExtractor(Stream stream)
        {
            _stream = stream;
        }

        public void Extract(string outputPath)
        {
            // Check if the stream is seekable
            if (!_stream.CanSeek)
            {
                throw new ArgumentException("The stream must be seekable.");
            }

            // Check if the output directory exists
            if (!Directory.Exists(outputPath))
            {
                throw new ArgumentException("The output directory does not exist.");
            }

            // Iterate through each entry in the archive and extract it to the output directory
            while (true)
            {
                // Read the next header from the stream
                byte[] headerBytes = ReadHeader();
                if (headerBytes == null)
                {
                    break; // End of archive
                }

                // Parse the header
                Header header = ParseHeader(headerBytes);

                // Read the data for the entry
                byte[] dataBytes = new byte[header.CompressedSize];
                _stream.Read(dataBytes, 0, dataBytes.Length);

                // Decompress the data
                byte[] uncompressedBytes = new byte[header.UncompressedSize];
                using (var decompressionStream = new LZ4.LZ4Stream(new MemoryStream(dataBytes), System.IO.Compression.CompressionMode.Decompress))
                {
                    decompressionStream.Read(uncompressedBytes, 0, uncompressedBytes.Length);
                }

                // Write the entry to disk
                string outputPathForEntry = Path.Combine(outputPath, header.FileName);
                using (var outputStream = new FileStream(outputPathForEntry, FileMode.Create))
                {
                    outputStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                }
            }
        }

        private byte[] ReadHeader()
        {
            // Find the start of the next header
            while (true)
            {
                int b = _stream.ReadByte();
                if (b == -1)
                {
                    return null; // End of archive
                }

                if (b == HEADER_BYTES[0])
                {
                    int b2 = _stream.ReadByte();
                    if (b2 == HEADER_BYTES[1])
                    {
                        int b3 = _stream.ReadByte();
                        if (b3 == HEADER_BYTES[2])
                        {
                            break; // Found header
                        }
                    }
                }
            }

            // Read the header bytes
            byte[] headerBytes = new byte[HEADER_SIZE];
            _stream.Read(headerBytes, 0, headerBytes.Length);

            return headerBytes;
        }

        private Header ParseHeader(byte[] headerBytes)
        {
            // Parse the header bytes
            string fileName = System.Text.Encoding.ASCII.GetString(headerBytes, 2, 13).TrimEnd('\0');
            int compressedSize = BitConverter.ToInt32(headerBytes, 15);
            int uncompressedSize = BitConverter.ToInt32(headerBytes, 19);

            return new Header()
            {
                FileName = fileName,
                CompressedSize = compressedSize,
                UncompressedSize = uncompressedSize
            };
        }

        private struct Header
        {
            public string FileName;
            public int CompressedSize;
            public int UncompressedSize;
        }
    }
}
