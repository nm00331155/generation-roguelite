using GenerationRoguelite.Navigator;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void Speak(DialogueData dialogue)
    {
        if (!dialogue.HasText)
        {
            return;
        }

        _navigatorBubbleTween?.Kill();
        _navigatorArea.Visible = true;
        _navigatorArea.Modulate = new Color(1f, 1f, 1f, 0f);
        _navigatorLabel.Text = $"ナビ: {dialogue.Text}";

        _navigatorBubbleTween = CreateTween();
        _navigatorBubbleTween.TweenProperty(_navigatorArea, "modulate:a", 1f, NavigatorBubbleFadeInSeconds);
        _navigatorBubbleTween.TweenInterval(NavigatorBubbleVisibleSeconds);
        _navigatorBubbleTween.TweenProperty(_navigatorArea, "modulate:a", 0f, NavigatorBubbleFadeOutSeconds);
        _navigatorBubbleTween.TweenCallback(Callable.From(() => _navigatorArea.Visible = false));

        _voicePlayer.Play(dialogue.VoiceId);
    }

    private void ApplyPurchasedNavigatorProfiles(string? preferredProfile = null)
    {
        foreach (var profile in _iapManager.OwnedNavigatorProfiles)
        {
            _navigatorManager.UnlockProfile(profile);
        }

        if (!string.IsNullOrWhiteSpace(preferredProfile)
            && _navigatorManager.SetActiveProfile(preferredProfile))
        {
            return;
        }

        _navigatorManager.SetActiveProfile(_navigatorManager.ActiveProfileId);
    }
}
