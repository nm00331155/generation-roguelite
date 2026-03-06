using Godot;

namespace GenerationRoguelite.UI;

public partial class FamilyTreeController : PanelContainer
{
    [Signal]
    public delegate void ClosePressedEventHandler();

    [Export]
    public NodePath TreeLabelPath { get; set; } = "Margin/VBox/TreeLabel";

    [Export]
    public NodePath CloseButtonPath { get; set; } = "Margin/VBox/CloseButton";

    private Label _treeLabel = null!;
    private Button _closeButton = null!;

    public override void _Ready()
    {
        _treeLabel = GetNode<Label>(TreeLabelPath);
        _closeButton = GetNode<Button>(CloseButtonPath);
        _closeButton.Pressed += OnClosePressed;
        Visible = false;
    }

    public override void _ExitTree()
    {
        _closeButton.Pressed -= OnClosePressed;
    }

    public void ShowTree(string treeText)
    {
        _treeLabel.Text = treeText;
        Visible = true;
    }

    public void HideTree()
    {
        Visible = false;
    }

    private void OnClosePressed()
    {
        EmitSignal(SignalName.ClosePressed);
    }
}
