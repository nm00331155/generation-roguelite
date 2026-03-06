using GenerationRoguelite.Events;
using Godot;

namespace GenerationRoguelite.UI;

public partial class EventPanelController : PanelContainer
{
    [Export]
    public NodePath BodyLabelPath { get; set; } = "Margin/VBox/EventBodyLabel";

    [Export]
    public NodePath TapButtonPath { get; set; } = "Margin/VBox/ChoiceTapButton";

    [Export]
    public NodePath SwipeButtonPath { get; set; } = "Margin/VBox/ChoiceSwipeButton";

    [Export]
    public NodePath TimeoutButtonPath { get; set; } = "Margin/VBox/ChoiceTimeoutButton";

    [Export]
    public NodePath TimeBarPath { get; set; } = "Margin/VBox/TimeBar";

    private Label _bodyLabel = null!;
    private Button _tapButton = null!;
    private Button _swipeButton = null!;
    private Button _timeoutButton = null!;
    private TextureProgressBar _timeBar = null!;

    public override void _Ready()
    {
        _bodyLabel = GetNode<Label>(BodyLabelPath);
        _tapButton = GetNode<Button>(TapButtonPath);
        _swipeButton = GetNode<Button>(SwipeButtonPath);
        _timeoutButton = GetNode<Button>(TimeoutButtonPath);
        _timeBar = GetNode<TextureProgressBar>(TimeBarPath);
    }

    public void ShowEvent(EventData eventData)
    {
        Visible = true;

        _bodyLabel.Text = eventData.EventText;
        _tapButton.Text = $"A: {eventData.TapChoice.Text}";
        _swipeButton.Text = $"B: {eventData.SwipeChoice.Text}";
        _timeoutButton.Text = $"C: {eventData.TimeoutChoice.Text}";

        _timeBar.MaxValue = eventData.RemainingSeconds;
        _timeBar.Value = eventData.RemainingSeconds;
    }

    public void UpdateTimer(float remainingSeconds)
    {
        _timeBar.Value = remainingSeconds;
    }

    public void HideEvent()
    {
        Visible = false;
    }
}
