import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { CreditCard, TrendingUp, TrendingDown, Download, ArrowUpRight, CheckCircle2, Clock, XCircle, DollarSign, Wallet, CalendarDays, BarChart3, AlertCircle } from 'lucide-react';

const MONTHS = ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'];
const REVENUE_DATA = [38, 45, 52, 40, 62, 58, 75, 68, 82, 71, 90, 88];

const TRANSACTIONS = [
  { id: 'PAY-20240601', desc: 'Tour Đà Nẵng – Đơn BK-001 (2 khách)', date: '01/06/2024', amount: '+7.600.000đ', status: 'received', net: '+7.220.000đ' },
  { id: 'PAY-20240531', desc: 'Khách sạn Vinpearl – Đơn BK-004', date: '31/05/2024', amount: '+5.400.000đ', status: 'received', net: '+5.130.000đ' },
  { id: 'PAY-20240528', desc: 'Phí nền tảng Q2/2024', date: '28/05/2024', amount: '-1.200.000đ', status: 'deducted', net: '' },
  { id: 'PAY-20240520', desc: 'Tour Phú Quốc – Đơn BK-003 (4 khách)', date: '20/05/2024', amount: '+20.800.000đ', status: 'received', net: '+19.760.000đ' },
  { id: 'PAY-20240515', desc: 'Hoàn tiền – Đơn BK-002 (Đã huỷ)', date: '15/05/2024', amount: '-2.800.000đ', status: 'refunded', net: '' },
];

const STATUS_CFG = {
  received: { label: 'Đã nhận',  color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={12} /> },
  deducted: { label: 'Phí KH',   color: 'bg-slate-100 text-slate-600',     icon: <AlertCircle size={12} /> },
  refunded: { label: 'Hoàn tiền',color: 'bg-rose-100 text-rose-700',       icon: <XCircle size={12} /> },
  pending:  { label: 'Đang xử lý',color:'bg-amber-100 text-amber-700',    icon: <Clock size={12} /> },
};

