using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Controls;

public partial class AdaptiveWrapPanel : Panel
{
    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(AdaptiveWrapPanel),
            new PropertyMetadata(8.0, LayoutPropertyChanged));

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(AdaptiveWrapPanel),
            new PropertyMetadata(8.0, LayoutPropertyChanged));
    private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveWrapPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        double x = 0;
        double y = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double maxWidth = availableSize.Width;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desired = child.DesiredSize;

            // 换行判断
            if (x + desired.Width > maxWidth && x > 0)
            {
                y += lineHeight + VerticalSpacing;
                totalHeight += lineHeight + VerticalSpacing;
                x = 0;
                lineHeight = 0;
            }

            lineHeight = Math.Max(lineHeight, desired.Height);
            x += desired.Width + HorizontalSpacing;
        }

        totalHeight += lineHeight;
        return new Size(maxWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double maxWidth = finalSize.Width;
        double x = 0;
        double y = 0;
        double lineHeight = 0;

        var currentLine = new List<UIElement>();
        var lines = new List<(List<UIElement> children, double height)>();

        // --- 先按行分组 ---
        foreach (var child in Children)
        {
            var desired = child.DesiredSize;

            if (x + desired.Width > maxWidth && x > 0)
            {
                lines.Add((new List<UIElement>(currentLine), lineHeight));
                y += lineHeight + VerticalSpacing;

                currentLine.Clear();
                x = 0;
                lineHeight = 0;
            }

            currentLine.Add(child);
            x += desired.Width + HorizontalSpacing;
            lineHeight = Math.Max(lineHeight, desired.Height);
        }

        if (currentLine.Count > 0)
        {
            lines.Add((new List<UIElement>(currentLine), lineHeight));
        }

        // --- 再逐行排列 ---
        y = 0;
        foreach (var line in lines)
        {
            x = 0;
            foreach (var child in line.children)
            {
                var desired = child.DesiredSize;
                child.Arrange(new Rect(new Point(x, y), new Size(desired.Width, line.height)));
                x += desired.Width + HorizontalSpacing;
            }
            y += line.height + VerticalSpacing;
        }

        return finalSize;
    }
}
