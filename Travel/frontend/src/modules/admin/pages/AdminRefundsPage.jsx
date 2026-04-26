import React, { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  AlertCircle,
  CheckCircle2,
  Clock,
  RefreshCw,
  RotateCcw,
  Search,
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
  { value: 'all', label: 'Tat ca' },
  { value: String(CUSTOMER_REFUND_STATUS.REQUESTED), label: 'Cho duyet' },
  { value: String(CUSTOMER_REFUND_STATUS.APPROVED), label: 'Da duyet' },
  { value: String(CUSTOMER_REFUND_STATUS.REFUNDED_PARTIAL), label: 'Da hoan' },
  { value: String(CUSTOMER_REFUND_STATUS.REJECTED), label: 'Tu choi' },
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

function canComplete(value) {
  return Number(value || 0) === CUSTOMER_REFUND_STATUS.APPROVED;
}

function ensureAmountDraft(value, fallback) {
  if (value === undefined || value === null || value === '') {
    return fallback === undefined || fallback === null ? '' : String(fallback);
  }

  return String(value);
}

function parseAmountDraft(value, fallback) {
  const raw = String(value ?? '').trim();
  if (!raw) {
    return fallback;
  }

  const normalized = Number(raw.replace(/,/g, ''));
  return Number.isFinite(normalized) ? normalized : fallback;
}

function getRemainingRefundableAmount(refund) {
  const value = Number(refund?.remainingRefundableAmount ?? refund?.requestedAmount ?? 0);
  return Number.isFinite(value) ? value : 0;
}

export default function AdminRefundsPage() {
  const [searchParams] = useSearchParams();
  const [filter, setFilter] = useState('all');
  const [search, setSearch] = useState(() => searchParams.get('q') || '');
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
  const [approvedDrafts, setApprovedDrafts] = useState({});
  const [refundedDrafts, setRefundedDrafts] = useState({});
  const [referenceDrafts, setReferenceDrafts] = useState({});

  useEffect(() => {
    setSearch(searchParams.get('q') || '');
  }, [searchParams]);

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
      setSelectedId((current) => (current && items.some((item) => item.id === current) ? current : items[0]?.id || ''));
      setNoteDrafts((current) => {
        const next = { ...current };
        items.forEach((item) => {
          if (next[item.id] === undefined) {
            next[item.id] = item.internalNote || '';
          }
        });
        return next;
      });
      setApprovedDrafts((current) => {
        const next = { ...current };
        items.forEach((item) => {
          if (next[item.id] === undefined) {
            next[item.id] = ensureAmountDraft(item.approvedAmount, item.requestedAmount);
          }
        });
        return next;
      });
      setRefundedDrafts((current) => {
        const next = { ...current };
        items.forEach((item) => {
          if (next[item.id] === undefined) {
            next[item.id] = ensureAmountDraft(item.refundedAmount, item.approvedAmount ?? item.requestedAmount);
          }
        });
        return next;
      });
      setReferenceDrafts((current) => {
        const next = { ...current };
        items.forEach((item) => {
          if (next[item.id] === undefined) {
            next[item.id] = item.refundReference || '';
          }
        });
        return next;
      });
    } catch (requestError) {
      setError(requestError.message || 'Khong the tai danh sach hoan tien.');
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

  function getApprovedAmount(refund) {
    return parseAmountDraft(approvedDrafts[refund.id], refund.requestedAmount);
  }

  function getRefundedAmount(refund) {
    return parseAmountDraft(refundedDrafts[refund.id], refund.approvedAmount || refund.requestedAmount);
  }

  function getInternalNote(refund) {
    return noteDrafts[refund.id]?.trim() || undefined;
  }

  function getRefundReference(refund) {
    return referenceDrafts[refund.id]?.trim() || undefined;
  }

  async function handleRefundAction(refund, action) {
    if (!refund) {
      return;
    }

    const approvedAmount = getApprovedAmount(refund);
    const refundedAmount = getRefundedAmount(refund);
    const internalNote = getInternalNote(refund);
    const refundReference = getRefundReference(refund);
    const remainingRefundable = getRemainingRefundableAmount(refund);

    if (action === 'approve' && (!Number.isFinite(approvedAmount) || approvedAmount <= 0)) {
      setError('So tien duyet phai lon hon 0.');
      return;
    }

    if (action === 'approve' && approvedAmount > remainingRefundable) {
      setError(`So tien duyet khong duoc vuot qua ${formatCurrency(remainingRefundable, refund.currencyCode)}.`);
      return;
    }

    if (!internalNote) {
      setError(action === 'reject' ? 'Vui long nhap ly do tu choi hoan tien.' : 'Vui long nhap ghi chu noi bo de luu audit.');
      return;
    }

    if (action === 'complete') {
      if (!Number.isFinite(refundedAmount) || refundedAmount <= 0) {
        setError('So tien da hoan phai lon hon 0.');
        return;
      }

      if (refundedAmount > remainingRefundable) {
        setError(`So tien da hoan khong duoc vuot qua ${formatCurrency(remainingRefundable, refund.currencyCode)}.`);
        return;
      }

      if (!refundReference) {
        setError('Vui long nhap ma tham chieu hoan tien.');
        return;
      }
    }

    const confirmMessage = action === 'approve'
      ? `Duyet refund ${refund.refundCode} voi so tien ${formatCurrency(approvedAmount, refund.currencyCode)}?`
      : action === 'reject'
        ? `Tu choi refund ${refund.refundCode}?`
        : `Xac nhan da hoan tien thu cong ${formatCurrency(refundedAmount, refund.currencyCode)} cho refund ${refund.refundCode}?`;

    if (!window.confirm(confirmMessage)) {
      return;
    }

    setSavingId(refund.id);
    setError('');
    setNotice('');

    try {
      if (action === 'approve') {
        await approveAdminCommerceRefund(refund.id, {
          approvedAmount,
          internalNote,
        }, refund.tenantId);
        setNotice('Yeu cau hoan tien da duoc duyet.');
      } else if (action === 'reject') {
        await rejectAdminCommerceRefund(refund.id, {
          internalNote,
        }, refund.tenantId);
        setNotice('Yeu cau hoan tien da bi tu choi.');
      } else {
        await completeAdminCommerceRefund(refund.id, {
          refundedAmount,
          refundReference,
          internalNote,
        }, refund.tenantId);
        setNotice('Refund da duoc danh dau hoan tat.');
      }

      await loadRefunds();
    } catch (requestError) {
      setError(requestError.message || 'Khong the cap nhat yeu cau hoan tien.');
    } finally {
      setSavingId('');
    }
  }

  const stats = [
    { label: 'Cho duyet', value: summary.pendingCount || 0, className: 'bg-amber-50', icon: <Clock size={18} className="text-amber-600" /> },
    { label: 'Da hoan', value: summary.completedCount || 0, className: 'bg-emerald-50', icon: <CheckCircle2 size={18} className="text-emerald-600" /> },
    { label: 'Tu choi', value: summary.rejectedCount || 0, className: 'bg-rose-50', icon: <XCircle size={18} className="text-rose-600" /> },
    { label: 'Tong yeu cau', value: summary.totalCount || 0, className: 'bg-slate-900', icon: <RotateCcw size={18} className="text-white/60" /> },
  ];

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Quan ly Hoan tien</h1>
          <p className="text-slate-500 text-sm mt-1">Duyet, tu choi va xac nhan hoan tien thu cong theo dung nghiep vu marketplace.</p>
        </div>
        <button
          type="button"
          onClick={loadRefunds}
          className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm flex items-center gap-2 hover:bg-blue-600 transition-all shadow-lg"
        >
          <RefreshCw size={16} />
          Tai lai
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
            placeholder="Ma hoan, ma don, ten khach..."
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
              <p className="font-bold">Dang tai yeu cau hoan tien...</p>
            </div>
          ) : refunds.length === 0 ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
              <RotateCcw size={36} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold">Khong co yeu cau nao</p>
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
                      <p className="text-sm font-bold text-slate-700">{item.customerName} - {item.serviceTitle || item.orderCode}</p>
                      <p className="text-xs text-slate-400 font-bold mt-0.5">{item.reasonText || item.reasonCode} - {formatDateTime(item.requestedAt)}</p>
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
                        Duyet
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
                        Tu choi
                      </button>
                    </div>
                  ) : canComplete(item.status) ? (
                    <button
                      type="button"
                      disabled={isSaving}
                      onClick={(event) => {
                        event.stopPropagation();
                        handleRefundAction(item, 'complete');
                      }}
                      className="mt-3 px-4 py-2 bg-blue-50 text-blue-700 rounded-xl text-[10px] font-black uppercase hover:bg-blue-100 transition-all disabled:opacity-60"
                    >
                      Xac nhan da hoan tien
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
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4">Chi tiet yeu cau</p>
              {[
                { label: 'Ma hoan', value: selected.refundCode },
                { label: 'Don dat', value: selected.orderCode },
                { label: 'Khach hang', value: selected.customerName },
                { label: 'Dich vu', value: selected.serviceTitle || selected.orderCode },
                { label: 'Ly do', value: selected.reasonText || selected.reasonCode },
                { label: 'Số tiền khách yêu cầu hoàn', value: formatCurrency(selected.requestedAmount, selected.currencyCode) },
                { label: 'Số tiền còn được phép hoàn', value: formatCurrency(getRemainingRefundableAmount(selected), selected.currencyCode) },
                { label: 'Đã hoàn trước yêu cầu này', value: formatCurrency(selected.alreadyRefundedAmount || 0, selected.currencyCode) },
                { label: 'Số tiền admin đã duyệt', value: selected.approvedAmount ? formatCurrency(selected.approvedAmount, selected.currencyCode) : '-' },
                { label: 'Số tiền đã chuyển cho khách', value: selected.refundedAmount ? formatCurrency(selected.refundedAmount, selected.currencyCode) : '-' },
                { label: 'Mã giao dịch hoàn tiền', value: selected.refundReference || '-' },
                { label: 'Ảnh hưởng đối soát', value: selected.settlementImpactNote || '-' },
                { label: 'Ngày khách gửi yêu cầu', value: formatDateTime(selected.requestedAt) },
                { label: 'Ngày admin xử lý', value: selected.completedAt ? formatDateTime(selected.completedAt) : selected.reviewedAt ? formatDateTime(selected.reviewedAt) : '-' },
              ].map((field) => (
                <div key={field.label} className="flex justify-between py-2.5 border-b border-slate-50 last:border-0 gap-4">
                  <span className="text-xs text-slate-400 font-bold">{field.label}</span>
                  <span className="text-xs font-black text-slate-900 text-right max-w-[55%]">{field.value}</span>
                </div>
              ))}

              <div className="mt-4 grid gap-3">
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Số tiền admin chấp thuận hoàn (VND)</label>
                  <input
                    inputMode="decimal"
                    value={approvedDrafts[selected.id] || ''}
                    onChange={(event) => setApprovedDrafts((current) => ({ ...current, [selected.id]: event.target.value }))}
                    placeholder={`Toi da ${formatCurrency(getRemainingRefundableAmount(selected), selected.currencyCode)}`}
                    className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
                  />
                  <p className="text-[11px] font-bold text-slate-400 mt-1">Không được vượt quá số tiền còn được phép hoàn của đơn.</p>
                </div>
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Số tiền đã chuyển lại cho khách (VND)</label>
                  <input
                    inputMode="decimal"
                    value={refundedDrafts[selected.id] || ''}
                    onChange={(event) => setRefundedDrafts((current) => ({ ...current, [selected.id]: event.target.value }))}
                    placeholder="Nhap so tien hoan thuc te"
                    className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
                  />
                  <p className="text-[11px] font-bold text-slate-400 mt-1">Nhập số tiền thực tế đã chuyển khi xác nhận hoàn thủ công.</p>
                </div>
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Mã giao dịch hoàn tiền / tham chiếu ngân hàng</label>
                  <input
                    value={referenceDrafts[selected.id] || ''}
                    onChange={(event) => setReferenceDrafts((current) => ({ ...current, [selected.id]: event.target.value }))}
                    placeholder="VD: MB-REF-20260420-001"
                    className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
                  />
                </div>
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ghi chú bắt buộc cho audit nội bộ</label>
                  <textarea
                    rows={3}
                    value={noteDrafts[selected.id] || ''}
                    onChange={(event) => setNoteDrafts((current) => ({ ...current, [selected.id]: event.target.value }))}
                    placeholder="Bat buoc nhap ly do/xac nhan xu ly..."
                    className="w-full bg-slate-50 rounded-xl p-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30 resize-none"
                  />
                </div>
              </div>

              <div className="mt-4 flex flex-wrap gap-2">
                {canApprove(selected.status) ? (
                  <>
                    <button
                      type="button"
                      disabled={savingId === selected.id}
                      onClick={() => handleRefundAction(selected, 'approve')}
                      className="px-4 py-2 bg-emerald-50 text-emerald-700 rounded-xl text-[10px] font-black uppercase hover:bg-emerald-100 transition-all disabled:opacity-60"
                    >
                      Duyet voi so tien nhap tay
                    </button>
                    <button
                      type="button"
                      disabled={savingId === selected.id}
                      onClick={() => handleRefundAction(selected, 'reject')}
                      className="px-4 py-2 bg-rose-50 text-rose-600 rounded-xl text-[10px] font-black uppercase hover:bg-rose-100 transition-all disabled:opacity-60"
                    >
                      Tu choi
                    </button>
                  </>
                ) : canComplete(selected.status) ? (
                  <button
                    type="button"
                    disabled={savingId === selected.id}
                    onClick={() => handleRefundAction(selected, 'complete')}
                    className="px-4 py-2 bg-blue-50 text-blue-700 rounded-xl text-[10px] font-black uppercase hover:bg-blue-100 transition-all disabled:opacity-60"
                  >
                    Xac nhan hoan thu cong
                  </button>
                ) : null}
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
              <AlertCircle size={32} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold text-sm">Chon yeu cau de xem chi tiet</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
