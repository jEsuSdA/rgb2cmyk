using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ImageMagick;
using Rgb2CmykGui.Conversion;

namespace Rgb2CmykGui.Views;

public partial class MainWindow : Window
{
    private string? _inputPath;
    private ColorProfile? _customProfile;
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
        _initialized = true;
        PopulateProfileCombo();
        this.FindControl<Slider>("QualitySlider")!.ValueChanged += OnQualitySliderChanged;
    }

    private void OnQualitySliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        this.FindControl<TextBlock>("QualityValue")!.Text = ((int)e.NewValue).ToString();
    }

    private void PopulateProfileCombo()
    {
        var combo = this.FindControl<ComboBox>("ProfileCombo")!;
        combo.Items.Clear();

        foreach (var p in CmykConverter.EmbeddedProfiles)
        {
            combo.Items.Add(new ComboBoxItem { Content = p.DisplayName, Tag = p });
        }

        combo.Items.Add(new ComboBoxItem { Content = "Cargar perfil desde disco…", Tag = null });
        combo.SelectedIndex = 0;
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.tif", "*.tiff", "*.bmp", "*.webp" }
                }
            }
        });

        if (files.Count > 0)
        {
            _inputPath = files[0].TryGetLocalPath();
            if (_inputPath == null) return;

            this.FindControl<TextBox>("FilePathBox")!.Text = _inputPath;
            LoadPreview(_inputPath);
            this.FindControl<Button>("ConvertBtn")!.IsEnabled = true;
            this.FindControl<TextBlock>("StatusBlock")!.Text = "";
        }
    }

    private async void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        var combo = this.FindControl<ComboBox>("ProfileCombo")!;
        if (combo.SelectedIndex < 0) return;

        var item = (ComboBoxItem)combo.Items[combo.SelectedIndex]!;
        if (item.Tag == null && combo.SelectedIndex == CmykConverter.EmbeddedProfiles.Count)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar perfil ICC",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Perfiles ICC")
                    {
                        Patterns = new[] { "*.icc", "*.icm" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var path = files[0].TryGetLocalPath();
                if (path != null)
                {
                    try
                    {
                        _customProfile = CmykConverter.LoadProfileFromDisk(path);
                        item.Content = $"📄 {Path.GetFileName(path)}";
                    }
                    catch (Exception ex)
                    {
                        this.FindControl<TextBlock>("StatusBlock")!.Text = $"Error cargando perfil: {ex.Message}";
                        combo.SelectedIndex = 0;
                    }
                }
            }
            else
            {
                combo.SelectedIndex = 0;
            }
        }
        else
        {
            _customProfile = null;
        }
    }

    private void OnFormatSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        var isJpg = this.FindControl<ComboBox>("FormatCombo")!.SelectedIndex == 0;
        this.FindControl<StackPanel>("QualityPanel")!.IsVisible = isJpg;
        this.FindControl<TextBlock>("QualityLabel")!.IsVisible = isJpg;
    }

    private void LoadPreview(string path)
    {
        try
        {
            using var magick = new MagickImage(path);
            magick.Thumbnail(500, 140);

            using var stream = new MemoryStream();
            magick.Format = MagickFormat.Png;
            magick.Write(stream);
            stream.Position = 0;

            this.FindControl<Image>("PreviewImg")!.Source = new Bitmap(stream);
        }
        catch
        {
        }
    }

    private CmykConversionOptions BuildOptions()
    {
        var profileCombo = this.FindControl<ComboBox>("ProfileCombo")!;
        var selectedProfileItem = (ComboBoxItem)profileCombo.Items[profileCombo.SelectedIndex]!;

        ColorProfile cmykProfile;
        if (_customProfile != null && profileCombo.SelectedIndex == CmykConverter.EmbeddedProfiles.Count)
        {
            cmykProfile = _customProfile;
        }
        else
        {
            var profileInfo = (CmykProfileInfo)selectedProfileItem.Tag!;
            cmykProfile = profileInfo.Profile;
        }

        return new CmykConversionOptions
        {
            Mode = this.FindControl<ComboBox>("ModeCombo")!.SelectedIndex == 0
                ? CmykMode.FullCmyk : CmykMode.KOnly,
            CmykProfile = cmykProfile,
            OutputFormat = this.FindControl<ComboBox>("FormatCombo")!.SelectedIndex == 0
                ? OutputFormat.Jpg : OutputFormat.Tif,
            RenderingIntent = GetSelectedRenderingIntent(),
            BlackPointCompensation = this.FindControl<CheckBox>("BpcCheck")!.IsChecked ?? true,
            JpgQuality = (int)this.FindControl<Slider>("QualitySlider")!.Value,
            DensityMode = this.FindControl<ComboBox>("DensityCombo")!.SelectedIndex == 0
                ? DensityMode.PreserveOriginal : DensityMode.Ppi300
        };
    }

    private RenderingIntent GetSelectedRenderingIntent()
    {
        return this.FindControl<ComboBox>("IntentCombo")!.SelectedIndex switch
        {
            1 => RenderingIntent.Perceptual,
            2 => RenderingIntent.Saturation,
            3 => RenderingIntent.Absolute,
            _ => RenderingIntent.Relative
        };
    }

    private void OnConvertClick(object? sender, RoutedEventArgs e)
    {
        if (_inputPath == null) return;

        var status = this.FindControl<TextBlock>("StatusBlock")!;
        try
        {
            var options = BuildOptions();
            var modeLabel = options.Mode == CmykMode.KOnly ? "solo K" : "CMYK";
            status.Text = $"Convirtiendo a {modeLabel}…";

            var outputPath = CmykConverter.Convert(_inputPath, options);
            status.Text = $"OK → {Path.GetFileName(outputPath)}";
        }
        catch (Exception ex)
        {
            status.Text = $"Error: {ex.Message}";
        }
    }
}
