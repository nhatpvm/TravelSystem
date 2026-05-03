import React, { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Settings,
  UserCircle,
  Bus,
  Train,
  Plane,
  Wrench,
  Hotel,
  Map,
  CreditCard,
  Ticket,
  Star,
  PieChart,
  Tag,
  Globe,
} from 'lucide-react';
import { logout, switchCurrentTenant } from '../../../services/auth';
import { getStoredAuthState } from '../../../services/interceptor';
import { useAuthSession } from '../../../modules/auth/hooks/useAuthSession';
import {
  canAccessTenantModule,
  getTenantAccessiblePath,
  getTenantDisplayName,
  getTenantOperatorBadge,
  getUserDisplayName,
  hasTenantPermission,
} from '../../../modules/auth/types';

function isTenantPathAccessible(pathname, session) {
  if (pathname === '/tenant' || pathname === '/tenant/') {
    return hasTenantPermission(session, 'tenant.dashboard.read');
  }

  if (pathname.startsWith('/tenant/bookings')) {
    return hasTenantPermission(session, 'tenant.bookings.read');
  }

  if (pathname.startsWith('/tenant/promos')) {
    return hasTenantPermission(session);
  }

  if (pathname.startsWith('/tenant/reviews')) {
    return hasTenantPermission(session, 'tenant.reviews.read');
  }

  if (pathname.startsWith('/tenant/cms')) {
    return hasTenantPermission(session, 'cms.posts.read');
  }

  if (pathname.startsWith('/tenant/staff')) {
    return hasTenantPermission(session, 'tenant.staff.manage');
  }

  if (pathname.startsWith('/tenant/finance')) {
    return hasTenantPermission(session, 'tenant.finance.read');
  }

  if (pathname.startsWith('/tenant/reports')) {
    return hasTenantPermission(session, 'tenant.reports.read');
  }

  if (pathname.startsWith('/tenant/settings')) {
    return hasTenantPermission(session, 'tenant.settings.read');
  }

  if (pathname.startsWith('/tenant/inventory/bus')
    || pathname.startsWith('/tenant/operations/bus')
    || pathname.startsWith('/tenant/providers/bus')) {
    return canAccessTenantModule(session, 'bus');
  }

  if (pathname.startsWith('/tenant/inventory/train')
    || pathname.startsWith('/tenant/operations/train')
    || pathname.startsWith('/tenant/providers/train')) {
    return canAccessTenantModule(session, 'train');
  }

  if (pathname.startsWith('/tenant/inventory/flight')
    || pathname.startsWith('/tenant/operations/flight')
    || pathname.startsWith('/tenant/providers/flight')) {
    return canAccessTenantModule(session, 'flight');
  }

  if (pathname.startsWith('/tenant/inventory/hotel')
    || pathname.startsWith('/tenant/operations/hotel')
    || pathname.startsWith('/tenant/providers/hotel')) {
    return canAccessTenantModule(session, 'hotel');
  }

  if (pathname.startsWith('/tenant/inventory/tour')
    || pathname.startsWith('/tenant/operations/tour')) {
    return canAccessTenantModule(session, 'tour');
  }

  return true;
}

