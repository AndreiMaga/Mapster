using Serilog;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Mapster.Protobuff
{
    internal class ReadFromOsm
    {

        private static ReadFromOsm? _instance;
        private static object _lock = new();
        public static ReadFromOsm Instance
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
                            _instance = new ReadFromOsm();
                        }
                    }
                }
                return _instance;
            }
        }

        #region Events
        public delegate void VoidDelegate();
        public delegate void DoubleDelegate(double value);


        public event VoidDelegate? FinishedEvent;
        public event DoubleDelegate? SetValueEvent;
        #endregion

        private ReadFromOsm() { }


        #region Deflaters
        public static async Task<byte[]> ZLibDeflate(byte[] input)
        {
            MemoryStream output = new();
            using (ZLibStream dstream = new(new MemoryStream(input), CompressionMode.Decompress))
            {
                await dstream.CopyToAsync(output);
            }
            return output.ToArray();
        }
        #endregion

        public async Task LoadProtos()
        {
            DirectoryInfo dir = new("Data/");

            var files = dir.GetFiles();

            double totalLength = files.Sum(f => f.Length);
            Log.Information("Found {0} files to load for a total of {1}", files.Length, Utils.Utils.BytesToString(totalLength));

            // loading screen should be completed after all of this is done

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
