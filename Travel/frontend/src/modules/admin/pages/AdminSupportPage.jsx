import React, { useEffect, useMemo, useState } from 'react';
import { MessageSquare, RefreshCw, Search, Send } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import {
  listAdminCommerceSupportTickets,
  replyAdminCommerceSupportTicket,
} from '../../../services/commerceBackofficeService';
import { formatDateTime } from '../../tenant/train/utils/presentation';

const STATUS_FILTERS = [
  { value: 'all', label: 'Tất cả' },
  { value: '1', label: 'Mới' },
  { value: '2', label: 'Đang xử lý' },
  { value: '3', label: 'Đã xong' },
  { value: '4', label: 'Đã đóng' },
];

function getPriorityConfig(priority) {
  switch (priority) {
    case 'high':
      return { label: 'Cao', color: 'bg-rose-100 text-rose-700' };
    case 'low':
      return { label: 'Thấp', color: 'bg-slate-100 text-slate-600' };
    default:
      return { label: 'TB', color: 'bg-amber-100 text-amber-700' };
  }
}

function getStatusConfig(value) {
  switch (Number(value || 0)) {
    case 1:
      return { label: 'Mới', color: 'bg-blue-100 text-blue-700' };
    case 2:
      return { label: 'Đang xử lý', color: 'bg-amber-100 text-amber-700' };
    case 3:
      return { label: 'Đã giải quyết', color: 'bg-emerald-100 text-emerald-700' };
    default:
      return { label: 'Đã đóng', color: 'bg-slate-100 text-slate-600' };
  }
}

