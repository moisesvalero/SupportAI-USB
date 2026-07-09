using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SupportAI.Core.Models;

namespace SupportAI.App.Wpf.Converters;

public class GravedadToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Gravedad g
            ? g switch
            {
                Gravedad.Critico or Gravedad.Alto => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Gravedad.Medio => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                Gravedad.Bajo => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                _ => new SolidColorBrush(Colors.Gray)
            }
            : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class BoolToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool ok)
            return ok ? "✓" : "⚠";
        return "•";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class BoolToCheckColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool ok)
            return ok
                ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                : new SolidColorBrush(Color.FromRgb(231, 76, 60));
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase);
        var visible = value is bool b && b;
        if (invert) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ScoreToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int score
            ? score switch
            {
                >= 80 => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                >= 50 => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                _ => new SolidColorBrush(Color.FromRgb(231, 76, 60))
            }
            : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
