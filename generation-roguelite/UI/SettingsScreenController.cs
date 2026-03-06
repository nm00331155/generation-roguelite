using Godot;

namespace GenerationRoguelite.UI;

public partial class SettingsScreenController : Control
{
    [Signal]
    public delegate void ClosedEventHandler();

    [Signal]
    public delegate void BgmVolumeChangedEventHandler(float value);

    [Signal]
    public delegate void SeVolumeChangedEventHandler(float value);

    [Signal]
    public delegate void VoiceVolumeChangedEventHandler(float value);

    [Signal]
    public delegate void GameSpeedChangedEventHandler(float speed);

    [Signal]
    public delegate void DebugOverlayToggledEventHandler(bool enabled);

    [Signal]
    public delegate void RemoveAdsPressedEventHandler();

    [Signal]
    public delegate void DeleteDataPressedEventHandler();

    [Signal]
    public delegate void CreditsPressedEventHandler();

    [Export]
    public NodePath BgmSliderPath { get; set; } = "VBox/BgmRow/BgmSlider";

    [Export]
    public NodePath SeSliderPath { get; set; } = "VBox/SeRow/SeSlider";

    [Export]
    public NodePath VoiceSliderPath { get; set; } = "VBox/VoiceRow/VoiceSlider";

    [Export]
    public NodePath SpeedLabelPath { get; set; } = "VBox/SpeedLabel";

    [Export]
    public NodePath SpeedX1Path { get; set; } = "VBox/SpeedButtons/SpeedX1";

    [Export]
    public NodePath SpeedX15Path { get; set; } = "VBox/SpeedButtons/SpeedX15";

    [Export]
    public NodePath SpeedX2Path { get; set; } = "VBox/SpeedButtons/SpeedX2";

    [Export]
    public NodePath DebugTogglePath { get; set; } = "VBox/DebugRow/DebugToggle";

    [Export]
    public NodePath RemoveAdsButtonPath { get; set; } = "VBox/RemoveAdsButton";

    [Export]
    public NodePath DeleteDataButtonPath { get; set; } = "VBox/DeleteDataButton";

    [Export]
    public NodePath CreditsButtonPath { get; set; } = "VBox/CreditsButton";

    [Export]
    public NodePath CloseButtonPath { get; set; } = "VBox/CloseButton";

    private HSlider _bgmSlider = null!;
    private HSlider _seSlider = null!;
    private HSlider _voiceSlider = null!;
    private Label _speedLabel = null!;
    private Button _speedX1Button = null!;
    private Button _speedX15Button = null!;
    private Button _speedX2Button = null!;
    private CheckButton _debugToggle = null!;
    private Button _removeAdsButton = null!;
    private Button _deleteDataButton = null!;
    private Button _creditsButton = null!;
    private Button _closeButton = null!;

    public float TimeScaleOption { get; private set; } = 1f;

    public override void _Ready()
    {
        _bgmSlider = GetNode<HSlider>(BgmSliderPath);
        _seSlider = GetNode<HSlider>(SeSliderPath);
        _voiceSlider = GetNode<HSlider>(VoiceSliderPath);
        _speedLabel = GetNode<Label>(SpeedLabelPath);
        _speedX1Button = GetNode<Button>(SpeedX1Path);
        _speedX15Button = GetNode<Button>(SpeedX15Path);
        _speedX2Button = GetNode<Button>(SpeedX2Path);
        _debugToggle = GetNode<CheckButton>(DebugTogglePath);
        _removeAdsButton = GetNode<Button>(RemoveAdsButtonPath);
        _deleteDataButton = GetNode<Button>(DeleteDataButtonPath);
        _creditsButton = GetNode<Button>(CreditsButtonPath);
        _closeButton = GetNode<Button>(CloseButtonPath);

        _bgmSlider.ValueChanged += OnBgmChanged;
        _seSlider.ValueChanged += OnSeChanged;
        _voiceSlider.ValueChanged += OnVoiceChanged;
        _speedX1Button.Pressed += OnSpeedX1Pressed;
        _speedX15Button.Pressed += OnSpeedX15Pressed;
        _speedX2Button.Pressed += OnSpeedX2Pressed;
        _debugToggle.Toggled += OnDebugToggled;
        _removeAdsButton.Pressed += OnRemoveAdsPressed;
        _deleteDataButton.Pressed += OnDeleteDataPressed;
        _creditsButton.Pressed += OnCreditsPressed;
        _closeButton.Pressed += OnCloseButtonPressed;

        UpdateSpeedLabel();
    }

