// ========================================
// 共通型定義
// ========================================

export type DataType =
  | 'items'
  | 'upgrades'
  | 'gacha_banners'
  | 'companies'
  | 'stocks'
  | 'stock_prestiges'
  | 'market_events'
  | 'game_events';

// ========================================
// Item
// ========================================

export type ItemType = 'KeyItem' | 'Material' | 'Consumable' | 'CostumeUnlock';
export type Rarity = 'Star1' | 'Star2' | 'Star3' | 'Star4' | 'Star5' | 'Star6';
export type ConsumableType = 'None' | 'RecoverSP' | 'BoostIncome' | 'InstantMoney' | 'RecoverLensBattery';
export type LensFilterMode = 'Normal' | 'NightVision' | 'Thermo' | 'XRay' | 'Mosaic';

export interface LensSpecs {
  isLens: boolean;
  viewRadius: number;
  maxDuration: number;
  penetrateLevel: number;
  filterMode: LensFilterMode;
  lensMask?: string;
  stability: number;
}

export interface ItemData {
  id: string;
  displayName: string;
  description: string;
  icon?: string;
  type: ItemType;
  rarity: Rarity;
  sortOrder: number;
  maxStack: number;
  sellPrice: number;
  useSound?: string;
  lensSpecs?: LensSpecs;
  useEffect: ConsumableType;
  effectValue: number;
  effectDuration: number;
  convertToItemId?: string;
  convertAmount: number;
  targetCharacterId?: string;
  targetCostumeIndex: number;
  effectFormat: string;
  isPercentDisplay: boolean;
  categoryIcon: string;
  isSpecial: boolean;
}

// ========================================
// Upgrade
// ========================================

export type UpgradeType =
  | 'Click_FlatAdd' | 'Click_PercentAdd'
  | 'Income_FlatAdd' | 'Income_PercentAdd'
  | 'Critical_ChanceAdd' | 'Critical_PowerAdd'
  | 'SP_ChargeAdd' | 'Fever_PowerAdd';

export type UpgradeCategory = 'Click' | 'Income' | 'Critical' | 'Skill' | 'Special';
export type CurrencyType = 'LMD' | 'Certificate' | 'Originium';

export interface ItemCost {
  itemId: string;
  amount: number;
}

export interface UpgradeData {
  id: string;
  displayName: string;
  description: string;
  icon?: string;
  upgradeType: UpgradeType;
  category: UpgradeCategory;
  effectValue: number;
  maxLevel: number;
  currencyType: CurrencyType;
  baseCost: number;
  costMultiplier: number;
  requiredMaterials: ItemCost[];
  materialScaling: number;
  requiredUnlockItemId?: string;
  prerequisiteUpgradeId?: string;
  prerequisiteLevel: number;
  relatedStockId?: string;
  scaleWithHolding: boolean;
  maxHoldingMultiplier: number;
  sortOrder: number;
  effectFormat: string;
  isPercentDisplay: boolean;
  categoryIcon: string;
  isSpecial: boolean;
}

// ========================================
// Gacha
// ========================================

export interface GachaPoolEntry {
  itemId: string;
  weight: number;
  isPickup: boolean;
  stockCount: number;
}

export interface GachaBannerData {
  bannerId: string;
  bannerName: string;
  description: string;
  bannerSprite?: string;
  isLimited: boolean;
  currencyType: CurrencyType;
  costSingle: number;
  costTen: number;
  hasPity: boolean;
  pityCount: number;
  softPityStart: number;
  pool: GachaPoolEntry[];
  pickupItemIds: string[];
  pickupRateBoost: number;
  startsLocked: boolean;
  prerequisiteBannerId?: string;
  requiredUnlockItemId?: string;
}

// ========================================
// Company & Stock
// ========================================

