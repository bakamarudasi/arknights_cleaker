using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲームデータ管理ツールとの連携用エディタウィンドウ
/// ScriptableObject ⟷ JSON 双方向同期
/// </summary>
public class GameDataManagerWindow : EditorWindow
{
    // JSON出力先（ツールのdataフォルダ）
    private string jsonOutputPath = "";
    private Vector2 scrollPosition;

    // 同期対象
    private bool syncItems = true;
    private bool syncUpgrades = true;
    private bool syncGacha = true;
    private bool syncCompanies = true;
    private bool syncEvents = true;

    // ログ
    private List<string> logs = new();
    private Vector2 logScroll;

    [MenuItem("Tools/Game Data Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameDataManagerWindow>("Data Manager");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        // デフォルトパスを設定
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? "";
        jsonOutputPath = Path.Combine(projectRoot, "tools", "game-data-manager", "backend", "data");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Game Data Manager 連携", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // パス設定
        EditorGUILayout.LabelField("JSON出力先", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        jsonOutputPath = EditorGUILayout.TextField(jsonOutputPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("JSONフォルダを選択", jsonOutputPath, "");
            if (!string.IsNullOrEmpty(path)) jsonOutputPath = path;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 同期対象選択
        EditorGUILayout.LabelField("同期対象", EditorStyles.boldLabel);
        syncItems = EditorGUILayout.Toggle("アイテム (ItemData)", syncItems);
        syncUpgrades = EditorGUILayout.Toggle("アップグレード (UpgradeData)", syncUpgrades);
        syncGacha = EditorGUILayout.Toggle("ガチャ (GachaBannerData)", syncGacha);
        syncCompanies = EditorGUILayout.Toggle("企業 (CompanyData)", syncCompanies);
        syncEvents = EditorGUILayout.Toggle("イベント (GameEventData)", syncEvents);

        GUILayout.Space(20);

        // エクスポートボタン
        EditorGUILayout.LabelField("エクスポート (Unity → JSON)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("現在のScriptableObjectをJSONファイルに出力します", MessageType.Info);

        if (GUILayout.Button("エクスポート実行", GUILayout.Height(30)))
        {
            ExportToJson();
        }

        GUILayout.Space(20);

        // インポートボタン
        EditorGUILayout.LabelField("インポート (JSON → Unity)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("JSONファイルからScriptableObjectを生成/更新します", MessageType.Info);

        if (GUILayout.Button("インポート実行", GUILayout.Height(30)))
        {
            ImportFromJson();
        }

        GUILayout.Space(20);

        // ログ表示
        EditorGUILayout.LabelField("ログ", EditorStyles.boldLabel);
        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(150));
        foreach (var log in logs.TakeLast(50))
        {
            EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("ログクリア"))
        {
            logs.Clear();
        }

        EditorGUILayout.EndScrollView();
    }

    private void Log(string message)
    {
        logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        Repaint();
    }

    // ========================================
    // エクスポート
    // ========================================

    private void ExportToJson()
    {
        if (!Directory.Exists(jsonOutputPath))
        {
            Directory.CreateDirectory(jsonOutputPath);
        }

        Log("エクスポート開始...");
        int total = 0;

        if (syncItems) total += ExportItems();
        if (syncUpgrades) total += ExportUpgrades();
        if (syncGacha) total += ExportGacha();
        if (syncCompanies) total += ExportCompanies();
        if (syncEvents) total += ExportEvents();

        Log($"エクスポート完了: {total}件");
        AssetDatabase.Refresh();
    }

    private int ExportItems()
    {
        var items = FindAllAssets<ItemData>();
        var jsonList = new List<Dictionary<string, object>>();

        foreach (var item in items)
        {
            jsonList.Add(new Dictionary<string, object>
            {
                ["id"] = item.id,
                ["displayName"] = item.displayName,
                ["description"] = item.description,
                ["icon"] = item.icon != null ? AssetDatabase.GetAssetPath(item.icon) : null,
                ["type"] = item.type.ToString(),
                ["rarity"] = item.rarity.ToString(),
                ["sortOrder"] = item.sortOrder,
                ["maxStack"] = item.maxStack,
                ["sellPrice"] = item.sellPrice,
                ["useEffect"] = item.useEffect.ToString(),
                ["effectValue"] = item.effectValue,
                ["effectDuration"] = item.effectDuration,
                ["convertToItemId"] = item.convertToItem != null ? item.convertToItem.id : null,
                ["convertAmount"] = item.convertAmount,
                ["targetCharacterId"] = item.targetCharacterId,
                ["targetCostumeIndex"] = item.targetCostumeIndex,
                ["effectFormat"] = item.effectFormat,
                ["isPercentDisplay"] = item.isPercentDisplay,
                ["categoryIcon"] = item.categoryIcon,
                ["isSpecial"] = item.isSpecial,
            });
        }

        WriteJson("items.json", jsonList);
        Log($"  Items: {items.Count}件");
        return items.Count;
    }

    private int ExportUpgrades()
    {
        var upgrades = FindAllAssets<UpgradeData>();
        var jsonList = new List<Dictionary<string, object>>();

        foreach (var u in upgrades)
        {
            jsonList.Add(new Dictionary<string, object>
            {
                ["id"] = u.id,
                ["displayName"] = u.displayName,
                ["description"] = u.description,
                ["icon"] = u.icon != null ? AssetDatabase.GetAssetPath(u.icon) : null,
                ["upgradeType"] = u.upgradeType.ToString(),
                ["category"] = u.category.ToString(),
                ["effectValue"] = u.effectValue,
                ["maxLevel"] = u.maxLevel,
                ["currencyType"] = u.currencyType.ToString(),
                ["baseCost"] = u.baseCost,
                ["costMultiplier"] = u.costMultiplier,
                ["requiredUnlockItemId"] = u.requiredUnlockItem != null ? u.requiredUnlockItem.id : null,
                ["relatedStockId"] = u.relatedStock != null ? u.relatedStock.stockId : null,
                ["scaleWithHolding"] = u.scaleWithHolding,
                ["maxHoldingMultiplier"] = u.maxHoldingMultiplier,
                ["sortOrder"] = u.sortOrder,
                ["effectFormat"] = u.effectFormat,
                ["isPercentDisplay"] = u.isPercentDisplay,
            });
        }

        WriteJson("upgrades.json", jsonList);
        Log($"  Upgrades: {upgrades.Count}件");
        return upgrades.Count;
    }

    private int ExportGacha()
    {
        var banners = FindAllAssets<GachaBannerData>();
        var jsonList = new List<Dictionary<string, object>>();

        foreach (var b in banners)
        {
            var pool = new List<Dictionary<string, object>>();
            if (b.pool != null)
            {
                foreach (var entry in b.pool)
                {
                    if (entry.item != null)
                    {
                        pool.Add(new Dictionary<string, object>
                        {
                            ["itemId"] = entry.item.id,
                            ["weight"] = entry.weight,
                            ["isPickup"] = entry.isPickup,
                            ["stockCount"] = entry.stockCount
                        });
                    }
                }
            }

            var pickupIds = new List<string>();
            if (b.pickupItems != null)
            {
                foreach (var item in b.pickupItems)
                {
                    if (item != null) pickupIds.Add(item.id);
                }
            }

            jsonList.Add(new Dictionary<string, object>
            {
                ["bannerId"] = b.bannerId,
                ["bannerName"] = b.bannerName,
                ["description"] = b.description,
                ["bannerSprite"] = b.bannerSprite != null ? AssetDatabase.GetAssetPath(b.bannerSprite) : null,
                ["isLimited"] = b.isLimited,
                ["currencyType"] = b.currencyType.ToString(),
                ["costSingle"] = b.costSingle,
                ["costTen"] = b.costTen,
                ["hasPity"] = b.hasPity,
                ["pityCount"] = b.pityCount,
                ["softPityStart"] = b.softPityStart,
                ["pool"] = pool,
                ["pickupItemIds"] = pickupIds,
                ["pickupRateBoost"] = b.pickupRateBoost,
                ["startsLocked"] = b.startsLocked,
                ["prerequisiteBannerId"] = b.prerequisiteBanner != null ? b.prerequisiteBanner.bannerId : null,
                ["requiredUnlockItemId"] = b.requiredUnlockItem != null ? b.requiredUnlockItem.id : null,
            });
        }

        WriteJson("gacha_banners.json", jsonList);
        Log($"  Gacha Banners: {banners.Count}件");
        return banners.Count;
    }

    private int ExportCompanies()
    {
        var companies = FindAllAssets<CompanyData>();
        var jsonList = new List<Dictionary<string, object>>();

        foreach (var c in companies)
        {
            jsonList.Add(new Dictionary<string, object>
            {
                ["id"] = c.id,
                ["displayName"] = c.displayName,
                ["description"] = c.description,
                ["logo"] = c.logo != null ? AssetDatabase.GetAssetPath(c.logo) : null,
                ["chartColor"] = ColorUtility.ToHtmlStringRGB(c.chartColor),
                ["themeColor"] = ColorUtility.ToHtmlStringRGB(c.themeColor),
                ["sortOrder"] = c.sortOrder,
                ["traitType"] = c.traitType.ToString(),
                ["traitMultiplier"] = c.traitMultiplier,
                ["initialPrice"] = c.initialPrice,
                ["minPrice"] = c.minPrice,
                ["maxPrice"] = c.maxPrice,
                ["volatility"] = c.volatility,
                ["drift"] = c.drift,
                ["jumpProbability"] = c.jumpProbability,
                ["jumpIntensity"] = c.jumpIntensity,
                ["transactionFee"] = c.transactionFee,
                ["sector"] = c.sector.ToString(),
                ["totalShares"] = c.totalShares,
                ["dividendRate"] = c.dividendRate,
                ["dividendIntervalSeconds"] = c.dividendIntervalSeconds,
                ["unlockKeyItemId"] = c.unlockKeyItem != null ? c.unlockKeyItem.id : null,
                ["isPlayerCompany"] = c.isPlayerCompany,
                ["canSell"] = c.canSell,
            });
        }

        WriteJson("companies.json", jsonList);
        Log($"  Companies: {companies.Count}件");
        return companies.Count;
    }

    private int ExportEvents()
    {
        var events = FindAllAssets<GameEventData>();
        var jsonList = new List<Dictionary<string, object>>();

        foreach (var e in events)
        {
            var rewards = new List<Dictionary<string, object>>();
            if (e.rewardItems != null)
            {
                foreach (var r in e.rewardItems)
                {
                    rewards.Add(new Dictionary<string, object>
                    {
                        ["itemId"] = r.itemId,
                        ["amount"] = r.amount
                    });
                }
            }

            jsonList.Add(new Dictionary<string, object>
            {
                ["eventId"] = e.eventId,
                ["eventName"] = e.eventName,
                ["description"] = e.description,
                ["triggerType"] = e.triggerType.ToString(),
                ["triggerValue"] = e.triggerValue,
                ["requireId"] = e.requireId,
                ["prerequisiteEventId"] = e.prerequisiteEvent != null ? e.prerequisiteEvent.eventId : null,
                ["oneTimeOnly"] = e.oneTimeOnly,
                ["pauseGame"] = e.pauseGame,
                ["priority"] = e.priority,
                ["notificationText"] = e.notificationText,
                ["notificationIcon"] = e.notificationIcon != null ? AssetDatabase.GetAssetPath(e.notificationIcon) : null,
                ["unlockMenu"] = e.unlockMenu?.ToString(),
                ["rewardMoney"] = e.rewardMoney,
                ["rewardCertificates"] = e.rewardCertificates,
                ["rewardItems"] = rewards,
            });
        }

        WriteJson("game_events.json", jsonList);
        Log($"  Game Events: {events.Count}件");
        return events.Count;
    }

    // ========================================
    // インポート
    // ========================================

    private void ImportFromJson()
    {
        Log("インポート開始...");
        int total = 0;

        if (syncItems) total += ImportItems();
        if (syncUpgrades) total += ImportUpgrades();
        if (syncGacha) total += ImportGacha();
        if (syncCompanies) total += ImportCompanies();
        if (syncEvents) total += ImportEvents();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"インポート完了: {total}件");
    }

    private int ImportItems()
    {
        var jsonPath = Path.Combine(jsonOutputPath, "items.json");
        if (!File.Exists(jsonPath))
        {
            Log("  items.json が見つかりません");
            return 0;
        }

        var jsonText = File.ReadAllText(jsonPath);
        var items = JsonHelper.FromJsonArray<ItemJsonData>(jsonText);
        int count = 0;

        foreach (var data in items)
        {
            var existing = FindAssetById<ItemData>(data.id);
            var item = existing ?? CreateInstance<ItemData>();

            item.id = data.id;
            item.displayName = data.displayName;
            item.description = data.description ?? "";

            if (Enum.TryParse<ItemData.ItemType>(data.type, out var itemType))
                item.type = itemType;
            if (Enum.TryParse<ItemData.Rarity>(data.rarity, out var rarity))
                item.rarity = rarity;

            item.sortOrder = data.sortOrder;
            item.maxStack = data.maxStack;
            item.sellPrice = data.sellPrice;

            if (Enum.TryParse<ItemData.ConsumableType>(data.useEffect, out var useEffect))
                item.useEffect = useEffect;

            item.effectValue = data.effectValue;
            item.effectDuration = data.effectDuration;
            item.convertAmount = data.convertAmount;
            item.targetCharacterId = data.targetCharacterId;
            item.targetCostumeIndex = data.targetCostumeIndex;
            item.effectFormat = data.effectFormat ?? "+{0}";
            item.isPercentDisplay = data.isPercentDisplay;
            item.categoryIcon = data.categoryIcon ?? "";
            item.isSpecial = data.isSpecial;

            if (existing == null)
            {
                var assetPath = $"Assets/Items/Generated/{data.id}.asset";
                EnsureDirectoryExists(assetPath);
                AssetDatabase.CreateAsset(item, assetPath);
            }

            EditorUtility.SetDirty(item);
            count++;
        }

        Log($"  Items: {count}件インポート");
        return count;
    }

    private int ImportUpgrades()
    {
        var jsonPath = Path.Combine(jsonOutputPath, "upgrades.json");
        if (!File.Exists(jsonPath))
        {
            Log("  upgrades.json が見つかりません");
            return 0;
        }

        var jsonText = File.ReadAllText(jsonPath);
        var upgrades = JsonHelper.FromJsonArray<UpgradeJsonData>(jsonText);
        int count = 0;

        foreach (var data in upgrades)
        {
            var existing = FindAssetById<UpgradeData>(data.id);
            var u = existing ?? CreateInstance<UpgradeData>();

            u.id = data.id;
            u.displayName = data.displayName;
            u.description = data.description ?? "";

            if (Enum.TryParse<UpgradeData.UpgradeType>(data.upgradeType, out var upgradeType))
                u.upgradeType = upgradeType;
            if (Enum.TryParse<UpgradeData.UpgradeCategory>(data.category, out var category))
                u.category = category;
            if (Enum.TryParse<UpgradeData.CurrencyType>(data.currencyType, out var currency))
                u.currencyType = currency;

            u.effectValue = data.effectValue;
            u.maxLevel = data.maxLevel;
            u.baseCost = data.baseCost;
            u.costMultiplier = data.costMultiplier;
            u.scaleWithHolding = data.scaleWithHolding;
            u.maxHoldingMultiplier = data.maxHoldingMultiplier;
            u.sortOrder = data.sortOrder;
            u.effectFormat = data.effectFormat ?? "+{0}";
            u.isPercentDisplay = data.isPercentDisplay;

            // 参照の解決
            if (!string.IsNullOrEmpty(data.requiredUnlockItemId))
                u.requiredUnlockItem = FindAssetById<ItemData>(data.requiredUnlockItemId);

            if (existing == null)
            {
                var assetPath = $"Assets/Items/Generated/Upgrades/{data.id}.asset";
                EnsureDirectoryExists(assetPath);
                AssetDatabase.CreateAsset(u, assetPath);
            }

            EditorUtility.SetDirty(u);
            count++;
        }

        Log($"  Upgrades: {count}件インポート");
        return count;
    }

    private int ImportGacha()
    {
        // 簡略化のため省略（同様のパターン）
        Log("  Gacha: スキップ（未実装）");
        return 0;
    }

    private int ImportCompanies()
    {
        // 簡略化のため省略（同様のパターン）
        Log("  Companies: スキップ（未実装）");
        return 0;
    }

    private int ImportEvents()
    {
        // 簡略化のため省略（同様のパターン）
        Log("  Events: スキップ（未実装）");
        return 0;
    }

    // ========================================
    // ヘルパー
    // ========================================

    private List<T> FindAllAssets<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var assets = new List<T>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) assets.Add(asset);
        }

        return assets;
    }

