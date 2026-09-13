using StbTrueTypeSharp;

namespace Entropy.Engine.Rendering;

public class FontAtlas(string ttfPath)
    : GlyphAtlas(BuildPixels(ttfPath, out var width, out var height), width, height, 16, 16)
{
    private const int CellWidth = 8;
    private const int CellHeight = 16;
    private const int SuperSample = 2;
    private const float InkFraction = 0.95f;
    private const float WidthFactor = 1f;

    private static byte[] BuildPixels(string ttfPath, out int width, out int height)
    {
        const int cellsX = 16;
        const int cellsY = 16;
        width = cellsX * CellWidth;
        height = cellsY * CellHeight;
        var pixels = new byte[width * height * 4];

        var fontData = File.ReadAllBytes(ttfPath);
        var font = StbTrueType.CreateFont(fontData, 0);

        unsafe
        {
            int ascent, descent, lineGap;

            StbTrueType.stbtt_GetFontVMetrics(
                font,
                &ascent,
                &descent,
                &lineGap);


            var scale = CellHeight * InkFraction * SuperSample / (ascent - descent);

            var scaleX = scale * WidthFactor;
            var scaleY = scale;
            var baseline = (int)MathF.Round(
                ascent * scale / SuperSample);

            for (var codepoint = 0; codepoint < cellsX * cellsY; codepoint++)
            {
                var cellX = (codepoint % cellsX) * CellWidth;
                var cellY = (codepoint / cellsX) * CellHeight;

                if (TryDrawBlock(
                        codepoint,
                        pixels,
                        width,
                        height,
                        cellX,
                        cellY,
                        CellWidth,
                        CellHeight))
                {
                    continue;
                }

                if (codepoint < 32)
                    continue;

                int x0, y0, x1, y1;

                StbTrueType.stbtt_GetCodepointBitmapBox(
                    font,
                    codepoint,
                    scaleX,
                    scaleY,
                    &x0,
                    &y0,
                    &x1,
                    &y1);

                var glyphWidth = x1 - x0;
                var glyphHeight = y1 - y0;

                if (glyphWidth <= 0 || glyphHeight <= 0)
                    continue;

                var glyph = new byte[glyphWidth * glyphHeight];

                fixed (byte* glyphPtr = glyph)
                {
                    StbTrueType.stbtt_MakeCodepointBitmap(
                        font,
                        glyphPtr,
                        glyphWidth,
                        glyphHeight,
                        glyphWidth,
                        scaleX,
                        scaleY,
                        codepoint);
                }


                var destWidth =
                    (glyphWidth + SuperSample - 1) / SuperSample;

                var destHeight =
                    (glyphHeight + SuperSample - 1) / SuperSample;

                for (var dy = 0; dy < destHeight; dy++)
                for (var dx = 0; dx < destWidth; dx++)
                {
                    var total = 0;
                    var samples = 0;

                    for (var sy = 0; sy < SuperSample; sy++)
                    for (var sx = 0; sx < SuperSample; sx++)
                    {
                        var gx = dx * SuperSample + sx;
                        var gy = dy * SuperSample + sy;

                        if (gx >= glyphWidth || gy >= glyphHeight)
                            continue;

                        total += glyph[gy * glyphWidth + gx];
                        samples++;
                    }

                    if (samples == 0)
                        continue;

                    var alpha = (byte)(total / samples);

                    if (alpha == 0)
                        continue;

                    var glyphX =
                        (int)MathF.Round(x0 / (float)SuperSample) + dx;

                    var glyphY =
                        baseline +
                        (int)MathF.Round(y0 / (float)SuperSample) +
                        dy;

                    var px = cellX + glyphX;
                    var py = cellY + glyphY;

                    if (px < cellX || px >= cellX + CellWidth ||
                        py < cellY || py >= cellY + CellHeight)
                        continue;

                    var index = (py * width + px) * 4;

                    pixels[index] = 255;
                    pixels[index + 1] = 255;
                    pixels[index + 2] = 255;
                    pixels[index + 3] =
                        Math.Max(pixels[index + 3], alpha);
                }
            }
        }

        return pixels;
    }

    private static bool TryDrawBlock(
        int codepoint, byte[] pixels, int width, int height,
        int cellX, int cellY, int cellWidth, int cellHeight)
    {
        switch (codepoint)
        {
            case 219: // full block
                Fill(pixels, width, height, cellX, cellY, cellWidth, cellHeight);
                return true;
            case 220: // lower half
                Fill(pixels, width, height, cellX, cellY + cellHeight / 2, cellWidth, cellHeight - cellHeight / 2);
                return true;
            case 223: // upper half
                Fill(pixels, width, height, cellX, cellY, cellWidth, cellHeight / 2);
                return true;
            case 254: // filled square
                Fill(pixels, width, height, cellX + 1, cellY + 1, cellWidth - 2, cellHeight - 2);
                return true;
            case 250: // middle dot
                Fill(pixels, width, height, cellX + cellWidth / 2, cellY + cellHeight / 2, 1, 1);
                return true;
            case 176: // light shade
                Shade(pixels, width, height, cellX, cellY, cellWidth, cellHeight, (x, y) => x % 2 == 0 && y % 2 == 0);
                return true;
            case 177: // medium shade
                Shade(pixels, width, height, cellX, cellY, cellWidth, cellHeight, (x, y) => (x + y) % 2 == 0);
                return true;
            case 178: // dark shade
                Shade(pixels, width, height, cellX, cellY, cellWidth, cellHeight, (x, y) => (x + y) % 2 != 0);
                return true;
            default:
                return false;
        }
    }

    private static void Fill(
        byte[] pixels, int width, int height,
        int px, int py, int w, int h)
    {
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var ax = px + x;
            var ay = py + y;
            if (ax < 0 || ax >= width || ay < 0 || ay >= height)
                continue;

            var index = (ay * width + ax) * 4;
            pixels[index] = 255;
            pixels[index + 1] = 255;
            pixels[index + 2] = 255;
            pixels[index + 3] = 255;
        }
    }

    private static void Shade(
        byte[] pixels, int width, int height,
        int px, int py, int cellWidth, int cellHeight, Func<int, int, bool> on)
    {
        for (var y = 0; y < cellHeight; y++)
        for (var x = 0; x < cellWidth; x++)
        {
            if (!on(x, y))
                continue;

            var ax = px + x;
            var ay = py + y;
            if (ax < 0 || ax >= width || ay < 0 || ay >= height)
                continue;

            var index = (ay * width + ax) * 4;
            pixels[index] = 255;
            pixels[index + 1] = 255;
            pixels[index + 2] = 255;
            pixels[index + 3] = 255;
        }
    }
}
