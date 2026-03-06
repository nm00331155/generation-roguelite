using System.Collections.Generic;
using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Obstacle;

public partial class ObstacleSpawner : Node2D
{
    [Export]
    public PackedScene? ObstacleScene { get; set; }

    [Export]
    public NodePath ContainerPath { get; set; } = ".";

    [Export]
    public float SpawnX { get; set; } = 1960f;

    [Export]
    public float SpawnY { get; set; } = 530f;

    private readonly DifficultyManager _difficulty = new();
    private readonly List<ObstacleController> _obstacles = new();
    private readonly RandomNumberGenerator _rng = new();

    private Node2D _container = null!;
    private LifePhase _currentPhase = LifePhase.Childhood;
    private float _spawnTimer;

    public override void _Ready()
    {
        _rng.Randomize();
        _container = GetNode<Node2D>(ContainerPath);
    }

    public override void _Process(double delta)
    {
        _difficulty.Tick(delta, _currentPhase);

        _spawnTimer -= (float)delta;
        if (_spawnTimer <= 0f)
        {
            TrySpawn();
            _spawnTimer = _difficulty.SpawnInterval;
        }

        CleanupReleasedObstacles();
    }

    public void SetPhase(LifePhase phase)
    {
        _currentPhase = phase;
        if (_currentPhase == LifePhase.Childhood)
        {
            _spawnTimer = 0f;
        }
    }

    public float DamageScale => _difficulty.GetDamageScale(_currentPhase);

    public void RegisterAvoid()
    {
        _difficulty.RegisterAvoid();
    }

    public void RegisterHit()
    {
        _difficulty.RegisterHit();
    }

    public IReadOnlyList<ObstacleController> ActiveObstacles => _obstacles;

    private void TrySpawn()
    {
        if (_currentPhase == LifePhase.Childhood || ObstacleScene is null)
        {
            return;
        }

        if (ObstacleScene.Instantiate() is not ObstacleController obstacle)
        {
            return;
        }

        obstacle.Position = new Vector2(SpawnX, SpawnY);
        obstacle.Speed = _difficulty.ObstacleSpeed;

        var type = _rng.Randf() switch
        {
            < 0.6f => "enemy",
            < 0.85f => "trap",
            _ => "falling",
        };
        obstacle.ConfigureType(type);

        _container.AddChild(obstacle);
        _obstacles.Add(obstacle);
    }

    private void CleanupReleasedObstacles()
    {
        for (var i = _obstacles.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_obstacles[i]))
            {
                _obstacles.RemoveAt(i);
            }
        }
    }
}
