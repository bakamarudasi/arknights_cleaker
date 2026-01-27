"""Game Data Manager - FastAPI Backend"""
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .routers import data_router

app = FastAPI(
    title="Game Data Manager",
    description="Arknights Cleaker ゲームデータ管理API",
    version="1.0.0"
)

# CORS設定 (React開発サーバーからのアクセスを許可)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:3000", "http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ルーター登録
app.include_router(data_router)


@app.get("/")
async def root():
    return {
        "name": "Game Data Manager",
        "version": "1.0.0",
        "endpoints": {
            "items": "/api/data/items",
            "upgrades": "/api/data/upgrades",
            "gacha_banners": "/api/data/gacha_banners",
            "companies": "/api/data/companies",
            "stocks": "/api/data/stocks",
            "stock_prestiges": "/api/data/stock_prestiges",
            "market_events": "/api/data/market_events",
            "game_events": "/api/data/game_events",
            "validation": "/api/data/validation/references",
            "graph": "/api/data/graph/dependencies",
        }
    }


@app.get("/health")
async def health():
    return {"status": "ok"}
