using Godot;

namespace GenerationRoguelite.UI;

public partial class StatPanelController : PanelContainer
{
    [Export]
    public NodePath BodyPath { get; set; } = "Margin/VBox/BodyLabel";

    private Label _bodyLabel = null!;
    private float _autoCloseTimer;

    public override void _Ready()
    {
        _bodyLabel = GetNode<Label>(BodyPath);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        _autoCloseTimer -= (float)delta;
        if (_autoCloseTimer <= 0f)
        {
            Visible = false;
        }
    }

    public void ShowStats(string body, float autoCloseSeconds = 3f)
    {
        _bodyLabel.Text = body;
        _autoCloseTimer = autoCloseSeconds;
        Visible = true;
    }
}
