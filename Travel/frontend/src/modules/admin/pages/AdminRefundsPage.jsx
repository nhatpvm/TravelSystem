import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  AlertCircle,
  CheckCircle2,
  Clock,
  RotateCcw,
  Search,
  RefreshCw,
  XCircle,
} from 'lucide-react';
import {
  approveAdminCommerceRefund,
  completeAdminCommerceRefund,
  listAdminCommerceRefunds,
  rejectAdminCommerceRefund,
} from '../../../services/commerceBackofficeService';
import {
  CUSTOMER_REFUND_STATUS,
  formatCustomerRefundStatusLabel,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

const FILTERS = [
  { value: 'all', label: 'Tất cả' },
  { value: String(CUSTOMER_REFUND_STATUS.REQUESTED), label: 'Chờ duyệt' },
  { value: String(CUSTOMER_REFUND_STATUS.APPROVED), label: 'Đã duyệt' },
  { value: String(CUSTOMER_REFUND_STATUS.REFUNDED_PARTIAL), label: 'Đã hoàn' },
  { value: String(CUSTOMER_REFUND_STATUS.REJECTED), label: 'Từ chối' },
];

function getStatusConfig(value) {
  switch (Number(value || 0)) {
    case CUSTOMER_REFUND_STATUS.REQUESTED:
    case CUSTOMER_REFUND_STATUS.UNDER_REVIEW:
    case CUSTOMER_REFUND_STATUS.PROCESSING:
      return { color: 'bg-amber-100 text-amber-700', icon: <Clock size={12} /> };
    case CUSTOMER_REFUND_STATUS.APPROVED:
      return { color: 'bg-blue-100 text-blue-700', icon: <CheckCircle2 size={12} /> };
    case CUSTOMER_REFUND_STATUS.REFUNDED_PARTIAL:
    case CUSTOMER_REFUND_STATUS.REFUNDED_FULL:
      return { color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> };
    default:
      return { color: 'bg-rose-100 text-rose-700', icon: <XCircle size={12} /> };
  }
}

function canApprove(value) {
  return [
    CUSTOMER_REFUND_STATUS.REQUESTED,
    CUSTOMER_REFUND_STATUS.UNDER_REVIEW,
    CUSTOMER_REFUND_STATUS.PROCESSING,
  ].includes(Number(value || 0));
}

export default function AdminRefundsPage() {
  const [filter, setFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [savingId, setSavingId] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [summary, setSummary] = useState({
    pendingCount: 0,
    completedCount: 0,
    rejectedCount: 0,
    totalCount: 0,
  });
  const [refunds, setRefunds] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [noteDrafts, setNoteDrafts] = useState({});

  async function loadRefunds() {
    setLoading(true);
    setError('');

    try {
      const response = await listAdminCommerceRefunds({
        q: search.trim() || undefined,
        status: filter === 'all' ? undefined : filter,
      });

      const items = Array.isArray(response?.items) ? response.items : [];
      setSummary(response?.summary || {});
      setRefunds(items);
      setSelectedId((current) => current && items.some((item) => item.id === current) ? current : items[0]?.id || '');
      setNoteDrafts((current) => {
        const next = { ...current };
        items.forEach((item) => {
          if (next[item.id] === undefined) {
            next[item.id] = item.reviewNote || '';
          }
        });
        return next;
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách hoàn tiền.');
      setSummary({
        pendingCount: 0,
        completedCount: 0,
        rejectedCount: 0,
        totalCount: 0,
      });
      setRefunds([]);
      setSelectedId('');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadRefunds();
  }, [filter, search]);

  const selected = useMemo(
    () => refunds.find((item) => item.id === selectedId) || null,
    [refunds, selectedId],
  );

  async function handleRefundAction(refund, action) {
    if (!refund) {
      return;
    }

    setSavingId(refund.id);
    setError('');
    setNotice('');

    const reviewNote = noteDrafts[refund.id]?.trim() || undefined;

    try {
      if (action === 'approve') {
        await approveAdminCommerceRefund(refund.id, {
          approvedAmount: refund.requestedAmount,
          reviewNote,
        });
        setNotice('Yêu cầu hoàn tiền đã được duyệt.');
      } else if (action === 'reject') {
        await rejectAdminCommerceRefund(refund.id, {
          reviewNote: reviewNote || 'Từ chối theo quyết định admin/platform.',
        });
        setNotice('Yêu cầu hoàn tiền đã bị từ chối.');
      } else {
        await completeAdminCommerceRefund(refund.id, {
          refundedAmount: refund.approvedAmount || refund.requestedAmount,
          reviewNote: reviewNote || 'Admin đã xác nhận hoàn tiền thủ công.',
        });
        setNotice('Refund đã được đánh dấu hoàn tất.');
      }

      await loadRefunds();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật yêu cầu hoàn tiền.');
    } finally {
      setSavingId('');
    }
  }

  const stats = [
    { label: 'Chờ duyệt', value: summary.pendingCount || 0, className: 'bg-amber-50', icon: <Clock size={18} className="text-amber-600" /> },
    { label: 'Đã hoàn', value: summary.completedCount || 0, className: 'bg-emerald-50', icon: <CheckCircle2 size={18} className="text-emerald-600" /> },
    { label: 'Từ chối', value: summary.rejectedCount || 0, className: 'bg-rose-50', icon: <XCircle size={18} className="text-rose-600" /> },
    { label: 'Tổng yêu cầu', value: summary.totalCount || 0, className: 'bg-slate-900', icon: <RotateCcw size={18} className="text-white/60" /> },
  ];

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Quản lý Hoàn tiền</h1>
          <p className="text-slate-500 text-sm mt-1">Duyệt, từ chối và theo dõi tiến trình hoàn tiền thật của platform marketplace.</p>
        </div>
        <button
          type="button"
          onClick={loadRefunds}
          className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm flex items-center gap-2 hover:bg-blue-600 transition-all shadow-lg"
        >
          <RefreshCw size={16} />
          Tải lại
        </button>
      </div>

      {notice ? (
        <div className="mb-5 rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      ) : null}

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
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.className} flex items-center gap-4`}
          >
            <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${item.className === 'bg-slate-900' ? 'bg-white/10' : 'bg-white/60'}`}>
              {item.icon}
            </div>
            <div>
              <p className={`text-3xl font-black ${item.className === 'bg-slate-900' ? 'text-white' : 'text-slate-900'}`}>
                {loading ? '--' : item.value}
              </p>
              <p className={`text-[10px] font-bold uppercase tracking-widest ${item.className === 'bg-slate-900' ? 'text-white/60' : 'text-slate-400'}`}>
                {item.label}
              </p>
            </div>
          </motion.div>
        ))}
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Mã hoàn, tên khách…"
            className="bg-transparent py-3 flex-1 text-sm font-medium outline-none"
          />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {FILTERS.map((item) => (
            <button
              key={item.value}
              onClick={() => setFilter(item.value)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest whitespace-nowrap transition-all ${filter === item.value ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-3">
          {loading ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-400 border border-slate-100">
              <p className="font-bold">Đang tải yêu cầu hoàn tiền...</p>
            </div>
          ) : refunds.length === 0 ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
              <RotateCcw size={36} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold">Không có yêu cầu nào</p>
            </div>
          ) : (
            refunds.map((item, index) => {
              const statusConfig = getStatusConfig(item.status);
              const isSaving = savingId === item.id;

              return (
                <motion.div
                  key={item.id}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.05 }}
                  onClick={() => setSelectedId(item.id)}
                  className={`bg-white rounded-2xl p-5 shadow-sm border transition-all cursor-pointer hover:shadow-md ${selectedId === item.id ? 'border-[#1EB4D4]' : 'border-slate-100'}`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap mb-1">
                        <p className="font-black text-slate-900 text-sm">{item.refundCode}</p>
                        <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${statusConfig.color}`}>
                          {statusConfig.icon}
                          {formatCustomerRefundStatusLabel(item.status)}
                        </span>
                      </div>
                      <p className="text-sm font-bold text-slate-700">{item.customerName} · {item.serviceTitle || item.orderCode}</p>
                      <p className="text-xs text-slate-400 font-bold mt-0.5">{item.reasonText || item.reasonCode} · {formatDateTime(item.requestedAt)}</p>
                    </div>
                    <p className="font-black text-slate-900 shrink-0">{formatCurrency(item.requestedAmount, item.currencyCode)}</p>
                  </div>
                  {canApprove(item.status) ? (
                    <div className="flex gap-2 mt-3">
                      <button
                        type="button"
                        disabled={isSaving}
                        onClick={(event) => {
                          event.stopPropagation();
                          handleRefundAction(item, 'approve');
                        }}
                        className="px-4 py-2 bg-emerald-50 text-emerald-700 rounded-xl text-[10px] font-black uppercase hover:bg-emerald-100 transition-all disabled:opacity-60"
                      >
                        ✓ Duyệt
                      </button>
                      <button
                        type="button"
                        disabled={isSaving}
                        onClick={(event) => {
                          event.stopPropagation();
                          handleRefundAction(item, 'reject');
                        }}
                        className="px-4 py-2 bg-rose-50 text-rose-600 rounded-xl text-[10px] font-black uppercase hover:bg-rose-100 transition-all disabled:opacity-60"
                      >
                        ✕ Từ chối
                      </button>
                    </div>
                  ) : Number(item.status) === CUSTOMER_REFUND_STATUS.APPROVED ? (
                    <button
                      type="button"
                      disabled={isSaving}
                      onClick={(event) => {
                        event.stopPropagation();
                        handleRefundAction(item, 'complete');
                      }}
                      className="mt-3 px-4 py-2 bg-blue-50 text-blue-700 rounded-xl text-[10px] font-black uppercase hover:bg-blue-100 transition-all disabled:opacity-60"
                    >
                      ⟳ Xác nhận đã hoàn tiền
                    </button>
                  ) : null}
                </motion.div>
              );
            })
          )}
        </div>

        <div className="lg:col-span-1">
          {selected ? (
            <div className="sticky top-6 bg-white rounded-2xl p-6 shadow-xl shadow-slate-100/60 border border-slate-100">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4">Chi tiết yêu cầu</p>
              {[
                { label: 'Mã hoàn', value: selected.refundCode },
                { label: 'Đơn đặt', value: selected.orderCode },
                { label: 'Khách hàng', value: selected.customerName },
                { label: 'Dịch vụ', value: selected.serviceTitle || selected.orderCode },
                { label: 'Lý do', value: selected.reasonText || selected.reasonCode },
                { label: 'Số tiền', value: formatCurrency(selected.requestedAmount, selected.currencyCode) },
                { label: 'Đã duyệt', value: selected.approvedAmount ? formatCurrency(selected.approvedAmount, selected.currencyCode) : '—' },
                { label: 'Đã hoàn', value: selected.refundedAmount ? formatCurrency(selected.refundedAmount, selected.currencyCode) : '—' },
                { label: 'Ngày yêu cầu', value: formatDateTime(selected.requestedAt) },
                { label: 'Ngày xử lý', value: selected.completedAt ? formatDateTime(selected.completedAt) : selected.reviewedAt ? formatDateTime(selected.reviewedAt) : '—' },
              ].map((field) => (
                <div key={field.label} className="flex justify-between py-2.5 border-b border-slate-50 last:border-0 gap-4">
                  <span className="text-xs text-slate-400 font-bold">{field.label}</span>
                  <span className="text-xs font-black text-slate-900 text-right max-w-[55%]">{field.value}</span>
                </div>
              ))}
              <div className="mt-4">
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ghi chú xử lý</label>
                <textarea
                  rows={3}
                  value={noteDrafts[selected.id] || ''}
                  onChange={(event) => setNoteDrafts((current) => ({ ...current, [selected.id]: event.target.value }))}
                  placeholder="Nhập ghi chú nội bộ…"
                  className="w-full bg-slate-50 rounded-xl p-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30 resize-none"
                />
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
              <AlertCircle size={32} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold text-sm">Chọn yêu cầu để xem chi tiết</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
