using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GDCVault.Client;

/// Numele unei resurse de tema -> pensula ei.
///
/// DE CE prin convertor si nu direct in XAML: culoarea insignei depinde de
/// DATE (cate zile au ramas), iar `DynamicResource` nu accepta o cheie
/// legata prin binding. Asa, randul alege CARE resursa, nu valoarea ei —
/// culorile raman in tema (Regula 37).
public sealed class ResourceKeyToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string key && Application.Current?.TryFindResource(key) is Brush brush
            ? brush
            : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
