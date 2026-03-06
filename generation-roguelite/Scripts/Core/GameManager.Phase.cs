using GenerationRoguelite.Navigator;
using GenerationRoguelite.Events;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void OnPhaseChanged(LifePhase phase)
    {
        _currentTerrain = TerrainProfile.ForPhase(phase);
        StartPhaseTransitionEffects(phase);

        if (phase == LifePhase.Youth && !_lifePathSelected)
        {
            _lifePath = DetermineLifePath();
            _lifePathSelected = true;
        }

        _eventLabel.Text = phase switch
        {
            LifePhase.Youth => "青年期に突入。アクション密度上昇!",
            LifePhase.Midlife => "壮年期。経験で受け流す時期。",
            LifePhase.Elderly => "老年期。慎重な判断が命をつなぐ。",
            _ => "幼少期。成長のはじまり。",
        };

        if (phase == LifePhase.Youth)
        {
            _eventLabel.Text += $"\n人生パス: {_lifePath}";
        }

        if (phase == LifePhase.Elderly && !_hasSpouse)
        {
            _forcedAdoption = !_adoptionUsed;
            _eventLabel.Text += _adoptionUsed
                ? "\n伴侶不在かつ養子縁組は使用済み。家系途絶の危機。"
                : "\n伴侶がいないため、次世代は養子を迎える必要がある。";
            RecordGenerationEvent("老年期突入: 次世代条件を更新", true);
        }

        ApplyPlayerVisualForPhase(phase);
        ApplyPhaseHudTheme(phase);
        Speak(_navigatorManager.OnPhaseChanged(phase));
        _lastPhaseForTransition = phase;
    }

    private void UpdateChildhoodAcceleration()
    {
        if (_phaseManager.CurrentPhase != LifePhase.Childhood)
        {
            _timeManager.SecondsPerYear = 1f;
            return;
        }

        _timeManager.SecondsPerYear = _currentInheritanceSeed >= 30 ? 0.3f : 1f;
    }

    private void ApplyPhaseHudTheme(LifePhase phase)
    {
        var phaseColor = phase switch
        {
            LifePhase.Childhood => new Color(0.49f, 0.82f, 0.95f),
            LifePhase.Youth => new Color(0.3f, 0.75f, 0.35f),
            LifePhase.Midlife => new Color(0.95f, 0.62f, 0.26f),
            LifePhase.Elderly => new Color(0.62f, 0.62f, 0.62f),
            _ => Colors.White,
        };

        _phaseLabel.AddThemeColorOverride("font_color", phaseColor);
        _hud.Modulate = new Color(1f, 1f, 1f, 0.96f);
    }

    private void StartPhaseTransitionEffects(LifePhase phase)
    {
        _phaseSlowMotionRemaining = PhaseSlowMotionSeconds;
        _phaseBannerElapsed = 0f;
        _phaseBannerActive = true;
        _phaseBannerLabel.Text = $"— {PhaseToText(phase)} —";
        _phaseBannerLabel.Visible = true;
        _phaseBannerLabel.Position = new Vector2(PhaseBannerStartX, _phaseBannerBaseY);
        _phaseBannerLabel.Modulate = Colors.White;
    }

    private DialogueData BuildPhaseTransitionDialogue(LifePhase fromPhase, LifePhase toPhase)
    {
        string[] templates = (fromPhase, toPhase) switch
        {
            (LifePhase.Childhood, LifePhase.Youth) =>
            [
                "大人の世界へようこそ！",
                "さあ、冒険の始まりだよ！",
            ],
            (LifePhase.Youth, LifePhase.Midlife) =>
            [
                "あれ、ジャンプが…もう無理みたい。",
                "経験が力になる時期だね。",
            ],
            (LifePhase.Midlife, LifePhase.Elderly) =>
            [
                "無理しないでね…",
                "静かに、でも確実に歩もう。",
            ],
            _ => [],
        };

        if (templates.Length == 0)
        {
            return _navigatorManager.OnPhaseChanged(toPhase);
        }

        var index = (int)_rng.RandiRange(0, templates.Length - 1);
        return new DialogueData(templates[index], "navi_phase_transition");
    }

    private string DetermineLifePath()
    {
        if (_character.Stats.Wealth >= _character.Stats.Vitality
            && _character.Stats.Wealth >= _character.Stats.Intelligence)
        {
            return "就職";
        }

        if (_character.Stats.Intelligence >= _character.Stats.Vitality)
        {
            return "修行";
        }

        return "冒険";
    }
}
