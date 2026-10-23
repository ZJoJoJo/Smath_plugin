using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace EngineeringTools
{
    public sealed class RasterPdfPage
    {
        public int PixelWidth;
        public int PixelHeight;
        public double WidthPoints;
        public double HeightPoints;
        public byte[] CompressedPixels;
    }

    public static class PdfWriter
    {
        private static readonly Encoding Ascii = Encoding.ASCII;
        private static string Number(double value) { return value.ToString("0.#####", CultureInfo.InvariantCulture); }
        private static void Write(Stream stream, string value) { byte[] bytes = Ascii.GetBytes(value); stream.Write(bytes, 0, bytes.Length); }
        public static RasterPdfPage Rasterize(Bitmap bitmap, double widthPoints, double heightPoints)
        {
            uint first = 1, second = 0;
            using (MemoryStream packed = new MemoryStream())
            {
                packed.WriteByte(0x78); packed.WriteByte(0x9c);
                BitmapData pixels = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    byte[] source = new byte[Math.Abs(pixels.Stride)];
                    byte[] row = new byte[bitmap.Width * 3];
                    using (DeflateStream compressor = new DeflateStream(packed, CompressionLevel.Optimal, true))
                    {
                        for (int vertical = 0; vertical < bitmap.Height; vertical++)
                        {
                            Marshal.Copy(IntPtr.Add(pixels.Scan0, vertical * pixels.Stride), source, 0, source.Length);
                            for (int horizontal = 0; horizontal < bitmap.Width; horizontal++)
                            {
                                row[3 * horizontal] = source[3 * horizontal + 2];
                                row[3 * horizontal + 1] = source[3 * horizontal + 1];
                                row[3 * horizontal + 2] = source[3 * horizontal];
                            }
                            foreach (byte value in row) { first = (first + value) % 65521; second = (second + first) % 65521; }
                            compressor.Write(row, 0, row.Length);
                        }
                    }
                }
                finally { bitmap.UnlockBits(pixels); }
                uint checksum = (second << 16) | first;
                packed.WriteByte((byte)(checksum >> 24)); packed.WriteByte((byte)(checksum >> 16)); packed.WriteByte((byte)(checksum >> 8)); packed.WriteByte((byte)checksum);
                return new RasterPdfPage { PixelWidth = bitmap.Width, PixelHeight = bitmap.Height, WidthPoints = widthPoints, HeightPoints = heightPoints, CompressedPixels = packed.ToArray() };
            }
        }
        public static void Save(string path, IList<RasterPdfPage> pages)
        {
            if (pages.Count == 0) throw new ArgumentException("没有可导出的页面。");
            string temporary = path + ".writing-" + Guid.NewGuid().ToString("N");
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                {
                    List<long> offsets = new List<long> { 0 };
                    Write(stream, "%PDF-1.4\n");
                    byte[] signature = { 37, 226, 227, 207, 211, 10 }; stream.Write(signature, 0, signature.Length);
                    Action<int, string> objectText = delegate(int identifier, string body) { offsets.Add(stream.Position); Write(stream, identifier + " 0 obj\n" + body + "\nendobj\n"); };
                    objectText(1, "<< /Type /Catalog /Pages 2 0 R >>");
                    StringBuilder children = new StringBuilder();
                    for (int index = 0; index < pages.Count; index++) children.Append((3 + index * 3) + " 0 R ");
                    objectText(2, "<< /Type /Pages /Count " + pages.Count + " /Kids [ " + children + "] >>");
                    for (int index = 0; index < pages.Count; index++)
                    {
                        RasterPdfPage page = pages[index];
                        int identifier = 3 + index * 3;
                        objectText(identifier, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + Number(page.WidthPoints) + " " + Number(page.HeightPoints) + "] /Resources << /XObject << /Im0 " + (identifier + 1) + " 0 R >> >> /Contents " + (identifier + 2) + " 0 R >>");
                        offsets.Add(stream.Position);
                        Write(stream, (identifier + 1) + " 0 obj\n<< /Type /XObject /Subtype /Image /Width " + page.PixelWidth + " /Height " + page.PixelHeight + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length " + page.CompressedPixels.Length + " >>\nstream\n");
                        stream.Write(page.CompressedPixels, 0, page.CompressedPixels.Length); Write(stream, "\nendstream\nendobj\n");
                        string content = "q\n" + Number(page.WidthPoints) + " 0 0 " + Number(page.HeightPoints) + " 0 0 cm\n/Im0 Do\nQ\n";
                        objectText(identifier + 2, "<< /Length " + Ascii.GetByteCount(content) + " >>\nstream\n" + content + "endstream");
                    }
                    long crossReference = stream.Position;
                    Write(stream, "xref\n0 " + offsets.Count + "\n0000000000 65535 f \n");
                    for (int index = 1; index < offsets.Count; index++) Write(stream, offsets[index].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
                    Write(stream, "trailer\n<< /Size " + offsets.Count + " /Root 1 0 R >>\nstartxref\n" + crossReference + "\n%%EOF\n");
                }
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