export default function AdminSupportPage() {
  const [searchParams] = useSearchParams();
  const [tickets, setTickets] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [reply, setReply] = useState('');
  const [search, setSearch] = useState(() => searchParams.get('q') || '');
  const [statusFilter, setStatusFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  useEffect(() => {
    setSearch(searchParams.get('q') || '');
  }, [searchParams]);

  async function loadTickets() {
    setLoading(true);
    setError('');

    try {
      const response = await listAdminCommerceSupportTickets({
        q: search.trim() || undefined,
        status: statusFilter === 'all' ? undefined : statusFilter,
      });

      const items = Array.isArray(response?.items) ? response.items : [];
      setTickets(items);
      setSelectedId((current) => (current && items.some((item) => item.id === current) ? current : items[0]?.id || ''));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải support tickets.');
      setTickets([]);
      setSelectedId('');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadTickets();
  }, [search, statusFilter]);

  const selected = useMemo(
    () => tickets.find((item) => item.id === selectedId) || null,
    [selectedId, tickets],
  );

  const stats = useMemo(() => ({
    open: tickets.filter((item) => Number(item.status) === 1).length,
    processing: tickets.filter((item) => Number(item.status) === 2).length,
    resolved: tickets.filter((item) => Number(item.status) === 3).length,
    high: tickets.filter((item) => item.priority === 'high').length,
  }), [tickets]);

  async function handleReply(markResolved = false) {
    if (!selected || (!reply.trim() && !markResolved)) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      await replyAdminCommerceSupportTicket(selected.id, {
        message: reply.trim() || 'Admin đã xác nhận ticket đã được xử lý.',
        markResolved,
      });
      setReply('');
      setNotice(markResolved ? 'Ticket đã được đánh dấu giải quyết.' : 'Đã gửi phản hồi cho customer.');
      await loadTickets();
    } catch (requestError) {
      setError(requestError.message || 'Không thể gửi phản hồi ticket.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-black text-slate-900">Support Tickets</h1>
        <p className="text-slate-500 text-sm mt-1">Phân loại, phản hồi và chốt ticket sau bán hàng theo order/payment/refund.</p>
      </div>

      {notice ? (
        <div className="mb-5 rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      ) : null}

      {error ? (
        <div className="mb-5 rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Mới', value: stats.open, className: 'bg-blue-50' },
          { label: 'Đang xử lý', value: stats.processing, className: 'bg-amber-50' },
          { label: 'Đã giải quyết', value: stats.resolved, className: 'bg-emerald-50' },
          { label: 'Ưu tiên cao', value: stats.high, className: 'bg-rose-50' },
        ].map((item) => (
          <div key={item.label} className={`rounded-2xl p-4 shadow-sm border border-slate-100 ${item.className}`}>
            <p className="text-3xl font-black text-slate-900">{loading ? '--' : item.value}</p>
            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        <div className="lg:col-span-2 space-y-3">
          <div className="bg-white rounded-2xl p-3 border border-slate-100 flex gap-2">
            <div className="flex-1 flex items-center gap-2 bg-slate-50 rounded-xl px-3">
              <Search size={14} className="text-slate-400" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Tìm ticket..."
                className="bg-transparent py-2.5 flex-1 text-xs font-medium outline-none"
              />
            </div>
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value)}
              className="bg-slate-50 rounded-xl px-3 py-2.5 text-xs font-black text-slate-600 border-none outline-none cursor-pointer"
            >
              {STATUS_FILTERS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <button
              type="button"
              onClick={loadTickets}
              className="px-3 py-2.5 bg-slate-900 text-white rounded-xl hover:bg-blue-600 transition-all"
            >
              <RefreshCw size={14} />
            </button>
          </div>

          {loading ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-400 border border-slate-100">
              <p className="font-bold">Đang tải ticket...</p>
            </div>
          ) : tickets.length === 0 ? (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
              <MessageSquare size={36} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold">Chưa có ticket phù hợp</p>
            </div>
          ) : (
            tickets.map((item) => {
              const statusConfig = getStatusConfig(item.status);
              const priorityConfig = getPriorityConfig(item.priority);

              return (
                <div
                  key={item.id}
                  onClick={() => setSelectedId(item.id)}
                  className={`bg-white rounded-2xl p-4 shadow-sm border cursor-pointer hover:shadow-md transition-all ${selectedId === item.id ? 'border-[#1EB4D4]' : 'border-slate-100'}`}
                >
                  <div className="flex items-start gap-2 mb-2">
                    <p className="font-black text-slate-900 text-sm flex-1 leading-tight">{item.subject}</p>
                    <span className={`px-2 py-0.5 rounded-lg text-[9px] font-black uppercase shrink-0 ${priorityConfig.color}`}>{priorityConfig.label}</span>
                  </div>
                  <p className="text-xs text-slate-500 font-bold">{item.customerName} - {item.category}</p>
                  <div className="flex items-center justify-between mt-2">
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-lg text-[9px] font-black uppercase ${statusConfig.color}`}>{statusConfig.label}</span>
                    <span className="text-[10px] text-slate-400 font-bold">{formatDateTime(item.lastActivityAt || item.createdAt)}</span>
                  </div>
                </div>
              );
            })
          )}
        </div>

        <div className="lg:col-span-3">
          {selected ? (
            <div className="bg-white rounded-2xl shadow-sm border border-slate-100 flex flex-col h-full min-h-[500px]">
              <div className="p-5 border-b border-slate-100">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-black text-slate-900">{selected.subject}</p>
                    <p className="text-xs text-slate-400 font-bold mt-0.5">
                      {selected.ticketCode} - {selected.customerName} - {selected.tenantName || 'Platform'}
                    </p>
                  </div>
                  {Number(selected.status) !== 3 ? (
                    <button
                      type="button"
                      onClick={() => handleReply(true)}
                      disabled={saving}
                      className="px-3 py-1.5 bg-emerald-50 text-emerald-700 rounded-xl text-[10px] font-black uppercase hover:bg-emerald-100 transition-all disabled:opacity-60"
                    >
                      Giải quyết
                    </button>
                  ) : null}
                </div>
              </div>

              <div className="flex-1 p-5 space-y-4 overflow-y-auto">
                {(selected.messages || []).map((message, index) => (
                  <div key={`${message.from}-${index}`} className={`flex ${message.from === 'agent' ? 'justify-end' : 'justify-start'}`}>
                    <div className={`max-w-[80%] rounded-2xl px-4 py-3 ${message.from === 'agent' ? 'bg-[#1EB4D4] text-white' : 'bg-slate-50 text-slate-900'}`}>
                      <p className="text-sm font-medium">{message.text}</p>
                      <p className={`text-[10px] font-bold mt-1 ${message.from === 'agent' ? 'text-white/60' : 'text-slate-400'}`}>
                        {message.from === 'agent' ? 'CS Team' : selected.customerName} - {formatDateTime(message.at)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>

              <div className="p-4 border-t border-slate-100 flex gap-3">
                <textarea
                  value={reply}
                  onChange={(event) => setReply(event.target.value)}
                  rows={2}
                  placeholder="Nhập phản hồi..."
                  className="flex-1 bg-slate-50 rounded-2xl px-4 py-3 text-sm font-medium outline-none border-2 border-transparent focus:border-[#1EB4D4]/30 resize-none"
                />
                <button
                  type="button"
                  onClick={() => handleReply(false)}
                  disabled={saving || !reply.trim()}
                  className="px-4 py-3 bg-[#1EB4D4] text-white rounded-2xl hover:bg-[#002B7F] transition-all flex items-center justify-center shrink-0 disabled:opacity-60"
                >
                  <Send size={16} />
                </button>
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100 h-full flex items-center justify-center">
              <div>
                <MessageSquare size={36} className="mx-auto mb-3 opacity-30" />
                <p className="font-bold">Chọn ticket để xem hội thoại</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
