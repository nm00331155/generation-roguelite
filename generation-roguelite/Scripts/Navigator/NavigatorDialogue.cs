using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace GenerationRoguelite.Navigator;

public static class NavigatorDialogue
{
    private const string DialoguePath = "res://Resources/Navigator/default_dialogues.json";

    public static bool TryLoadDefault(out NavigatorDialogueSet set, out string error)
    {
        set = NavigatorDialogueSet.Empty;
        error = string.Empty;

        try
        {
            var path = ProjectSettings.GlobalizePath(DialoguePath);
            if (!File.Exists(path))
            {
                error = "default_dialogues.json が存在しません";
                return false;
            }

            var raw = File.ReadAllText(path);
            var source = JsonSerializer.Deserialize<NavigatorDialogueSource>(raw);
            if (source is null)
            {
                error = "JSONパースに失敗しました";
                return false;
            }

            set = new NavigatorDialogueSet(
                Convert(source.EventStart, "navi_event_start"),
                Convert(source.EventSuccess, "navi_event_success"),
                Convert(source.EventFail, "navi_event_fail"),
                Convert(source.PhaseYouth, "navi_phase_youth"),
                Convert(source.PhaseAdult, "navi_phase_adult"),
                Convert(source.PhaseElder, "navi_phase_elder"),
                Convert(source.Random, "navi_random"),
                Convert(source.Warning, "navi_warning"),
                Convert(source.Death, "navi_death"),
                Convert(source.Generation, "navi_generation"),
                Convert(source.GameOver, "navi_gameover"));

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static DialogueData[] Convert(string[]? lines, string prefix)
    {
        if (lines is null || lines.Length == 0)
        {
            return Array.Empty<DialogueData>();
        }

        var result = new DialogueData[lines.Length];
        var count = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var text = lines[i]?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            result[count] = new DialogueData(text, $"{prefix}_{count + 1:00}");
            count++;
        }

        if (count == result.Length)
        {
            return result;
        }

        var compacted = new DialogueData[count];
        Array.Copy(result, compacted, count);
        return compacted;
    }

    private sealed class NavigatorDialogueSource
    {
        [JsonPropertyName("event_start")]
        public string[]? EventStart { get; set; }

        [JsonPropertyName("event_success")]
        public string[]? EventSuccess { get; set; }

        [JsonPropertyName("event_fail")]
        public string[]? EventFail { get; set; }

        [JsonPropertyName("phase_youth")]
        public string[]? PhaseYouth { get; set; }

        [JsonPropertyName("phase_adult")]
        public string[]? PhaseAdult { get; set; }

        [JsonPropertyName("phase_elder")]
        public string[]? PhaseElder { get; set; }

        [JsonPropertyName("random")]
        public string[]? Random { get; set; }

        [JsonPropertyName("warning")]
        public string[]? Warning { get; set; }

        [JsonPropertyName("death")]
        public string[]? Death { get; set; }

        [JsonPropertyName("generation")]
        public string[]? Generation { get; set; }

        [JsonPropertyName("gameover")]
        public string[]? GameOver { get; set; }
    }
}

public readonly record struct NavigatorDialogueSet(
    DialogueData[] EventStartLines,
    DialogueData[] EventSuccessLines,
    DialogueData[] EventFailLines,
    DialogueData[] PhaseYouthLines,
    DialogueData[] PhaseAdultLines,
    DialogueData[] PhaseElderLines,
    DialogueData[] RandomLines,
    DialogueData[] WarningLines,
    DialogueData[] DeathLines,
    DialogueData[] GenerationLines,
    DialogueData[] GameOverLines)
{
    public static NavigatorDialogueSet Empty => new(
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>(),
        Array.Empty<DialogueData>());
}
