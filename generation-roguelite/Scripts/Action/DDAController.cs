using System.Collections.Generic;
using Godot;

namespace GenerationRoguelite.Action;

public sealed class DDAController
{
    private const float WindowSeconds = 30f;
    private const float EvaluationIntervalSeconds = 5f;

    private readonly Queue<Sample> _samples = new();
    private float _elapsed;
    private float _evaluationTimer;
    private float _densityAdjustment;
    private float _spawnIntervalOffset;
    private float _speedAdjustment;

    public float SuccessRate
    {
        get
        {
            if (_samples.Count == 0)
            {
                return 0.5f;
            }

            var success = 0;
            foreach (var sample in _samples)
            {
                if (sample.Success)
                {
                    success += 1;
                }
            }

            return (float)success / _samples.Count;
        }
    }

    public void Tick(double delta)
    {
        _elapsed += (float)delta;
        _evaluationTimer += (float)delta;
        TrimOldSamples();

        if (_evaluationTimer >= EvaluationIntervalSeconds)
        {
            _evaluationTimer = 0f;
            EvaluateAdjustments();
        }
    }

    public void RegisterResult(bool success)
    {
        _samples.Enqueue(new Sample(_elapsed, success));
        TrimOldSamples();
    }

    public float GetDensityAdjustment()
    {
        return _densityAdjustment;
    }

    public float GetSpawnIntervalOffset()
    {
        return _spawnIntervalOffset;
    }

    public float GetSpeedAdjustment()
    {
        return _speedAdjustment;
    }

    public void Reset()
    {
        _samples.Clear();
        _elapsed = 0f;
        _evaluationTimer = 0f;
        _densityAdjustment = 0f;
        _spawnIntervalOffset = 0f;
        _speedAdjustment = 0f;
    }

    private void EvaluateAdjustments()
    {
        if (_samples.Count < 5)
        {
            _densityAdjustment = 0f;
            _spawnIntervalOffset = 0f;
            _speedAdjustment = 0f;
            return;
        }

        var rate = SuccessRate;
        if (rate > 0.85f)
        {
            _densityAdjustment = 0.15f;
            _spawnIntervalOffset = -0.3f;
            _speedAdjustment = 15f;
            return;
        }

        if (rate < 0.4f)
        {
            _densityAdjustment = -0.15f;
            _spawnIntervalOffset = 0.3f;
            _speedAdjustment = -15f;
            return;
        }

        _densityAdjustment = 0f;
        _spawnIntervalOffset = 0f;
        _speedAdjustment = 0f;
    }

    private void TrimOldSamples()
    {
        while (_samples.Count > 0 && _elapsed - _samples.Peek().Timestamp > WindowSeconds)
        {
            _samples.Dequeue();
        }
    }

    private readonly record struct Sample(float Timestamp, bool Success);
}
