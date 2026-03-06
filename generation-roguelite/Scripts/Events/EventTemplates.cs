using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Events;

public static class EventTemplates
{
    private const string JsonPath = "res://Scripts/Data/EventTemplateData.json";

    public static bool TryLoad(out List<TemplateEventDefinition> templates, out string error)
    {
        templates = new List<TemplateEventDefinition>();
        error = string.Empty;

        try
        {
            var path = ProjectSettings.GlobalizePath(JsonPath);
            if (!File.Exists(path))
            {
                error = "EventTemplateData.json が見つかりません";
                return false;
            }

            var raw = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<EventTemplateRoot>(raw);
            if (root?.Events is null || root.Events.Count == 0)
            {
                error = "テンプレートイベントが空です";
                return false;
            }

            foreach (var source in root.Events)
            {
                if (!TryConvert(source, out var converted))
                {
                    continue;
                }

                templates.Add(converted);
            }

            if (templates.Count == 0)
            {
                error = "有効なテンプレートイベントがありません";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryConvert(EventTemplateSource source, out TemplateEventDefinition definition)
    {
        definition = default;
        if (source.Choices is null || source.Choices.Count == 0 || string.IsNullOrWhiteSpace(source.Text))
        {
            return false;
        }

        var phase = ResolvePhase(source.MinAge, source.MaxAge);
        var ordered = source.Choices.OrderBy(choice => choice.Risk).ToList();

        var tap = BuildChoice(source.Choices.ElementAtOrDefault(0), "様子を見る");
        var swipe = BuildChoice(source.Choices.ElementAtOrDefault(1) ?? source.Choices.ElementAtOrDefault(0), "安全策を取る");
        var timeout = BuildChoice(ordered.FirstOrDefault(), "深呼吸して撤退する");

        var title = string.IsNullOrWhiteSpace(source.Title) ? "出来事" : source.Title.Trim();
        var eventText = $"{title}\n{source.Text.Trim()}";

        definition = new TemplateEventDefinition(
            source.Id ?? string.Empty,
            source.Category ?? "misc",
            phase,
            eventText,
            tap,
            swipe,
            timeout,
            TerrainProfile.ForPhase(phase));

        return true;
    }

    private static EventChoice BuildChoice(EventChoiceSource? source, string fallbackText)
    {
        if (source is null)
        {
            return new EventChoice(
                fallbackText,
                null,
                0,
                StatDelta.Zero,
                StatDelta.Zero,
                0f,
                0f,
                "特に変化はなかった。",
                "特に変化はなかった。");
        }

        var success = source.Success ?? ChoiceOutcomeSource.FromEffects(source.SuccessEffects, source.SuccessText);
        var failure = source.Failure ?? ChoiceOutcomeSource.FromEffects(source.FailEffects, source.FailText);
        var text = string.IsNullOrWhiteSpace(source.Text) ? fallbackText : source.Text.Trim();
        var checkStat = !string.IsNullOrWhiteSpace(source.CheckStat)
            ? source.CheckStat
            : source.SuccessStat;
        var difficulty = source.Difficulty > 0 ? source.Difficulty : source.SuccessThreshold;

        return new EventChoice(
            text,
            NormalizeStat(checkStat),
            Mathf.Max(0, difficulty),
            new StatDelta(success.Vitality, success.Intelligence, success.Charisma, success.Luck, success.Wealth),
            new StatDelta(failure.Vitality, failure.Intelligence, failure.Charisma, failure.Luck, failure.Wealth),
            Mathf.Max(0f, success.LifeDamage),
            Mathf.Max(0f, failure.LifeDamage),
            string.IsNullOrWhiteSpace(success.ResultText) ? "うまくいった。" : success.ResultText.Trim(),
            string.IsNullOrWhiteSpace(failure.ResultText) ? "うまくいかなかった。" : failure.ResultText.Trim(),
            Mathf.Clamp(source.DropChance, 0f, 1f));
    }

    private static string? NormalizeStat(string? checkStat)
    {
        if (string.IsNullOrWhiteSpace(checkStat))
        {
            return null;
        }

        return checkStat.Trim().ToLowerInvariant() switch
        {
            "health" => "vitality",
            "vitality" => "vitality",
            "int" => "intelligence",
            "intelligence" => "intelligence",
            "cha" => "charisma",
            "charisma" => "charisma",
            "luck" => "luck",
            "wealth" => "wealth",
            _ => null,
        };
    }

    private static LifePhase ResolvePhase(int minAge, int maxAge)
    {
        var mid = (minAge + maxAge) / 2;
        return mid switch
        {
            <= 12 => LifePhase.Childhood,
            <= 30 => LifePhase.Youth,
            <= 55 => LifePhase.Midlife,
            _ => LifePhase.Elderly,
        };
    }

    private sealed class EventTemplateRoot
    {
        [JsonPropertyName("events")]
        public List<EventTemplateSource> Events { get; set; } = new();
    }

    private sealed class EventTemplateSource
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("min_age")]
        public int MinAge { get; set; }

        [JsonPropertyName("max_age")]
        public int MaxAge { get; set; } = 99;

        [JsonPropertyName("choices")]
        public List<EventChoiceSource> Choices { get; set; } = new();
    }

    private sealed class EventChoiceSource
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("risk")]
        public int Risk { get; set; } = 5;

