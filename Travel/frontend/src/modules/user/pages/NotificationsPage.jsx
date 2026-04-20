import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  AlertCircle,
  Bell,
  CheckCircle2,
  Clock3,
  CreditCard,
  FileText,
  Plane,
  RefreshCw,
  ShieldCheck,
  MessageSquareText,
  Ticket,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import {
  listCustomerNotifications,
  markAllCustomerNotificationsRead,
  markCustomerNotificationRead,
} from '../../../services/customerCommerceService';
import { getCustomerLocale } from '../../../services/customerPreferences';

function getNotificationIcon(category) {
  switch (String(category || '').toLowerCase()) {
    case 'payment':
      return <CreditCard size={16} className="text-[#1EB4D4]" />;
    case 'ticket':
      return <Ticket size={16} className="text-emerald-500" />;
    case 'refund':
      return <RefreshCw size={16} className="text-amber-500" />;
    case 'order':
      return <Plane size={16} className="text-sky-500" />;
    case 'support':
      return <MessageSquareText size={16} className="text-indigo-500" />;
    case 'checkout':
      return <Clock3 size={16} className="text-violet-500" />;
    default:
      return <FileText size={16} className="text-slate-400" />;
  }
}

function getNotificationTone(category) {
  switch (String(category || '').toLowerCase()) {
    case 'payment':
      return 'bg-sky-50';
    case 'ticket':
      return 'bg-emerald-50';
    case 'refund':
      return 'bg-amber-50';
    case 'order':
      return 'bg-indigo-50';
    case 'support':
      return 'bg-indigo-50';
    case 'checkout':
      return 'bg-violet-50';
    default:
      return 'bg-slate-50';
  }
}

function formatTime(value) {
  if (!value) {
    return 'Vừa cập nhật';
  }

  return new Date(value).toLocaleString(getCustomerLocale());
}

