using Godot;

namespace GenerationRoguelite.UI;

public partial class GameOverScreenController : PanelContainer
{
    [Signal]
    public delegate void RetryPressedEventHandler();

    [Export]
    public NodePath ReasonLabelPath { get; set; } = "Margin/VBox/ReasonLabel";

    [Export]
    public NodePath RetryButtonPath { get; set; } = "Margin/VBox/RetryButton";

    private Label _reasonLabel = null!;
    private Button _retryButton = null!;

    public override void _Ready()
    {
        _reasonLabel = GetNode<Label>(ReasonLabelPath);
        _retryButton = GetNode<Button>(RetryButtonPath);
        _retryButton.Pressed += HandleRetryPressed;
        Visible = false;
    }

    public override void _ExitTree()
    {
        _retryButton.Pressed -= HandleRetryPressed;
    }

    public void ShowGameOver(string reason)
    {
        _reasonLabel.Text = reason;
        Visible = true;
    }

    public void HideScreen()
    {
        Visible = false;
    }

    private void HandleRetryPressed()
    {
        EmitSignal(SignalName.RetryPressed);
    }
}
