using Godot;

namespace GenerationRoguelite.Generation;

public sealed class PartnerSystem
{
    private readonly RandomNumberGenerator _rng = new();

    public PartnerSystem()
    {
        _rng.Randomize();
    }

    public bool TryFindPartner(int charisma, int luck, int generation, out string partnerName)
    {
        var baseChance = 0.18f;
        var statBonus = charisma * 0.005f + luck * 0.004f;
        var generationPenalty = generation * 0.003f;
        var chance = Mathf.Clamp(baseChance + statBonus - generationPenalty, 0.05f, 0.85f);

        if (_rng.Randf() <= chance)
        {
            partnerName = BuildPartnerName(generation);
            return true;
        }

        partnerName = string.Empty;
        return false;
    }

    private string BuildPartnerName(int generation)
    {
        var family = new[] { "朝霧", "風間", "遠野", "結城", "白石", "雨宮" };
        var given = new[] { "ミナト", "カナメ", "アオイ", "ヒナ", "ユウト", "サラ" };

        var familyName = family[(int)_rng.RandiRange(0, family.Length - 1)];
        var givenName = given[(int)_rng.RandiRange(0, given.Length - 1)];
        return $"{familyName} {givenName}{generation}";
    }
}
