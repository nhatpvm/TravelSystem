import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  AlertCircle,
  BarChart3,
  Bus,
  Compass,
  Download,
  Hotel,
  Loader2,
  Percent,
  Plane,
  RefreshCw,
  Train,
  TrendingDown,
  TrendingUp,
  Users,
  Wallet,
} from 'lucide-react';
import { getTenantCommerceReports } from '../../../services/commerceBackofficeService';
import { formatCurrency } from '../train/utils/presentation';

const PERIODS = [
  { value: 'month', label: 'Tháng' },
  { value: 'quarter', label: 'Quý' },
  { value: 'year', label: 'Năm' },
];

const PRODUCT_TONES = {
  1: { color: 'bg-blue-600', icon: <Bus size={14} /> },
  2: { color: 'bg-cyan-500', icon: <Train size={14} /> },
  3: { color: 'bg-indigo-500', icon: <Plane size={14} /> },
  4: { color: 'bg-emerald-500', icon: <Hotel size={14} /> },
  5: { color: 'bg-amber-500', icon: <Compass size={14} /> },
};

function getProductTone(productType) {
  return PRODUCT_TONES[Number(productType)] || { color: 'bg-slate-500', icon: <BarChart3 size={14} /> };
}

function formatPercent(value) {
  return `${Number(value || 0).toFixed(1)}%`;
}

