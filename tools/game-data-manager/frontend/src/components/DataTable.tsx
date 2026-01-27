import { useState } from 'react';
import clsx from 'clsx';

interface Column<T> {
  key: keyof T | string;
  header: string;
  render?: (item: T) => React.ReactNode;
  width?: string;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  idField: keyof T;
  onEdit?: (item: T) => void;
  onDelete?: (id: string) => void;
  onRowClick?: (item: T) => void;
}

export function DataTable<T extends Record<string, unknown>>({
  data,
  columns,
  idField,
  onEdit,
  onDelete,
  onRowClick,
}: DataTableProps<T>) {
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortAsc, setSortAsc] = useState(true);
  const [filter, setFilter] = useState('');

  const filteredData = data.filter((item) =>
    Object.values(item).some((val) =>
      String(val).toLowerCase().includes(filter.toLowerCase())
    )
  );

  const sortedData = sortKey
    ? [...filteredData].sort((a, b) => {
        const aVal = a[sortKey as keyof T];
        const bVal = b[sortKey as keyof T];
        if (aVal === bVal) return 0;
        const cmp = aVal < bVal ? -1 : 1;
        return sortAsc ? cmp : -cmp;
      })
    : filteredData;

  const handleSort = (key: string) => {
    if (sortKey === key) {
      setSortAsc(!sortAsc);
    } else {
      setSortKey(key);
      setSortAsc(true);
    }
  };

  return (
    <div className="space-y-4">
      {/* Search */}
      <input
        type="text"
        placeholder="検索..."
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
        className="w-full px-4 py-2 bg-ark-dark border border-gray-700 rounded-lg focus:border-ark-accent focus:outline-none"
      />

      {/* Table */}
      <div className="overflow-x-auto rounded-lg border border-gray-700">
        <table className="w-full">
          <thead className="bg-ark-dark">
            <tr>
              {columns.map((col) => (
                <th
                  key={String(col.key)}
                  className={clsx(
                    'px-4 py-3 text-left text-sm font-medium text-gray-300 cursor-pointer hover:text-ark-accent',
                    col.width
                  )}
                  onClick={() => handleSort(String(col.key))}
                >
                  {col.header}
                  {sortKey === col.key && (sortAsc ? ' ▲' : ' ▼')}
                </th>
              ))}
              {(onEdit || onDelete) && (
                <th className="px-4 py-3 text-right text-sm font-medium text-gray-300 w-24">
                  操作
                </th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-700">
            {sortedData.map((item) => (
              <tr
                key={String(item[idField])}
                className={clsx(
                  'bg-ark-darker hover:bg-ark-dark transition-colors',
                  onRowClick && 'cursor-pointer'
                )}
                onClick={() => onRowClick?.(item)}
              >
                {columns.map((col) => (
                  <td key={String(col.key)} className="px-4 py-3 text-sm">
                    {col.render
                      ? col.render(item)
                      : String(item[col.key as keyof T] ?? '')}
                  </td>
                ))}
                {(onEdit || onDelete) && (
                  <td className="px-4 py-3 text-right space-x-2">
                    {onEdit && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          onEdit(item);
                        }}
                        className="px-2 py-1 text-xs bg-ark-blue hover:bg-blue-600 rounded"
                      >
                        編集
                      </button>
                    )}
                    {onDelete && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          if (confirm('削除しますか？')) {
                            onDelete(String(item[idField]));
                          }
                        }}
                        className="px-2 py-1 text-xs bg-red-600 hover:bg-red-700 rounded"
                      >
                        削除
                      </button>
                    )}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>

        {sortedData.length === 0 && (
          <div className="px-4 py-8 text-center text-gray-500">
            データがありません
          </div>
        )}
      </div>

      <div className="text-sm text-gray-500">
        {sortedData.length} / {data.length} 件
      </div>
    </div>
  );
}
