using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Character;

public partial class PlayerCharacter : CharacterBody2D
{
    [Signal]
    public delegate void AttackWindowChangedEventHandler(bool enabled);

    [Signal]
    public delegate void CaneBuffChangedEventHandler(bool enabled, float speedScale);

    [Export]
    public NodePath VisualPath { get; set; } = "Visual";

    private ColorRect _visual = null!;
    private readonly CharacterAction _characterAction = new();

    private float _attackWindowRemaining;
    private float _defendRemaining;
    private float _caneRemaining;

    private bool _attackEnabled;
    private bool _defending;

    public override void _Ready()
    {
        _visual = GetNode<ColorRect>(VisualPath);
        ApplyPhase(LifePhase.Childhood);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        var phase = DetectPhaseByScale();
        var result = _characterAction.Consume(phase, @event);
        if (!result.IsValid)
        {
            return;
        }

        ApplyAction(result);
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;

        Velocity += Vector2.Down * 2200f * dt;
        MoveAndSlide();

        _attackWindowRemaining = Mathf.Max(0f, _attackWindowRemaining - dt);
        if (_attackEnabled && _attackWindowRemaining <= 0f)
        {
            _attackEnabled = false;
            EmitSignal(SignalName.AttackWindowChanged, false);
        }

        _defendRemaining = Mathf.Max(0f, _defendRemaining - dt);
        _defending = _defendRemaining > 0f;

        _caneRemaining = Mathf.Max(0f, _caneRemaining - dt);
        if (_caneRemaining <= 0f)
        {
            EmitSignal(SignalName.CaneBuffChanged, false, 1f);
        }
    }

    public bool IsDefending()
    {
        return _defending;
    }

    public void ApplyPhase(LifePhase phase)
    {
        switch (phase)
        {
            case LifePhase.Childhood:
                ApplyVisualStyle(new Color(0.49f, 0.82f, 0.95f), new Vector2(72f, 72f));
                break;

            case LifePhase.Youth:
                ApplyVisualStyle(new Color(0.31f, 0.82f, 0.43f), new Vector2(90f, 126f));
                break;

            case LifePhase.Midlife:
                ApplyVisualStyle(new Color(0.94f, 0.62f, 0.29f), new Vector2(100f, 126f));
                break;

            case LifePhase.Elderly:
                ApplyVisualStyle(new Color(0.72f, 0.72f, 0.72f), new Vector2(82f, 108f));
                break;
        }
    }

    public void PlayDeathVisual()
    {
        _visual.Color = new Color(1f, 0.24f, 0.24f);
        var tween = CreateTween();
        tween.TweenProperty(_visual, "modulate:a", 0f, 0.5f);
    }

    private void ApplyAction(CharacterActionResult result)
    {
        switch (result.ActionType)
        {
            case CharacterActionType.Jump:
                Velocity = new Vector2(Velocity.X, -result.Magnitude);
                break;

            case CharacterActionType.Attack:
                _attackWindowRemaining = result.DurationSeconds;
                if (!_attackEnabled)
                {
                    _attackEnabled = true;
                    EmitSignal(SignalName.AttackWindowChanged, true);
                }

                break;

            case CharacterActionType.Defend:
                _defendRemaining = result.DurationSeconds;
                break;

            case CharacterActionType.Cane:
                _caneRemaining = result.DurationSeconds;
                EmitSignal(SignalName.CaneBuffChanged, true, result.Magnitude);
                break;

            case CharacterActionType.Evade:
                var from = Position;
                var to = from + Vector2.Left * result.Magnitude;
                var tween = CreateTween();
                tween.TweenProperty(this, "position", to, result.DurationSeconds * 0.5f);
                tween.TweenProperty(this, "position", from, result.DurationSeconds * 0.5f);
                break;
        }
    }

    private LifePhase DetectPhaseByScale()
    {
        var size = _visual.Size;
        if (size.IsEqualApprox(new Vector2(72f, 72f)))
        {
            return LifePhase.Childhood;
        }

        if (size.IsEqualApprox(new Vector2(90f, 126f)))
        {
            return LifePhase.Youth;
        }

        if (size.IsEqualApprox(new Vector2(100f, 126f)))
        {
            return LifePhase.Midlife;
        }

        return LifePhase.Elderly;
    }

    private void ApplyVisualStyle(Color color, Vector2 size)
    {
        _visual.Color = color;
        _visual.Size = size;
        _visual.Position = new Vector2(-size.X * 0.5f, -size.Y);
    }
}
