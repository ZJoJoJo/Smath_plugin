using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using SMath.Controls;

namespace EngineeringTools
{
    public sealed class PdfExportSettings
    {
        public double WidthMillimeters = 210;
        public double HeightMillimeters = 297;
        public double MarginMillimeters = 12;
        public int Dpi = 300;
        public bool PageNumbers = true;
    }
    public sealed class ExportRegion
    {
        public RegionBase Region;
        public Rectangle Bounds;
    }
    public static class WorksheetPdf
    {
        public static List<double> PageBreaks(IEnumerable<Rectangle> rectangles, double pageHeight)
        {
            Rectangle[] bounds = rectangles.ToArray();
            double end = bounds.Length == 0 ? 0 : bounds.Max(box => box.Bottom);
            List<double> breaks = new List<double> { 0 };
            while (breaks[breaks.Count - 1] + pageHeight < end)
            {
                double start = breaks[breaks.Count - 1];
                double next = start + pageHeight;
                bool moved;
                do
                {
                    moved = false;
                    foreach (Rectangle box in bounds)
                    {
                        if (box.Top >= next || box.Bottom <= next || box.Height > pageHeight) continue;
                        if (box.Top > start + 1) { next = box.Top; moved = true; }
                    }
                } while (moved);
                if (next <= start + 1) next = start + pageHeight;
                breaks.Add(next);
            }
            return breaks;
        }
        public static int Export(Worksheet worksheet, string path, PdfExportSettings settings)
        {
            if (settings.Dpi < 96 || settings.Dpi > 600 || settings.MarginMillimeters < 0 || settings.MarginMillimeters * 2 >= Math.Min(settings.WidthMillimeters, settings.HeightMillimeters))
                throw new ArgumentException("PDF 分辨率或页边距无效。");
            List<ExportRegion> regions = new List<ExportRegion>();
            using (RegionsEnumerator enumerator = worksheet.GetRegionsEnumerator())
            {
                enumerator.FilterInvisible = true;
                while (enumerator.MoveNext())
                {
                    RegionBase region = enumerator.Current;
                    if (region.Container != null && !region.Container.GetType().Name.Contains("AreaRegion")) continue;
                    if (region.Size.Width <= 0 || region.Size.Height <= 0) continue;
                    regions.Add(new ExportRegion { Region = region, Bounds = new Rectangle(enumerator.AbsoluteLocation, region.Size) });
                }
            }
            if (regions.Count == 0) throw new InvalidOperationException("当前计算书没有可导出的内容。");
            double contentMillimeters = settings.WidthMillimeters - 2 * settings.MarginMillimeters;
            double origin = Math.Min(0, regions.Min(region => region.Bounds.Left));
            double logicalWidth = Math.Max(749, regions.Max(region => region.Bounds.Right) - origin);
            double millimetersPerLogicalUnit = contentMillimeters / logicalWidth;
            double logicalHeight = (settings.HeightMillimeters - 2 * settings.MarginMillimeters - (settings.PageNumbers ? 6 : 0)) / millimetersPerLogicalUnit;
            List<double> breaks = PageBreaks(regions.Select(region => region.Bounds), logicalHeight);
            int pixelWidth = (int)Math.Round(settings.WidthMillimeters / 25.4 * settings.Dpi);
            int pixelHeight = (int)Math.Round(settings.HeightMillimeters / 25.4 * settings.Dpi);
            float margin = (float)(settings.MarginMillimeters / 25.4 * settings.Dpi);
            float scale = (float)(millimetersPerLogicalUnit / 25.4 * settings.Dpi);
            List<RasterPdfPage> pages = new List<RasterPdfPage>();
            for (int pageIndex = 0; pageIndex < breaks.Count; pageIndex++)
            {
                double pageTop = breaks[pageIndex];
                double pageBottom = pageIndex + 1 < breaks.Count ? breaks[pageIndex + 1] : pageTop + logicalHeight;
                using (Bitmap bitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format24bppRgb))
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    bitmap.SetResolution(settings.Dpi, settings.Dpi);
                    graphics.Clear(Color.White);
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    if (settings.PageNumbers)
                    {
                        using (Font font = new Font("Arial", 8))
                        using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
                            graphics.DrawString((pageIndex + 1) + " / " + breaks.Count, font, Brushes.DimGray, new RectangleF(0, pixelHeight - margin, pixelWidth, margin), format);
                    }
                    graphics.TranslateTransform(margin, margin);
                    graphics.ScaleTransform(scale, scale);
                    using (SMath.Drawing.IGraphics canvas = new SMath.Drawing.Graphics(graphics))
                    {
                        canvas.SaveAndClip(new RectangleF(0, 0, (float)logicalWidth, (float)logicalHeight));
                        foreach (ExportRegion region in regions)
                        {
                            if (region.Bounds.Bottom <= pageTop || region.Bounds.Top >= pageBottom) continue;
                            bool selected = region.Region.Selected;
                            region.Region.Selected = false;
                            try
                            {
                                canvas.Save();
                                canvas.Translate((float)(region.Bounds.Left - origin), (float)(region.Bounds.Top - pageTop));
                                region.Region.DrawToBitmap(canvas);
                                canvas.Restore();
                            }
                            finally { region.Region.Selected = selected; }
                        }
                        canvas.Restore();
                    }
                    pages.Add(PdfWriter.Rasterize(bitmap, settings.WidthMillimeters / 25.4 * 72, settings.HeightMillimeters / 25.4 * 72));
                }
            }
            PdfWriter.Save(path, pages);
            return pages.Count;
        }
    }
}
