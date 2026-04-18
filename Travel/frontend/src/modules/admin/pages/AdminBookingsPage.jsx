import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Search, Ticket, ChevronDown, ChevronUp, CheckCircle2, Clock, XCircle, Plane, Bus, Hotel, Compass, User, Eye, MessageSquare, RotateCcw } from 'lucide-react';

const TYPE_ICON = { flight:<Plane size={13}/>, bus:<Bus size={13}/>, hotel:<Hotel size={13}/>, tour:<Compass size={13}/> };
const TYPE_BG   = { flight:'bg-sky-50 text-sky-600', bus:'bg-indigo-50 text-indigo-600', hotel:'bg-emerald-50 text-emerald-600', tour:'bg-amber-50 text-amber-600' };
const STATUS_CFG = {
  confirmed: { label:'Xác nhận', color:'bg-emerald-100 text-emerald-700', icon:<CheckCircle2 size={12}/> },
  pending:   { label:'Chờ xử lý',color:'bg-amber-100 text-amber-700',    icon:<Clock size={12}/> },
  cancelled: { label:'Đã huỷ',   color:'bg-rose-100 text-rose-700',      icon:<XCircle size={12}/> },
  completed: { label:'Hoàn thành',color:'bg-sky-100 text-sky-700',        icon:<CheckCircle2 size={12}/> },
};

const BOOKINGS = [
  { id:'BK-20240601-001', customer:'Nguyễn Văn A', tenant:'Hoàng Long Bus', type:'bus',    service:'HN–Vinh · 18/06',               pax:2, amount:'500.000đ',    date:'01/06/2024 09:12', status:'confirmed' },
  { id:'BK-20240601-002', customer:'Trần Thị B',   tenant:'Vinpearl Resort',type:'hotel',  service:'Phòng Superior · 3 đêm · 22/06', pax:2, amount:'5.400.000đ',  date:'01/06/2024 11:44', status:'pending' },
  { id:'BK-20240530-003', customer:'Lê Minh C',    tenant:'Sun Travel',    type:'tour',   service:'Tour Đà Nẵng 3N2Đ · 25/06',      pax:4, amount:'15.200.000đ', date:'30/05/2024 08:05', status:'completed' },
  { id:'BK-20240529-004', customer:'Phạm Thu D',   tenant:'Vietnam Airlines',type:'flight','service':'VN-123 HAN→DAD · 15/06',       pax:1, amount:'1.250.000đ',  date:'29/05/2024 19:33', status:'cancelled' },
];

export default function AdminBookingsPage() {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [expanded, setExpanded] = useState(null);

  const filtered = BOOKINGS.filter(b => {
    const m = b.customer.toLowerCase().includes(search.toLowerCase()) || b.id.toLowerCase().includes(search.toLowerCase()) || b.tenant.toLowerCase().includes(search.toLowerCase());
    const s = statusFilter === 'all' || b.status === statusFilter;
    return m && s;
  });

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Tất cả Đơn đặt</h1>
          <p className="text-slate-500 text-sm mt-1">Tìm kiếm và quản lý đơn đặt trên toàn bộ nền tảng</p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[{l:'Tổng đơn',v:BOOKINGS.length,c:'bg-slate-900 text-white'}, {l:'Đang chờ',v:1,c:'bg-amber-50'}, {l:'Hoàn thành',v:1,c:'bg-emerald-50'}, {l:'Đã huỷ',v:1,c:'bg-rose-50'}].map((s,i) => (
          <motion.div key={i} initial={{opacity:0,y:10}} animate={{opacity:1,y:0}} transition={{delay:i*0.07}}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${s.c}`}>
            <p className={`text-3xl font-black ${s.c.includes('slate-900') ? 'text-white' : 'text-slate-900'}`}>{s.v}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${s.c.includes('slate-900') ? 'text-white/60' : 'text-slate-400'}`}>{s.l}</p>
          </motion.div>
        ))}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Mã đơn, khách, tenant…" className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {[{v:'all',l:'Tất cả'},{v:'pending',l:'Chờ'},{v:'confirmed',l:'Xác nhận'},{v:'completed',l:'Hoàn thành'},{v:'cancelled',l:'Huỷ'}].map(t => (
            <button key={t.v} onClick={() => setStatusFilter(t.v)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${statusFilter===t.v?'bg-white shadow-md text-blue-600':'text-slate-400 hover:text-slate-700'}`}
            >{t.l}</button>
          ))}
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Khách / Tenant</div>
          <div className="col-span-4">Dịch vụ</div>
          <div className="col-span-2">Số tiền</div>
          <div className="col-span-2">Trạng thái</div>
          <div className="col-span-1"></div>
        </div>
        <div className="divide-y divide-slate-50">
          {filtered.map((b, idx) => {
            const st = STATUS_CFG[b.status];
            const isExp = expanded === b.id;
            return (
              <div key={b.id}>
                <div onClick={() => setExpanded(isExp ? null : b.id)}
                  className="grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 hover:bg-slate-50 cursor-pointer items-center transition-all"
                >
                  <div className="col-span-1 md:col-span-3">
                    <p className="font-black text-slate-900 text-sm">{b.customer}</p>
                    <p className="text-[10px] text-slate-400 font-bold">{b.tenant}</p>
                  </div>
                  <div className="col-span-1 md:col-span-4">
                    <div className="flex items-center gap-2">
                      <span className={`w-6 h-6 rounded-lg flex items-center justify-center ${TYPE_BG[b.type]}`}>{TYPE_ICON[b.type]}</span>
                      <div>
                        <p className="text-sm font-bold text-slate-800">{b.service}</p>
                        <p className="text-[10px] text-slate-400">{b.id}</p>
                      </div>
                    </div>
                  </div>
                  <div className="col-span-1 md:col-span-2 font-black text-slate-900 text-sm">{b.amount}</div>
                  <div className="col-span-1 md:col-span-2">
                    <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${st.color}`}>{st.icon}{st.label}</span>
                  </div>
                  <div className="col-span-1 flex justify-end">{isExp ? <ChevronUp size={16} className="text-slate-400" /> : <ChevronDown size={16} className="text-slate-400" />}</div>
                </div>
                {isExp && (
                  <motion.div initial={{opacity:0}} animate={{opacity:1}} className="bg-slate-50 border-t border-slate-100 px-5 py-4 flex flex-wrap gap-2">
                    <button className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-blue-50 hover:text-blue-600 transition-all"><Eye size={13}/>Xem chi tiết</button>
                    <button className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-slate-100 transition-all"><MessageSquare size={13}/>Ghi chú</button>
                    {b.status !== 'cancelled' && <button className="flex items-center gap-1.5 px-4 py-2 bg-rose-50 text-rose-600 rounded-xl text-[10px] font-black uppercase border border-rose-100 hover:bg-rose-100 transition-all"><XCircle size={13}/>Huỷ đơn</button>}
                    <button className="flex items-center gap-1.5 px-4 py-2 bg-amber-50 text-amber-600 rounded-xl text-[10px] font-black uppercase border border-amber-100 hover:bg-amber-100 transition-all"><RotateCcw size={13}/>Hoàn tiền</button>
                  </motion.div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
