using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageDecoder
{
    enum RawFormat { Undefined, Gray, RGB};

    class Program
    {
        class FormatLink
        {
            public RawFormat Raw;
            public PixelFormat Pixel;
            public int BytesPerPixel;
        }

        static readonly FormatLink[] Formats = new[]
        {
            new FormatLink { Raw = RawFormat.Gray, BytesPerPixel = 1, Pixel = PixelFormat.Format8bppIndexed},
            new FormatLink { Raw = RawFormat.RGB,  BytesPerPixel = 3, Pixel = PixelFormat.Format24bppRgb}
        };

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ImageDecoder <image_file>");
                Console.WriteLine("   or: ImageDecoder <raw_file>");
                return 0;
            }
            string inputFileName = args[0], outputFileName = null;
            bool encode = Path.GetExtension(inputFileName).ToLowerInvariant() == ".raw";
            try
            {
                outputFileName = encode ?
                    EncodeFile(inputFileName) //, true) // for jpeg
                    : DecodeFile(inputFileName);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
            Console.WriteLine($"{(encode ? "En" : "De")}coded \"{inputFileName}\" into \"{outputFileName}\" successfully.");
            return 0;
        }

        static string DecodeFile(string fileName)
        {
            Size size;
            FormatLink link;
            byte[] bytes;
            using (var bmp = (Bitmap)Image.FromFile(fileName))
            {
                link = Formats.FirstOrDefault(fl => fl.Pixel == bmp.PixelFormat)
                    ?? throw new FormatException($"File {fileName} has unsupported pixel format {bmp.PixelFormat}.");
                bytes = Decode(bmp, link, out size);
            }
            return SaveBitmap(fileName, link.Raw, size, bytes);
        }

        static byte[] Decode(Bitmap bmp, FormatLink link, out Size size)
        {
            int lineBytes = link.BytesPerPixel * bmp.Width;
            byte[] data = new byte[bmp.Height * lineBytes];

            size = new Size(bmp.Width, bmp.Height);
            var rect = new Rectangle(Point.Empty, size);
            var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

            IntPtr ptr = bd.Scan0;
            int bytesCopied = 0;
            for (int y = 0; y < bmp.Height; y++)
            {
                Marshal.Copy(ptr, data, bytesCopied, lineBytes);
                ptr += bd.Stride;
                bytesCopied += lineBytes;

            }
            bmp.UnlockBits(bd);
            return data;
        }

        static string SaveBitmap(string origName, RawFormat format, Size size, byte[] data)
        {
            string rawName = Path.ChangeExtension(origName, $"{format}-{size.Width}x{size.Height}.raw");
            File.WriteAllBytes(rawName, data);
            return rawName;
        }

        static string EncodeFile(string fileName, bool toJpeg = false)
        {
            var m = Regex.Match(fileName, @"^(.+)\.(\w+)-(\d+)x(\d+)\.raw$");
            if (!m.Success
                || !Enum.TryParse<RawFormat>(m.Groups[2].Value, true, out var format)
                || !int.TryParse(m.Groups[3].Value, out int width)
                || !int.TryParse(m.Groups[4].Value, out int height)
               )
                throw new FormatException($"Input raw file name {fileName} has wrong format.");
            string oext = toJpeg ? "jpg" : "png";
            string oname = m.Groups[1].Value + "." + oext;
            var link = Formats.FirstOrDefault(fl => fl.Raw == format)
                ?? throw new FormatException($"Raw file {fileName} has unsupported format {format}.");
            var bytes = File.ReadAllBytes(fileName);
            using (var bmp = Encode(bytes, width, height, link))
            {
                bmp.Save(oname, toJpeg ? ImageFormat.Jpeg : ImageFormat.Png);
            }
            return oname;
        }

        private static Bitmap Encode(byte[] data, int width, int height, FormatLink link)
        {
            int lineBytes = link.BytesPerPixel * width;
            if (data.Length != height * lineBytes)
                throw new FormatException($"Wrong raw file size {data.Length}, should be {height * lineBytes} bytes.");

            var bmp = new Bitmap(width, height, link.Pixel);
            var rect = new Rectangle(Point.Empty, new Size(width, height));
            var bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, link.Pixel);

            IntPtr ptr = bd.Scan0;
            int bytesCopied = 0;
            for (int y = 0; y < bmp.Height; y++)
            {
                Marshal.Copy(data, bytesCopied, ptr, lineBytes);
                ptr += bd.Stride;
                bytesCopied += lineBytes;
            }
            bmp.UnlockBits(bd);

            if (link.Raw == RawFormat.Gray)
            {
                var colorPalette = bmp.Palette;
                for (int i = 0; i < 256; i++)
                {
                    colorPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bmp.Palette = colorPalette;
            }

            return bmp;
        }
    }
}