        [JsonPropertyName("check_stat")]
        public string? CheckStat { get; set; }

        [JsonPropertyName("success_stat")]
        public string? SuccessStat { get; set; }

        [JsonPropertyName("difficulty")]
        public int Difficulty { get; set; }

        [JsonPropertyName("success_threshold")]
        public int SuccessThreshold { get; set; }

        [JsonPropertyName("success")]
        public ChoiceOutcomeSource? Success { get; set; }

        [JsonPropertyName("failure")]
        public ChoiceOutcomeSource? Failure { get; set; }

        [JsonPropertyName("success_effects")]
        public ChoiceEffectsSource? SuccessEffects { get; set; }

        [JsonPropertyName("fail_effects")]
        public ChoiceEffectsSource? FailEffects { get; set; }

        [JsonPropertyName("success_text")]
        public string? SuccessText { get; set; }

        [JsonPropertyName("fail_text")]
        public string? FailText { get; set; }

        [JsonPropertyName("drop_chance")]
        public float DropChance { get; set; } = 0.5f;
    }

    private sealed class ChoiceEffectsSource
    {
        [JsonPropertyName("vitality")]
        public int Vitality { get; set; }

        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }

        [JsonPropertyName("charisma")]
        public int Charisma { get; set; }

        [JsonPropertyName("luck")]
        public int Luck { get; set; }

        [JsonPropertyName("wealth")]
        public int Wealth { get; set; }
    }

    private sealed class ChoiceOutcomeSource
    {
        [JsonPropertyName("vitality")]
        public int Vitality { get; set; }

        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }

        [JsonPropertyName("charisma")]
        public int Charisma { get; set; }

        [JsonPropertyName("luck")]
        public int Luck { get; set; }

        [JsonPropertyName("wealth")]
        public int Wealth { get; set; }

        [JsonPropertyName("life_damage")]
        public float LifeDamage { get; set; }

        [JsonPropertyName("result_text")]
        public string? ResultText { get; set; }

        public static ChoiceOutcomeSource FromEffects(ChoiceEffectsSource? effects, string? resultText)
        {
            effects ??= new ChoiceEffectsSource();
            return new ChoiceOutcomeSource
            {
                Vitality = effects.Vitality,
                Intelligence = effects.Intelligence,
                Charisma = effects.Charisma,
                Luck = effects.Luck,
                Wealth = effects.Wealth,
                ResultText = resultText,
            };
        }
    }
}

public readonly record struct TemplateEventDefinition(
    string Id,
    string Category,
    LifePhase Phase,
    string EventText,
    EventChoice TapChoice,
    EventChoice SwipeChoice,
    EventChoice TimeoutChoice,
    TerrainProfile Terrain);
