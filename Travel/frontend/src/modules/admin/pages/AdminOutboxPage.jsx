import React, { useEffect, useMemo, useState } from 'react';
import { ActivitySquare, CheckCircle2, Clock, Loader2, Search, XCircle } from 'lucide-react';
import { listAdminOpsOutboxMessages } from '../../../services/adminOpsService';

function formatDate(value) {
  if (!value) {
    return '--';
  }

  return new Date(value).toLocaleString('vi-VN');
}

export default function AdminOutboxPage() {
  const [search, setSearch] = useState('');
  const [data, setData] = useState({ summary: {}, items: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const timer = setTimeout(async () => {
      setLoading(true);
      setError('');

      try {
        const response = await listAdminOpsOutboxMessages({ q: search, pageSize: 150 });
        setData({
          summary: response?.summary || {},
          items: Array.isArray(response?.items) ? response.items : [],
        });
      } catch (err) {
        setError(err?.message || 'Không tải được outbox message.');
      } finally {
        setLoading(false);
      }
    }, 250);

    return () => clearTimeout(timer);
  }, [search]);

  const stats = useMemo(() => ([
    { label: 'In-app đã tạo', value: data.summary?.inAppCount || 0, className: 'bg-emerald-50', icon: <CheckCircle2 size={16} className="text-emerald-600" /> },
    { label: 'Chưa đọc', value: data.summary?.unreadCount || 0, className: 'bg-amber-50', icon: <Clock size={16} className="text-amber-600" /> },
    { label: 'Đã đọc', value: data.summary?.readCount || 0, className: 'bg-slate-50', icon: <XCircle size={16} className="text-slate-500" /> },
  ]), [data.summary]);

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Outbox Messages</h1>
        <p className="text-slate-500 text-sm mt-1">Thông báo hệ thống đã phát sinh từ dữ liệu CustomerNotifications.</p>
      </div>

      <div className="grid grid-cols-3 gap-4 mb-5">
        {stats.map((item) => (
          <div key={item.label} className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.className}`}>
            <div className="flex items-center justify-between gap-3">
              <p className="text-3xl font-black text-slate-900">{item.value}</p>
              {item.icon}
            </div>
            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex gap-3">
        <div className="flex-1 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm tiêu đề, nội dung, người nhận..."
            className="bg-transparent py-3 flex-1 text-sm font-medium outline-none"
          />
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="px-8 py-12 text-center">
            <Loader2 size={24} className="animate-spin text-blue-600 mx-auto mb-3" />
            <p className="text-sm font-bold text-slate-400">Đang tải outbox...</p>
          </div>
        ) : error ? (
          <div className="px-8 py-12 text-center">
            <div className="w-14 h-14 rounded-2xl bg-rose-50 text-rose-500 flex items-center justify-center mx-auto mb-4">
              <ActivitySquare size={24} />
            </div>
            <h2 className="text-lg font-black text-slate-900">Không tải được outbox</h2>
            <p className="text-sm font-bold text-slate-400 mt-2">{error}</p>
          </div>
        ) : data.items.length === 0 ? (
          <div className="px-8 py-12 text-center">
            <div className="w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mx-auto mb-4">
              <ActivitySquare size={24} />
            </div>
            <h2 className="text-lg font-black text-slate-900">Chưa có thông báo phát sinh</h2>
            <p className="text-sm font-bold text-slate-400 mt-2">Outbox hiện đọc từ bảng CustomerNotifications, không còn dùng dữ liệu mẫu.</p>
          </div>
        ) : (
          <div className="divide-y divide-slate-50">
            {data.items.map((item) => (
              <div key={item.id} className="p-5 hover:bg-slate-50/60">
                <div className="flex flex-col lg:flex-row lg:items-start justify-between gap-4">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2 mb-2">
                      <span className="rounded-full bg-blue-50 px-3 py-1 text-[10px] font-black uppercase tracking-widest text-blue-700">{item.channel}</span>
                      <span className="rounded-full bg-slate-50 px-3 py-1 text-[10px] font-black uppercase tracking-widest text-slate-500">{item.status}</span>
                      <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">{item.category || 'general'}</span>
                    </div>
                    <h3 className="text-sm font-black text-slate-900">{item.title}</h3>
                    <p className="text-sm font-bold text-slate-500 mt-1 line-clamp-2">{item.body}</p>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      Người nhận: {item.recipientName} · Tenant: {item.tenantName || 'Toàn hệ thống'}
                    </p>
                  </div>
                  <div className="text-left lg:text-right shrink-0">
                    <p className="text-xs font-black text-slate-500">{formatDate(item.createdAt)}</p>
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-300 mt-2">{item.referenceType || item.source}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
