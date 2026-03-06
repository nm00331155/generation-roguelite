using System.Collections.Generic;
using GenerationRoguelite.Core;
using GenerationRoguelite.Events;
using Godot;

namespace GenerationRoguelite.Action;

public sealed class ObstacleSpawner
{
    private const float Width = 108f;
    private const float Height = 108f;
    private const float MinSpawnInterval = 1.5f;
    private const float MaxSpawnInterval = 6.0f;
    private const float BaseSpawnInterval = 4.0f;
    private const float BaseSpeed = 220f;

    private readonly Node2D _container;
    private readonly List<ObstacleInstance> _obstacles = new();
    private readonly RandomNumberGenerator _rng = new();

    private readonly float _spawnX;
    private readonly float _groundY;

    private float _spawnTimer;
    private float _spawnSuppressionTimer;
    private float _baseDensity = TerrainProfile.Default.ObstacleDensity;
    private float _densityAdjustment;
    private float _terrainSpeedModifier = TerrainProfile.Default.SpeedModifier;
    private string _obstacleType = TerrainProfile.Default.ObstacleType;
    private float _ddaSpawnIntervalOffset;
    private float _ddaSpeedAdjustment;

    public ObstacleSpawner(Node2D container, float spawnX, float groundY)
    {
        _container = container;
        _spawnX = spawnX;
        _groundY = groundY;
        _rng.Randomize();
    }

    public void Reset()
    {
        ClearAll();
        _spawnTimer = 0f;
        _spawnSuppressionTimer = 0f;
        _baseDensity = TerrainProfile.Default.ObstacleDensity;
        _densityAdjustment = 0f;
        _terrainSpeedModifier = TerrainProfile.Default.SpeedModifier;
        _obstacleType = TerrainProfile.Default.ObstacleType;
    }

    public void SetTerrain(TerrainProfile terrain, float densityAdjustment)
    {
        _baseDensity = terrain.ObstacleDensity;
        _densityAdjustment = densityAdjustment;
        _terrainSpeedModifier = terrain.SpeedModifier;
        _obstacleType = terrain.ObstacleType;
    }

    public void Tick(double delta, LifePhase phase)
    {
        _spawnSuppressionTimer -= (float)delta;
        if (_spawnSuppressionTimer < 0f)
        {
            _spawnSuppressionTimer = 0f;
        }

        _spawnTimer -= (float)delta;
        if (phase != LifePhase.Childhood
            && _spawnSuppressionTimer <= 0f
            && _spawnTimer <= 0f)
        {
            SpawnObstacle(phase);
            var effectiveDensity = Mathf.Clamp(_baseDensity + _densityAdjustment, 0.25f, 1.4f);
            var interval = (BaseSpawnInterval + _ddaSpawnIntervalOffset) / Mathf.Clamp(effectiveDensity, 0.8f, 1.2f);
            _spawnTimer = Mathf.Clamp(interval, MinSpawnInterval, MaxSpawnInterval);
        }

        var speed = Mathf.Clamp(
            (BaseSpeed + _ddaSpeedAdjustment) * _terrainSpeedModifier * PhaseManager.ObstacleSpeedMultiplier(phase),
            150f,
            400f);
        for (var i = _obstacles.Count - 1; i >= 0; i--)
        {
            var obstacle = _obstacles[i];
            obstacle.Node.Position += Vector2.Left * speed * (float)delta;

            if (obstacle.Node.Position.X < -200f)
            {
                obstacle.Node.QueueFree();
                _obstacles.RemoveAt(i);
            }
        }
    }

    public void SetSpawnSuppression(float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        _spawnSuppressionTimer = Mathf.Max(_spawnSuppressionTimer, seconds);
    }

    public void SetDdaAdjustments(float spawnIntervalOffset, float speedAdjustment)
    {
        _ddaSpawnIntervalOffset = spawnIntervalOffset;
        _ddaSpeedAdjustment = speedAdjustment;
    }

