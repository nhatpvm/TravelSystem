import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { ActivitySquare, RefreshCw, CheckCircle2, XCircle, Clock, AlertTriangle } from 'lucide-react';

const MESSAGES = [
  { id:'OB-001', type:'BOOKING_CONFIRMED', entity:'BK-0012', attempt:1, max:3, status:'sent',    next:null,            error:null, date:'01/06/2024 09:15' },
  { id:'OB-002', type:'PAYMENT_SUCCESS',   entity:'TXN-001', attempt:1, max:3, status:'sent',    next:null,            error:null, date:'01/06/2024 09:16' },
  { id:'OB-003', type:'REFUND_INITIATED',  entity:'RF-0009', attempt:2, max:3, status:'pending', next:'01/06 10:00',   error:'Timeout 30s', date:'01/06/2024 09:45' },
  { id:'OB-004', type:'TICKET_ISSUED',     entity:'TK-0055', attempt:3, max:3, status:'failed',  next:null,            error:'HTTP 503 – Service Unavailable', date:'31/05/2024 22:10' },
  { id:'OB-005', type:'BOOKING_CANCELLED', entity:'BK-0009', attempt:1, max:3, status:'sent',    next:null,            error:null, date:'31/05/2024 17:30' },
];

const STATUS_CFG = {
  sent:    { l:'Đã gửi',  c:'bg-emerald-100 text-emerald-700', icon:<CheckCircle2 size={12}/> },
  pending: { l:'Đang chờ', c:'bg-amber-100 text-amber-700',   icon:<Clock size={12}/> },
  failed:  { l:'Thất bại', c:'bg-rose-100 text-rose-700',     icon:<XCircle size={12}/> },
};

export default function AdminOutboxPage() {
  const [messages, setMessages] = useState(MESSAGES);
  const retry = (id) => setMessages(prev => prev.map(m => m.id===id ? {...m, status:'pending', attempt:m.attempt+1, error:null} : m));

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Outbox Messages</h1>
        <p className="text-slate-500 text-sm mt-1">Hàng đợi sự kiện gửi đi — retry, xem lỗi</p>
      </div>

      <div className="grid grid-cols-3 gap-4 mb-8">
        {[
          {l:'Đã gửi',  v:messages.filter(m=>m.status==='sent').length,    c:'bg-emerald-50'},
          {l:'Chờ retry',v:messages.filter(m=>m.status==='pending').length, c:'bg-amber-50'},
          {l:'Thất bại', v:messages.filter(m=>m.status==='failed').length,  c:'bg-rose-50'},
        ].map((s,i)=>(
          <div key={i} className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${s.c}`}>
            <p className="text-3xl font-black text-slate-900">{s.v}</p>
            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mt-1">{s.l}</p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="divide-y divide-slate-50">
          {messages.map((m,i)=>{
            const st = STATUS_CFG[m.status];
            return (
              <div key={m.id} className="flex items-center gap-4 px-5 py-4 hover:bg-slate-50 transition-all">
                <div className="w-8 h-8 bg-slate-100 rounded-xl flex items-center justify-center shrink-0"><ActivitySquare size={14} className="text-slate-500"/></div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-0.5">
                    <p className="font-black text-slate-900 text-xs">{m.type}</p>
                    <span className="text-[10px] text-slate-400 font-bold">#{m.entity}</span>
                  </div>
                  <div className="flex items-center gap-3 text-[10px] font-bold text-slate-400">
                    <span>Attempt: {m.attempt}/{m.max}</span>
                    {m.next && <span>Next: {m.next}</span>}
                    {m.error && <span className="text-rose-500 flex items-center gap-0.5"><AlertTriangle size={10}/>{m.error}</span>}
                  </div>
                </div>
                <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${st.c}`}>{st.icon}{st.l}</span>
                <p className="text-[10px] text-slate-400 font-bold shrink-0">{m.date}</p>
                {(m.status === 'failed' || m.status === 'pending') && m.attempt < m.max && (
                  <button onClick={()=>retry(m.id)} className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all shrink-0"><RefreshCw size={14}/></button>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
