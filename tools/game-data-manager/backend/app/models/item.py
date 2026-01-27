"""ItemData model - アイテム定義"""
from enum import Enum
from typing import Optional
from pydantic import BaseModel, Field


class ItemType(str, Enum):
    KEY_ITEM = "KeyItem"
    MATERIAL = "Material"
    CONSUMABLE = "Consumable"
    COSTUME_UNLOCK = "CostumeUnlock"


class Rarity(str, Enum):
    STAR1 = "Star1"
    STAR2 = "Star2"
    STAR3 = "Star3"
    STAR4 = "Star4"
    STAR5 = "Star5"
    STAR6 = "Star6"


class ConsumableType(str, Enum):
    NONE = "None"
    RECOVER_SP = "RecoverSP"
    BOOST_INCOME = "BoostIncome"
    INSTANT_MONEY = "InstantMoney"
    RECOVER_LENS_BATTERY = "RecoverLensBattery"


class LensFilterMode(str, Enum):
    NORMAL = "Normal"
    NIGHT_VISION = "NightVision"
    THERMO = "Thermo"
    XRAY = "XRay"
    MOSAIC = "Mosaic"


class LensSpecs(BaseModel):
    """レンズスペック (透視ガジェット用)"""
    is_lens: bool = Field(default=False, alias="isLens")
    view_radius: float = Field(default=100.0, alias="viewRadius")
    max_duration: float = Field(default=30.0, alias="maxDuration")
    penetrate_level: int = Field(default=1, ge=0, le=5, alias="penetrateLevel")
    filter_mode: LensFilterMode = Field(default=LensFilterMode.NORMAL, alias="filterMode")
    lens_mask: Optional[str] = Field(default=None, alias="lensMask")
    stability: float = Field(default=1.0)

    class Config:
        populate_by_name = True


class ItemData(BaseModel):
    """アイテムデータ"""
    # 基本情報 (BaseDataから継承)
    id: str
    display_name: str = Field(alias="displayName")
    description: str = ""
    icon: Optional[str] = None

    # 基本設定
    type: ItemType
    rarity: Rarity
    sort_order: int = Field(default=0, alias="sortOrder")
    max_stack: int = Field(default=-1, alias="maxStack")
    sell_price: int = Field(default=0, alias="sellPrice")

    # 演出設定
    use_sound: Optional[str] = Field(default=None, alias="useSound")

    # レンズスペック
    lens_specs: Optional[LensSpecs] = Field(default=None, alias="lensSpecs")

    # 消耗品設定
    use_effect: ConsumableType = Field(default=ConsumableType.NONE, alias="useEffect")
    effect_value: float = Field(default=0.0, alias="effectValue")
    effect_duration: float = Field(default=0.0, alias="effectDuration")

    # ガチャ被り設定
    convert_to_item_id: Optional[str] = Field(default=None, alias="convertToItemId")
    convert_amount: int = Field(default=1, alias="convertAmount")

    # 衣装解放設定
    target_character_id: Optional[str] = Field(default=None, alias="targetCharacterId")
    target_costume_index: int = Field(default=1, alias="targetCostumeIndex")

    # 表示設定
    effect_format: str = Field(default="+{0}", alias="effectFormat")
    is_percent_display: bool = Field(default=False, alias="isPercentDisplay")
    category_icon: str = Field(default="", alias="categoryIcon")
    is_special: bool = Field(default=False, alias="isSpecial")

    class Config:
        populate_by_name = True
        use_enum_values = True
