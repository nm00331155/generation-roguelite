using System;
using System.IO;
using System.Text.Json;
using GenerationRoguelite.Expansion;
using GenerationRoguelite.Monetization;
using Godot;

namespace GenerationRoguelite.Data;

public sealed class GameSaveData
{
    public int Version { get; set; } = 3;

    public int TotalScore { get; set; }

    public int NextGeneration { get; set; } = 1;

    public string ActiveNavigatorProfile { get; set; } = "default";

    public string LegendText { get; set; } = string.Empty;

    public bool AdoptionUsed { get; set; }

    public bool LastResortUsed { get; set; }

    public float GameSpeed { get; set; } = 1f;

    public float BgmVolume { get; set; } = 1f;

    public float SeVolume { get; set; } = 1f;

    public float VoiceVolume { get; set; } = 1f;

    public bool DebugOverlayEnabled { get; set; } = true;

    public DateTime LastSaveTime { get; set; } = DateTime.UtcNow;

    public IapState Iap { get; set; } = new();

    public BattlePassState BattlePass { get; set; } = new();

    public CosmeticState Cosmetic { get; set; } = new();

    public SocialState Social { get; set; } = new();
}

public sealed class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _savePath;

    public SaveManager(string savePath = "user://save_profile.json")
    {
        _savePath = savePath;
    }

    public bool TryLoad(out GameSaveData data, out string message)
    {
        data = new GameSaveData();

        var path = ResolveAbsolutePath(_savePath);
        if (!File.Exists(path))
        {
            message = "save not found";
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            data = JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions) ?? new GameSaveData();
            message = "save loaded";
            return true;
        }
        catch (Exception ex)
        {
            message = $"save load error: {ex.Message}";
            return false;
        }
    }

    public bool TryLoadGameData(out GameData data, out string message)
    {
        data = new GameData();

        if (!TryLoad(out GameSaveData raw, out message))
        {
            return false;
        }

        data = GameData.FromSaveData(raw);
        return true;
    }

    public bool TrySave(GameSaveData data, out string message)
    {
        var path = ResolveAbsolutePath(_savePath);

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
            message = "save written";
            return true;
        }
        catch (Exception ex)
        {
            message = $"save write error: {ex.Message}";
            return false;
        }
    }

    public bool TrySaveGameData(GameData data, out string message)
    {
        return TrySave(data.ToSaveData(), out message);
    }

    private static string ResolveAbsolutePath(string path)
    {
        return path.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(path)
            : path;
    }
}
