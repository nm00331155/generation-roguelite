using System;
using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Expansion;

public sealed record SocialRankEntry(int Generation, int Score, string Founder);

public sealed class SocialState
{
    public int TotalSharedLegends { get; set; }

    public List<string> PendingLegends { get; set; } = [];

    public List<SocialRankEntryState> Ranking { get; set; } = [];

    public List<SocialBondOfferState> PendingBondOffers { get; set; } = [];
}

public sealed class SocialRankEntryState
{
    public int Generation { get; set; }

    public int Score { get; set; }

    public string Founder { get; set; } = string.Empty;
}

public sealed class SocialBondOfferState
{
    public int Generation { get; set; }

    public string Founder { get; set; } = string.Empty;

    public int Rank { get; set; }

    public int VitalityBonus { get; set; }

    public int LuckBonus { get; set; }

    public int WealthBonus { get; set; }
}

public sealed class AsyncSocialManager
{
    private const int RankingLimit = 10;
    private const int OfferLimit = 8;

    private readonly List<SocialRankEntry> _ranking = [];
    private readonly Queue<string> _legendQueue = [];
    private readonly Queue<SocialBondOfferState> _bondOfferQueue = [];

    private float _flushCooldown = 12f;

    public int TotalSharedLegends { get; private set; }

    public IReadOnlyList<SocialRankEntry> Ranking => _ranking;

    public string Tick(double delta)
    {
        _flushCooldown -= (float)delta;
        if (_flushCooldown > 0f || _legendQueue.Count == 0)
        {
            return string.Empty;
        }

        _flushCooldown = 12f;
        _legendQueue.Dequeue();
        TotalSharedLegends += 1;
        return "非同期共有: 家系の伝説をランキングサーバへ送信完了";
    }

    public void SubmitGenerationScore(int generation, int score, string founder, string? legend)
    {
        var normalizedFounder = string.IsNullOrWhiteSpace(founder) ? "無名" : founder;
        _ranking.Add(new SocialRankEntry(generation, score, normalizedFounder));
        _ranking.Sort((left, right) => right.Score.CompareTo(left.Score));

        var rank = ResolveSubmittedRank(generation, score, normalizedFounder);
        if (rank >= 1 && rank <= 5)
        {
            _bondOfferQueue.Enqueue(BuildBondOffer(generation, normalizedFounder, rank));
            while (_bondOfferQueue.Count > OfferLimit)
            {
                _bondOfferQueue.Dequeue();
            }
        }

        if (_ranking.Count > RankingLimit)
        {
            _ranking.RemoveRange(RankingLimit, _ranking.Count - RankingLimit);
        }

        if (!string.IsNullOrWhiteSpace(legend))
        {
            _legendQueue.Enqueue(legend);
            while (_legendQueue.Count > 24)
            {
                _legendQueue.Dequeue();
            }
        }
    }

    public bool TryConsumeBondOffer(out StatBonus bonus, out string message)
    {
        if (_bondOfferQueue.Count == 0)
        {
            bonus = StatBonus.Zero;
            message = string.Empty;
            return false;
        }

        var offer = _bondOfferQueue.Dequeue();
        bonus = new StatBonus(
            offer.VitalityBonus,
            0,
            0,
            offer.LuckBonus,
            offer.WealthBonus);

        var bonusParts = new List<string>();
        if (offer.VitalityBonus > 0)
        {
            bonusParts.Add($"体力+{offer.VitalityBonus}");
        }

        if (offer.LuckBonus > 0)
        {
            bonusParts.Add($"運+{offer.LuckBonus}");
        }

        if (offer.WealthBonus > 0)
        {
            bonusParts.Add($"財力+{offer.WealthBonus}");
        }

        var bonusText = bonusParts.Count == 0 ? "補正なし" : string.Join(" / ", bonusParts);
        message = $"非同期縁談: {offer.Founder}家(順位{offer.Rank})から支援到着 ({bonusText})";
        return true;
    }

