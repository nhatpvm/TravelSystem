import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { MessageSquare, Search, ChevronDown, ChevronUp, Clock, CheckCircle2, AlertCircle, User, Tag, Filter, Send } from 'lucide-react';

const TICKETS = [
  { id: 'SP-20240601-001', subject: 'Chưa nhận vé sau thanh toán', customer: 'Nguyễn Văn A', category: 'Vé', priority: 'high', status: 'open', tenant: 'Hoàng Long Bus', date: '01/06/2024 09:12', assignee: null, messages: [
    { from: 'customer', text: 'Tôi đã thanh toán nhưng chưa nhận được vé. Mã đơn: BK-001.', time: '09:12' },
  ]},
  { id: 'SP-20240531-002', subject: 'Yêu cầu hoàn tiền tour', customer: 'Trần Thị B', category: 'Hoàn tiền', priority: 'medium', status: 'in_progress', tenant: 'Sun Travel', date: '31/05/2024 14:30', assignee: 'Nguyễn CS', messages: [
    { from: 'customer', text: 'Tôi muốn huỷ tour và hoàn tiền. Lý do sức khoẻ.', time: '14:30' },
    { from: 'agent', text: 'Chào bạn, chúng tôi đang xem xét yêu cầu. Vui lòng chờ trong 1–2 ngày.', time: '15:00' },
  ]},
  { id: 'SP-20240530-003', subject: 'Lỗi thanh toán QR SePay', customer: 'Lê Minh C', category: 'Thanh toán', priority: 'high', status: 'resolved', tenant: 'VN Airlines Tenant', date: '30/05/2024 08:05', assignee: 'Admin', messages: [
    { from: 'customer', text: 'QR code hết hạn nhưng tiền đã bị trừ.', time: '08:05' },
    { from: 'agent', text: 'Đã kiểm tra. Giao dịch được hoàn 100% trong 2–5 ngày.', time: '09:30' },
  ]},
];

const PRIORITY_CFG = { high: { l:'Cao', c:'bg-rose-100 text-rose-700' }, medium: { l:'TB', c:'bg-amber-100 text-amber-700' }, low: { l:'Thấp', c:'bg-slate-100 text-slate-600' } };
const STATUS_CFG = {
  open:        { l:'Mới',         c:'bg-blue-100 text-blue-700' },
  in_progress: { l:'Đang xử lý',  c:'bg-amber-100 text-amber-700' },
  resolved:    { l:'Đã giải quyết',c:'bg-emerald-100 text-emerald-700' },
};

