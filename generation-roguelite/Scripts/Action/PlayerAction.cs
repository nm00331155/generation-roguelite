using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Action;

public enum PlayerActionType
{
    None,
    Tap,
    Swipe,
}

public enum SwipeDirection
{
    None,
    Left,
    Right,
    Up,
    Down,
}

public readonly record struct PlayerActionResult(
    PlayerActionType Type,
    SwipeDirection SwipeDirection,
    Vector2 Position);

public sealed class PlayerAction
{
    private const float TapMaxSeconds = 0.3f;
    private const float TapMaxDistance = 20f;
    private const float SwipeThreshold = 50f;

    private static readonly PlayerActionResult NoneResult =
        new(PlayerActionType.None, SwipeDirection.None, Vector2.Zero);

    private bool _trackingTouch;
    private Vector2 _startPosition;
    private ulong _startTouchMs;

    public PlayerActionResult Consume(InputEvent inputEvent)
    {
        if (inputEvent is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                if (!IsGameplayArea(touchEvent.Position))
                {
                    _trackingTouch = false;
                    return NoneResult;
                }

                _trackingTouch = true;
                _startPosition = touchEvent.Position;
                _startTouchMs = Time.GetTicksMsec();
                return NoneResult;
            }

            if (_trackingTouch)
            {
                _trackingTouch = false;
                var duration = (Time.GetTicksMsec() - _startTouchMs) / 1000f;
                var distance = touchEvent.Position.DistanceTo(_startPosition);
                if (duration <= TapMaxSeconds && distance <= TapMaxDistance)
                {
                    return new PlayerActionResult(PlayerActionType.Tap, SwipeDirection.None, touchEvent.Position);
                }

                return NoneResult;
            }
        }

        if (inputEvent is InputEventScreenDrag dragEvent && _trackingTouch)
        {
            if (dragEvent.Position.DistanceTo(_startPosition) >= SwipeThreshold)
            {
                _trackingTouch = false;
                return BuildSwipeResult(dragEvent.Position);
            }
        }

        if (inputEvent is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex != MouseButton.Left)
            {
                return NoneResult;
            }

            if (mouseButton.Pressed)
            {
                if (!IsGameplayArea(mouseButton.Position))
                {
                    _trackingTouch = false;
                    return NoneResult;
                }

                _trackingTouch = true;
                _startPosition = mouseButton.Position;
                _startTouchMs = Time.GetTicksMsec();
                return NoneResult;
            }

            if (_trackingTouch)
            {
                _trackingTouch = false;
                var duration = (Time.GetTicksMsec() - _startTouchMs) / 1000f;
                var distance = mouseButton.Position.DistanceTo(_startPosition);
                if (duration <= TapMaxSeconds && distance <= TapMaxDistance)
                {
                    return new PlayerActionResult(PlayerActionType.Tap, SwipeDirection.None, mouseButton.Position);
                }

                return NoneResult;
            }
        }

        if (inputEvent is InputEventMouseMotion mouseMotion && _trackingTouch)
        {
            if (mouseMotion.Position.DistanceTo(_startPosition) >= SwipeThreshold)
            {
                _trackingTouch = false;
                return BuildSwipeResult(mouseMotion.Position);
            }
        }

        if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                return new PlayerActionResult(PlayerActionType.Tap, SwipeDirection.None, Vector2.Zero);
            }

            if (keyEvent.Keycode == Key.S)
            {
                return new PlayerActionResult(PlayerActionType.Swipe, SwipeDirection.Right, Vector2.Zero);
            }

            if (keyEvent.Keycode == Key.A)
            {
                return new PlayerActionResult(PlayerActionType.Swipe, SwipeDirection.Left, Vector2.Zero);
            }

            if (keyEvent.Keycode == Key.W)
            {
                return new PlayerActionResult(PlayerActionType.Swipe, SwipeDirection.Up, Vector2.Zero);
            }
        }

        return NoneResult;
    }

    public static string BuildHint(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => "Tap: 好奇心タッチ / Swipe: 無効",
            LifePhase.Youth => "Tap: ジャンプ / Swipe左右: 攻撃 / Swipe上: 無効",
            LifePhase.Midlife => "Tap: 防御(受け流し) / Swipe左右: 攻撃",
            LifePhase.Elderly => "Tap: 杖をつく / Swipe左右: 回避",
            _ => "Tap / Swipe",
        };
    }

    private PlayerActionResult BuildSwipeResult(Vector2 currentPosition)
    {
        var delta = currentPosition - _startPosition;
        var direction = ResolveSwipeDirection(delta);
        return new PlayerActionResult(PlayerActionType.Swipe, direction, currentPosition);
    }

    private static SwipeDirection ResolveSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
        {
            if (Mathf.IsZeroApprox(delta.X))
            {
                return SwipeDirection.None;
            }

            return delta.X >= 0f ? SwipeDirection.Right : SwipeDirection.Left;
        }

        return SwipeDirection.None;
    }

    private static bool IsGameplayArea(Vector2 position)
    {
        var windowHeight = DisplayServer.WindowGetSize().Y;
        return position.Y <= windowHeight * 0.5f;
    }
}
