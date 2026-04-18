import React, { useEffect, useMemo, useRef, useState } from 'react';
import { AlertCircle, ArrowLeft, CheckCircle2, Copy, CreditCard, ExternalLink, Info, RefreshCw } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import {
  getCustomerOrder,
  startCustomerPayment,
  syncCustomerPayment,
} from '../../../services/customerCommerceService';
import {
  CUSTOMER_ORDER_STATUS,
  CUSTOMER_PAYMENT_STATUS,
  formatCustomerPaymentStatusLabel,
  getCountdownText,
  getCustomerPaymentStatusClass,
  getOrderSnapshot,
  isPaidOrder,
} from '../utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

function copyText(value) {
  if (!value) {
    return;
  }

  navigator.clipboard?.writeText(String(value)).catch(() => {});
}

function getFinalPaymentHint(paymentStatus) {
  switch (Number(paymentStatus || 0)) {
    case CUSTOMER_PAYMENT_STATUS.EXPIRED:
      return 'Phiên thanh toán này đã hết hạn. Bạn vui lòng quay lại chi tiết đơn để tạo đơn mới từ dịch vụ gốc.';
    case CUSTOMER_PAYMENT_STATUS.CANCELLED:
      return 'Phiên thanh toán này đã bị hủy. Bạn vui lòng quay lại chi tiết đơn nếu cần đặt lại.';
    case CUSTOMER_PAYMENT_STATUS.FAILED:
      return 'Phiên thanh toán chưa thành công. Bạn vui lòng quay lại chi tiết đơn để tạo lại giao dịch mới.';
    default:
      return 'Đơn hàng đang ở trạng thái không thể khởi tạo thêm phiên thanh toán.';
  }
}