export type CompanyTrait = 'None' | 'TechInnovation' | 'Logistics' | 'Military' | 'Trading' | 'Arts';
export type StockSector = 'Tech' | 'Military' | 'Logistics' | 'Finance' | 'Entertainment' | 'Resource';

export interface CompanyData {
  id: string;
  displayName: string;
  description: string;
  logo?: string;
  chartColor: string;
  themeColor: string;
  sortOrder: number;
  traitType: CompanyTrait;
  traitMultiplier: number;
  initialPrice: number;
  minPrice: number;
  maxPrice: number;
  volatility: number;
  drift: number;
  jumpProbability: number;
  jumpIntensity: number;
  transactionFee: number;
  sector: StockSector;
  totalShares: number;
  dividendRate: number;
  dividendIntervalSeconds: number;
  unlockKeyItemId?: string;
  isPlayerCompany: boolean;
  canSell: boolean;
}

export interface StockData {
  stockId: string;
  companyId: string;
  stockIdOverride?: string;
}

// ========================================
// Stock Prestige
// ========================================

export type PrestigeBonusType =
  | 'ClickEfficiency' | 'AutoIncome' | 'CriticalRate' | 'CriticalPower'
  | 'SPChargeSpeed' | 'FeverPower' | 'SellPriceBonus'
  | 'GachaCostReduction' | 'UpgradeCostReduction' | 'DividendBonus';

export interface PrestigeBonus {
  bonusType: PrestigeBonusType;
  valuePerLevel: number;
  description: string;
}

export interface StockPrestigeData {
  id: string;
  targetStockId: string;
  sharesMultiplier: number;
  maxPrestigeLevel: number;
  prestigeBonuses: PrestigeBonus[];
  acquisitionMessage: string;
  acquisitionSound?: string;
}

// ========================================
// Market Event
// ========================================

export type EventSeverity = 'Minor' | 'Normal' | 'Major' | 'Critical';

export interface SectorImpact {
  sector: StockSector;
  impact: number;
}

export interface CompanyImpact {
  companyId: string;
  impact: number;
}

export interface MarketEventData {
  eventId: string;
  eventName: string;
  description: string;
  icon?: string;
  globalImpact: number;
  sectorImpacts: SectorImpact[];
  companyImpacts: CompanyImpact[];
  dailyProbability: number;
  durationSeconds: number;
  severity: EventSeverity;
}

// ========================================
// Game Event
// ========================================

export type EventTriggerType =
  | 'None' | 'MoneyReached' | 'ClickCount' | 'UpgradePurchased'
  | 'ItemObtained' | 'TimeElapsed' | 'AffectionLevel' | 'StockOwned';

export type MenuType = 'Shop' | 'Inventory' | 'Gacha' | 'Market' | 'Settings' | 'Operator';

export interface ItemReward {
  itemId: string;
  amount: number;
}

export interface GameEventData {
  eventId: string;
  eventName: string;
  description: string;
  triggerType: EventTriggerType;
  triggerValue: number;
  requireId?: string;
  prerequisiteEventId?: string;
  oneTimeOnly: boolean;
  pauseGame: boolean;
  priority: number;
  eventPrefab?: string;
  notificationText: string;
  notificationIcon?: string;
  unlockMenu?: MenuType;
  rewardMoney: number;
  rewardCertificates: number;
  rewardItems: ItemReward[];
}

// ========================================
// Graph
// ========================================

export interface GraphNode {
  id: string;
  type: string;
  label: string;
}

export interface GraphEdge {
  from: string;
  to: string;
  type: string;
}

export interface DependencyGraph {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

// ========================================
// Validation
// ========================================

export interface ReferenceError {
  source: string;
  field: string;
  missing_id: string;
}

export interface ValidationResult {
  missing_items: ReferenceError[];
  missing_upgrades: ReferenceError[];
  missing_companies: ReferenceError[];
  missing_stocks: ReferenceError[];
  missing_events: ReferenceError[];
  missing_banners: ReferenceError[];
}
