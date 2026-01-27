import { NavLink, Outlet } from 'react-router-dom';
import clsx from 'clsx';

const navItems = [
  { to: '/items', label: 'アイテム', icon: '📦' },
  { to: '/upgrades', label: 'アップグレード', icon: '⬆️' },
  { to: '/gacha', label: 'ガチャ', icon: '🎰' },
  { to: '/companies', label: '企業/株式', icon: '📈' },
  { to: '/events', label: 'イベント', icon: '🎭' },
  { to: '/graph', label: '依存関係', icon: '🔗' },
  { to: '/validation', label: '整合性', icon: '✓' },
];

export function Layout() {
  return (
    <div className="min-h-screen bg-ark-darker">
      {/* Header */}
      <header className="bg-ark-dark border-b border-gray-700 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <h1 className="text-xl font-bold text-ark-accent">
              Game Data Manager
            </h1>
            <span className="text-sm text-gray-500">Arknights Cleaker</span>
          </div>
        </div>
      </header>

      <div className="flex">
        {/* Sidebar */}
        <nav className="w-56 bg-ark-dark border-r border-gray-700 min-h-[calc(100vh-65px)] sticky top-[65px]">
          <ul className="py-4">
            {navItems.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  className={({ isActive }) =>
                    clsx(
                      'flex items-center gap-3 px-6 py-3 text-sm transition-colors',
                      isActive
                        ? 'bg-ark-accent/20 text-ark-accent border-r-2 border-ark-accent'
                        : 'text-gray-400 hover:text-white hover:bg-gray-800'
                    )
                  }
                >
                  <span>{item.icon}</span>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        {/* Main Content */}
        <main className="flex-1 p-6">
          <div className="max-w-6xl mx-auto">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