export default function PaymentPage() {
  const [searchParams] = useSearchParams();
  const orderCode = searchParams.get('orderCode') || '';
  const result = searchParams.get('result') || '';
  const formRef = useRef(null);
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [error, setError] = useState('');

  function canInitializePayment(detail) {
    return Number(detail?.paymentStatus || 0) === CUSTOMER_PAYMENT_STATUS.PENDING
      && Number(detail?.status || 0) === CUSTOMER_ORDER_STATUS.PENDING_PAYMENT
      && !detail?.payment?.checkoutForm;
  }

  async function loadOrder({ withSync = false } = {}) {
    if (!orderCode) {
      setOrder(null);
      setError('Thiếu mã đơn hàng để thanh toán.');
      setLoading(false);
      return;
    }

    try {
      setError('');

      const detail = withSync
        ? await syncCustomerPayment(orderCode)
        : await getCustomerOrder(orderCode);

      if (canInitializePayment(detail)) {
        try {
          const initialized = await startCustomerPayment(orderCode, {});
          setOrder(initialized);
          return;
        } catch (initError) {
          setOrder(detail);
          setError(initError.message || 'Không thể khởi tạo phiên thanh toán mới.');
          return;
        }
      }

      setOrder(detail);
    } catch (requestError) {
      setOrder(null);
      setError(requestError.message || 'Không thể tải thông tin thanh toán.');
    } finally {
      setLoading(false);
      setSyncing(false);
    }
  }

  useEffect(() => {
    setLoading(true);
    setError('');
    loadOrder({ withSync: result === 'success' || result === 'error' || result === 'cancel' });
  }, [orderCode, result]);

  useEffect(() => {
    if (!orderCode || isPaidOrder(order) || Number(order?.paymentStatus || 0) !== CUSTOMER_PAYMENT_STATUS.PENDING) {
      return undefined;
    }

    const timer = window.setInterval(() => {
      setSyncing(true);
      loadOrder({ withSync: true });
    }, 15000);

    return () => window.clearInterval(timer);
  }, [order, orderCode]);

  const payment = order?.payment || null;
  const snapshot = getOrderSnapshot(order);
  const checkoutForm = payment?.checkoutForm || null;
  const isPaid = isPaidOrder(order);
  const paymentStatus = Number(order?.paymentStatus || 0);
  const orderStatus = Number(order?.status || 0);
  const isPendingPayment = paymentStatus === CUSTOMER_PAYMENT_STATUS.PENDING
    && orderStatus === CUSTOMER_ORDER_STATUS.PENDING_PAYMENT;
  const countdownText = getCountdownText(payment?.expiresAt || order?.expiresAt);
  const paymentStatusClass = getCustomerPaymentStatusClass(order?.paymentStatus);
  const paymentStatusLabel = formatCustomerPaymentStatusLabel(order?.paymentStatus);
  const amount = payment?.amount ?? order?.payableAmount ?? 0;
  const currencyCode = payment?.currencyCode || order?.currencyCode || 'VND';
  const summaryLines = useMemo(() => (Array.isArray(snapshot?.lines) ? snapshot.lines : []), [snapshot]);
  const statusCardTitle = isPendingPayment ? 'Thời hạn thanh toán' : 'Trạng thái phiên';
  const statusCardValue = isPendingPayment
    ? countdownText
    : isPaid
      ? 'Đã xong'
      : paymentStatus === CUSTOMER_PAYMENT_STATUS.CANCELLED
        ? 'Đã hủy'
        : paymentStatus === CUSTOMER_PAYMENT_STATUS.EXPIRED
          ? 'Hết hạn'
          : paymentStatus === CUSTOMER_PAYMENT_STATUS.FAILED
            ? 'Thất bại'
            : '--';
  const statusCardNote = isPendingPayment
    ? (payment?.expiresAt ? formatDateTime(payment.expiresAt) : 'Đang cập nhật')
    : isPaid
      ? (payment?.paidAt || order?.paidAt ? `Xác nhận lúc ${formatDateTime(payment?.paidAt || order?.paidAt)}` : 'SePay đã xác nhận giao dịch')
      : paymentStatus === CUSTOMER_PAYMENT_STATUS.EXPIRED
        ? (payment?.expiresAt ? `Hết hạn lúc ${formatDateTime(payment.expiresAt)}` : 'Phiên thanh toán đã hết hạn')
        : 'Phiên thanh toán không còn hiệu lực';

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-5xl">
          <div className="flex items-center gap-4 mb-8">
            <Link
              to={orderCode ? `/my-account/bookings/${encodeURIComponent(orderCode)}` : '/my-account/bookings'}
              className="w-10 h-10 bg-white rounded-xl shadow-sm border border-slate-100 flex items-center justify-center text-slate-400 hover:text-slate-900 transition-all"
            >
              <ArrowLeft size={20} />
            </Link>
            <h1 className="text-2xl font-black text-slate-900">Thanh toán đơn hàng</h1>
          </div>

          {loading ? (
            <div className="bg-white p-12 rounded-[3rem] shadow-xl border border-slate-100 text-center text-sm font-bold text-slate-400">
              Đang tải phiên thanh toán...
            </div>
          ) : error && !order ? (
            <div className="bg-rose-50 p-12 rounded-[3rem] shadow-sm border border-rose-100 text-center text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
              <div className="bg-white p-10 rounded-[3rem] shadow-xl border border-slate-100 relative overflow-hidden">
                <div className="flex items-center justify-between gap-4 mb-8">
                  <div>
                    <p className="text-[10px] font-black text-blue-600 uppercase tracking-[0.25em] mb-2">SePay Gateway</p>
                    <h2 className="text-2xl font-black text-slate-900">Phiên thanh toán #{order?.orderCode}</h2>
                  </div>
                  <span className={`inline-flex items-center gap-2 px-3 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest ${paymentStatusClass}`}>
                    {isPaid ? <CheckCircle2 size={14} /> : syncing ? <RefreshCw size={14} className="animate-spin" /> : <CreditCard size={14} />}
                    {paymentStatusLabel}
                  </span>
                </div>

                {error ? (
                  <div className="mb-6 rounded-[2rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
                    {error}
                  </div>
                ) : null}

                <div className="space-y-5">
                  <div className="rounded-[2rem] bg-slate-50 border border-slate-100 p-6">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Dịch vụ</p>
                    <p className="text-xl font-black text-slate-900">{snapshot?.title || order?.orderCode}</p>
                    <p className="text-sm font-bold text-slate-500 mt-2">{snapshot?.subtitle || snapshot?.providerName || 'Đơn hàng trên nền tảng'}</p>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="rounded-[2rem] bg-slate-50 border border-slate-100 p-5">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Số tiền cần thanh toán</p>
                      <p className="text-3xl font-black text-blue-600">{formatCurrency(amount, currencyCode)}</p>
                    </div>
                    <div className="rounded-[2rem] bg-slate-50 border border-slate-100 p-5">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">{statusCardTitle}</p>
                      <p className="text-3xl font-black text-slate-900">{statusCardValue}</p>
                      <p className="text-[11px] font-bold text-slate-400 mt-2">{statusCardNote}</p>
                    </div>
                  </div>

                  <div className="rounded-[2rem] border border-blue-100 bg-blue-50 p-5">
                    <div className="flex items-start gap-3">
                      <Info size={18} className="text-blue-500 mt-0.5 shrink-0" />
                      <div className="text-sm font-medium text-slate-600 leading-relaxed">
                        Tiền được thu về tài khoản platform. Sau khi SePay xác nhận giao dịch, hệ thống sẽ tự động phát hành vé/voucher và cập nhật doanh thu cho đúng tenant.
                      </div>
                    </div>
                  </div>

                  {checkoutForm && isPendingPayment ? (
                    <form ref={formRef} action={checkoutForm.actionUrl} method="POST" className="space-y-4">
                      {checkoutForm.fields?.map((field) => (
                        <input key={field.name} type="hidden" name={field.name} value={field.value} readOnly />
                      ))}
                      <button type="submit" className="w-full h-16 bg-slate-900 text-white rounded-[1.75rem] font-black uppercase tracking-[0.2em] flex items-center justify-center gap-3 hover:bg-blue-600 transition-all shadow-xl">
                        Thanh toán trên SePay <ExternalLink size={18} />
                      </button>
                    </form>
                  ) : null}

                  {isPendingPayment ? (
                    <button
                      type="button"
                      onClick={() => {
                        setSyncing(true);
                        loadOrder({ withSync: true });
                      }}
                      disabled={syncing}
                      className="w-full h-14 bg-white border border-slate-100 text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-3 hover:border-blue-200 hover:text-blue-600 transition-all disabled:opacity-60"
                    >
                      {syncing ? <RefreshCw size={16} className="animate-spin" /> : <RefreshCw size={16} />}
                      Tôi đã thanh toán, kiểm tra lại
                    </button>
                  ) : null}

                  {!isPaid && !isPendingPayment ? (
                    <div className="rounded-[2rem] border border-amber-100 bg-amber-50 px-5 py-4 text-sm font-bold text-amber-700">
                      {getFinalPaymentHint(paymentStatus)}
                    </div>
                  ) : null}

                  {isPaid ? (
                    <Link to={`/ticket/success?orderCode=${encodeURIComponent(order.orderCode)}`} className="w-full h-14 bg-emerald-500 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-3 hover:bg-emerald-600 transition-all shadow-xl">
                      Xem vé / voucher <CheckCircle2 size={16} />
                    </Link>
                  ) : null}
                </div>
              </div>

              <div className="space-y-6">
                <div className="bg-white p-8 rounded-[2.5rem] shadow-sm border border-slate-100">
                  <h3 className="font-black text-slate-900 text-lg mb-6">Thông tin đối soát</h3>
                  <div className="space-y-5">
                    <div className="flex justify-between items-end">
                      <div>
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Mã thanh toán</p>
                        <p className="text-lg font-black text-slate-900">{payment?.paymentCode || '--'}</p>
                      </div>
                      <button type="button" onClick={() => copyText(payment?.paymentCode)} className="p-2 hover:bg-slate-50 text-slate-400 hover:text-blue-600 rounded-xl transition-all shadow-sm">
                        <Copy size={18} />
                      </button>
                    </div>
                    <div className="flex justify-between items-end">
                      <div>
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Mã invoice SePay</p>
                        <p className="text-lg font-black text-slate-900">{payment?.providerInvoiceNumber || '--'}</p>
                      </div>
                      <button type="button" onClick={() => copyText(payment?.providerInvoiceNumber)} className="p-2 hover:bg-slate-50 text-slate-400 hover:text-blue-600 rounded-xl transition-all shadow-sm">
                        <Copy size={18} />
                      </button>
                    </div>
                    {summaryLines.map((line) => (
                      <div key={`${line.label}-${line.quantity}`} className="flex justify-between items-start">
                        <span className="text-sm font-bold text-slate-500 italic">{line.label} x{line.quantity}</span>
                        <p className="font-black text-slate-900">{formatCurrency(line.lineAmount, currencyCode)}</p>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="bg-slate-900 p-8 rounded-[2.5rem] shadow-2xl relative overflow-hidden text-white">
                  <div className="absolute top-[-20%] right-[-10%] w-32 h-32 bg-blue-500/10 rounded-full blur-2xl" />
                  <div className="flex items-start gap-4">
                    <AlertCircle size={24} className="text-blue-400 bg-blue-400/10 p-1.5 rounded-lg shrink-0" />
                    <div>
                      <h4 className="font-bold text-white text-sm">Lưu ý giao dịch</h4>
                      <p className="text-xs text-slate-400 mt-2 leading-relaxed">
                        Hệ thống chỉ xác nhận đơn hàng khi backend nhận được trạng thái thanh toán thành công từ SePay. Đừng đóng trang quá sớm nếu bạn đang chờ hệ thống đồng bộ.
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
