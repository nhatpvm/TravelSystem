import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Clock,
  CreditCard,
  Eye,
  RefreshCw,
  Search,
  XCircle,
} from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { listAdminCommercePayments } from '../../../services/commerceBackofficeService';
import {
  CUSTOMER_PAYMENT_STATUS,
  formatCustomerPaymentStatusLabel,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const STATUS_FILTERS = [
  { value: 'all', label: 'Tất cả' },
  { value: String(CUSTOMER_PAYMENT_STATUS.PAID), label: 'Thành công' },
  { value: String(CUSTOMER_PAYMENT_STATUS.PENDING), label: 'Chờ' },
  { value: String(CUSTOMER_PAYMENT_STATUS.FAILED), label: 'Thất bại' },
  { value: String(CUSTOMER_PAYMENT_STATUS.EXPIRED), label: 'Hết hạn' },
];

function getPaymentStatusConfig(value) {
  switch (Number(value || 0)) {
    case CUSTOMER_PAYMENT_STATUS.PAID:
      return { color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> };
    case CUSTOMER_PAYMENT_STATUS.PENDING:
      return { color: 'bg-amber-100 text-amber-700', icon: <Clock size={12} /> };
    case CUSTOMER_PAYMENT_STATUS.REFUNDED_PARTIAL:
    case CUSTOMER_PAYMENT_STATUS.REFUNDED_FULL:
      return { color: 'bg-sky-100 text-sky-700', icon: <RefreshCw size={12} /> };
    default:
      return { color: 'bg-rose-100 text-rose-700', icon: <XCircle size={12} /> };
  }
}

function getCallbackConfig(item) {
  if (item?.webhookReceivedAt) {
    return { label: 'Nhận CB', color: 'text-emerald-600' };
  }

  if (Number(item?.status) === CUSTOMER_PAYMENT_STATUS.PENDING) {
    return { label: 'Chờ CB', color: 'text-amber-600' };
  }

  return { label: 'Không CB', color: 'text-slate-400' };
}

export default function AdminPaymentsPage() {
  const [searchParams] = useSearchParams();
  const [search, setSearch] = useState(() => searchParams.get('q') || '');
  const [statusFilter, setStatusFilter] = useState('all');
  const [expanded, setExpanded] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [summary, setSummary] = useState({
    totalCount: 0,
    paidCount: 0,
    failedCount: 0,
    totalAmount: 0,
  });
  const [payments, setPayments] = useState([]);

  const loadPaymentsRef = useLatestRef(loadPayments);

  useEffect(() => {
    setSearch(searchParams.get('q') || '');
  }, [searchParams]);

  async function loadPayments(nextRefreshing = false) {
    if (nextRefreshing) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    setError('');

    try {
      const response = await listAdminCommercePayments({
        q: search.trim() || undefined,
        status: statusFilter === 'all' ? undefined : statusFilter,
      });

      setSummary(response?.summary || {});
      setPayments(Array.isArray(response?.items) ? response.items : []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách giao dịch.');
      setSummary({
        totalCount: 0,
        paidCount: 0,
        failedCount: 0,
        totalAmount: 0,
      });
      setPayments([]);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  useEffect(() => {
    loadPaymentsRef.current();
  }, [loadPaymentsRef, search, statusFilter]);

  const stats = useMemo(() => ([
    { label: 'Tổng GD', value: summary.totalCount || 0, className: 'bg-slate-900 text-white' },
    { label: 'Thành công', value: summary.paidCount || 0, className: 'bg-emerald-50' },
    { label: 'Thất bại/HH', value: summary.failedCount || 0, className: 'bg-rose-50' },
    { label: 'Tổng tiền đã ghi nhận', value: formatCurrency(summary.totalAmount || 0, 'VND'), className: 'bg-blue-50' },
  ]), [summary]);

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Thanh toán & Giao dịch</h1>
        <p className="text-slate-500 text-sm mt-1">PaymentIntents, transactions và callback logs thật từ hệ thống commerce.</p>
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
            placeholder="Mã GD, khách, ref…"
            className="bg-transparent py-3 flex-1 text-sm font-medium outline-none"
          />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {STATUS_FILTERS.map((filter) => (
            <button
              key={filter.value}
              onClick={() => setStatusFilter(filter.value)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest whitespace-nowrap transition-all ${statusFilter === filter.value ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}
            >
              {filter.label}
            </button>
          ))}
        </div>
        <button
          type="button"
          onClick={() => loadPaymentsRef.current(true)}
          className="px-4 py-3 bg-slate-900 text-white rounded-xl text-[10px] font-black uppercase tracking-widest flex items-center gap-2 hover:bg-blue-600 transition-all"
        >
          <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />
          Tải lại
        </button>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-3 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Giao dịch / Khách</div>
          <div className="col-span-2">Số tiền giao dịch</div>
          <div className="col-span-2">Phương thức</div>
          <div className="col-span-2">Callback</div>
          <div className="col-span-2">Trạng thái</div>
          <div className="col-span-1"></div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
              Đang tải giao dịch...
            </div>
          ) : payments.length === 0 ? (
            <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
              Chưa có giao dịch phù hợp bộ lọc.
            </div>
          ) : (
            payments.map((item) => {
              const statusConfig = getPaymentStatusConfig(item.status);
              const callbackConfig = getCallbackConfig(item);
              const isExpanded = expanded === item.id;

              return (
                <div key={item.id}>
                  <div
                    onClick={() => setExpanded(isExpanded ? null : item.id)}
                    className="grid grid-cols-2 md:grid-cols-12 gap-3 px-5 py-4 hover:bg-slate-50 cursor-pointer items-center transition-all"
                  >
                    <div className="col-span-1 md:col-span-3">
                      <p className="font-black text-slate-900 text-xs">{item.paymentCode}</p>
                      <p className="text-[10px] text-slate-400 font-bold">{item.customerName}</p>
                    </div>
                    <div className="col-span-1 md:col-span-2 font-black text-slate-900">
                      {formatCurrency(item.paidAmount || item.amount || 0, item.currencyCode || 'VND')}
                    </div>
                    <div className="col-span-1 md:col-span-2 text-xs font-bold text-slate-600">
                      {item.provider === 1 ? 'SePay' : 'Provider'} · {item.method === 1 ? 'QR / Bank' : 'Manual'}
                    </div>
                    <div className={`col-span-1 md:col-span-2 text-[10px] font-black ${callbackConfig.color}`}>
                      {callbackConfig.label}
                    </div>
                    <div className="col-span-1 md:col-span-2">
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${statusConfig.color}`}>
                        {statusConfig.icon}
                        {formatCustomerPaymentStatusLabel(item.status)}
                      </span>
                    </div>
                    <div className="col-span-1 flex justify-end">
                      {isExpanded ? <ChevronUp size={15} className="text-slate-400" /> : <ChevronDown size={15} className="text-slate-400" />}
                    </div>
                  </div>
                  {isExpanded ? (
                    <div className="bg-slate-50 border-t border-slate-100 px-5 py-4">
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-xs mb-3">
                        {[
                          { label: 'Mã tham chiếu provider', value: item.providerInvoiceNumber || '--' },
                          { label: 'Đơn hàng', value: item.orderCode },
                          { label: 'Tenant', value: item.tenantName || '--' },
                          { label: 'Ngày GD', value: formatDateTime(item.paidAt || item.createdAt) },
                        ].map((field) => (
                          <div key={field.label} className="bg-white rounded-xl p-3 border border-slate-100">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{field.label}</p>
                            <p className="font-black text-slate-900 mt-1">{field.value}</p>
                          </div>
                        ))}
                      </div>
                      <div className="flex gap-2 flex-wrap">
                        <Link
                          to={`/admin/bookings?q=${encodeURIComponent(item.orderCode)}`}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-blue-50 hover:text-blue-600 transition-all"
                        >
                          <Eye size={12} />
                          Chi tiết đơn
                        </Link>
                        <button
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation();
                            loadPaymentsRef.current(true);
                          }}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-blue-50 hover:text-blue-600 transition-all"
                        >
                          <RefreshCw size={12} className={refreshing ? 'animate-spin' : ''} />
                          Refresh payment
                        </button>
                      </div>
                    </div>
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
