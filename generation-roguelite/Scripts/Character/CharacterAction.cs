using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Character;

public enum CharacterActionType
{
    None,
    CuriosityTouch,
    Jump,
    Attack,
    Defend,
    Cane,
    Evade,
}

public readonly record struct CharacterActionResult(
    CharacterActionType ActionType,
    float DurationSeconds,
    float Magnitude,
    string Description)
{
    public bool IsValid => ActionType != CharacterActionType.None;
}

public sealed class CharacterAction
{
    private const float TapMaxSeconds = 0.3f;
    private const float TapMaxDistance = 20f;
    private const float SwipeMinDistance = 50f;

    private bool _tracking;
    private Vector2 _startPosition;
    private ulong _startMs;

    public CharacterActionResult Consume(LifePhase phase, InputEvent inputEvent)
    {
        var inputType = ParseInputType(inputEvent);
        if (inputType == PlayerActionType.None)
        {
            return default;
        }

        return Resolve(phase, inputType);
    }

    private PlayerActionType ParseInputType(InputEvent inputEvent)
    {
        if (inputEvent is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _tracking = true;
                _startPosition = touch.Position;
                _startMs = Time.GetTicksMsec();
                return PlayerActionType.None;
            }

            if (_tracking)
            {
                _tracking = false;

                var duration = (Time.GetTicksMsec() - _startMs) / 1000f;
                var distance = touch.Position.DistanceTo(_startPosition);
                if (duration <= TapMaxSeconds && distance <= TapMaxDistance)
                {
                    return PlayerActionType.Tap;
                }
            }
        }

        if (inputEvent is InputEventScreenDrag drag && _tracking)
        {
            var delta = drag.Position - _startPosition;
            if (Mathf.Abs(delta.X) >= SwipeMinDistance && Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
            {
                _tracking = false;
                return PlayerActionType.Swipe;
            }
        }

        if (inputEvent is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Space)
            {
                return PlayerActionType.Tap;
            }

            if (key.Keycode == Key.S)
            {
                return PlayerActionType.Swipe;
            }
        }

        return PlayerActionType.None;
    }

    private static CharacterActionResult Resolve(LifePhase phase, PlayerActionType inputType)
    {
        return phase switch
        {
            LifePhase.Childhood when inputType == PlayerActionType.Tap =>
                new CharacterActionResult(CharacterActionType.CuriosityTouch, 0f, 1f, "好奇心タッチ"),

            LifePhase.Youth when inputType == PlayerActionType.Tap =>
                new CharacterActionResult(CharacterActionType.Jump, 0.4f, 400f, "ジャンプ"),

            LifePhase.Youth when inputType == PlayerActionType.Swipe =>
                new CharacterActionResult(CharacterActionType.Attack, 0.2f, 1f, "攻撃"),

            LifePhase.Midlife when inputType == PlayerActionType.Tap =>
                new CharacterActionResult(CharacterActionType.Defend, 0.5f, 0.2f, "防御"),

            LifePhase.Midlife when inputType == PlayerActionType.Swipe =>
                new CharacterActionResult(CharacterActionType.Attack, 0.2f, 1f, "攻撃"),

            LifePhase.Elderly when inputType == PlayerActionType.Tap =>
                new CharacterActionResult(CharacterActionType.Cane, 3f, 0.5f, "杖"),

            LifePhase.Elderly when inputType == PlayerActionType.Swipe =>
                new CharacterActionResult(CharacterActionType.Evade, 0.3f, 50f, "回避"),

            _ => default,
        };
    }

    private enum PlayerActionType
    {
        None,
        Tap,
        Swipe,
    }
}
