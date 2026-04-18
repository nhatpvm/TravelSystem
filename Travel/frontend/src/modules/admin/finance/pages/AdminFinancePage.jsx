import React, { useState } from 'react';
import {
  CreditCard,
  ArrowUpRight,
  ArrowDownLeft,
  Search,
  Filter,
  Download,
  ChevronRight,
  CheckCircle2,
  Clock,
  AlertCircle,
  MoreVertical,
  Plus,
  RefreshCw
} from 'lucide-react';

const COMMISSION_RULES = [
  { id: 1, service: 'Vé xe khách', default: '8%', customized: '12 tenants', lastUpdate: '10/03/2024' },
  { id: 2, service: 'Vé máy bay', default: '50,000đ/vé', customized: '0 tenants', lastUpdate: '01/01/2024' },
  { id: 3, service: 'Khách sạn', default: '15%', customized: '5 tenants', lastUpdate: '05/03/2024' },
  { id: 4, service: 'Tour du lịch', default: '12%', customized: '2 tenants', lastUpdate: '02/03/2024' },
];

const PAYOUT_REQUESTS = [
  { id: 'PAY-882', tenant: 'Hoàng Long Bus', amount: '45.000.000đ', bank: 'Vietcombank', method: 'Chuyển khoản', status: 'Pending', date: 'Vừa xong' },
  { id: 'PAY-881', tenant: 'InterCon Danang', amount: '120.000.000đ', bank: 'Techcombank', method: 'Chuyển khoản', status: 'Approved', date: '2 giờ trước' },
];

