@echo off
chcp 65001 > nul
echo ===================================
echo  Game Data Manager
echo  Arknights Cleaker
echo ===================================

cd /d "%~dp0"

echo.
echo バックエンド依存関係をインストール中...
cd backend
pip install -r requirements.txt

echo.
echo バックエンドを起動中 (port 8000)...
start "Backend" cmd /k "uvicorn app.main:app --reload --port 8000"

cd ..

echo.
echo フロントエンド依存関係をインストール中...
cd frontend
call npm install

echo.
echo フロントエンドを起動中 (port 5173)...
start "Frontend" cmd /k "npm run dev"

cd ..

echo.
echo ===================================
echo  起動完了!
echo  フロントエンド: http://localhost:5173
echo  バックエンドAPI: http://localhost:8000
echo  APIドキュメント: http://localhost:8000/docs
echo ===================================
echo.
pause
