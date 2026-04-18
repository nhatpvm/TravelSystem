import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { CreditCard, Search, ChevronDown, ChevronUp, CheckCircle2, Clock, XCircle, RefreshCw, Eye } from 'lucide-react';

const TRANSACTIONS = [
  { id:'TXN-20240601-001', booking:'BK-001', customer:'Nguyễn Văn A', amount:'500.000', method:'SePay QR', provider:'SePay', status:'success', date:'01/06 09:15', callback:'received', ref:'SEPA123456' },
  { id:'TXN-20240601-002', booking:'BK-002', customer:'Trần Thị B',   amount:'5.400.000', method:'Bank Transfer', provider:'VCB', status:'pending', date:'01/06 11:44', callback:'pending', ref:'VCB987654' },
  { id:'TXN-20240530-003', booking:'BK-003', customer:'Lê Minh C',    amount:'15.200.000', method:'SePay QR', provider:'SePay', status:'success', date:'30/05 08:05', callback:'received', ref:'SEPA456789' },
  { id:'TXN-20240529-004', booking:'BK-004', customer:'Phạm Thu D',   amount:'1.250.000', method:'SePay QR', provider:'SePay', status:'failed',  date:'29/05 19:33', callback:'error', ref:'SEPA000001' },
  { id:'TXN-20240528-005', booking:'BK-005', customer:'Hoàng Văn E',  amount:'850.000',  method:'Bank Transfer', provider:'TCB', status:'expired', date:'28/05 14:00', callback:'none', ref:'–' },
];

const STATUS_CFG = {
  success: { l:'Thành công', c:'bg-emerald-100 text-emerald-700', icon:<CheckCircle2 size={12}/> },
  pending: { l:'Chờ xử lý', c:'bg-amber-100 text-amber-700',    icon:<Clock size={12}/> },
  failed:  { l:'Thất bại',  c:'bg-rose-100 text-rose-700',       icon:<XCircle size={12}/> },
  expired: { l:'Hết hạn',   c:'bg-slate-100 text-slate-600',     icon:<Clock size={12}/> },
};

const CB_CFG = {
  received:{ l:'Nhận CB', c:'text-emerald-600' },
  pending: { l:'Chờ CB',  c:'text-amber-600' },
  error:   { l:'Lỗi CB',  c:'text-rose-600' },
  none:    { l:'Không CB', c:'text-slate-400' },
};

export default function AdminPaymentsPage() {
  const [search, setSearch] = useState('');
  const [statusF, setStatusF] = useState('all');
  const [expanded, setExpanded] = useState(null);

  const filtered = TRANSACTIONS.filter(t => {
    const m = t.customer.toLowerCase().includes(search.toLowerCase()) || t.id.toLowerCase().includes(search.toLowerCase()) || t.ref.toLowerCase().includes(search.toLowerCase());
    const s = statusF === 'all' || t.status === statusF;
    return m && s;
  });

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Thanh toán & Giao dịch</h1>
        <p className="text-slate-500 text-sm mt-1">PaymentIntents, Transactions và Callback logs</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { l:'Tổng GD', v: TRANSACTIONS.length, c:'bg-slate-900 text-white' },
          { l:'Thành công', v: TRANSACTIONS.filter(t=>t.status==='success').length, c:'bg-emerald-50' },
          { l:'Thất bại/HH', v: TRANSACTIONS.filter(t=>['failed','expired'].includes(t.status)).length, c:'bg-rose-50' },
          { l:'Tổng giá trị', v: '23.2M', c:'bg-blue-50' },
        ].map((s,i) => (
          <motion.div key={i} initial={{opacity:0,y:10}} animate={{opacity:1,y:0}} transition={{delay:i*0.07}}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${s.c}`}>
            <p className={`text-3xl font-black ${s.c.includes('slate-900')?'text-white':'text-slate-900'}`}>{s.v}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${s.c.includes('slate-900')?'text-white/60':'text-slate-400'}`}>{s.l}</p>
          </motion.div>
        ))}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Mã GD, khách, ref…" className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {[{v:'all',l:'Tất cả'},{v:'success',l:'Thành công'},{v:'pending',l:'Chờ'},{v:'failed',l:'Thất bại'},{v:'expired',l:'Hết hạn'}].map(f=>(
            <button key={f.v} onClick={()=>setStatusF(f.v)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest whitespace-nowrap transition-all ${statusF===f.v?'bg-white shadow-md text-blue-600':'text-slate-400 hover:text-slate-700'}`}>{f.l}</button>
          ))}
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-3 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Giao dịch / Khách</div>
          <div className="col-span-2">Số tiền</div>
          <div className="col-span-2">Phương thức</div>
          <div className="col-span-2">Callback</div>
          <div className="col-span-2">Trạng thái</div>
          <div className="col-span-1"></div>
        </div>
        <div className="divide-y divide-slate-50">
          {filtered.map((tx, idx) => {
            const st = STATUS_CFG[tx.status];
            const cb = CB_CFG[tx.callback];
            const isExp = expanded === tx.id;
            return (
              <div key={tx.id}>
                <div onClick={()=>setExpanded(isExp?null:tx.id)}
                  className="grid grid-cols-2 md:grid-cols-12 gap-3 px-5 py-4 hover:bg-slate-50 cursor-pointer items-center transition-all"
                >
                  <div className="col-span-1 md:col-span-3">
                    <p className="font-black text-slate-900 text-xs">{tx.id}</p>
                    <p className="text-[10px] text-slate-400 font-bold">{tx.customer}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2 font-black text-slate-900">{tx.amount}đ</div>
                  <div className="col-span-1 md:col-span-2 text-xs font-bold text-slate-600">{tx.method}</div>
                  <div className={`col-span-1 md:col-span-2 text-[10px] font-black ${cb.c}`}>{cb.l}</div>
                  <div className="col-span-1 md:col-span-2">
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${st.c}`}>{st.icon}{st.l}</span>
                  </div>
                  <div className="col-span-1 flex justify-end">{isExp?<ChevronUp size={15} className="text-slate-400"/>:<ChevronDown size={15} className="text-slate-400"/>}</div>
                </div>
                {isExp && (
                  <div className="bg-slate-50 border-t border-slate-100 px-5 py-4">
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-xs mb-3">
                      {[{l:'Ref', v:tx.ref},{l:'Booking', v:tx.booking},{l:'Provider', v:tx.provider},{l:'Ngày GD', v:tx.date}].map((f,i)=>(
                        <div key={i} className="bg-white rounded-xl p-3 border border-slate-100">
                          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{f.l}</p>
                          <p className="font-black text-slate-900 mt-1">{f.v}</p>
                        </div>
                      ))}
                    </div>
                    <div className="flex gap-2 flex-wrap">
                      <button className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 rounded-xl text-[10px] font-black uppercase border border-slate-100 hover:bg-blue-50 hover:text-blue-600 transition-all"><Eye size={12}/>Chi tiết</button>
                      {tx.status === 'pending' && <button className="flex items-center gap-1.5 px-4 py-2 bg-amber-50 text-amber-600 rounded-xl text-[10px] font-black uppercase border border-amber-100 hover:bg-amber-100 transition-all"><RefreshCw size={12}/>Retry CB</button>}
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
