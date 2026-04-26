import React, { useMemo, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  Activity,
  Bell,
  CreditCard,
  Database,
  Globe,
  Hotel,
  LayoutDashboard,
  LogOut,
  MessageSquare,
  Package,
  Plane,
  RotateCcw,
  Search,
  Send,
  Settings,
  Shield,
  Tag,
  Ticket,
  Train,
  Users,
} from 'lucide-react';
import logo from '../../../assets/logo.png';
import { logout } from '../../../services/auth';
import { useAuthSession } from '../../../modules/auth/hooks/useAuthSession';
import { getUserDisplayName, getUserInitials } from '../../../modules/auth/types';

const menuItems = [
  { icon: <LayoutDashboard size={18} />, label: 'Dashboard', path: '/admin' },
  { icon: <Users size={18} />, label: 'Người dùng', path: '/admin/users' },
  { icon: <Shield size={18} />, label: 'Vai trò', path: '/admin/roles' },
  { icon: <Users size={18} />, label: 'Tenants', path: '/admin/tenants' },
  { icon: <Ticket size={18} />, label: 'Bookings', path: '/admin/bookings' },
  { icon: <CreditCard size={18} />, label: 'Thanh toán', path: '/admin/payments' },
  { icon: <RotateCcw size={18} />, label: 'Hoàn tiền', path: '/admin/refunds' },
  { icon: <Package size={18} />, label: 'Settlement', path: '/admin/settlement' },
  { icon: <Tag size={18} />, label: 'Khuyến mãi', path: '/admin/promos' },
  { icon: <MessageSquare size={18} />, label: 'Support', path: '/admin/support' },
  { icon: <Globe size={18} />, label: 'CMS', path: '/admin/cms' },
  { icon: <Package size={18} />, label: 'Tour', path: '/admin/tours' },
  { icon: <Train size={18} />, label: 'Tàu', path: '/admin/train' },
  { icon: <Plane size={18} />, label: 'Máy bay', path: '/admin/flight' },
  { icon: <Hotel size={18} />, label: 'Khách sạn', path: '/admin/hotels' },
  { icon: <MessageSquare size={18} />, label: 'Đánh giá tour', path: '/admin/tour-reviews' },
  { icon: <Bell size={18} />, label: 'Thông báo', path: '/admin/notifications' },
  { icon: <Database size={18} />, label: 'Master Data', path: '/admin/master-data' },
  { icon: <Activity size={18} />, label: 'Audit Logs', path: '/admin/audit' },
  { icon: <Send size={18} />, label: 'Outbox', path: '/admin/outbox' },
  { icon: <CreditCard size={18} />, label: 'Tài chính', path: '/admin/finance' },
  { icon: <Settings size={18} />, label: 'Cài đặt', path: '/admin/settings' },
];

const AdminLayout = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuthSession();
  const headerSearchFromUrl = useMemo(() => {
    const params = new URLSearchParams(location.search);
    return params.get('q') || '';
  }, [location.search]);
  const [headerSearchDraft, setHeaderSearchDraft] = useState({ key: '', value: '' });
  const headerSearch = headerSearchDraft.key === location.search ? headerSearchDraft.value : headerSearchFromUrl;

  const handleLogout = async () => {
    await logout();
    navigate('/auth/login', { replace: true });
  };

  const resolveSearchTarget = () => {
    const path = location.pathname;
    if (path.startsWith('/admin/tenants')) return '/admin/tenants';
    if (path.startsWith('/admin/bookings')) return '/admin/bookings';
    if (path.startsWith('/admin/payments')) return '/admin/payments';
    if (path.startsWith('/admin/refunds')) return '/admin/refunds';
    if (path.startsWith('/admin/support')) return '/admin/support';
    return '/admin/users';
  };

  const handleHeaderSearch = (event) => {
    event.preventDefault();
    const query = headerSearch.trim();
    if (!query) {
      return;
    }

    navigate(`${resolveSearchTarget()}?q=${encodeURIComponent(query)}`);
  };

  return (
    <div className="flex h-screen bg-slate-50 font-sans antialiased overflow-hidden">
      <aside className="w-64 bg-slate-900 text-slate-300 flex flex-col shadow-2xl z-20">
        <div className="p-6 flex items-center gap-4">
          <img src={logo} alt="2TMNY Logo" className="h-20 brightness-0 invert" />
          <span className="text-2xl font-black text-white tracking-tighter">2TMNY</span>
        </div>

        <nav className="flex-1 mt-6 px-4 pb-4 space-y-1 overflow-y-auto min-h-0 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-slate-700 [&::-webkit-scrollbar-thumb]:rounded-full">
          {menuItems.map((item) => (
            <Link
              key={item.label}
              to={item.path}
              className="flex items-center gap-3 px-4 py-3 rounded-xl transition-all hover:bg-slate-800 hover:text-white group"
            >
              <span className="text-slate-400 group-hover:text-blue-400 transition-colors">{item.icon}</span>
              <span className="font-medium">{item.label}</span>
            </Link>
          ))}
        </nav>

        <div className="p-4 border-t border-slate-800">
          <button onClick={handleLogout} className="flex items-center gap-3 px-4 py-3 w-full text-left rounded-xl hover:bg-red-500/10 hover:text-red-400 transition-all">
            <LogOut size={20} />
            <span className="font-medium">Logout</span>
          </button>
        </div>
      </aside>

      <main className="flex-1 flex flex-col overflow-hidden">
        <header className="h-20 bg-white border-b border-slate-200 flex items-center justify-between px-8 z-10 shadow-sm">
          <form onSubmit={handleHeaderSearch} className="flex items-center gap-4 bg-slate-100 px-4 py-2 rounded-xl w-96">
            <Search size={18} className="text-slate-400" />
            <input
              type="text"
              value={headerSearch}
              onChange={(event) => setHeaderSearchDraft({ key: location.search, value: event.target.value })}
              placeholder="Tìm user, tenant, booking..."
              className="bg-transparent border-none focus:outline-none text-sm w-full"
            />
          </form>

          <div className="flex items-center gap-6">
            <Link to="/admin/notifications" className="relative text-slate-400 hover:text-slate-600 transition-colors" title="Thông báo">
              <Bell size={22} />
            </Link>
            <button onClick={handleLogout} className="flex items-center gap-3 pl-6 border-l border-slate-200">
              <div className="text-right hidden sm:block">
                <p className="text-sm font-bold text-slate-800">{getUserDisplayName(user)}</p>
                <p className="text-xs text-slate-500">Quản trị viên</p>
              </div>
              <div className="w-10 h-10 rounded-full bg-slate-900 flex items-center justify-center text-white font-bold border-2 border-white shadow-sm">
                {getUserInitials(user)}
              </div>
            </button>
          </div>
        </header>

        <section className="flex-1 overflow-y-auto bg-slate-50/50">
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </section>
      </main>
    </div>
  );
};

export default AdminLayout;
