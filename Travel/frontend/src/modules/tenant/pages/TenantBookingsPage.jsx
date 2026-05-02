import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  Banknote,
  CheckCircle2,
  Clock,
  Loader2,
  PackageCheck,
  RefreshCw,
  Search,
  Ticket,
  XCircle,
} from 'lucide-react';
import { listTenantCommerceBookings } from '../../../services/commerceBackofficeService';
import { formatCurrency } from '../train/utils/presentation';

const STATUS_OPTIONS = [
  { value: 'all', label: 'Tất cả' },
  { value: '1', label: 'Chờ thanh toán' },
  { value: '2', label: 'Đã thanh toán' },
  { value: '3', label: 'Đã xuất vé' },
  { value: '4', label: 'Hoàn tất' },
  { value: '5', label: 'Đã hủy' },
  { value: '8', label: 'Hoàn tiền' },
];

const ORDER_STATUS = {
  1: { label: 'Chờ thanh toán', color: 'bg-amber-50 text-amber-700', icon: <Clock size={12} /> },
  2: { label: 'Đã thanh toán', color: 'bg-emerald-50 text-emerald-700', icon: <CheckCircle2 size={12} /> },
  3: { label: 'Đã xuất vé', color: 'bg-sky-50 text-sky-700', icon: <Ticket size={12} /> },
  4: { label: 'Hoàn tất', color: 'bg-emerald-50 text-emerald-700', icon: <PackageCheck size={12} /> },
  5: { label: 'Đã hủy', color: 'bg-rose-50 text-rose-700', icon: <XCircle size={12} /> },
  6: { label: 'Hết hạn', color: 'bg-slate-100 text-slate-600', icon: <XCircle size={12} /> },
  7: { label: 'Thất bại', color: 'bg-rose-50 text-rose-700', icon: <XCircle size={12} /> },
  8: { label: 'Yêu cầu hoàn tiền', color: 'bg-orange-50 text-orange-700', icon: <RefreshCw size={12} /> },
  9: { label: 'Hoàn một phần', color: 'bg-orange-50 text-orange-700', icon: <RefreshCw size={12} /> },
  10: { label: 'Đã hoàn tiền', color: 'bg-orange-50 text-orange-700', icon: <RefreshCw size={12} /> },
};

