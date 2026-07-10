using ImageMagick;
using ImageMagick.Drawing;
using Inkluzitron.Models;
using Inkluzitron.Services;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class BdsmGraphPaintingStrategy : GraphPaintingStrategy
    {
        public BdsmGraphPaintingStrategy()
            : base(
                  new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Normal, FontStretch.Condensed),
                  new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Bold, FontStretch.Condensed),
                  new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Normal, FontStretch.Condensed),
                  new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Normal, FontStretch.Condensed)
            )
        {
        }

        public override int CalculateColumnCount(IDictionary<string, List<GraphItem>> results)
            => Math.Min(ColumnCount, results.Count);

        public override (int GridLineCount, float Step) CalculateGridLines(float lowerLimit, float upperLimit)
        {
            const float rangeSize = 1.0f;
            const float step = 0.1f;

            var innerGridLineCount = (int)Math.Floor(rangeSize / step);
            return (innerGridLineCount, step);
        }

        public override int CalculateRowCount(IDictionary<string, List<GraphItem>> results)
            => (int)Math.Ceiling(results.Count / (1f * CalculateColumnCount(results)));

        public override float ClampAxisValue(float value)
            => Clamp(value, 0f, 1f);

        public override string FormatGridLineValueLabel(float value)
            => value.ToString("P0", CultureInfo.InvariantCulture);

        public override string FormatUserValueLabel(float value)
            => (100 * value).ToString("N0");

        public override (float, float) SmoothenAxisLimits(float minValue, float maxValue)
        {
            minValue = 0;
            maxValue = 1;
            return (minValue, maxValue);
        }
    }
}
