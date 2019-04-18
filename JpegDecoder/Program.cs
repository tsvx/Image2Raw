using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpegDecoder
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: JpegDecoder <JPEG_file>");
                return 0;
            }
            string jpegName = args[0];
            try
            {
                var bmp = (Bitmap)Image.FromFile(jpegName);
                var bytes = Decode(bmp, out var size);
                SaveBitmap(jpegName, size, bytes);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
            return 0;
        }

        static byte[] Decode(Bitmap bmp, out Size size)
        {
            size = new Size(bmp.Width, bmp.Height);

            return null;
        }

        static void SaveBitmap(string origName, Size size, byte[] data)
        {

        }
    }
}