export default function NotificationsPage() {
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function loadNotifications() {
    setLoading(true);
    setError('');

    try {
      const response = await listCustomerNotifications();
      setItems(Array.isArray(response?.items) ? response.items : []);
    } catch (requestError) {
      setItems([]);
      setError(requestError.message || 'Không thể tải thông báo.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadNotifications();
  }, []);

  const unreadCount = useMemo(
    () => items.filter((item) => Number(item.status || 0) === 1).length,
    [items],
  );

  async function handleRead(item) {
    if (Number(item.status || 0) !== 1) {
      if (item.actionUrl) {
        navigate(item.actionUrl);
      }
      return;
    }

    setBusy(true);

    try {
      await markCustomerNotificationRead(item.id);
      await loadNotifications();

      if (item.actionUrl) {
        navigate(item.actionUrl);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái thông báo.');
    } finally {
      setBusy(false);
    }
  }

  async function handleReadAll() {
    setBusy(true);
    setError('');

    try {
      await markAllCustomerNotificationsRead();
      await loadNotifications();
    } catch (requestError) {
      setError(requestError.message || 'Không thể đánh dấu tất cả là đã đọc.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Bell size={14} className="text-[#1EB4D4]" />
            <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Inbox</span>
          </div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight flex items-center gap-3">
            Thông báo
            {unreadCount > 0 ? (
              <span className="inline-flex items-center justify-center w-7 h-7 bg-rose-500 text-white rounded-full text-[11px] font-black">
                {unreadCount}
              </span>
            ) : null}
          </h1>
          <p className="text-slate-400 text-sm font-medium mt-1">Theo dõi trạng thái payment, ticket, refund và các cập nhật vận hành.</p>
        </div>
        <div className="flex items-center gap-3">
          {unreadCount > 0 ? (
            <button
              type="button"
              onClick={handleReadAll}
              disabled={busy}
              className="flex items-center gap-2 px-5 py-3 bg-slate-50 hover:bg-slate-100 text-slate-600 rounded-2xl font-black text-xs uppercase tracking-widest transition-all disabled:opacity-60"
            >
              <CheckCircle2 size={14} /> Đánh dấu tất cả đã đọc
            </button>
          ) : null}
          <div className="w-11 h-11 rounded-2xl bg-slate-50 text-slate-500 flex items-center justify-center">
            <ShieldCheck size={18} />
          </div>
        </div>
      </div>

      {error ? (
        <div className="rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {loading ? (
        <div className="bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
          Đang tải thông báo...
        </div>
      ) : items.length === 0 ? (
        <div className="bg-white rounded-[2.5rem] p-16 text-center shadow-xl shadow-slate-100/60">
          <div className="w-20 h-20 bg-slate-50 rounded-full flex items-center justify-center mx-auto mb-4">
            <Bell size={36} className="text-slate-300" />
          </div>
          <h3 className="text-xl font-black text-slate-900 mb-2">Không có thông báo mới</h3>
          <p className="text-slate-400 text-sm italic" style={{ fontFamily: "'Kalam', cursive" }}>
            Khi có thay đổi về đơn hàng hoặc thanh toán, hệ thống sẽ hiển thị tại đây.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {items.map((item, index) => {
            const unread = Number(item.status || 0) === 1;

            return (
              <motion.button
                key={item.id}
                type="button"
                layout
                initial={{ opacity: 0, x: -16 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: index * 0.05 }}
                onClick={() => handleRead(item)}
                className={`group relative w-full text-left flex gap-5 p-5 rounded-[2rem] transition-all border ${
                  unread
                    ? 'bg-white border-[#1EB4D4]/20 shadow-lg shadow-sky-100/40 hover:shadow-xl'
                    : 'bg-white border-slate-100 shadow-sm hover:shadow-md hover:border-slate-200'
                }`}
              >
                {unread ? (
                  <div className="absolute left-4 top-1/2 -translate-y-1/2 w-2 h-2 bg-[#1EB4D4] rounded-full" />
                ) : null}

                <div className={`w-11 h-11 rounded-2xl flex items-center justify-center shrink-0 ${getNotificationTone(item.category)}`}>
                  {getNotificationIcon(item.category)}
                </div>

                <div className="flex-1 min-w-0">
                  <p className={`text-sm font-black mb-0.5 ${unread ? 'text-slate-900' : 'text-slate-700'}`}>{item.title}</p>
                  <p className="text-xs text-slate-500 font-medium leading-relaxed">{item.body}</p>
                  <div className="flex flex-wrap items-center gap-3 mt-3">
                    <p className="text-[10px] text-slate-400 font-bold italic">{formatTime(item.createdAt)}</p>
                    {item.actionUrl ? (
                      <span className="text-[10px] font-black uppercase tracking-widest text-[#1EB4D4]">Có thể mở chi tiết</span>
                    ) : null}
                  </div>
                </div>

                <div className="self-start">
                  {unread ? (
                    <span className="inline-flex items-center gap-1 px-3 py-1 bg-[#1EB4D4]/10 text-[#1EB4D4] rounded-xl text-[10px] font-black uppercase tracking-widest">
                      Mới
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1 px-3 py-1 bg-slate-100 text-slate-500 rounded-xl text-[10px] font-black uppercase tracking-widest">
                      Đã đọc
                    </span>
                  )}
                </div>
              </motion.button>
            );
          })}
        </div>
      )}

      <div className="relative rounded-[2rem] overflow-hidden bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-100 p-8">
        <div className="relative z-10 flex items-start gap-6">
          <div className="w-14 h-14 bg-white rounded-2xl flex items-center justify-center text-amber-500 shadow-xl shadow-amber-200/50 shrink-0">
            <AlertCircle size={28} strokeWidth={1.5} />
          </div>
          <div>
            <h3 className="font-black text-amber-900 text-lg mb-1">Thông báo customer commerce</h3>
            <p className="text-amber-700/80 text-sm font-medium leading-relaxed max-w-xl">
              Các mốc như tạo order, thanh toán SePay, phát hành ticket, yêu cầu hoàn tiền và VAT invoice đều sẽ đổ về hộp thư này để bạn theo dõi trọn vòng đời đơn.
            </p>
          </div>
        </div>
      </div>
    </motion.div>
  );
}
