import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { RefreshCw, Search, Clock, CheckCircle2, XCircle, AlertCircle, Eye, MessageSquare, RotateCcw, DollarSign } from 'lucide-react';

const REFUNDS = [
  { id: 'RF-20240601-001', booking: 'BK-20240520-001', customer: 'Nguyễn Văn A', service: 'Vé xe HN–Vinh · 20/06', amount: '130.000đ', reason: 'Khách hủy trước 48h', status: 'pending',    date: '01/06/2024', processed: null },
  { id: 'RF-20240530-002', booking: 'BK-20240510-002', customer: 'Trần Thị B',   service: 'Tour Đà Nẵng 3N2Đ',    amount: '3.800.000đ',reason: 'Thay đổi lịch trình',  status: 'approved',   date: '30/05/2024', processed: '01/06/2024' },
  { id: 'RF-20240528-003', booking: 'BK-20240505-003', customer: 'Lê Minh C',    service: 'Phòng Deluxe 2 đêm',   amount: '2.100.000đ',reason: 'Khách sạn không đạt chuẩn', status: 'rejected', date: '28/05/2024', processed: '29/05/2024' },
  { id: 'RF-20240526-004', booking: 'BK-20240502-004', customer: 'Phạm Thu D',   service: 'Vé bay VN-123',        amount: '850.000đ',  reason: 'Chuyến bay bị hủy',   status: 'completed',  date: '26/05/2024', processed: '27/05/2024' },
  { id: 'RF-20240525-005', booking: 'BK-20240501-005', customer: 'Hoàng Văn E',  service: 'Vé tàu SE1 HN→HUE',   amount: '540.000đ',  reason: 'Tàu bị kéo dài giờ',  status: 'pending',    date: '25/05/2024', processed: null },
];

const STATUS_CFG = {
  pending:   { l: 'Chờ duyệt',  c: 'bg-amber-100 text-amber-700',   icon: <Clock size={12} /> },
  approved:  { l: 'Đã duyệt',   c: 'bg-blue-100 text-blue-700',     icon: <CheckCircle2 size={12} /> },
  completed: { l: 'Đã hoàn',    c: 'bg-emerald-100 text-emerald-700',icon: <CheckCircle2 size={12} /> },
  rejected:  { l: 'Từ chối',    c: 'bg-rose-100 text-rose-700',     icon: <XCircle size={12} /> },
};

