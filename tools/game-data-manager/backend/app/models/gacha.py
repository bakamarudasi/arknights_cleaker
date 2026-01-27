"""GachaBannerData model - ガチャ定義"""
from typing import List, Optional
from pydantic import BaseModel, Field
from .upgrade import CurrencyType


class GachaPoolEntry(BaseModel):
    """排出テーブルエントリ"""
    item_id: str = Field(alias="itemId")
    weight: float = Field(default=1.0, ge=0.01, le=100.0)
    is_pickup: bool = Field(default=False, alias="isPickup")
    stock_count: int = Field(default=0, alias="stockCount")

    class Config:
        populate_by_name = True


class GachaBannerData(BaseModel):
    """ガチャバナーデータ"""
    # 基本情報
    banner_id: str = Field(alias="bannerId")
    banner_name: str = Field(alias="bannerName")
    description: str = ""
    banner_sprite: Optional[str] = Field(default=None, alias="bannerSprite")
    is_limited: bool = Field(default=False, alias="isLimited")

    # コスト設定
    currency_type: CurrencyType = Field(default=CurrencyType.CERTIFICATE, alias="currencyType")
    cost_single: float = Field(default=600.0, alias="costSingle")
    cost_ten: float = Field(default=6000.0, alias="costTen")

    # 天井システム
    has_pity: bool = Field(default=False, alias="hasPity")
    pity_count: int = Field(default=50, alias="pityCount")
    soft_pity_start: int = Field(default=40, alias="softPityStart")

    # 排出テーブル
    pool: List[GachaPoolEntry] = Field(default_factory=list)

    # ピックアップ
    pickup_item_ids: List[str] = Field(default_factory=list, alias="pickupItemIds")
    pickup_rate_boost: float = Field(default=0.5, ge=0.0, le=1.0, alias="pickupRateBoost")

    # 解放条件
    starts_locked: bool = Field(default=False, alias="startsLocked")
    prerequisite_banner_id: Optional[str] = Field(default=None, alias="prerequisiteBannerId")
    required_unlock_item_id: Optional[str] = Field(default=None, alias="requiredUnlockItemId")

    class Config:
        populate_by_name = True
        use_enum_values = True
