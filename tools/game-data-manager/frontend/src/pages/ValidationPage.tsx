import { useValidation } from '../hooks/useDataQuery';
import type { ReferenceError } from '../types';

const ERROR_LABELS: Record<string, { label: string; color: string }> = {
  missing_items: { label: 'アイテム参照エラー', color: 'text-green-400' },
  missing_upgrades: { label: 'アップグレード参照エラー', color: 'text-blue-400' },
  missing_companies: { label: '企業参照エラー', color: 'text-yellow-400' },
  missing_stocks: { label: '株式参照エラー', color: 'text-orange-400' },
  missing_events: { label: 'イベント参照エラー', color: 'text-purple-400' },
  missing_banners: { label: 'バナー参照エラー', color: 'text-pink-400' },
};

function ErrorList({ errors, label, color }: { errors: ReferenceError[]; label: string; color: string }) {
  if (errors.length === 0) return null;

  return (
    <div className="bg-ark-dark border border-gray-700 rounded-lg p-4">
      <h3 className={`text-lg font-medium mb-3 ${color}`}>
        {label} ({errors.length}件)
      </h3>
      <div className="space-y-2">
        {errors.map((err, i) => (
          <div key={i} className="bg-ark-darker p-3 rounded text-sm">
            <div className="flex items-center gap-2">
              <span className="text-gray-400">参照元:</span>
              <code className="text-ark-accent">{err.source}</code>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-gray-400">フィールド:</span>
              <code className="text-gray-300">{err.field}</code>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-gray-400">見つからないID:</span>
              <code className="text-red-400">{err.missing_id}</code>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function ValidationPage() {
  const { data: validation, isLoading, error, refetch } = useValidation();

  if (isLoading) {
    return <div className="text-center py-8">検証中...</div>;
  }

  if (error) {
    return <div className="text-center py-8 text-red-500">エラーが発生しました</div>;
  }

  const totalErrors = validation
    ? Object.values(validation).reduce((sum, arr) => sum + arr.length, 0)
    : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">参照整合性チェック</h2>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-ark-accent hover:bg-orange-600 rounded-lg transition-colors"
        >
          再チェック
        </button>
      </div>

      {/* サマリー */}
      <div className={`p-6 rounded-lg border ${totalErrors === 0 ? 'bg-green-900/20 border-green-700' : 'bg-red-900/20 border-red-700'}`}>
        {totalErrors === 0 ? (
          <div className="flex items-center gap-3">
            <span className="text-3xl">✓</span>
            <div>
              <h3 className="text-xl font-bold text-green-400">整合性OK</h3>
              <p className="text-gray-400">すべての参照が有効です</p>
            </div>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            <span className="text-3xl">⚠</span>
            <div>
              <h3 className="text-xl font-bold text-red-400">{totalErrors}件のエラー</h3>
              <p className="text-gray-400">無効な参照が見つかりました</p>
            </div>
          </div>
        )}
      </div>

      {/* エラー詳細 */}
      {validation && (
        <div className="space-y-4">
          {Object.entries(ERROR_LABELS).map(([key, { label, color }]) => (
            <ErrorList
              key={key}
              errors={validation[key as keyof typeof validation] || []}
              label={label}
              color={color}
            />
          ))}
        </div>
      )}

      {/* 説明 */}
      <div className="bg-ark-dark border border-gray-700 rounded-lg p-4">
        <h3 className="text-lg font-medium mb-3">チェック項目</h3>
        <ul className="space-y-2 text-sm text-gray-400">
          <li>• <strong>アップグレード</strong>: 解放条件アイテム、前提アップグレード、素材アイテム</li>
          <li>• <strong>ガチャ</strong>: 排出アイテム、ピックアップアイテム、前提バナー、解放アイテム</li>
          <li>• <strong>企業</strong>: 解放キーアイテム</li>
          <li>• <strong>イベント</strong>: 前提イベント、報酬アイテム</li>
        </ul>
      </div>
    </div>
  );
}
