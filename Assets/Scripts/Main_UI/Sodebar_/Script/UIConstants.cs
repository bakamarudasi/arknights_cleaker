using UnityEngine;

public static class UIConstants
{
    // === UXML Element Names (name属性) ===
    public const string CONTAINER_SIDEBAR = "sidebar-container"; 
    public const string LIST_SIDEBAR      = "sidebar-list";

    // ★これを追加してください！
    public const string CONTENT_AREA      = "content-area"; 

    // Sidebar Item Elements
    public const string EL_ICON_BOX       = "icon-box";
    public const string EL_ICON_IMAGE     = "icon-image";
    public const string EL_ITEM_LABEL     = "item-label";
    public const string EL_BADGE          = "notification-badge";
    public const string EL_BADGE_TEXT     = "badge-text";
    public const string EL_SELECTION_BAR  = "selection-bar";

    // === USS Class Names (class属性) ===
    public const string CLS_SIDEBAR_ITEM  = "sidebar-item";
    public const string CLS_SELECTED      = "selected";
    public const string CLS_SHOW          = "show";
    public const string CLS_COLLAPSED     = "collapsed";
}

// MenuTypeの定義などはそのままでOK
public enum MenuType
{
    Home,
    Shop,
    Operators,
    Settings,
    Gacha,
    Market
}