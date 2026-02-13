using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace syncFolder.Views;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool invert = parameter is string s && s == "Invert";
        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s == "Invert";
        bool isNull = value == null;
        if (invert) isNull = !isNull;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RelativeDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime date) return string.Empty;

        var diff = DateTime.Now - date;

        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 2) return "1 min ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 2) return "1 hr ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
        if (diff.TotalDays < 2) return "yesterday";
        return $"{(int)diff.TotalDays} days ago";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Syncing" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),   // Blue
                "Error" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),       // Red
                "Warning" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),    // Orange
                "Success" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),     // Green
                "Disabled" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),  // Gray
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94))             // Green default
            };
        }
        return new SolidColorBrush(System.Windows.Media.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SyncModeToArrowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.SyncMode mode)
        {
            return mode == Models.SyncMode.Bidirectional ? "\u2194" : "\u2192";
        }
        return "\u2192";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PathAbbreviator : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                return "~" + path.Substring(userProfile.Length);
            }
            return path;
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
