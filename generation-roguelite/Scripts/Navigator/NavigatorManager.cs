using System;
using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Core;
using GenerationRoguelite.Events;
using Godot;

namespace GenerationRoguelite.Navigator;

public sealed class NavigatorManager
{
    private const float IdleTalkIntervalSeconds = 60f;
    private const float SpeechWindowSeconds = 60f;
    private const int MaxSpeechPerMinute = 3;
    private const float WarningCooldownSeconds = 6f;

    private readonly RandomNumberGenerator _rng = new();
    private readonly HashSet<string> _unlockedProfiles = new(StringComparer.Ordinal)
    {
        "default",
    };

    private readonly Queue<float> _recentSpeechTimes = new();

    private readonly DialogueData[] _eventStartLines;
    private readonly DialogueData[] _eventSuccessLines;
    private readonly DialogueData[] _eventFailLines;
    private readonly DialogueData[] _phaseYouthLines;
    private readonly DialogueData[] _phaseAdultLines;
    private readonly DialogueData[] _phaseElderLines;
    private readonly DialogueData[] _randomLines;
    private readonly DialogueData[] _warningLines;
    private readonly DialogueData[] _deathLines;
    private readonly DialogueData[] _generationLines;
    private readonly DialogueData[] _gameOverLines;

    private float _runtimeSeconds;
    private float _silenceSeconds;
    private float _warningCooldownSeconds;
    private float _affinity;

    public string ActiveProfileId { get; private set; } = "default";

    public NavigatorManager()
    {
        _rng.Randomize();

        _eventStartLines =
        [
            new DialogueData("何か起きそうだ。", "navi_event_start_01"),
            new DialogueData("面白い出会いだな。", "navi_event_start_02"),
            new DialogueData("気をつけろよ。", "navi_event_start_03"),
        ];

        _eventSuccessLines =
        [
            new DialogueData("やるじゃないか。", "navi_event_success_01"),
            new DialogueData("見事だ。", "navi_event_success_02"),
            new DialogueData("ツイてるな。", "navi_event_success_03"),
        ];

        _eventFailLines =
        [
            new DialogueData("残念だったな。", "navi_event_fail_01"),
            new DialogueData("次がある。", "navi_event_fail_02"),
            new DialogueData("まぁこんなものか。", "navi_event_fail_03"),
        ];

        _phaseYouthLines =
        [
            new DialogueData("大人の仲間入りだ。", "navi_phase_youth_01"),
            new DialogueData("ここからが本番だぞ。", "navi_phase_youth_02"),
        ];

        _phaseAdultLines =
        [
            new DialogueData("落ち着いてきたな。", "navi_phase_adult_01"),
            new DialogueData("守るものが増えたか。", "navi_phase_adult_02"),
        ];

        _phaseElderLines =
        [
            new DialogueData("長い道のりだったな。", "navi_phase_elder_01"),
            new DialogueData("最後まで見届けよう。", "navi_phase_elder_02"),
        ];

        _randomLines =
        [
            new DialogueData("いい天気だな。", "navi_random_01"),
            new DialogueData("何か落ちてるぞ。", "navi_random_02"),
            new DialogueData("この道は長いな。", "navi_random_03"),
        ];

        _warningLines =
        [
            new DialogueData("危ない！", "navi_warning_01"),
            new DialogueData("気をつけろ！", "navi_warning_02"),
            new DialogueData("避けろ！", "navi_warning_03"),
        ];

        _deathLines =
        [
            new DialogueData("お疲れ様だった。", "navi_death_01"),
            new DialogueData("安らかに。", "navi_death_02"),
        ];

        _generationLines =
        [
            new DialogueData("新しい命だ。", "navi_generation_01"),
            new DialogueData("意志は受け継がれる。", "navi_generation_02"),
        ];

        _gameOverLines =
        [
            new DialogueData("家系が…途絶えてしまった。", "navi_gameover_01"),
        ];

        if (NavigatorDialogue.TryLoadDefault(out var loaded, out var error))
        {
            _eventStartLines = PickLoadedOrFallback(loaded.EventStartLines, _eventStartLines);
            _eventSuccessLines = PickLoadedOrFallback(loaded.EventSuccessLines, _eventSuccessLines);
            _eventFailLines = PickLoadedOrFallback(loaded.EventFailLines, _eventFailLines);
            _phaseYouthLines = PickLoadedOrFallback(loaded.PhaseYouthLines, _phaseYouthLines);
            _phaseAdultLines = PickLoadedOrFallback(loaded.PhaseAdultLines, _phaseAdultLines);
            _phaseElderLines = PickLoadedOrFallback(loaded.PhaseElderLines, _phaseElderLines);
            _randomLines = PickLoadedOrFallback(loaded.RandomLines, _randomLines);
            _warningLines = PickLoadedOrFallback(loaded.WarningLines, _warningLines);
            _deathLines = PickLoadedOrFallback(loaded.DeathLines, _deathLines);
            _generationLines = PickLoadedOrFallback(loaded.GenerationLines, _generationLines);
            _gameOverLines = PickLoadedOrFallback(loaded.GameOverLines, _gameOverLines);
        }
        else
        {
            GD.Print($"[NavigatorManager] default_dialogues fallback: {error}");
        }
    }

    public DialogueData OnGenerationStart()
    {
        _affinity = Mathf.Clamp(_affinity + 0.02f, 0f, 1f);
        return EmitCritical(_generationLines);
    }

    public DialogueData OnGenerationEnd()
    {
        _affinity = Mathf.Clamp(_affinity + 0.01f, 0f, 1f);
        return EmitCritical(_deathLines.Length > 0 ? _deathLines : _generationLines);
    }

