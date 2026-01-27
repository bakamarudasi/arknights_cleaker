import { Routes, Route, Navigate } from 'react-router-dom'
import { Layout } from './components/Layout'
import { ItemsPage } from './pages/ItemsPage'
import { UpgradesPage } from './pages/UpgradesPage'
import { GachaPage } from './pages/GachaPage'
import { CompaniesPage } from './pages/CompaniesPage'
import { EventsPage } from './pages/EventsPage'
import { GraphPage } from './pages/GraphPage'
import { ValidationPage } from './pages/ValidationPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to="/items" replace />} />
        <Route path="items" element={<ItemsPage />} />
        <Route path="upgrades" element={<UpgradesPage />} />
        <Route path="gacha" element={<GachaPage />} />
        <Route path="companies" element={<CompaniesPage />} />
        <Route path="events" element={<EventsPage />} />
        <Route path="graph" element={<GraphPage />} />
        <Route path="validation" element={<ValidationPage />} />
      </Route>
    </Routes>
  )
}

export default App
