"""データAPI Router"""
from typing import List, Dict, Any
from fastapi import APIRouter, HTTPException, Query

from ..services.data_service import get_data_service

router = APIRouter(prefix="/api/data", tags=["data"])

# 有効なデータタイプ
VALID_DATA_TYPES = [
    "items", "upgrades", "gacha_banners", "companies",
    "stocks", "stock_prestiges", "market_events", "game_events"
]


def validate_data_type(data_type: str):
    if data_type not in VALID_DATA_TYPES:
        raise HTTPException(status_code=400, detail=f"Invalid data type: {data_type}")


# ========================================
# 全データ取得
# ========================================

@router.get("/{data_type}")
async def get_all(data_type: str) -> List[Dict]:
    """指定タイプの全データを取得"""
    validate_data_type(data_type)
    service = get_data_service()
    return service.get_all(data_type)


@router.get("/{data_type}/{item_id}")
async def get_by_id(data_type: str, item_id: str) -> Dict:
    """IDでデータを取得"""
    validate_data_type(data_type)
    service = get_data_service()
    result = service.get_by_id(data_type, item_id)
    if result is None:
        raise HTTPException(status_code=404, detail=f"Not found: {item_id}")
    return result


# ========================================
# 作成・更新・削除
# ========================================

@router.post("/{data_type}")
async def create(data_type: str, item: Dict) -> Dict:
    """新規データ作成"""
    validate_data_type(data_type)
    service = get_data_service()
    try:
        return service.create(data_type, item)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.put("/{data_type}/{item_id}")
async def update(data_type: str, item_id: str, item: Dict) -> Dict:
    """データ更新"""
    validate_data_type(data_type)
    service = get_data_service()
    try:
        return service.update(data_type, item_id, item)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.delete("/{data_type}/{item_id}")
async def delete(data_type: str, item_id: str) -> Dict[str, bool]:
    """データ削除"""
    validate_data_type(data_type)
    service = get_data_service()
    success = service.delete(data_type, item_id)
    if not success:
        raise HTTPException(status_code=404, detail=f"Not found: {item_id}")
    return {"success": True}


@router.post("/{data_type}/bulk")
async def bulk_create(data_type: str, items: List[Dict]) -> List[Dict]:
    """一括作成"""
    validate_data_type(data_type)
    service = get_data_service()
    try:
        return service.bulk_create(data_type, items)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


# ========================================
# 参照整合性 & 依存関係
# ========================================

@router.get("/validation/references")
async def check_references() -> Dict[str, List[Dict]]:
    """参照整合性チェック"""
    service = get_data_service()
    return service.check_references()


@router.get("/graph/dependencies")
async def get_dependency_graph() -> Dict[str, Any]:
    """依存関係グラフを取得"""
    service = get_data_service()
    return service.get_dependency_graph()


# ========================================
# エクスポート/インポート
# ========================================

@router.get("/export/all")
async def export_all() -> Dict[str, List[Dict]]:
    """全データをエクスポート"""
    service = get_data_service()
    return {
        data_type: service.get_all(data_type)
        for data_type in VALID_DATA_TYPES
    }


@router.post("/import/all")
async def import_all(data: Dict[str, List[Dict]]) -> Dict[str, int]:
    """全データをインポート"""
    service = get_data_service()
    counts = {}
    for data_type, items in data.items():
        if data_type in VALID_DATA_TYPES:
            # 既存データをクリアして新規作成
            for existing in service.get_all(data_type):
                id_field = service._get_id_field(data_type)
                service.delete(data_type, existing[id_field])
            for item in items:
                service.create(data_type, item)
            counts[data_type] = len(items)
    return counts
