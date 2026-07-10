using ImageMagick;
using ImageMagick.Drawing;
using Inkluzitron.Models;
using Inkluzitron.Services;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.Points
{
    public class PointsGraphPaintingStrategy : GraphPaintingStrategy
    {
        public PointsGraphPaintingStrategy()
            : base(
                new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Normal, FontStretch.Condensed),
                new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Medium, FontStretch.Condensed), 
                new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Medium, FontStretch.Condensed),
                new DrawableFont("Open Sans", FontStyleType.Normal, FontWeight.Normal, FontStretch.Condensed)
            )
        {
            CategoryBoxHeight = 1024;
        }

        public override int CalculateColumnCount(IDictionary<string, List<GraphItem>> results)
            => 1;

        public override int CalculateRowCount(IDictionary<string, List<GraphItem>> results)
            => 1;

        public override (int GridLineCount, float Step) CalculateGridLines(float lowerLimit, float upperLimit)
        {
            var targetInnerGridLineCount = 15f;
            var rangeSize = upperLimit - lowerLimit;
            var rawStep = rangeSize / targetInnerGridLineCount;
            var magnitude = (float)Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            var normalizedStep = rawStep / magnitude;
            var step = normalizedStep switch
            {
                < 1.5f => 1.0f,
                < 2.25f => 2.0f,
                < 3.75f => 2.5f,
                < 7.5f => 5.0f,
                _ => 10.0f
            } * magnitude;

            var innerGridLineCount = (int) Math.Floor(rangeSize / step);
            return (innerGridLineCount, step);
        }

        public override float ClampAxisValue(float value)
            => value;

        static private string CreateLabel(float value, string order)
            => $"{value.ToString("F2", CultureInfo.InvariantCulture)}{order}";

        static private string FormatValue(float value)
        {
            const int Million = 1_000_000;
            const int Thousand = 1_000;
            float absVal = Math.Abs(value);

            if (absVal > Million)
                return CreateLabel(value / Million, "m");
            else if (absVal > Thousand)
                return CreateLabel(value / Thousand, "k");
            else
                return value.ToString("F0");
        }

        public override string FormatGridLineValueLabel(float value)
            => FormatValue(value);

        public override string FormatUserValueLabel(float value)
            => FormatValue(value);

        public override (float, float) SmoothenAxisLimits(float minValue, float maxValue)
        {
            var decadicLogarithm = Math.Floor(Math.Log10(maxValue - minValue));
            var largestReachedPowerOfTen = Math.Pow(10, Math.Max(1, decadicLogarithm));

            minValue = 0;
            maxValue += (float) (largestReachedPowerOfTen - (maxValue % largestReachedPowerOfTen));
            return (minValue, maxValue);
        }
    }
}
