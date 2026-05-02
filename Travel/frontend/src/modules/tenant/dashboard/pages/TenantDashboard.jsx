import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  ArrowRight,
  BadgeCheck,
  CalendarDays,
  CheckCircle2,
  Clock,
  Loader2,
  MessageSquare,
  PackageCheck,
  RefreshCw,
  Star,
  Ticket,
  Wallet,
} from 'lucide-react';
import {
  getTenantCommerceFinance,
  listTenantCommerceBookings,
  listTenantCommerceReviews,
} from '../../../../services/commerceBackofficeService';
import { useAuthSession } from '../../../auth/hooks/useAuthSession';
import {
  getCurrentTenantConfig,
  getTenantAccessiblePath,
  getTenantDisplayName,
  getTenantOperatorBadge,
  hasTenantPermission,
} from '../../../auth/types';
import { formatCurrency } from '../../train/utils/presentation';

const MODULE_LABEL = {
  bus: 'Nhà xe',
  train: 'Đường sắt',
  flight: 'Hãng bay',
  hotel: 'Khách sạn',
  tour: 'Tour',
};

const PRODUCT_TYPE = {
  1: 'Xe khách',
  2: 'Tàu',
  3: 'Máy bay',
  4: 'Khách sạn',
  5: 'Tour',
};

const ORDER_STATUS = {
  1: { label: 'Chờ thanh toán', color: 'bg-amber-50 text-amber-700', icon: <Clock size={12} /> },
  2: { label: 'Đã thanh toán', color: 'bg-emerald-50 text-emerald-700', icon: <CheckCircle2 size={12} /> },
  3: { label: 'Đã xuất vé', color: 'bg-sky-50 text-sky-700', icon: <Ticket size={12} /> },
  4: { label: 'Hoàn tất', color: 'bg-emerald-50 text-emerald-700', icon: <PackageCheck size={12} /> },
  5: { label: 'Đã hủy', color: 'bg-rose-50 text-rose-700', icon: <Clock size={12} /> },
  8: { label: 'Hoàn tiền', color: 'bg-orange-50 text-orange-700', icon: <RefreshCw size={12} /> },
  9: { label: 'Hoàn một phần', color: 'bg-orange-50 text-orange-700', icon: <RefreshCw size={12} /> },
  10: { label: 'Đã hoàn tiền', color: 'bg-orange-50 text-orange-700', icon: <RefreshCw size={12} /> },
};