    private T FindAssetById<T>(string id) where T : ScriptableObject
    {
        var assets = FindAllAssets<T>();

        foreach (var asset in assets)
        {
            // リフレクションでidフィールドを取得
            var idField = typeof(T).GetField("id") ?? typeof(T).GetField("eventId") ?? typeof(T).GetField("bannerId");
            if (idField != null)
            {
                var value = idField.GetValue(asset) as string;
                if (value == id) return asset;
            }
        }

        return null;
    }

    private void WriteJson(string filename, object data)
    {
        var path = Path.Combine(jsonOutputPath, filename);
        var json = JsonUtility.ToJson(new JsonWrapper { items = data as List<Dictionary<string, object>> }, true);

        // Unity JsonUtilityは辞書を直接シリアライズできないので、簡易的なJSON生成
        var jsonText = SerializeToJson(data);
        File.WriteAllText(path, jsonText);
    }

    private string SerializeToJson(object data)
    {
        if (data is List<Dictionary<string, object>> list)
        {
            var items = new List<string>();
            foreach (var dict in list)
            {
                items.Add(SerializeDictionary(dict));
            }
            return "[\n  " + string.Join(",\n  ", items) + "\n]";
        }
        return "[]";
    }

    private string SerializeDictionary(Dictionary<string, object> dict)
    {
        var pairs = new List<string>();
        foreach (var kvp in dict)
        {
            var value = SerializeValue(kvp.Value);
            pairs.Add($"\"{kvp.Key}\": {value}");
        }
        return "{\n    " + string.Join(",\n    ", pairs) + "\n  }";
    }

