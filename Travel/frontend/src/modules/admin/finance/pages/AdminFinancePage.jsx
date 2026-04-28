import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AlertCircle,
  ArrowUpRight,
  CreditCard,
  Plus,
  RefreshCw,
  RotateCcw,
} from 'lucide-react';
import {
  getAdminCommerceSettlements,
  listAdminCommercePayments,
  listAdminCommerceRefunds,
} from '../../../../services/commerceBackofficeService';
import { formatCurrency } from '../../../tenant/train/utils/presentation';

export default function AdminFinancePage() {
  const [activeTab, setActiveTab] = useState('settlements');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [finance, setFinance] = useState({
    payments: {},
    refunds: {},
    settlements: { summary: {}, batches: [], payouts: [] },
  });

  async function loadFinance() {
    setLoading(true);
    setError('');

    try {
      const [payments, refunds, settlements] = await Promise.all([
        listAdminCommercePayments({}),
        listAdminCommerceRefunds({}),
        getAdminCommerceSettlements(),
      ]);

      setFinance({
        payments: payments || {},
        refunds: refunds || {},
        settlements: settlements || { summary: {}, batches: [], payouts: [] },
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dữ liệu tài chính.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadFinance();
  }, []);

  const paymentSummary = useMemo(() => finance.payments.summary || {}, [finance.payments.summary]);
  const refundSummary = useMemo(() => finance.refunds.summary || {}, [finance.refunds.summary]);
  const settlementSummary = useMemo(() => finance.settlements.summary || {}, [finance.settlements.summary]);
  const batches = useMemo(
    () => (Array.isArray(finance.settlements.batches) ? finance.settlements.batches : []),
    [finance.settlements.batches],
  );
  const payouts = useMemo(
    () => (Array.isArray(finance.settlements.payouts) ? finance.settlements.payouts : []),
    [finance.settlements.payouts],
  );

  const stats = useMemo(() => ([
    {
      label: 'Tổng GMV đã ghi nhận',
      value: formatCurrency(paymentSummary.totalAmount || settlementSummary.totalBatchAmount || 0, 'VND'),
      sub: `${paymentSummary.paidCount || 0} giao dịch paid`,
      className: 'bg-blue-50',
    },
    {
      label: 'Net payout đang chờ đối soát',
      value: formatCurrency(settlementSummary.processingAmount || 0, 'VND'),
      sub: `${batches.filter((item) => Number(item.status) !== 3).length} batch`,
      className: 'bg-amber-50',
    },
    {
      label: 'Net payout đã trả tenant',
      value: formatCurrency(settlementSummary.paidAmount || 0, 'VND'),
      sub: `${payouts.filter((item) => item.paidAt).length} payout`,
      className: 'bg-emerald-50',
    },
    {
      label: 'Refund đang chờ xử lý',
      value: formatCurrency(refundSummary.pendingAmount || 0, 'VND'),
      sub: `${refundSummary.pendingCount || 0} lệnh`,
      className: 'bg-rose-50',
    },
  ]), [paymentSummary, settlementSummary, refundSummary, batches, payouts]);

  return (
    <div className="p-8 space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <h1 className="text-3xl font-black text-slate-900">Tài chính & Đối soát</h1>
          <p className="text-slate-500 font-medium mt-1">Dòng tiền thật từ payment, refund và settlement batch.</p>
        </div>
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={loadFinance}
            disabled={loading}
            className="px-6 py-3 bg-white border border-slate-100 rounded-2xl font-bold text-slate-600 flex items-center gap-2 disabled:opacity-60"
          >
            <RefreshCw size={18} className={loading ? 'animate-spin' : ''} /> Tải lại
          </button>
          <Link to="/admin/settlement" className="px-8 py-3 bg-slate-900 text-white rounded-2xl font-bold flex items-center gap-2 shadow-xl shadow-blue-500/10 hover:bg-blue-600 transition-all">
            <Plus size={18} /> Tạo kỳ đối soát
          </Link>
        </div>
      </div>

      {error ? (
        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {stats.map((stat) => (
          <div key={stat.label} className={`bg-white p-6 rounded-[2.5rem] border border-slate-100 shadow-sm relative overflow-hidden group ${stat.className}`}>
            <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{stat.label}</p>
            <div className="mt-2 relative z-10">
              <p className="text-2xl font-black text-slate-900">{loading ? '--' : stat.value}</p>
              <p className="text-[10px] font-bold text-slate-400 mt-1">{stat.sub}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="flex gap-1 bg-slate-100 p-1 rounded-2xl border border-slate-200 w-fit">
        {[
          { id: 'settlements', label: 'Đối soát' },
          { id: 'payments', label: 'Thanh toán' },
          { id: 'refunds', label: 'Hoàn tiền' },
        ].map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setActiveTab(item.id)}
            className={`px-6 py-2.5 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all ${
              activeTab === item.id ? 'bg-white text-blue-600 shadow-md' : 'text-slate-400 hover:text-slate-600'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
        {activeTab === 'settlements' ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-slate-50/50">
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Batch</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Chu kỳ</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Tenant</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Net payout trả tenant</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Hành động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {batches.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-8 py-10 text-center text-sm font-bold text-slate-400">Chưa có batch settlement.</td>
                  </tr>
                ) : batches.map((batch) => (
                  <tr key={batch.id} className="hover:bg-slate-50/30 transition-all">
                    <td className="px-8 py-6 font-black text-slate-900">{batch.batchCode}</td>
                    <td className="px-8 py-6 text-xs font-bold text-slate-500">{batch.periodLabel || `${batch.startDate} - ${batch.endDate}`}</td>
                    <td className="px-8 py-6 font-bold text-slate-700">{batch.tenantCount}</td>
                    <td className="px-8 py-6 font-black text-slate-900">{formatCurrency(batch.totalNetPayoutAmount || 0, batch.currencyCode || 'VND')}</td>
                    <td className="px-8 py-6">
                      <span className={`px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest ${Number(batch.status) === 3 ? 'bg-green-100 text-green-600' : 'bg-blue-100 text-blue-600'}`}>
                        {Number(batch.status) === 3 ? 'Hoàn thành' : 'Đang xử lý'}
                      </span>
                    </td>
                    <td className="px-8 py-6"><Link to="/admin/settlement" className="text-blue-600 hover:underline text-[10px] font-black">XEM CHI TIẾT</Link></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}

        {activeTab === 'payments' ? (
          <div className="p-8 grid grid-cols-1 md:grid-cols-3 gap-5">
            {[
              { label: 'Tổng giao dịch', value: paymentSummary.totalCount || 0, icon: <CreditCard size={18} /> },
              { label: 'Paid', value: paymentSummary.paidCount || 0, icon: <ArrowUpRight size={18} /> },
              { label: 'Failed', value: paymentSummary.failedCount || 0, icon: <AlertCircle size={18} /> },
            ].map((item) => (
              <Link key={item.label} to="/admin/payments" className="rounded-3xl bg-slate-50 p-6 border border-slate-100 hover:border-blue-200 transition-all">
                <div className="flex items-center justify-between text-slate-400">
                  <p className="text-[10px] font-black uppercase tracking-widest">{item.label}</p>
                  {item.icon}
                </div>
                <p className="text-3xl font-black text-slate-900 mt-3">{loading ? '--' : item.value}</p>
              </Link>
            ))}
          </div>
        ) : null}

        {activeTab === 'refunds' ? (
          <div className="p-8 grid grid-cols-1 md:grid-cols-3 gap-5">
            {[
              { label: 'Chờ xử lý', value: refundSummary.pendingCount || 0 },
              { label: 'Đã hoàn', value: refundSummary.completedCount || 0 },
              { label: 'Từ chối', value: refundSummary.rejectedCount || 0 },
            ].map((item) => (
              <Link key={item.label} to="/admin/refunds" className="rounded-3xl bg-slate-50 p-6 border border-slate-100 hover:border-blue-200 transition-all">
                <div className="flex items-center justify-between text-slate-400">
                  <p className="text-[10px] font-black uppercase tracking-widest">{item.label}</p>
                  <RotateCcw size={18} />
                </div>
                <p className="text-3xl font-black text-slate-900 mt-3">{loading ? '--' : item.value}</p>
              </Link>
            ))}
          </div>
        ) : null}
      </div>
    </div>
  );
}
