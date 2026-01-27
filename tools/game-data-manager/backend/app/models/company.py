"""CompanyData model - 企業定義"""
from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field


class CompanyTrait(str, Enum):
    NONE = "None"
    TECH_INNOVATION = "TechInnovation"
    LOGISTICS = "Logistics"
    MILITARY = "Military"
    TRADING = "Trading"
    ARTS = "Arts"


class StockSector(str, Enum):
    TECH = "Tech"
    MILITARY = "Military"
    LOGISTICS = "Logistics"
    FINANCE = "Finance"
    ENTERTAINMENT = "Entertainment"
    RESOURCE = "Resource"


class StockHoldingBonus(BaseModel):
    """株式保有ボーナス (旧機能)"""
    threshold: float = 0.0
    bonus_type: str = Field(default="None", alias="bonusType")
    bonus_value: float = Field(default=0.0, alias="bonusValue")

    class Config:
        populate_by_name = True


class StockEventTrigger(BaseModel):
    """株価変動イベント"""
    event_id: str = Field(alias="eventId")
    trigger_condition: str = Field(default="", alias="triggerCondition")
    price_impact: float = Field(default=0.0, alias="priceImpact")

    class Config:
        populate_by_name = True


class OwnershipBonus(BaseModel):
    """経営権ボーナス"""
    threshold: float = 0.0
    bonus_type: str = Field(default="None", alias="bonusType")
    bonus_value: float = Field(default=0.0, alias="bonusValue")
    description: str = ""

    class Config:
        populate_by_name = True


class CompanyData(BaseModel):
    """企業データ"""
    # 基本情報
    id: str
    display_name: str = Field(alias="displayName")
    description: str = ""
    logo: Optional[str] = None

    # 表示設定
    chart_color: str = Field(default="#00FF00", alias="chartColor")
    theme_color: str = Field(default="#FFFFFF", alias="themeColor")
    sort_order: int = Field(default=0, alias="sortOrder")

    # 企業特性
    trait_type: CompanyTrait = Field(default=CompanyTrait.NONE, alias="traitType")
    trait_multiplier: float = Field(default=1.0, alias="traitMultiplier")

    # 株価設定
    initial_price: float = Field(default=1000.0, alias="initialPrice")
    min_price: float = Field(default=10.0, alias="minPrice")
    max_price: float = Field(default=0.0, alias="maxPrice")

    # 変動特性
    volatility: float = Field(default=0.1, ge=0.01, le=0.5)
    drift: float = Field(default=0.02, ge=-0.1, le=0.2)
    jump_probability: float = Field(default=0.01, ge=0.0, le=0.1, alias="jumpProbability")
    jump_intensity: float = Field(default=0.2, ge=0.1, le=0.5, alias="jumpIntensity")

    # 取引設定
    transaction_fee: float = Field(default=0.01, ge=0.0, le=0.05, alias="transactionFee")
    sector: StockSector = StockSector.TECH
    total_shares: int = Field(default=1000000, alias="totalShares")

    # 配当設定
    dividend_rate: float = Field(default=0.0, ge=0.0, le=0.1, alias="dividendRate")
    dividend_interval_seconds: int = Field(default=0, alias="dividendIntervalSeconds")

    # 保有ボーナス (旧機能)
    holding_bonuses: List[StockHoldingBonus] = Field(default_factory=list, alias="holdingBonuses")

    # 解放条件
    unlock_key_item_id: Optional[str] = Field(default=None, alias="unlockKeyItemId")

    # 株価変動イベント
    stock_events: List[StockEventTrigger] = Field(default_factory=list, alias="stockEvents")

    # 自社株設定
    is_player_company: bool = Field(default=False, alias="isPlayerCompany")
    can_sell: bool = Field(default=True, alias="canSell")
    buyback_penalty: float = Field(default=0.0, ge=0.0, le=0.5, alias="buybackPenalty")

    # 経営権ボーナス
    ownership_bonuses: List[OwnershipBonus] = Field(default_factory=list, alias="ownershipBonuses")

    # クリック連動設定
    active_click_bonus_rate: float = Field(default=0.02, ge=0.0, le=0.1, alias="activeClickBonusRate")
    idle_decay_rate: float = Field(default=0.005, ge=0.0, le=0.05, alias="idleDecayRate")
    active_click_threshold: int = Field(default=10, alias="activeClickThreshold")
    shareholder_meeting_interval: int = Field(default=600, alias="shareholderMeetingInterval")

    class Config:
        populate_by_name = True
        use_enum_values = True
