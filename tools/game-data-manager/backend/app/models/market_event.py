"""MarketEventData model - マーケットイベント定義"""
from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field
from .company import StockSector


class EventSeverity(str, Enum):
    MINOR = "Minor"
    NORMAL = "Normal"
    MAJOR = "Major"
    CRITICAL = "Critical"


class SectorImpact(BaseModel):
    """セクター影響"""
    sector: StockSector
    impact: float = Field(default=0.0, ge=-0.5, le=0.5)

    class Config:
        populate_by_name = True
        use_enum_values = True


class CompanyImpact(BaseModel):
    """企業影響"""
    company_id: str = Field(alias="companyId")
    impact: float = Field(default=0.0, ge=-0.5, le=0.5)

    class Config:
        populate_by_name = True


class MarketEventData(BaseModel):
    """マーケットイベントデータ"""
    # 基本情報
    event_id: str = Field(alias="eventId")
    event_name: str = Field(alias="eventName")
    description: str = ""
    icon: Optional[str] = None

    # 影響設定
    global_impact: float = Field(default=0.0, ge=-0.5, le=0.5, alias="globalImpact")
    sector_impacts: List[SectorImpact] = Field(default_factory=list, alias="sectorImpacts")
    company_impacts: List[CompanyImpact] = Field(default_factory=list, alias="companyImpacts")

    # 発生条件
    daily_probability: float = Field(default=0.05, ge=0.0, le=1.0, alias="dailyProbability")
    duration_seconds: float = Field(default=600.0, alias="durationSeconds")
    severity: EventSeverity = EventSeverity.NORMAL

    class Config:
        populate_by_name = True
        use_enum_values = True
