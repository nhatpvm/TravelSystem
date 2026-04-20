import React, { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  AlertCircle,
  ArrowLeft,
  Calendar,
  CheckCircle2,
  Circle,
  Clock,
  CreditCard,
  MapPin,
  ShieldCheck,
  User,
} from 'lucide-react';
import {
  getCustomerOrder,
  getCustomerOrderTimeline,
} from '../../../services/customerCommerceService';
import {
  canCancelPendingOrder,
  canRequestRefund,
  formatCustomerOrderStatusLabel,
  formatCustomerPaymentStatusLabel,
  formatCustomerProductLabel,
  formatCustomerRefundStatusLabel,
  getCustomerOrderStatusClass,
  getOrderSnapshot,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

function getTimelineToneClasses(tone, isCurrent) {
  switch (tone) {
    case 'success':
      return {
        dot: 'bg-emerald-500 text-white',
        card: 'bg-emerald-50 border-emerald-100',
        title: 'text-emerald-900',
      };
    case 'warning':
      return {
        dot: isCurrent ? 'bg-amber-500 text-white animate-pulse' : 'bg-amber-400 text-white',
        card: 'bg-amber-50 border-amber-100',
        title: 'text-amber-900',
      };
    case 'danger':
      return {
        dot: 'bg-rose-500 text-white',
        card: 'bg-rose-50 border-rose-100',
        title: 'text-rose-900',
      };
    default:
      return {
        dot: isCurrent ? 'bg-[#1EB4D4] text-white animate-pulse' : 'bg-slate-300 text-slate-600',
        card: 'bg-slate-50 border-slate-100',
        title: 'text-slate-900',
      };
  }
}

export default function BookingDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [timelineItems, setTimelineItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) {
      setLoading(false);
      setError('Thiếu mã đơn hàng.');
      return;
    }

    let active = true;
    setLoading(true);
    setError('');

    Promise.all([
      getCustomerOrder(id),
      getCustomerOrderTimeline(id),
    ])
      .then(([orderResponse, timelineResponse]) => {
        if (!active) {
          return;
        }

        setOrder(orderResponse);
        setTimelineItems(Array.isArray(timelineResponse) ? timelineResponse : []);
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải chi tiết đơn hàng.');
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
  }, [id]);

  const snapshot = useMemo(() => getOrderSnapshot(order), [order]);
  const timeline = useMemo(() => (Array.isArray(timelineItems) ? timelineItems : []), [timelineItems]);
  const lines = Array.isArray(snapshot?.lines) ? snapshot.lines : [];
  const showAfterSales = canCancelPendingOrder(order) || canRequestRefund(order);

  if (loading) {
    return (
      <div className="bg-white rounded-[2.5rem] p-10 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
        Đang tải chi tiết đơn hàng...
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="bg-rose-50 rounded-[2.5rem] p-10 text-center text-sm font-bold text-rose-600 shadow-xl shadow-slate-100/60 border border-rose-100">
        {error || 'Không tìm thấy đơn hàng.'}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Link to="/my-account/bookings" className="inline-flex items-center gap-2 text-slate-500 hover:text-[#1EB4D4] font-bold text-sm transition-colors">
        <ArrowLeft size={16} /> Quay lại danh sách
      </Link>

      <div className="bg-white rounded-[2.5rem] shadow-xl shadow-slate-200/50 overflow-hidden">
        <div className="bg-[#002B7F] p-8 text-white relative overflow-hidden">
          <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full -translate-y-1/2 translate-x-1/2 blur-3xl" />
          <div className="relative z-10 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
            <div>
              <div className="flex items-center gap-2 mb-2 opacity-80">
                <span className="text-[10px] font-black uppercase tracking-widest">Mã đơn hàng</span>
              </div>
              <h1 className="text-3xl font-black tracking-tight">{order.orderCode}</h1>
            </div>
            <div className={`px-4 py-2 rounded-xl border border-white/10 text-[10px] font-black uppercase tracking-widest ${getCustomerOrderStatusClass(order.status)}`}>
              {formatCustomerOrderStatusLabel(order.status)}
            </div>
          </div>
        </div>

        <div className="p-8">
          <div className="mb-12">
            <h2 className="text-sm font-black text-slate-900 uppercase tracking-widest mb-8 flex items-center gap-2">
              <Clock size={16} className="text-[#1EB4D4]" /> Tiến trình đơn hàng
            </h2>
            <div className="relative pl-5 space-y-4">
              <div className="absolute left-4 top-1 bottom-1 w-0.5 bg-slate-100" />
              {timeline.map((step, index) => {
                const tone = getTimelineToneClasses(step.tone, step.isCurrent);

                return (
                  <div key={`${step.key || step.title}-${index}`} className="relative flex items-start gap-4">
                    <div className={`relative z-10 mt-1 w-9 h-9 rounded-full flex items-center justify-center border-4 border-white shadow-md ${tone.dot}`}>
                      {step.tone === 'success' ? <CheckCircle2 size={18} /> : <Circle size={10} fill="currentColor" />}
                    </div>
                    <div className={`flex-1 rounded-3xl border p-5 ${tone.card}`}>
                      <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-2">
                        <div>
                          <p className={`text-sm font-black ${tone.title}`}>{step.title}</p>
                          <p className="text-[10px] font-bold text-slate-400 mt-1">{formatDateTime(step.occurredAt)}</p>
                        </div>
                        {step.isCurrent ? (
                          <span className="inline-flex items-center px-3 py-1 rounded-xl bg-white text-[10px] font-black uppercase tracking-widest text-[#1EB4D4] border border-[#1EB4D4]/10">
                            Mốc hiện tại
                          </span>
                        ) : null}
                      </div>
                      <p className="text-xs font-medium text-slate-600 mt-3 leading-relaxed">
                        {step.description || 'Hệ thống đang tiếp tục cập nhật tiến trình cho đơn hàng này.'}
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="grid md:grid-cols-2 gap-8">
            <div className="space-y-6">
              <div className="bg-slate-50 rounded-3xl p-6 border border-slate-100">
                <p className="text-xs font-black text-slate-400 uppercase tracking-widest mb-4">Thông tin dịch vụ</p>
                <p className="text-xl font-black text-slate-900">{snapshot?.title || order.orderCode}</p>
                <p className="text-sm font-bold text-slate-500 mt-2">{formatCustomerProductLabel(order.productType)} • {snapshot?.subtitle || snapshot?.providerName || 'Đối tác trên nền tảng'}</p>
                <div className="grid grid-cols-2 gap-4 mt-6">
                  <div className="flex items-center gap-3">
                    <Calendar size={14} className="text-slate-400" />
                    <div>
                      <p className="text-[9px] font-bold text-slate-400 uppercase leading-none mb-1">Khởi hành / nhận dịch vụ</p>
                      <p className="text-sm font-black text-slate-800">{snapshot?.departureAt ? formatDateTime(snapshot.departureAt) : '--'}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <Clock size={14} className="text-slate-400" />
                    <div>
                      <p className="text-[9px] font-bold text-slate-400 uppercase leading-none mb-1">Kết thúc / đến nơi</p>
                      <p className="text-sm font-black text-slate-800">{snapshot?.arrivalAt ? formatDateTime(snapshot.arrivalAt) : '--'}</p>
                    </div>
                  </div>
                  <div className="col-span-2 flex items-center gap-3">
                    <MapPin size={14} className="text-slate-400" />
                    <div>
                      <p className="text-[9px] font-bold text-slate-400 uppercase leading-none mb-1">Hành trình / vị trí</p>
                      <p className="text-sm font-black text-slate-800">{snapshot?.routeFrom && snapshot?.routeTo ? `${snapshot.routeFrom} → ${snapshot.routeTo}` : snapshot?.locationText || 'Đang cập nhật'}</p>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-slate-50 rounded-3xl p-6 border border-slate-100 space-y-4">
                <p className="text-xs font-black text-slate-400 uppercase tracking-widest">Chi tiết tính tiền</p>
                {lines.map((line) => (
                  <div key={`${line.label}-${line.quantity}`} className="flex justify-between items-center text-sm">
                    <span className="font-bold text-slate-500">{line.label} x{line.quantity}</span>
                    <span className="font-black text-slate-900">{formatCurrency(line.lineAmount, order.currencyCode)}</span>
                  </div>
                ))}
                <div className="pt-4 border-t border-slate-200 flex justify-between items-center text-sm">
                  <span className="font-black text-slate-900">Tổng cộng</span>
                  <span className="text-xl font-black text-[#1EB4D4]">{formatCurrency(order.payableAmount, order.currencyCode)}</span>
                </div>
              </div>
            </div>

            <div className="space-y-6">
              <div className="bg-white rounded-3xl p-6 border border-slate-100 shadow-sm space-y-4">
                <p className="text-xs font-black text-slate-400 uppercase tracking-widest">Người đặt & thanh toán</p>
                <div className="flex items-center gap-4">
                  <div className="w-10 h-10 bg-slate-100 rounded-xl flex items-center justify-center text-slate-500">
                    <User size={18} />
                  </div>
                  <div>
                    <p className="text-sm font-black text-slate-900">{order.contactFullName}</p>
                    <p className="text-[11px] font-bold text-slate-400">{order.contactEmail} • {order.contactPhone}</p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <div className="w-10 h-10 bg-slate-100 rounded-xl flex items-center justify-center text-slate-500">
                    <CreditCard size={18} />
                  </div>
                  <div>
                    <p className="text-sm font-black text-slate-900">{formatCustomerPaymentStatusLabel(order.paymentStatus)}</p>
                    <p className="text-[11px] font-bold text-slate-400">{order.payment?.paymentCode || order.payment?.providerInvoiceNumber || 'Đang cập nhật payment code'}</p>
                  </div>
                </div>
                <div className="rounded-2xl bg-slate-50 px-4 py-3">
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Refund hiện tại</p>
                  <p className="text-sm font-black text-slate-900">{formatCustomerRefundStatusLabel(order.refundStatus)}</p>
                  <p className="text-[11px] font-bold text-slate-400 mt-1">
                    {order.refundedAmount > 0
                      ? `Đã hoàn ${formatCurrency(order.refundedAmount, order.currencyCode)}`
                      : 'Chưa phát sinh khoản refund nào trên đơn này'}
                  </p>
                </div>
                {order.vatInvoiceRequested ? (
                  <div className="rounded-2xl bg-blue-50 p-4 text-[11px] font-bold text-blue-700">
                    Đơn hàng này đã yêu cầu xuất hóa đơn VAT.
                  </div>
                ) : null}
              </div>

              {Array.isArray(order.refunds) && order.refunds.length > 0 ? (
                <div className="bg-amber-50 rounded-3xl p-5 border border-amber-100">
                  <p className="text-xs font-black text-amber-900 uppercase tracking-wider mb-3">Lịch sử hoàn tiền</p>
                  <div className="space-y-3">
                    {order.refunds.map((item) => (
                      <div key={item.id} className="rounded-2xl bg-white/70 p-4">
                        <p className="text-sm font-black text-slate-900">{item.refundCode}</p>
                        <p className="text-[11px] font-bold text-slate-500 mt-1">{item.reasonText || item.reasonCode}</p>
                        <p className="text-[11px] font-bold text-amber-700 mt-2">
                          {formatCurrency(item.requestedAmount, item.currencyCode)} • {formatCustomerRefundStatusLabel(item.status)}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              ) : null}

              <div className="bg-amber-50 rounded-3xl p-5 border border-amber-100 flex flex-col gap-4">
                <div className="flex gap-4">
                  <AlertCircle className="text-amber-500 shrink-0" size={20} />
                  <div>
                    <h4 className="text-xs font-black text-amber-900 uppercase tracking-wider mb-1">Lưu ý marketplace</h4>
                    <p className="text-[11px] text-amber-700 font-medium leading-relaxed">
                      Nền tảng thu tiền và quản lý trạng thái giao dịch trung tâm. Tenant chỉ nhìn thấy phần doanh thu và đơn hàng thuộc phạm vi của họ.
                    </p>
                  </div>
                </div>
                {showAfterSales ? (
                  <Link to={`/my-account/bookings/${order.orderCode}/cancel`} className="w-full py-3 bg-white text-rose-500 border border-rose-200 rounded-xl text-[10px] font-black uppercase tracking-widest text-center hover:bg-rose-50 transition-all">
                    Hủy / hoàn đơn hàng này
                  </Link>
                ) : null}
                {Number(order.paymentStatus) === 2 ? (
                  <Link to={`/ticket/success?orderCode=${encodeURIComponent(order.orderCode)}`} className="w-full py-3 bg-slate-900 text-white rounded-xl text-[10px] font-black uppercase tracking-widest text-center hover:bg-[#1EB4D4] transition-all">
                    Xem vé điện tử / voucher
                  </Link>
                ) : null}
                <Link
                  to={`/support?tab=create&orderCode=${encodeURIComponent(order.orderCode)}&category=${encodeURIComponent('Thanh toán & hoàn tiền')}&subject=${encodeURIComponent(`Hỗ trợ sau bán cho đơn ${order.orderCode}`)}&content=${encodeURIComponent('Khách cần hỗ trợ theo dõi payment / refund / ticket cho đơn hàng này.')}`}
                  className="w-full py-3 bg-white text-slate-700 border border-slate-200 rounded-xl text-[10px] font-black uppercase tracking-widest text-center hover:border-[#1EB4D4] hover:text-[#1EB4D4] transition-all"
                >
                  Gửi support cho đơn này
                </Link>
              </div>

              <div className="bg-white rounded-3xl p-6 border border-slate-100 shadow-sm">
                <div className="flex gap-4">
                  <ShieldCheck className="text-[#1EB4D4] shrink-0" size={20} />
                  <div>
                    <h4 className="text-xs font-black text-slate-900 uppercase tracking-wider mb-1">Theo dõi hậu mãi rõ hơn</h4>
                    <p className="text-[11px] text-slate-500 font-medium leading-relaxed">
                      Timeline ở trên lấy trực tiếp từ payment, ticket, refund và support event của đơn này, nên bạn có thể theo dõi từ lúc tạo đơn cho tới khi hoàn tiền mà không phải đoán trạng thái.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
