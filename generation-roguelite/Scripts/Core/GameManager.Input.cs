using GenerationRoguelite.Action;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    public override void _UnhandledInput(InputEvent @event)
    {
        CaptureTouchPosition(@event);

        if (TryHandleDebugCommand(@event))
        {
            UpdateHud();
            return;
        }

        if (_isInFuneral)
        {
            return;
        }

        if (_willPanel.Visible)
        {
            return;
        }

        var action = _playerAction.Consume(@event);
        if (action.Type == PlayerActionType.None)
        {
            return;
        }

        HandlePlayerAction(action);
    }

    private void HandlePlayerAction(PlayerActionResult action)
    {
        switch (_phaseManager.CurrentPhase)
        {
            case LifePhase.Childhood:
                HandleChildhoodAction(action);
                break;

            case LifePhase.Youth:
                HandleYouthAction(action);
                break;

            case LifePhase.Midlife:
                HandleMidlifeAction(action);
                break;

            case LifePhase.Elderly:
                HandleElderlyAction(action);
                break;
        }
    }

    private void HandleChildhoodAction(PlayerActionResult action)
    {
        if (action.Type == PlayerActionType.Swipe)
        {
            _eventLabel.Text = "幼少期ではスワイプはまだ使えない。";
            return;
        }

        if (TryCollectibleTouch(action.Position, out var result))
        {
            _eventLabel.Text = result;
            return;
        }

        _eventLabel.Text = "好奇心タッチ: 何も見つからなかった。";
    }

    private void HandleYouthAction(PlayerActionResult action)
    {
        if (action.Type == PlayerActionType.Tap)
        {
            TryStartJump();
            _avoidWindowRemaining = YouthTapAvoidWindow;
            _eventLabel.Text = "ジャンプで回避を試みた。";
            return;
        }

        if (action.SwipeDirection == SwipeDirection.Up)
        {
            _eventLabel.Text = "上スワイプは青年期では無効。";
            return;
        }

        if (action.SwipeDirection is SwipeDirection.Left or SwipeDirection.Right)
        {
            TriggerAttackMotion();
            if (TryAttackForward(out var attackResult))
            {
                _eventLabel.Text = attackResult;
            }
            else
            {
                _eventLabel.Text = "攻撃したが、敵に届かなかった。";
            }

            return;
        }

        _eventLabel.Text = "この方向のスワイプは無効。";
    }

    private void HandleMidlifeAction(PlayerActionResult action)
    {
        if (action.Type == PlayerActionType.Tap)
        {
            _parryWindowRemaining = MidlifeParryWindowSeconds;
            _eventLabel.Text = "防御体勢: 受け流し準備。";
            return;
        }

        if (action.SwipeDirection is SwipeDirection.Left or SwipeDirection.Right)
        {
            TriggerAttackMotion();
            if (TryAttackForward(out var attackResult))
            {
                _eventLabel.Text = attackResult;
            }
            else
            {
                _eventLabel.Text = "攻撃したが、敵に届かなかった。";
            }

            return;
        }

        _eventLabel.Text = "壮年期では左右スワイプのみ攻撃に対応。";
    }

    private void HandleElderlyAction(PlayerActionResult action)
    {
        if (action.Type == PlayerActionType.Tap)
        {
            ActivateElderlyCaneStance();
            return;
        }

        if (action.SwipeDirection is SwipeDirection.Left or SwipeDirection.Right)
        {
            TriggerElderlyEvade(action.SwipeDirection);
            return;
        }

        _eventLabel.Text = "老年期では左右スワイプで回避する。";
    }

    private void CaptureTouchPosition(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventScreenTouch touch when touch.Pressed:
                _lastTouchPosition = touch.Position;
                _debugOverlay.SetLastTouch(_lastTouchPosition);
                break;

            case InputEventScreenDrag drag:
                _lastTouchPosition = drag.Position;
                _debugOverlay.SetLastTouch(_lastTouchPosition);
                break;

            case InputEventMouseButton mouseButton when mouseButton.Pressed:
                _lastTouchPosition = mouseButton.Position;
                _debugOverlay.SetLastTouch(_lastTouchPosition);
                break;
        }
    }
}
