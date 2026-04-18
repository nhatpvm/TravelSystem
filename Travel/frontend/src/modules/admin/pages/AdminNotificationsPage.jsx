import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Bell, Plus, Edit2, Send, Mail, MessageSquare, Smartphone } from 'lucide-react';

const TEMPLATES = [
  { id:'TPL-001', name:'Xác nhận đặt chỗ', channel:'email', event:'BOOKING_CONFIRMED', vars:['{{customer_name}}','{{booking_id}}','{{service}}'], status:'active' },
  { id:'TPL-002', name:'Thanh toán thành công', channel:'email+sms', event:'PAYMENT_SUCCESS', vars:['{{customer_name}}','{{amount}}','{{ref}}'], status:'active' },
  { id:'TPL-003', name:'OTP xác minh', channel:'sms', event:'AUTH_OTP', vars:['{{otp_code}}','{{expire_minutes}}'], status:'active' },
  { id:'TPL-004', name:'Nhắc nhở chuyến đi', channel:'email', event:'TRIP_REMINDER', vars:['{{customer_name}}','{{trip_date}}','{{ticket_code}}'], status:'active' },
  { id:'TPL-005', name:'Hoàn tiền thành công', channel:'email', event:'REFUND_COMPLETED', vars:['{{customer_name}}','{{refund_amount}}','{{days}}'], status:'draft' },
];

const CHANNEL_CFG = { email:{ l:'Email', icon:<Mail size={14}/>, c:'bg-blue-100 text-blue-700' }, sms:{ l:'SMS', icon:<Smartphone size={14}/>, c:'bg-green-100 text-green-700' }, 'email+sms':{ l:'Email + SMS', icon:<Bell size={14}/>, c:'bg-purple-100 text-purple-700' } };
const STATUS_CFG = { active:{ l:'Kích hoạt', c:'bg-emerald-100 text-emerald-700' }, draft:{ l:'Nháp', c:'bg-slate-100 text-slate-600' } };

export default function AdminNotificationsPage() {
  const [tab, setTab] = useState('templates');
  const [testEvent, setTestEvent] = useState('');

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Thông báo & Templates</h1>
          <p className="text-slate-500 text-sm mt-1">Quản lý mẫu email/SMS và gửi thông báo thủ công</p>
        </div>
        <button className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg"><Plus size={16}/>Tạo template</button>
      </div>

      <div className="flex gap-1 bg-white rounded-2xl p-1 border border-slate-100 shadow-sm mb-6 w-fit">
        {[{v:'templates',l:'Mẫu thông báo'},{v:'broadcast',l:'Gửi thủ công'}].map(t=>(
          <button key={t.v} onClick={()=>setTab(t.v)}
            className={`px-5 py-3 rounded-xl text-xs font-black uppercase tracking-widest transition-all ${tab===t.v?'bg-slate-900 text-white shadow-md':'text-slate-400 hover:text-slate-700'}`}>{t.l}</button>
        ))}
      </div>

      {tab === 'templates' && (
        <div className="space-y-3">
          {TEMPLATES.map((t,i)=>{
            const ch = CHANNEL_CFG[t.channel];
            const st = STATUS_CFG[t.status];
            return (
              <motion.div key={t.id} initial={{opacity:0,y:8}} animate={{opacity:1,y:0}} transition={{delay:i*0.05}}
                className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 flex items-center gap-4 hover:shadow-md transition-all group"
              >
                <div className="w-10 h-10 bg-slate-100 rounded-xl flex items-center justify-center shrink-0">{ch.icon}</div>
                <div className="flex-1">
                  <div className="flex items-center gap-2 flex-wrap mb-0.5">
                    <p className="font-black text-slate-900">{t.name}</p>
                    <span className={`px-2 py-0.5 rounded-lg text-[10px] font-black uppercase ${st.c}`}>{st.l}</span>
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-lg text-[10px] font-black uppercase ${ch.c}`}>{ch.l}</span>
                  </div>
                  <p className="text-xs text-slate-400 font-bold">Event: {t.event}</p>
                  <div className="flex flex-wrap gap-1.5 mt-1.5">
                    {t.vars.map(v=><span key={v} className="px-2 py-0.5 bg-slate-50 rounded-lg text-[10px] font-bold text-slate-500 font-mono">{v}</span>)}
                  </div>
                </div>
                <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-all">
                  <button className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl"><Edit2 size={14}/></button>
                  <button className="p-2 text-slate-400 hover:text-green-600 hover:bg-green-50 rounded-xl"><Send size={14}/></button>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}

      {tab === 'broadcast' && (
        <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 max-w-xl">
          <h2 className="font-black text-slate-900 text-lg mb-6">Gửi thông báo thủ công</h2>
          <div className="space-y-5">
            {[
              {l:'Tiêu đề', p:'VD: Thông báo bảo trì hệ thống'},
              {l:'Nội dung', p:'Nhập nội dung thông báo…', rows:4},
            ].map(f=>(
              <div key={f.l}>
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-1.5 block">{f.l}</label>
                {f.rows ? (
                  <textarea rows={f.rows} placeholder={f.p} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all resize-none" />
                ) : (
                  <input type="text" placeholder={f.p} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" />
                )}
              </div>
            ))}
            <div>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-1.5 block">Đối tượng</label>
              <select className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent outline-none cursor-pointer">
                <option>Tất cả người dùng</option>
                <option>Tenant cụ thể</option>
                <option>Customer đã đăng nhập</option>
              </select>
            </div>
            <div>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-1.5 block">Kênh gửi</label>
              <div className="flex gap-3">
                {[{l:'Email', icon:<Mail size={16}/>},{l:'SMS', icon:<Smartphone size={16}/>},{l:'In-app', icon:<Bell size={16}/>}].map(c=>(
                  <label key={c.l} className="flex items-center gap-2 cursor-pointer bg-slate-50 rounded-xl px-4 py-3 hover:bg-slate-100 transition-all">
                    <input type="checkbox" className="accent-[#1EB4D4]" />
                    {c.icon}<span className="text-sm font-bold text-slate-700">{c.l}</span>
                  </label>
                ))}
              </div>
            </div>
            <button className="w-full py-5 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl hover:-translate-y-0.5 transition-all flex items-center justify-center gap-2"><Send size={16}/>Gửi ngay</button>
          </div>
        </div>
      )}
    </div>
  );
}
