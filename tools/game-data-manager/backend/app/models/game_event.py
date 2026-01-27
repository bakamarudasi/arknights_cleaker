"""GameEventData model - ゲームイベント定義"""
from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field


class EventTriggerType(str, Enum):
    NONE = "None"
    MONEY_REACHED = "MoneyReached"
    CLICK_COUNT = "ClickCount"
    UPGRADE_PURCHASED = "UpgradePurchased"
    ITEM_OBTAINED = "ItemObtained"
    TIME_ELAPSED = "TimeElapsed"
    AFFECTION_LEVEL = "AffectionLevel"
    STOCK_OWNED = "StockOwned"


class MenuType(str, Enum):
    SHOP = "Shop"
    INVENTORY = "Inventory"
    GACHA = "Gacha"
    MARKET = "Market"
    SETTINGS = "Settings"
    OPERATOR = "Operator"


class ItemReward(BaseModel):
    """アイテム報酬"""
    item_id: str = Field(alias="itemId")
    amount: int = 1

    class Config:
        populate_by_name = True


class GameEventData(BaseModel):
    """ゲームイベントデータ"""
    # 基本情報
    event_id: str = Field(alias="eventId")
    event_name: str = Field(alias="eventName")
    description: str = ""

    # 発動条件
    trigger_type: EventTriggerType = Field(default=EventTriggerType.NONE, alias="triggerType")
    trigger_value: float = Field(default=0.0, alias="triggerValue")
    require_id: Optional[str] = Field(default=None, alias="requireId")
    prerequisite_event_id: Optional[str] = Field(default=None, alias="prerequisiteEventId")

    # 発動設定
    one_time_only: bool = Field(default=True, alias="oneTimeOnly")
    pause_game: bool = Field(default=False, alias="pauseGame")
    priority: int = 0

    # 表示設定
    event_prefab: Optional[str] = Field(default=None, alias="eventPrefab")
    notification_text: str = Field(default="", alias="notificationText")
    notification_icon: Optional[str] = Field(default=None, alias="notificationIcon")

    # 報酬
    unlock_menu: Optional[MenuType] = Field(default=None, alias="unlockMenu")
    reward_money: float = Field(default=0.0, alias="rewardMoney")
    reward_certificates: int = Field(default=0, alias="rewardCertificates")
    reward_items: List[ItemReward] = Field(default_factory=list, alias="rewardItems")

    class Config:
        populate_by_name = True
        use_enum_values = True
