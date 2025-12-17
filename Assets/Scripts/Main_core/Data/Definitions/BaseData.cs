using UnityEngine;

/// <summary>
/// すべてのデータ（アイテム、強化、施設など）の親となるクラス。
/// 共通する「ID」「名前」「説明」「アイコン」だけを管理します。
/// </summary>
public abstract class BaseData : ScriptableObject
{
    [Header("基本情報 (Base)")]
    [Tooltip("システム内部で使うID (例: key_card_01)。重複しないように！")]
    public string id;

    [Tooltip("ゲーム画面に表示する名前")]
    public string displayName;

    [TextArea(3, 5)]
    [Tooltip("詳細説明文。フレーバーテキストなど。")]
    public string description;

    [Tooltip("UIに表示するアイコン画像")]
    public Sprite icon;

    [Header("所属企業")]
    [Tooltip("このアイテムを製造している企業。設定すると企業ボーナスが乗る。")]
    public CompanyData affiliatedCompany;
}