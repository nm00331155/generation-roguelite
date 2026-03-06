using System.Collections.Generic;
using GenerationRoguelite.Character;
using GenerationRoguelite.Core;

namespace GenerationRoguelite.Events;

public sealed class EventGenerationContext
{
    public int Age { get; }
    public Stats Stats { get; }
    public int Generation { get; }
    public LifePhase Phase { get; }
    public string Era { get; }
    public string LifePath { get; }
    public IReadOnlyList<string> Bonds { get; }
    public IReadOnlyList<string> FamilyTraits { get; }
    public IReadOnlyList<string> RecentEvents { get; }
    public string EraMechanic { get; }

    public EventGenerationContext(
        int age,
        Stats stats,
        int generation,
        LifePhase phase,
        string era,
        string lifePath,
        IReadOnlyList<string> bonds,
        IReadOnlyList<string> familyTraits,
        IReadOnlyList<string> recentEvents,
        string eraMechanic)
    {
        Age = age;
        Stats = stats;
        Generation = generation;
        Phase = phase;
        Era = era;
        LifePath = lifePath;
        Bonds = bonds;
        FamilyTraits = familyTraits;
        RecentEvents = recentEvents;
        EraMechanic = eraMechanic;
    }
}
