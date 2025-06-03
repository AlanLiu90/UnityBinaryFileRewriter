using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EngineBinaryFileRewriter
{
    internal sealed class StaticLibrary
    {
        public sealed class Item
        {
            public long Offset;
            public long Length;

            public Item(long offset, long length)
            {
                Offset = offset;
                Length = length;
            }
        }

        private readonly Dictionary<string, Item> mItems = new Dictionary<string, Item>();
        private readonly HashSet<string> mDuplicateFile = new HashSet<string>();

        public static StaticLibrary Parse(string file)
        {
            var lib = new StaticLibrary();

            using (var fs = File.OpenRead(file))
            {
                long offset = 8;

                byte[] headerBuffer = new byte[60];
                byte[] nameBuffer = null;

                fs.Seek(offset, SeekOrigin.Begin);

                while (fs.Position < fs.Length)
                {
                    fs.Read(headerBuffer, 0, headerBuffer.Length);

                    string name = Encoding.ASCII.GetString(headerBuffer, 0, 16).TrimEnd('\0');
                    int nameLength = 0;

                    if (name.StartsWith("#1/", StringComparison.Ordinal))
                    {
                        nameLength = int.Parse(name.Substring(3).Trim());

                        if (nameBuffer == null || nameBuffer.Length < nameLength)
                            Array.Resize(ref nameBuffer, nameLength);

                        fs.Read(nameBuffer, 0, nameLength);

                        name = Encoding.ASCII.GetString(nameBuffer, 0, nameLength).TrimEnd('\0');
                    }

                    long size = long.Parse(Encoding.ASCII.GetString(headerBuffer, 48, 10).Trim());

                    if (!lib.mItems.ContainsKey(name))
                        lib.mItems.Add(name, new Item(fs.Position, size - nameLength));
                    else
                        lib.mDuplicateFile.Add(name);

                    offset += 60 + size;

                    fs.Seek(offset, SeekOrigin.Begin);
                }
            }

            return lib;
        }

        public Item GetItem(string name)
        {
            if (mDuplicateFile.Contains(name))
                throw new NotImplementedException($"Duplicate object files: {name}");

            return mItems[name];
        }
    }
}
