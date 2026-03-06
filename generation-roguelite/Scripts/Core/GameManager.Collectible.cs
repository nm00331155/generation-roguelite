using System.Collections.Generic;
using GenerationRoguelite.Data;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private bool TryCollectibleTouch(Vector2 inputPosition, out string result)
    {
        result = string.Empty;
        if (_collectibles.Count == 0)
        {
            return false;
        }

        var selectByInput = !inputPosition.IsEqualApprox(Vector2.Zero);
        var targetPosition = selectByInput ? inputPosition : _player.Position;
        var maxDistance = selectByInput ? CollectiblePickupDistance : 260f;

        var bestIndex = -1;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < _collectibles.Count; i++)
        {
            var collectible = _collectibles[i];
            var distance = collectible.Node.Position.DistanceTo(targetPosition);
            if (distance > maxDistance || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestIndex = i;
        }

        if (bestIndex < 0)
        {
            return false;
        }

        var picked = _collectibles[bestIndex];
        _character.Stats.ApplyBonus(picked.Reward.Bonus);
        _generationScore += picked.Reward.ScoreGain;
        result = $"好奇心タッチ: {picked.Reward.Name} ({picked.Reward.BonusText})";
        RecordGenerationEvent(result, true);
        RemoveCollectibleAt(bestIndex);
        return true;
    }

    private void TickCollectibles(double delta)
    {
        if (_phaseManager.CurrentPhase != LifePhase.Childhood)
        {
            if (_collectibles.Count > 0)
            {
                ClearCollectibles();
            }

            return;
        }

        _collectibleSpawnCooldown -= (float)delta;
        if (_collectibleSpawnCooldown <= 0f)
        {
            SpawnCollectible();
            _collectibleSpawnCooldown = _rng.RandfRange(CollectibleSpawnMinSeconds, CollectibleSpawnMaxSeconds);
        }

        for (var i = _collectibles.Count - 1; i >= 0; i--)
        {
            var collectible = _collectibles[i];
            collectible.RemainingSeconds -= (float)delta;
            var alpha = Mathf.Clamp(collectible.RemainingSeconds / CollectibleLifetimeSeconds, 0.2f, 1f);
            collectible.Visual.Modulate = new Color(1f, 1f, 1f, alpha);

            if (collectible.RemainingSeconds <= 0f)
            {
                RemoveCollectibleAt(i);
            }
        }
    }

    private void SpawnCollectible()
    {
        var reward = RollCollectibleReward();
        var collectibleRoot = new Node2D
        {
            Position = new Vector2(
                _rng.RandfRange(360f, 1180f),
                _rng.RandfRange(220f, 470f)),
        };

        var visual = new Polygon2D
        {
            Color = new Color(0.93f, 0.89f, 0.42f),
            Polygon =
            [
                new Vector2(0f, -16f),
                new Vector2(14f, 0f),
                new Vector2(0f, 16f),
                new Vector2(-14f, 0f),
            ]
        };

        collectibleRoot.AddChild(visual);
        _collectiblesRoot.AddChild(collectibleRoot);
        _collectibles.Add(new CollectibleInstance(collectibleRoot, visual, CollectibleLifetimeSeconds, reward));
    }

    private CollectibleReward RollCollectibleReward()
    {
        return ((int)_rng.RandiRange(0, 5)) switch
        {
            0 => new CollectibleReward("木彫りのお守り", "体力+1", new StatBonus(1, 0, 0, 0, 0), 2),
            1 => new CollectibleReward("古い知恵袋", "知力+1", new StatBonus(0, 1, 0, 0, 0), 2),
            2 => new CollectibleReward("きらめく羽", "魅力+1", new StatBonus(0, 0, 1, 0, 0), 2),
            3 => new CollectibleReward("四つ葉の札", "運+1", new StatBonus(0, 0, 0, 1, 0), 2),
            4 => new CollectibleReward("銅貨の束", "財力+2", new StatBonus(0, 0, 0, 0, 2), 2),
            _ => new CollectibleReward("古びた地図", "伏線アイテム", StatBonus.Zero, 1),
        };
    }

    private void RemoveCollectibleAt(int index)
    {
        if (index < 0 || index >= _collectibles.Count)
        {
            return;
        }

        _collectibles[index].Node.QueueFree();
        _collectibles.RemoveAt(index);
    }

    private void ClearCollectibles()
    {
        for (var i = _collectibles.Count - 1; i >= 0; i--)
        {
            _collectibles[i].Node.QueueFree();
        }

        _collectibles.Clear();
    }

    private sealed class CollectibleInstance
    {
        public Node2D Node { get; }
        public Polygon2D Visual { get; }
        public CollectibleReward Reward { get; }
        public float RemainingSeconds { get; set; }

        public CollectibleInstance(Node2D node, Polygon2D visual, float remainingSeconds, CollectibleReward reward)
        {
            Node = node;
            Visual = visual;
            RemainingSeconds = remainingSeconds;
            Reward = reward;
        }
    }

    private readonly record struct CollectibleReward(
        string Name,
        string BonusText,
        StatBonus Bonus,
        int ScoreGain);
}
