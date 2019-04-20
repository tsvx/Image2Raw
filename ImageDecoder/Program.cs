using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageDecoder
{
    enum RawFormat { Undefined, Gray = 1, RGB = 3}; // Hack: item ordinal means bytes per pixel.

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ImageDecoder <image_file>");
                //Console.WriteLine("   or: ImageDecoder -e <raw_file>");
                return 0;
            }
            string inputFileName = args[0], outputFileName = null;
            try
            {
                outputFileName = DecodeFile(inputFileName);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
            Console.WriteLine($"Decoded {inputFileName} into {outputFileName} successfully.");
            return 0;
        }

        static string DecodeFile(string fileName)
        {
            Size size;
            RawFormat format;
            byte[] bytes;
            using (var bmp = (Bitmap)Image.FromFile(fileName))
            {
                switch (bmp.PixelFormat)
                {
                    case PixelFormat.Format8bppIndexed:
                        format = RawFormat.Gray;
                        break;
                    case PixelFormat.Format24bppRgb:
                        format = RawFormat.RGB;
                        break;
                    default:
                        throw new FormatException($"File {fileName} has unknown format");
                }
                bytes = Decode(bmp, format, out size);
            }
            return SaveBitmap(fileName, format, size, bytes);
        }

        static byte[] Decode(Bitmap bmp, RawFormat format, out Size size)
        {
            int bytesPerPixel = (int)format; // hack
            int lineBytes = bytesPerPixel * bmp.Width;
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
    }
}
