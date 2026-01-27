# Game Data Manager

Arknights Cleaker のゲームデータを管理するWebアプリケーションです。

## 機能

- **アイテム管理**: KeyItem、素材、消耗品、衣装解放アイテムの管理
- **アップグレード管理**: クリック強化、収入強化、クリティカルなどの管理
- **ガチャ管理**: バナー設定、排出テーブル、天井システムの管理
- **企業/株式管理**: 株価設定、変動特性、配当設定の管理
- **イベント管理**: ゲームイベント、トリガー条件、報酬の管理
- **依存関係グラフ**: データ間の参照関係を視覚化
- **参照整合性チェック**: 無効な参照を検出

## 技術スタック

### バックエンド
- Python 3.10+
- FastAPI
- Pydantic

### フロントエンド
- React 18
- TypeScript
- TailwindCSS
- TanStack Query
- React Hook Form

## セットアップ

### 必要環境
- Python 3.10以上
- Node.js 18以上
- npm または yarn

### インストール

```bash
# リポジトリルートから
cd tools/game-data-manager

# 起動スクリプトに実行権限を付与
chmod +x start.sh

# 起動
./start.sh
```

### 手動起動

#### バックエンド
```bash
cd backend
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

#### フロントエンド
```bash
cd frontend
npm install
npm run dev
```

## 使い方

1. ブラウザで http://localhost:5173 を開く
2. 左サイドバーから管理したいカテゴリを選択
3. 「+ 新規作成」でデータを追加
4. テーブルの「編集」「削除」ボタンで既存データを操作

## API

APIドキュメントは http://localhost:8000/docs で確認できます。

### エンドポイント

| メソッド | パス | 説明 |
|---------|------|------|
| GET | /api/data/{type} | 全データ取得 |
| GET | /api/data/{type}/{id} | ID指定取得 |
| POST | /api/data/{type} | 新規作成 |
| PUT | /api/data/{type}/{id} | 更新 |
| DELETE | /api/data/{type}/{id} | 削除 |
| GET | /api/data/validation/references | 参照整合性チェック |
| GET | /api/data/graph/dependencies | 依存関係グラフ |
| GET | /api/data/export/all | 全データエクスポート |
| POST | /api/data/import/all | 全データインポート |

### データタイプ
- `items` - アイテム
- `upgrades` - アップグレード
- `gacha_banners` - ガチャバナー
- `companies` - 企業
- `stocks` - 株式
- `stock_prestiges` - 株式プレステージ
- `market_events` - マーケットイベント
- `game_events` - ゲームイベント

## データ連動

アップグレードやガチャなどで他のデータを参照する場合、
ドロップダウンから既存のデータを選択できます。

例:
- アップグレードの「必要アイテム」→ アイテム一覧から選択
- ガチャの「排出テーブル」→ アイテム一覧から選択
- イベントの「報酬アイテム」→ アイテム一覧から選択

## Unity連携 (TODO)

現在はJSON形式でデータを管理しています。
Unity ScriptableObjectとの双方向同期は今後実装予定です。

## ディレクトリ構造

```
game-data-manager/
├── backend/
│   ├── app/
│   │   ├── main.py           # FastAPIアプリ
│   │   ├── models/           # Pydanticモデル
│   │   ├── routers/          # APIルーター
│   │   └── services/         # ビジネスロジック
│   ├── data/                  # JSONデータ保存先
│   └── requirements.txt
├── frontend/
│   ├── src/
│   │   ├── components/       # 共通コンポーネント
│   │   ├── pages/            # 各ページ
│   │   ├── hooks/            # カスタムフック
│   │   ├── types/            # TypeScript型定義
│   │   └── utils/            # ユーティリティ
│   └── package.json
├── start.sh                   # 起動スクリプト
└── README.md
```
