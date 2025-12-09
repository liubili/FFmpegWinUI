using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FFmpegWinUI.Models
{
    /// <summary>
    /// Preset data - Contains all encoding parameter configurations
    /// 预设数据类型 - 包含所有编码参数配置
    /// </summary>
    [Serializable]
    public partial class PresetData : ObservableObject
    {
        // Output settings
        [ObservableProperty]
        private string _outputContainer = string.Empty;

        [ObservableProperty]
        private string _outputFileExtension = string.Empty;

        // Output naming
        [ObservableProperty]
        private bool _useAutoNaming = false;

        [ObservableProperty]
        private int _autoNamingOption = 0;

        [ObservableProperty]
        private string _prefixText = string.Empty;

        [ObservableProperty]
        private string _replacementText = string.Empty;

        [ObservableProperty]
        private string _suffixText = string.Empty;

        [ObservableProperty]
        private bool _preserveCreationTime = false;

        [ObservableProperty]
        private bool _preserveModifiedTime = false;

        [ObservableProperty]
        private bool _preserveAccessTime = false;

        [ObservableProperty]
        private bool _不使用输出文件参数 = false;

        // Decoder parameters
        [ObservableProperty]
        private string _decoderName = string.Empty;

        [ObservableProperty]
        private string _decoderThreads = string.Empty;

        [ObservableProperty]
        private string _decoderFormat = string.Empty;

        [ObservableProperty]
        private string _hardwareAccelName = string.Empty;

        [ObservableProperty]
        private string _hardwareAccelParams = string.Empty;

        // Video encoder
        [ObservableProperty]
        private string _videoEncoderCategory = string.Empty;

        [ObservableProperty]
        private string _videoEncoder = string.Empty;

        [ObservableProperty]
        private string _videoEncoderPreset = string.Empty;

        [ObservableProperty]
        private string _videoEncoderProfile = string.Empty;

        [ObservableProperty]
        private string _videoEncoderTune = string.Empty;

        [ObservableProperty]
        private string _videoEncoderGPU = string.Empty;

        [ObservableProperty]
        private string _videoEncoderThreads = string.Empty;

        // Video resolution and framerate
        [ObservableProperty]
        private string _videoResolution = string.Empty;

        [ObservableProperty]
        private string _videoResolutionWidth = string.Empty;

        [ObservableProperty]
        private string _videoResolutionHeight = string.Empty;

        [ObservableProperty]
        private string _videoCropFilter = string.Empty;

        [ObservableProperty]
        private string _videoFrameRate = string.Empty;

        [ObservableProperty]
        private string _videoFrameRateMaxChange = string.Empty;

        // Frame interpolation
        [ObservableProperty]
        private string _interpolationTargetFPS = string.Empty;

        [ObservableProperty]
        private string _interpolationMode = string.Empty;

        [ObservableProperty]
        private string _motionEstimationMode = string.Empty;

        [ObservableProperty]
        private string _motionEstimationAlgorithm = string.Empty;

        [ObservableProperty]
        private string _motionCompensationMode = string.Empty;

        [ObservableProperty]
        private bool _variableBlockSizeMC = false;

        [ObservableProperty]
        private string _blockSize = string.Empty;

        [ObservableProperty]
        private string _searchRange = string.Empty;

        [ObservableProperty]
        private string _sceneChangeThreshold = string.Empty;

        // Frame blending
        [ObservableProperty]
        private string _blendingTargetFPS = string.Empty;

        [ObservableProperty]
        private string _blendingMode = string.Empty;

        [ObservableProperty]
        private string _blendingRatio = string.Empty;

        // Upscaling
        [ObservableProperty]
        private string _upscaleTargetWidth = string.Empty;

        [ObservableProperty]
        private string _upscaleTargetHeight = string.Empty;

        [ObservableProperty]
        private string _upsampleAlgorithm = string.Empty;

        [ObservableProperty]
        private string _downsampleAlgorithm = string.Empty;

        [ObservableProperty]
        private string _antiRingingStrength = string.Empty;

        [ObservableProperty]
        private List<string> _shaderList = new List<string>();

        // Quality control
        [ObservableProperty]
        private string _bitrateControlMode = string.Empty;

        [ObservableProperty]
        private string _qualityControlParamName = string.Empty;

        [ObservableProperty]
        private string _qualityControlValue = string.Empty;

        [ObservableProperty]
        private string _videoBitrate = string.Empty;

        [ObservableProperty]
        private string _videoMinBitrate = string.Empty;

        [ObservableProperty]
        private string _videoMaxBitrate = string.Empty;

        [ObservableProperty]
        private string _videoBufferSize = string.Empty;

        [ObservableProperty]
        private List<string> _advancedQualityParams = new List<string>();

        // Color management
        [ObservableProperty]
        private string _pixelFormat = string.Empty;

        [ObservableProperty]
        private string _colorFilterSelection = string.Empty;

        [ObservableProperty]
        private string _colorMatrix = string.Empty;

        [ObservableProperty]
        private string _colorPrimaries = string.Empty;

        [ObservableProperty]
        private string _colorTransfer = string.Empty;

        [ObservableProperty]
        private string _colorRange = string.Empty;

        [ObservableProperty]
        private string _tonemapAlgorithm = string.Empty;

        [ObservableProperty]
        private string _colorProcessingMode = string.Empty;

        [ObservableProperty]
        private string _brightness = string.Empty;

        [ObservableProperty]
        private string _contrast = string.Empty;

        [ObservableProperty]
        private string _saturation = string.Empty;

        [ObservableProperty]
        private string _gamma = string.Empty;

        // Video filters
        [ObservableProperty]
        private string _videoFilter = string.Empty;

        [ObservableProperty]
        private string _deinterlaceMode = string.Empty;

        [ObservableProperty]
        private string _denoiseFilter = string.Empty;

        [ObservableProperty]
        private string _denoiseParam1 = string.Empty;

        [ObservableProperty]
        private string _denoiseParam2 = string.Empty;

        [ObservableProperty]
        private string _denoiseParam3 = string.Empty;

        [ObservableProperty]
        private string _denoiseParam4 = string.Empty;

        [ObservableProperty]
        private string _sharpenFilter = string.Empty;

        [ObservableProperty]
        private string _sharpenLumaWidth = string.Empty;

        [ObservableProperty]
        private string _sharpenLumaHeight = string.Empty;

        [ObservableProperty]
        private string _sharpenStrength = string.Empty;

        [ObservableProperty]
        private int _rotationAngle = 0;  // 0=无, 1=90度, 2=180度, 3=270度

        [ObservableProperty]
        private int _flipMirrorMode = 0;  // 0=无, 1=水平, 2=垂直

        // Audio encoder
        [ObservableProperty]
        private string _audioEncoderCategory = string.Empty;

        [ObservableProperty]
        private string _audioEncoder = string.Empty;

        [ObservableProperty]
        private string _audioBitrate = string.Empty;

        [ObservableProperty]
        private string _audioQualityParamName = string.Empty;

        [ObservableProperty]
        private string _audioQualityValue = string.Empty;

        [ObservableProperty]
        private string _audioSampleRate = string.Empty;

        [ObservableProperty]
        private string _audioChannels = string.Empty;

        [ObservableProperty]
        private string _audioChannelLayout = string.Empty;

        // Audio filters
        [ObservableProperty]
        private string _audioFilter = string.Empty;

        [ObservableProperty]
        private string _audioVolume = string.Empty;

        [ObservableProperty]
        private string _audioNormalization = string.Empty;

        // Subtitle parameters
        [ObservableProperty]
        private string _subtitleEncoder = string.Empty;

        [ObservableProperty]
        private string _subtitleFilter = string.Empty;

        [ObservableProperty]
        private string _subtitleBurnMode = string.Empty;

        // Clipping parameters
        [ObservableProperty]
        private string _clipStartTime = string.Empty;

        [ObservableProperty]
        private string _clipEndTime = string.Empty;

        [ObservableProperty]
        private string _clipDuration = string.Empty;

        [ObservableProperty]
        private int _clipMethod = 1;  // 1=粗剪, 2=精剪标准, 3=精剪快速响应

        [ObservableProperty]
        private string _preDecodeSeconds = string.Empty;

        // Stream control
        [ObservableProperty]
        private bool _discardVideo = false;

        [ObservableProperty]
        private bool _discardAudio = false;

        [ObservableProperty]
        private bool _discardSubtitle = false;

        [ObservableProperty]
        private bool _discardData = false;

        [ObservableProperty]
        private bool _discardAttachment = false;

        [ObservableProperty]
        private bool _enableKeepOtherVideoStreams = false;

        [ObservableProperty]
        private List<string> _videoStreamParamsList = new List<string>();

        [ObservableProperty]
        private bool _autoMuxSRT = false;

        [ObservableProperty]
        private bool _autoMuxASS = false;

        [ObservableProperty]
        private bool _autoMuxSSA = false;

        [ObservableProperty]
        private bool _convertSubtitleToMovText = false;

        [ObservableProperty]
        private int _元数据选项 = 0;

        [ObservableProperty]
        private int _章节选项 = 0;

        [ObservableProperty]
        private int _附件选项 = 0;

        // Stream selection
        [ObservableProperty]
        private string _videoStreamIndex = string.Empty;

        [ObservableProperty]
        private string _audioStreamIndex = string.Empty;

        [ObservableProperty]
        private string _subtitleStreamIndex = string.Empty;

        // Custom parameters
        [ObservableProperty]
        private string _complexFilter = string.Empty;

        [ObservableProperty]
        private string _additionalParams = string.Empty;

        [ObservableProperty]
        private string _自定义视频参数 = string.Empty;

        [ObservableProperty]
        private string _自定义音频参数 = string.Empty;

        [ObservableProperty]
        private string _自定义开头参数 = string.Empty;

        [ObservableProperty]
        private string _完全自定义命令行 = string.Empty;

        [ObservableProperty]
        private List<string> _preInputParams = new List<string>();

        [ObservableProperty]
        private List<string> _postOutputParams = new List<string>();

        // Other settings
        [ObservableProperty]
        private bool _overwriteOutput = true;

        [ObservableProperty]
        private bool _copyTimestamps = false;

        [ObservableProperty]
        private bool _fastStart = false;

        [ObservableProperty]
        private string _metadataTitle = string.Empty;

        [ObservableProperty]
        private string _metadataAuthor = string.Empty;

        [ObservableProperty]
        private string _metadataComment = string.Empty;
    }
}
