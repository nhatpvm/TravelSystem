import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  AlertCircle,
  BarChart3,
  CheckCircle2,
  Clock,
  DollarSign,
  Download,
  TrendingDown,
  TrendingUp,
  Wallet,
  XCircle,
} from 'lucide-react';
import { getTenantCommerceFinance } from '../../../services/commerceBackofficeService';
import { formatCurrency } from '../../tenant/train/utils/presentation';

function getStatusConfig(type) {
  switch (type) {
    case 'order':
      return { label: 'Đã nhận', color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> };
    case 'refund':
      return { label: 'Hoàn tiền', color: 'bg-rose-100 text-rose-700', icon: <XCircle size={12} /> };
    default:
      return { label: 'Đang xử lý', color: 'bg-amber-100 text-amber-700', icon: <Clock size={12} /> };
  }
}

export default function PartnerFinancePage() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dashboard, setDashboard] = useState({
    summary: {},
    payoutAccount: null,
    monthlySeries: [],
    transactions: [],
  });

  async function loadDashboard() {
    setLoading(true);
    setError('');

    try {
      const response = await getTenantCommerceFinance();
      setDashboard({
        summary: response?.summary || {},
        payoutAccount: response?.payoutAccount || null,
        monthlySeries: Array.isArray(response?.monthlySeries) ? response.monthlySeries : [],
        transactions: Array.isArray(response?.transactions) ? response.transactions : [],
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải tài chính tenant.');
      setDashboard({
        summary: {},
        payoutAccount: null,
        monthlySeries: [],
        transactions: [],
      });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  const maxBar = useMemo(
    () => Math.max(...dashboard.monthlySeries.map((item) => Number(item.grossAmount || 0)), 1),
    [dashboard.monthlySeries],
  );

  const stats = [
    {
      label: 'Doanh thu tháng',
      value: formatCurrency(dashboard.summary?.currentMonthGrossAmount || 0, 'VND'),
      delta: 'Gross tháng hiện tại',
      up: true,
      icon: <DollarSign size={18} />,
      color: 'bg-gradient-to-br from-slate-900 to-slate-700 text-white',
    },
    {
      label: 'Số dư khả dụng',
      value: formatCurrency(dashboard.summary?.settledAmount || 0, 'VND'),
      delta: 'Đã đối soát',
      up: true,
      icon: <Wallet size={18} />,
      color: 'bg-white',
    },
    {
      label: 'Đang thanh toán',
      value: formatCurrency(dashboard.summary?.pendingSettlementAmount || 0, 'VND'),
      delta: 'Chờ batch tháng',
      up: null,
      icon: <Clock size={18} />,
      color: 'bg-white',
    },
    {
      label: 'Điều chỉnh refund',
      value: formatCurrency(dashboard.summary?.adjustedAmount || 0, 'VND'),
      delta: 'Cấn trừ sau hoàn',
      up: false,
      icon: <BarChart3 size={18} />,
      color: 'bg-white',
    },
  ];

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Tài chính đối tác</h1>
          <p className="text-slate-500 text-sm mt-1">Doanh thu, settlement và lịch sử điều chỉnh của tenant hiện tại.</p>
        </div>
        <button
          type="button"
          onClick={loadDashboard}
          className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg"
        >
          <Download size={16} />
          Tải lại sao kê
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
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.color}`}
          >
            <div className={`w-9 h-9 rounded-xl mb-3 flex items-center justify-center ${item.color.includes('gradient') ? 'bg-white/10 text-white' : 'bg-slate-50 text-slate-600'}`}>
              {item.icon}
            </div>
            <p className={`text-2xl font-black ${item.color.includes('gradient') ? 'text-white' : 'text-slate-900'}`}>
              {loading ? '--' : item.value}
            </p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-0.5 ${item.color.includes('gradient') ? 'text-white/60' : 'text-slate-400'}`}>
              {item.label}
            </p>
            <p className={`text-xs font-bold mt-1 flex items-center gap-1 ${item.up === true ? 'text-emerald-500' : item.up === false ? 'text-rose-500' : item.color.includes('gradient') ? 'text-white/60' : 'text-slate-500'}`}>
              {item.up === true ? <TrendingUp size={12} /> : item.up === false ? <TrendingDown size={12} /> : null}
              {item.delta}
            </p>
          </motion.div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        <div className="lg:col-span-2 bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="font-black text-slate-900">Doanh thu theo tháng</h2>
              <p className="text-xs text-slate-400 font-bold mt-0.5">12 tháng gần nhất · đơn vị VND</p>
            </div>
            <div className="flex items-center gap-1.5 text-emerald-600 bg-emerald-50 px-3 py-1.5 rounded-xl text-[10px] font-black uppercase">
              <TrendingUp size={12} />
              Net theo batch
            </div>
          </div>
          <div className="flex items-end gap-2 h-40 px-2">
            {dashboard.monthlySeries.map((item, index) => {
              const height = Math.round((Number(item.grossAmount || 0) / maxBar) * 100);

              return (
                <div key={`${item.label}-${index}`} className="flex-1 flex flex-col items-center gap-1 group">
                  <div className="w-full relative" style={{ height: '120px' }}>
                    <motion.div
                      initial={{ height: 0 }}
                      animate={{ height: `${height}%` }}
                      transition={{ delay: index * 0.03, duration: 0.4, ease: 'easeOut' }}
                      className={`absolute bottom-0 w-full rounded-t-lg transition-all ${index === dashboard.monthlySeries.length - 1 ? 'bg-blue-600' : 'bg-slate-100 group-hover:bg-blue-300'}`}
                    />
                  </div>
                  <span className="text-[9px] font-bold text-slate-400">{item.label}</span>
                </div>
              );
            })}
          </div>
        </div>

        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <h2 className="font-black text-slate-900 mb-4">Thông tin thanh toán</h2>
          <div className="space-y-4">
            <div className="p-4 bg-slate-50 rounded-2xl">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Tài khoản nhận tiền</p>
              <p className="font-black text-slate-900">
                {dashboard.payoutAccount?.bankName ? `${dashboard.payoutAccount.bankName} – ${dashboard.payoutAccount.accountNumber}` : 'Chưa cấu hình'}
              </p>
              <p className="text-xs text-slate-400 font-bold mt-0.5">{dashboard.payoutAccount?.accountHolder || 'Cập nhật trong Cài đặt > Thanh toán'}</p>
            </div>
            <div className="p-4 bg-slate-50 rounded-2xl">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Chu kỳ thanh toán</p>
              <p className="font-black text-slate-900">Batch tháng</p>
              <p className="text-xs text-slate-400 font-bold mt-0.5">Thanh toán tiếp theo: {dashboard.summary?.nextSettlementDate || '—'}</p>
            </div>
            <div className="p-4 bg-blue-50 rounded-2xl">
              <p className="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-1">Số dư chờ thanh toán</p>
              <p className="font-black text-blue-700 text-2xl">{formatCurrency(dashboard.summary?.pendingSettlementAmount || 0, 'VND')}</p>
              <div className="mt-3 w-full py-2.5 bg-white/80 text-blue-700 rounded-xl text-[11px] font-black uppercase tracking-widest flex items-center justify-center gap-1.5">
                Chờ admin đối soát
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <h2 className="font-black text-slate-900">Lịch sử giao dịch</h2>
          <div className="text-xs font-bold text-slate-400">Gross / commission / net / refund</div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
              Đang tải lịch sử giao dịch...
            </div>
          ) : dashboard.transactions.length === 0 ? (
            <div className="px-5 py-10 text-center text-sm font-bold text-slate-400">
              Chưa có giao dịch nào.
            </div>
          ) : (
            dashboard.transactions.map((item) => {
              const statusConfig = getStatusConfig(item.type);
              const isPositive = Number(item.netAmount || 0) >= 0;

              return (
                <motion.div
                  key={item.id}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="flex items-center gap-4 px-5 py-4 hover:bg-slate-50 transition-all"
                >
                  <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${isPositive ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-500'}`}>
                    {isPositive ? <TrendingUp size={16} /> : <TrendingDown size={16} />}
                  </div>
                  <div className="flex-1">
                    <p className="font-bold text-slate-900 text-sm">{item.description}</p>
                    <p className="text-[10px] text-slate-400 font-bold">{item.id} · {item.orderCode || 'N/A'}</p>
                  </div>
                  <div className="text-right mr-4">
                    <p className={`font-black text-base ${isPositive ? 'text-emerald-600' : 'text-rose-500'}`}>{formatCurrency(item.netAmount || 0, item.currencyCode || 'VND')}</p>
                    <p className="text-[10px] text-slate-400 font-bold">
                      Gross: {formatCurrency(item.grossAmount || 0, item.currencyCode || 'VND')} · Fee: {formatCurrency(item.commissionAmount || 0, item.currencyCode || 'VND')}
                    </p>
                  </div>
                  <span className={`hidden md:inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase shrink-0 ${statusConfig.color}`}>
                    {statusConfig.icon}
                    {statusConfig.label}
                  </span>
                </motion.div>
              );
            })
          )}
        </div>
      </div>

      {!loading && !dashboard.payoutAccount ? (
        <div className="mt-6 rounded-2xl border border-amber-100 bg-amber-50 px-5 py-4 text-sm font-bold text-amber-700 flex items-center gap-3">
          <AlertCircle size={16} />
          Tenant chưa có payout account, admin vẫn thấy dòng tiền nhưng chưa thể payout đúng tài khoản.
        </div>
      ) : null}
    </div>
  );
}
