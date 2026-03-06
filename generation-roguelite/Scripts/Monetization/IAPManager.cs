using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Monetization;

public static class IapProductIds
{
    public const string RemoveAds = "remove_ads";
    public const string PremiumPass = "premium_pass";
    public const string NavigatorMentor = "navigator_mentor";
    public const string NavigatorTrickster = "navigator_trickster";
    public const string NavigatorOracle = "navigator_oracle";
}

public readonly record struct IapPurchaseResult(bool Success, string ProductId, string Message);

public readonly record struct IapProductDefinition(
    string ProductId,
    string DisplayName,
    int PriceYen);

public sealed class IapState
{
    public bool AdsRemoved { get; set; }

    public bool PremiumPassOwned { get; set; }

    public List<string> OwnedNavigatorProfiles { get; set; } = [];
}

public sealed class IAPManager
{
    private static readonly IReadOnlyDictionary<string, IapProductDefinition> ProductCatalog =
        new Dictionary<string, IapProductDefinition>(StringComparer.Ordinal)
        {
            [IapProductIds.RemoveAds] = new(IapProductIds.RemoveAds, "広告除去", 980),
            [IapProductIds.PremiumPass] = new(IapProductIds.PremiumPass, "運命の書", 360),
            [IapProductIds.NavigatorMentor] = new(IapProductIds.NavigatorMentor, "追加ナビ: Mentor", 480),
            [IapProductIds.NavigatorTrickster] = new(IapProductIds.NavigatorTrickster, "追加ナビ: Trickster", 480),
            [IapProductIds.NavigatorOracle] = new(IapProductIds.NavigatorOracle, "追加ナビ: Oracle", 480),
        };

    private readonly HashSet<string> _ownedProducts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ownedNavigatorProfiles = new(StringComparer.Ordinal)
    {
        "default",
    };

    public bool AdsRemoved => _ownedProducts.Contains(IapProductIds.RemoveAds);

    public bool PremiumPassOwned => _ownedProducts.Contains(IapProductIds.PremiumPass);

    public IReadOnlyList<string> OwnedNavigatorProfiles => _ownedNavigatorProfiles
        .OrderBy(id => id)
        .ToArray();

    public IReadOnlyList<IapProductDefinition> GetCatalog()
    {
        return ProductCatalog.Values
            .OrderBy(product => product.ProductId)
            .ToArray();
    }

    public bool TryGetProductDefinition(string productId, out IapProductDefinition definition)
    {
        return ProductCatalog.TryGetValue(productId, out definition);
    }

    public IapPurchaseResult Purchase(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return new IapPurchaseResult(false, productId, "無効な商品IDです。");
        }

        if (!ProductCatalog.TryGetValue(productId, out var product))
        {
            return new IapPurchaseResult(false, productId, "未登録の商品IDです。");
        }

        if (_ownedProducts.Contains(productId))
        {
            return new IapPurchaseResult(true, productId, "すでに購入済みです。");
        }

        _ownedProducts.Add(productId);

        switch (productId)
        {
            case IapProductIds.NavigatorMentor:
                _ownedNavigatorProfiles.Add("mentor");
                break;
            case IapProductIds.NavigatorTrickster:
                _ownedNavigatorProfiles.Add("trickster");
                break;
            case IapProductIds.NavigatorOracle:
                _ownedNavigatorProfiles.Add("oracle");
                break;
        }

        return new IapPurchaseResult(true, productId, $"購入完了: {product.DisplayName} ({product.PriceYen}円)");
    }

    public bool OwnsNavigatorProfile(string profileId)
    {
        return _ownedNavigatorProfiles.Contains(profileId);
    }

    public string BuildSummary()
    {
        var products = _ownedProducts.Count == 0
            ? "なし"
            : string.Join(",", _ownedProducts.OrderBy(item => item));

        var nav = string.Join(",", OwnedNavigatorProfiles);
        var removeAdsPrice = ProductCatalog[IapProductIds.RemoveAds].PriceYen;
        var premiumPassPrice = ProductCatalog[IapProductIds.PremiumPass].PriceYen;
        var navigatorPrice = ProductCatalog[IapProductIds.NavigatorMentor].PriceYen;
        return $"IAP: [{products}] / ナビ[{nav}] / 価格(広告除去{removeAdsPrice}円, パス{premiumPassPrice}円, ナビ{navigatorPrice}円)";
    }

    public IapState BuildState()
    {
        return new IapState
        {
            AdsRemoved = AdsRemoved,
            PremiumPassOwned = PremiumPassOwned,
            OwnedNavigatorProfiles = [.. _ownedNavigatorProfiles.OrderBy(id => id)],
        };
    }

    public void LoadState(IapState? state)
    {
        _ownedProducts.Clear();
        _ownedNavigatorProfiles.Clear();
        _ownedNavigatorProfiles.Add("default");

        if (state is null)
        {
            return;
        }

        if (state.AdsRemoved)
        {
            _ownedProducts.Add(IapProductIds.RemoveAds);
        }

        if (state.PremiumPassOwned)
        {
            _ownedProducts.Add(IapProductIds.PremiumPass);
        }

        foreach (var profile in state.OwnedNavigatorProfiles)
        {
            if (!string.IsNullOrWhiteSpace(profile))
            {
                _ownedNavigatorProfiles.Add(profile);
            }
        }
    }
}
