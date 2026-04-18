import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import { ArrowRight, CheckCircle2, Clock, Compass, CreditCard, Plane, Train, Bus, Hotel, XCircle } from 'lucide-react';
import { listCustomerOrders } from '../../../services/customerCommerceService';
import {
  canCancelPendingOrder,
  canRequestRefund,
  CUSTOMER_ORDER_STATUS,
  formatCustomerOrderStatusLabel,
  formatCustomerProductLabel,
  getCustomerOrderStatusClass,
  getOrderSnapshot,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

const TABS = [
  { key: 'all', label: 'Tất cả' },
  { key: 'pending', label: 'Chờ thanh toán' },
  { key: 'active', label: 'Đang hiệu lực' },
  { key: 'done', label: 'Hoàn tất' },
  { key: 'refund', label: 'Hoàn / hủy' },
];

function getProductIcon(productType) {
  switch (Number(productType || 0)) {
    case 1:
      return <Bus size={20} />;
    case 2:
      return <Train size={20} />;
    case 3:
      return <Plane size={20} />;
    case 4:
      return <Hotel size={20} />;
    default:
      return <Compass size={20} />;
  }
}

export default function MyBookingsPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState('all');

  useEffect(() => {
    let active = true;

    listCustomerOrders({ page: 1, pageSize: 50 })
      .then((response) => {
        if (active) {
          setItems(response?.items || []);
        }
      })
      .catch((requestError) => {
        if (active) {
          setItems([]);
          setError(requestError.message || 'Không thể tải danh sách đơn hàng.');
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const filteredItems = useMemo(() => items.filter((item) => {
    const status = Number(item.status || 0);

    if (activeTab === 'pending') {
      return status === CUSTOMER_ORDER_STATUS.PENDING_PAYMENT;
    }

    if (activeTab === 'active') {
      return status === CUSTOMER_ORDER_STATUS.PAID || status === CUSTOMER_ORDER_STATUS.TICKET_ISSUED;
    }

    if (activeTab === 'done') {
      return status === CUSTOMER_ORDER_STATUS.COMPLETED;
    }

    if (activeTab === 'refund') {
      return canCancelPendingOrder(item) || canRequestRefund(item) || status >= CUSTOMER_ORDER_STATUS.REFUND_REQUESTED;
    }

    return true;
  }), [activeTab, items]);

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <CreditCard size={14} className="text-[#1EB4D4]" />
              <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Trung tâm đơn hàng</span>
            </div>
            <h1 className="text-3xl font-black text-slate-900 tracking-tight">Đơn hàng của tôi</h1>
            <p className="text-slate-400 text-sm font-medium mt-1">Theo dõi thanh toán, vé và hậu mãi cho toàn bộ dịch vụ đã đặt qua nền tảng.</p>
          </div>

          <div className="flex bg-slate-50 p-1 rounded-2xl border border-slate-100 gap-1 flex-wrap">
            {TABS.map((tab) => (
              <button
                key={tab.key}
                type="button"
                onClick={() => setActiveTab(tab.key)}
                className={`px-5 py-2.5 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all ${activeTab === tab.key ? 'bg-white shadow-md text-[#1EB4D4]' : 'text-slate-400 hover:text-slate-700'}`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {loading ? (
        <div className="bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
          Đang tải lịch sử đơn hàng...
        </div>
      ) : error ? (
        <div className="bg-rose-50 rounded-[2rem] p-12 text-center text-sm font-bold text-rose-600 shadow-xl shadow-slate-100/60 border border-rose-100">
          {error}
        </div>
      ) : filteredItems.length === 0 ? (
        <div className="bg-white rounded-[2rem] p-12 text-center shadow-xl shadow-slate-100/60">
          <p className="text-lg font-black text-slate-900 mb-2">Chưa có đơn hàng phù hợp</p>
          <p className="text-sm font-medium text-slate-400 mb-6">Khi bạn đặt dịch vụ thành công, vé/voucher sẽ xuất hiện tại đây.</p>
          <Link to="/" className="inline-flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
            Khám phá dịch vụ <ArrowRight size={14} />
          </Link>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredItems.map((order, index) => {
            const snapshot = getOrderSnapshot(order);
            const showAfterSales = canCancelPendingOrder(order) || canRequestRefund(order);

            return (
              <motion.div
                key={order.orderCode}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: index * 0.05 }}
                className="bg-white rounded-[2rem] shadow-xl shadow-slate-100/60 overflow-hidden"
              >
                <div className="flex flex-col md:flex-row">
                  <div className="md:w-52 h-36 md:h-auto shrink-0 relative overflow-hidden bg-slate-900 text-white flex items-center justify-center">
                    <div className="w-14 h-14 bg-white/10 rounded-[1.5rem] flex items-center justify-center">
                      {getProductIcon(order.productType)}
                    </div>
                  </div>

                  <div className="flex-1 p-6 md:pl-8">
                    <div className="flex items-center gap-3 mb-3 flex-wrap">
                      <span className="text-[10px] font-black text-slate-400 tracking-[0.2em]">{order.orderCode}</span>
                      <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-black uppercase tracking-wider ${getCustomerOrderStatusClass(order.status)}`}>
                        {Number(order.status) >= CUSTOMER_ORDER_STATUS.REFUND_REQUESTED || canCancelPendingOrder(order) ? <XCircle size={12} /> : <CheckCircle2 size={12} />}
                        {formatCustomerOrderStatusLabel(order.status)}
                      </span>
                    </div>

                    <div className="mb-4">
                      <p className="text-xl font-black text-slate-900">{snapshot?.title || order.orderCode}</p>
                      <p className="text-sm text-slate-400 font-bold mt-1">{formatCustomerProductLabel(order.productType)} • {snapshot?.subtitle || snapshot?.providerName || 'Đối tác trên nền tảng'}</p>
                    </div>

                    <div className="flex flex-wrap items-center gap-6 text-xs text-slate-500 font-bold">
                      <span className="flex items-center gap-1.5"><Clock size={13} className="text-slate-300" />{formatDateTime(order.createdAt)}</span>
                      <span>{snapshot?.routeFrom && snapshot?.routeTo ? `${snapshot.routeFrom} → ${snapshot.routeTo}` : snapshot?.locationText || 'Thông tin sẽ hiển thị theo loại dịch vụ'}</span>
                    </div>
                  </div>

                  <div className="flex md:flex-col items-center justify-between md:justify-center gap-4 px-6 py-4 md:py-0 md:w-60 border-t md:border-t-0 md:border-l border-dashed border-slate-200">
                    <div className="text-center">
                      <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mb-0.5">Tổng tiền</p>
                      <p className="text-xl font-black text-slate-900">{formatCurrency(order.payableAmount, order.currencyCode)}</p>
                    </div>
                    <div className="flex md:flex-col gap-2">
                      <Link to={`/my-account/bookings/${order.orderCode}`} className="flex items-center gap-1.5 px-4 py-2.5 bg-slate-900 text-white rounded-xl text-[10px] font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
                        Chi tiết <ArrowRight size={12} />
                      </Link>
                      {showAfterSales ? (
                        <Link to={`/my-account/bookings/${order.orderCode}/cancel`} className="flex items-center gap-1.5 px-4 py-2.5 bg-[#1EB4D4]/10 text-[#1EB4D4] rounded-xl text-[10px] font-black uppercase tracking-widest hover:bg-[#1EB4D4] hover:text-white transition-all">
                          Hủy / hoàn
                        </Link>
                      ) : null}
                    </div>
                  </div>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </motion.div>
  );
}
