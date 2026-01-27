import { useRef, useEffect, useCallback } from 'react';
import { useDependencyGraph } from '../hooks/useDataQuery';

// グラフの色設定
const NODE_COLORS: Record<string, string> = {
  item: '#4ade80',     // 緑
  upgrade: '#60a5fa',  // 青
  gacha: '#f472b6',    // ピンク
  company: '#fbbf24',  // 黄
  event: '#a78bfa',    // 紫
};

const EDGE_COLORS: Record<string, string> = {
  unlock: '#4ade80',
  prerequisite: '#60a5fa',
  material: '#f472b6',
  contains: '#fbbf24',
  reward: '#a78bfa',
};

export function GraphPage() {
  const { data: graph, isLoading, error } = useDependencyGraph();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // シンプルなフォースレイアウト
  const drawGraph = useCallback(() => {
    if (!graph || !canvasRef.current || !containerRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const { width, height } = containerRef.current.getBoundingClientRect();
    canvas.width = width;
    canvas.height = height;

    // ノード位置を初期化（円形配置）
    const nodes = graph.nodes.map((node, i) => {
      const angle = (2 * Math.PI * i) / graph.nodes.length;
      const radius = Math.min(width, height) * 0.35;
      return {
        ...node,
        x: width / 2 + radius * Math.cos(angle),
        y: height / 2 + radius * Math.sin(angle),
      };
    });

    // ノードIDからインデックスへのマップ
    const nodeMap = new Map(nodes.map((n, i) => [n.id, i]));

    // 描画
    ctx.fillStyle = '#0f0f1a';
    ctx.fillRect(0, 0, width, height);

    // エッジを描画
    graph.edges.forEach((edge) => {
      const fromIdx = nodeMap.get(edge.from);
      const toIdx = nodeMap.get(edge.to);
      if (fromIdx === undefined || toIdx === undefined) return;

      const from = nodes[fromIdx];
      const to = nodes[toIdx];

      ctx.beginPath();
      ctx.moveTo(from.x, from.y);
      ctx.lineTo(to.x, to.y);
      ctx.strokeStyle = EDGE_COLORS[edge.type] || '#666';
      ctx.lineWidth = 1;
      ctx.globalAlpha = 0.3;
      ctx.stroke();
      ctx.globalAlpha = 1;
    });

    // ノードを描画
    nodes.forEach((node) => {
      ctx.beginPath();
      ctx.arc(node.x, node.y, 8, 0, 2 * Math.PI);
      ctx.fillStyle = NODE_COLORS[node.type] || '#888';
      ctx.fill();
      ctx.strokeStyle = '#fff';
      ctx.lineWidth = 1;
      ctx.stroke();

      // ラベル
      ctx.fillStyle = '#fff';
      ctx.font = '10px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(node.label.substring(0, 15), node.x, node.y + 20);
    });
  }, [graph]);

  useEffect(() => {
    drawGraph();
    window.addEventListener('resize', drawGraph);
    return () => window.removeEventListener('resize', drawGraph);
  }, [drawGraph]);

  if (isLoading) {
    return <div className="text-center py-8">読み込み中...</div>;
  }

  if (error) {
    return <div className="text-center py-8 text-red-500">エラーが発生しました</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">依存関係グラフ</h2>
        <div className="flex gap-4 text-sm">
          {Object.entries(NODE_COLORS).map(([type, color]) => (
            <div key={type} className="flex items-center gap-2">
              <div className="w-3 h-3 rounded-full" style={{ backgroundColor: color }} />
              <span className="text-gray-400">{type}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="bg-ark-dark border border-gray-700 rounded-lg p-4">
        <div className="mb-4 text-sm text-gray-400">
          ノード数: {graph?.nodes.length || 0} / エッジ数: {graph?.edges.length || 0}
        </div>
        <div ref={containerRef} className="w-full h-[600px] bg-ark-darker rounded-lg overflow-hidden">
          <canvas ref={canvasRef} />
        </div>
      </div>

      <div className="bg-ark-dark border border-gray-700 rounded-lg p-4">
        <h3 className="text-lg font-medium mb-4">エッジタイプ凡例</h3>
        <div className="grid grid-cols-5 gap-4 text-sm">
          {Object.entries(EDGE_COLORS).map(([type, color]) => (
            <div key={type} className="flex items-center gap-2">
              <div className="w-8 h-0.5" style={{ backgroundColor: color }} />
              <span className="text-gray-400">{type}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
