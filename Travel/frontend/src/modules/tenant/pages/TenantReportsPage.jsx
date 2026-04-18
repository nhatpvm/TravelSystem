import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { BarChart3, TrendingUp, TrendingDown, Download, Calendar, Bus, Hotel, Compass, Users, DollarSign, Percent } from 'lucide-react';

const MONTHS = ['T1','T2','T3','T4','T5','T6','T7','T8','T9','T10','T11','T12'];
const REVENUE = [32, 41, 38, 52, 48, 62, 58, 71, 65, 78, 82, 89];
const BOOKINGS_DATA = [28, 34, 30, 44, 39, 51, 47, 60, 53, 65, 68, 74];

const SERVICE_BREAKDOWN = [
  { name: 'Tour du lịch', pct: 48, amount: '42.8M', color: 'bg-blue-500', icon: <Compass size={14} /> },
  { name: 'Khách sạn',   pct: 31, amount: '27.6M', color: 'bg-emerald-500', icon: <Hotel size={14} /> },
  { name: 'Xe khách',    pct: 21, amount: '18.7M', color: 'bg-amber-500', icon: <Bus size={14} /> },
];

const TOP_PRODUCTS = [
  { name: 'Tour Đà Nẵng 3N2Đ', bookings: 84, revenue: '319.2M', growth: +18 },
  { name: 'Phòng Superior Nha Trang', bookings: 56, revenue: '100.8M', growth: +12 },
  { name: 'Tour Phú Quốc 4N3Đ', bookings: 48, revenue: '249.6M', growth: +31 },
  { name: 'Xe HN–Vinh', bookings: 210, revenue: '52.5M', growth: -4 },
  { name: 'Tour Sapa 3N2Đ', bookings: 20, revenue: '82M', growth: +8 },
];

