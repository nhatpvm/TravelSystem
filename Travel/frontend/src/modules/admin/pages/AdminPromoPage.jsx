import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Tag, Plus, Edit2, Trash2, Filter, BarChart3, Search } from 'lucide-react';

const PROMOS = [
  { code: 'SUMMER2024', type: 'percent', discount: 15, min_order: 500000, max_discount: 200000, used: 142, limit: 500, expires: '31/08/2024', services: ['Tất cả'], status: 'active', segment: 'Tất cả', roi: '3.2x' },
  { code: 'NEWUSER50', type: 'fixed', discount: 50000, min_order: 200000, max_discount: 50000, used: 891, limit: 1000, expires: '31/12/2024', services: ['Vé xe', 'Vé tàu'], status: 'active', segment: 'Người mới', roi: '5.8x' },
  { code: 'LOYALTY10', type: 'percent', discount: 10, min_order: 0, max_discount: 100000, used: 25, limit: 100, expires: '30/06/2024', services: ['Tất cả'], status: 'active', segment: 'VIP', roi: '2.1x' },
];

export default function AdminPromoPage() {
  const [promos, setPromos] = useState(PROMOS);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: '', type: 'percent', discount: '', min_order: '', max_discount: '', limit: '', expires: '', segment: 'Tất cả' });

  const totalUsage = promos.reduce((s, p) => s + p.used, 0);
  const totalLimit = promos.reduce((s, p) => s + p.limit, 0);

  const handleSave = (e) => {
    e.preventDefault();
    setPromos(prev => [...prev, { 
      ...form, 
      services: ['Tất cả'], 
      status: 'active', 
      used: 0, 
      discount: +form.discount, 
      min_order: +form.min_order, 
      max_discount: +form.max_discount, 
      limit: +form.limit,
      roi: '0.0x'
    }]);
    setShowForm(false);
    setForm({ code: '', type: 'percent', discount: '', min_order: '', max_discount: '', limit: '', expires: '', segment: 'Tất cả' });
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Marketing & Khuyến mãi</h1>
          <p className="text-slate-500 text-sm mt-1">Đẩy mạnh doanh thu qua các chiến dịch ưu đãi mục tiêu</p>
        </div>
        <button onClick={() => setShowForm(!showForm)} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
          <Plus size={16} /> Tạo chiến dịch
        </button>
      </div>

      {showForm && (
        <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} className="bg-white rounded-2xl p-8 shadow-xl mb-6 border border-[#1EB4D4]/20">
          <h2 className="font-black text-slate-900 mb-6">Tạo chiến dịch mới</h2>
          <form onSubmit={handleSave} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {[
              { l: 'Mã code', k: 'code', placeholder: 'VD: SUMMER2024', type: 'text' },
              { l: 'Giá trị giảm', k: 'discount', placeholder: '15 (%) hoặc 50000 (đ)', type: 'number' },
              { l: 'Ngày hết hạn', k: 'expires', placeholder: '31/12/2024', type: 'text' },
              { l: 'Giới hạn số lượt', k: 'limit', placeholder: '500', type: 'number' },
              { l: 'Đối tượng khách hàng', k: 'segment', placeholder: 'Tất cả', type: 'text' },
            ].map(f => (
              <div key={f.k}>
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest ml-1 mb-1.5 block">{f.l}</label>
                <input required type={f.type} value={form[f.k]} onChange={e => setForm({ ...form, [f.k]: e.target.value })}
                  placeholder={f.placeholder} className="w-full bg-slate-50 rounded-2xl py-3 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" />
              </div>
            ))}
            <div className="md:col-span-3 flex gap-3">
              <button type="submit" className="px-8 py-3 bg-slate-900 text-white rounded-2xl font-black text-sm uppercase tracking-widest shadow-lg hover:bg-blue-600 transition-all">Lưu chiến dịch</button>
              <button type="button" onClick={() => setShowForm(false)} className="px-6 py-3 bg-slate-100 text-slate-600 rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-slate-200 transition-all">Hủy</button>
            </div>
          </form>
        </motion.div>
      )}

      {/* Campaign Highlights */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
         {promos.slice(0, 3).map((p, i) => (
            <div key={i} className="bg-white p-6 rounded-3xl border border-slate-100 shadow-sm flex items-center justify-between group hover:border-[#1EB4D4] transition-all cursor-pointer">
               <div>
                  <p className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-widest mb-1">{p.segment}</p>
                  <h4 className="font-black text-slate-900">{p.code}</h4>
                  <p className="text-2xl font-black text-blue-600 mt-2">{p.roi}</p>
                  <p className="text-[9px] font-bold text-slate-400 uppercase tracking-tighter">Doanh thu / Chi phí (ROI)</p>
               </div>
               <div className="w-12 h-12 bg-slate-50 rounded-2xl flex items-center justify-center text-slate-300 group-hover:text-[#1EB4D4] transition-all">
                  <BarChart3 size={24} />
               </div>
            </div>
         ))}
      </div>

      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
        <div className="p-6 border-b border-slate-50 bg-slate-50/50 flex items-center justify-between">
            <h3 className="font-black text-slate-900 text-sm">Danh sách Chiến dịch</h3>
            <div className="flex gap-2">
               <button className="p-2 bg-white rounded-xl border border-slate-100 text-slate-400"><Filter size={16} /></button>
            </div>
        </div>
        
        <div className="divide-y divide-slate-50">
           {promos.map((p, idx) => (
              <div key={idx} className="p-6 hover:bg-slate-50/50 transition-all flex flex-col md:flex-row md:items-center justify-between gap-6">
                 <div className="flex items-center gap-5">
                    <div className="w-14 h-14 bg-blue-50 text-blue-600 rounded-2xl flex items-center justify-center font-black italic">
                       {p.type === 'percent' ? '%' : 'VNĐ'}
                    </div>
                    <div>
                       <div className="flex items-center gap-2 mb-1">
                          <span className="font-black text-slate-900 text-lg">{p.code}</span>
                          <span className="px-2 py-0.5 bg-emerald-100 text-emerald-700 rounded-lg text-[9px] font-black uppercase tracking-widest">{p.status}</span>
                       </div>
                       <p className="text-xs text-slate-400 font-bold">
                          Đối tượng: <span className="text-slate-900">{p.segment}</span> · 
                          Hạn: <span className="text-slate-900">{p.expires}</span>
                       </p>
                    </div>
                 </div>

                 <div className="flex-1 max-w-xs">
                    <div className="flex justify-between text-[10px] font-black uppercase tracking-widest text-slate-400 mb-1.5">
                       <span>Dùng: {p.used}/{p.limit}</span>
                       <span>{p.limit > 0 ? Math.round((p.used/p.limit)*100) : 0}%</span>
                    </div>
                    <div className="w-full h-1.5 bg-slate-100 rounded-full overflow-hidden">
                       <div className="bg-[#1EB4D4] h-full rounded-full" style={{ width: `${p.limit > 0 ? (p.used/p.limit)*100 : 0}%` }}></div>
                    </div>
                 </div>

                 <div className="flex items-center gap-8">
                    <div className="text-right">
                       <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Hiệu quả</p>
                       <p className="font-black text-emerald-600">+{p.roi}</p>
                    </div>
                    <button className="w-10 h-10 bg-slate-100 text-slate-400 rounded-xl flex items-center justify-center hover:bg-slate-900 hover:text-white transition-all">
                       <Edit2 size={16} />
                    </button>
                 </div>
              </div>
           ))}
        </div>
      </div>
    </div>
  );
}
