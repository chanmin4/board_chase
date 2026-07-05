// SectorShopOpenRequestEventChannelSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct SectorShopOpenRequest
{
    public readonly IReadOnlyList<PlayerShopOffer> bulletOffers;
    public readonly IReadOnlyList<PlayerShopOffer> itemOffers;
    public readonly IReadOnlyList<PlayerShopOffer> offers;
    public readonly ShopRoomDropTableSO dropTable;
    public readonly bool allowReroll;

    public SectorShopOpenRequest(
        IReadOnlyList<PlayerShopOffer> bulletOffers,
        IReadOnlyList<PlayerShopOffer> itemOffers,
        ShopRoomDropTableSO dropTable,
        bool allowReroll)
    {
        this.bulletOffers = bulletOffers ?? Array.Empty<PlayerShopOffer>();
        this.itemOffers = itemOffers ?? Array.Empty<PlayerShopOffer>();
        this.offers = BuildMergedOffers(this.bulletOffers, this.itemOffers);
        this.dropTable = dropTable;
        this.allowReroll = allowReroll;
    }

    public SectorShopOpenRequest(
        IReadOnlyList<PlayerShopOffer> offers,
        ShopRoomDropTableSO dropTable,
        bool allowReroll)
    {
        SplitOffers(
            offers,
            out IReadOnlyList<PlayerShopOffer> splitBulletOffers,
            out IReadOnlyList<PlayerShopOffer> splitItemOffers);

        bulletOffers = splitBulletOffers;
        itemOffers = splitItemOffers;
        this.offers = offers ?? Array.Empty<PlayerShopOffer>();
        this.dropTable = dropTable;
        this.allowReroll = allowReroll;
    }

    private static IReadOnlyList<PlayerShopOffer> BuildMergedOffers(
        IReadOnlyList<PlayerShopOffer> bulletOffers,
        IReadOnlyList<PlayerShopOffer> itemOffers)
    {
        List<PlayerShopOffer> merged = new();

        if (bulletOffers != null)
        {
            for (int i = 0; i < bulletOffers.Count; i++)
            {
                if (bulletOffers[i] != null)
                    merged.Add(bulletOffers[i]);
            }
        }

        if (itemOffers != null)
        {
            for (int i = 0; i < itemOffers.Count; i++)
            {
                if (itemOffers[i] != null)
                    merged.Add(itemOffers[i]);
            }
        }

        return merged;
    }

    private static void SplitOffers(
        IReadOnlyList<PlayerShopOffer> source,
        out IReadOnlyList<PlayerShopOffer> bulletOffers,
        out IReadOnlyList<PlayerShopOffer> itemOffers)
    {
        List<PlayerShopOffer> bullets = new();
        List<PlayerShopOffer> items = new();

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                PlayerShopOffer offer = source[i];

                if (offer == null)
                    continue;

                if (offer.IsBullet)
                    bullets.Add(offer);
                else
                    items.Add(offer);
            }
        }

        bulletOffers = bullets;
        itemOffers = items;
    }
}

[CreateAssetMenu(
    fileName = "SectorShopOpenRequestEventChannel",
    menuName = "Events/Shop/Sector Shop Open Request Event Channel")]
public class SectorShopOpenRequestEventChannelSO : ScriptableObject
{
    public event Action<SectorShopOpenRequest> OnEventRaised;

    public void RaiseEvent(SectorShopOpenRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}