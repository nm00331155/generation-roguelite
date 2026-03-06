using System;
using GenerationRoguelite.Character;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private Rect2 GetPlayerRect()
    {
        return new Rect2(
            _player.Position.X - _playerHalfWidth,
            _player.Position.Y - _playerHeight,
            _playerHalfWidth * 2f,
            _playerHeight);
    }

    private void TickBackground(double delta)
    {
        var scroll = (Vector2)_parallaxBackground.Get("scroll_offset");
        scroll.X += BackgroundScrollSpeed * _walkSpeedScale * (float)delta;
        _parallaxBackground.Set("scroll_offset", scroll);
    }

    private void TickPlayerMotion(double delta)
    {
        var dt = (float)delta;

        if (!Mathf.IsEqualApprox(_player.Position.X, PlayerFixedX))
        {
            _player.Position = new Vector2(PlayerFixedX, _player.Position.Y);
        }

        if (_player.Position.Y < _playerGroundY || _playerVerticalVelocity != 0f)
        {
            _playerVerticalVelocity += PlayerGravity * dt;
            var nextY = _player.Position.Y + _playerVerticalVelocity * dt;

            if (nextY >= _playerGroundY)
            {
                nextY = _playerGroundY;
                _playerVerticalVelocity = 0f;
            }

            _player.Position = new Vector2(_player.Position.X, nextY);
        }

        if (_attackAnimationRemaining > 0f)
        {
            _attackAnimationRemaining -= dt;
            var ratio = Mathf.Clamp(_attackAnimationRemaining / AttackAnimationDuration, 0f, 1f);
            _playerVisual.Rotation = Mathf.Lerp(0f, -0.35f, ratio);
        }
        else
        {
            _playerVisual.Rotation = Mathf.MoveToward(_playerVisual.Rotation, 0f, dt * 4f);
        }
    }

    private void TryStartJump()
    {
        if (_phaseManager.CurrentPhase != LifePhase.Youth)
        {
            return;
        }

        if (_player.Position.Y >= _playerGroundY - 0.01f)
        {
            _playerVerticalVelocity = -YouthJumpVelocity;
        }
    }

    private void ApplyDropPresentation(DropPresentation presentation)
    {
        _eventLabel.AddThemeColorOverride("font_color", presentation.TextColor);
        if (!presentation.ShouldFlash)
        {
            return;
        }

        _dropFlashRemaining = DropFlashDuration;
        _dropFlashRect.Visible = true;
        _dropFlashRect.Color = new Color(1f, 1f, 1f, 0.58f);
    }

    private void TickDropFlash(double delta)
    {
        if (_dropFlashRemaining <= 0f)
        {
            if (_dropFlashRect.Visible)
            {
                _dropFlashRect.Visible = false;
                _dropFlashRect.Color = new Color(1f, 1f, 1f, 0f);
            }

            return;
        }

        _dropFlashRemaining -= (float)delta;
        var alpha = Mathf.Clamp(_dropFlashRemaining / DropFlashDuration, 0f, 1f) * 0.58f;
        _dropFlashRect.Visible = alpha > 0.01f;
        _dropFlashRect.Color = new Color(1f, 1f, 1f, alpha);
    }

    private void ApplyPlayerVisualForPhase(LifePhase phase)
    {
        var (width, height, color) = phase switch
        {
            LifePhase.Childhood => (72f, 72f, new Color(0.31f, 0.76f, 0.97f)),
            LifePhase.Youth => (90f, 126f, new Color(0.3f, 0.75f, 0.35f)),
            LifePhase.Midlife => (100f, 126f, new Color(0.95f, 0.62f, 0.26f)),
            LifePhase.Elderly => (82f, 108f, new Color(0.62f, 0.62f, 0.62f)),
            _ => (72f, 72f, Colors.White),
        };

        _playerHalfWidth = width * 0.5f;
        _playerHeight = height;
        _playerVisual.Polygon =
        [
            new Vector2(-_playerHalfWidth, -_playerHeight),
            new Vector2(_playerHalfWidth, -_playerHeight),
            new Vector2(_playerHalfWidth, 0f),
            new Vector2(-_playerHalfWidth, 0f),
        ];
        _playerVisual.Color = color;
    }

    private void TickPhaseTransitionEffects(double delta)
    {
        if (_phaseSlowMotionRemaining > 0f)
        {
            Engine.TimeScale = PhaseSlowMotionScale;
            _phaseSlowMotionRemaining -= (float)(delta / Math.Max(PhaseSlowMotionScale, 0.01f));
            if (_phaseSlowMotionRemaining <= 0f)
            {
                _phaseSlowMotionRemaining = 0f;
                Engine.TimeScale = 1f;
            }
        }
        else if (!Mathf.IsEqualApprox(Engine.TimeScale, 1f))
        {
            Engine.TimeScale = 1f;
        }

        TickPhaseBanner(delta);
    }

    private void TickPhaseBanner(double delta)
    {
        if (!_phaseBannerActive)
        {
            return;
        }

        _phaseBannerElapsed += (float)delta;
        var progress = _phaseBannerElapsed / PhaseBannerDurationSeconds;
        if (progress >= 1f)
        {
            _phaseBannerActive = false;
            _phaseBannerLabel.Visible = false;
            _phaseBannerLabel.Position = new Vector2(PhaseBannerStartX, _phaseBannerBaseY);
            _phaseBannerLabel.Modulate = Colors.White;
            return;
        }

        var slide = Mathf.Clamp(_phaseBannerElapsed / PhaseBannerSlideSeconds, 0f, 1f);
        var x = Mathf.Lerp(PhaseBannerStartX, PhaseBannerCenterX, slide);
        var alpha = progress <= 0.72f ? 1f : 1f - ((progress - 0.72f) / 0.28f);

        _phaseBannerLabel.Visible = true;
        _phaseBannerLabel.Position = new Vector2(x, _phaseBannerBaseY);
        _phaseBannerLabel.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(alpha, 0f, 1f));
    }

    private void ApplyTheme()
    {
        _background.Color = _cosmeticManager.ResolveBackgroundColor();
    }
}