    public DialogueData OnGameOver()
    {
        return EmitCritical(_gameOverLines);
    }

    public DialogueData OnPhaseChanged(LifePhase phase)
    {
        _affinity = Mathf.Clamp(_affinity + 0.02f, 0f, 1f);

        var source = phase switch
        {
            LifePhase.Youth => _phaseYouthLines,
            LifePhase.Midlife => _phaseAdultLines,
            LifePhase.Elderly => _phaseElderLines,
            _ => _phaseYouthLines,
        };

        return EmitCritical(source);
    }

    public DialogueData OnEventPresented(EventData eventData)
    {
        var line = TryEmit(_eventStartLines, 0.7f, true);
        if (!line.HasText)
        {
            return default;
        }

        return line with { Text = $"{line.Text} {eventData.TapChoice.Text}か、{eventData.SwipeChoice.Text}か。" };
    }

    public DialogueData OnEventResolved(EventResolution resolution, bool isTimeout)
    {
        if (isTimeout)
        {
            _affinity = Mathf.Clamp(_affinity + 0.003f, 0f, 1f);
            return TryEmit(_eventFailLines, 0.5f, true);
        }

        if (resolution.Success)
        {
            _affinity = Mathf.Clamp(_affinity + 0.01f, 0f, 1f);
            return TryEmit(_eventSuccessLines, 0.5f, true);
        }

        _affinity = Mathf.Clamp(_affinity - 0.01f, 0f, 1f);
        return TryEmit(_eventFailLines, 0.5f, true);
    }

    public DialogueData OnObstacleApproaching(bool hasCluster)
    {
        if (!hasCluster || _warningCooldownSeconds > 0f)
        {
            return default;
        }

        _warningCooldownSeconds = WarningCooldownSeconds;
        return TryEmit(_warningLines, 0.5f, true);
    }

    public DialogueData TickIdle(double delta)
    {
        AdvanceTimers((float)delta);

        if (_silenceSeconds < IdleTalkIntervalSeconds)
        {
            return default;
        }

        return TryEmit(_randomLines, 1f, true);
    }

    public bool UnlockProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return false;
        }

        return _unlockedProfiles.Add(profileId);
    }

    public bool SetActiveProfile(string profileId)
    {
        if (!_unlockedProfiles.Contains(profileId))
        {
            return false;
        }

        ActiveProfileId = profileId;
        return true;
    }

    public string CycleToNextProfile()
    {
        var profiles = _unlockedProfiles.OrderBy(id => id).ToArray();
        if (profiles.Length == 0)
        {
            ActiveProfileId = "default";
            return ActiveProfileId;
        }

        var currentIndex = Array.IndexOf(profiles, ActiveProfileId);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % profiles.Length;
        ActiveProfileId = profiles[nextIndex];
        return ActiveProfileId;
    }

    public string BuildProfileSummary()
    {
        var list = string.Join(",", _unlockedProfiles.OrderBy(id => id));
        return $"ナビ:{ActiveProfileId} / 所持[{list}]";
    }

    public string GetSummaryText(EventData eventData)
    {
        return $"{eventData.TapChoice.Text} or {eventData.SwipeChoice.Text}";
    }

    private static DialogueData[] PickLoadedOrFallback(DialogueData[] loaded, DialogueData[] fallback)
    {
        return loaded.Length > 0 ? loaded : fallback;
    }

    private void AdvanceTimers(float delta)
    {
        if (delta <= 0f)
        {
            return;
        }

        _runtimeSeconds += delta;
        _silenceSeconds += delta;
        _warningCooldownSeconds = Mathf.Max(0f, _warningCooldownSeconds - delta);
    }

    private DialogueData EmitCritical(IReadOnlyList<DialogueData> source)
    {
        return TryEmit(source, 1f, false);
    }

    private DialogueData TryEmit(IReadOnlyList<DialogueData> source, float probability, bool respectMinuteLimit)
    {
        if (source.Count == 0)
        {
            return default;
        }

        if (probability < 1f && _rng.Randf() > probability)
        {
            return default;
        }

        if (respectMinuteLimit && !CanSpeakMinuteWindow())
        {
            return default;
        }

        var dialogue = ApplyProfileTone(Pick(source));
        RegisterSpeech(respectMinuteLimit);
        return dialogue;
    }

    private bool CanSpeakMinuteWindow()
    {
        TrimSpeechWindow();
        return _recentSpeechTimes.Count < MaxSpeechPerMinute;
    }

    private void RegisterSpeech(bool countTowardMinuteWindow)
    {
        _silenceSeconds = 0f;

        if (!countTowardMinuteWindow)
        {
            return;
        }

        TrimSpeechWindow();
        _recentSpeechTimes.Enqueue(_runtimeSeconds);
    }

    private void TrimSpeechWindow()
    {
        while (_recentSpeechTimes.Count > 0 && _runtimeSeconds - _recentSpeechTimes.Peek() >= SpeechWindowSeconds)
        {
            _recentSpeechTimes.Dequeue();
        }
    }

    private DialogueData Pick(IReadOnlyList<DialogueData> list)
    {
        return list[(int)_rng.RandiRange(0, list.Count - 1)];
    }

    private DialogueData ApplyProfileTone(DialogueData dialogue)
    {
        var text = ActiveProfileId switch
        {
            "mentor" => $"ふむ… {dialogue.Text}",
            "trickster" => $"へへっ、{dialogue.Text}",
            "oracle" => $"……視えた。{dialogue.Text}",
            _ => dialogue.Text,
        };

        return dialogue with { Text = text };
    }
}
