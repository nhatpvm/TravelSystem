import React, { useEffect, useMemo, useState } from 'react';
import {
  Bus,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Compass,
  Eye,
  Hotel,
  MessageSquare,
  Plane,
  RefreshCw,
  RotateCcw,
  Search,
  Ticket,
  XCircle,
} from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { getAdminCommerceBooking, listAdminCommerceBookings } from '../../../services/commerceBackofficeService';
import {
  CUSTOMER_ORDER_STATUS,
  CUSTOMER_PAYMENT_STATUS,
  CUSTOMER_PRODUCT,
  CUSTOMER_REFUND_STATUS,
  CUSTOMER_TICKET_STATUS,
  formatCustomerOrderStatusLabel,
  formatCustomerPaymentStatusLabel,
  formatCustomerProductLabel,
  formatCustomerRefundStatusLabel,
  formatCustomerTicketStatusLabel,
  getCustomerOrderStatusClass,
  getCustomerPaymentStatusClass,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const TYPE_ICON = {
  [CUSTOMER_PRODUCT.FLIGHT]: <Plane size={13} />,
  [CUSTOMER_PRODUCT.BUS]: <Bus size={13} />,
  [CUSTOMER_PRODUCT.HOTEL]: <Hotel size={13} />,
  [CUSTOMER_PRODUCT.TOUR]: <Compass size={13} />,
};

const TYPE_BG = {
  [CUSTOMER_PRODUCT.FLIGHT]: 'bg-sky-50 text-sky-600',
  [CUSTOMER_PRODUCT.BUS]: 'bg-indigo-50 text-indigo-600',
  [CUSTOMER_PRODUCT.HOTEL]: 'bg-emerald-50 text-emerald-600',
  [CUSTOMER_PRODUCT.TOUR]: 'bg-amber-50 text-amber-600',
  [CUSTOMER_PRODUCT.TRAIN]: 'bg-slate-100 text-slate-600',
};

const FILTERS = [
  { value: 'all', label: 'Tat ca' },
  { value: 'pending', label: 'Cho thanh toan' },
  { value: 'paid', label: 'Da thanh toan' },
  { value: 'issued', label: 'Da phat hanh' },
  { value: 'closed', label: 'Hoan/Huy' },
];

function getFallbackTypeConfig(productType) {
  return TYPE_BG[productType] || 'bg-slate-100 text-slate-600';
}

function getSupportConfig(item) {
  if ((item?.openSupportTicketCount || 0) > 0) {
    return { label: `${item.openSupportTicketCount} dang mo`, color: 'text-rose-600' };
  }

  if ((item?.supportTicketCount || 0) > 0) {
    return { label: `${item.supportTicketCount} ticket`, color: 'text-blue-600' };
  }

  return { label: 'Chua co', color: 'text-slate-400' };
}

function getRefundClass(value) {
  switch (Number(value || 0)) {
    case CUSTOMER_REFUND_STATUS.REQUESTED:
    case CUSTOMER_REFUND_STATUS.UNDER_REVIEW:
    case CUSTOMER_REFUND_STATUS.PROCESSING:
      return 'bg-amber-100 text-amber-700';
    case CUSTOMER_REFUND_STATUS.APPROVED:
      return 'bg-blue-100 text-blue-700';
    case CUSTOMER_REFUND_STATUS.REFUNDED_PARTIAL:
    case CUSTOMER_REFUND_STATUS.REFUNDED_FULL:
      return 'bg-sky-100 text-sky-700';
    case CUSTOMER_REFUND_STATUS.REJECTED:
      return 'bg-rose-100 text-rose-700';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

function getTicketClass(value) {
  switch (Number(value || 0)) {
    case CUSTOMER_TICKET_STATUS.ISSUED:
      return 'bg-emerald-100 text-emerald-700';
    case CUSTOMER_TICKET_STATUS.PENDING:
      return 'bg-amber-100 text-amber-700';
    case CUSTOMER_TICKET_STATUS.CANCELLED:
    case CUSTOMER_TICKET_STATUS.REFUNDED:
      return 'bg-rose-100 text-rose-700';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

function formatSettlementLabel(value) {
  switch (Number(value || 0)) {
    case 1:
      return 'Chua doi soat';
    case 2:
      return 'Dang doi soat';
    case 3:
      return 'Da settlement';
    case 4:
      return 'Da dieu chinh';
    case 5:
      return 'Tam giu';
    default:
      return 'Dang cap nhat';
  }
}

function getTimelineDotClass(tone) {
  switch (tone) {
    case 'success':
      return 'bg-emerald-500';
    case 'warning':
      return 'bg-amber-500';
    case 'danger':
      return 'bg-rose-500';
    case 'info':
      return 'bg-blue-500';
    default:
      return 'bg-slate-400';
  }
}

function matchesFilter(item, filter) {
  switch (filter) {
    case 'pending':
      return Number(item.status) === CUSTOMER_ORDER_STATUS.PENDING_PAYMENT;
    case 'paid':
      return Number(item.paymentStatus) === CUSTOMER_PAYMENT_STATUS.PAID
        || Number(item.status) === CUSTOMER_ORDER_STATUS.PAID;
    case 'issued':
      return Number(item.ticketStatus) === CUSTOMER_TICKET_STATUS.ISSUED
        || Number(item.status) === CUSTOMER_ORDER_STATUS.TICKET_ISSUED
        || Number(item.status) === CUSTOMER_ORDER_STATUS.COMPLETED;
    case 'closed':
      return Number(item.status) === CUSTOMER_ORDER_STATUS.CANCELLED
        || Number(item.status) === CUSTOMER_ORDER_STATUS.EXPIRED
        || Number(item.status) === CUSTOMER_ORDER_STATUS.FAILED
        || Number(item.status) === CUSTOMER_ORDER_STATUS.REFUNDED_PARTIAL
        || Number(item.status) === CUSTOMER_ORDER_STATUS.REFUNDED_FULL
        || Number(item.refundStatus) === CUSTOMER_REFUND_STATUS.REFUNDED_PARTIAL
        || Number(item.refundStatus) === CUSTOMER_REFUND_STATUS.REFUNDED_FULL;
    default:
      return true;
  }
}

export default function AdminBookingsPage() {
  const [searchParams] = useSearchParams();
  const [search, setSearch] = useState(() => searchParams.get('q') || '');
  const [statusFilter, setStatusFilter] = useState('all');
  const [expanded, setExpanded] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [summary, setSummary] = useState({
    totalCount: 0,
    pendingCount: 0,
    paidCount: 0,
    cancelledCount: 0,
    refundAttentionCount: 0,
    totalGrossAmount: 0,
  });
  const [bookings, setBookings] = useState([]);
  const [bookingDetails, setBookingDetails] = useState({});
  const [detailLoading, setDetailLoading] = useState({});
  const [detailErrors, setDetailErrors] = useState({});

  const loadBookingsRef = useLatestRef(loadBookings);

  useEffect(() => {
    setSearch(searchParams.get('q') || '');
  }, [searchParams]);

  async function loadBookings(nextRefreshing = false) {
    if (nextRefreshing) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    setError('');

    try {
      const response = await listAdminCommerceBookings({
        q: search.trim() || undefined,
      });

      setSummary(response?.summary || {});
      setBookings(Array.isArray(response?.items) ? response.items : []);
    } catch (requestError) {
      setError(requestError.message || 'Khong the tai danh sach don dat.');
      setSummary({
        totalCount: 0,
        pendingCount: 0,
        paidCount: 0,
        cancelledCount: 0,
        refundAttentionCount: 0,
        totalGrossAmount: 0,
      });
      setBookings([]);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  async function loadBookingDetail(orderId) {
    setDetailLoading((current) => ({ ...current, [orderId]: true }));
    setDetailErrors((current) => ({ ...current, [orderId]: '' }));

    try {
      const response = await getAdminCommerceBooking(orderId);
      setBookingDetails((current) => ({ ...current, [orderId]: response }));
    } catch (requestError) {
      setDetailErrors((current) => ({
        ...current,
        [orderId]: requestError.message || 'Khong the tai chi tiet booking.',
      }));
    } finally {
      setDetailLoading((current) => ({ ...current, [orderId]: false }));
    }
  }

  function handleToggleBooking(item) {
    const nextExpanded = expanded === item.id ? null : item.id;
    setExpanded(nextExpanded);

    if (nextExpanded && !bookingDetails[item.id] && !detailLoading[item.id]) {
      loadBookingDetail(item.id);
    }
  }

  useEffect(() => {
    loadBookingsRef.current();
  }, [loadBookingsRef, search]);

  const filteredBookings = useMemo(
    () => bookings.filter((item) => matchesFilter(item, statusFilter)),
    [bookings, statusFilter],
  );

  const stats = useMemo(() => ([
    { label: 'Tong don', value: summary.totalCount || 0, className: 'bg-slate-900 text-white' },
    { label: 'Cho thanh toan', value: summary.pendingCount || 0, className: 'bg-amber-50' },
    { label: 'Da thanh toan', value: summary.paidCount || 0, className: 'bg-emerald-50' },
    { label: 'Hoan/Huy', value: (summary.cancelledCount || 0) + (summary.refundAttentionCount || 0), className: 'bg-rose-50' },
  ]), [summary]);

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Tat ca Don dat</h1>
          <p className="text-slate-500 text-sm mt-1">Tim kiem va theo doi booking that tren toan bo marketplace, gom payment, ticket, refund va support.</p>
        </div>
        <button
          type="button"
          onClick={() => loadBookingsRef.current(true)}
          className="px-4 py-3 bg-slate-900 text-white rounded-xl text-[10px] font-black uppercase tracking-widest flex items-center gap-2 hover:bg-blue-600 transition-all"
        >
          <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />
          Tai lai
        </button>
      </div>

      {error ? (
        <div className="mb-5 rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {stats.map((item, index) => (
          <motion.div
            key={item.label}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.className}`}
          >
            <p className={`text-3xl font-black ${item.className.includes('slate-900') ? 'text-white' : 'text-slate-900'}`}>
              {loading ? '--' : item.value}
            </p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${item.className.includes('slate-900') ? 'text-white/60' : 'text-slate-400'}`}>
              {item.label}
            </p>
          </motion.div>
        ))}
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Ma don, khach, tenant, payment, ticket..."
            className="bg-transparent py-3 flex-1 text-sm font-medium outline-none"
          />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1 flex-wrap">
          {FILTERS.map((filter) => (
            <button
              key={filter.value}
              onClick={() => setStatusFilter(filter.value)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${statusFilter === filter.value ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}
            >
              {filter.label}
            </button>
          ))}
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Khach / Tenant</div>
          <div className="col-span-4">Dich vu</div>
          <div className="col-span-2">Số tiền khách trả</div>
          <div className="col-span-2">Trang thai</div>
          <div className="col-span-1"></div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
              Dang tai danh sach don dat...
            </div>
          ) : filteredBookings.length === 0 ? (
            <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
              Chua co don dat phu hop bo loc.
            </div>
          ) : (
            filteredBookings.map((item) => {
              const isExpanded = expanded === item.id;
              const supportConfig = getSupportConfig(item);
              const detail = bookingDetails[item.id];
              const isDetailLoading = !!detailLoading[item.id];
              const detailError = detailErrors[item.id];

              return (
                <div key={item.id}>
                  <div
                    onClick={() => handleToggleBooking(item)}
                    className="grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 hover:bg-slate-50 cursor-pointer items-center transition-all"
                  >
                    <div className="col-span-1 md:col-span-3">
                      <p className="font-black text-slate-900 text-sm">{item.customerName}</p>
                      <p className="text-[10px] text-slate-400 font-bold">{item.tenantName || '--'}</p>
                    </div>
                    <div className="col-span-1 md:col-span-4">
                      <div className="flex items-center gap-2">
                        <span className={`w-6 h-6 rounded-lg flex items-center justify-center ${getFallbackTypeConfig(item.productType)}`}>
                          {TYPE_ICON[item.productType] || <Ticket size={13} />}
                        </span>
                        <div className="min-w-0">
                          <p className="text-sm font-bold text-slate-800 truncate">{item.serviceTitle}</p>
                          <p className="text-[10px] text-slate-400 truncate">{item.orderCode} · {formatCustomerProductLabel(item.productType)}</p>
                        </div>
                      </div>
                    </div>
                    <div className="col-span-1 md:col-span-2">
                      <p className="font-black text-slate-900 text-sm">{formatCurrency(item.payableAmount || 0, item.currencyCode || 'VND')}</p>
                      <p className="text-[10px] text-slate-400 font-bold">Tổng giá gốc {formatCurrency(item.grossAmount || 0, item.currencyCode || 'VND')}</p>
                    </div>
                    <div className="col-span-1 md:col-span-2">
                      <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${getCustomerOrderStatusClass(item.status)}`}>
                        {Number(item.status) === CUSTOMER_ORDER_STATUS.CANCELLED || Number(item.status) === CUSTOMER_ORDER_STATUS.EXPIRED || Number(item.status) === CUSTOMER_ORDER_STATUS.FAILED
                          ? <XCircle size={12} />
                          : <CheckCircle2 size={12} />}
                        {formatCustomerOrderStatusLabel(item.status)}
                      </span>
                    </div>
                    <div className="col-span-1 flex justify-end">
                      {isExpanded ? <ChevronUp size={16} className="text-slate-400" /> : <ChevronDown size={16} className="text-slate-400" />}
                    </div>
                  </div>

                  {isExpanded ? (
                    <motion.div
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      className="bg-slate-50 border-t border-slate-100 px-5 py-4"
                    >
                      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4 text-xs mb-4">
                        {[
                          { label: 'Payment', value: item.paymentCode || '--' },
                          { label: 'Ticket', value: item.ticketCode || '--' },
                          { label: 'Refund', value: item.latestRefundCode || '--' },
                          { label: 'Support', value: supportConfig.label },
                          { label: 'Tổng giá gốc', value: formatCurrency(item.grossAmount || 0, item.currencyCode || 'VND') },
                          { label: 'Hoa hồng platform', value: formatCurrency(item.platformCommissionAmount || 0, item.currencyCode || 'VND') },
                          { label: 'Tiền còn lại cho tenant', value: formatCurrency(item.tenantNetAmount || 0, item.currencyCode || 'VND') },
                          { label: 'Settlement', value: formatSettlementLabel(item.settlementStatus) },
                          { label: 'Ngay tao', value: formatDateTime(item.createdAt) },
                          { label: 'Ngay paid', value: item.paidAt ? formatDateTime(item.paidAt) : '--' },
                          { label: 'Phat hanh ve', value: item.ticketIssuedAt ? formatDateTime(item.ticketIssuedAt) : '--' },
                          { label: 'Refunded', value: formatCurrency(item.refundedAmount || 0, item.currencyCode || 'VND') },
                        ].map((field) => (
                          <div key={field.label} className="bg-white rounded-xl p-3 border border-slate-100">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{field.label}</p>
                            <p className="font-black text-slate-900 mt-1 break-words">{field.value}</p>
                          </div>
                        ))}
                      </div>

                      <div className="flex flex-wrap gap-2 mb-4">
                        <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-xl text-[10px] font-black uppercase ${getCustomerPaymentStatusClass(item.paymentStatus)}`}>
                          {formatCustomerPaymentStatusLabel(item.paymentStatus)}
                        </span>
                        <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-xl text-[10px] font-black uppercase ${getTicketClass(item.ticketStatus)}`}>
                          {formatCustomerTicketStatusLabel(item.ticketStatus)}
                        </span>
                        <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-xl text-[10px] font-black uppercase ${getRefundClass(item.refundStatus)}`}>
                          {formatCustomerRefundStatusLabel(item.refundStatus)}
                        </span>
                        <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-xl text-[10px] font-black uppercase ${supportConfig.color.includes('rose') ? 'bg-rose-100 text-rose-700' : supportConfig.color.includes('blue') ? 'bg-blue-100 text-blue-700' : 'bg-slate-100 text-slate-600'}`}>
                          {supportConfig.label}
                        </span>
                      </div>

                      {item.serviceSubtitle || item.contactEmail || item.contactPhone || item.failureReason ? (
                        <div className="bg-white rounded-2xl border border-slate-100 px-4 py-3 mb-4">
                          {item.serviceSubtitle ? (
                            <p className="text-sm font-bold text-slate-800">{item.serviceSubtitle}</p>
                          ) : null}
                          {(item.contactEmail || item.contactPhone) ? (
                            <p className="text-xs text-slate-500 font-bold mt-1">{item.contactEmail || '--'} · {item.contactPhone || '--'}</p>
                          ) : null}
                          {item.failureReason ? (
                            <p className="text-xs font-bold text-rose-600 mt-2">{item.failureReason}</p>
                          ) : null}
                        </div>
                      ) : null}

                      {isDetailLoading ? (
                        <div className="bg-white rounded-2xl border border-slate-100 px-4 py-5 mb-4 text-center text-xs font-black uppercase tracking-widest text-slate-400">
                          Dang tai chi tiet booking...
                        </div>
                      ) : null}

                      {detailError ? (
                        <div className="bg-rose-50 rounded-2xl border border-rose-100 px-4 py-4 mb-4 text-sm font-bold text-rose-600">
                          {detailError}
                        </div>
                      ) : null}

                      {detail ? (
                        <>
                          <div className="bg-white rounded-2xl border border-slate-100 px-4 py-4 mb-4">
                            <div className="flex items-center justify-between gap-3 mb-3">
                              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Timeline</p>
                              <span className="text-[10px] font-black uppercase tracking-widest text-slate-300">{detail.timeline?.length || 0} su kien</span>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
                              {(detail.timeline || []).map((event) => (
                                <div key={event.key} className="rounded-xl border border-slate-100 bg-slate-50 px-3 py-3">
                                  <div className="flex items-center gap-2">
                                    <span className={`w-2 h-2 rounded-full ${getTimelineDotClass(event.tone)}`} />
                                    <p className="text-sm font-black text-slate-900">{event.title}</p>
                                  </div>
                                  <p className="text-[10px] font-bold text-slate-400 mt-1">{formatDateTime(event.occurredAt)}</p>
                                  {event.description ? (
                                    <p className="text-xs font-bold text-slate-500 mt-1 break-words">{event.description}</p>
                                  ) : null}
                                </div>
                              ))}
                            </div>
                          </div>

                          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4 mb-4">
                            <div className="bg-white rounded-2xl border border-slate-100 p-4">
                              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-3">Payments</p>
                              {(detail.payments || []).length === 0 ? (
                                <p className="text-xs font-bold text-slate-400">Chua co payment.</p>
                              ) : (
                                <div className="space-y-2">
                                  {detail.payments.map((payment) => (
                                    <div key={payment.id} className="rounded-xl bg-slate-50 px-3 py-3">
                                      <div className="flex flex-wrap items-center justify-between gap-2">
                                        <p className="font-black text-slate-900 text-sm">{payment.paymentCode}</p>
                                        <span className={`px-2 py-1 rounded-lg text-[10px] font-black uppercase ${getCustomerPaymentStatusClass(payment.status)}`}>
                                          {formatCustomerPaymentStatusLabel(payment.status)}
                                        </span>
                                      </div>
                                      <p className="text-xs font-bold text-slate-500 mt-1">
                                        {payment.providerInvoiceNumber || '--'} · {formatCurrency(payment.paidAmount || payment.amount || 0, payment.currencyCode || 'VND')}
                                      </p>
                                      <p className="text-[10px] font-bold text-slate-400 mt-1">
                                        Paid: {payment.paidAt ? formatDateTime(payment.paidAt) : '--'} · Webhook: {payment.webhookReceivedAt ? formatDateTime(payment.webhookReceivedAt) : '--'}
                                      </p>
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>

                            <div className="bg-white rounded-2xl border border-slate-100 p-4">
                              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-3">Tickets QR</p>
                              {(detail.tickets || []).length === 0 ? (
                                <p className="text-xs font-bold text-slate-400">Chua co ve QR.</p>
                              ) : (
                                <div className="space-y-2">
                                  {detail.tickets.map((ticketItem) => (
                                    <div key={ticketItem.id} className="flex gap-3 rounded-xl bg-slate-50 px-3 py-3">
                                      <img
                                        src={`https://api.qrserver.com/v1/create-qr-code/?size=86x86&data=${encodeURIComponent(ticketItem.ticketCode)}`}
                                        alt={ticketItem.ticketCode}
                                        className="w-20 h-20 rounded-xl bg-white border border-slate-100 p-1"
                                      />
                                      <div className="min-w-0 flex-1">
                                        <div className="flex flex-wrap items-center justify-between gap-2">
                                          <p className="font-black text-slate-900 text-sm truncate">{ticketItem.ticketCode}</p>
                                          <span className={`px-2 py-1 rounded-lg text-[10px] font-black uppercase ${getTicketClass(ticketItem.status)}`}>
                                            {formatCustomerTicketStatusLabel(ticketItem.status)}
                                          </span>
                                        </div>
                                        <p className="text-xs font-bold text-slate-500 mt-1">{ticketItem.title}</p>
                                        <p className="text-[10px] font-bold text-slate-400 mt-1">{formatDateTime(ticketItem.issuedAt)}</p>
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>

                            <div className="bg-white rounded-2xl border border-slate-100 p-4">
                              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-3">Refunds</p>
                              {(detail.refunds || []).length === 0 ? (
                                <p className="text-xs font-bold text-slate-400">Chua co yeu cau hoan tien.</p>
                              ) : (
                                <div className="space-y-2">
                                  {detail.refunds.map((refund) => (
                                    <div key={refund.id} className="rounded-xl bg-slate-50 px-3 py-3">
                                      <div className="flex flex-wrap items-center justify-between gap-2">
                                        <p className="font-black text-slate-900 text-sm">{refund.refundCode}</p>
                                        <span className={`px-2 py-1 rounded-lg text-[10px] font-black uppercase ${getRefundClass(refund.status)}`}>
                                          {formatCustomerRefundStatusLabel(refund.status)}
                                        </span>
                                      </div>
                                      <p className="text-xs font-bold text-slate-500 mt-1">
                                        Request {formatCurrency(refund.requestedAmount || 0, refund.currencyCode || 'VND')} · Approved {refund.approvedAmount ? formatCurrency(refund.approvedAmount, refund.currencyCode || 'VND') : '--'} · Refunded {refund.refundedAmount ? formatCurrency(refund.refundedAmount, refund.currencyCode || 'VND') : '--'}
                                      </p>
                                      <p className="text-[10px] font-bold text-slate-400 mt-1">
                                        {refund.reasonText || refund.reasonCode || '--'} · {refund.refundReference || 'Chua co ma GD'}
                                      </p>
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>

                            <div className="bg-white rounded-2xl border border-slate-100 p-4">
                              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-3">Support</p>
                              {(detail.supportTickets || []).length === 0 ? (
                                <p className="text-xs font-bold text-slate-400">Chua co support ticket.</p>
                              ) : (
                                <div className="space-y-2">
                                  {detail.supportTickets.map((ticketItem) => (
                                    <div key={ticketItem.id} className="rounded-xl bg-slate-50 px-3 py-3">
                                      <div className="flex flex-wrap items-center justify-between gap-2">
                                        <p className="font-black text-slate-900 text-sm">{ticketItem.ticketCode}</p>
                                        <span className="px-2 py-1 rounded-lg text-[10px] font-black uppercase bg-blue-100 text-blue-700">
                                          {ticketItem.priority}
                                        </span>
                                      </div>
                                      <p className="text-xs font-bold text-slate-500 mt-1">{ticketItem.subject}</p>
                                      <p className="text-[10px] font-bold text-slate-400 mt-1">
                                        {ticketItem.contactEmail || '--'} · {ticketItem.lastActivityAt ? formatDateTime(ticketItem.lastActivityAt) : formatDateTime(ticketItem.createdAt)}
                                      </p>
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>
                          </div>

                          <div className="bg-white rounded-2xl border border-slate-100 p-4 mb-4">
                            <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-3">Settlement lines</p>
                            {(detail.settlementLines || []).length === 0 ? (
                              <p className="text-xs font-bold text-slate-400">Chua vao batch doi soat.</p>
                            ) : (
                              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                                {detail.settlementLines.map((line) => (
                                  <div key={line.id} className="rounded-xl bg-slate-50 px-3 py-3">
                                    <div className="flex flex-wrap items-center justify-between gap-2">
                                      <p className="font-black text-slate-900 text-sm">{line.batchCode || '--'}</p>
                                      <span className="px-2 py-1 rounded-lg text-[10px] font-black uppercase bg-slate-100 text-slate-600">
                                        {formatSettlementLabel(line.status)}
                                      </span>
                                    </div>
                                    <p className="text-xs font-bold text-slate-500 mt-1">{line.description}</p>
                                    <p className="text-[10px] font-bold text-slate-400 mt-1">
                                      Tổng giá gốc {formatCurrency(line.grossAmount || 0, line.currencyCode || 'VND')} · Hoa hồng platform {formatCurrency(line.commissionAmount || 0, line.currencyCode || 'VND')} · Tiền refund trừ payout {formatCurrency(line.refundAmount || 0, line.currencyCode || 'VND')}
                                    </p>
                                    <p className="text-sm font-black text-slate-900 mt-1">
                                      Net payout {formatCurrency(line.netPayoutAmount || 0, line.currencyCode || 'VND')}
                                    </p>
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        </>
                      ) : null}

                      <div className="flex flex-wrap gap-2">
                        <Link
                          to={`/admin/payments?q=${encodeURIComponent(item.orderCode)}`}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-blue-50 hover:text-blue-600 transition-all"
                        >
                          <Eye size={13} />
                          Thanh toan
                        </Link>
                        <Link
                          to={`/admin/refunds?q=${encodeURIComponent(item.orderCode)}`}
                          className="flex items-center gap-1.5 px-4 py-2 bg-amber-50 text-amber-600 rounded-xl text-[10px] font-black uppercase border border-amber-100 hover:bg-amber-100 transition-all"
                        >
                          <RotateCcw size={13} />
                          Hoan tien
                        </Link>
                        <Link
                          to={`/admin/support?q=${encodeURIComponent(item.orderCode)}`}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-slate-100 transition-all"
                        >
                          <MessageSquare size={13} />
                          Support
                        </Link>
                      </div>
                    </motion.div>
                  ) : null}
                </div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