export default function AdminSupportPage() {
  const [tickets, setTickets] = useState(TICKETS);
  const [selected, setSelected] = useState(TICKETS[0]);
  const [reply, setReply] = useState('');
  const [search, setSearch] = useState('');
  const [statusF, setStatusF] = useState('all');

  const filtered = tickets.filter(t => {
    const m = t.subject.toLowerCase().includes(search.toLowerCase()) || t.customer.toLowerCase().includes(search.toLowerCase());
    const s = statusF === 'all' || t.status === statusF;
    return m && s;
  });

  const sendReply = () => {
    if (!reply.trim()) return;
    setTickets(prev => prev.map(t => t.id === selected.id ? {
      ...t, status: 'in_progress',
      messages: [...t.messages, { from: 'agent', text: reply, time: new Date().toLocaleTimeString('vi', { hour:'2-digit', minute:'2-digit' }) }]
    } : t));
    setSelected(prev => ({ ...prev, status: 'in_progress', messages: [...prev.messages, { from: 'agent', text: reply, time: new Date().toLocaleTimeString('vi', { hour:'2-digit', minute:'2-digit' }) }] }));
    setReply('');
  };

  const resolve = (id) => {
    setTickets(prev => prev.map(t => t.id === id ? { ...t, status: 'resolved' } : t));
    if (selected?.id === id) setSelected(prev => ({ ...prev, status: 'resolved' }));
  };

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Support Tickets</h1>
        <p className="text-slate-500 text-sm mt-1">Phân loại, phân công và xử lý yêu cầu hỗ trợ</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { l:'Mới',         v: tickets.filter(t=>t.status==='open').length,        c:'bg-blue-50' },
          { l:'Đang xử lý',  v: tickets.filter(t=>t.status==='in_progress').length, c:'bg-amber-50' },
          { l:'Đã giải quyết',v:tickets.filter(t=>t.status==='resolved').length,   c:'bg-emerald-50' },
          { l:'Ưu tiên cao',  v: tickets.filter(t=>t.priority==='high').length,     c:'bg-rose-50' },
        ].map((s,i) => (
          <div key={i} className={`rounded-2xl p-4 shadow-sm border border-slate-100 ${s.c}`}>
            <p className="text-3xl font-black text-slate-900">{s.v}</p>
            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mt-1">{s.l}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        {/* Ticket list */}
        <div className="lg:col-span-2 space-y-3">
          <div className="bg-white rounded-2xl p-3 border border-slate-100 flex gap-2">
            <div className="flex-1 flex items-center gap-2 bg-slate-50 rounded-xl px-3">
              <Search size={14} className="text-slate-400" />
              <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Tìm ticket…" className="bg-transparent py-2.5 flex-1 text-xs font-medium outline-none" />
            </div>
            <select value={statusF} onChange={e => setStatusF(e.target.value)} className="bg-slate-50 rounded-xl px-3 py-2.5 text-xs font-black text-slate-600 border-none outline-none cursor-pointer">
              <option value="all">Tất cả</option>
              <option value="open">Mới</option>
              <option value="in_progress">Đang xử lý</option>
              <option value="resolved">Đã xong</option>
            </select>
          </div>
          {filtered.map(t => {
            const st = STATUS_CFG[t.status];
            const pr = PRIORITY_CFG[t.priority];
            return (
              <div key={t.id} onClick={() => setSelected(t)}
                className={`bg-white rounded-2xl p-4 shadow-sm border cursor-pointer hover:shadow-md transition-all ${selected?.id === t.id ? 'border-[#1EB4D4]' : 'border-slate-100'}`}
              >
                <div className="flex items-start gap-2 mb-2">
                  <p className="font-black text-slate-900 text-sm flex-1 leading-tight">{t.subject}</p>
                  <span className={`px-2 py-0.5 rounded-lg text-[9px] font-black uppercase shrink-0 ${pr.c}`}>{pr.l}</span>
                </div>
                <p className="text-xs text-slate-500 font-bold">{t.customer} · {t.category}</p>
                <div className="flex items-center justify-between mt-2">
                  <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-lg text-[9px] font-black uppercase ${st.c}`}>{st.l}</span>
                  <span className="text-[10px] text-slate-400 font-bold">{t.date.split(' ')[0]}</span>
                </div>
              </div>
            );
          })}
        </div>

        {/* Conversation panel */}
        <div className="lg:col-span-3">
          {selected ? (
            <div className="bg-white rounded-2xl shadow-sm border border-slate-100 flex flex-col h-full min-h-[500px]">
              <div className="p-5 border-b border-slate-100">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-black text-slate-900">{selected.subject}</p>
                    <p className="text-xs text-slate-400 font-bold mt-0.5">{selected.id} · {selected.customer} · {selected.tenant}</p>
                  </div>
                  <div className="flex gap-2 shrink-0">
                    {selected.status !== 'resolved' && (
                      <button onClick={() => resolve(selected.id)} className="px-3 py-1.5 bg-emerald-50 text-emerald-700 rounded-xl text-[10px] font-black uppercase hover:bg-emerald-100 transition-all">✓ Giải quyết</button>
                    )}
                  </div>
                </div>
              </div>
              <div className="flex-1 p-5 space-y-4 overflow-y-auto">
                {selected.messages.map((m, i) => (
                  <div key={i} className={`flex ${m.from === 'agent' ? 'justify-end' : 'justify-start'}`}>
                    <div className={`max-w-[80%] rounded-2xl px-4 py-3 ${m.from === 'agent' ? 'bg-[#1EB4D4] text-white' : 'bg-slate-50 text-slate-900'}`}>
                      <p className="text-sm font-medium">{m.text}</p>
                      <p className={`text-[10px] font-bold mt-1 ${m.from === 'agent' ? 'text-white/60' : 'text-slate-400'}`}>{m.from === 'agent' ? 'CS Team' : selected.customer} · {m.time}</p>
                    </div>
                  </div>
                ))}
              </div>
              <div className="p-4 border-t border-slate-100 flex gap-3">
                <textarea value={reply} onChange={e => setReply(e.target.value)} rows={2} placeholder="Nhập phản hồi…" className="flex-1 bg-slate-50 rounded-2xl px-4 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30 resize-none" />
                <button onClick={sendReply} className="px-4 py-3 bg-[#1EB4D4] text-white rounded-2xl hover:bg-[#002B7F] transition-all flex items-center justify-center shrink-0"><Send size={16} /></button>
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100 h-full flex items-center justify-center">
              <div><MessageSquare size={36} className="mx-auto mb-3 opacity-30" /><p className="font-bold">Chọn ticket để xem hội thoại</p></div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
