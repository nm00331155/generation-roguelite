using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GenerationRoguelite.Events;

namespace GenerationRoguelite.SLM;

public sealed partial class JsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex EffectRegex = BuildEffectRegex();

    public bool TryParse(string json, out EventData eventData, out string error)
    {
        eventData = null!;
        error = string.Empty;

        try
        {
            var root = JsonSerializer.Deserialize<SlmEventRoot>(json, JsonOptions);
            if (root is null)
            {
                error = "JSON root is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(root.EventText))
            {
                error = "event_text is empty.";
                return false;
            }

            if (root.Choices is null || root.Choices.Count < 2)
            {
                error = "choices must contain at least 2 entries.";
                return false;
            }

            var parsedChoices = new List<EventChoice>(root.Choices.Count);
            foreach (var sourceChoice in root.Choices)
            {
                parsedChoices.Add(ParseChoice(sourceChoice));
            }

            var timeoutChoice = SelectLowestRiskChoice(parsedChoices);

            var terrain = root.Terrain is null
                ? TerrainProfile.Default
                : new TerrainProfile(
                    root.Terrain.ObstacleDensity ?? TerrainProfile.Default.ObstacleDensity,
                    root.Terrain.ObstacleType ?? TerrainProfile.Default.ObstacleType,
                    root.Terrain.SpeedModifier ?? TerrainProfile.Default.SpeedModifier);

            eventData = new EventData(
                root.EventText,
                parsedChoices[0],
                parsedChoices[1],
                timeoutChoice,
                limitSeconds: 8.6f,
                terrain: terrain);

            return true;
        }
        catch (Exception ex)
        {
            error = $"JSON parse error: {ex.Message}";
            return false;
        }
    }

    private static EventChoice ParseChoice(SlmChoice source)
    {
        var successText = source.Success ?? "うまくいった。";
        var failText = source.Fail ?? "失敗した。";

        ParseEffect(successText, out var successDelta, out var successLifeDamage);
        ParseEffect(failText, out var failDelta, out var failLifeDamage);

        var check = string.IsNullOrWhiteSpace(source.Check) || source.Check == "null"
            ? null
            : source.Check;

        var difficulty = source.Difficulty ?? (check is null ? 0 : 12);

        return new EventChoice(
            source.Text ?? "行動する",
            check,
            difficulty,
            successDelta,
            failDelta,
            successLifeDamage,
            failLifeDamage,
            successText,
            failText);
    }

    private static EventChoice SelectLowestRiskChoice(IReadOnlyList<EventChoice> choices)
    {
        return choices
            .OrderBy(choice => choice.FailLifeDamage)
            .ThenBy(choice => Math.Abs(choice.FailDelta.Vitality)
                + Math.Abs(choice.FailDelta.Intelligence)
                + Math.Abs(choice.FailDelta.Charisma)
                + Math.Abs(choice.FailDelta.Luck)
                + Math.Abs(choice.FailDelta.Wealth))
            .First();
    }

    private static void ParseEffect(string text, out StatDelta delta, out float lifeDamage)
    {
        var vitality = 0;
        var intelligence = 0;
        var charisma = 0;
        var luck = 0;
        var wealth = 0;
        lifeDamage = 0f;

        foreach (Match match in EffectRegex.Matches(text))
        {
            var key = match.Groups["key"].Value;
            var sign = match.Groups["sign"].Value == "+" ? 1 : -1;
            var value = int.Parse(match.Groups["value"].Value);

            switch (key)
            {
                case "体力":
                    vitality += sign * value;
                    break;
                case "知力":
                    intelligence += sign * value;
                    break;
                case "魅力":
                    charisma += sign * value;
                    break;
                case "運":
                    luck += sign * value;
                    break;
                case "財力":
                    wealth += sign * value;
                    break;
                case "寿命" when sign < 0:
                    lifeDamage += value;
                    break;
            }
        }

        delta = new StatDelta(vitality, intelligence, charisma, luck, wealth);
    }

    [GeneratedRegex("(?<key>体力|知力|魅力|運|財力|寿命)\\s*(?<sign>[+-])\\s*(?<value>\\d+)", RegexOptions.Compiled)]
    private static partial Regex BuildEffectRegex();

    private sealed class SlmEventRoot
    {
        [JsonPropertyName("event_text")]
        public string? EventText { get; set; }

        [JsonPropertyName("choices")]
        public List<SlmChoice>? Choices { get; set; }

        [JsonPropertyName("terrain")]
        public SlmTerrain? Terrain { get; set; }
    }

    private sealed class SlmChoice
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("check")]
        public string? Check { get; set; }

        [JsonPropertyName("difficulty")]
        public int? Difficulty { get; set; }

        [JsonPropertyName("success")]
        public string? Success { get; set; }

        [JsonPropertyName("fail")]
        public string? Fail { get; set; }
    }

    private sealed class SlmTerrain
    {
        [JsonPropertyName("obstacle_density")]
        public float? ObstacleDensity { get; set; }

        [JsonPropertyName("obstacle_type")]
        public string? ObstacleType { get; set; }

        [JsonPropertyName("speed_modifier")]
        public float? SpeedModifier { get; set; }
    }
}
