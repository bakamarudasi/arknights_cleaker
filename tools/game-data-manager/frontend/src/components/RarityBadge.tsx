import type { Rarity } from '../types';
import clsx from 'clsx';

interface RarityBadgeProps {
  rarity: Rarity;
}

const rarityConfig: Record<Rarity, { stars: number; color: string }> = {
  Star1: { stars: 1, color: 'text-gray-400' },
  Star2: { stars: 2, color: 'text-green-400' },
  Star3: { stars: 3, color: 'text-blue-400' },
  Star4: { stars: 4, color: 'text-purple-400' },
  Star5: { stars: 5, color: 'text-yellow-400' },
  Star6: { stars: 6, color: 'text-orange-400' },
};

export function RarityBadge({ rarity }: RarityBadgeProps) {
  const config = rarityConfig[rarity];
  return (
    <span className={clsx('font-bold', config.color)}>
      {'★'.repeat(config.stars)}
    </span>
  );
}
