import React, { useEffect, useMemo, useState } from 'react';
import { FileText, Loader2, Search, ShieldAlert } from 'lucide-react';
import { listAdminOpsAuditEvents } from '../../../services/adminOpsService';

const severityClass = {
  Success: 'bg-emerald-50 text-emerald-700',
  Warning: 'bg-amber-50 text-amber-700',
  Error: 'bg-rose-50 text-rose-700',
  Info: 'bg-slate-50 text-slate-600',
};

function formatDate(value) {
  if (!value) {
    return '--';
  }

  return new Date(value).toLocaleString('vi-VN');
}

export default function AdminAuditPage() {
  const [search, setSearch] = useState('');
  const [data, setData] = useState({ summary: {}, items: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const timer = setTimeout(async () => {
      setLoading(true);
      setError('');

      try {
        const response = await listAdminOpsAuditEvents({ q: search, pageSize: 150 });
        setData({
          summary: response?.summary || {},
          items: Array.isArray(response?.items) ? response.items : [],
        });
      } catch (err) {
        setError(err?.message || 'Không tải được audit log.');
      } finally {
        setLoading(false);
      }
    }, 250);

    return () => clearTimeout(timer);
  }, [search]);

  const stats = useMemo(() => ([
    { label: 'Tổng sự kiện', value: data.summary?.totalCount || 0 },
    { label: 'Booking/payment/refund', value: (data.summary?.bookingEvents || 0) + (data.summary?.paymentEvents || 0) + (data.summary?.refundEvents || 0) },
    { label: 'Tenant/onboarding', value: (data.summary?.tenantEvents || 0) + (data.summary?.onboardingEvents || 0) },
    { label: 'Support/outbox/promo', value: (data.summary?.supportEvents || 0) + (data.summary?.notificationEvents || 0) + (data.summary?.promoEvents || 0) },
  ]), [data.summary]);

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Audit Logs</h1>
        <p className="text-slate-500 text-sm mt-1">Lịch sử thao tác và sự kiện quan trọng toàn hệ thống.</p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-5">
        {stats.map((item) => (
          <div key={item.label} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
            <p className="text-2xl font-black text-slate-900">{item.value}</p>
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
            placeholder="Tìm actor, action, entity, tenant..."
            className="bg-transparent py-3 flex-1 text-sm font-medium outline-none"
          />
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="px-8 py-12 text-center">
            <Loader2 size={24} className="animate-spin text-blue-600 mx-auto mb-3" />
            <p className="text-sm font-bold text-slate-400">Đang tải audit log...</p>
          </div>
        ) : error ? (
          <div className="px-8 py-12 text-center">
            <div className="w-14 h-14 rounded-2xl bg-rose-50 text-rose-500 flex items-center justify-center mx-auto mb-4">
              <ShieldAlert size={24} />
            </div>
            <h2 className="text-lg font-black text-slate-900">Không tải được audit log</h2>
            <p className="text-sm font-bold text-slate-400 mt-2">{error}</p>
          </div>
        ) : data.items.length === 0 ? (
          <div className="px-8 py-12 text-center">
            <div className="w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mx-auto mb-4">
              <FileText size={24} />
            </div>
            <h2 className="text-lg font-black text-slate-900">Chưa có sự kiện phù hợp</h2>
            <p className="text-sm font-bold text-slate-400 mt-2">Thử đổi từ khóa hoặc kiểm tra dữ liệu demo đã seed.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50/60">
                  <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Thời gian</th>
                  <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Actor</th>
                  <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Action</th>
                  <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Entity</th>
                  <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Tenant</th>
                  <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Nguồn</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {data.items.map((item, index) => (
                  <tr key={`${item.id}-${index}`} className="hover:bg-slate-50/60">
                    <td className="px-5 py-4 text-xs font-bold text-slate-500 whitespace-nowrap">{formatDate(item.occurredAt)}</td>
                    <td className="px-5 py-4 text-sm font-black text-slate-800">{item.actorName || 'System'}</td>
                    <td className="px-5 py-4">
                      <div className="flex flex-col gap-2">
                        <span className={`w-fit rounded-full px-3 py-1 text-[10px] font-black uppercase tracking-widest ${severityClass[item.severity] || severityClass.Info}`}>
                          {item.action}
                        </span>
                        <span className="text-xs font-bold text-slate-400 max-w-md">{item.description}</span>
                      </div>
                    </td>
                    <td className="px-5 py-4">
                      <p className="text-sm font-black text-slate-900">{item.entityType}</p>
                      <p className="text-xs font-bold text-slate-400">{item.entityCode || item.entityId}</p>
                    </td>
                    <td className="px-5 py-4 text-xs font-bold text-slate-500">{item.tenantName || 'Toàn hệ thống'}</td>
                    <td className="px-5 py-4 text-xs font-black text-slate-400 uppercase tracking-widest">{item.source}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
