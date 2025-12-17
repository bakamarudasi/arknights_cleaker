using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// メニュー項目の設定データ（ScriptableObject）
/// Inspectorから設定可能
/// </summary>
[CreateAssetMenu(fileName = "MenuItemData", menuName = "UI/Menu Item Data")]
public class MenuItemData : ScriptableObject
{
    [Header("基本設定")]
    public MenuType menuType;
    public string displayName = "Menu Item";
    public Sprite icon;
    
    [Header("初期状態")]
    public bool showBadgeOnStart = false;
    
    [Header("View設定")]
    [Tooltip("このメニューで表示するUXMLテンプレート（オプション）")]
    public VisualTreeAsset viewTemplate;
    
    [Header("メッセージ")]
    public string logMessage = "Menu switched.";
}
