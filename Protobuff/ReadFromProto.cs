using Serilog;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Mapster.Protobuff
{
    internal class ReadFromProto
    {

        private static ReadFromProto? _instance;
        private static readonly object _lock = new();
        /// <summary>
        /// The Singleton Instance
        /// </summary>
        public static ReadFromProto Instance
        {
            get
            {
                // threadsafe singleton implementation
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ReadFromProto();
                        }
                    }
                }
                return _instance;
            }
        }

        #region Events
        public delegate void VoidDelegate();
        public delegate void DoubleDelegate(double value);

        /// <summary>
        /// Event that is fired when the read finishes
        /// </summary>
        public event VoidDelegate? FinishedEvent;
        /// <summary>
        /// Event that is fired with the percent of all bytes finished
        /// </summary>
        public event DoubleDelegate? SetValueEvent;
        #endregion

        /// <summary>
        /// Private constructor, for singleton
        /// </summary>
        private ReadFromProto() { }


        #region Deflaters
        /// <summary>
        /// Deflates a byte array that is inflated with zlib
        /// </summary>
        /// <param name="input">The byte array to be deflated</param>
        /// <returns>The deflated byte array</returns>
        private static async Task<byte[]> ZLibDeflate(byte[] input)
        {
            MemoryStream output = new();
            using (ZLibStream dstream = new(new MemoryStream(input), CompressionMode.Decompress))
            {
                await dstream.CopyToAsync(output);
            }
            return output.ToArray();
        }
        #endregion

        /// <summary>
        /// Searches the Data folder for .pbf files and parses them
        /// </summary>
        /// <returns></returns>
        public async Task LoadProtos()
        {
            DirectoryInfo dir = new("Data/");

            var files = dir.GetFiles().Where(f => f.Extension == ".pbf").ToArray();

            double totalLength = files.Sum(f => f.Length);
            Log.Information("Found {0} files to load for a total of {1}", files.Length, Utils.Utils.BytesToString(totalLength));

            double finishedBytes = 0;

            foreach (FileInfo file in files)
            {
                Log.Information("Loading {0}", file.Name);

                using FileStream sr = new FileInfo(file.FullName).OpenRead();

                while (sr.Length > sr.Position)
                {
                    SetValueEvent?.Invoke(((finishedBytes + sr.Position) / totalLength) * 100);

                    byte[] buff = new byte[4];
                    await sr.ReadAsync(buff.AsMemory(0, 4));
                    int length = BinaryPrimitives.ReadInt32BigEndian(buff.AsSpan());

                    buff = new byte[length];

                    await sr.ReadAsync(buff.AsMemory(0, length));

                    var header = BlobHeader.Parser.ParseFrom(buff);

                    buff = new byte[header.Datasize];

                    await sr.ReadAsync(buff.AsMemory(0, header.Datasize));

                    var data = Blob.Parser.ParseFrom(buff);

                    if (data.HasZlibData)
                    {
                        var deflatedData = await ZLibDeflate(data.ZlibData.ToByteArray());

                        if (header.HasType)
                        {
                            switch (header.Type)
                            {
                                case "OSMHeader":
                                    HeaderBlock headerBlock = HeaderBlock.Parser.ParseFrom(deflatedData);
                                    break;
                                case "OSMData":
                                    PrimitiveBlock pblock = PrimitiveBlock.Parser.ParseFrom(deflatedData);
                                    break;
                            }
                        }
                    }

                }
                finishedBytes += sr.Position;
            }

            FinishedEvent?.Invoke();

        }
    }
}
