using Godot;

namespace GenerationRoguelite.UI;

public partial class TestamentController : PanelContainer
{
    [Signal]
    public delegate void ConfirmPressedEventHandler();

    [Signal]
    public delegate void ClosePressedEventHandler();

    [Export]
    public NodePath SummaryLabelPath { get; set; } = "Margin/VBox/SummaryLabel";

    [Export]
    public NodePath ConfirmButtonPath { get; set; } = "Margin/VBox/ConfirmButton";

    [Export]
    public NodePath CloseButtonPath { get; set; } = "Margin/VBox/CloseButton";

    private Label _summaryLabel = null!;
    private Button _confirmButton = null!;
    private Button _closeButton = null!;

    public override void _Ready()
    {
        _summaryLabel = GetNode<Label>(SummaryLabelPath);
        _confirmButton = GetNode<Button>(ConfirmButtonPath);
        _closeButton = GetNode<Button>(CloseButtonPath);
        _confirmButton.Pressed += OnConfirmPressed;
        _closeButton.Pressed += OnClosePressed;
        Visible = false;
    }

    public override void _ExitTree()
    {
        _confirmButton.Pressed -= OnConfirmPressed;
        _closeButton.Pressed -= OnClosePressed;
    }

    public void ShowPanel(string summary)
    {
        _summaryLabel.Text = summary;
        Visible = true;
    }

    public void HidePanel()
    {
        Visible = false;
    }

    private void OnConfirmPressed()
    {
        EmitSignal(SignalName.ConfirmPressed);
    }

    private void OnClosePressed()
    {
        EmitSignal(SignalName.ClosePressed);
    }
}
