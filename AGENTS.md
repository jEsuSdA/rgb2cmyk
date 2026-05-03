# AGENTS.md

## What this is

Two shell scripts for RGB→CMYK image conversion using ImageMagick (`convert`). Bundled ICC profiles: `srgb-color-space-profile.icm` (source RGB) and `psouncoated_v3_fogra52.icc` (target CMYK, PSO Uncoated v3 FOGRA52). Comments and output are in Spanish.

A cross-platform GUI app (`rgb2cmyk-gui/`) built with .NET 8 + Avalonia + Magick.NET that replicates the same conversions with a file picker, profile selector, and configurable options.

## Usage

### Shell scripts (require ImageMagick installed)

- `./img2cmyk.sh <image>` — full CMYK conversion, outputs JPG
- `./img2cmyk.sh <image> tif` — full CMYK conversion, outputs compressed TIFF
- `./img2cmyk-black-only.sh <image>` — K-channel-only CMYK grayscale, outputs TIFF only

### GUI app (self-contained, no external deps)

```bash
cd rgb2cmyk-gui
source setup.sh      # first time: downloads .NET 8 SDK to .dotnet/
./run.sh             # run on Linux
./build.sh           # publish to publish/linux-x64/, publish/win-x64/, and publish/osx-arm64/
```

Output binaries: ~120–130 MB each (self-contained, include ImageMagick native libs). macOS version packaged as `.app` bundle inside a `.zip` (~55 MB compressed).

## Gotchas

- **`MAGICK_TMPDIR` is hardcoded to `/mnt/cajon`** in both shell scripts. Must be changed per environment or commented out; ImageMagick will fall back to `/tmp`.
- **ImageMagick resource limits** may need increasing in `/etc/ImageMagick-6/policy.xml` for large images. Both scripts document the recommended values inline.
- The `COLORSPACE` and `DENSITY` variables in `img2cmyk.sh` are assigned multiple times; only the last assignment takes effect (read the code, not the intermediate assignments).
- **`LD_PRELOAD=libgtk3-nocsd.so.0`** (from `gtk3-nocsd` package) crashes Magick.NET at startup. The `run.sh` wrapper and publish wrapper both `unset LD_PRELOAD`. Direct execution requires `LD_PRELOAD= ./rgb2cmyk-gui`.

## GUI app architecture

- `src/Conversion/CmykConverter.cs` — core conversion logic using Magick.NET
  - Unified `Convert(inputPath, CmykConversionOptions)` method handles both Full CMYK and K-Only
  - `CmykConversionOptions`: Mode, CmykProfile, OutputFormat (JPG/TIF), RenderingIntent, BlackPointCompensation, JpgQuality, DensityMode
  - `ConvertFullCmyk()`: sets RenderingIntent + BPC before `SetProfile()` (order matters — conversion triggers on the second profile)
  - `ConvertKOnly()`: grayscale → negate → combine as CMYK with C=M=Y=0, K=negated gray
  - `CmykProfileInfo`: pairs display name with embedded resource; `EmbeddedProfiles` list is the single source of truth for the profile dropdown
  - `LoadProfileFromDisk(path)`: loads arbitrary `.icc`/`.icm` at runtime
- ICC profiles are embedded in the binary as `EmbeddedResource` — no external files needed at runtime
- `setup.sh` uses `${BASH_SOURCE[0]}` (not `$0`) so it works when sourced from other scripts
- Magick.NET v14 API: `SetProfile()` (not `AddProfile`), `image.Settings.Compression` (not `image.CompressionMethod`), `ApplyDensity` takes `IMagickImage<byte>` (not just `MagickImage`)
- `dotnet publish -r win-x64 --self-contained` from Linux produces a working `.exe` (native Windows DLLs come from NuGet)
- csproj post-publish targets: replaces `libSkiaSharp.so` with NoDependencies version (avoids fontconfig crash on older Debian), generates `run.sh` wrapper with `unset LD_PRELOAD`
- macOS build: `dotnet publish -r osx-arm64 --self-contained` from Linux; csproj post-publish target creates `.app` bundle with `Info.plist`, icon, and all published files. Distributed as `.zip`
- `Magick.NET-Q8-arm64` must use `<ExcludeAssets>compile</ExcludeAssets>` in csproj to avoid type conflicts with the x64 package (both expose the same `MagickImage` types)
- Wine 5.x cannot run .NET 8 apps — Windows native or Wine 8+ required