const TenantLayout = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const session = useAuthSession();
  const { user, memberships, currentTenantId } = session;
  const [switchingTenant, setSwitchingTenant] = useState(false);

  const menuItems = [
    { icon: <LayoutDashboard size={20} />, label: 'Dashboard', path: '/tenant', permission: 'tenant.dashboard.read' },
    { icon: <Bus size={20} />, label: 'Kho xe khách', path: '/tenant/inventory/bus', module: 'bus' },
    { icon: <Wrench size={20} />, label: 'Vận hành xe khách', path: '/tenant/operations/bus', module: 'bus' },
    { icon: <Bus size={20} />, label: 'Đội xe & giữ chỗ', path: '/tenant/providers/bus', module: 'bus' },
    { icon: <Train size={20} />, label: 'Kho vé tàu', path: '/tenant/inventory/train', module: 'train' },
    { icon: <Wrench size={20} />, label: 'Vận hành tàu', path: '/tenant/operations/train', module: 'train' },
    { icon: <Train size={20} />, label: 'Toa tàu & giữ chỗ', path: '/tenant/providers/train', module: 'train' },
    { icon: <Plane size={20} />, label: 'Kho vé máy bay', path: '/tenant/inventory/flight', module: 'flight' },
    { icon: <Wrench size={20} />, label: 'Vận hành hàng không', path: '/tenant/operations/flight', module: 'flight' },
    { icon: <Plane size={20} />, label: 'Đội bay & ghế cabin', path: '/tenant/providers/flight', module: 'flight' },
    { icon: <Hotel size={20} />, label: 'Kho khách sạn', path: '/tenant/inventory/hotel', module: 'hotel' },
    { icon: <Wrench size={20} />, label: 'Vận hành khách sạn', path: '/tenant/operations/hotel', module: 'hotel' },
    { icon: <Hotel size={20} />, label: 'Dịch vụ & ARI', path: '/tenant/providers/hotel', module: 'hotel' },
    { icon: <Map size={20} />, label: 'Quản lý tour', path: '/tenant/inventory/tour', module: 'tour' },
    { icon: <Globe size={20} />, label: 'CMS & SEO', path: '/tenant/cms', permission: 'cms.posts.read' },
    { icon: <Ticket size={20} />, label: 'Đơn hàng', path: '/tenant/bookings', permission: 'tenant.bookings.read' },
    { icon: <Tag size={20} />, label: 'Khuyến mãi', path: '/tenant/promos' },
    { icon: <Star size={20} />, label: 'Đánh giá', path: '/tenant/reviews', permission: 'tenant.reviews.read' },
    { icon: <Users size={20} />, label: 'Nhân viên', path: '/tenant/staff', permission: 'tenant.staff.manage' },
    { icon: <CreditCard size={20} />, label: 'Tài chính', path: '/tenant/finance', permission: 'tenant.finance.read' },
    { icon: <PieChart size={20} />, label: 'Báo cáo', path: '/tenant/reports', permission: 'tenant.reports.read' },
    { icon: <Settings size={20} />, label: 'Cài đặt', path: '/tenant/settings', permission: 'tenant.settings.read' },
  ];

  const visibleMenuItems = menuItems.filter((item) => {
    if (item.module) {
      return canAccessTenantModule(session, item.module);
    }

    return hasTenantPermission(session, item.permission);
  });

  const operatorBadge = getTenantOperatorBadge(session);
  const tenantDisplayName = getTenantDisplayName(session);

  const handleLogout = async () => {
    await logout();
    navigate('/auth/login', { replace: true });
  };

  const handleTenantSwitch = async (event) => {
    const nextTenantId = event.target.value;
    if (!nextTenantId || nextTenantId === currentTenantId) {
      return;
    }

    setSwitchingTenant(true);

    try {
      await switchCurrentTenant(nextTenantId);
      const nextState = getStoredAuthState();
      const currentUrl = `${location.pathname}${location.search}${location.hash}`;
      navigate(
        isTenantPathAccessible(location.pathname, nextState)
          ? currentUrl
          : getTenantAccessiblePath(nextState),
        { replace: true },
      );
    } finally {
      setSwitchingTenant(false);
    }
  };

  return (
    <div className="flex h-screen bg-slate-50 font-sans antialiased overflow-hidden">
      <aside className="w-64 bg-white border-r border-slate-200 flex flex-col z-20">
        <div className="p-6 flex items-center gap-3">
          <div className="w-10 h-10 bg-slate-900 rounded-xl flex items-center justify-center text-white font-bold text-xl">P</div>
          <span className="text-xl font-bold text-slate-900 tracking-tight">Partner<span className="text-blue-600">Hub</span></span>
        </div>

        <nav className="flex-1 mt-6 px-4 pb-4 space-y-1 overflow-y-auto min-h-0 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-slate-200 [&::-webkit-scrollbar-thumb]:rounded-full">
          <div className="px-4 text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-4">Main Menu</div>
          {visibleMenuItems.map((item) => (
            <Link
              key={item.label}
              to={item.path}
              className="flex items-center gap-3 px-4 py-3 rounded-xl transition-all hover:bg-blue-50 hover:text-blue-600 group"
            >
              <span className="text-slate-400 group-hover:text-blue-600 transition-colors">{item.icon}</span>
              <span className="font-semibold text-sm">{item.label}</span>
            </Link>
          ))}
        </nav>

        <div className="p-6">
          <div className="bg-slate-900 rounded-2xl p-4 text-white">
            <p className="text-xs opacity-60">Your current plan</p>
            <p className="font-bold text-sm mt-1">Premium Partner</p>
            <button onClick={handleLogout} className="mt-4 w-full py-2 bg-white/10 hover:bg-white/20 rounded-lg text-xs font-bold transition-all">Logout</button>
          </div>
        </div>
      </aside>

      <main className="flex-1 flex flex-col overflow-hidden">
        <header className="h-20 bg-white border-b border-slate-200 flex items-center justify-between px-8 z-10">
          <div className="flex items-center gap-2">
            <div className="px-3 py-1 bg-amber-100 text-amber-700 text-[10px] font-bold rounded-full">{operatorBadge}</div>
            <p className="text-sm font-semibold text-slate-800">{tenantDisplayName}</p>
            {memberships.length > 1 && (
              <select
                value={currentTenantId || memberships[0]?.tenantId || ''}
                onChange={handleTenantSwitch}
                disabled={switchingTenant}
                className="ml-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-600 outline-none disabled:opacity-70"
              >
                {memberships.map((item) => (
                  <option key={item.tenantId} value={item.tenantId}>
                    {item.name}
                  </option>
                ))}
              </select>
            )}
          </div>

          <div className="flex items-center gap-4">
            <div className="h-8 w-px bg-slate-200 mx-2"></div>
            <button onClick={handleLogout} className="flex items-center gap-2 hover:bg-slate-50 p-1 pr-3 rounded-full transition-all border border-transparent hover:border-slate-100">
              <div className="w-8 h-8 rounded-full bg-slate-200 flex items-center justify-center text-slate-600 overflow-hidden">
                <UserCircle size={24} />
              </div>
              <span className="text-xs font-bold text-slate-700 font-sans">{getUserDisplayName(user)}</span>
            </button>
          </div>
        </header>

        <section className="flex-1 overflow-y-auto p-4 md:p-8">
          <Outlet />
        </section>
      </main>
    </div>
  );
};

export default TenantLayout;