export default function AdminFinancePage() {
  const [activeTab, setActiveTab] = useState('settlements');

  const stats = [
    { label: 'Tổng GMV (Tháng)', value: '12.4 tỷ', change: '+12.5%', color: 'blue' },
    { label: 'Đang chờ đối soát', value: '840tr', count: '12 đợt', color: 'amber' },
    { label: 'Yêu cầu thanh toán', value: '450tr', count: '5 lệnh', color: 'green' },
    { label: 'Hoàn tiền (Refund)', value: '12.5tr', count: '3 lệnh', color: 'red' },
  ];

  const batches = [
    { id: 'SET-001', tenant: 'Hoàng Long Bus', amount: '240.000.000đ', period: '01/03 - 07/03', status: 'Completed', date: '08/03/2026' },
    { id: 'SET-002', tenant: 'InterContinental Danang', amount: '820.000.000đ', period: '01/03 - 07/03', status: 'Processing', date: '08/03/2026' },
    { id: 'SET-003', tenant: 'Hải Âu Limousine', amount: '45.000.000đ', period: '01/03 - 07/03', status: 'Pending', date: '08/03/2026' },
  ];

  return (
    <div className="p-8 space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
            <h1 className="text-3xl font-black text-slate-900">Tài chính & Đối soát</h1>
            <p className="text-slate-500 font-medium mt-1">Quản lý dòng tiền, hoa hồng và lệnh chi trả đối tác</p>
        </div>
        <div className="flex items-center gap-3">
             <button className="px-6 py-3 bg-white border border-slate-100 rounded-2xl font-bold text-slate-600 hover:bg-slate-50 transition-all flex items-center gap-2">
                <Download size={18} /> Báo cáo GMV
             </button>
             <button className="px-8 py-3 bg-slate-900 text-white rounded-2xl font-bold flex items-center gap-2 shadow-xl shadow-blue-500/10 hover:bg-blue-600 transition-all">
                <Plus size={18} /> Tạo kỳ đối soát
             </button>
        </div>
      </div>

      {/* Stats Summary */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
         {stats.map((stat, i) => (
            <div key={i} className="bg-white p-6 rounded-[2.5rem] border border-slate-100 shadow-sm relative overflow-hidden group">
                <div className={`absolute top-0 right-0 w-24 h-24 bg-${stat.color}-50 rounded-full -mr-12 -mt-12 transition-all group-hover:scale-110`}></div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{stat.label}</p>
                <div className="mt-2 relative z-10">
                    <p className="text-2xl font-black text-slate-900">{stat.value}</p>
                    <p className="text-[10px] font-bold mt-1">
                        {stat.change ? (
                            <span className="text-green-600 flex items-center gap-1"><ArrowUpRight size={12} /> {stat.change}</span>
                        ) : (
                            <span className="text-slate-400">{stat.count}</span>
                        )}
                    </p>
                </div>
            </div>
         ))}
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-slate-100 p-1 rounded-2xl border border-slate-200 w-fit">
        {[
          { id: 'settlements', label: 'Đối soát' },
          { id: 'commissions', label: 'Hoa hồng' },
          { id: 'payouts', label: 'Yêu cầu rút tiền' },
        ].map(t => (
          <button
            key={t.id}
            onClick={() => setActiveTab(t.id)}
            className={`px-6 py-2.5 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all ${
              activeTab === t.id ? 'bg-white text-blue-600 shadow-md' : 'text-slate-400 hover:text-slate-600'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
        {activeTab === 'settlements' && (
          <div className="overflow-x-auto">
              <table className="w-full text-left">
                  <thead>
                      <tr className="bg-slate-50/50">
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">ID Settlement</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Đối tác</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Chu kỳ</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Số tiền</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Hành động</th>
                      </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-50">
                      {batches.map((batch) => (
                          <tr key={batch.id} className="hover:bg-slate-50/30 transition-all">
                              <td className="px-8 py-6 font-black text-slate-900">{batch.id}</td>
                              <td className="px-8 py-6 font-bold text-slate-700">{batch.tenant}</td>
                              <td className="px-8 py-6 text-xs font-bold text-slate-500">{batch.period}</td>
                              <td className="px-8 py-6 font-black text-slate-900">{batch.amount}</td>
                              <td className="px-8 py-6">
                                  <span className={`px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest ${
                                    batch.status === 'Completed' ? 'bg-green-100 text-green-600' : 'bg-blue-100 text-blue-600'
                                  }`}>{batch.status}</span>
                              </td>
                              <td className="px-8 py-6"><button className="text-blue-600 hover:underline text-[10px] font-black">XEM CHI TIẾT</button></td>
                          </tr>
                      ))}
                  </tbody>
              </table>
          </div>
        )}

        {activeTab === 'commissions' && (
          <div className="p-8">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {COMMISSION_RULES.map(rule => (
                <div key={rule.id} className="bg-slate-50 rounded-3xl p-6 border border-slate-100 hover:border-blue-200 transition-all">
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{rule.service}</p>
                  <h4 className="text-2xl font-black text-slate-900 mb-4">{rule.default}</h4>
                  <div className="space-y-2 pt-4 border-t border-slate-200/50">
                    <div className="flex justify-between text-[10px] font-bold">
                      <span className="text-slate-400">Customized</span>
                      <span className="text-blue-600">{rule.customized}</span>
                    </div>
                    <div className="flex justify-between text-[10px] font-bold text-slate-400">
                      <span>Cập nhật</span>
                      <span>{rule.lastUpdate}</span>
                    </div>
                  </div>
                </div>
              ))}
              <button className="border-4 border-dashed border-slate-100 rounded-3xl flex items-center justify-center p-6 text-slate-300 hover:bg-slate-50 transition-all">
                <Plus size={32} />
              </button>
            </div>
          </div>
        )}

        {activeTab === 'payouts' && (
          <div className="overflow-x-auto">
              <table className="w-full text-left">
                  <thead>
                      <tr className="bg-slate-50/50">
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Yêu cầu</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Đối tác</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Số tiền</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Ngân hàng</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
                          <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Xử lý</th>
                      </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-50">
                      {PAYOUT_REQUESTS.map((req) => (
                          <tr key={req.id} className="hover:bg-slate-50/30 transition-all">
                              <td className="px-8 py-6">
                                <p className="font-black text-slate-900">{req.id}</p>
                                <p className="text-[10px] text-slate-400 font-bold">{req.date}</p>
                              </td>
                              <td className="px-8 py-6 font-bold text-slate-700">{req.tenant}</td>
                              <td className="px-8 py-6 font-black text-slate-900">{req.amount}</td>
                              <td className="px-8 py-6">
                                <p className="text-xs font-black text-slate-700">{req.bank}</p>
                                <p className="text-[10px] text-slate-400 font-bold uppercase">{req.method}</p>
                              </td>
                              <td className="px-8 py-6">
                                <span className={`px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest ${
                                  req.status === 'Pending' ? 'bg-amber-100 text-amber-600' : 'bg-emerald-100 text-emerald-600'
                                }`}>{req.status}</span>
                              </td>
                              <td className="px-8 py-6">
                                {req.status === 'Pending' && (
                                  <div className="flex gap-2">
                                    <button className="px-3 py-1.5 bg-emerald-500 text-white rounded-lg text-[10px] font-black uppercase hover:bg-emerald-600 transition-all">Duyệt</button>
                                    <button className="px-3 py-1.5 bg-rose-100 text-rose-500 rounded-lg text-[10px] font-black uppercase hover:bg-rose-500 hover:text-white transition-all">Từ chối</button>
                                  </div>
                                )}
                              </td>
                          </tr>
                      ))}
                  </tbody>
              </table>
          </div>
        )}
      </div>
    </div>
  );
}
