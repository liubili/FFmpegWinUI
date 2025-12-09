using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using FFmpegWinUI.Models;

namespace FFmpegWinUI.Converters
{
    /// <summary>
    /// 将 TimeSpan 转换为可读字符串格式
    /// </summary>
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan timeSpan)
            {
                if (timeSpan.TotalSeconds == 0)
                    return "--:--:--";

                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            return "--:--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将百分比双精度值转换为字符串格式
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double percentage)
            {
                return $"{percentage:F1}%";
            }
            return "0.0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将非空字符串转换为 Visibility.Visible，空字符串转换为 Visibility.Collapsed
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将编码状态转换为错误布尔值（用于 ProgressBar.ShowError）
    /// </summary>
    public class StatusToErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is EncodingStatus status)
            {
                return status == EncodingStatus.Error;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将编码状态转换为暂停布尔值（用于 ProgressBar.ShowPaused）
    /// </summary>
    public class StatusToPausedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is EncodingStatus status)
            {
                return status == EncodingStatus.Paused;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将整数减1（用于剪辑方法的ComboBox索引转换）
    /// 0-based ComboBox index <-> 1-based enum value
    /// </summary>
    public class IntMinusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue && intValue > 0)
                return intValue - 1;  // 1,2,3 -> 0,1,2
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue)
                return intValue + 1;  // 0,1,2 -> 1,2,3
            return 1;
        }
    }

    /// <summary>
    /// 当剪辑方法=3（精剪快速响应）时显示，否则隐藏
    /// </summary>
    public class ClipMethod3VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int method && method == 3)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将 List<string> 转换为逗号分隔的字符串
    /// </summary>
    public class StringListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<string> list && list.Count > 0)
                return string.Join(", ", list);
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
                return str.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            return new List<string>();
        }
    }
}
