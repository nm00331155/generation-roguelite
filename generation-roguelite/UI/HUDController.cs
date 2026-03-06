using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.UI;

public partial class HUDController : Control
{
    private Label _nameAgeLabel = null!;
    private Label _phaseLabel = null!;
    private Label _generationLabel = null!;
    private Label _scoreLabel = null!;

    private Control _collapsedDots = null!;
    private PanelContainer _expandedStatPanel = null!;

    private Label _vitalityLabel = null!;
    private Label _intelligenceLabel = null!;
    private Label _charismaLabel = null!;
    private Label _luckLabel = null!;
    private Label _wealthLabel = null!;

    private ProgressBar _vitalityBar = null!;
    private ProgressBar _intelligenceBar = null!;
    private ProgressBar _charismaBar = null!;
    private ProgressBar _luckBar = null!;
    private ProgressBar _wealthBar = null!;

    private ProgressBar _lifespanBar = null!;
    private Label _lifespanWarning = null!;

    private Label _navigatorBubbleLabel = null!;
    private CanvasItem _navigatorBubble = null!;

    private float _statAutoCloseSeconds;
    private Tween? _bubbleTween;

    public override void _Ready()
    {
        _nameAgeLabel = GetNode<Label>("TopBar/TopRow/LeftGroup/NameAgeLabel");
        _phaseLabel = GetNode<Label>("TopBar/TopRow/LeftGroup/PhaseLabel");
        _generationLabel = GetNode<Label>("TopBar/TopRow/RightGroup/GenerationLabel");
        _scoreLabel = GetNode<Label>("TopBar/TopRow/RightGroup/ScoreLabel");

        _collapsedDots = GetNode<Control>("StatDock/CollapsedDots");
        _expandedStatPanel = GetNode<PanelContainer>("StatDock/ExpandedPanel");

        _vitalityLabel = GetNode<Label>("StatDock/ExpandedPanel/Margin/VBox/VitalityRow/Value");
        _intelligenceLabel = GetNode<Label>("StatDock/ExpandedPanel/Margin/VBox/IntelligenceRow/Value");
        _charismaLabel = GetNode<Label>("StatDock/ExpandedPanel/Margin/VBox/CharismaRow/Value");
        _luckLabel = GetNode<Label>("StatDock/ExpandedPanel/Margin/VBox/LuckRow/Value");
        _wealthLabel = GetNode<Label>("StatDock/ExpandedPanel/Margin/VBox/WealthRow/Value");

        _vitalityBar = GetNode<ProgressBar>("StatDock/ExpandedPanel/Margin/VBox/VitalityRow/Bar");
        _intelligenceBar = GetNode<ProgressBar>("StatDock/ExpandedPanel/Margin/VBox/IntelligenceRow/Bar");
        _charismaBar = GetNode<ProgressBar>("StatDock/ExpandedPanel/Margin/VBox/CharismaRow/Bar");
        _luckBar = GetNode<ProgressBar>("StatDock/ExpandedPanel/Margin/VBox/LuckRow/Bar");
        _wealthBar = GetNode<ProgressBar>("StatDock/ExpandedPanel/Margin/VBox/WealthRow/Bar");

        _lifespanBar = GetNode<ProgressBar>("LifespanDock/LifespanBar");
        _lifespanWarning = GetNode<Label>("LifespanDock/Warning");

        _navigatorBubble = GetNode<CanvasItem>("Bottom/Navigator/Bubble");
        _navigatorBubbleLabel = GetNode<Label>("Bottom/Navigator/Bubble/Margin/Label");

        _expandedStatPanel.Visible = false;
        _navigatorBubble.Visible = false;
        _lifespanWarning.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_expandedStatPanel.Visible)
        {
            _statAutoCloseSeconds -= (float)delta;
            if (_statAutoCloseSeconds <= 0f)
            {
                SetStatPanelExpanded(false);
            }
        }

