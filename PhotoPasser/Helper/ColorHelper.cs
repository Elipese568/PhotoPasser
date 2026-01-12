using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoPasser.Helper;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace PhotoPasser.Helper;

public static class ColorHelper
{
    private static Dictionary<string, (int R, int G, int B)> _maps = new();

    public static async Task<Windows.UI.Color> GetAverageColor(string imagePath, int alpha, int blockSize = 64)
    {
        if(_maps.TryGetValue(imagePath, out var cachedColor))
        {
            (int R, int G, int B) = cachedColor;
            return Windows.UI.Color.FromArgb((byte)alpha, (byte)R, (byte)G, (byte)B);
        }
        // 加载图像
        var stream = await (await StorageItemProvider.GetStorageFile(imagePath)).OpenAsync(FileAccessMode.Read);
        var decoder = await BitmapDecoder.CreateAsync(stream);

        // 获取图像的宽高
        var pixelWidth = decoder.PixelWidth;
        var pixelHeight = decoder.PixelHeight;

        // 获取像素数据
        var pixelData = await decoder.GetPixelDataAsync();
        var pixels = pixelData.DetachPixelData();

        long totalR = 0, totalG = 0, totalB = 0;
        int pixelCount = pixels.Length / 4; // 每个像素有 4 字节数据（A, R, G, B）

        // 计算块的数量，水平和垂直方向
        int blocksPerRow = (int)Math.Ceiling((double)pixelWidth / blockSize);
        int blocksPerColumn = (int)Math.Ceiling((double)pixelHeight / blockSize);

        // 使用 Parallel.For 来并行处理每个图像块
        Parallel.For(0, blocksPerColumn, (blockY) =>
        {
            for (int blockX = 0; blockX < blocksPerRow; blockX++)
            {
                // 计算当前块的起始位置和结束位置
                int startX = blockX * blockSize;
                int startY = blockY * blockSize;

                // 限制块的大小不超出图像边界
                int endX = (int)Math.Min(startX + blockSize, pixelWidth);
                int endY = (int)Math.Min(startY + blockSize, pixelHeight);

                long blockR = 0, blockG = 0, blockB = 0;

                // 计算当前块内的像素颜色
                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        int index = (int)(y * pixelWidth + x) * 4; // 每个像素 4 字节数据（A, R, G, B）
                        byte b = pixels[index];        // Blue
                        byte g = pixels[index + 1];    // Green
                        byte r = pixels[index + 2];    // Red

                        blockB += b;
                        blockG += g;
                        blockR += r;
                    }
                }

                // 更新总计颜色，确保线程安全
                Interlocked.Add(ref totalB, blockB);
                Interlocked.Add(ref totalG, blockG);
                Interlocked.Add(ref totalR, blockR);
            }
        });

        // 计算平均色
        byte avgR = (byte)(totalR / pixelCount);
        byte avgG = (byte)(totalG / pixelCount);
        byte avgB = (byte)(totalB / pixelCount);

        // 返回平均色
        _maps[imagePath] = (avgR, avgG, avgB);
        return Windows.UI.Color.FromArgb((byte)alpha, avgR, avgG, avgB);
    }

    public static (double H, double S, double L) RGBToHSL(Windows.UI.Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double h = 0, s = 0, l = (max + min) / 2;

        if (max != min)
        {
            double delta = max - min;
            s = (l > 0.5) ? delta / (2 - max - min) : delta / (max + min);

            if (max == r)
                h = (g - b) / delta + (g < b ? 6 : 0);
            else if (max == g)
                h = (b - r) / delta + 2;
            else if (max == b)
                h = (r - g) / delta + 4;

            h /= 6;
        }

        return (h * 360, s, l);
    }
    public static Windows.UI.Color AdjustToBackground(Windows.UI.Color color)
    {
        // 将 RGB 转换为 HSL
        var (h, s, l) = RGBToHSL(color);

        // 获取当前应用的主题
        var isDarkTheme = (App.Current.MainWindow.Content as Grid).RequestedTheme switch
        {
            ElementTheme.Dark => true,
            ElementTheme.Light => false,
            _ => SystemThemeHelper.IsAppDarkMode(),
        };

        if (isDarkTheme)
        {
            // 映射到深色主题的亮度和饱和度
            l = MapValue(l, 0.0, 1.0, 0.0, 0.74);  // 亮度从 [0, 1] 映射到 [0, 0.74]
            s = MapValue(s, 0.0, 1.0, 0.2, 0.6);   // 饱和度从 [0, 1] 映射到 [0.2, 0.9]
        }
        else
        {
            // 映射到浅色主题的亮度和饱和度
            l = MapValue(l, 0.0, 1.0, 0.5, 0.9);  // 亮度从 [0, 1] 映射到 [0.5, 0.9]
            s = MapValue(s, 0.0, 1.0, 0.2, 0.9);  // 饱和度从 [0, 1] 映射到 [0.2, 0.6]
        }

        // 保持色相不变，或者可以根据需求微调
        h = h % 360;

        // 将 HSL 转换回 RGB
        return HSLToRGB(color.A, h, s, l);
    }

    // 映射函数：将原始值从一个范围映射到另一个范围
    public static double MapValue(double value, double fromMin, double fromMax, double toMin, double toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

    public static Windows.UI.Color HSLToRGB(byte a, double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;

        double r = 0, g = 0, b = 0;

        if (h >= 0 && h < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (h >= 60 && h < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (h >= 120 && h < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (h >= 180 && h < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (h >= 240 && h < 300)
        {
            r = x; g = 0; b = c;
        }
        else if (h >= 300 && h < 360)
        {
            r = c; g = 0; b = x;
        }

        r += m; g += m; b += m;

        return Windows.UI.Color.FromArgb(a, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

}
