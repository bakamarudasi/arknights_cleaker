#!/bin/bash

# Game Data Manager 起動スクリプト

echo "==================================="
echo " Game Data Manager"
echo " Arknights Cleaker"
echo "==================================="

# カレントディレクトリを取得
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Python仮想環境をチェック/作成
if [ ! -d "backend/venv" ]; then
    echo "Python仮想環境を作成中..."
    python3 -m venv backend/venv
fi

# 依存関係インストール
echo "バックエンド依存関係をインストール中..."
source backend/venv/bin/activate
pip install -q -r backend/requirements.txt

# フロントエンド依存関係
if [ ! -d "frontend/node_modules" ]; then
    echo "フロントエンド依存関係をインストール中..."
    cd frontend && npm install && cd ..
fi

# バックエンド起動 (バックグラウンド)
echo "バックエンドを起動中 (port 8000)..."
cd backend
source venv/bin/activate
uvicorn app.main:app --reload --port 8000 &
BACKEND_PID=$!
cd ..

# フロントエンド起動
echo "フロントエンドを起動中 (port 5173)..."
cd frontend
npm run dev &
FRONTEND_PID=$!
cd ..

echo ""
echo "==================================="
echo " 起動完了!"
echo " フロントエンド: http://localhost:5173"
echo " バックエンドAPI: http://localhost:8000"
echo " APIドキュメント: http://localhost:8000/docs"
echo "==================================="
echo ""
echo "終了するには Ctrl+C を押してください"

# 終了処理
cleanup() {
    echo ""
    echo "シャットダウン中..."
    kill $BACKEND_PID 2>/dev/null
    kill $FRONTEND_PID 2>/dev/null
    exit 0
}

trap cleanup SIGINT SIGTERM

# 待機
wait