export default function PartnerFinancePage() {
  const maxBar = Math.max(...REVENUE_DATA);
  const [period, setPeriod] = useState('month');

  return (
    <div>
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Tài chính đối tác</h1>
          <p className="text-slate-500 text-sm mt-1">Doanh thu, thanh toán và lịch sử giao dịch</p>
        </div>
        <button className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
          <Download size={16} /> Xuất sao kê
        </button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Doanh thu tháng', value: '89.2M đ', delta: '+12.4%', up: true, icon: <DollarSign size={18} />, color: 'bg-gradient-to-br from-slate-900 to-slate-700 text-white' },
          { label: 'Số dư khả dụng', value: '24.6M đ', delta: 'Sẵn sàng rút', up: true, icon: <Wallet size={18} />, color: 'bg-white' },
          { label: 'Đang thanh toán', value: '7.6M đ', delta: '2 lệnh chờ', up: null, icon: <Clock size={18} />, color: 'bg-white' },
          { label: 'Phí nền tảng', value: '1.2M đ', delta: '5% doanh thu', up: false, icon: <BarChart3 size={18} />, color: 'bg-white' },
        ].map((k, i) => (
          <motion.div key={i} initial={{ opacity:0, y:10 }} animate={{ opacity:1, y:0 }} transition={{ delay: i*0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${k.color}`}
          >
            <div className={`w-9 h-9 rounded-xl mb-3 flex items-center justify-center ${k.color.includes('gradient') ? 'bg-white/10 text-white' : 'bg-slate-50 text-slate-600'}`}>{k.icon}</div>
            <p className={`text-2xl font-black ${k.color.includes('gradient') ? 'text-white' : 'text-slate-900'}`}>{k.value}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-0.5 ${k.color.includes('gradient') ? 'text-white/60' : 'text-slate-400'}`}>{k.label}</p>
            {k.delta && (
              <p className={`text-xs font-bold mt-1 flex items-center gap-1 ${k.up === true ? 'text-emerald-500' : k.up === false ? 'text-rose-500' : k.color.includes('gradient') ? 'text-white/60' : 'text-slate-500'}`}>
                {k.up === true && <TrendingUp size={12} />}
                {k.up === false && <TrendingDown size={12} />}
                {k.delta}
              </p>
            )}
          </motion.div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        {/* Revenue Bar Chart */}
        <div className="lg:col-span-2 bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="font-black text-slate-900">Doanh thu theo tháng</h2>
              <p className="text-xs text-slate-400 font-bold mt-0.5">2024 · đơn vị: triệu đồng</p>
            </div>
            <div className="flex items-center gap-1.5 text-emerald-600 bg-emerald-50 px-3 py-1.5 rounded-xl text-[10px] font-black uppercase">
              <TrendingUp size={12} /> +23% vs 2023
            </div>
          </div>
          <div className="flex items-end gap-2 h-40 px-2">
            {REVENUE_DATA.map((val, i) => {
              const h = Math.round((val / maxBar) * 100);
              return (
                <div key={i} className="flex-1 flex flex-col items-center gap-1 group">
                  <div className="w-full relative" style={{ height: '120px' }}>
                    <motion.div
                      initial={{ height: 0 }}
                      animate={{ height: `${h}%` }}
                      transition={{ delay: i * 0.03, duration: 0.4, ease: 'easeOut' }}
                      className={`absolute bottom-0 w-full rounded-t-lg transition-all ${i === 5 ? 'bg-blue-600' : 'bg-slate-100 group-hover:bg-blue-300'}`}
                    />
                  </div>
                  <span className="text-[9px] font-bold text-slate-400">{MONTHS[i]}</span>
                </div>
              );
            })}
          </div>
        </div>

        {/* Payout Info */}
        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <h2 className="font-black text-slate-900 mb-4">Thông tin thanh toán</h2>
          <div className="space-y-4">
            <div className="p-4 bg-slate-50 rounded-2xl">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Tài khoản nhận tiền</p>
              <p className="font-black text-slate-900">VCB – 0123 4567 8901</p>
              <p className="text-xs text-slate-400 font-bold mt-0.5">Hoàng Long Travel Co.</p>
            </div>
            <div className="p-4 bg-slate-50 rounded-2xl">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Chu kỳ thanh toán</p>
              <p className="font-black text-slate-900">01 – 05 hàng tháng</p>
              <p className="text-xs text-slate-400 font-bold mt-0.5">Thanh toán tiếp theo: 01/07/2024</p>
            </div>
            <div className="p-4 bg-blue-50 rounded-2xl">
              <p className="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-1">Số dư chờ thanh toán</p>
              <p className="font-black text-blue-700 text-2xl">24.6M đ</p>
              <button className="mt-3 w-full py-2.5 bg-blue-600 text-white rounded-xl text-[11px] font-black uppercase tracking-widest hover:bg-blue-700 transition-all flex items-center justify-center gap-1.5">
                Yêu cầu rút tiền <ArrowUpRight size={13} />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Transaction History */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <h2 className="font-black text-slate-900">Lịch sử giao dịch</h2>
          <select className="px-3 py-2 bg-slate-50 rounded-xl text-xs font-bold text-slate-600 border-none outline-none cursor-pointer">
            <option>Tháng 6/2024</option>
            <option>Tháng 5/2024</option>
            <option>Tháng 4/2024</option>
          </select>
        </div>
        <div className="divide-y divide-slate-50">
          {TRANSACTIONS.map((t, idx) => {
            const st = STATUS_CFG[t.status];
            const isPositive = t.amount.startsWith('+');
            return (
              <motion.div key={t.id} initial={{ opacity:0 }} animate={{ opacity:1 }} transition={{ delay: idx*0.04 }}
                className="flex items-center gap-4 px-5 py-4 hover:bg-slate-50 transition-all"
              >
                <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${isPositive ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-500'}`}>
                  {isPositive ? <TrendingUp size={16} /> : <TrendingDown size={16} />}
                </div>
                <div className="flex-1">
                  <p className="font-bold text-slate-900 text-sm">{t.desc}</p>
                  <p className="text-[10px] text-slate-400 font-bold">{t.id} · {t.date}</p>
                </div>
                <div className="text-right mr-4">
                  <p className={`font-black text-base ${isPositive ? 'text-emerald-600' : 'text-rose-500'}`}>{t.amount}</p>
                  {t.net && <p className="text-[10px] text-slate-400 font-bold">Net: {t.net}</p>}
                </div>
                <span className={`hidden md:inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase shrink-0 ${st.color}`}>
                  {st.icon} {st.label}
                </span>
              </motion.div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
