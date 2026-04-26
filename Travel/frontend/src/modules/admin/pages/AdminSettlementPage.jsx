import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Clock,
  Plus,
} from 'lucide-react';
import {
  generateAdminCommerceSettlementBatch,
  getAdminCommerceSettlements,
  markAdminCommerceSettlementBatchPaid,
} from '../../../services/commerceBackofficeService';
import { formatCurrency } from '../../tenant/train/utils/presentation';

const PERIOD_OPTIONS = [
  { value: 'day', label: 'Ngay' },
  { value: 'month', label: 'Thang' },
  { value: 'quarter', label: 'Quy' },
  { value: 'year', label: 'Nam' },
];

function getQuarterFromMonth(month) {
  return Math.floor((month - 1) / 3) + 1;
}

function buildTodayDateValue() {
  return new Date().toISOString().slice(0, 10);
}

function toDateTimeInputValue(value) {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60 * 1000);
  return local.toISOString().slice(0, 16);
}

function toApiDateTimeValue(value) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}

function formatDateRange(startDate, endDate) {
  if (!startDate || !endDate) {
    return '--';
  }

  return `${startDate} - ${endDate}`;
}

function getBatchStatusConfig(value) {
  switch (Number(value || 0)) {
    case 3:
      return { label: 'Hoan thanh', color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> };
    case 2:
      return { label: 'Dang xu ly', color: 'bg-blue-100 text-blue-700', icon: <Clock size={12} /> };
    case 4:
      return { label: 'Da huy', color: 'bg-rose-100 text-rose-700', icon: <AlertCircle size={12} /> };
    default:
      return { label: 'Nhap', color: 'bg-amber-100 text-amber-700', icon: <Clock size={12} /> };
  }
}

function getPayoutStatusConfig(value) {
  switch (Number(value || 0)) {
    case 3:
      return { label: 'Settled', color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> };
    case 4:
      return { label: 'Adjusted', color: 'bg-amber-100 text-amber-700', icon: <AlertCircle size={12} /> };
    case 5:
      return { label: 'On hold', color: 'bg-rose-100 text-rose-700', icon: <AlertCircle size={12} /> };
    case 2:
      return { label: 'In settlement', color: 'bg-blue-100 text-blue-700', icon: <Clock size={12} /> };
    default:
      return { label: 'Unsettled', color: 'bg-slate-100 text-slate-600', icon: <Clock size={12} /> };
  }
}

function hasMissingPayoutAccount(line) {
  return !line?.bankName || !line?.accountNumberMasked || !line?.accountHolder;
}

