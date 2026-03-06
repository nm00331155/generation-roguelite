using Godot;

namespace GenerationRoguelite.UI;

public partial class InventoryPanelController : PanelContainer
{
    [Signal]
    public delegate void CloseRequestedEventHandler();

    [Export]
    public NodePath SummaryLabelPath { get; set; } = "Margin/VBox/SummaryLabel";

    [Export]
    public NodePath CloseButtonPath { get; set; } = "Margin/VBox/CloseButton";

    private Label _summaryLabel = null!;
    private Button _closeButton = null!;

    public override void _Ready()
    {
        _summaryLabel = GetNode<Label>(SummaryLabelPath);
        _closeButton = GetNode<Button>(CloseButtonPath);
        _closeButton.Pressed += OnCloseButtonPressed;
        Visible = false;
    }

    public override void _ExitTree()
    {
        _closeButton.Pressed -= OnCloseButtonPressed;
    }

    public void ShowInventory(string summary)
    {
        _summaryLabel.Text = summary;
        Visible = true;
    }

    public void HideInventory()
    {
        Visible = false;
    }

    private void OnCloseButtonPressed()
    {
        EmitSignal(SignalName.CloseRequested);
    }
}
