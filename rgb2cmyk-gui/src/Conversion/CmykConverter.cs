using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ImageMagick;

namespace Rgb2CmykGui.Conversion;

public enum CmykMode { FullCmyk, KOnly }
public enum OutputFormat { Jpg, Tif }
public enum DensityMode { PreserveOriginal, Ppi300 }

public class CmykConversionOptions
{
    public CmykMode Mode { get; set; } = CmykMode.FullCmyk;
    public ColorProfile CmykProfile { get; set; } = null!;
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Jpg;
    public RenderingIntent RenderingIntent { get; set; } = RenderingIntent.Relative;
    public bool BlackPointCompensation { get; set; } = true;
    public int JpgQuality { get; set; } = 90;
    public DensityMode DensityMode { get; set; } = DensityMode.PreserveOriginal;
}

public class CmykProfileInfo
{
    public string DisplayName { get; }
    public string ResourceName { get; }
    public ColorProfile Profile { get; }

    public CmykProfileInfo(string displayName, string resourceName)
    {
        DisplayName = displayName;
        ResourceName = resourceName;
        Profile = LoadEmbeddedResource(resourceName);
    }

    private static ColorProfile LoadEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new ColorProfile(ms.ToArray());
    }
}

public static class CmykConverter
{
    private static ColorProfile? _srgbProfile;

    public static readonly List<CmykProfileInfo> EmbeddedProfiles = new()
    {
        new CmykProfileInfo("PSO Uncoated v3 (FOGRA52)", "psouncoated_v3_fogra52.icc")
    };

    public static ColorProfile GetSrgbProfile()
    {
        if (_srgbProfile == null)
        {
            var bytes = ReadEmbeddedResource("srgb-color-space-profile.icm");
            _srgbProfile = new ColorProfile(bytes);
        }
        return _srgbProfile;
    }

    public static ColorProfile LoadProfileFromDisk(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return new ColorProfile(bytes);
    }

    private static byte[] ReadEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string GetOutputPath(string inputPath, string suffix, string extension)
    {
        var dir = Path.GetDirectoryName(inputPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(dir, $"{name}{suffix}.{extension}");
    }

    private static void ApplyDensity(IMagickImage<byte> image, DensityMode densityMode)
    {
        switch (densityMode)
        {
            case DensityMode.Ppi300:
                image.Density = new Density(300, 300, DensityUnit.PixelsPerInch);
                break;
            case DensityMode.PreserveOriginal:
            default:
                break;
        }
    }

    public static string Convert(string inputPath, CmykConversionOptions options)
    {
        var modeSuffix = options.Mode == CmykMode.KOnly ? "CMYK-Key-only" : "CMYK";
        var ext = options.OutputFormat == OutputFormat.Tif ? "tif" : "jpg";
        var outputPath = GetOutputPath(inputPath, modeSuffix, ext);

        if (options.Mode == CmykMode.FullCmyk)
        {
            ConvertFullCmyk(inputPath, outputPath, options);
        }
        else
        {
            ConvertKOnly(inputPath, outputPath, options);
        }

        return outputPath;
    }

    private static void ConvertFullCmyk(string inputPath, string outputPath, CmykConversionOptions options)
    {
        using var image = new MagickImage(inputPath);
        image.Alpha(AlphaOption.Off);
        ApplyDensity(image, options.DensityMode);

        image.RenderingIntent = options.RenderingIntent;
        image.BlackPointCompensation = options.BlackPointCompensation;

        image.SetProfile(GetSrgbProfile());
        image.SetProfile(options.CmykProfile);

        if (options.OutputFormat == OutputFormat.Jpg)
        {
            image.Format = MagickFormat.Jpeg;
            image.Quality = (uint)options.JpgQuality;
        }
        else
        {
            image.Format = MagickFormat.Tiff;
            image.Settings.Compression = CompressionMethod.Zip;
        }

        image.Write(outputPath);
    }

    private static void ConvertKOnly(string inputPath, string outputPath, CmykConversionOptions options)
    {
        using var image = new MagickImage(inputPath);
        image.Alpha(AlphaOption.Off);
        ApplyDensity(image, options.DensityMode);
        image.Grayscale();

        using var noInk = new MagickImage(MagickColors.Black, image.Width, image.Height);
        noInk.Alpha(AlphaOption.Off);
        noInk.ColorType = ColorType.Grayscale;

        image.ColorType = ColorType.Grayscale;
        using var kChannel = image.Clone();
        kChannel.Negate();

        using var collection = new MagickImageCollection();
        collection.Add(noInk.Clone());
        collection.Add(noInk.Clone());
        collection.Add(noInk.Clone());
        collection.Add(kChannel.Clone());

        using var result = collection.Combine(ColorSpace.CMYK);
        ApplyDensity(result, options.DensityMode);

        if (options.OutputFormat == OutputFormat.Jpg)
        {
            result.Format = MagickFormat.Jpeg;
            result.Quality = (uint)options.JpgQuality;
        }
        else
        {
            result.Format = MagickFormat.Tiff;
            result.Settings.Compression = CompressionMethod.Zip;
        }

        result.Write(outputPath);
    }
}
