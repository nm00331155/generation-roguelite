using System;

namespace GenerationRoguelite.Generation;

public sealed class GenerationManager
{
    public event Action<int>? GenerationAdvanced;

    public int CurrentGeneration { get; private set; } = 1;

    public bool IsLineageExtinct { get; private set; }

    public void Reset()
    {
        CurrentGeneration = 1;
        IsLineageExtinct = false;
    }

    public int Advance(bool lineageExtinct)
    {
        IsLineageExtinct = lineageExtinct;
        if (IsLineageExtinct)
        {
            return CurrentGeneration;
        }

        CurrentGeneration += 1;
        GenerationAdvanced?.Invoke(CurrentGeneration);
        return CurrentGeneration;
    }
}
