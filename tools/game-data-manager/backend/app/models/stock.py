"""StockData & StockPrestigeData models - 株式定義"""
from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field


class StockData(BaseModel):
    """銘柄データ (CompanyDataを参照)"""
    # 基本情報
    stock_id: str = Field(alias="stockId")
    company_id: str = Field(alias="companyId")
    stock_id_override: Optional[str] = Field(default=None, alias="stockIdOverride")

    class Config:
        populate_by_name = True


class PrestigeBonusType(str, Enum):
    CLICK_EFFICIENCY = "ClickEfficiency"
    AUTO_INCOME = "AutoIncome"
    CRITICAL_RATE = "CriticalRate"
    CRITICAL_POWER = "CriticalPower"
    SP_CHARGE_SPEED = "SPChargeSpeed"
    FEVER_POWER = "FeverPower"
    SELL_PRICE_BONUS = "SellPriceBonus"
    GACHA_COST_REDUCTION = "GachaCostReduction"
    UPGRADE_COST_REDUCTION = "UpgradeCostReduction"
    DIVIDEND_BONUS = "DividendBonus"


class PrestigeBonus(BaseModel):
    """周回ボーナス"""
    bonus_type: PrestigeBonusType = Field(alias="bonusType")
    value_per_level: float = Field(default=0.05, alias="valuePerLevel")
    description: str = ""

    class Config:
        populate_by_name = True
        use_enum_values = True


class StockPrestigeData(BaseModel):
    """株式プレステージデータ"""
    # 基本情報
    id: str
    target_stock_id: str = Field(alias="targetStockId")

    # 周回設定
    shares_multiplier: float = Field(default=1.5, ge=1.1, le=3.0, alias="sharesMultiplier")
    max_prestige_level: int = Field(default=0, alias="maxPrestigeLevel")

    # 永続ボーナス
    prestige_bonuses: List[PrestigeBonus] = Field(default_factory=list, alias="prestigeBonuses")

    # 買収完了演出
    acquisition_message: str = Field(default="{0}の買収が完了しました！", alias="acquisitionMessage")
    acquisition_sound: Optional[str] = Field(default=None, alias="acquisitionSound")

    class Config:
        populate_by_name = True
