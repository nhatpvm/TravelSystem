import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { FileText, Search, Filter, User, Clock } from 'lucide-react';

const LOGS = [
  { id:1, actor:'admin@turmet.vn', action:'TENANT_LOCK',    entity:'Tenant',  entity_id:'TNT-005', detail:'Khoá tenant Hoàng Long Bus',           ip:'14.232.1.1',  date:'01/06/2024 14:52' },
  { id:2, actor:'admin@turmet.vn', action:'USER_RESET_PWD', entity:'User',    entity_id:'USR-021', detail:'Đặt lại mật khẩu cho user@gmail.com',  ip:'14.232.1.1',  date:'01/06/2024 11:30' },
  { id:3, actor:'cs@turmet.vn',    action:'BOOKING_CANCEL', entity:'Booking', entity_id:'BK-0012', detail:'CS huỷ booking theo yêu cầu khách',    ip:'203.160.5.22', date:'31/05/2024 09:15' },
  { id:4, actor:'admin@turmet.vn', action:'PROMO_CREATE',   entity:'Promo',   entity_id:'SUMMER24',detail:'Tạo mã SUMMER2024 giảm 15%',           ip:'14.232.1.1',  date:'30/05/2024 16:00' },
  { id:5, actor:'system',          action:'REFUND_AUTO',    entity:'Refund',  entity_id:'RF-0009', detail:'Hoàn tiền tự động sau 48h timeout',    ip:'–',           date:'29/05/2024 03:00' },
  { id:6, actor:'ops@suntravel.vn',action:'TRIP_UPDATE',    entity:'Trip',    entity_id:'TR-0045', detail:'Cập nhật giờ khởi hành SE1 HN→HUE',   ip:'42.118.9.5',  date:'28/05/2024 08:20' },
];

const ACTION_COLOR = { TENANT_LOCK:'bg-rose-100 text-rose-700', USER_RESET_PWD:'bg-amber-100 text-amber-700', BOOKING_CANCEL:'bg-rose-100 text-rose-700', PROMO_CREATE:'bg-emerald-100 text-emerald-700', REFUND_AUTO:'bg-blue-100 text-blue-700', TRIP_UPDATE:'bg-slate-100 text-slate-600' };

export default function AdminAuditPage() {
  const [search, setSearch] = useState('');
  const filtered = LOGS.filter(l => l.actor.toLowerCase().includes(search.toLowerCase()) || l.action.toLowerCase().includes(search.toLowerCase()) || l.detail.toLowerCase().includes(search.toLowerCase()));

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Audit Logs</h1>
        <p className="text-slate-500 text-sm mt-1">Lịch sử toàn bộ thao tác hệ thống</p>
      </div>
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex gap-3">
        <div className="flex-1 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Tìm actor, action, chi tiết…" className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
      </div>
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="divide-y divide-slate-50">
          {filtered.map((log, i) => (
            <motion.div key={log.id} initial={{opacity:0}} animate={{opacity:1}} transition={{delay:i*0.04}}
              className="flex items-center gap-4 px-5 py-4 hover:bg-slate-50 transition-all"
            >
              <div className="w-8 h-8 bg-slate-100 rounded-xl flex items-center justify-center shrink-0">
                {log.actor === 'system' ? <FileText size={14} className="text-slate-500"/> : <User size={14} className="text-slate-500"/>}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap mb-0.5">
                  <span className={`px-2 py-0.5 rounded-lg text-[10px] font-black uppercase ${ACTION_COLOR[log.action]||'bg-slate-100 text-slate-600'}`}>{log.action}</span>
                  <span className="text-xs font-bold text-slate-600">{log.entity} <span className="text-slate-400 font-bold">#{log.entity_id}</span></span>
                </div>
                <p className="text-sm font-medium text-slate-700 truncate">{log.detail}</p>
                <p className="text-[10px] text-slate-400 font-bold mt-0.5">{log.actor} · {log.ip}</p>
              </div>
              <div className="text-right shrink-0">
                <p className="text-[10px] text-slate-400 font-bold flex items-center gap-1 justify-end"><Clock size={10}/>{log.date}</p>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  );
}
