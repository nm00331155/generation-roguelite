using Godot;

namespace GenerationRoguelite.Core;

public partial class DebugOverlay : Control
{
    private Label _infoLabel = null!;
    private Vector2 _lastTouchPosition;
    private bool _hasTouchPosition;

    public override void _Ready()
    {
        _infoLabel = GetNode<Label>("Panel/Margin/InfoLabel");
    }

    public void SetOverlayEnabled(bool enabled)
    {
        Visible = enabled;
    }

    public void SetLastTouch(Vector2 position)
    {
        _lastTouchPosition = position;
        _hasTouchPosition = true;
    }

    public void UpdateMetrics(
        float fps,
        Vector2 viewportSize,
        string gameState,
        int age = -1,
        string phase = "-",
        string era = "-")
    {
        var touchText = _hasTouchPosition
            ? $"({_lastTouchPosition.X:0},{_lastTouchPosition.Y:0})"
            : "-";

        var ageText = age >= 0 ? $"{age}" : "-";

        _infoLabel.Text =
            $"FPS: {fps:0}\n"
            + $"RES: {(int)viewportSize.X}x{(int)viewportSize.Y}\n"
            + $"TOUCH: {touchText}\n"
            + $"STATE: {gameState}\n"
            + $"AGE: {ageText}\n"
            + $"PHASE: {phase}\n"
            + $"ERA: {era}";
    }
}