export default function TenantReportsPage() {
  const [period, setPeriod] = useState('year');
  const maxR = Math.max(...REVENUE);

  return (
    <div>
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Báo cáo & Thống kê</h1>
          <p className="text-slate-500 text-sm mt-1">Tổng quan doanh thu, lượt đặt và hiệu suất dịch vụ</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex bg-white rounded-2xl p-1 border border-slate-100 gap-1 shadow-sm">
            {['month','quarter','year'].map(p => (
              <button key={p} onClick={() => setPeriod(p)}
                className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${period === p ? 'bg-slate-900 text-white shadow-md' : 'text-slate-400 hover:text-slate-700'}`}
              >{p === 'month' ? 'Tháng' : p === 'quarter' ? 'Quý' : 'Năm'}</button>
            ))}
          </div>
          <button className="flex items-center gap-2 px-5 py-3 bg-slate-900 text-white rounded-2xl font-bold text-xs hover:bg-blue-600 transition-all shadow-lg">
            <Download size={14} /> Xuất Excel
          </button>
        </div>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Tổng doanh thu', value: '89.1M đ', delta: '+12.4%', up: true, icon: <DollarSign size={18} />, dark: true },
          { label: 'Lượt đặt chỗ', value: '1,240', delta: '+18%', up: true, icon: <Users size={18} /> },
          { label: 'Tỷ lệ hoàn thành', value: '91.3%', delta: '+2.1%', up: true, icon: <Percent size={18} /> },
          { label: 'Tỷ lệ huỷ', value: '4.9%', delta: '-0.8%', up: false, icon: <BarChart3 size={18} /> },
        ].map((k, i) => (
          <motion.div key={i} initial={{ opacity:0, y:12 }} animate={{ opacity:1, y:0 }} transition={{ delay: i*0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${k.dark ? 'bg-gradient-to-br from-slate-900 to-slate-700' : 'bg-white'}`}
          >
            <div className={`w-9 h-9 rounded-xl mb-3 flex items-center justify-center ${k.dark ? 'bg-white/10 text-white' : 'bg-slate-50 text-slate-600'}`}>{k.icon}</div>
            <p className={`text-2xl font-black ${k.dark ? 'text-white' : 'text-slate-900'}`}>{k.value}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-0.5 ${k.dark ? 'text-white/50' : 'text-slate-400'}`}>{k.label}</p>
            <p className={`text-xs font-bold mt-1 flex items-center gap-1 ${k.up ? 'text-emerald-400' : 'text-rose-400'}`}>
              {k.up ? <TrendingUp size={11} /> : <TrendingDown size={11} />}{k.delta} so với kỳ trước
            </p>
          </motion.div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        {/* Revenue Bar Chart */}
        <div className="lg:col-span-2 bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <div className="flex items-center justify-between mb-6">
            <h2 className="font-black text-slate-900">Doanh thu theo tháng 2024</h2>
            <span className="text-xs font-bold text-slate-400">Đơn vị: Triệu đ</span>
          </div>
          <div className="flex items-end gap-2 h-40">
            {REVENUE.map((val, i) => {
              const h = Math.round((val / maxR) * 100);
              return (
                <div key={i} className="flex-1 flex flex-col items-center gap-1 group cursor-pointer">
                  <div className="relative w-full" style={{ height: '120px' }}>
                    <motion.div
                      initial={{ height: 0 }} animate={{ height: `${h}%` }}
                      transition={{ delay: i * 0.04, duration: 0.5, ease: 'easeOut' }}
                      className={`absolute bottom-0 w-full rounded-t-lg ${i === new Date().getMonth() ? 'bg-blue-600' : 'bg-slate-100 group-hover:bg-blue-300'} transition-colors`}
                    />
                  </div>
                  <span className="text-[9px] font-bold text-slate-400">{MONTHS[i]}</span>
                </div>
              );
            })}
          </div>

          {/* Secondary line: bookings */}
          <div className="mt-4 pt-4 border-t border-slate-100 flex items-center gap-4">
            <div className="flex items-center gap-2">
              <div className="w-3 h-3 bg-blue-600 rounded-sm" />
              <span className="text-[10px] font-bold text-slate-500">Tháng hiện tại</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-3 h-3 bg-slate-100 rounded-sm" />
              <span className="text-[10px] font-bold text-slate-500">Các tháng khác</span>
            </div>
          </div>
        </div>

        {/* Service Breakdown */}
        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <h2 className="font-black text-slate-900 mb-6">Cơ cấu doanh thu</h2>
          <div className="space-y-5">
            {SERVICE_BREAKDOWN.map((s, i) => (
              <div key={i}>
                <div className="flex items-center justify-between mb-1.5">
                  <div className="flex items-center gap-2">
                    <span className="text-slate-400">{s.icon}</span>
                    <span className="text-sm font-bold text-slate-700">{s.name}</span>
                  </div>
                  <span className="text-sm font-black text-slate-900">{s.pct}%</span>
                </div>
                <div className="w-full h-2.5 bg-slate-100 rounded-full overflow-hidden">
                  <motion.div
                    initial={{ width: 0 }} animate={{ width: `${s.pct}%` }}
                    transition={{ delay: i * 0.15, duration: 0.6, ease: 'easeOut' }}
                    className={`h-full rounded-full ${s.color}`}
                  />
                </div>
                <p className="text-[10px] text-slate-400 font-bold mt-1">{s.amount} đ tháng này</p>
              </div>
            ))}
          </div>

          <div className="mt-6 pt-4 border-t border-slate-100">
            <div className="flex items-center justify-between">
              <span className="font-black text-slate-900 text-sm">Tổng</span>
              <span className="font-black text-xl text-slate-900">89.1M đ</span>
            </div>
          </div>
        </div>
      </div>

      {/* Top Products */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <h2 className="font-black text-slate-900">Top sản phẩm bán chạy</h2>
          <select className="px-3 py-2 bg-slate-50 rounded-xl text-xs font-bold text-slate-600 border-none outline-none cursor-pointer">
            <option>Theo doanh thu</option>
            <option>Theo lượt đặt</option>
          </select>
        </div>
        <div className="divide-y divide-slate-50">
          {TOP_PRODUCTS.map((p, idx) => (
            <motion.div key={idx} initial={{ opacity:0 }} animate={{ opacity:1 }} transition={{ delay: idx*0.05 }}
              className="flex items-center gap-4 px-5 py-4 hover:bg-slate-50 transition-all"
            >
              <div className="w-8 h-8 bg-slate-100 rounded-xl flex items-center justify-center font-black text-slate-500 text-sm shrink-0">
                {idx + 1}
              </div>
              <div className="flex-1">
                <p className="font-bold text-slate-900 text-sm">{p.name}</p>
                <p className="text-xs text-slate-400 font-bold">{p.bookings} lượt đặt</p>
              </div>
              <div className="text-right">
                <p className="font-black text-slate-900">{p.revenue}</p>
                <p className={`text-xs font-bold flex items-center gap-1 justify-end ${p.growth > 0 ? 'text-emerald-500' : 'text-rose-500'}`}>
                  {p.growth > 0 ? <TrendingUp size={11} /> : <TrendingDown size={11} />}
                  {Math.abs(p.growth)}%
                </p>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  );
}