    public bool HasApproachingCluster(float playerX, int requiredCount = 3, float forwardDistance = 560f)
    {
        var targetCount = Mathf.Max(1, requiredCount);
        var maxX = playerX + Mathf.Max(80f, forwardDistance);

        var count = 0;
        for (var i = 0; i < _obstacles.Count; i++)
        {
            var obstacle = _obstacles[i];
            if (obstacle.Resolved)
            {
                continue;
            }

            var obstacleX = obstacle.Node.Position.X;
            if (obstacleX <= playerX || obstacleX > maxX)
            {
                continue;
            }

            count++;
            if (count >= targetCount)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryDestroyFrontEnemy(float playerFrontX, float attackRange, out AttackReward reward)
    {
        reward = default;

        var minX = playerFrontX - 8f;
        var maxX = playerFrontX + attackRange;
        var bestIndex = -1;
        var bestX = float.MaxValue;

        for (var i = 0; i < _obstacles.Count; i++)
        {
            var obstacle = _obstacles[i];
            if (obstacle.Resolved)
            {
                continue;
            }

            var x = obstacle.Node.Position.X;
            if (x < minX || x > maxX || x >= bestX)
            {
                continue;
            }

            bestIndex = i;
            bestX = x;
        }

        if (bestIndex < 0)
        {
            return false;
        }

        var target = _obstacles[bestIndex];
        if (!string.Equals(target.ObstacleType, "enemy", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        target.Node.QueueFree();
        _obstacles.RemoveAt(bestIndex);

        var wealthGain = _rng.Randf() <= 0.75f ? (int)_rng.RandiRange(1, 3) : 0;
        var dropText = wealthGain > 0 ? $"ゴールド+{wealthGain}" : "消耗品の欠片";
        reward = new AttackReward(wealthGain, dropText);
        return true;
    }

    public ObstacleInteractionResult ResolvePlayerInteractions(Rect2 playerRect, bool hasAvoidWindow)
    {
        var hits = 0;
        var avoided = 0;
        for (var i = _obstacles.Count - 1; i >= 0; i--)
        {
            var obstacle = _obstacles[i];
            if (obstacle.Resolved)
            {
                continue;
            }

            var obstacleRect = obstacle.GetRect();
            if (obstacleRect.Position.X > playerRect.End.X)
            {
                continue;
            }

            obstacle.Resolved = true;
            if (!hasAvoidWindow && obstacleRect.Intersects(playerRect))
            {
                hits += 1;
            }
            else
            {
                avoided += 1;
            }

            obstacle.Node.QueueFree();
            _obstacles.RemoveAt(i);
        }

        return new ObstacleInteractionResult(hits, avoided);
    }

    public void ClearAll()
    {
        foreach (var obstacle in _obstacles)
        {
            obstacle.Node.QueueFree();
        }

        _obstacles.Clear();
    }

    private void SpawnObstacle(LifePhase phase)
    {
        var obstacleRoot = new Node2D
        {
            Position = new Vector2(_spawnX, _groundY),
        };

        var visual = new Polygon2D
        {
            Color = ColorByContext(phase, _obstacleType),
            Polygon =
            [
                new Vector2(-Width * 0.5f, -Height),
                new Vector2(Width * 0.5f, -Height),
                new Vector2(Width * 0.5f, 0f),
                new Vector2(-Width * 0.5f, 0f),
            ]
        };

        obstacleRoot.AddChild(visual);
        _container.AddChild(obstacleRoot);
        _obstacles.Add(new ObstacleInstance(obstacleRoot, _obstacleType));
    }

    private static Color ColorByContext(LifePhase phase, string obstacleType)
    {
        if (obstacleType == "enemy")
        {
            return new Color(0.94f, 0.39f, 0.34f);
        }

        if (obstacleType == "trap")
        {
            return new Color(0.91f, 0.54f, 0.19f);
        }

        if (obstacleType == "hazard")
        {
            return new Color(0.73f, 0.57f, 0.92f);
        }

        return phase switch
        {
            LifePhase.Childhood => new Color(0.45f, 0.87f, 0.74f),
            LifePhase.Youth => new Color(0.96f, 0.72f, 0.31f),
            LifePhase.Midlife => new Color(0.94f, 0.49f, 0.43f),
            LifePhase.Elderly => new Color(0.76f, 0.63f, 0.92f),
            _ => Colors.White,
        };
    }

    private sealed class ObstacleInstance
    {
        public Node2D Node { get; }
        public string ObstacleType { get; }
        public bool Resolved { get; set; }

        public ObstacleInstance(Node2D node, string obstacleType)
        {
            Node = node;
            ObstacleType = obstacleType;
        }

        public Rect2 GetRect()
        {
            return new Rect2(
                Node.Position.X - Width * 0.5f,
                Node.Position.Y - Height,
                Width,
                Height);
        }
    }
}

public readonly record struct ObstacleInteractionResult(int Hits, int Avoided);

public readonly record struct AttackReward(int WealthGain, string DropText);