    public string BuildSummary()
    {
        var best = _ranking.Count == 0
            ? "なし"
            : $"{_ranking[0].Score}点(G{_ranking[0].Generation})";

        return $"非同期: Top={best} / 送信済み{TotalSharedLegends} / 伝説待機{_legendQueue.Count} / 縁談待機{_bondOfferQueue.Count}";
    }

    public SocialState BuildState()
    {
        return new SocialState
        {
            TotalSharedLegends = TotalSharedLegends,
            PendingLegends = [.. _legendQueue],
            Ranking = _ranking
                .Select(entry => new SocialRankEntryState
                {
                    Generation = entry.Generation,
                    Score = entry.Score,
                    Founder = entry.Founder,
                })
                .ToList(),
            PendingBondOffers = _bondOfferQueue
                .Select(offer => new SocialBondOfferState
                {
                    Generation = offer.Generation,
                    Founder = offer.Founder,
                    Rank = offer.Rank,
                    VitalityBonus = offer.VitalityBonus,
                    LuckBonus = offer.LuckBonus,
                    WealthBonus = offer.WealthBonus,
                })
                .ToList(),
        };
    }

    public void LoadState(SocialState? state)
    {
        _ranking.Clear();
        _legendQueue.Clear();
        _bondOfferQueue.Clear();
        TotalSharedLegends = state?.TotalSharedLegends ?? 0;

        if (state is null)
        {
            return;
        }

        foreach (var rank in state.Ranking ?? [])
        {
            if (rank is null || string.IsNullOrWhiteSpace(rank.Founder))
            {
                continue;
            }

            _ranking.Add(new SocialRankEntry(rank.Generation, rank.Score, rank.Founder));
        }

        _ranking.Sort((left, right) => right.Score.CompareTo(left.Score));
        if (_ranking.Count > RankingLimit)
        {
            _ranking.RemoveRange(RankingLimit, _ranking.Count - RankingLimit);
        }

        foreach (var legend in (state.PendingLegends ?? []).Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            _legendQueue.Enqueue(legend);
        }

        foreach (var offer in state.PendingBondOffers ?? [])
        {
            if (offer is null)
            {
                continue;
            }

            _bondOfferQueue.Enqueue(new SocialBondOfferState
            {
                Generation = offer.Generation,
                Founder = offer.Founder,
                Rank = offer.Rank,
                VitalityBonus = offer.VitalityBonus,
                LuckBonus = offer.LuckBonus,
                WealthBonus = offer.WealthBonus,
            });
        }

        while (_bondOfferQueue.Count > OfferLimit)
        {
            _bondOfferQueue.Dequeue();
        }
    }

    private int ResolveSubmittedRank(int generation, int score, string founder)
    {
        for (var i = 0; i < _ranking.Count; i++)
        {
            var entry = _ranking[i];
            if (entry.Generation == generation
                && entry.Score == score
                && entry.Founder == founder)
            {
                return i + 1;
            }
        }

        return -1;
    }

    private static SocialBondOfferState BuildBondOffer(int generation, string founder, int rank)
    {
        return rank switch
        {
            1 => new SocialBondOfferState
            {
                Generation = generation,
                Founder = founder,
                Rank = rank,
                VitalityBonus = 2,
                LuckBonus = 2,
                WealthBonus = 6,
            },
            2 => new SocialBondOfferState
            {
                Generation = generation,
                Founder = founder,
                Rank = rank,
                VitalityBonus = 1,
                LuckBonus = 1,
                WealthBonus = 4,
            },
            3 => new SocialBondOfferState
            {
                Generation = generation,
                Founder = founder,
                Rank = rank,
                VitalityBonus = 1,
                LuckBonus = 1,
                WealthBonus = 3,
            },
            4 => new SocialBondOfferState
            {
                Generation = generation,
                Founder = founder,
                Rank = rank,
                VitalityBonus = 0,
                LuckBonus = 1,
                WealthBonus = 2,
            },
            _ => new SocialBondOfferState
            {
                Generation = generation,
                Founder = founder,
                Rank = rank,
                VitalityBonus = 0,
                LuckBonus = 0,
                WealthBonus = 1,
            },
        };
    }
}
