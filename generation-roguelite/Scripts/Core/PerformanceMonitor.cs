using Godot;

namespace GenerationRoguelite.Core;

public readonly record struct PerformanceTick(
    bool HasSample,
    float Fps,
    float MemoryMb,
    bool ShouldTrimCaches,
    string Summary);

public sealed class PerformanceMonitor
{
    private float _sampleTimer;

    public float SampleIntervalSeconds { get; set; } = 4f;

    public float MemoryTrimThresholdMb { get; set; } = 900f;

    public PerformanceTick Tick(double delta)
    {
        _sampleTimer -= (float)delta;
        if (_sampleTimer > 0f)
        {
            return default;
        }

        _sampleTimer = SampleIntervalSeconds;

        var fps = (float)Engine.GetFramesPerSecond();
        var memoryBytes = (double)Performance.GetMonitor(Performance.Monitor.MemoryStatic);
        var memoryMb = (float)(memoryBytes / (1024d * 1024d));

        var shouldTrim = memoryMb >= MemoryTrimThresholdMb;
        var summary = $"perf: {fps:F0}fps / {memoryMb:F0}MB";

        return new PerformanceTick(true, fps, memoryMb, shouldTrim, summary);
    }
}
