import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function Shell() {
  const { auth, logout } = useAuth()

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-mark">#</span>
          <div>
            <strong>Hashtag Wall</strong>
            <div className="muted sm">{auth.username}</div>
          </div>
        </div>
        <nav className="nav">
          <NavLink to="/dashboard" className={({ isActive }) => (isActive ? 'active' : '')}>
            Dashboard
          </NavLink>
          <NavLink to="/onboarding" className={({ isActive }) => (isActive ? 'active' : '')}>
            Setup guiado
          </NavLink>
          <NavLink to="/hashtags/new" className={({ isActive }) => (isActive ? 'active' : '')}>
            Nova hashtag
          </NavLink>
        </nav>
        <button type="button" className="btn ghost sidebar-logout" onClick={logout}>
          Sair
        </button>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}
