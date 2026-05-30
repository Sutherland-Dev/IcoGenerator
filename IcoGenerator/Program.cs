using SkiaSharp;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: IcoGenerator <input.png> <output.ico> [sizes]");
    Console.Error.WriteLine("Example: IcoGenerator logo.png logo.ico 16,20,24,32,40,48,64,128,256");
    return 1;
}

string inputPath = args[0];
string outputPath = args[1];

int[] sizes = args.Length >= 3
    ? args[2]
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .Distinct()
        .OrderBy(x => x)
        .ToArray()
    : new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

foreach (int size in sizes)
{
    if (size < 1 || size > 256)
    {
        Console.Error.WriteLine($"Invalid size: {size}. ICO frame sizes must be between 1 and 256.");
        return 1;
    }
}

using var inputStream = File.OpenRead(inputPath);
using var codec = SKCodec.Create(inputStream);

if (codec is null)
{
    Console.Error.WriteLine("Could not decode input image.");
    return 1;
}

var sourceInfo = codec.Info;
using var originalBitmap = SKBitmap.Decode(codec);

if (originalBitmap is null)
{
    Console.Error.WriteLine("Could not load bitmap from input image.");
    return 1;
}

if (sourceInfo.Width != sourceInfo.Height)
{
    Console.Error.WriteLine($"Input image must be square. Got {sourceInfo.Width}x{sourceInfo.Height}.");
    return 1;
}

var pngFrames = new List<byte[]>();

foreach (int size in sizes)
{
    using var resizedBitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
    using (var canvas = new SKCanvas(resizedBitmap))
    {
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true,
            IsDither = true
        };

        var destRect = new SKRect(0, 0, size, size);
        canvas.DrawBitmap(originalBitmap, destRect, paint);
        canvas.Flush();
    }

    using var image = SKImage.FromBitmap(resizedBitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);

    if (data is null)
    {
        Console.Error.WriteLine($"Failed to encode PNG frame for {size}x{size}.");
        return 1;
    }

    pngFrames.Add(data.ToArray());
}

using var fs = File.Create(outputPath);
using var bw = new BinaryWriter(fs);

WriteIconHeader(bw, pngFrames.Count);

int directorySize = 6 + (16 * pngFrames.Count);
int offset = directorySize;

for (int i = 0; i < sizes.Length; i++)
{
    int size = sizes[i];
    byte[] png = pngFrames[i];

    bw.Write((byte)(size == 256 ? 0 : size));
    bw.Write((byte)(size == 256 ? 0 : size));
    bw.Write((byte)0);
    bw.Write((byte)0);
    bw.Write((ushort)1);
    bw.Write((ushort)32);
    bw.Write((uint)png.Length);
    bw.Write((uint)offset);

    offset += png.Length;
}

foreach (byte[] png in pngFrames)
{
    bw.Write(png);
}

Console.WriteLine($"Wrote {outputPath} with frames: {string.Join(", ", sizes)}");
return 0;

static void WriteIconHeader(BinaryWriter bw, int imageCount)
{
    bw.Write((ushort) 0); // Reserved (0)
    bw.Write((ushort) 1); // Icon Type (1)
    bw.Write((ushort) imageCount);
}