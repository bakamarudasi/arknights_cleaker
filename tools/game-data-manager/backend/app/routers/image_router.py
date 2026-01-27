"""画像アップロード・管理 Router"""
import os
import shutil
from pathlib import Path
from typing import List
from fastapi import APIRouter, UploadFile, File, HTTPException
from fastapi.responses import FileResponse

router = APIRouter(prefix="/api/images", tags=["images"])

# 画像保存ディレクトリ（Unityプロジェクトのパスも設定可能）
IMAGES_DIR = Path(__file__).parent.parent.parent / "data" / "images"
IMAGES_DIR.mkdir(parents=True, exist_ok=True)

# カテゴリ別サブフォルダ
CATEGORIES = ["items", "upgrades", "gacha", "companies", "events", "characters"]

# 許可する拡張子
ALLOWED_EXTENSIONS = {".png", ".jpg", ".jpeg", ".gif", ".webp"}


def get_category_path(category: str) -> Path:
    """カテゴリのパスを取得"""
    if category not in CATEGORIES:
        raise HTTPException(status_code=400, detail=f"Invalid category: {category}")
    path = IMAGES_DIR / category
    path.mkdir(exist_ok=True)
    return path


@router.get("/categories")
async def list_categories() -> List[str]:
    """利用可能なカテゴリ一覧"""
    return CATEGORIES


@router.get("/{category}")
async def list_images(category: str) -> List[dict]:
    """カテゴリ内の画像一覧"""
    path = get_category_path(category)
    images = []
    for file in path.iterdir():
        if file.suffix.lower() in ALLOWED_EXTENSIONS:
            images.append({
                "name": file.name,
                "path": f"/api/images/{category}/{file.name}",
                "size": file.stat().st_size,
            })
    return sorted(images, key=lambda x: x["name"])


@router.get("/{category}/{filename}")
async def get_image(category: str, filename: str):
    """画像ファイルを取得"""
    path = get_category_path(category) / filename
    if not path.exists():
        raise HTTPException(status_code=404, detail="Image not found")
    return FileResponse(path)


@router.post("/{category}")
async def upload_image(category: str, file: UploadFile = File(...)) -> dict:
    """画像をアップロード"""
    # 拡張子チェック
    ext = Path(file.filename).suffix.lower()
    if ext not in ALLOWED_EXTENSIONS:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid file type. Allowed: {', '.join(ALLOWED_EXTENSIONS)}"
        )

    # ファイル名をサニタイズ
    safe_name = "".join(c for c in file.filename if c.isalnum() or c in "._-")
    if not safe_name:
        safe_name = "image" + ext

    # 保存
    path = get_category_path(category) / safe_name

    # 同名ファイルがあればリネーム
    counter = 1
    original_stem = path.stem
    while path.exists():
        path = path.with_name(f"{original_stem}_{counter}{ext}")
        counter += 1

    with open(path, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    return {
        "name": path.name,
        "path": f"/api/images/{category}/{path.name}",
        "size": path.stat().st_size,
    }


@router.delete("/{category}/{filename}")
async def delete_image(category: str, filename: str) -> dict:
    """画像を削除"""
    path = get_category_path(category) / filename
    if not path.exists():
        raise HTTPException(status_code=404, detail="Image not found")
    path.unlink()
    return {"success": True}


# ========================================
# Unityプロジェクト連携
# ========================================

# Unityプロジェクトのパス（設定で変更可能にする）
UNITY_PROJECT_PATH: Path | None = None


@router.post("/sync/unity")
async def sync_from_unity(unity_path: str) -> dict:
    """Unityプロジェクトから画像を同期"""
    global UNITY_PROJECT_PATH
    UNITY_PROJECT_PATH = Path(unity_path)

    if not UNITY_PROJECT_PATH.exists():
        raise HTTPException(status_code=400, detail="Unity project path not found")

    # Resources/Iconsなどから画像をコピー
    icons_path = UNITY_PROJECT_PATH / "Assets" / "Resources" / "Icons"
    if not icons_path.exists():
        return {"synced": 0, "message": "No Icons folder found in Unity project"}

    synced = 0
    for category in CATEGORIES:
        category_path = icons_path / category
        if category_path.exists():
            dest_path = get_category_path(category)
            for file in category_path.iterdir():
                if file.suffix.lower() in ALLOWED_EXTENSIONS:
                    shutil.copy2(file, dest_path / file.name)
                    synced += 1

    return {"synced": synced, "message": f"Synced {synced} images from Unity project"}


@router.get("/unity/path")
async def get_unity_path() -> dict:
    """現在設定されているUnityプロジェクトパス"""
    return {
        "path": str(UNITY_PROJECT_PATH) if UNITY_PROJECT_PATH else None,
        "exists": UNITY_PROJECT_PATH.exists() if UNITY_PROJECT_PATH else False,
    }
