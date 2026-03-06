using System;
using Godot;

namespace GenerationRoguelite.Expansion;

public sealed class InflationBalancer
{
    private static readonly ScaleBand[] ScaleBands =
    [
        new("原始", 1, 50, 1_000d, "千", 36_000f, 0.75f, 0.35f, 2.6f),
        new("古代", 10, 100, 10_000d, "万", 92_000f, 0.85f, 0.42f, 3.2f),
        new("中世", 50, 500, 100_000d, "十万", 210_000f, 0.95f, 0.5f, 4.1f),
        new("近世", 100, 1_000, 1_000_000d, "百万", 440_000f, 1.05f, 0.58f, 5.0f),
        new("現代", 500, 5_000, 100_000_000d, "億", 920_000f, 1.15f, 0.7f, 6.3f),
        new("未来", 5_000, 50_000, 1_000_000_000_000d, "兆", 1_850_000f, 1.25f, 0.85f, 7.8f),
    ];

    public float GetScoreMultiplier(int generation, int totalScore, float eraMultiplier, WorldExpansionSnapshot expansion)
    {
        var band = ResolveBand(generation);
        var generationFactor = 1f + Mathf.Clamp((generation - 1) * 0.0225f, 0f, 1.35f);
        var scoreFactor = 1f + Mathf.Clamp(totalScore / band.ScorePivot, 0f, band.ScoreFactorCap);
        var combined = eraMultiplier * generationFactor * scoreFactor * expansion.InflationFactor;
        return Mathf.Clamp(combined, band.MinMultiplier, band.MaxMultiplier);
    }

    public string BuildSummary(int generation, int totalScore, float eraMultiplier, WorldExpansionSnapshot expansion)
    {
        var band = ResolveBand(generation);
        var value = GetScoreMultiplier(generation, totalScore, eraMultiplier, expansion);
        return $"インフレ係数 x{value:F2} ({band.EraName}/{band.ScoreUnitLabel}スケール)";
    }

    public string FormatTotalScore(int totalScore, int generation)
    {
        if (totalScore <= 0)
        {
            return "0";
        }

        var band = ResolveBand(generation);
        var scaled = totalScore / band.ScoreUnitValue;
        if (scaled < 0.01d)
        {
            return totalScore.ToString();
        }

        return $"{scaled:F2}{band.ScoreUnitLabel}";
    }

    private static ScaleBand ResolveBand(int generation)
    {
        var index = Math.Clamp((generation - 1) / 3, 0, ScaleBands.Length - 1);
        return ScaleBands[index];
    }

    private readonly record struct ScaleBand(
        string EraName,
        int StatRangeMin,
        int StatRangeMax,
        double ScoreUnitValue,
        string ScoreUnitLabel,
        float ScorePivot,
        float MinMultiplier,
        float ScoreFactorCap,
        float MaxMultiplier);
}
