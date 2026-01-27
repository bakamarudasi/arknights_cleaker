"""UpgradeData model - アップグレード定義"""
from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field


class UpgradeType(str, Enum):
    CLICK_FLAT_ADD = "Click_FlatAdd"
    CLICK_PERCENT_ADD = "Click_PercentAdd"
    INCOME_FLAT_ADD = "Income_FlatAdd"
    INCOME_PERCENT_ADD = "Income_PercentAdd"
    CRITICAL_CHANCE_ADD = "Critical_ChanceAdd"
    CRITICAL_POWER_ADD = "Critical_PowerAdd"
    SP_CHARGE_ADD = "SP_ChargeAdd"
    FEVER_POWER_ADD = "Fever_PowerAdd"


class UpgradeCategory(str, Enum):
    CLICK = "Click"
    INCOME = "Income"
    CRITICAL = "Critical"
    SKILL = "Skill"
    SPECIAL = "Special"


class CurrencyType(str, Enum):
    LMD = "LMD"
    CERTIFICATE = "Certificate"
    ORIGINIUM = "Originium"


class ItemCost(BaseModel):
    """素材コスト"""
    item_id: str = Field(alias="itemId")
    amount: int = 1

    class Config:
        populate_by_name = True


class UpgradeData(BaseModel):
    """アップグレードデータ"""
    # 基本情報 (BaseDataから継承)
    id: str
    display_name: str = Field(alias="displayName")
    description: str = ""
    icon: Optional[str] = None

    # 強化設定
    upgrade_type: UpgradeType = Field(alias="upgradeType")
    category: UpgradeCategory
    effect_value: float = Field(default=1.0, alias="effectValue")
    max_level: int = Field(default=10, alias="maxLevel")

    # コスト設定 (通貨)
    currency_type: CurrencyType = Field(default=CurrencyType.LMD, alias="currencyType")
    base_cost: float = Field(default=100.0, alias="baseCost")
    cost_multiplier: float = Field(default=1.15, alias="costMultiplier")

    # コスト設定 (素材)
    required_materials: List[ItemCost] = Field(default_factory=list, alias="requiredMaterials")
    material_scaling: float = Field(default=1.0, alias="materialScaling")

    # 解放条件
    required_unlock_item_id: Optional[str] = Field(default=None, alias="requiredUnlockItemId")
    prerequisite_upgrade_id: Optional[str] = Field(default=None, alias="prerequisiteUpgradeId")
    prerequisite_level: int = Field(default=1, alias="prerequisiteLevel")

    # 株式連動設定
    related_stock_id: Optional[str] = Field(default=None, alias="relatedStockId")
    scale_with_holding: bool = Field(default=False, alias="scaleWithHolding")
    max_holding_multiplier: float = Field(default=2.0, alias="maxHoldingMultiplier")

    # 表示設定
    sort_order: int = Field(default=0, alias="sortOrder")
    effect_format: str = Field(default="+{0}", alias="effectFormat")
    is_percent_display: bool = Field(default=False, alias="isPercentDisplay")
    category_icon: str = Field(default="", alias="categoryIcon")
    is_special: bool = Field(default=False, alias="isSpecial")

    class Config:
        populate_by_name = True
        use_enum_values = True
