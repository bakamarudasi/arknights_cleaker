using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ガチャバナーを管理するデータベース
/// </summary>
[CreateAssetMenu(fileName = "GachaDatabase", menuName = "Game/Gacha/Database")]
public class GachaDatabase : ScriptableObject
{
    [Header("利用可能なバナー")]
    [SerializeField] private List<GachaBannerData> banners = new();

    /// <summary>
    /// 全バナーを取得
    /// </summary>
    public List<GachaBannerData> GetAllBanners()
    {
        return new List<GachaBannerData>(banners);
    }

    /// <summary>
    /// 限定バナーのみ取得
    /// </summary>
    public List<GachaBannerData> GetLimitedBanners()
    {
        return banners.FindAll(b => b.isLimited);
    }

    /// <summary>
    /// 常設バナーのみ取得
    /// </summary>
    public List<GachaBannerData> GetPermanentBanners()
    {
        return banners.FindAll(b => !b.isLimited);
    }

    /// <summary>
    /// IDでバナー検索
    /// </summary>
    public GachaBannerData GetBannerById(string bannerId)
    {
        return banners.Find(b => b.bannerId == bannerId);
    }

    /// <summary>
    /// バナー数
    /// </summary>
    public int Count => banners.Count;
}