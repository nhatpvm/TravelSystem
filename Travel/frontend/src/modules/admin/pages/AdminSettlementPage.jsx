import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { DollarSign, CheckCircle2, Clock, AlertCircle, ChevronDown, ChevronUp, Plus } from 'lucide-react';

const BATCHES = [
  { id:'STLB-202406-001', period:'01/06–07/06/2024', tenants:5, total:'142.500.000', status:'completed', lines:12, paid:'142.500.000', date:'08/06/2024' },
  { id:'STLB-202405-001', period:'01/05–31/05/2024', tenants:5, total:'320.000.000', status:'completed', lines:24, paid:'320.000.000', date:'02/06/2024' },
  { id:'STLB-202406-002', period:'08/06–14/06/2024', tenants:3, total:'89.200.000',  status:'pending',   lines:8,  paid:'0',          date:'–' },
];
const PAYOUTS = [
  { id:'PO-001', tenant:'Hoàng Long Bus', batch:'STLB-202405-001', amount:'45.000.000', bank:'VCB – 1234567890', status:'paid', date:'02/06/2024' },
  { id:'PO-002', tenant:'Sun Travel',     batch:'STLB-202405-001', amount:'78.500.000', bank:'TCB – 0987654321', status:'paid', date:'02/06/2024' },
  { id:'PO-003', tenant:'Vinpearl Hotel', batch:'STLB-202406-002', amount:'30.200.000', bank:'VCB – 1111222233', status:'processing', date:'–' },
];
const STATUS_CFG = {
  completed:  { l:'Hoàn thành', c:'bg-emerald-100 text-emerald-700', icon:<CheckCircle2 size={12}/> },
  pending:    { l:'Chờ duyệt', c:'bg-amber-100 text-amber-700',    icon:<Clock size={12}/> },
  processing: { l:'Đang xử lý',c:'bg-blue-100 text-blue-700',      icon:<Clock size={12}/> },
  paid:       { l:'Đã trả',    c:'bg-emerald-100 text-emerald-700', icon:<CheckCircle2 size={12}/> },
};

export default function AdminSettlementPage() {
  const [tab, setTab] = useState('batches');
  const [expanded, setExpanded] = useState(null);

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Settlement & Payouts</h1>
          <p className="text-slate-500 text-sm mt-1">Quyết toán theo đợt và thanh toán cho đối tác</p>
        </div>
        <button className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg"><Plus size={16}/>Tạo batch mới</button>
      </div>

      {/* Summary KPIs */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { l:'Tổng quyết toán', v:'462.5M', c:'bg-slate-900 text-white' },
          { l:'Đang xử lý',      v:'89.2M',  c:'bg-amber-50' },
          { l:'Đã thanh toán',   v:'462.5M', c:'bg-emerald-50' },
          { l:'Đối tác',         v: PAYOUTS.length, c:'bg-blue-50' },
        ].map((s,i) => (
          <div key={i} className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${s.c}`}>
            <p className={`text-2xl font-black ${s.c.includes('slate-900')?'text-white':'text-slate-900'}`}>{s.v}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${s.c.includes('slate-900')?'text-white/60':'text-slate-400'}`}>{s.l}</p>
          </div>
        ))}
      </div>

      <div className="flex gap-1 bg-white rounded-2xl p-1 border border-slate-100 shadow-sm mb-6 w-fit">
        {[{v:'batches',l:'Settlement Batches'},{v:'payouts',l:'Payouts'}].map(t=>(
          <button key={t.v} onClick={()=>setTab(t.v)}
            className={`px-5 py-3 rounded-xl text-xs font-black uppercase tracking-widest transition-all ${tab===t.v?'bg-slate-900 text-white shadow-md':'text-slate-400 hover:text-slate-700'}`}>{t.l}</button>
        ))}
      </div>

      {tab === 'batches' && (
        <div className="space-y-3">
          {BATCHES.map((b,i) => {
            const st = STATUS_CFG[b.status];
            const isExp = expanded === b.id;
            return (
              <div key={b.id} className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
                <div onClick={()=>setExpanded(isExp?null:b.id)}
                  className="flex items-center gap-4 p-5 cursor-pointer hover:bg-slate-50 transition-all"
                >
                  <div className="flex-1">
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{b.id}</p>
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${st.c}`}>{st.icon}{st.l}</span>
                    </div>
                    <p className="text-xs text-slate-400 font-bold mt-0.5">{b.period} · {b.tenants} đối tác · {b.lines} lines</p>
                  </div>
                  <div className="text-right">
                    <p className="font-black text-slate-900">{b.total}đ</p>
                    <p className="text-[10px] text-slate-400 font-bold">Ngày trả: {b.date}</p>
                  </div>
                  {isExp?<ChevronUp size={16} className="text-slate-400 shrink-0"/>:<ChevronDown size={16} className="text-slate-400 shrink-0"/>}
                </div>
                {isExp && (
                  <div className="border-t border-slate-100 p-5 bg-slate-50">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Settlement Lines</p>
                    {PAYOUTS.filter(p=>p.batch===b.id).map((p,j)=>(
                      <div key={j} className="flex items-center gap-4 bg-white rounded-xl p-3 mb-2 border border-slate-100">
                        <p className="font-black text-slate-900 text-sm flex-1">{p.tenant}</p>
                        <p className="text-xs text-slate-500 font-bold">{p.bank}</p>
                        <p className="font-black text-slate-900">{p.amount}đ</p>
                        <span className={`px-2 py-0.5 rounded-lg text-[10px] font-black uppercase ${STATUS_CFG[p.status].c}`}>{STATUS_CFG[p.status].l}</span>
                      </div>
                    ))}
                    {PAYOUTS.filter(p=>p.batch===b.id).length === 0 && <p className="text-xs text-slate-400 font-bold">Chưa có lines.</p>}
                    {b.status === 'pending' && (
                      <button className="mt-3 px-5 py-2.5 bg-emerald-600 text-white rounded-xl font-black text-xs uppercase tracking-widest hover:bg-emerald-700 transition-all">✓ Duyệt & Khởi tạo Payout</button>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {tab === 'payouts' && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <div className="hidden md:grid grid-cols-12 gap-3 px-5 py-3 bg-slate-50 border-b border-slate-100 text-[10px] font-black text-slate-400 uppercase tracking-widest">
            <div className="col-span-2">Mã</div><div className="col-span-3">Đối tác</div><div className="col-span-2">Tài khoản</div><div className="col-span-2">Số tiền</div><div className="col-span-2">Trạng thái</div><div className="col-span-1">Ngày</div>
          </div>
          <div className="divide-y divide-slate-50">
            {PAYOUTS.map((p,i)=>{
              const st = STATUS_CFG[p.status];
              return (
                <div key={p.id} className="grid grid-cols-2 md:grid-cols-12 gap-3 px-5 py-4 items-center hover:bg-slate-50 transition-all">
                  <div className="col-span-1 md:col-span-2 font-black text-slate-900 text-xs">{p.id}</div>
                  <div className="col-span-1 md:col-span-3 font-bold text-slate-700">{p.tenant}</div>
                  <div className="col-span-1 md:col-span-2 text-xs text-slate-500 font-bold">{p.bank}</div>
                  <div className="col-span-1 md:col-span-2 font-black text-slate-900">{p.amount}đ</div>
                  <div className="col-span-1 md:col-span-2"><span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${st.c}`}>{st.icon}{st.l}</span></div>
                  <div className="col-span-1 text-[10px] text-slate-400 font-bold">{p.date}</div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