    private string SerializeValue(object value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{EscapeString(s)}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is int || value is long || value is float || value is double) return value.ToString();
        if (value is List<string> strList)
            return "[" + string.Join(", ", strList.Select(x => $"\"{EscapeString(x)}\"")) + "]";
        if (value is List<Dictionary<string, object>> dictList)
        {
            var items = dictList.Select(d => SerializeDictionary(d));
            return "[\n      " + string.Join(",\n      ", items) + "\n    ]";
        }
        return $"\"{value}\"";
    }

    private string EscapeString(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private void EnsureDirectoryExists(string assetPath)
    {
        var dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    [Serializable]
    private class JsonWrapper
    {
        public List<Dictionary<string, object>> items;
    }
}

// ========================================
// JSON用データクラス
// ========================================

[Serializable]
public class ItemJsonData
{
    public string id;
    public string displayName;
    public string description;
    public string icon;
    public string type;
    public string rarity;
    public int sortOrder;
    public int maxStack;
    public int sellPrice;
    public string useEffect;
    public float effectValue;
    public float effectDuration;
    public string convertToItemId;
    public int convertAmount;
    public string targetCharacterId;
    public int targetCostumeIndex;
    public string effectFormat;
    public bool isPercentDisplay;
    public string categoryIcon;
    public bool isSpecial;
}

[Serializable]
public class UpgradeJsonData
{
    public string id;
    public string displayName;
    public string description;
    public string icon;
    public string upgradeType;
    public string category;
    public double effectValue;
    public int maxLevel;
    public string currencyType;
    public double baseCost;
    public float costMultiplier;
    public string requiredUnlockItemId;
    public string relatedStockId;
    public bool scaleWithHolding;
    public float maxHoldingMultiplier;
    public int sortOrder;
    public string effectFormat;
    public bool isPercentDisplay;
}

// JSONパースヘルパー
public static class JsonHelper
{
    public static List<T> FromJsonArray<T>(string json)
    {
        // Unity JsonUtilityは配列を直接パースできないので、ラッパーを使う
        string wrapped = "{\"items\":" + json + "}";
        var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
        return wrapper?.items ?? new List<T>();
    }

    [Serializable]
    private class JsonArrayWrapper<T>
    {
        public List<T> items;
    }
}
