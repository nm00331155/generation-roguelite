using GenerationRoguelite.Monetization;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager
{
    private void SetDebugOverlayEnabled(bool enabled)
    {
        _debugOverlayEnabled = enabled;
        _debugOverlay.SetOverlayEnabled(enabled);
    }

    private bool TryHandleDebugCommand(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return false;
        }

        switch (keyEvent.Keycode)
        {
            case Key.F5:
            {
                var purchase = _iapManager.Purchase(IapProductIds.RemoveAds);
                _adManager.AdsRemoved = _iapManager.AdsRemoved;
                _eventLabel.Text = purchase.Message;
                return true;
            }

            case Key.F6:
            {
                var purchase = _iapManager.Purchase(IapProductIds.PremiumPass);
                _battlePassManager.SetPremiumEnabled(_iapManager.PremiumPassOwned);
                _eventLabel.Text = purchase.Message;
                return true;
            }

            case Key.F7:
            {
                string[] products =
                [
                    IapProductIds.NavigatorMentor,
                    IapProductIds.NavigatorTrickster,
                    IapProductIds.NavigatorOracle,
                ];

                var index = _character.Generation % products.Length;
                var purchase = _iapManager.Purchase(products[index]);
                ApplyPurchasedNavigatorProfiles();
                _eventLabel.Text = purchase.Message;
                return true;
            }

            case Key.F8:
            {
                ApplyPurchasedNavigatorProfiles();
                var switched = _navigatorManager.CycleToNextProfile();
                _eventLabel.Text = $"ナビ切替: {switched}";
                return true;
            }

            case Key.F9:
            {
                if (_adManager.TryWatchRewardAd(RewardAdType.InheritanceBoost, _character.Generation, out var reward))
                {
                    _pendingInheritanceBonusWealth += reward.WealthBonus;
                    _eventLabel.Text = reward.Message;
                }
                else
                {
                    _eventLabel.Text = reward.Message;
                }

                return true;
            }

            case Key.F10:
            {
                if (_adManager.TryWatchRewardAd(RewardAdType.EventRetry, _character.Generation, out var reward))
                {
                    _retryTokenAvailable = reward.RetryToken;
                    _eventLabel.Text = reward.Message;
                }
                else
                {
                    _eventLabel.Text = reward.Message;
                }

                return true;
            }

            case Key.F11:
            {
                if (_adManager.TryWatchRewardAd(RewardAdType.ShopSlot, _character.Generation, out var reward))
                {
                    _shopSlotBonus += reward.ShopSlotBonus;
                    _eventLabel.Text = reward.Message;
                }
                else
                {
                    _eventLabel.Text = reward.Message;
                }

                return true;
            }

            case Key.F12:
            {
                SetDebugOverlayEnabled(!_debugOverlayEnabled);
                _eventLabel.Text = _debugOverlayEnabled ? "デバッグ表示: ON" : "デバッグ表示: OFF";
                return true;
            }
        }

        return false;
    }
}
