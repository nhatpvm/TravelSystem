import React, { useEffect, useState } from 'react';
import { Bell, Loader2, Mail, MessageSquare, Smartphone } from 'lucide-react';
import { listAdminOpsOutboxMessages } from '../../../services/adminOpsService';

const CHANNELS = [
  { label: 'Email', icon: <Mail size={16} /> },
  { label: 'SMS', icon: <Smartphone size={16} /> },
  { label: 'In-app', icon: <Bell size={16} /> },
];

function formatDate(value) {
  if (!value) {
    return '--';
  }

  return new Date(value).toLocaleString('vi-VN');
}

export default function AdminNotificationsPage() {
  const [tab, setTab] = useState('templates');
  const [messages, setMessages] = useState([]);
  const [summary, setSummary] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    async function load() {
      setLoading(true);
      setError('');

      try {
        const response = await listAdminOpsOutboxMessages({ pageSize: 8 });
        if (!active) {
          return;
        }

        setMessages(Array.isArray(response?.items) ? response.items : []);
        setSummary(response?.summary || {});
      } catch (err) {
        if (active) {
          setError(err?.message || 'Không tải được thông báo gần đây.');
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    load();
    return () => {
      active = false;
    };
  }, []);

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Thông báo & Templates</h1>
          <p className="text-slate-500 text-sm mt-1">Theo dõi thông báo in-app thật và trạng thái template/broadcast.</p>
        </div>
      </div>

      <div className="flex gap-1 bg-white rounded-2xl p-1 border border-slate-100 shadow-sm mb-6 w-fit">
        {[
          { value: 'templates', label: 'Trạng thái' },
          { value: 'broadcast', label: 'Gửi thủ công' },
        ].map((item) => (
          <button
            key={item.value}
            type="button"
            onClick={() => setTab(item.value)}
            className={`px-5 py-3 rounded-xl text-xs font-black uppercase tracking-widest transition-all ${tab === item.value ? 'bg-slate-900 text-white shadow-md' : 'text-slate-400 hover:text-slate-700'}`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === 'templates' ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-2xl p-8 shadow-sm border border-slate-100">
            <div className="w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mb-5">
              <MessageSquare size={24} />
            </div>
            <h2 className="text-lg font-black text-slate-900">Notification template chưa có API CRUD</h2>
            <p className="text-sm font-bold text-slate-400 mt-2">
              Phần template email/SMS chưa có entity và endpoint quản trị. Màn này chỉ hiển thị trạng thái thật và lịch sử
              thông báo in-app đã phát sinh.
            </p>
            <div className="grid grid-cols-3 gap-3 mt-6">
              <div className="rounded-2xl bg-blue-50 p-4">
                <p className="text-2xl font-black text-slate-900">{summary.inAppCount || 0}</p>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">In-app</p>
              </div>
              <div className="rounded-2xl bg-amber-50 p-4">
                <p className="text-2xl font-black text-slate-900">{summary.unreadCount || 0}</p>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chưa đọc</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-2xl font-black text-slate-900">{summary.readCount || 0}</p>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Đã đọc</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-2xl p-8 shadow-sm border border-slate-100">
            <h2 className="text-lg font-black text-slate-900 mb-5">Thông báo gần đây</h2>
            {loading ? (
              <div className="py-8 text-center">
                <Loader2 size={22} className="animate-spin text-blue-600 mx-auto mb-3" />
                <p className="text-sm font-bold text-slate-400">Đang tải...</p>
              </div>
            ) : error ? (
              <p className="text-sm font-bold text-rose-500">{error}</p>
            ) : messages.length === 0 ? (
              <p className="text-sm font-bold text-slate-400">Chưa có thông báo nào.</p>
            ) : (
              <div className="space-y-4">
                {messages.map((item) => (
                  <div key={item.id} className="rounded-2xl bg-slate-50 px-4 py-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-black text-slate-900">{item.title}</p>
                        <p className="text-xs font-bold text-slate-500 mt-1 line-clamp-2">{item.body}</p>
                      </div>
                      <span className="rounded-full bg-white px-3 py-1 text-[10px] font-black uppercase tracking-widest text-slate-400">{item.status}</span>
                    </div>
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-300 mt-2">{formatDate(item.createdAt)}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      ) : (
        <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 max-w-xl">
          <h2 className="font-black text-slate-900 text-lg mb-6">Gửi thông báo thủ công</h2>
          <div className="space-y-5">
            {[
              { label: 'Tiêu đề', placeholder: 'VD: Thông báo bảo trì hệ thống' },
              { label: 'Nội dung', placeholder: 'Nhập nội dung thông báo...', rows: 4 },
            ].map((field) => (
              <div key={field.label}>
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-1.5 block">{field.label}</label>
                {field.rows ? (
                  <textarea
                    rows={field.rows}
                    disabled
                    placeholder={field.placeholder}
                    className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-400 text-sm border-2 border-transparent outline-none resize-none cursor-not-allowed"
                  />
                ) : (
                  <input
                    type="text"
                    disabled
                    placeholder={field.placeholder}
                    className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-400 text-sm border-2 border-transparent outline-none cursor-not-allowed"
                  />
                )}
              </div>
            ))}
            <div>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-1.5 block">Kênh gửi</label>
              <div className="flex gap-3">
                {CHANNELS.map((channel) => (
                  <div key={channel.label} className="flex items-center gap-2 bg-slate-50 rounded-xl px-4 py-3 text-slate-400">
                    {channel.icon}
                    <span className="text-sm font-bold">{channel.label}</span>
                  </div>
                ))}
              </div>
            </div>
            <div className="rounded-2xl bg-amber-50 border border-amber-100 px-5 py-4 text-xs font-bold text-amber-700">
              Broadcast notification chưa có API gửi thật nên form đang khóa, không có nút gửi chết.
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
