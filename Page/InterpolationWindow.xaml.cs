using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FFmpegWinUI.Models;

namespace FFmpegWinUI.Page
{
    /// <summary>
    /// 帧插值设置对话框
    /// </summary>
    public sealed partial class InterpolationWindow : ContentDialog
    {
        private PresetData _presetData;

        public InterpolationWindow(PresetData presetData)
        {
            this.InitializeComponent();
            _presetData = presetData;

            // 加载现有设置
            LoadSettings();
        }

        /// <summary>
        /// 加载现有设置
        /// </summary>
        private void LoadSettings()
        {
            if (!string.IsNullOrEmpty(_presetData.InterpolationTargetFPS))
            {
                TargetFPSComboBox.Text = _presetData.InterpolationTargetFPS;
            }

            if (!string.IsNullOrEmpty(_presetData.InterpolationMode))
            {
                var mode = _presetData.InterpolationMode.ToLower();
                InterpolationModeComboBox.SelectedIndex = mode switch
                {
                    "mci" => 0,
                    "mcb" => 1,
                    "blend" => 2,
                    "dup" => 3,
                    _ => 0
                };
            }

            if (!string.IsNullOrEmpty(_presetData.MotionEstimationMode))
            {
                MotionEstimationModeComboBox.SelectedIndex = _presetData.MotionEstimationMode.ToLower() == "bidir" ? 0 : 1;
            }

            if (!string.IsNullOrEmpty(_presetData.MotionEstimationAlgorithm))
            {
                var algo = _presetData.MotionEstimationAlgorithm.ToLower();
                MotionEstimationAlgorithmComboBox.SelectedIndex = algo switch
                {
                    "epzs" => 0,
                    "esa" => 1,
                    "fss" => 2,
                    "ntss" => 3,
                    "tdls" => 4,
                    "tss" => 5,
                    "umh" => 6,
                    _ => 0
                };
            }

            if (!string.IsNullOrEmpty(_presetData.MotionCompensationMode))
            {
                MotionCompensationModeComboBox.SelectedIndex = _presetData.MotionCompensationMode.ToLower() == "obmc" ? 0 : 1;
            }

            VariableBlockSizeCheckBox.IsChecked = _presetData.VariableBlockSizeMC;

            if (!string.IsNullOrEmpty(_presetData.BlockSize))
            {
                BlockSizeComboBox.SelectedIndex = _presetData.BlockSize switch
                {
                    "4" => 0,
                    "8" => 1,
                    "16" => 2,
                    "32" => 3,
                    _ => 2
                };
            }

            if (!string.IsNullOrEmpty(_presetData.SearchRange) && int.TryParse(_presetData.SearchRange, out int searchRange))
            {
                SearchRangeSlider.Value = searchRange;
            }

            if (!string.IsNullOrEmpty(_presetData.SceneChangeThreshold) && double.TryParse(_presetData.SceneChangeThreshold, out double threshold))
            {
                SceneChangeSlider.Value = threshold;
            }
        }

        /// <summary>
        /// 保存设置到 PresetData
        /// </summary>
        private void SaveSettings()
        {
            _presetData.InterpolationTargetFPS = TargetFPSComboBox.Text;

            _presetData.InterpolationMode = InterpolationModeComboBox.SelectedIndex switch
            {
                0 => "mci",
                1 => "mcb",
                2 => "blend",
                3 => "dup",
                _ => "mci"
            };

            _presetData.MotionEstimationMode = MotionEstimationModeComboBox.SelectedIndex == 0 ? "bidir" : "bilat";

            _presetData.MotionEstimationAlgorithm = MotionEstimationAlgorithmComboBox.SelectedIndex switch
            {
                0 => "epzs",
                1 => "esa",
                2 => "fss",
                3 => "ntss",
                4 => "tdls",
                5 => "tss",
                6 => "umh",
                _ => "epzs"
            };

            _presetData.MotionCompensationMode = MotionCompensationModeComboBox.SelectedIndex == 0 ? "obmc" : "aobmc";

            _presetData.VariableBlockSizeMC = VariableBlockSizeCheckBox.IsChecked ?? false;

            _presetData.BlockSize = BlockSizeComboBox.SelectedIndex switch
            {
                0 => "4",
                1 => "8",
                2 => "16",
                3 => "32",
                _ => "16"
            };

            _presetData.SearchRange = ((int)SearchRangeSlider.Value).ToString();
            _presetData.SceneChangeThreshold = SceneChangeSlider.Value.ToString("F1");
        }

        /// <summary>
        /// 应用平衡预设
        /// </summary>
        private void ApplyBalancedPreset(object sender, RoutedEventArgs e)
        {
            TargetFPSComboBox.Text = "60";
            InterpolationModeComboBox.SelectedIndex = 0; // mci
            MotionEstimationModeComboBox.SelectedIndex = 0; // bidir
            MotionEstimationAlgorithmComboBox.SelectedIndex = 0; // epzs
            MotionCompensationModeComboBox.SelectedIndex = 0; // obmc
            VariableBlockSizeCheckBox.IsChecked = false;
            BlockSizeComboBox.SelectedIndex = 2; // 16
            SearchRangeSlider.Value = 16;
            SceneChangeSlider.Value = 10;
        }

        /// <summary>
        /// 应用高质量预设
        /// </summary>
        private void ApplyHighQualityPreset(object sender, RoutedEventArgs e)
        {
            TargetFPSComboBox.Text = "60";
            InterpolationModeComboBox.SelectedIndex = 0; // mci
            MotionEstimationModeComboBox.SelectedIndex = 0; // bidir
            MotionEstimationAlgorithmComboBox.SelectedIndex = 1; // esa
            MotionCompensationModeComboBox.SelectedIndex = 0; // obmc
            VariableBlockSizeCheckBox.IsChecked = true;
            BlockSizeComboBox.SelectedIndex = 1; // 8
            SearchRangeSlider.Value = 32;
            SceneChangeSlider.Value = 12;
        }

        /// <summary>
        /// 应用快速预设
        /// </summary>
        private void ApplyFastPreset(object sender, RoutedEventArgs e)
        {
            TargetFPSComboBox.Text = "60";
            InterpolationModeComboBox.SelectedIndex = 2; // blend
            MotionEstimationModeComboBox.SelectedIndex = 1; // bilat
            MotionEstimationAlgorithmComboBox.SelectedIndex = 2; // fss
            MotionCompensationModeComboBox.SelectedIndex = 1; // aobmc
            VariableBlockSizeCheckBox.IsChecked = false;
            BlockSizeComboBox.SelectedIndex = 3; // 32
            SearchRangeSlider.Value = 8;
            SceneChangeSlider.Value = 15;
        }

        /// <summary>
        /// 搜索范围滑块值变化事件
        /// </summary>
        private void SearchRangeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (SearchRangeValueText != null)
            {
                SearchRangeValueText.Text = ((int)e.NewValue).ToString();
            }
        }

        /// <summary>
        /// 场景切换阈值滑块值变化事件
        /// </summary>
        private void SceneChangeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (SceneChangeValueText != null)
            {
                SceneChangeValueText.Text = ((int)e.NewValue).ToString();
            }
        }

        /// <summary>
        /// 确定按钮点击
        /// </summary>
        private void OkButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SaveSettings();
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void CancelButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 取消时不保存设置
        }
    }
}