    public override void _ExitTree()
    {
        _bgmSlider.ValueChanged -= OnBgmChanged;
        _seSlider.ValueChanged -= OnSeChanged;
        _voiceSlider.ValueChanged -= OnVoiceChanged;
        _speedX1Button.Pressed -= OnSpeedX1Pressed;
        _speedX15Button.Pressed -= OnSpeedX15Pressed;
        _speedX2Button.Pressed -= OnSpeedX2Pressed;
        _debugToggle.Toggled -= OnDebugToggled;
        _removeAdsButton.Pressed -= OnRemoveAdsPressed;
        _deleteDataButton.Pressed -= OnDeleteDataPressed;
        _creditsButton.Pressed -= OnCreditsPressed;
        _closeButton.Pressed -= OnCloseButtonPressed;
    }

    public void ApplyValues(float bgm, float se, float voice, float speed, bool debugOverlayEnabled = true)
    {
        _bgmSlider.Value = Mathf.Clamp(bgm, 0f, 100f);
        _seSlider.Value = Mathf.Clamp(se, 0f, 100f);
        _voiceSlider.Value = Mathf.Clamp(voice, 0f, 100f);
        SetTimeScale(speed);
        SetDebugOverlayEnabled(debugOverlayEnabled);
    }

    public void SetTimeScale(float scale)
    {
        TimeScaleOption = Mathf.Clamp(scale, 1f, 2f);
        UpdateSpeedLabel();
    }

    public void SetDebugOverlayEnabled(bool enabled)
    {
        _debugToggle.SetPressedNoSignal(enabled);
        _debugToggle.Text = enabled ? "ON" : "OFF";
    }

    private void UpdateSpeedLabel()
    {
        _speedLabel.Text = $"ゲーム速度: x{TimeScaleOption:0.0}";
    }

    private void SetSpeedAndEmit(float speed)
    {
        SetTimeScale(speed);
        EmitSignal(SignalName.GameSpeedChanged, TimeScaleOption);
    }

    private void OnBgmChanged(double value)
    {
        EmitSignal(SignalName.BgmVolumeChanged, (float)value);
    }

    private void OnSeChanged(double value)
    {
        EmitSignal(SignalName.SeVolumeChanged, (float)value);
    }

    private void OnVoiceChanged(double value)
    {
        EmitSignal(SignalName.VoiceVolumeChanged, (float)value);
    }

    private void OnSpeedX1Pressed()
    {
        SetSpeedAndEmit(1f);
    }

    private void OnSpeedX15Pressed()
    {
        SetSpeedAndEmit(1.5f);
    }

    private void OnSpeedX2Pressed()
    {
        SetSpeedAndEmit(2f);
    }

    private void OnDebugToggled(bool enabled)
    {
        _debugToggle.Text = enabled ? "ON" : "OFF";
        EmitSignal(SignalName.DebugOverlayToggled, enabled);
    }

    private void OnRemoveAdsPressed()
    {
        EmitSignal(SignalName.RemoveAdsPressed);
    }

    private void OnDeleteDataPressed()
    {
        EmitSignal(SignalName.DeleteDataPressed);
    }

    private void OnCreditsPressed()
    {
        EmitSignal(SignalName.CreditsPressed);
    }

    private void OnCloseButtonPressed()
    {
        EmitSignal(SignalName.Closed);
    }
}
