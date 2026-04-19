import React, { useEffect, useMemo, useState } from 'react';
import {
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

function getBatchStatusConfig(value) {
  switch (Number(value || 0)) {
    case 3:
      return { label: 'Hoàn thành', color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> };
    case 2:
      return { label: 'Đang xử lý', color: 'bg-blue-100 text-blue-700', icon: <Clock size={12} /> };
    case 4:
      return { label: 'Đã hủy', color: 'bg-rose-100 text-rose-700', icon: <Clock size={12} /> };
    default:
      return { label: 'Chờ duyệt', color: 'bg-amber-100 text-amber-700', icon: <Clock size={12} /> };
  }
}

export default function AdminSettlementPage() {
  const [tab, setTab] = useState('batches');
  const [expanded, setExpanded] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
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
      setDashboard({
        summary: response?.summary || {},
        batches: Array.isArray(response?.batches) ? response.batches : [],
        payouts: Array.isArray(response?.payouts) ? response.payouts : [],
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dashboard settlement.');
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
    { label: 'Tổng quyết toán', value: formatCurrency(dashboard.summary?.totalBatchAmount || 0, 'VND'), className: 'bg-slate-900 text-white' },
    { label: 'Đang xử lý', value: formatCurrency(dashboard.summary?.processingAmount || 0, 'VND'), className: 'bg-amber-50' },
    { label: 'Đã thanh toán', value: formatCurrency(dashboard.summary?.paidAmount || 0, 'VND'), className: 'bg-emerald-50' },
    { label: 'Đối tác', value: dashboard.summary?.tenantCount || 0, className: 'bg-blue-50' },
  ]), [dashboard.summary]);

  async function handleGenerateBatch() {
    setSaving(true);
    setError('');
    setNotice('');

    const now = new Date();

    try {
      const response = await generateAdminCommerceSettlementBatch({
        year: now.getFullYear(),
        month: now.getMonth() + 1,
        notes: 'Batch đối soát tháng hiện tại',
      });
      setDashboard({
        summary: response?.summary || {},
        batches: Array.isArray(response?.batches) ? response.batches : [],
        payouts: Array.isArray(response?.payouts) ? response.payouts : [],
      });
      setNotice('Đã tạo hoặc cập nhật batch settlement tháng hiện tại.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tạo batch settlement.');
    } finally {
      setSaving(false);
    }
  }

  async function handleMarkPaid(batchId) {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const response = await markAdminCommerceSettlementBatchPaid(batchId, {
        notes: 'Admin xác nhận đã thanh toán tenant.',
      });
      setDashboard({
        summary: response?.summary || {},
        batches: Array.isArray(response?.batches) ? response.batches : [],
        payouts: Array.isArray(response?.payouts) ? response.payouts : [],
      });
      setNotice('Batch settlement đã được đánh dấu paid.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể đánh dấu batch đã thanh toán.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Settlement & Payouts</h1>
          <p className="text-slate-500 text-sm mt-1">Đối soát theo tháng và thanh toán cho tenant từ dòng tiền platform.</p>
        </div>
        <button
          type="button"
          onClick={handleGenerateBatch}
          disabled={saving}
          className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-60"
        >
          <Plus size={16} />
          Tạo batch mới
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
              <p className="font-bold">Đang tải settlement batches...</p>
            </div>
          ) : dashboard.batches.length === 0 ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-400 border border-slate-100">
              <p className="font-bold">Chưa có batch settlement nào.</p>
            </div>
          ) : (
            dashboard.batches.map((batch) => {
              const statusConfig = getBatchStatusConfig(batch.status);
              const isExpanded = expanded === batch.id;
              const lines = dashboard.payouts.filter((item) => item.batchId === batch.id);

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
                        {batch.startDate} – {batch.endDate} · {batch.tenantCount} đối tác · {batch.lineCount} lines
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="font-black text-slate-900">{formatCurrency(batch.totalNetPayoutAmount || 0, batch.currencyCode || 'VND')}</p>
                      <p className="text-[10px] text-slate-400 font-bold">
                        Ngày trả: {batch.paidAt ? batch.paidAt.slice(0, 10) : '—'}
                      </p>
                    </div>
                    {isExpanded ? <ChevronUp size={16} className="text-slate-400 shrink-0" /> : <ChevronDown size={16} className="text-slate-400 shrink-0" />}
                  </div>
                  {isExpanded ? (
                    <div className="border-t border-slate-100 p-5 bg-slate-50">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Settlement Lines</p>
                      {lines.map((line) => (
                        <div key={`${line.tenantId}-${line.batchId}`} className="flex items-center gap-4 bg-white rounded-xl p-3 mb-2 border border-slate-100">
                          <p className="font-black text-slate-900 text-sm flex-1">{line.tenantName}</p>
                          <p className="text-xs text-slate-500 font-bold">
                            {line.bankName ? `${line.bankName} – ${line.accountNumberMasked || '—'}` : 'Chưa có payout account'}
                          </p>
                          <p className="font-black text-slate-900">{formatCurrency(line.amount || 0, line.currencyCode || 'VND')}</p>
                          <span className={`px-2 py-0.5 rounded-lg text-[10px] font-black uppercase ${getBatchStatusConfig(line.status).color}`}>
                            {getBatchStatusConfig(line.status).label}
                          </span>
                        </div>
                      ))}
                      {lines.length === 0 ? (
                        <p className="text-xs text-slate-400 font-bold">Chưa có payout lines.</p>
                      ) : null}
                      {Number(batch.status) !== 3 ? (
                        <button
                          type="button"
                          disabled={saving}
                          onClick={() => handleMarkPaid(batch.id)}
                          className="mt-3 px-5 py-2.5 bg-emerald-600 text-white rounded-xl font-black text-xs uppercase tracking-widest hover:bg-emerald-700 transition-all disabled:opacity-60"
                        >
                          ✓ Duyệt & Khởi tạo Payout
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
            <div className="col-span-3">Đối tác</div>
            <div className="col-span-2">Tài khoản</div>
            <div className="col-span-2">Số tiền</div>
            <div className="col-span-2">Trạng thái</div>
            <div className="col-span-1">Ngày</div>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
                Đang tải payouts...
              </div>
            ) : dashboard.payouts.length === 0 ? (
              <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
                Chưa có payout nào.
              </div>
            ) : (
              dashboard.payouts.map((item) => {
                const statusConfig = getBatchStatusConfig(item.status);

                return (
                  <div key={`${item.batchId}-${item.tenantId}`} className="grid grid-cols-2 md:grid-cols-12 gap-3 px-5 py-4 items-center hover:bg-slate-50 transition-all">
                    <div className="col-span-1 md:col-span-2 font-black text-slate-900 text-xs">{dashboard.batches.find((batch) => batch.id === item.batchId)?.batchCode || '—'}</div>
                    <div className="col-span-1 md:col-span-3 font-bold text-slate-700">{item.tenantName}</div>
                    <div className="col-span-1 md:col-span-2 text-xs text-slate-500 font-bold">{item.bankName ? `${item.bankName} – ${item.accountNumberMasked || '—'}` : 'Chưa cấu hình'}</div>
                    <div className="col-span-1 md:col-span-2 font-black text-slate-900">{formatCurrency(item.amount || 0, item.currencyCode || 'VND')}</div>
                    <div className="col-span-1 md:col-span-2">
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${statusConfig.color}`}>
                        {statusConfig.icon}
                        {statusConfig.label}
                      </span>
                    </div>
                    <div className="col-span-1 text-[10px] text-slate-400 font-bold">{item.paidAt ? item.paidAt.slice(0, 10) : '—'}</div>
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