export default function TenantReportsPage() {
  const [period, setPeriod] = useState('year');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [dashboard, setDashboard] = useState({
    tenant: {},
    summary: {},
    monthlySeries: [],
    productBreakdown: [],
    topProducts: [],
  });

  const loadReports = useCallback(async () => {
    setError('');

    try {
      const response = await getTenantCommerceReports({ period });
      setDashboard({
        tenant: response?.tenant || {},
        summary: response?.summary || {},
        monthlySeries: Array.isArray(response?.monthlySeries) ? response.monthlySeries : [],
        productBreakdown: Array.isArray(response?.productBreakdown) ? response.productBreakdown : [],
        topProducts: Array.isArray(response?.topProducts) ? response.topProducts : [],
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải báo cáo tenant.');
      setDashboard({
        tenant: {},
        summary: {},
        monthlySeries: [],
        productBreakdown: [],
        topProducts: [],
      });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [period]);

  useEffect(() => {
    setLoading(true);
    loadReports();
  }, [loadReports]);

  const maxRevenue = useMemo(
    () => Math.max(...dashboard.monthlySeries.map((item) => Number(item.grossAmount || 0)), 1),
    [dashboard.monthlySeries],
  );

  const currencyCode = dashboard.summary?.currencyCode || 'VND';
  const periodText = PERIODS.find((item) => item.value === period)?.label.toLowerCase() || 'năm';

  const stats = [
    {
      label: 'Tổng doanh thu',
      value: formatCurrency(dashboard.summary?.grossAmount || 0, currencyCode),
      delta: `Doanh thu gross trong ${periodText} này`,
      up: true,
      icon: <Wallet size={18} />,
      dark: true,
    },
    {
      label: 'Lượt đặt chỗ',
      value: dashboard.summary?.totalBookings ?? 0,
      delta: `${dashboard.summary?.paidBookings ?? 0} đơn đã ghi nhận doanh thu`,
      up: true,
      icon: <Users size={18} />,
    },
    {
      label: 'Tỷ lệ hoàn thành',
      value: formatPercent(dashboard.summary?.completionRate),
      delta: `${dashboard.summary?.completedBookings ?? 0} đơn hoàn tất`,
      up: true,
      icon: <Percent size={18} />,
    },
    {
      label: 'Tỷ lệ hủy',
      value: formatPercent(dashboard.summary?.cancellationRate),
      delta: `${dashboard.summary?.cancelledBookings ?? 0} đơn hủy/hết hạn/thất bại`,
      up: false,
      icon: <BarChart3 size={18} />,
    },
  ];

  function refresh() {
    setRefreshing(true);
    loadReports();
  }

  return (
    <div>
      <div className="mb-8 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Báo cáo & Thống kê</h1>
          <p className="mt-1 text-sm text-slate-500">
            Số liệu thực tế của {dashboard.tenant?.name || 'tenant hiện tại'}, không trộn dữ liệu đối tác khác.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex gap-1 rounded-2xl border border-slate-100 bg-white p-1 shadow-sm">
            {PERIODS.map((item) => (
              <button
                key={item.value}
                type="button"
                onClick={() => setPeriod(item.value)}
                className={`rounded-xl px-4 py-2 text-[10px] font-black uppercase tracking-widest transition-all ${
                  period === item.value ? 'bg-slate-900 text-white shadow-md' : 'text-slate-400 hover:text-slate-700'
                }`}
              >
                {item.label}
              </button>
            ))}
          </div>
          <button
            type="button"
            onClick={refresh}
            disabled={refreshing}
            className="inline-flex items-center gap-2 rounded-2xl bg-slate-900 px-5 py-3 text-xs font-bold text-white shadow-lg transition-all hover:bg-blue-600 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {refreshing ? <Loader2 size={14} className="animate-spin" /> : <RefreshCw size={14} />}
            Tải lại
          </button>
          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 py-3 text-xs font-bold text-slate-700 transition-all hover:border-blue-200 hover:text-blue-600"
          >
            <Download size={14} />
            Xuất Excel
          </button>
        </div>
      </div>

      {error ? (
        <div className="mb-5 flex items-center gap-2 rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          <AlertCircle size={16} />
          {error}
        </div>
      ) : null}

      <div className="mb-8 grid grid-cols-2 gap-4 lg:grid-cols-4">
        {stats.map((item, index) => (
          <motion.div
            key={item.label}
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.07 }}
            className={`rounded-2xl border border-slate-100 p-5 shadow-sm ${item.dark ? 'bg-gradient-to-br from-slate-900 to-slate-700' : 'bg-white'}`}
          >
            <div className={`mb-3 flex h-9 w-9 items-center justify-center rounded-xl ${item.dark ? 'bg-white/10 text-white' : 'bg-slate-50 text-slate-600'}`}>
              {item.icon}
            </div>
            <p className={`text-2xl font-black ${item.dark ? 'text-white' : 'text-slate-900'}`}>
              {loading ? '--' : item.value}
            </p>
            <p className={`mt-0.5 text-[10px] font-bold uppercase tracking-widest ${item.dark ? 'text-white/50' : 'text-slate-400'}`}>
              {item.label}
            </p>
            <p className={`mt-1 flex items-center gap-1 text-xs font-bold ${item.up ? 'text-emerald-500' : 'text-rose-500'}`}>
              {item.up ? <TrendingUp size={11} /> : <TrendingDown size={11} />}
              {item.delta}
            </p>
          </motion.div>
        ))}
      </div>

      <div className="mb-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-100 bg-white p-6 shadow-sm lg:col-span-2">
          <div className="mb-6 flex items-center justify-between">
            <div>
              <h2 className="font-black text-slate-900">Doanh thu theo tháng</h2>
              <p className="mt-0.5 text-xs font-bold text-slate-400">12 tháng trong năm hiện tại · đơn vị VND</p>
            </div>
            <span className="text-xs font-bold text-slate-400">Gross</span>
          </div>
          <div className="flex h-40 items-end gap-2">
            {dashboard.monthlySeries.map((item, index) => {
              const height = Math.round((Number(item.grossAmount || 0) / maxRevenue) * 100);

              return (
                <div key={`${item.label}-${index}`} className="group flex flex-1 cursor-pointer flex-col items-center gap-1">
                  <div className="relative w-full" style={{ height: '120px' }}>
                    <motion.div
                      initial={{ height: 0 }}
                      animate={{ height: `${height}%` }}
                      transition={{ delay: index * 0.04, duration: 0.5, ease: 'easeOut' }}
                      title={formatCurrency(item.grossAmount || 0, currencyCode)}
                      className={`absolute bottom-0 w-full rounded-t-lg transition-colors ${
                        index === new Date().getMonth() ? 'bg-blue-600' : 'bg-slate-100 group-hover:bg-blue-300'
                      }`}
                    />
                  </div>
                  <span className="text-[9px] font-bold text-slate-400">{item.label}</span>
                </div>
              );
            })}
          </div>
          <div className="mt-4 flex items-center gap-4 border-t border-slate-100 pt-4">
            <div className="flex items-center gap-2">
              <div className="h-3 w-3 rounded-sm bg-blue-600" />
              <span className="text-[10px] font-bold text-slate-500">Tháng hiện tại</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="h-3 w-3 rounded-sm bg-slate-100" />
              <span className="text-[10px] font-bold text-slate-500">Các tháng khác</span>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-slate-100 bg-white p-6 shadow-sm">
          <h2 className="mb-6 font-black text-slate-900">Cơ cấu doanh thu</h2>
          {dashboard.productBreakdown.length === 0 ? (
            <div className="rounded-2xl bg-slate-50 px-4 py-6 text-center text-sm font-bold text-slate-500">
              Chưa có doanh thu trong kỳ này.
            </div>
          ) : (
            <div className="space-y-5">
              {dashboard.productBreakdown.map((item) => {
                const tone = getProductTone(item.productType);

                return (
                  <div key={item.productType}>
                    <div className="mb-1.5 flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="text-slate-400">{tone.icon}</span>
                        <span className="text-sm font-bold text-slate-700">{item.label}</span>
                      </div>
                      <span className="text-sm font-black text-slate-900">{formatPercent(item.percentage)}</span>
                    </div>
                    <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100">
                      <motion.div
                        initial={{ width: 0 }}
                        animate={{ width: `${Number(item.percentage || 0)}%` }}
                        transition={{ duration: 0.6, ease: 'easeOut' }}
                        className={`h-full rounded-full ${tone.color}`}
                      />
                    </div>
                    <p className="mt-1 text-[10px] font-bold text-slate-400">
                      {formatCurrency(item.grossAmount || 0, currencyCode)} · {item.bookingCount} lượt đặt
                    </p>
                  </div>
                );
              })}
            </div>
          )}

          <div className="mt-6 border-t border-slate-100 pt-4">
            <div className="flex items-center justify-between">
              <span className="text-sm font-black text-slate-900">Tổng</span>
              <span className="text-xl font-black text-slate-900">
                {formatCurrency(dashboard.summary?.grossAmount || 0, currencyCode)}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-100 bg-white shadow-sm">
        <div className="flex items-center justify-between border-b border-slate-100 p-5">
          <h2 className="font-black text-slate-900">Dịch vụ bán chạy</h2>
          <span className="rounded-xl bg-slate-50 px-3 py-2 text-xs font-bold text-slate-600">Theo doanh thu</span>
        </div>
        {dashboard.topProducts.length === 0 ? (
          <div className="px-5 py-10 text-center text-sm font-bold text-slate-500">
            Chưa có booking phát sinh doanh thu trong kỳ này.
          </div>
        ) : (
          <div className="divide-y divide-slate-50">
            {dashboard.topProducts.map((item, index) => (
              <motion.div
                key={`${item.name}-${item.productType}-${index}`}
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: index * 0.05 }}
                className="flex items-center gap-4 px-5 py-4 transition-all hover:bg-slate-50"
              >
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-sm font-black text-slate-500">
                  {index + 1}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-bold text-slate-900">{item.name}</p>
                  <p className="text-xs font-bold text-slate-400">
                    {item.productTypeLabel} · {item.bookingCount} lượt đặt
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-black text-slate-900">{formatCurrency(item.grossAmount || 0, currencyCode)}</p>
                  <p className="text-xs font-bold text-slate-400">
                    Net {formatCurrency(item.netAmount || 0, currencyCode)}
                  </p>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
