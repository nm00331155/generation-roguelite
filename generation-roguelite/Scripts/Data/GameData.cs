using System;
using GenerationRoguelite.Expansion;
using GenerationRoguelite.Monetization;

namespace GenerationRoguelite.Data;

public sealed class GameData
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

    public static GameData FromSaveData(GameSaveData save)
    {
        return new GameData
        {
            Version = save.Version,
            TotalScore = save.TotalScore,
            NextGeneration = save.NextGeneration,
            ActiveNavigatorProfile = save.ActiveNavigatorProfile,
            LegendText = save.LegendText,
            AdoptionUsed = save.AdoptionUsed,
            LastResortUsed = save.LastResortUsed,
            GameSpeed = save.GameSpeed,
            BgmVolume = save.BgmVolume,
            SeVolume = save.SeVolume,
            VoiceVolume = save.VoiceVolume,
            DebugOverlayEnabled = save.DebugOverlayEnabled,
            LastSaveTime = save.LastSaveTime,
            Iap = save.Iap,
            BattlePass = save.BattlePass,
            Cosmetic = save.Cosmetic,
            Social = save.Social,
        };
    }

    public GameSaveData ToSaveData()
    {
        return new GameSaveData
        {
            Version = Version,
            TotalScore = TotalScore,
            NextGeneration = NextGeneration,
            ActiveNavigatorProfile = ActiveNavigatorProfile,
            LegendText = LegendText,
            AdoptionUsed = AdoptionUsed,
            LastResortUsed = LastResortUsed,
            GameSpeed = GameSpeed,
            BgmVolume = BgmVolume,
            SeVolume = SeVolume,
            VoiceVolume = VoiceVolume,
            DebugOverlayEnabled = DebugOverlayEnabled,
            LastSaveTime = LastSaveTime,
            Iap = Iap,
            BattlePass = BattlePass,
            Cosmetic = Cosmetic,
            Social = Social,
        };
    }
}
