using System;

namespace GenerationRoguelite.Events;

public sealed class EventValidator
{
    public bool IsValid(EventData eventData, out string reason)
    {
        if (eventData.EventText.Length is < 20 or > 120)
        {
            reason = "event_text length out of range (20-120).";
            return false;
        }

        if (!IsChoiceValid(eventData.TapChoice, out reason))
        {
            return false;
        }

        if (!IsChoiceValid(eventData.SwipeChoice, out reason))
        {
            return false;
        }

        if (eventData.Terrain.ObstacleDensity is < 0.1f or > 1.5f)
        {
            reason = "terrain.obstacle_density out of range.";
            return false;
        }

        if (eventData.Terrain.SpeedModifier is < 0.6f or > 1.6f)
        {
            reason = "terrain.speed_modifier out of range.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsChoiceValid(EventChoice choice, out string reason)
    {
        if (string.IsNullOrWhiteSpace(choice.Text))
        {
            reason = "choice text is empty.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(choice.CheckStat) && choice.SuccessText == choice.FailText)
        {
            reason = "choice success/fail text must differ.";
            return false;
        }

        if (!IsWithinStatDeltaRange(choice.SuccessDelta) || !IsWithinStatDeltaRange(choice.FailDelta))
        {
            reason = "choice stat delta exceeds ±10.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsWithinStatDeltaRange(StatDelta delta)
    {
        return
            Math.Abs(delta.Vitality) <= 10
            && Math.Abs(delta.Intelligence) <= 10
            && Math.Abs(delta.Charisma) <= 10
            && Math.Abs(delta.Luck) <= 10
            && Math.Abs(delta.Wealth) <= 10;
    }
}
