using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SMath.Controls;

namespace EngineeringTools
{
    public enum AlignmentMode { Left, Center, Right, Top, Middle, Bottom, HorizontalSpacing, VerticalSpacing, Baseline }

    public static class Alignment
    {
        public static Point[] Positions(Rectangle[] boxes, float[] baselines, AlignmentMode mode)
        {
            if (boxes.Length < 2) throw new ArgumentException("请先框选至少两个区域。");
            Point[] positions = boxes.Select(box => box.Location).ToArray();
            int left = boxes.Min(box => box.Left);
            int right = boxes.Max(box => box.Right);
            int top = boxes.Min(box => box.Top);
            int bottom = boxes.Max(box => box.Bottom);
            if (mode == AlignmentMode.HorizontalSpacing || mode == AlignmentMode.VerticalSpacing)
            {
                if (boxes.Length < 3) throw new ArgumentException("等间距分布需要至少三个区域。");
                bool horizontal = mode == AlignmentMode.HorizontalSpacing;
                int[] order = Enumerable.Range(0, boxes.Length).OrderBy(index => horizontal ? boxes[index].Left : boxes[index].Top).ToArray();
                double total = boxes.Sum(box => horizontal ? box.Width : box.Height);
                double span = horizontal ? right - left : bottom - top;
                double gap = (span - total) / (boxes.Length - 1);
                if (gap < 0) throw new ArgumentException("选区空间不足，等间距分布会重叠，请先拉开两端区域。");
                double cursor = horizontal ? left : top;
                foreach (int index in order)
                {
                    if (horizontal) positions[index].X = (int)Math.Round(cursor);
                    else positions[index].Y = (int)Math.Round(cursor);
                    cursor += (horizontal ? boxes[index].Width : boxes[index].Height) + gap;
                }
                return positions;
            }
            float baseline = Enumerable.Range(0, boxes.Length).Max(index => boxes[index].Top + baselines[index]);
            for (int index = 0; index < boxes.Length; index++)
            {
                switch (mode)
                {
                    case AlignmentMode.Left: positions[index].X = left; break;
                    case AlignmentMode.Center: positions[index].X = (left + right - boxes[index].Width) / 2; break;
                    case AlignmentMode.Right: positions[index].X = right - boxes[index].Width; break;
                    case AlignmentMode.Top: positions[index].Y = top; break;
                    case AlignmentMode.Middle: positions[index].Y = (top + bottom - boxes[index].Height) / 2; break;
                    case AlignmentMode.Bottom: positions[index].Y = bottom - boxes[index].Height; break;
                    case AlignmentMode.Baseline: positions[index].Y = (int)Math.Round(baseline - baselines[index]); break;
                }
            }
            return positions;
        }

        public static void Apply(MenuButtonArgs args, AlignmentMode mode)
        {
            EditableWorksheet worksheet = args.Worksheet as EditableWorksheet;
            if (worksheet == null) throw new InvalidOperationException("当前工作表不可编辑。");
            List<RegionBase> regions = worksheet.GetSelectedRegions();
            if (regions.Any(region => region.FixedLayout)) throw new InvalidOperationException("选区包含固定位置的区域。");
            Rectangle[] boxes = regions.Select(region => new Rectangle(region.Location, region.Size)).ToArray();
            float[] baselines = regions.Select(region => region.MiddleLine).ToArray();
            Point[] positions = Positions(boxes, baselines, mode);
            using (worksheet.History.CreateTransaction(false))
            {
                for (int index = 0; index < regions.Count; index++)
                {
                    if (regions[index].Location == positions[index]) continue;
                    regions[index].Location = positions[index];
                }
            }
            worksheet.SortRegions();
            worksheet.RefreshContent();
            using (RegionsEnumerator enumerator = worksheet.GetRegionsEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    RegionEvaluable first = enumerator.Current as RegionEvaluable;
                    if (first == null) continue;
                    first.RequestEvaluation(true);
                    break;
                }
            }
            worksheet.Invalidate();
        }
    }
}