function formatDateTime(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

function getStatusConfig(status) {
  return ORDER_STATUS[Number(status)] || { label: 'Không rõ', color: 'bg-slate-100 text-slate-600', icon: <Clock size={12} /> };
}

function getShortError(error) {
  const message = error?.message || '';
  if (!message) {
    return '';
  }

  if (message.includes('403') || message.toLowerCase().includes('forbidden')) {
    return 'Tài khoản chưa có quyền xem một số dữ liệu.';
  }

  return message;
}

export default function TenantDashboard() {
  const session = useAuthSession();
  const tenantConfig = getCurrentTenantConfig(session);
  const tenantName = getTenantDisplayName(session);
  const operatorBadge = getTenantOperatorBadge(session);
  const inventoryPath = getTenantAccessiblePath(session);
  const canReadBookings = hasTenantPermission(session, 'tenant.bookings.read');
  const canReadReviews = hasTenantPermission(session, 'tenant.reviews.read');
  const canReadFinance = hasTenantPermission(session, 'tenant.finance.read');

  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [softError, setSoftError] = useState('');
  const [bookings, setBookings] = useState([]);
  const [bookingSummary, setBookingSummary] = useState({});
  const [financeSummary, setFinanceSummary] = useState({});
  const [reviewSummary, setReviewSummary] = useState({});

  const loadDashboard = useCallback(async () => {
    if (!session.isReady) {
      return;
    }

    const [bookingResult, financeResult, reviewResult] = await Promise.all([
      canReadBookings
        ? listTenantCommerceBookings({ page: 1, pageSize: 6 }).then((data) => ({ data })).catch((error) => ({ error }))
        : Promise.resolve({ data: null }),
      canReadFinance
        ? getTenantCommerceFinance().then((data) => ({ data })).catch((error) => ({ error }))
        : Promise.resolve({ data: null }),
      canReadReviews
        ? listTenantCommerceReviews({ page: 1, pageSize: 6 }).then((data) => ({ data })).catch((error) => ({ error }))
        : Promise.resolve({ data: null }),
    ]);

    if (bookingResult.data) {
      setBookings(Array.isArray(bookingResult.data.items) ? bookingResult.data.items : []);
      setBookingSummary(bookingResult.data.summary || {});
    } else {
      setBookings([]);
      setBookingSummary({});
    }

    if (financeResult.data) {
      setFinanceSummary(financeResult.data.summary || {});
    } else {
      setFinanceSummary({});
    }

    if (reviewResult.data) {
      setReviewSummary(reviewResult.data.summary || {});
    } else {
      setReviewSummary({});
    }

    const firstError = bookingResult.error || financeResult.error || reviewResult.error;
    setSoftError(getShortError(firstError));
    setLoading(false);
    setRefreshing(false);
  }, [canReadBookings, canReadFinance, canReadReviews, session.isReady]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      loadDashboard();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadDashboard]);

  const stats = useMemo(() => [
    {
      label: 'Đơn hàng',
      value: canReadBookings ? (bookingSummary.totalCount ?? 0) : '--',
      sub: canReadBookings ? 'Booking thuộc tenant hiện tại' : 'Chưa có quyền xem',
      color: 'bg-slate-900 text-white',
      icon: <Ticket size={18} />,
      to: '/tenant/bookings',
    },
    {
      label: 'Doanh thu tháng',
      value: canReadFinance ? formatCurrency(financeSummary.currentMonthGrossAmount || 0, 'VND') : '--',
      sub: canReadFinance ? 'Gross tháng hiện tại' : 'Chưa có quyền tài chính',
      color: 'bg-emerald-50 text-emerald-700',
      icon: <Wallet size={18} />,
      to: '/tenant/finance',
    },
    {
      label: 'Đánh giá',
      value: canReadReviews ? (reviewSummary.totalCount ?? 0) : '--',
      sub: canReadReviews ? `${Number(reviewSummary.averageRating || 0).toFixed(1)} điểm trung bình` : 'Chưa có quyền xem',
      color: 'bg-amber-50 text-amber-700',
      icon: <Star size={18} fill="currentColor" />,
      to: '/tenant/reviews',
    },
    {
      label: 'Chờ xử lý',
      value: canReadBookings ? ((bookingSummary.pendingCount || 0) + (bookingSummary.refundAttentionCount || 0)) : '--',
      sub: 'Thanh toán hoặc hoàn tiền',
      color: 'bg-blue-50 text-blue-700',
      icon: <CalendarDays size={18} />,
      to: '/tenant/bookings',
    },
  ], [bookingSummary, canReadBookings, canReadFinance, canReadReviews, financeSummary, reviewSummary]);

  function refresh() {
    setRefreshing(true);
    loadDashboard();
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1 rounded-lg bg-blue-50 px-2.5 py-1 text-[10px] font-black uppercase tracking-widest text-blue-700">
              <BadgeCheck size={12} />
              {operatorBadge}
            </span>
            <span className="rounded-lg bg-slate-100 px-2.5 py-1 text-[10px] font-black uppercase tracking-widest text-slate-500">
              {MODULE_LABEL[tenantConfig?.module] || 'Partner'}
            </span>
          </div>
          <h1 className="text-2xl font-black text-slate-900">{tenantName}</h1>
          <p className="mt-1 text-sm font-medium text-slate-500">
            Tổng quan booking, doanh thu và đánh giá của tenant đang đăng nhập.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link
            to={inventoryPath}
            className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-700 transition hover:border-blue-200 hover:text-blue-600"
          >
            Kho dịch vụ
            <ArrowRight size={16} />
          </Link>
          <button
            type="button"
            onClick={refresh}
            disabled={refreshing || loading}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-slate-900 px-5 py-3 text-sm font-black text-white transition hover:bg-blue-600 disabled:opacity-60"
          >
            {refreshing || loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
            Tải lại
          </button>
        </div>
      </div>

      {softError ? (
        <div className="rounded-xl border border-amber-100 bg-amber-50 px-5 py-4 text-sm font-bold text-amber-700">
          {softError}
        </div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {stats.map((item, index) => {
          const card = (
            <motion.div
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.04 }}
              className={`h-full rounded-xl border border-slate-100 p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md ${item.color}`}
            >
              <div className={`mb-4 flex h-9 w-9 items-center justify-center rounded-xl ${item.color.includes('slate-900') ? 'bg-white/10' : 'bg-white'}`}>
                {item.icon}
              </div>
              <p className="text-3xl font-black">{loading ? '--' : item.value}</p>
              <p className={`mt-1 text-[10px] font-black uppercase tracking-widest ${item.color.includes('slate-900') ? 'text-white/60' : 'text-current/70'}`}>
                {item.label}
              </p>
              <p className={`mt-1 text-xs font-bold ${item.color.includes('slate-900') ? 'text-white/70' : 'text-slate-500'}`}>
                {item.sub}
              </p>
            </motion.div>
          );

          return item.to && item.value !== '--' ? (
            <Link key={item.label} to={item.to}>
              {card}
            </Link>
          ) : (
            <div key={item.label}>{card}</div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <div className="overflow-hidden rounded-xl border border-slate-100 bg-white shadow-sm xl:col-span-2">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
            <div>
              <h2 className="font-black text-slate-900">Booking mới nhất</h2>
              <p className="mt-0.5 text-xs font-bold text-slate-400">Các đơn gần đây của tenant</p>
            </div>
            {canReadBookings ? (
              <Link to="/tenant/bookings" className="inline-flex items-center gap-1 text-xs font-black text-blue-600">
                Xem tất cả
                <ArrowRight size={14} />
              </Link>
            ) : null}
          </div>

          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="flex items-center gap-3 px-5 py-10 text-sm font-bold text-slate-400">
                <Loader2 size={16} className="animate-spin" />
                Đang tải booking...
              </div>
            ) : !canReadBookings ? (
              <div className="px-5 py-12 text-center text-sm font-bold text-slate-400">
                Tài khoản này chưa có quyền xem booking.
              </div>
            ) : bookings.length === 0 ? (
              <div className="px-5 py-12 text-center text-sm font-bold text-slate-400">
                Chưa có booking nào cho tenant hiện tại.
              </div>
            ) : (
              bookings.map((booking) => {
                const statusConfig = getStatusConfig(booking.status);

                return (
                  <div key={booking.id} className="grid grid-cols-1 gap-3 px-5 py-4 transition hover:bg-slate-50 md:grid-cols-12 md:items-center md:gap-4">
                    <div className="md:col-span-5">
                      <p className="text-sm font-black text-slate-900">{booking.orderCode}</p>
                      <p className="mt-1 text-xs font-bold text-slate-500">{booking.serviceTitle || '--'}</p>
                      <p className="mt-1 text-[10px] font-black uppercase tracking-widest text-slate-400">
                        {PRODUCT_TYPE[Number(booking.productType)] || 'Dịch vụ'}
                      </p>
                    </div>
                    <div className="md:col-span-3">
                      <p className="text-sm font-bold text-slate-800">{booking.customerName || '--'}</p>
                      <p className="mt-1 text-[10px] font-bold text-slate-400">{formatDateTime(booking.createdAt)}</p>
                    </div>
                    <div className="md:col-span-2">
                      <p className="text-sm font-black text-slate-900">{formatCurrency(booking.payableAmount || 0, booking.currencyCode || 'VND')}</p>
                    </div>
                    <div className="md:col-span-2">
                      <span className={`inline-flex items-center gap-1 rounded-lg px-2.5 py-1 text-[10px] font-black uppercase ${statusConfig.color}`}>
                        {statusConfig.icon}
                        {statusConfig.label}
                      </span>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        <div className="space-y-6">
          <div className="rounded-xl border border-slate-100 bg-white p-5 shadow-sm">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="font-black text-slate-900">Tài chính</h2>
                <p className="mt-0.5 text-xs font-bold text-slate-400">Đối soát tenant</p>
              </div>
              <Wallet size={18} className="text-slate-400" />
            </div>
            <div className="space-y-3">
              <div className="rounded-xl bg-slate-50 px-4 py-3">
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Net tháng này</p>
                <p className="mt-1 text-xl font-black text-slate-900">
                  {loading || !canReadFinance ? '--' : formatCurrency(financeSummary.currentMonthNetAmount || 0, 'VND')}
                </p>
              </div>
              <div className="rounded-xl bg-blue-50 px-4 py-3">
                <p className="text-[10px] font-black uppercase tracking-widest text-blue-400">Chờ thanh toán</p>
                <p className="mt-1 text-xl font-black text-blue-700">
                  {loading || !canReadFinance ? '--' : formatCurrency(financeSummary.pendingSettlementAmount || 0, 'VND')}
                </p>
              </div>
            </div>
          </div>

          <div className="rounded-xl border border-slate-100 bg-white p-5 shadow-sm">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="font-black text-slate-900">Đánh giá</h2>
                <p className="mt-0.5 text-xs font-bold text-slate-400">Chất lượng dịch vụ</p>
              </div>
              <MessageSquare size={18} className="text-slate-400" />
            </div>
            <div className="flex items-center gap-4">
              <div>
                <p className="text-4xl font-black text-slate-900">
                  {loading || !canReadReviews ? '--' : Number(reviewSummary.averageRating || 0).toFixed(1)}
                </p>
                <div className="mt-1 flex gap-0.5">
                  {[0, 1, 2, 3, 4].map((index) => (
                    <Star
                      key={index}
                      size={14}
                      className={index < Math.round(Number(reviewSummary.averageRating || 0)) ? 'text-amber-400' : 'text-slate-200'}
                      fill="currentColor"
                    />
                  ))}
                </div>
              </div>
              <div className="flex-1 space-y-2">
                <div className="rounded-xl bg-slate-50 px-4 py-3">
                  <p className="text-sm font-black text-slate-900">{loading || !canReadReviews ? '--' : reviewSummary.totalCount ?? 0}</p>
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tổng đánh giá</p>
                </div>
                <div className="rounded-xl bg-emerald-50 px-4 py-3">
                  <p className="text-sm font-black text-emerald-700">{loading || !canReadReviews ? '--' : reviewSummary.publishedCount ?? 0}</p>
                  <p className="text-[10px] font-black uppercase tracking-widest text-emerald-500">Công khai</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
