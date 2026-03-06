using System;
using GenerationRoguelite.Action;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private bool TryAttackForward(out string message)
    {
        var playerFrontX = GetPlayerRect().End.X;
        if (!_obstacleSpawner.TryDestroyFrontEnemy(playerFrontX, AttackReachX, out var reward))
        {
            message = string.Empty;
            return false;
        }

        _generationScore += 3;
        if (reward.WealthGain > 0)
        {
            _character.Stats.ApplyDelta(wealth: reward.WealthGain);
        }

        var dropText = reward.WealthGain > 0
            ? reward.DropText
            : $"{reward.DropText}を獲得";
        message = $"敵を撃破! {dropText}";
        RecordGenerationEvent($"敵撃破: {reward.DropText}", true);
        return true;
    }

    private void ActivateElderlyCaneStance()
    {
        _elderlyCaneRemaining = ElderlyCaneDurationSeconds;
        _walkSpeedScale = ElderlyWalkSpeedScale;
        _obstacleSpawner.SetSpawnSuppression(ElderlyCaneDurationSeconds);
        _eventLabel.Text = "杖をついて歩幅を整えた。3秒間、障害物出現を抑える。";
    }

    private void TriggerElderlyEvade(SwipeDirection direction)
    {
        _avoidWindowRemaining = ElderlySwipeAvoidWindow;
        var offset = direction == SwipeDirection.Left ? -ElderlyEvadeOffset : ElderlyEvadeOffset * 0.5f;
        var nextY = Mathf.Clamp(_player.Position.Y + offset, _playerGroundY - 64f, _playerGroundY + 20f);
        _player.Position = new Vector2(_player.Position.X, nextY);
        _eventLabel.Text = "回避動作!";
    }

    private void TickElderlyCaneState(double delta)
    {
        if (_phaseManager.CurrentPhase != LifePhase.Elderly)
        {
            _elderlyCaneRemaining = 0f;
            _walkSpeedScale = 1f;
            return;
        }

        if (_elderlyCaneRemaining <= 0f)
        {
            _walkSpeedScale = 1f;
            return;
        }

        _elderlyCaneRemaining -= (float)delta;
        if (_elderlyCaneRemaining <= 0f)
        {
            _elderlyCaneRemaining = 0f;
            _walkSpeedScale = 1f;
        }
    }

    private void TriggerAttackMotion()
    {
        if (_phaseManager.CurrentPhase != LifePhase.Youth
            && _phaseManager.CurrentPhase != LifePhase.Midlife)
        {
            return;
        }

        _attackAnimationRemaining = AttackAnimationDuration;
    }
}
