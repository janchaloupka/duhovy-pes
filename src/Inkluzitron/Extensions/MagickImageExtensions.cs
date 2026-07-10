using ImageMagick;
using ImageMagick.Drawing;
using System.Linq;
using System.Security;

namespace Inkluzitron.Extensions
{
    static public class MagickImageExtensions
    {
        static public void RoundImage(this IMagickImage<byte> image)
        {
            using var mask = new MagickImage(MagickColors.Black, image.Width, image.Height);
            new Drawables()
                .FillColor(MagickColors.White)
                .Circle(image.Width / 2, image.Height / 2, image.Width / 2, 0)
                .Draw(mask);

            image.Alpha(AlphaOption.On);
            mask.Alpha(AlphaOption.Off);
            image.Composite(mask, CompositeOperator.CopyAlpha);
        }

        static public IMagickColor<byte> GetDominantColor(this IMagickImage<byte> image)
        {
            using var img = image.Clone();
            img.HasAlpha = false;
            img.InterpolativeResize(32, 32, PixelInterpolateMethod.Average);
            img.Quantize(new QuantizeSettings() { Colors = 8 });
            var histogram = img.Histogram();

            var dominantColor = histogram.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            return dominantColor;
        }

        static public void DrawEnhancedText(
            this IMagickImage<byte> image, string text, int x, int y, MagickColor foreground,
            DrawableFont font, double fontPointSize, uint maxWidth, bool ellipsize = true)
            => DrawEnhancedText(image, text, Gravity.Undefined, x, y, foreground, font, fontPointSize, maxWidth, ellipsize);

        static public void DrawEnhancedText(
            this IMagickImage<byte> image, string text, Gravity gravity, int x, int y, MagickColor foreground,
            DrawableFont font, double fontPointSize, uint maxWidth, bool ellipsize = true)
        {
            using var enhancedText = PrepareEnhancedText(text, gravity, foreground, font, fontPointSize, maxWidth, ellipsize: ellipsize);
            image.Composite(enhancedText, x, y, CompositeOperator.Over);
        }

        static public MagickImage PrepareEnhancedText(string text, Gravity gravity, MagickColor foreground,
            DrawableFont font, double fontPointSize, uint maxWidth, bool ellipsize = true)
        {
            var settings = new MagickReadSettings()
            {
                BackgroundColor = MagickColors.Transparent,
                TextGravity = gravity,
                TextAntiAlias = false
            };

            if (ellipsize)
            {
                settings.Width = maxWidth;
                settings.SetDefine("pango:ellipsize", "end");
            }

            //settings.SetDefine("pango:wrap", "char");                

            // Escape text for use in pango markup language
            // For some reason the text must be excaped twice otherwise it will not work
            text = SecurityElement.Escape(SecurityElement.Escape(text)).Replace("%", "%%");

            // Map your ImageMagick types to strings Pango natively understands
            string styleString = font.Style == FontStyleType.Italic ? "Italic" : "Normal";
            string weightString = ((int)font.Weight).ToString(); // E.g., "400", "700"
            string stretchString = font.Stretch.ToString();       // E.g., "Condensed"

            // Combine everything into a clean Pango Font Description string
            string fontDesc = $"{font.Family} {stretchString} {styleString} {weightString}";

            using var textArea = new MagickImage($@"pango:<span
                size=""{fontPointSize * 1000}""
                font_desc=""{fontDesc}""
                foreground=""white""
                >{text}</span>", settings);

            var colored = new MagickImage(foreground, textArea.Width, textArea.Height);
            colored.Alpha(AlphaOption.On);
            colored.Composite(textArea, CompositeOperator.In);
            return colored;
        }
    }
}
