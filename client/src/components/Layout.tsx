import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../AuthContext';

const navItems = [
  { to: '/properties', label: 'Properties' },
  { to: '/transactions', label: 'Transactions' },
  { to: '/reports', label: 'Reports' },
  { to: '/maintenance', label: 'Maintenance' },
];

export default function Layout() {
  const { username, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-blue-800 text-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-2">
              <span className="text-xl font-bold tracking-tight">🏠 Property Management</span>
            </div>
            <div className="flex items-center gap-1">
              {navItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                      isActive ? 'bg-blue-900 text-white' : 'text-blue-100 hover:bg-blue-700'
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </div>
            <div className="flex items-center gap-4">
              <span className="text-sm text-blue-200">Welcome, {username}</span>
              <button
                onClick={handleLogout}
                className="px-3 py-1.5 text-sm bg-blue-900 hover:bg-blue-950 rounded-md transition-colors"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>
      <main className="max-w-7xl mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  );
}
