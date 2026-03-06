using Godot;

namespace GenerationRoguelite.UI;

public partial class GenerationScreenController : PanelContainer
{
    [Signal]
    public delegate void ConfirmPressedEventHandler();

    [Export]
    public NodePath SummaryLabelPath { get; set; } = "Margin/VBox/SummaryLabel";

    [Export]
    public NodePath ConfirmButtonPath { get; set; } = "Margin/VBox/ConfirmButton";

    private Label _summaryLabel = null!;
    private Button _confirmButton = null!;

    public override void _Ready()
    {
        _summaryLabel = GetNode<Label>(SummaryLabelPath);
        _confirmButton = GetNode<Button>(ConfirmButtonPath);
        _confirmButton.Pressed += HandleConfirmPressed;
        Visible = false;
    }

    public override void _ExitTree()
    {
        _confirmButton.Pressed -= HandleConfirmPressed;
    }

    public void ShowSummary(string summary)
    {
        _summaryLabel.Text = summary;
        Visible = true;
    }

    public void HideScreen()
    {
        Visible = false;
    }

    private void HandleConfirmPressed()
    {
        EmitSignal(SignalName.ConfirmPressed);
    }
}
