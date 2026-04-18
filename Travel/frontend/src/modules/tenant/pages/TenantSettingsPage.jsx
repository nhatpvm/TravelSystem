import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Settings, Building2, Globe, Bell, Shield, CreditCard, Save, ChevronRight, Link, AlertTriangle } from 'lucide-react';

const TABS = [
  { id: 'general',  icon: <Building2 size={16} />, label: 'Thông tin chung' },
  { id: 'booking',  icon: <Globe size={16} />,     label: 'Cài đặt đặt chỗ' },
  { id: 'notify',   icon: <Bell size={16} />,      label: 'Thông báo' },
  { id: 'security', icon: <Shield size={16} />,    label: 'Bảo mật' },
  { id: 'payment',  icon: <CreditCard size={16} />,label: 'Thanh toán' },
];

export default function TenantSettingsPage() {
  const [activeTab, setActiveTab] = useState('general');
  const [holdMinutes, setHoldMinutes] = useState(15);
  const [autoConfirm, setAutoConfirm] = useState(true);
  const [vatEnabled, setVatEnabled] = useState(false);
  const [saved, setSaved] = useState(false);

  const handleSave = () => {
    setSaved(true);
    setTimeout(() => setSaved(false), 2500);
  };

  return (
    <div>
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Cài đặt hệ thống</h1>
          <p className="text-slate-500 text-sm mt-1">Cấu hình tài khoản đối tác và các tuỳ chọn vận hành</p>
        </div>
        <button onClick={handleSave} className={`flex items-center gap-2 px-6 py-3 rounded-2xl font-bold text-sm shadow-lg transition-all ${saved ? 'bg-emerald-500 text-white' : 'bg-slate-900 text-white hover:bg-blue-600'}`}>
          <Save size={16} /> {saved ? 'Đã lưu!' : 'Lưu thay đổi'}
        </button>
      </div>

      <div className="flex flex-col lg:flex-row gap-6">
        {/* Sidebar tabs */}
        <div className="lg:w-56 shrink-0">
          <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-2 flex flex-row lg:flex-col gap-1 overflow-x-auto lg:overflow-visible">
            {TABS.map(tab => (
              <button key={tab.id} onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-bold whitespace-nowrap transition-all w-full text-left ${activeTab === tab.id ? 'bg-slate-900 text-white shadow-md' : 'text-slate-500 hover:bg-slate-50 hover:text-slate-800'}`}
              >
                <span className={activeTab === tab.id ? 'text-white' : 'text-slate-400'}>{tab.icon}</span>
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        {/* Panel Content */}
        <div className="flex-1">
          <motion.div key={activeTab} initial={{ opacity:0, x:8 }} animate={{ opacity:1, x:0 }} transition={{ duration: 0.25 }}>
            {activeTab === 'general' && (
              <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 space-y-6">
                <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest border-b border-slate-100 pb-4">Thông tin doanh nghiệp</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                  {[
                    { label: 'Tên doanh nghiệp', value: 'Hoàng Long Travel Co.' },
                    { label: 'Mã số thuế', value: '0123456789' },
                    { label: 'Địa chỉ', value: '123 Trần Duy Hưng, Hà Nội' },
                    { label: 'Website', value: 'https://hoanglong.vn' },
                    { label: 'Email liên hệ', value: 'contact@hoanglong.vn' },
                    { label: 'Hotline', value: '1800 1234' },
                  ].map((f, i) => (
                    <div key={i} className="space-y-2">
                      <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{f.label}</label>
                      <input defaultValue={f.value} className="w-full bg-slate-50 rounded-xl py-3 px-4 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-blue-200 focus:bg-white outline-none transition-all" />
                    </div>
                  ))}
                </div>

                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Mô tả doanh nghiệp</label>
                  <textarea rows={3} defaultValue="Hoàng Long là nhà vận chuyển xe khách uy tín hàng đầu tại Việt Nam với hơn 20 năm kinh nghiệm." className="w-full bg-slate-50 rounded-xl py-3 px-4 font-medium text-slate-900 text-sm border-2 border-transparent focus:border-blue-200 focus:bg-white outline-none transition-all resize-none" />
                </div>
              </div>
            )}

            {activeTab === 'booking' && (
              <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 space-y-6">
                <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest border-b border-slate-100 pb-4">Cài đặt đặt chỗ</h2>

                {/* Hold minutes */}
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] block mb-3">Thời gian giữ chỗ (Hold)</label>
                  <div className="flex items-center gap-4">
                    <input type="range" min={5} max={60} step={5} value={holdMinutes} onChange={e => setHoldMinutes(+e.target.value)}
                      className="flex-1 accent-blue-600" />
                    <div className="w-20 h-12 bg-slate-900 text-white rounded-xl flex items-center justify-center font-black text-xl">{holdMinutes}'</div>
                  </div>
                  <p className="text-xs text-slate-400 font-bold mt-2">Chỗ sẽ được {holdMinutes} phút trước khi tự động huỷ nếu không thanh toán</p>
                </div>

                {/* Toggles */}
                {[
                  { label: 'Tự động xác nhận đơn hàng', sub: 'Đơn hàng thanh toán thành công sẽ tự chuyển sang "Xác nhận"', value: autoConfirm, set: setAutoConfirm },
                  { label: 'Hỗ trợ xuất hoá đơn VAT', sub: 'Cho phép khách hàng yêu cầu hoá đơn VAT khi đặt chỗ', value: vatEnabled, set: setVatEnabled },
                ].map((t, i) => (
                  <div key={i} className="flex items-center justify-between p-5 bg-slate-50 rounded-2xl">
                    <div>
                      <p className="font-black text-slate-900 text-sm">{t.label}</p>
                      <p className="text-xs text-slate-500 mt-0.5">{t.sub}</p>
                    </div>
                    <button onClick={() => t.set(!t.value)}
                      className={`relative inline-flex w-12 h-6 rounded-full transition-all ${t.value ? 'bg-blue-600' : 'bg-slate-200'}`}
                    >
                      <span className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${t.value ? 'translate-x-6' : ''}`} />
                    </button>
                  </div>
                ))}

                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Chính sách huỷ vé mặc định</label>
                  <select className="w-full bg-slate-50 rounded-xl py-3 px-4 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-blue-200 outline-none cursor-pointer">
                    <option>Hoàn 100% nếu huỷ trước 48 giờ</option>
                    <option>Hoàn 50% nếu huỷ trước 24 giờ</option>
                    <option>Không hoàn tiền</option>
                  </select>
                </div>
              </div>
            )}

            {activeTab === 'notify' && (
              <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 space-y-4">
                <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest border-b border-slate-100 pb-4">Cài đặt thông báo</h2>
                {[
                  { label: 'Đơn đặt mới', sub: 'Thông báo khi có đặt chỗ thành công' },
                  { label: 'Thanh toán thành công', sub: 'Nhận xác nhận khi thanh toán được xử lý' },
                  { label: 'Yêu cầu huỷ vé', sub: 'Cảnh báo khi khách yêu cầu huỷ' },
                  { label: 'Báo cáo hàng tuần', sub: 'Tổng kết doanh thu mỗi thứ Hai' },
                  { label: 'Cảnh báo tồn kho thấp', sub: 'Nhắc khi còn ≤ 5 chỗ / phòng / suất' },
                ].map((n, i) => (
                  <div key={i} className="flex items-center justify-between p-5 bg-slate-50 rounded-2xl">
                    <div>
                      <p className="font-black text-slate-900 text-sm">{n.label}</p>
                      <p className="text-xs text-slate-500 mt-0.5">{n.sub}</p>
                    </div>
                    <button className="relative inline-flex w-12 h-6 rounded-full bg-blue-600 shrink-0">
                      <span className="absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow translate-x-6" />
                    </button>
                  </div>
                ))}
              </div>
            )}

            {activeTab === 'security' && (
              <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 space-y-6">
                <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest border-b border-slate-100 pb-4">Bảo mật tài khoản</h2>
                {[
                  { label: 'Đổi mật khẩu đăng nhập', action: 'Đổi mật khẩu', link: true },
                  { label: 'Xác thực 2 lớp (2FA)', action: 'Bật 2FA', link: true },
                  { label: 'Phiên đăng nhập đang hoạt động', action: 'Xem', link: true },
                ].map((s, i) => (
                  <div key={i} className="flex items-center justify-between p-5 bg-slate-50 rounded-2xl hover:bg-slate-100 transition-all cursor-pointer">
                    <div>
                      <p className="font-black text-slate-900 text-sm">{s.label}</p>
                    </div>
                    <button className="flex items-center gap-1.5 text-blue-600 font-black text-xs uppercase">
                      {s.action} <ChevronRight size={14} />
                    </button>
                  </div>
                ))}

                <div className="p-5 bg-rose-50 rounded-2xl border border-rose-100">
                  <div className="flex items-center gap-2 mb-2">
                    <AlertTriangle size={16} className="text-rose-500" />
                    <p className="font-black text-rose-700 text-sm">Vùng nguy hiểm</p>
                  </div>
                  <p className="text-xs text-rose-600 font-medium mb-4">Xoá tài khoản đối tác sẽ xoá toàn bộ dữ liệu, lịch sử đặt chỗ và không thể phục hồi.</p>
                  <button className="px-5 py-2.5 bg-rose-500 text-white rounded-xl text-[11px] font-black uppercase tracking-widest hover:bg-rose-600 transition-all">
                    Yêu cầu xoá tài khoản
                  </button>
                </div>
              </div>
            )}

            {activeTab === 'payment' && (
              <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 space-y-6">
                <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest border-b border-slate-100 pb-4">Thông tin thanh toán</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                  {[
                    { label: 'Ngân hàng', value: 'Vietcombank (VCB)' },
                    { label: 'Số tài khoản', value: '0123 4567 8901' },
                    { label: 'Chủ tài khoản', value: 'HOANG LONG TRAVEL CO LTD' },
                    { label: 'Chi nhánh', value: 'Hà Nội' },
                  ].map((f, i) => (
                    <div key={i} className="space-y-2">
                      <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{f.label}</label>
                      <input defaultValue={f.value} className="w-full bg-slate-50 rounded-xl py-3 px-4 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-blue-200 focus:bg-white outline-none transition-all" />
                    </div>
                  ))}
                </div>

                <div className="p-5 bg-blue-50 rounded-2xl">
                  <p className="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-2">API Key tích hợp</p>
                  <div className="flex items-center gap-3">
                    <code className="flex-1 font-mono text-xs text-blue-800 bg-blue-100 px-3 py-2 rounded-xl font-bold truncate">sk_live_hl_••••••••••••••••••••••••</code>
                    <button className="px-4 py-2 bg-blue-600 text-white rounded-xl text-[10px] font-black uppercase tracking-widest hover:bg-blue-700 transition-all">Tạo mới</button>
                  </div>
                </div>
              </div>
            )}
          </motion.div>
        </div>
      </div>
    </div>
  );
}
