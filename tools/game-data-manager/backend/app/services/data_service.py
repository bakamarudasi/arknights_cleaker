"""データサービス - JSON読み書きとバリデーション"""
import json
import os
from pathlib import Path
from typing import Dict, List, Any, Optional, TypeVar, Type
from pydantic import BaseModel

from ..models import (
    ItemData, UpgradeData, GachaBannerData, CompanyData,
    StockData, StockPrestigeData, MarketEventData, GameEventData
)

T = TypeVar('T', bound=BaseModel)

# データタイプとファイル名のマッピング
DATA_FILES = {
    "items": "items.json",
    "upgrades": "upgrades.json",
    "gacha_banners": "gacha_banners.json",
    "companies": "companies.json",
    "stocks": "stocks.json",
    "stock_prestiges": "stock_prestiges.json",
    "market_events": "market_events.json",
    "game_events": "game_events.json",
}

DATA_MODELS: Dict[str, Type[BaseModel]] = {
    "items": ItemData,
    "upgrades": UpgradeData,
    "gacha_banners": GachaBannerData,
    "companies": CompanyData,
    "stocks": StockData,
    "stock_prestiges": StockPrestigeData,
    "market_events": MarketEventData,
    "game_events": GameEventData,
}


class DataService:
    """データ管理サービス"""

    def __init__(self, data_dir: str = None):
        if data_dir is None:
            # デフォルトはbackend/dataフォルダ
            data_dir = Path(__file__).parent.parent.parent / "data"
        self.data_dir = Path(data_dir)
        self.data_dir.mkdir(parents=True, exist_ok=True)

        # キャッシュ
        self._cache: Dict[str, List[Dict]] = {}

    def _get_file_path(self, data_type: str) -> Path:
        """データタイプに対応するファイルパスを取得"""
        if data_type not in DATA_FILES:
            raise ValueError(f"Unknown data type: {data_type}")
        return self.data_dir / DATA_FILES[data_type]

    def _load_json(self, data_type: str) -> List[Dict]:
        """JSONファイルを読み込み"""
        file_path = self._get_file_path(data_type)
        if not file_path.exists():
            return []
        with open(file_path, 'r', encoding='utf-8') as f:
            return json.load(f)

    def _save_json(self, data_type: str, data: List[Dict]) -> None:
        """JSONファイルに保存"""
        file_path = self._get_file_path(data_type)
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        # キャッシュを更新
        self._cache[data_type] = data

    # ========================================
    # CRUD操作
    # ========================================

    def get_all(self, data_type: str) -> List[Dict]:
        """全データを取得"""
        if data_type not in self._cache:
            self._cache[data_type] = self._load_json(data_type)
        return self._cache[data_type]

    def get_by_id(self, data_type: str, item_id: str) -> Optional[Dict]:
        """IDでデータを取得"""
        id_field = self._get_id_field(data_type)
        data = self.get_all(data_type)
        for item in data:
            if item.get(id_field) == item_id:
                return item
        return None

    def create(self, data_type: str, item: Dict) -> Dict:
        """新規データ作成"""
        data = self.get_all(data_type)

        # IDの重複チェック
        id_field = self._get_id_field(data_type)
        item_id = item.get(id_field)
        if any(d.get(id_field) == item_id for d in data):
            raise ValueError(f"Duplicate ID: {item_id}")

        # バリデーション
        model_class = DATA_MODELS[data_type]
        validated = model_class(**item)
        validated_dict = validated.model_dump(by_alias=True, exclude_none=True)

        data.append(validated_dict)
        self._save_json(data_type, data)
        return validated_dict

    def update(self, data_type: str, item_id: str, item: Dict) -> Dict:
        """データ更新"""
        data = self.get_all(data_type)
        id_field = self._get_id_field(data_type)

        # バリデーション
        model_class = DATA_MODELS[data_type]
        validated = model_class(**item)
        validated_dict = validated.model_dump(by_alias=True, exclude_none=True)

        # 更新
        for i, d in enumerate(data):
            if d.get(id_field) == item_id:
                data[i] = validated_dict
                self._save_json(data_type, data)
                return validated_dict

        raise ValueError(f"Not found: {item_id}")

    def delete(self, data_type: str, item_id: str) -> bool:
        """データ削除"""
        data = self.get_all(data_type)
        id_field = self._get_id_field(data_type)

        for i, d in enumerate(data):
            if d.get(id_field) == item_id:
                data.pop(i)
                self._save_json(data_type, data)
                return True
        return False

    def bulk_create(self, data_type: str, items: List[Dict]) -> List[Dict]:
        """一括作成"""
        results = []
        for item in items:
            results.append(self.create(data_type, item))
        return results

    def _get_id_field(self, data_type: str) -> str:
        """データタイプに対応するIDフィールド名を取得"""
        id_fields = {
            "items": "id",
            "upgrades": "id",
            "gacha_banners": "bannerId",
            "companies": "id",
            "stocks": "stockId",
            "stock_prestiges": "id",
            "market_events": "eventId",
            "game_events": "eventId",
        }
        return id_fields.get(data_type, "id")

    # ========================================
    # 参照整合性チェック
    # ========================================

    def check_references(self) -> Dict[str, List[Dict]]:
        """全データの参照整合性をチェック"""
        errors = {
            "missing_items": [],
            "missing_upgrades": [],
            "missing_companies": [],
            "missing_stocks": [],
            "missing_events": [],
            "missing_banners": [],
        }

        # 全IDを収集
        item_ids = {d["id"] for d in self.get_all("items")}
        upgrade_ids = {d["id"] for d in self.get_all("upgrades")}
        company_ids = {d["id"] for d in self.get_all("companies")}
        stock_ids = {d["stockId"] for d in self.get_all("stocks")}
        event_ids = {d["eventId"] for d in self.get_all("game_events")}
        banner_ids = {d["bannerId"] for d in self.get_all("gacha_banners")}

        # アップグレードの参照チェック
        for upgrade in self.get_all("upgrades"):
            # 解放条件アイテム
            if ref := upgrade.get("requiredUnlockItemId"):
                if ref not in item_ids:
                    errors["missing_items"].append({
                        "source": f"upgrade:{upgrade['id']}",
                        "field": "requiredUnlockItemId",
                        "missing_id": ref
                    })
            # 前提アップグレード
            if ref := upgrade.get("prerequisiteUpgradeId"):
                if ref not in upgrade_ids:
                    errors["missing_upgrades"].append({
                        "source": f"upgrade:{upgrade['id']}",
                        "field": "prerequisiteUpgradeId",
                        "missing_id": ref
                    })
            # 素材アイテム
            for mat in upgrade.get("requiredMaterials", []):
                if mat.get("itemId") not in item_ids:
                    errors["missing_items"].append({
                        "source": f"upgrade:{upgrade['id']}",
                        "field": "requiredMaterials",
                        "missing_id": mat.get("itemId")
                    })

        # ガチャの参照チェック
        for banner in self.get_all("gacha_banners"):
            # 排出アイテム
            for entry in banner.get("pool", []):
                if entry.get("itemId") not in item_ids:
                    errors["missing_items"].append({
                        "source": f"gacha:{banner['bannerId']}",
                        "field": "pool",
                        "missing_id": entry.get("itemId")
                    })
            # ピックアップアイテム
            for item_id in banner.get("pickupItemIds", []):
                if item_id not in item_ids:
                    errors["missing_items"].append({
                        "source": f"gacha:{banner['bannerId']}",
                        "field": "pickupItemIds",
                        "missing_id": item_id
                    })
            # 前提バナー
            if ref := banner.get("prerequisiteBannerId"):
                if ref not in banner_ids:
                    errors["missing_banners"].append({
                        "source": f"gacha:{banner['bannerId']}",
                        "field": "prerequisiteBannerId",
                        "missing_id": ref
                    })

        # 企業の参照チェック
        for company in self.get_all("companies"):
            if ref := company.get("unlockKeyItemId"):
                if ref not in item_ids:
                    errors["missing_items"].append({
                        "source": f"company:{company['id']}",
                        "field": "unlockKeyItemId",
                        "missing_id": ref
                    })

        # ゲームイベントの参照チェック
        for event in self.get_all("game_events"):
            if ref := event.get("prerequisiteEventId"):
                if ref not in event_ids:
                    errors["missing_events"].append({
                        "source": f"event:{event['eventId']}",
                        "field": "prerequisiteEventId",
                        "missing_id": ref
                    })
            for reward in event.get("rewardItems", []):
                if reward.get("itemId") not in item_ids:
                    errors["missing_items"].append({
                        "source": f"event:{event['eventId']}",
                        "field": "rewardItems",
                        "missing_id": reward.get("itemId")
                    })

        return errors

    # ========================================
    # 依存関係グラフ
    # ========================================

    def get_dependency_graph(self) -> Dict[str, Any]:
        """依存関係グラフを生成"""
        nodes = []
        edges = []

        # ノード追加
        for item in self.get_all("items"):
            nodes.append({"id": f"item:{item['id']}", "type": "item", "label": item.get("displayName", item["id"])})
        for upgrade in self.get_all("upgrades"):
            nodes.append({"id": f"upgrade:{upgrade['id']}", "type": "upgrade", "label": upgrade.get("displayName", upgrade["id"])})
        for banner in self.get_all("gacha_banners"):
            nodes.append({"id": f"gacha:{banner['bannerId']}", "type": "gacha", "label": banner.get("bannerName", banner["bannerId"])})
        for company in self.get_all("companies"):
            nodes.append({"id": f"company:{company['id']}", "type": "company", "label": company.get("displayName", company["id"])})
        for event in self.get_all("game_events"):
            nodes.append({"id": f"event:{event['eventId']}", "type": "event", "label": event.get("eventName", event["eventId"])})

        # エッジ追加
        for upgrade in self.get_all("upgrades"):
            if ref := upgrade.get("requiredUnlockItemId"):
                edges.append({"from": f"upgrade:{upgrade['id']}", "to": f"item:{ref}", "type": "unlock"})
            if ref := upgrade.get("prerequisiteUpgradeId"):
                edges.append({"from": f"upgrade:{upgrade['id']}", "to": f"upgrade:{ref}", "type": "prerequisite"})
            for mat in upgrade.get("requiredMaterials", []):
                edges.append({"from": f"upgrade:{upgrade['id']}", "to": f"item:{mat['itemId']}", "type": "material"})

        for banner in self.get_all("gacha_banners"):
            for entry in banner.get("pool", []):
                edges.append({"from": f"gacha:{banner['bannerId']}", "to": f"item:{entry['itemId']}", "type": "contains"})

        for company in self.get_all("companies"):
            if ref := company.get("unlockKeyItemId"):
                edges.append({"from": f"company:{company['id']}", "to": f"item:{ref}", "type": "unlock"})

        for event in self.get_all("game_events"):
            if ref := event.get("prerequisiteEventId"):
                edges.append({"from": f"event:{event['eventId']}", "to": f"event:{ref}", "type": "prerequisite"})
            for reward in event.get("rewardItems", []):
                edges.append({"from": f"event:{event['eventId']}", "to": f"item:{reward['itemId']}", "type": "reward"})

        return {"nodes": nodes, "edges": edges}


# シングルトンインスタンス
_service: Optional[DataService] = None


def get_data_service() -> DataService:
    """データサービスのシングルトンを取得"""
    global _service
    if _service is None:
        _service = DataService()
    return _service
