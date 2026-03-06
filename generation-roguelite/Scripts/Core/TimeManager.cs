namespace GenerationRoguelite.Core;

public sealed class TimeManager
{
    private float _elapsedSeconds;
    private bool _isPaused;

    public event System.Action<int>? YearPassed;

    public float SecondsPerYear { get; set; } = 1.0f;

    public float SpeedMultiplier { get; private set; } = 1.0f;

    public bool IsPaused => _isPaused;

    public void SetSpeedMultiplier(float multiplier)
    {
        SpeedMultiplier = System.Math.Clamp(multiplier, 1.0f, 2.0f);
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    public int ConsumeAdvancedYears(double delta)
    {
        if (_isPaused)
        {
            return 0;
        }

        _elapsedSeconds += (float)delta * SpeedMultiplier;

        var advancedYears = (int)(_elapsedSeconds / SecondsPerYear);
        if (advancedYears <= 0)
        {
            return 0;
        }

        _elapsedSeconds -= advancedYears * SecondsPerYear;

        for (var i = 0; i < advancedYears; i++)
        {
            YearPassed?.Invoke(1);
        }

        return advancedYears;
    }

    public void Reset()
    {
        _elapsedSeconds = 0f;
    }
}
