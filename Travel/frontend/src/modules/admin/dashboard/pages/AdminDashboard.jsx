import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Activity,
  AlertTriangle,
  Globe,
  RefreshCw,
  ShieldCheck,
  ShoppingBag,
  TrendingUp,
} from 'lucide-react';
import { getAdminOpsOverview } from '../../../../services/adminOpsService';
import { formatCurrency, formatDateTime } from '../../../tenant/train/utils/presentation';

const activityClass = {
  Success: 'bg-green-50 text-green-600',
  Warning: 'bg-amber-50 text-amber-600',
  Error: 'bg-rose-50 text-rose-600',
  Info: 'bg-slate-50 text-slate-600',
};

const AdminDashboard = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [data, setData] = useState({
    bookingCount: 0,
    paidBookingCount: 0,
    gmvAmount: 0,
    failedPaymentCount: 0,
    failedPaymentRate: 0,
    pendingTenantCount: 0,
    pendingOnboardingCount: 0,
    openSupportTicketCount: 0,
    pendingRefundCount: 0,
    recentActivities: [],
  });

  async function loadDashboard() {
    setLoading(true);
    setError('');

    try {
      const response = await getAdminOpsOverview();
      setData({
        bookingCount: response?.bookingCount || 0,
        paidBookingCount: response?.paidBookingCount || 0,
        gmvAmount: response?.gmvAmount || 0,
        failedPaymentCount: response?.failedPaymentCount || 0,
        failedPaymentRate: response?.failedPaymentRate || 0,
        pendingTenantCount: response?.pendingTenantCount || 0,
        pendingOnboardingCount: response?.pendingOnboardingCount || 0,
        openSupportTicketCount: response?.openSupportTicketCount || 0,
        pendingRefundCount: response?.pendingRefundCount || 0,
        recentActivities: Array.isArray(response?.recentActivities) ? response.recentActivities : [],
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dashboard admin.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  const stats = useMemo(() => ([
    {
      label: 'Tổng GMV đã ghi nhận',
      value: formatCurrency(data.gmvAmount || 0, 'VND'),
      sub: `${data.paidBookingCount || 0} booking đã thanh toán`,
      icon: <TrendingUp size={24} />,
      className: 'bg-blue-50',
    },
    {
      label: 'Hồ sơ tenant chờ xử lý',
      value: data.pendingOnboardingCount || 0,
      sub: `${data.pendingTenantCount || 0} tenant đang tạm khóa/chờ kiểm tra`,
      icon: <Globe size={24} />,
      className: 'bg-indigo-50',
    },
    {
      label: 'Bookings',
      value: data.bookingCount || 0,
      sub: `${data.paidBookingCount || 0} đã thanh toán`,
      icon: <ShoppingBag size={24} />,
      className: 'bg-emerald-50',
    },
    {
      label: 'Thanh toán lỗi',
      value: `${Number(data.failedPaymentRate || 0).toFixed(1)}%`,
      sub: `${data.failedPaymentCount || 0} giao dịch lỗi · ${data.pendingRefundCount || 0} refund cần xử lý`,
      icon: <AlertTriangle size={24} />,
      className: 'bg-rose-50',
    },
  ]), [data]);

  const operations = [
    { label: 'Booking paid', value: data.paidBookingCount || 0 },
    { label: 'Payment failed', value: data.failedPaymentCount || 0 },
    { label: 'Refund open', value: data.pendingRefundCount || 0 },
    { label: 'Support open', value: data.openSupportTicketCount || 0 },
    { label: 'Tenant pending', value: data.pendingTenantCount || 0 },
    { label: 'Onboarding pending', value: data.pendingOnboardingCount || 0 },
    { label: 'Bookings total', value: data.bookingCount || 0 },
    { label: 'Audit recent', value: data.recentActivities.length || 0 },
  ];

  return (
    <div className="p-8 space-y-10">
      <div className="flex items-center justify-between">
        <div className="animate-in fade-in slide-in-from-left duration-700">
          <h1 className="text-4xl font-black text-slate-900 tracking-tight">Master Dashboard</h1>
          <p className="text-slate-500 font-medium mt-1 uppercase tracking-widest text-[10px]">Dữ liệu thật từ Admin Ops API</p>
        </div>
        <button
          type="button"
          onClick={loadDashboard}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-3 bg-white rounded-2xl border border-slate-100 shadow-sm text-xs font-black uppercase tracking-widest text-slate-600 disabled:opacity-60"
        >
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Tải lại
        </button>
      </div>

      {error ? (
        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
        {stats.map((stat) => (
          <div key={stat.label} className="bg-white p-8 rounded-[3rem] shadow-sm border border-slate-100 hover:shadow-2xl hover:shadow-blue-500/10 transition-all duration-500 group relative overflow-hidden">
            <div className={`absolute top-0 right-0 w-32 h-32 ${stat.className} rounded-full -mr-16 -mt-16 transition-transform group-hover:scale-110`} />
            <div className="w-14 h-14 rounded-2xl flex items-center justify-center bg-slate-900 text-white mb-6 shadow-lg shadow-slate-900/20 relative z-10 transition-all group-hover:bg-blue-600">
              {stat.icon}
            </div>
            <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2">{stat.label}</p>
            <p className="text-3xl font-black text-slate-900 tracking-tight">{loading ? '--' : stat.value}</p>
            <p className="text-[10px] font-bold text-slate-400 mt-2">{stat.sub}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
        <div className="lg:col-span-2 bg-slate-900 rounded-[4rem] p-10 text-white shadow-2xl relative overflow-hidden group">
          <div className="flex items-center justify-between mb-10 relative z-10">
            <div>
              <h3 className="text-xl font-black">Tổng quan vận hành</h3>
              <p className="text-xs text-slate-400 font-medium mt-1">Booking, payment, refund, support và tenant hiện có</p>
            </div>
            <span className="flex items-center gap-2 px-4 py-2 bg-blue-50 text-blue-600 rounded-xl font-black text-xs">
              <Activity size={14} /> LIVE DATA
            </span>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 relative z-10">
            {operations.map((item) => (
              <div key={item.label} className="rounded-2xl bg-white/10 px-4 py-5 border border-white/5">
                <p className="text-2xl font-black">{loading ? '--' : item.value}</p>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-[4rem] p-10 shadow-sm border border-slate-100">
          <h3 className="text-xl font-black text-slate-900 mb-8 flex items-center gap-3">
            <Activity size={24} className="text-blue-600" /> Hoạt động mới
          </h3>

          <div className="space-y-6">
            {loading ? (
              <p className="text-sm font-bold text-slate-400">Đang tải hoạt động...</p>
            ) : data.recentActivities.length === 0 ? (
              <p className="text-sm font-bold text-slate-400">Chưa có hoạt động gần đây.</p>
            ) : (
              data.recentActivities.map((event) => (
                <div key={event.id} className="flex items-start gap-4 group">
                  <div className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 transition-transform group-hover:scale-110 ${activityClass[event.severity] || activityClass.Info}`}>
                    {event.severity === 'Success' ? <ShieldCheck size={20} /> : <Activity size={20} />}
                  </div>
                  <div className="flex-1 border-b border-slate-50 pb-5 group-last:border-none">
                    <div className="flex justify-between items-start gap-3">
                      <p className="text-sm font-bold text-slate-800 leading-tight group-hover:text-blue-600 transition-colors">{event.description || event.action}</p>
                      <p className="text-[10px] font-black text-slate-900">{event.entityCode}</p>
                    </div>
                    <p className="text-[10px] text-slate-400 font-bold uppercase tracking-widest mt-2">{formatDateTime(event.occurredAt)}</p>
                  </div>
                </div>
              ))
            )}
          </div>

          <Link to="/admin/audit" className="block text-center w-full mt-6 py-4 border-2 border-slate-50 rounded-2xl text-[10px] font-black text-slate-400 uppercase tracking-widest hover:bg-slate-50 hover:text-slate-900 transition-all">
            Xem audit logs
          </Link>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
