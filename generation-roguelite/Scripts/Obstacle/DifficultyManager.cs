using System.Collections.Generic;
using GenerationRoguelite.Core;
using Godot;

namespace GenerationRoguelite.Obstacle;

public sealed class DifficultyManager
{
    private readonly Queue<ResultSample> _samples = new();

    private float _elapsedSeconds;
    private float _evaluationTimer;

    public float SpawnInterval { get; private set; } = 4f;

    public float ObstacleSpeed { get; private set; } = 300f;

    public void Tick(double delta, LifePhase phase)
    {
        _elapsedSeconds += (float)delta;
        _evaluationTimer += (float)delta;

        TrimSamples();

        if (_evaluationTimer < 5f)
        {
            return;
        }

        _evaluationTimer = 0f;
        Evaluate(phase);
    }

    public void RegisterAvoid()
    {
        _samples.Enqueue(new ResultSample(_elapsedSeconds, true));
    }

    public void RegisterHit()
    {
        _samples.Enqueue(new ResultSample(_elapsedSeconds, false));
    }

    public float GetDamageScale(LifePhase phase)
    {
        return phase switch
        {
            LifePhase.Childhood => 0f,
            LifePhase.Youth => 1f,
            LifePhase.Midlife => 1.5f,
            LifePhase.Elderly => 3f,
            _ => 1f,
        };
    }

    private void Evaluate(LifePhase phase)
    {
        if (phase == LifePhase.Childhood)
        {
            SpawnInterval = 999f;
            ObstacleSpeed = 300f;
            return;
        }

        var successCount = 0;
        var total = _samples.Count;
        foreach (var sample in _samples)
        {
            if (sample.Success)
            {
                successCount += 1;
            }
        }

        var ratio = total == 0 ? 0.5f : (float)successCount / total;

        if (ratio > 0.85f)
        {
            SpawnInterval = Mathf.Max(1.5f, SpawnInterval - 0.3f);
            ObstacleSpeed = Mathf.Min(500f, ObstacleSpeed + 20f);
        }
        else if (ratio < 0.4f)
        {
            SpawnInterval = Mathf.Min(6f, SpawnInterval + 0.3f);
            ObstacleSpeed = Mathf.Max(200f, ObstacleSpeed - 20f);
        }

        if (phase == LifePhase.Midlife)
        {
            ObstacleSpeed *= 1.2f;
        }
        else if (phase == LifePhase.Elderly)
        {
            ObstacleSpeed *= 0.8f;
        }
    }

    private void TrimSamples()
    {
        while (_samples.Count > 0 && _elapsedSeconds - _samples.Peek().Timestamp > 30f)
        {
            _samples.Dequeue();
        }
    }

    private readonly record struct ResultSample(float Timestamp, bool Success);
}
