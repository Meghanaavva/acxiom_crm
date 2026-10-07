import { useState } from 'react';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { Role } from '../types';
import { roleLabel } from '../utils/format';
import GlobalSearch from './GlobalSearch';
import NotificationBell from './NotificationBell';

interface MenuItem {
  to: string;
  label: string;
  icon: string;
  roles?: Role[]; // undefined = everyone
}

const MENU: MenuItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: 'bi-speedometer2' },
  { to: '/customers', label: 'Customers', icon: 'bi-people' },
  { to: '/leads', label: 'Leads', icon: 'bi-person-plus' },
  { to: '/opportunities', label: 'Opportunities', icon: 'bi-briefcase' },
  { to: '/pipeline', label: 'Sales Pipeline', icon: 'bi-kanban' },
  { to: '/followups', label: 'Follow-Ups', icon: 'bi-calendar-check' },
  { to: '/activities', label: 'Activities', icon: 'bi-activity' },
  { to: '/reports', label: 'Reports', icon: 'bi-bar-chart-line' },
  { to: '/users', label: 'Users', icon: 'bi-person-gear', roles: ['Admin', 'Manager'] },
  { to: '/roles', label: 'Roles & Permissions', icon: 'bi-shield-lock', roles: ['Admin'] },
  { to: '/audit-logs', label: 'Audit Log', icon: 'bi-journal-text', roles: ['Admin', 'Manager'] },
];

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const visibleMenu = MENU.filter((m) => !m.roles || (user && m.roles.includes(user.role)));

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="d-flex flex-column min-vh-100">
      {/* Header */}
      <nav className="navbar navbar-dark bg-primary sticky-top shadow-sm px-3">
        <div className="d-flex align-items-center">
          <button
            className="btn btn-link text-white d-lg-none me-2 p-0"
            onClick={() => setSidebarOpen(!sidebarOpen)}
            aria-label="Toggle navigation"
          >
            <i className="bi bi-list fs-3"></i>
          </button>
          <Link className="navbar-brand fw-bold" to="/dashboard">
            <i className="bi bi-diagram-3-fill me-2"></i>AcxiomCRM
          </Link>
        </div>
        <GlobalSearch />
        <div className="d-flex align-items-center gap-2">
          <NotificationBell />
          <UserMenu name={user?.name || ''} role={roleLabel(user?.role || '')} onLogout={handleLogout} />
        </div>
      </nav>

      <div className="d-flex flex-grow-1">
        {/* Sidebar */}
        <aside className={`sidebar bg-white border-end ${sidebarOpen ? 'open' : ''}`}>
          <ul className="nav flex-column py-3">
            {visibleMenu.map((item) => (
              <li className="nav-item" key={item.to}>
                <NavLink
                  to={item.to}
                  className={({ isActive }) => `nav-link sidebar-link ${isActive ? 'active' : ''}`}
                  onClick={() => setSidebarOpen(false)}
                >
                  <i className={`bi ${item.icon} me-2`}></i>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </aside>
        {sidebarOpen && <div className="sidebar-backdrop d-lg-none" onClick={() => setSidebarOpen(false)}></div>}

        {/* Page content */}
        <main className="flex-grow-1 p-3 p-md-4 bg-light main-content">
          <Outlet />
        </main>
      </div>

      <footer className="bg-white border-top py-2 px-3 small text-muted d-flex justify-content-between">
        <span>&copy; {new Date().getFullYear()} AcxiomCRM</span>
        <span>Customer Relationship Management</span>
      </footer>
    </div>
  );
}

function UserMenu({ name, role, onLogout }: { name: string; role: string; onLogout: () => void }) {
  const [open, setOpen] = useState(false);
  return (
    <div className="position-relative">
      <button className="btn btn-outline-light btn-sm d-flex align-items-center" onClick={() => setOpen(!open)} aria-expanded={open}>
        <i className="bi bi-person-circle me-2"></i>
        <span className="d-none d-sm-inline">{name}</span>
        <i className="bi bi-chevron-down ms-2 small"></i>
      </button>
      {open && (
        <ul className="dropdown-menu dropdown-menu-end show mt-1" style={{ right: 0, left: 'auto' }} onMouseLeave={() => setOpen(false)}>
          <li className="px-3 py-2 small">
            <div className="fw-semibold">{name}</div>
            <div className="text-muted">{role}</div>
          </li>
          <li>
            <hr className="dropdown-divider" />
          </li>
          <li>
            <Link className="dropdown-item" to="/change-password" onClick={() => setOpen(false)}>
              <i className="bi bi-key me-2"></i>Change Password
            </Link>
          </li>
          <li>
            <button className="dropdown-item text-danger" onClick={onLogout}>
              <i className="bi bi-box-arrow-right me-2"></i>Logout
            </button>
          </li>
        </ul>
      )}
    </div>
  );
}