export default function AdminSettlementPage() {
  const now = new Date();
  const [tab, setTab] = useState('batches');
  const [expanded, setExpanded] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [periodType, setPeriodType] = useState('month');
  const [periodYear, setPeriodYear] = useState(String(now.getFullYear()));
  const [periodMonth, setPeriodMonth] = useState(String(now.getMonth() + 1));
  const [periodQuarter, setPeriodQuarter] = useState(String(getQuarterFromMonth(now.getMonth() + 1)));
  const [periodDate, setPeriodDate] = useState(buildTodayDateValue());
  const [batchNote, setBatchNote] = useState('');
  const [paidAtDrafts, setPaidAtDrafts] = useState({});
  const [payoutReferenceDrafts, setPayoutReferenceDrafts] = useState({});
  const [payoutNoteDrafts, setPayoutNoteDrafts] = useState({});
  const [dashboard, setDashboard] = useState({
    summary: {},
    batches: [],
    payouts: [],
  });

  async function loadDashboard() {
    setLoading(true);
    setError('');

    try {
      const response = await getAdminCommerceSettlements();
      const batches = Array.isArray(response?.batches) ? response.batches : [];
      setDashboard({
        summary: response?.summary || {},
        batches,
        payouts: Array.isArray(response?.payouts) ? response.payouts : [],
      });
      setPaidAtDrafts((current) => {
        const next = { ...current };
        batches.forEach((batch) => {
          if (next[batch.id] === undefined) {
            next[batch.id] = toDateTimeInputValue(batch.paidAt) || toDateTimeInputValue(new Date().toISOString());
          }
        });
        return next;
      });
      setPayoutNoteDrafts((current) => {
        const next = { ...current };
        batches.forEach((batch) => {
          if (next[batch.id] === undefined) {
            next[batch.id] = batch.notes || '';
          }
        });
        return next;
      });
      setPayoutReferenceDrafts((current) => {
        const next = { ...current };
        batches.forEach((batch) => {
          if (next[batch.id] === undefined) {
            next[batch.id] = '';
          }
        });
        return next;
      });
    } catch (requestError) {
      setError(requestError.message || 'Khong the tai dashboard settlement.');
      setDashboard({
        summary: {},
        batches: [],
        payouts: [],
      });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  const stats = useMemo(() => ([
    { label: 'Tong payout', value: formatCurrency(dashboard.summary?.totalBatchAmount || 0, 'VND'), className: 'bg-slate-900 text-white' },
    { label: 'Dang xu ly', value: formatCurrency(dashboard.summary?.processingAmount || 0, 'VND'), className: 'bg-amber-50' },
    { label: 'Da thanh toan', value: formatCurrency(dashboard.summary?.paidAmount || 0, 'VND'), className: 'bg-emerald-50' },
    { label: 'So tenant', value: dashboard.summary?.tenantCount || 0, className: 'bg-blue-50' },
  ]), [dashboard.summary]);

  function buildGeneratePayload() {
    const payload = {
      periodType,
      year: Number(periodYear),
      notes: batchNote.trim() || undefined,
    };

    if (periodType === 'month') {
      payload.month = Number(periodMonth);
    } else if (periodType === 'quarter') {
      payload.quarter = Number(periodQuarter);
    } else if (periodType === 'day') {
      const [year, month, day] = String(periodDate || '').split('-').map(Number);
      payload.year = year;
      payload.month = month;
      payload.day = day;
    }

    return payload;
  }

  async function handleGenerateBatch() {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const response = await generateAdminCommerceSettlementBatch(buildGeneratePayload());
      setDashboard({
        summary: response?.summary || {},
        batches: Array.isArray(response?.batches) ? response.batches : [],
        payouts: Array.isArray(response?.payouts) ? response.payouts : [],
      });
      setNotice('Da tao hoac cap nhat batch settlement theo ky da chon.');
      setBatchNote('');
    } catch (requestError) {
      setError(requestError.message || 'Khong the tao batch settlement.');
    } finally {
      setSaving(false);
    }
  }

  async function handleMarkPaid(batch) {
    if (!batch) {
      return;
    }

    const paidAt = toApiDateTimeValue(paidAtDrafts[batch.id]);
    const bankTransactionCode = payoutReferenceDrafts[batch.id]?.trim();
    const notes = payoutNoteDrafts[batch.id]?.trim();
    const lines = dashboard.payouts.filter((item) => item.batchId === batch.id);

    if (lines.some(hasMissingPayoutAccount)) {
      setError('Batch nay con tenant chua cau hinh du payout account mac dinh.');
      return;
    }

    if (!paidAt) {
      setError('Vui long nhap ngay chuyen tien.');
      return;
    }

    if (!bankTransactionCode) {
      setError('Vui long nhap ma giao dich chuyen tien.');
      return;
    }

    if (!notes) {
      setError('Vui long nhap ghi chu xac nhan payout.');
      return;
    }

    if (!window.confirm(`Danh dau batch ${batch.batchCode} da tra tien tenant?`)) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      const response = await markAdminCommerceSettlementBatchPaid(batch.id, {
        paidAt,
        bankTransactionCode,
        notes,
      });
      setDashboard({
        summary: response?.summary || {},
        batches: Array.isArray(response?.batches) ? response.batches : [],
        payouts: Array.isArray(response?.payouts) ? response.payouts : [],
      });
      setNotice('Batch settlement da duoc danh dau da thanh toan.');
    } catch (requestError) {
      setError(requestError.message || 'Khong the danh dau batch da thanh toan.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Settlement & Payouts</h1>
          <p className="text-slate-500 text-sm mt-1">Tao batch doi soat va danh dau thanh toan cho tenant theo ky payout.</p>
        </div>
        <button
          type="button"
          onClick={handleGenerateBatch}
          disabled={saving}
          className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-60"
        >
          <Plus size={16} />
          Tao batch
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

      <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 mb-6">
        <div className="flex flex-wrap gap-2 mb-4">
          {PERIOD_OPTIONS.map((item) => (
            <button
              key={item.value}
              type="button"
              onClick={() => setPeriodType(item.value)}
              className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${periodType === item.value ? 'bg-slate-900 text-white shadow-md' : 'bg-slate-50 text-slate-500 hover:text-slate-900'}`}
            >
              {item.label}
            </button>
          ))}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          {periodType === 'day' ? (
            <div className="md:col-span-2">
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ngay doi soat</label>
              <input
                type="date"
                value={periodDate}
                onChange={(event) => setPeriodDate(event.target.value)}
                className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
              />
            </div>
          ) : null}

          {periodType !== 'day' ? (
            <div>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Nam</label>
              <input
                inputMode="numeric"
                value={periodYear}
                onChange={(event) => setPeriodYear(event.target.value)}
                placeholder="2026"
                className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
              />
            </div>
          ) : null}

          {periodType === 'month' ? (
            <div>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Thang</label>
              <select
                value={periodMonth}
                onChange={(event) => setPeriodMonth(event.target.value)}
                className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
              >
                {Array.from({ length: 12 }, (_, index) => String(index + 1)).map((item) => (
                  <option key={item} value={item}>{`Thang ${item}`}</option>
                ))}
              </select>
            </div>
          ) : null}

          {periodType === 'quarter' ? (
            <div>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Quy</label>
              <select
                value={periodQuarter}
                onChange={(event) => setPeriodQuarter(event.target.value)}
                className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
              >
                {[1, 2, 3, 4].map((item) => (
                  <option key={item} value={String(item)}>{`Quy ${item}`}</option>
                ))}
              </select>
            </div>
          ) : null}

          <div className={periodType === 'day' ? 'md:col-span-2' : 'md:col-span-2'}>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ghi chu batch</label>
            <input
              value={batchNote}
              onChange={(event) => setBatchNote(event.target.value)}
              placeholder="Ghi chu doi soat/payout"
              className="w-full bg-slate-50 rounded-xl px-3 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30"
            />
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {stats.map((item) => (
          <div key={item.label} className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.className}`}>
            <p className={`text-2xl font-black ${item.className.includes('slate-900') ? 'text-white' : 'text-slate-900'}`}>
              {loading ? '--' : item.value}
            </p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${item.className.includes('slate-900') ? 'text-white/60' : 'text-slate-400'}`}>
              {item.label}
            </p>
          </div>
        ))}
      </div>

      <div className="flex gap-1 bg-white rounded-2xl p-1 border border-slate-100 shadow-sm mb-6 w-fit">
        {[
          { value: 'batches', label: 'Settlement Batches' },
          { value: 'payouts', label: 'Payouts' },
        ].map((item) => (
          <button
            key={item.value}
            type="button"
            onClick={() => setTab(item.value)}
            className={`px-5 py-3 rounded-xl text-xs font-black uppercase tracking-widest transition-all ${tab === item.value ? 'bg-slate-900 text-white shadow-md' : 'text-slate-400 hover:text-slate-700'}`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === 'batches' ? (
        <div className="space-y-3">
          {loading ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-400 border border-slate-100">
              <p className="font-bold">Dang tai settlement batches...</p>
            </div>
          ) : dashboard.batches.length === 0 ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-400 border border-slate-100">
              <p className="font-bold">Chua co batch settlement nao.</p>
            </div>
          ) : (
            dashboard.batches.map((batch) => {
              const statusConfig = getBatchStatusConfig(batch.status);
              const isExpanded = expanded === batch.id;
              const lines = dashboard.payouts.filter((item) => item.batchId === batch.id);
              const hasMissingPayout = lines.some(hasMissingPayoutAccount);

              return (
                <div key={batch.id} className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
                  <div
                    onClick={() => setExpanded(isExpanded ? null : batch.id)}
                    className="flex items-center gap-4 p-5 cursor-pointer hover:bg-slate-50 transition-all"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{batch.batchCode}</p>
                        <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${statusConfig.color}`}>
                          {statusConfig.icon}
                          {statusConfig.label}
                        </span>
                      </div>
                      <p className="text-xs text-slate-400 font-bold mt-0.5">
                        {batch.periodLabel || formatDateRange(batch.startDate, batch.endDate)} - {batch.tenantCount} tenant - {batch.lineCount} lines
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="font-black text-slate-900">{formatCurrency(batch.totalNetPayoutAmount || 0, batch.currencyCode || 'VND')}</p>
                      <p className="text-[10px] text-slate-400 font-bold">
                        Da tra: {batch.paidAt ? batch.paidAt.slice(0, 10) : '-'}
                      </p>
                    </div>
                    {isExpanded ? <ChevronUp size={16} className="text-slate-400 shrink-0" /> : <ChevronDown size={16} className="text-slate-400 shrink-0" />}
                  </div>
                  {isExpanded ? (
                    <div className="border-t border-slate-100 p-5 bg-slate-50">
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-4">
                        <div>
                          <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ngày thực tế chuyển tiền</label>
                          <input
                            type="datetime-local"
                            value={paidAtDrafts[batch.id] || ''}
                            onChange={(event) => setPaidAtDrafts((current) => ({ ...current, [batch.id]: event.target.value }))}
                            className="w-full bg-white rounded-xl px-3 py-3 text-sm font-medium outline-none border border-slate-200"
                          />
                        </div>
                        <div>
                          <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Mã giao dịch ngân hàng</label>
                          <input
                            value={payoutReferenceDrafts[batch.id] || ''}
                            onChange={(event) => setPayoutReferenceDrafts((current) => ({ ...current, [batch.id]: event.target.value }))}
                            placeholder="VD: MB-PAYOUT-20260420-001"
                            className="w-full bg-white rounded-xl px-3 py-3 text-sm font-medium outline-none border border-slate-200"
                          />
                        </div>
                        <div>
                          <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ghi chú xác nhận payout</label>
                          <input
                            value={payoutNoteDrafts[batch.id] || ''}
                            onChange={(event) => setPayoutNoteDrafts((current) => ({ ...current, [batch.id]: event.target.value }))}
                            placeholder="VD: đã chuyển đủ theo batch, kèm sao kê nội bộ"
                            className="w-full bg-white rounded-xl px-3 py-3 text-sm font-medium outline-none border border-slate-200"
                          />
                        </div>
                      </div>
                      <p className="text-[11px] font-bold text-slate-400 mb-4">
                        Ba trường trên là căn cứ audit khi admin đánh dấu batch đã trả tiền cho tenant.
                      </p>

                      {hasMissingPayout ? (
                        <div className="mb-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-bold text-amber-700">
                          Van con tenant chua cau hinh du payout account mac dinh. Khong the danh dau da thanh toan batch nay.
                        </div>
                      ) : null}

                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Payout Lines</p>
                      {lines.map((line) => {
                        const payoutStatus = getPayoutStatusConfig(line.status);

                        return (
                          <div key={`${line.tenantId}-${line.batchId}`} className="flex items-center gap-4 bg-white rounded-xl p-3 mb-2 border border-slate-100">
                            <div className="flex-1">
                              <p className="font-black text-slate-900 text-sm">{line.tenantName}</p>
                              <p className="text-xs text-slate-500 font-bold mt-1">
                                {line.bankName ? `${line.bankName} - ${line.accountNumberMasked || '-'}` : 'Chua co payout account'}
                              </p>
                            </div>
                            <p className="font-black text-slate-900">{formatCurrency(line.amount || 0, line.currencyCode || 'VND')}</p>
                            <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${payoutStatus.color}`}>
                              {payoutStatus.icon}
                              {payoutStatus.label}
                            </span>
                          </div>
                        );
                      })}
                      {lines.length === 0 ? (
                        <p className="text-xs text-slate-400 font-bold">Chua co payout lines.</p>
                      ) : null}
                      {Number(batch.status) !== 3 ? (
                        <button
                          type="button"
                          disabled={saving || lines.length === 0 || hasMissingPayout}
                          onClick={() => handleMarkPaid(batch)}
                          className="mt-3 px-5 py-2.5 bg-emerald-600 text-white rounded-xl font-black text-xs uppercase tracking-widest hover:bg-emerald-700 transition-all disabled:opacity-60"
                        >
                          Danh dau da tra tenant
                        </button>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              );
            })
          )}
        </div>
      ) : (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <div className="hidden md:grid grid-cols-12 gap-3 px-5 py-3 bg-slate-50 border-b border-slate-100 text-[10px] font-black text-slate-400 uppercase tracking-widest">
            <div className="col-span-2">Batch</div>
            <div className="col-span-3">Tenant</div>
            <div className="col-span-2">Tai khoan</div>
            <div className="col-span-2">So tien</div>
            <div className="col-span-2">Trang thai</div>
            <div className="col-span-1">Ngay</div>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
                Dang tai payouts...
              </div>
            ) : dashboard.payouts.length === 0 ? (
              <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
                Chua co payout nao.
              </div>
            ) : (
              dashboard.payouts.map((item) => {
                const payoutStatus = getPayoutStatusConfig(item.status);
                const batch = dashboard.batches.find((entry) => entry.id === item.batchId);

                return (
                  <div key={`${item.batchId}-${item.tenantId}`} className="grid grid-cols-2 md:grid-cols-12 gap-3 px-5 py-4 items-center hover:bg-slate-50 transition-all">
                    <div className="col-span-1 md:col-span-2 font-black text-slate-900 text-xs">{batch?.batchCode || '-'}</div>
                    <div className="col-span-1 md:col-span-3 font-bold text-slate-700">{item.tenantName}</div>
                    <div className="col-span-1 md:col-span-2 text-xs text-slate-500 font-bold">{item.bankName ? `${item.bankName} - ${item.accountNumberMasked || '-'}` : 'Chua cau hinh'}</div>
                    <div className="col-span-1 md:col-span-2 font-black text-slate-900">{formatCurrency(item.amount || 0, item.currencyCode || 'VND')}</div>
                    <div className="col-span-1 md:col-span-2">
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${payoutStatus.color}`}>
                        {payoutStatus.icon}
                        {payoutStatus.label}
                      </span>
                    </div>
                    <div className="col-span-1 text-[10px] text-slate-400 font-bold">{item.paidAt ? item.paidAt.slice(0, 10) : '-'}</div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
}