        if (_lifespanWarning.Visible)
        {
            var alpha = 0.35f + Mathf.Abs(Mathf.Sin(Time.GetTicksMsec() / 130f)) * 0.65f;
            _lifespanWarning.Modulate = new Color(1f, 0.22f, 0.2f, alpha);
        }
    }

    public void SetTopBar(string name, int age, LifePhase phase, int generation, int score)
    {
        _nameAgeLabel.Text = $"{name} Age:{age}";
        _generationLabel.Text = $"Gen:{generation}";
        _scoreLabel.Text = $"Score:{score:N0}";

        _phaseLabel.Text = phase switch
        {
            LifePhase.Childhood => "●幼少期",
            LifePhase.Youth => "●青年期",
            LifePhase.Midlife => "●壮年期",
            LifePhase.Elderly => "●老年期",
            _ => "●不明",
        };

        _phaseLabel.Modulate = phase switch
        {
            LifePhase.Childhood => new Color(0.49f, 0.82f, 0.95f),
            LifePhase.Youth => new Color(0.34f, 0.8f, 0.44f),
            LifePhase.Midlife => new Color(0.97f, 0.62f, 0.26f),
            LifePhase.Elderly => new Color(0.72f, 0.72f, 0.72f),
            _ => Colors.White,
        };
    }

    public void SetStats(int vitality, int intelligence, int charisma, int luck, int wealth)
    {
        SetStatRow(_vitalityLabel, _vitalityBar, vitality);
        SetStatRow(_intelligenceLabel, _intelligenceBar, intelligence);
        SetStatRow(_charismaLabel, _charismaBar, charisma);
        SetStatRow(_luckLabel, _luckBar, luck);
        SetStatRow(_wealthLabel, _wealthBar, wealth);
    }

    public void UpdateLifespanRatio(float ratio)
    {
        ratio = Mathf.Clamp(ratio, 0f, 1f);
        _lifespanBar.Value = ratio * 100f;

        if (ratio > 0.8f)
        {
            _lifespanBar.Modulate = new Color(1f, 0.62f, 0.24f);
            _lifespanWarning.Visible = false;
        }
        else if (ratio > 0.5f)
        {
            _lifespanBar.Modulate = new Color(0.95f, 0.86f, 0.24f);
            _lifespanWarning.Visible = false;
        }
        else if (ratio > 0.2f)
        {
            _lifespanBar.Modulate = new Color(0.95f, 0.34f, 0.26f);
            _lifespanWarning.Visible = false;
        }
        else
        {
            _lifespanBar.Modulate = new Color(0.95f, 0.18f, 0.18f);
            _lifespanWarning.Visible = true;
        }
    }

    public void ToggleStatPanel()
    {
        SetStatPanelExpanded(!_expandedStatPanel.Visible);
    }

    public void ShowNavigatorBubble(string text, float displaySeconds = 3f)
    {
        _navigatorBubbleLabel.Text = text;
        _navigatorBubble.Visible = true;

        _bubbleTween?.Kill();
        _bubbleTween = CreateTween();
        _navigatorBubble.Modulate = new Color(1f, 1f, 1f, 0f);
        _bubbleTween.TweenProperty(_navigatorBubble, "modulate:a", 1f, 0.2f);
        _bubbleTween.TweenInterval(displaySeconds);
        _bubbleTween.TweenProperty(_navigatorBubble, "modulate:a", 0f, 0.3f);
        _bubbleTween.Finished += () => _navigatorBubble.Visible = false;
    }

    private void SetStatPanelExpanded(bool expanded)
    {
        _collapsedDots.Visible = !expanded;
        _expandedStatPanel.Visible = expanded;
        _statAutoCloseSeconds = expanded ? 3f : 0f;
    }

    private static void SetStatRow(Label valueLabel, ProgressBar progressBar, int value)
    {
        var clamped = Mathf.Clamp(value, 0, 100);
        valueLabel.Text = clamped.ToString();
        progressBar.Value = clamped;
    }
}