const PRODUCT_TYPE = {
  1: 'Xe khách',
  2: 'Tàu',
  3: 'Máy bay',
  4: 'Khách sạn',
  5: 'Tour',
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

export default function TenantBookingsPage() {
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [bookings, setBookings] = useState([]);
  const [summary, setSummary] = useState({});

  const loadData = useCallback(async () => {
    setError('');
    try {
      const response = await listTenantCommerceBookings({
        q: search.trim() || undefined,
        status: statusFilter === 'all' ? undefined : statusFilter,
      });
      setBookings(Array.isArray(response?.items) ? response.items : []);
      setSummary(response?.summary || {});
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải đơn hàng tenant.');
      setBookings([]);
      setSummary({});
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [search, statusFilter]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setLoading(true);
      loadData();
    }, 200);

    return () => window.clearTimeout(timer);
  }, [loadData]);

  const stats = useMemo(() => [
    {
      label: 'Tổng đơn',
      value: summary.totalCount ?? bookings.length,
      sub: 'Tất cả booking trong tenant',
      color: 'bg-slate-900 text-white',
      icon: <Ticket size={18} />,
    },
    {
      label: 'Chờ thanh toán',
      value: summary.pendingCount ?? 0,
      sub: 'Cần khách hoàn tất thanh toán',
      color: 'bg-amber-50 text-amber-700',
      icon: <Clock size={18} />,
    },
    {
      label: 'Đã thanh toán',
      value: summary.paidCount ?? 0,
      sub: 'Đã ghi nhận tiền hoặc vé',
      color: 'bg-emerald-50 text-emerald-700',
      icon: <CheckCircle2 size={18} />,
    },
    {
      label: 'Hoàn tiền',
      value: summary.refundAttentionCount ?? 0,
      sub: 'Có phát sinh refund',
      color: 'bg-orange-50 text-orange-700',
      icon: <RefreshCw size={18} />,
    },
  ], [bookings.length, summary]);

  function refresh() {
    setRefreshing(true);
    loadData();
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Đơn hàng tenant</h1>
          <p className="mt-1 text-sm font-medium text-slate-500">
            Theo dõi các đơn khách đặt thuộc tenant hiện tại, bao gồm thanh toán, vé, hoàn tiền và đối soát.
          </p>
        </div>
        <button
          type="button"
          onClick={refresh}
          disabled={refreshing}
          className="inline-flex items-center justify-center gap-2 rounded-xl bg-slate-900 px-5 py-3 text-sm font-black text-white transition hover:bg-blue-600 disabled:opacity-60"
        >
          {refreshing ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
          Tải lại
        </button>
      </div>

      {error ? (
        <div className="rounded-xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {stats.map((item, index) => (
          <motion.div
            key={item.label}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.04 }}
            className={`rounded-xl border border-slate-100 p-5 shadow-sm ${item.color}`}
          >
            <div className={`mb-4 flex h-9 w-9 items-center justify-center rounded-xl ${item.color.includes('slate-900') ? 'bg-white/10' : 'bg-white'}`}>
              {item.icon}
            </div>
            <p className="text-3xl font-black">{loading ? '--' : item.value}</p>
            <p className={`mt-1 text-[10px] font-black uppercase tracking-widest ${item.color.includes('slate-900') ? 'text-white/60' : 'text-current/70'}`}>
              {item.label}
            </p>
            <p className={`mt-1 text-xs font-bold ${item.color.includes('slate-900') ? 'text-white/70' : 'text-slate-500'}`}>{item.sub}</p>
          </motion.div>
        ))}
      </div>

      <div className="flex flex-col gap-3 rounded-xl border border-slate-100 bg-white p-4 shadow-sm xl:flex-row">
        <div className="flex min-w-0 flex-1 items-center gap-2 rounded-xl bg-slate-50 px-4">
          <Search size={16} className="shrink-0 text-slate-400" />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm mã đơn, khách hàng, email, SĐT, mã vé..."
            className="min-w-0 flex-1 bg-transparent py-3 text-sm font-bold text-slate-700 outline-none"
          />
        </div>
        <div className="flex gap-1 overflow-x-auto rounded-xl border border-slate-100 bg-slate-50 p-1">
          {STATUS_OPTIONS.map((item) => (
            <button
              key={item.value}
              type="button"
              onClick={() => setStatusFilter(item.value)}
              className={`whitespace-nowrap rounded-lg px-3 py-2 text-[10px] font-black uppercase tracking-widest transition ${
                statusFilter === item.value ? 'bg-white text-blue-600 shadow-sm' : 'text-slate-400 hover:text-slate-700'
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-100 bg-white shadow-sm">
        <div className="hidden grid-cols-12 gap-4 border-b border-slate-100 bg-slate-50 px-5 py-3 text-[10px] font-black uppercase tracking-widest text-slate-400 md:grid">
          <div className="col-span-4">Đơn hàng</div>
          <div className="col-span-2">Khách</div>
          <div className="col-span-2">Thanh toán</div>
          <div className="col-span-2">Trạng thái</div>
          <div className="col-span-2">Thời gian</div>
        </div>

        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="flex items-center gap-3 px-5 py-10 text-sm font-bold text-slate-400">
              <Loader2 size={16} className="animate-spin" />
              Đang tải đơn hàng...
            </div>
          ) : bookings.length === 0 ? (
            <div className="px-5 py-14 text-center">
              <p className="text-sm font-black text-slate-500">Chưa có đơn hàng phù hợp.</p>
              <p className="mt-1 text-xs font-bold text-slate-400">Khi khách đặt dịch vụ của tenant, đơn sẽ xuất hiện tại đây.</p>
            </div>
          ) : (
            bookings.map((booking) => {
              const statusConfig = getStatusConfig(booking.status);

              return (
                <div key={booking.id} className="grid grid-cols-1 gap-3 px-5 py-4 transition hover:bg-slate-50 md:grid-cols-12 md:items-center md:gap-4">
                  <div className="md:col-span-4">
                    <p className="text-sm font-black text-slate-900">{booking.orderCode}</p>
                    <p className="mt-1 text-xs font-bold text-slate-500">{booking.serviceTitle || '--'}</p>
                    <p className="mt-1 text-[10px] font-black uppercase tracking-widest text-slate-400">
                      {PRODUCT_TYPE[Number(booking.productType)] || 'Dịch vụ'} · {booking.ticketCode || booking.paymentCode || 'chưa phát hành mã'}
                    </p>
                  </div>
                  <div className="md:col-span-2">
                    <p className="text-sm font-bold text-slate-800">{booking.customerName || '--'}</p>
                    <p className="mt-1 text-[10px] font-bold text-slate-400">{booking.contactPhone || booking.contactEmail || '--'}</p>
                  </div>
                  <div className="md:col-span-2">
                    <p className="text-sm font-black text-slate-900">{formatCurrency(booking.payableAmount || 0, booking.currencyCode || 'VND')}</p>
                    <p className="mt-1 inline-flex items-center gap-1 text-[10px] font-bold text-slate-400">
                      <Banknote size={11} />
                      Net: {formatCurrency(booking.tenantNetAmount || 0, booking.currencyCode || 'VND')}
                    </p>
                  </div>
                  <div className="md:col-span-2">
                    <span className={`inline-flex items-center gap-1 rounded-lg px-2.5 py-1 text-[10px] font-black uppercase ${statusConfig.color}`}>
                      {statusConfig.icon}
                      {statusConfig.label}
                    </span>
                    {booking.openSupportTicketCount > 0 ? (
                      <p className="mt-1 text-[10px] font-bold text-orange-600">{booking.openSupportTicketCount} yêu cầu hỗ trợ mở</p>
                    ) : null}
                  </div>
                  <div className="text-xs font-bold text-slate-500 md:col-span-2">
                    <p>Tạo: {formatDateTime(booking.createdAt)}</p>
                    <p className="mt-1 text-slate-400">Thanh toán: {formatDateTime(booking.paidAt)}</p>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
