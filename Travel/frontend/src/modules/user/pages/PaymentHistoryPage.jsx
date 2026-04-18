import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import {
  Bus,
  Compass,
  CreditCard,
  Filter,
  Hotel,
  Plane,
  Train,
} from 'lucide-react';
import { listCustomerPayments } from '../../../services/customerCommerceService';
import {
  CUSTOMER_PAYMENT_STATUS,
  formatCustomerPaymentStatusLabel,
  formatCustomerProductLabel,
  getCustomerPaymentStatusClass,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

const FILTERS = [
  { key: 'all', label: 'Tất cả' },
  { key: 'paid', label: 'Thành công' },
  { key: 'pending', label: 'Chờ xử lý' },
  { key: 'refunded', label: 'Đã hoàn' },
  { key: 'problem', label: 'Lỗi / hủy' },
];

function getProductIcon(productType) {
  switch (Number(productType || 0)) {
    case 1:
      return <Bus size={16} />;
    case 2:
      return <Train size={16} />;
    case 3:
      return <Plane size={16} />;
    case 4:
      return <Hotel size={16} />;
    default:
      return <Compass size={16} />;
  }
}

function matchFilter(item, filter) {
  const status = Number(item.paymentStatus || 0);

  if (filter === 'paid') {
    return status === CUSTOMER_PAYMENT_STATUS.PAID;
  }

  if (filter === 'pending') {
    return status === CUSTOMER_PAYMENT_STATUS.PENDING;
  }

  if (filter === 'refunded') {
    return status === CUSTOMER_PAYMENT_STATUS.REFUNDED_PARTIAL || status === CUSTOMER_PAYMENT_STATUS.REFUNDED_FULL;
  }

  if (filter === 'problem') {
    return status === CUSTOMER_PAYMENT_STATUS.CANCELLED
      || status === CUSTOMER_PAYMENT_STATUS.EXPIRED
      || status === CUSTOMER_PAYMENT_STATUS.FAILED;
  }

  return true;
}

export default function PaymentHistoryPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('all');

  useEffect(() => {
    let active = true;

    async function loadPayments() {
      setLoading(true);
      setError('');

      try {
        const response = await listCustomerPayments();

        if (!active) {
          return;
        }

        setItems(Array.isArray(response) ? response : []);
      } catch (requestError) {
        if (!active) {
          return;
        }

        setItems([]);
        setError(requestError.message || 'Không thể tải lịch sử thanh toán.');
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    loadPayments();

    return () => {
      active = false;
    };
  }, []);

  const filteredItems = useMemo(() => items.filter((item) => matchFilter(item, filter)), [filter, items]);
  const totalPaid = useMemo(
    () => items
      .filter((item) => Number(item.paymentStatus || 0) === CUSTOMER_PAYMENT_STATUS.PAID)
      .reduce((sum, item) => sum + Number(item.paidAmount || item.amount || 0), 0),
    [items],
  );

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-1">
          <CreditCard size={14} className="text-[#1EB4D4]" />
          <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Sổ thanh toán</span>
        </div>
        <h1 className="text-3xl font-black text-slate-900 tracking-tight">Lịch sử thanh toán</h1>
        <p className="text-slate-400 text-sm font-medium mt-1">Theo dõi toàn bộ giao dịch SePay, trạng thái xử lý và hoàn tiền của bạn.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {[
          { label: 'Tổng tiền đã thu', value: formatCurrency(totalPaid, 'VND'), tone: 'from-[#1EB4D4] to-[#002B7F]', highlight: true },
          { label: 'Giao dịch đã ghi nhận', value: String(items.length), tone: '', highlight: false },
          { label: 'Đơn có hoàn tiền', value: String(items.filter((item) => Number(item.refundedAmount || 0) > 0).length), tone: '', highlight: false },
        ].map((item) => (
          <div key={item.label} className={`p-6 rounded-[2rem] shadow-xl ${item.highlight ? `bg-gradient-to-br ${item.tone} text-white` : 'bg-white'}`}>
            <p className={`text-[10px] font-black uppercase tracking-widest mb-1 ${item.highlight ? 'text-white/60' : 'text-slate-400'}`}>{item.label}</p>
            <p className={`text-2xl font-black ${item.highlight ? 'text-white' : 'text-slate-900'}`}>{item.value}</p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 mb-6">
          <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest flex items-center gap-2">
            <Filter size={14} /> Danh sách giao dịch
          </h2>
          <div className="flex bg-slate-50 p-1 rounded-2xl border border-slate-100 gap-1 flex-wrap">
            {FILTERS.map((item) => (
              <button
                key={item.key}
                type="button"
                onClick={() => setFilter(item.key)}
                className={`px-5 py-2.5 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${
                  filter === item.key ? 'bg-white shadow-md text-[#1EB4D4]' : 'text-slate-400 hover:text-slate-700'
                }`}
              >
                {item.label}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="rounded-[2rem] bg-slate-50 p-8 text-center text-sm font-bold text-slate-500">
            Đang tải lịch sử giao dịch...
          </div>
        ) : error ? (
          <div className="rounded-[2rem] border border-rose-100 bg-rose-50 p-8 text-center text-sm font-bold text-rose-600">
            {error}
          </div>
        ) : filteredItems.length === 0 ? (
          <div className="rounded-[2rem] bg-slate-50 p-8 text-center text-sm font-bold text-slate-500">
            Chưa có giao dịch phù hợp với bộ lọc hiện tại.
          </div>
        ) : (
          <div className="space-y-3">
            {filteredItems.map((item) => (
              <div
                key={item.paymentId}
                className="flex flex-col md:flex-row items-start md:items-center gap-4 p-5 bg-slate-50 rounded-2xl hover:bg-white hover:shadow-md transition-all group border border-transparent hover:border-slate-100"
              >
                <div className="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 bg-white text-[#1EB4D4] border border-slate-100">
                  {getProductIcon(item.productType)}
                </div>

                <div className="flex-1 min-w-0">
                  <p className="font-black text-slate-900 text-sm">{item.title}</p>
                  <p className="text-xs text-slate-400 font-bold mt-1">
                    {formatCustomerProductLabel(item.productType)} • {item.paymentCode} • {formatDateTime(item.createdAt)}
                  </p>
                  <p className="text-[11px] text-slate-400 font-bold mt-1">{item.subtitle || item.orderCode}</p>
                </div>

                <div className="flex items-center gap-4 ml-auto">
                  <span className={`inline-flex items-center gap-1 px-3 py-1.5 rounded-xl text-[10px] font-black uppercase tracking-widest ${getCustomerPaymentStatusClass(item.paymentStatus)}`}>
                    {formatCustomerPaymentStatusLabel(item.paymentStatus)}
                  </span>

                  <div className="text-right min-w-[130px]">
                    <p className="font-black text-slate-900">{formatCurrency(item.amount, item.currencyCode)}</p>
                    <p className="text-[10px] text-slate-400 font-bold">
                      {Number(item.refundedAmount || 0) > 0
                        ? `Đã hoàn ${formatCurrency(item.refundedAmount, item.currencyCode)}`
                        : item.paidAt
                          ? `Đã thanh toán lúc ${formatDateTime(item.paidAt)}`
                          : 'Đang chờ xác nhận'}
                    </p>
                  </div>

                  <Link
                    to={`/my-account/bookings/${item.orderCode}`}
                    className="opacity-0 group-hover:opacity-100 px-4 py-2 text-[#1EB4D4] hover:bg-[#1EB4D4]/10 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all"
                  >
                    Xem đơn
                  </Link>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </motion.div>
  );
}