export default function AdminRefundsPage() {
  const [filter, setFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [refunds, setRefunds] = useState(REFUNDS);
  const [selected, setSelected] = useState(null);

  const filtered = refunds.filter(r => {
    const m = r.customer.toLowerCase().includes(search.toLowerCase()) || r.id.toLowerCase().includes(search.toLowerCase());
    const f = filter === 'all' || r.status === filter;
    return m && f;
  });

  const approve = (id) => setRefunds(p => p.map(r => r.id === id ? { ...r, status: 'approved', processed: new Date().toLocaleDateString('vi') } : r));
  const reject  = (id) => setRefunds(p => p.map(r => r.id === id ? { ...r, status: 'rejected', processed: new Date().toLocaleDateString('vi') } : r));
  const complete = (id) => setRefunds(p => p.map(r => r.id === id ? { ...r, status: 'completed', processed: new Date().toLocaleDateString('vi') } : r));

  const total = { pending: refunds.filter(r => r.status === 'pending').length, pending_amount: '670.000đ', completed: refunds.filter(r => r.status === 'completed').length };

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Quản lý Hoàn tiền</h1>
          <p className="text-slate-500 text-sm mt-1">Duyệt, từ chối và theo dõi tiến trình hoàn tiền</p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { l: 'Chờ duyệt',   v: total.pending,   c: 'bg-amber-50',   icon: <Clock size={18} className="text-amber-600" /> },
          { l: 'Đã hoàn',     v: total.completed, c: 'bg-emerald-50', icon: <CheckCircle2 size={18} className="text-emerald-600" /> },
          { l: 'Từ chối',     v: refunds.filter(r=>r.status==='rejected').length, c: 'bg-rose-50', icon: <XCircle size={18} className="text-rose-600" /> },
          { l: 'Tổng yêu cầu',v: refunds.length,  c: 'bg-slate-900',  icon: <RotateCcw size={18} className="text-white/60" /> },
        ].map((s, i) => (
          <motion.div key={i} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${s.c} flex items-center gap-4`}>
            <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${s.c === 'bg-slate-900' ? 'bg-white/10' : 'bg-white/60'}`}>{s.icon}</div>
            <div>
              <p className={`text-3xl font-black ${s.c === 'bg-slate-900' ? 'text-white' : 'text-slate-900'}`}>{s.v}</p>
              <p className={`text-[10px] font-bold uppercase tracking-widest ${s.c === 'bg-slate-900' ? 'text-white/60' : 'text-slate-400'}`}>{s.l}</p>
            </div>
          </motion.div>
        ))}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Mã hoàn, tên khách…" className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {[{v:'all',l:'Tất cả'},{v:'pending',l:'Chờ duyệt'},{v:'approved',l:'Đã duyệt'},{v:'completed',l:'Đã hoàn'},{v:'rejected',l:'Từ chối'}].map(f => (
            <button key={f.v} onClick={() => setFilter(f.v)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest whitespace-nowrap transition-all ${filter===f.v?'bg-white shadow-md text-blue-600':'text-slate-400 hover:text-slate-700'}`}
            >{f.l}</button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* List */}
        <div className="lg:col-span-2 space-y-3">
          {filtered.map((r, idx) => {
            const st = STATUS_CFG[r.status];
            return (
              <motion.div key={r.id} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: idx * 0.05 }}
                onClick={() => setSelected(r)}
                className={`bg-white rounded-2xl p-5 shadow-sm border transition-all cursor-pointer hover:shadow-md ${selected?.id === r.id ? 'border-[#1EB4D4]' : 'border-slate-100'}`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap mb-1">
                      <p className="font-black text-slate-900 text-sm">{r.id}</p>
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${st.c}`}>{st.icon}{st.l}</span>
                    </div>
                    <p className="text-sm font-bold text-slate-700">{r.customer} · {r.service}</p>
                    <p className="text-xs text-slate-400 font-bold mt-0.5">{r.reason} · {r.date}</p>
                  </div>
                  <p className="font-black text-slate-900 shrink-0">{r.amount}</p>
                </div>
                {r.status === 'pending' && (
                  <div className="flex gap-2 mt-3">
                    <button onClick={e => { e.stopPropagation(); approve(r.id); }} className="px-4 py-2 bg-emerald-50 text-emerald-700 rounded-xl text-[10px] font-black uppercase hover:bg-emerald-100 transition-all">✓ Duyệt</button>
                    <button onClick={e => { e.stopPropagation(); reject(r.id); }} className="px-4 py-2 bg-rose-50 text-rose-600 rounded-xl text-[10px] font-black uppercase hover:bg-rose-100 transition-all">✗ Từ chối</button>
                  </div>
                )}
                {r.status === 'approved' && (
                  <button onClick={e => { e.stopPropagation(); complete(r.id); }} className="mt-3 px-4 py-2 bg-blue-50 text-blue-700 rounded-xl text-[10px] font-black uppercase hover:bg-blue-100 transition-all">⟳ Xác nhận đã hoàn tiền</button>
                )}
              </motion.div>
            );
          })}
          {filtered.length === 0 && (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
              <RotateCcw size={36} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold">Không có yêu cầu nào</p>
            </div>
          )}
        </div>

        {/* Detail panel */}
        <div className="lg:col-span-1">
          {selected ? (
            <div className="sticky top-6 bg-white rounded-2xl p-6 shadow-xl shadow-slate-100/60 border border-slate-100">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4">Chi tiết yêu cầu</p>
              {[
                { l: 'Mã hoàn', v: selected.id },
                { l: 'Đơn đặt', v: selected.booking },
                { l: 'Khách hàng', v: selected.customer },
                { l: 'Dịch vụ', v: selected.service },
                { l: 'Lý do', v: selected.reason },
                { l: 'Số tiền', v: selected.amount },
                { l: 'Ngày yêu cầu', v: selected.date },
                { l: 'Ngày xử lý', v: selected.processed || '–' },
              ].map((f, i) => (
                <div key={i} className="flex justify-between py-2.5 border-b border-slate-50 last:border-0">
                  <span className="text-xs text-slate-400 font-bold">{f.l}</span>
                  <span className="text-xs font-black text-slate-900 text-right max-w-[55%]">{f.v}</span>
                </div>
              ))}
              <div className="mt-4">
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest block mb-2">Ghi chú xử lý</label>
                <textarea rows={3} placeholder="Nhập ghi chú nội bộ…" className="w-full bg-slate-50 rounded-xl p-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30 resize-none" />
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
