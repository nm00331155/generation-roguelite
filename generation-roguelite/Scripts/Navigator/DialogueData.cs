namespace GenerationRoguelite.Navigator;

public readonly record struct DialogueData(string Text, string VoiceId)
{
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
}
