using System;
using FFmpegWinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 超分辨率设置窗口
    /// Upscaling/Super-resolution settings dialog
    /// </summary>
    public sealed partial class UpscaleWindow : ContentDialog
    {
        private PresetData _presetData;

        public UpscaleWindow(PresetData presetData)
        {
            this.InitializeComponent();
            _presetData = presetData;
            LoadSettings();
        }

        /// <summary>
        /// 从预设数据加载设置到UI控件
        /// </summary>
        private void LoadSettings()
        {
            // 目标分辨率
            TargetWidthTextBox.Text = _presetData.UpscaleTargetWidth ?? string.Empty;
            TargetHeightTextBox.Text = _presetData.UpscaleTargetHeight ?? string.Empty;

            // 上采样算法
            if (!string.IsNullOrEmpty(_presetData.UpsampleAlgorithm))
            {
                var upsampleIndex = _presetData.UpsampleAlgorithm.ToLower() switch
                {
                    "lanczos" => 0,
                    "bicubic" => 1,
                    "spline" => 2,
                    "bilinear" => 3,
                    _ => 0
                };
                UpsampleAlgorithmComboBox.SelectedIndex = upsampleIndex;
            }
        }

        /// <summary>
        /// 保存UI控件的设置到预设数据
        /// </summary>
        private void SaveSettings()
        {
            // 目标分辨率
            _presetData.UpscaleTargetWidth = TargetWidthTextBox.Text?.Trim() ?? string.Empty;
            _presetData.UpscaleTargetHeight = TargetHeightTextBox.Text?.Trim() ?? string.Empty;

            // 上采样算法
            var upsampleItem = UpsampleAlgorithmComboBox.SelectedItem as ComboBoxItem;
            _presetData.UpsampleAlgorithm = upsampleItem?.Tag?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SaveSettings();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 取消操作，不保存设置
        }
    }
}
