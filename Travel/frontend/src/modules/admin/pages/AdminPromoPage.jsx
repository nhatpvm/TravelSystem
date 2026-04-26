import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CheckCircle2, Hotel, Loader2, Tag } from 'lucide-react';
import { getAdminOpsPromoReadiness } from '../../../services/adminOpsService';

const statusClass = {
  Ready: 'bg-emerald-50 text-emerald-700 border-emerald-100',
  Missing: 'bg-amber-50 text-amber-700 border-amber-100',
};

function formatDate(value) {
  if (!value) {
    return '--';
  }

  return new Date(value).toLocaleDateString('vi-VN');
}

export default function AdminPromoPage() {
  const [data, setData] = useState({ summary: {}, tenants: [], readiness: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    async function load() {
      setLoading(true);
      setError('');

      try {
        const response = await getAdminOpsPromoReadiness();
        if (!active) {
          return;
        }

        setData({
          summary: response?.summary || {},
          tenants: Array.isArray(response?.tenants) ? response.tenants : [],
          readiness: Array.isArray(response?.readiness) ? response.readiness : [],
        });
      } catch (err) {
        if (active) {
          setError(err?.message || 'Không tải được trạng thái khuyến mãi.');
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

  const cards = [
    {
      label: 'Platform promo',
      value: 'Chưa có engine',
      className: 'bg-amber-50',
      icon: <AlertTriangle size={20} className="text-amber-600" />,
    },
    {
      label: 'Hotel promo overrides',
      value: `${data.summary?.activeHotelOverrideCount || 0} đang hiệu lực`,
      className: 'bg-emerald-50',
      icon: <Hotel size={20} className="text-emerald-600" />,
    },
    {
      label: 'Tổng override',
      value: data.summary?.hotelOverrideCount || 0,
      className: 'bg-slate-50',
      icon: <Tag size={20} className="text-slate-500" />,
    },
  ];

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Marketing & Khuyến mãi</h1>
          <p className="text-slate-500 text-sm mt-1">Quản lý trạng thái khuyến mãi toàn sàn và dữ liệu hotel promo đang có.</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {cards.map((item) => (
          <div key={item.label} className={`bg-white p-6 rounded-3xl border border-slate-100 shadow-sm flex items-center justify-between ${item.className}`}>
            <div>
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{item.label}</p>
              <p className="text-xl font-black text-slate-900">{item.value}</p>
            </div>
            <div className="w-12 h-12 bg-white/70 rounded-2xl flex items-center justify-center">
              {item.icon}
            </div>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
        <div className="p-8 border-b border-slate-50 bg-slate-50/50">
          <h3 className="font-black text-slate-900 text-lg">Trạng thái triển khai</h3>
          <p className="text-sm font-bold text-slate-400 mt-1">
            Màn hình này đọc trạng thái thật từ backend admin ops, không dùng campaign mẫu.
          </p>
        </div>

        {loading ? (
          <div className="p-8 text-center">
            <Loader2 size={24} className="animate-spin text-blue-600 mx-auto mb-3" />
            <p className="text-sm font-bold text-slate-400">Đang tải trạng thái khuyến mãi...</p>
          </div>
        ) : error ? (
          <div className="p-8">
            <div className="rounded-3xl bg-rose-50 border border-rose-100 p-6">
              <p className="text-[10px] font-black uppercase tracking-widest text-rose-700">Lỗi tải dữ liệu</p>
              <h4 className="text-xl font-black text-slate-900 mt-2">Không đọc được trạng thái khuyến mãi</h4>
              <p className="text-sm font-bold text-slate-500 mt-2">{error}</p>
            </div>
          </div>
        ) : (
          <div className="p-8 grid grid-cols-1 lg:grid-cols-2 gap-5">
            {data.readiness.map((item) => (
              <div key={item.area} className={`rounded-3xl border p-6 ${statusClass[item.status] || 'bg-slate-50 text-slate-600 border-slate-100'}`}>
                <div className="flex items-center justify-between gap-4">
                  <p className="text-[10px] font-black uppercase tracking-widest">{item.status === 'Ready' ? 'Có thể thao tác' : 'Cần bổ sung backend'}</p>
                  {item.status === 'Ready' ? <CheckCircle2 size={18} /> : <AlertTriangle size={18} />}
                </div>
                <h4 className="text-xl font-black text-slate-900 mt-2">{item.area}</h4>
                <p className="text-sm font-bold text-slate-500 mt-2">{item.note}</p>
                {item.actionUrl ? (
                  <Link to={item.actionUrl} className="inline-flex mt-5 px-5 py-3 bg-slate-900 text-white rounded-2xl text-xs font-black uppercase tracking-widest hover:bg-blue-600 transition-all">
                    Mở màn quản lý
                  </Link>
                ) : null}
              </div>
            ))}
          </div>
        )}
      </div>

      {!loading && !error ? (
        <div className="bg-white rounded-3xl border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-6 border-b border-slate-50">
            <h3 className="font-black text-slate-900">Tenant có hotel promo override</h3>
            <p className="text-sm font-bold text-slate-400 mt-1">
              Đang có {data.summary?.tenantCount || 0} tenant dùng override khuyến mãi khách sạn.
            </p>
          </div>

          {data.tenants.length === 0 ? (
            <div className="p-8 text-center text-sm font-bold text-slate-400">Chưa có hotel promo override nào.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/60">
                    <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Tenant</th>
                    <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Override</th>
                    <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Đang hiệu lực</th>
                    <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Khoảng ngày</th>
                    <th className="px-5 py-4 text-[10px] font-black uppercase tracking-widest text-slate-400">Cập nhật</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-50">
                  {data.tenants.map((tenant) => (
                    <tr key={tenant.tenantId} className="hover:bg-slate-50/60">
                      <td className="px-5 py-4">
                        <p className="text-sm font-black text-slate-900">{tenant.tenantName}</p>
                        <p className="text-xs font-bold text-slate-400">{tenant.tenantCode}</p>
                      </td>
                      <td className="px-5 py-4 text-sm font-black text-slate-900">{tenant.overrideCount}</td>
                      <td className="px-5 py-4 text-sm font-black text-emerald-700">{tenant.activeCount}</td>
                      <td className="px-5 py-4 text-xs font-bold text-slate-500">
                        {tenant.firstStartDate || '--'} - {tenant.lastEndDate || '--'}
                      </td>
                      <td className="px-5 py-4 text-xs font-bold text-slate-500">{formatDate(tenant.lastUpdatedAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      ) : null}
    </div>
  );
}
