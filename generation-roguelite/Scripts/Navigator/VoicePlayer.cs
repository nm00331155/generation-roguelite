using Godot;

namespace GenerationRoguelite.Navigator;

public sealed class VoicePlayer
{
    private readonly AudioStreamPlayer _player;
    private readonly string _voiceRootPath;

    public VoicePlayer(AudioStreamPlayer player, string voiceRootPath = "res://Assets/Audio/Voice/default/")
    {
        _player = player;
        _voiceRootPath = voiceRootPath;
    }

    public void Play(string voiceId)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            return;
        }

        var path = $"{_voiceRootPath}{voiceId}.ogg";
        if (!ResourceLoader.Exists(path))
        {
            return;
        }

        var stream = GD.Load<AudioStream>(path);
        if (stream is null)
        {
            return;
        }

        _player.Stream = stream;
        _player.Play();
    }
}
