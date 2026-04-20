import React, { useEffect, useMemo, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { AlertTriangle, ArrowLeft, Info, RefreshCw, ShieldCheck } from 'lucide-react';
import {
  cancelCustomerOrder,
  getCustomerOrder,
  getCustomerRefundEstimate,
  requestCustomerRefund,
} from '../../../services/customerCommerceService';
import {
  canCancelPendingOrder,
  canRequestRefund,
  getOrderSnapshot,
} from '../../booking/utils/customerCommerce';
import { formatCurrency } from '../../tenant/train/utils/presentation';

const REASONS = [
  'Thay đổi lịch trình cá nhân',
  'Không thể tham gia đúng thời gian',
  'Muốn chuyển sang dịch vụ khác',
  'Cần hủy theo chính sách doanh nghiệp',
  'Lý do khác',
];

function toAmountInput(value) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '';
  }

  return String(Math.max(0, Number(value)));
}

export default function CancelBookingPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(false);
  const [order, setOrder] = useState(null);
  const [estimate, setEstimate] = useState(null);
  const [refundAmountInput, setRefundAmountInput] = useState('');
  const [estimateLoading, setEstimateLoading] = useState(false);
  const [estimateError, setEstimateError] = useState('');
  const [initialLoading, setInitialLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) {
      setInitialLoading(false);
      setError('Thiếu mã đơn hàng.');
      return;
    }

    let active = true;

    getCustomerOrder(id)
      .then((response) => {
        if (active) {
          setOrder(response);
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải đơn hàng.');
        }
      })
      .finally(() => {
        if (active) {
          setInitialLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [id]);

  const snapshot = useMemo(() => getOrderSnapshot(order), [order]);
  const canCancel = canCancelPendingOrder(order);
  const canRefund = canRequestRefund(order);
  const actionLabel = canCancel ? 'Hủy đơn hàng' : 'Yêu cầu hoàn tiền';
  const requestedRefundAmount = useMemo(() => {
    const parsed = Number(refundAmountInput);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      return undefined;
    }

    return parsed;
  }, [refundAmountInput]);

  useEffect(() => {
    if (!order) {
      return;
    }

    if (!canCancel && !canRefund) {
      setEstimate(null);
      return;
    }

    let active = true;
    setEstimateLoading(true);
    setEstimateError('');

    getCustomerRefundEstimate(order.orderCode, canRefund ? { requestedAmount: requestedRefundAmount } : {})
      .then((response) => {
        if (!active) {
          return;
        }

        setEstimate(response);
        if (canRefund && !refundAmountInput) {
          setRefundAmountInput(toAmountInput(response?.suggestedAmount));
        }
      })
      .catch((requestError) => {
        if (active) {
          setEstimate(null);
          setEstimateError(requestError.message || 'Không thể tải ước tính hoàn tiền.');
        }
      })
      .finally(() => {
        if (active) {
          setEstimateLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [order, canCancel, canRefund, requestedRefundAmount, refundAmountInput]);

  async function handleConfirm() {
    if (!order) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      if (canCancel) {
        await cancelCustomerOrder(order.orderCode);
      } else if (canRefund) {
        const amountToRequest = estimate?.suggestedAmount ?? requestedRefundAmount ?? order.payableAmount;
        await requestCustomerRefund(order.orderCode, {
          requestedAmount: amountToRequest,
          reasonCode: 'CUSTOMER_REQUEST',
          reasonText: reason,
        });
      } else {
        throw new Error('Đơn hàng này hiện chưa hỗ trợ hủy hoặc hoàn tiền từ phía khách hàng.');
      }

      setStep(3);
    } catch (requestError) {
      setError(requestError.message || 'Không thể xử lý yêu cầu.');
    } finally {
      setLoading(false);
    }
  }

  if (initialLoading) {
    return (
      <div className="bg-white rounded-[2.5rem] p-10 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
        Đang tải đơn hàng...
      </div>
    );
  }

  if (error && !order) {
    return (
      <div className="bg-rose-50 rounded-[2.5rem] p-10 text-center text-sm font-bold text-rose-600 shadow-xl shadow-slate-100/60 border border-rose-100">
        {error}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Link to={`/my-account/bookings/${id}`} className="inline-flex items-center gap-2 text-slate-500 hover:text-[#1EB4D4] font-bold text-sm transition-colors">
        <ArrowLeft size={16} /> Quay lại chi tiết đơn hàng
      </Link>

      <div className="flex items-center justify-between mb-6 px-4">
        {[1, 2, 3].map((currentStep) => (
          <div key={currentStep} className="flex flex-col items-center gap-2">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-xs transition-all ${step >= currentStep ? 'bg-[#1EB4D4] text-white shadow-lg shadow-sky-500/30' : 'bg-slate-200 text-slate-400'}`}>
              {step > currentStep ? '✓' : currentStep}
            </div>
            <span className={`text-[10px] uppercase font-black tracking-widest ${step >= currentStep ? 'text-slate-900' : 'text-slate-400'}`}>
              {currentStep === 1 ? 'Lý do' : currentStep === 2 ? 'Xác nhận' : 'Hoàn tất'}
            </span>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-[2.5rem] shadow-xl shadow-slate-200/50 p-10 overflow-hidden">
        {step === 1 && (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }}>
            <h2 className="text-2xl font-black text-slate-900 mb-2">{actionLabel}</h2>
            <p className="text-slate-500 text-sm mb-8">Chọn lý do để hệ thống ghi nhận luồng hậu mãi cho đơn hàng này.</p>

            <div className="space-y-3 mb-10">
              {REASONS.map((item) => (
                <button
                  key={item}
                  type="button"
                  onClick={() => setReason(item)}
                  className={`w-full flex items-center justify-between p-5 rounded-2xl border-2 transition-all ${reason === item ? 'border-[#1EB4D4] bg-sky-50/50' : 'border-slate-100 hover:border-slate-200'}`}
                >
                  <span className={`font-bold text-sm ${reason === item ? 'text-[#1EB4D4]' : 'text-slate-600'}`}>{item}</span>
                  <div className={`w-5 h-5 rounded-full border-2 flex items-center justify-center ${reason === item ? 'border-[#1EB4D4] bg-[#1EB4D4]' : 'border-slate-300'}`}>
                    {reason === item && <div className="w-2 h-2 bg-white rounded-full" />}
                  </div>
                </button>
              ))}
            </div>

            <div className="flex gap-4">
              <Link to={`/my-account/bookings/${id}`} className="flex-1 py-4 text-center rounded-2xl font-black text-xs uppercase tracking-widest text-slate-400 hover:text-slate-600 transition-colors">
                Quay lại
              </Link>
              <button type="button" disabled={!reason} onClick={() => setStep(2)} className={`flex-1 py-4 rounded-2xl font-black text-xs uppercase tracking-widest transition-all ${reason ? 'bg-slate-900 text-white shadow-xl hover:-translate-y-0.5' : 'bg-slate-100 text-slate-300 cursor-not-allowed'}`}>
                Tiếp theo
              </button>
            </div>
          </motion.div>
        )}

        {step === 2 && order && (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }}>
            <div className="flex items-center gap-4 p-6 bg-rose-50 rounded-3xl border border-rose-100 mb-8">
              <AlertTriangle className="text-rose-500" size={32} />
              <div>
                <h2 className="text-lg font-black text-rose-900 leading-tight">{actionLabel}</h2>
                <p className="text-rose-600/70 text-xs font-bold mt-0.5">Mã đơn hàng: {order.orderCode}</p>
              </div>
            </div>

            <div className="bg-slate-50 rounded-3xl p-6 mb-8 space-y-4">
              <div className="flex justify-between items-center py-2 border-b border-slate-200/50">
                <span className="text-sm font-bold text-slate-500">Dịch vụ</span>
                <span className="text-sm font-black text-slate-900">{snapshot?.title || order.orderCode}</span>
              </div>
              <div className="flex justify-between items-center py-2 border-b border-slate-200/50">
                <span className="text-sm font-bold text-slate-500">Đơn vị / gói</span>
                <span className="text-sm font-black text-slate-900">{snapshot?.subtitle || snapshot?.providerName || 'Đối tác nền tảng'}</span>
              </div>
              <div className="flex justify-between items-center py-2 border-b border-slate-200/50">
                <span className="text-sm font-bold text-slate-500">Giá trị đơn hàng</span>
                <span className="text-sm font-black text-slate-900">{formatCurrency(order.payableAmount, order.currencyCode)}</span>
              </div>
              <div className="flex justify-between items-center pt-2">
                <span className="text-sm font-black text-[#1EB4D4]">Luồng xử lý</span>
                <span className="text-xl font-black text-[#1EB4D4]">{canCancel ? 'Hủy đơn pending' : 'Hoàn tiền sau thanh toán'}</span>
              </div>
            </div>

            {canRefund ? (
              <div className="bg-white rounded-3xl border border-slate-100 p-6 mb-8 shadow-sm">
                <div className="flex items-center gap-3 mb-4">
                  <Info className="text-[#1EB4D4]" size={18} />
                  <h3 className="text-sm font-black text-slate-900 uppercase tracking-widest">Ước tính hoàn tiền</h3>
                </div>

                <div className="grid md:grid-cols-2 gap-6 mb-6">
                  <div>
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Số tiền muốn yêu cầu hoàn</label>
                    <input
                      type="number"
                      min="0"
                      step="1000"
                      value={refundAmountInput}
                      onChange={(event) => setRefundAmountInput(event.target.value)}
                      className="mt-2 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-black text-slate-900 focus:border-[#1EB4D4]/30 focus:bg-white focus:ring-0 transition-all"
                    />
                    <p className="text-[11px] font-bold text-slate-400 mt-2">
                      Còn có thể hoàn tối đa {formatCurrency(estimate?.remainingRefundableAmount || 0, order.currencyCode)}
                    </p>
                  </div>
                  <div className="rounded-2xl bg-slate-50 border border-slate-100 p-4">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Kết quả dự kiến</p>
                    <div className="space-y-2 text-sm">
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-slate-500">Customer nhận lại</span>
                        <span className="font-black text-slate-900">{formatCurrency(estimate?.estimatedRefundAmount || 0, order.currencyCode)}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-slate-500">Commission hoàn lại</span>
                        <span className="font-black text-slate-900">{formatCurrency(estimate?.estimatedCommissionReversalAmount || 0, order.currencyCode)}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-slate-500">Tenant bị điều chỉnh</span>
                        <span className="font-black text-slate-900">{formatCurrency(estimate?.estimatedTenantAdjustmentAmount || 0, order.currencyCode)}</span>
                      </div>
                    </div>
                  </div>
                </div>

                {estimateLoading ? (
                  <div className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-500">
                    Đang tính lại ước tính refund...
                  </div>
                ) : null}

                {estimateError ? (
                  <div className="rounded-2xl border border-rose-100 bg-rose-50 px-4 py-3 text-sm font-bold text-rose-600">
                    {estimateError}
                  </div>
                ) : null}

                {estimate ? (
                  <>
                    <div className="rounded-2xl bg-amber-50 border border-amber-100 p-4 mb-4">
                      <p className="text-sm font-black text-amber-900">{estimate.timingNote}</p>
                      <p className="text-[11px] font-medium text-amber-700 mt-2 leading-relaxed">{estimate.settlementImpact}</p>
                      <p className="text-[11px] font-medium text-amber-700 mt-2 leading-relaxed">{estimate.statusNote}</p>
                    </div>

                    <div className="grid md:grid-cols-2 gap-4">
                      <div className="rounded-2xl bg-slate-50 border border-slate-100 p-4">
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Rule minh bạch</p>
                        <div className="space-y-2">
                          {(estimate.ruleSummary || []).map((item) => (
                            <p key={item} className="text-[11px] font-medium text-slate-600 leading-relaxed">{item}</p>
                          ))}
                        </div>
                      </div>
                      <div className="rounded-2xl bg-rose-50 border border-rose-100 p-4">
                        <p className="text-[10px] font-black text-rose-500 uppercase tracking-widest mb-2">Lưu ý trước khi gửi</p>
                        <div className="space-y-2">
                          {(estimate.warningMessages || []).map((item) => (
                            <p key={item} className="text-[11px] font-medium text-rose-600 leading-relaxed">{item}</p>
                          ))}
                        </div>
                      </div>
                    </div>
                  </>
                ) : null}
              </div>
            ) : null}

            <div className="p-6 bg-amber-50 rounded-3xl border border-amber-100 mb-10">
              <div className="flex items-center gap-3 mb-2">
                <Info className="text-amber-500" size={20} />
                <h3 className="text-sm font-black text-amber-900">Lưu ý tài chính</h3>
              </div>
              <p className="text-[11px] text-amber-700/80 font-medium leading-relaxed">
                Admin/platform là bên quản lý dòng tiền tổng. Nếu đơn đã thanh toán, yêu cầu của bạn sẽ đi vào luồng hoàn tiền và ảnh hưởng tới doanh thu đối soát của tenant theo rule marketplace đã chốt.
              </p>
            </div>

            {error ? (
              <div className="mb-6 rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
                {error}
              </div>
            ) : null}

            <div className="flex gap-4">
              <button type="button" onClick={() => setStep(1)} className="flex-1 py-4 text-center rounded-2xl font-black text-xs uppercase tracking-widest text-slate-400 hover:text-slate-600 transition-colors">
                Thay đổi lý do
              </button>
              <button type="button" onClick={handleConfirm} disabled={loading || estimateLoading} className="flex-1 py-4 bg-rose-500 text-white rounded-2xl font-black text-xs uppercase tracking-widest shadow-xl shadow-rose-500/20 hover:bg-rose-600 transition-all flex items-center justify-center disabled:opacity-60">
                {loading ? <RefreshCw className="animate-spin" size={18} /> : actionLabel}
              </button>
            </div>
          </motion.div>
        )}

        {step === 3 && order && (
          <motion.div initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 1 }} className="text-center py-10">
            <div className="w-24 h-24 bg-emerald-100 text-emerald-500 rounded-full flex items-center justify-center mx-auto mb-8 shadow-inner">
              <ShieldCheck size={48} />
            </div>
            <h2 className="text-3xl font-black text-slate-900 mb-4">Đã ghi nhận yêu cầu!</h2>
            <p className="text-slate-500 text-sm mb-10 max-w-sm mx-auto leading-relaxed">
              Đơn hàng <strong>{order.orderCode}</strong> đã được chuyển sang luồng xử lý sau bán/hoàn tiền tương ứng.
            </p>

            <div className="bg-slate-50 rounded-[2.5rem] p-8 mb-10 text-left">
              <p className="text-lg font-black text-slate-900 mb-3">Trạng thái tiếp theo</p>
              <p className="text-[13px] font-medium text-slate-600 leading-relaxed">
                Bạn có thể quay lại trang chi tiết đơn hàng để theo dõi timeline mới. Khi có thay đổi về payment / refund / support, hệ thống sẽ đồng thời ghi nhận notification trong tài khoản của bạn.
              </p>
            </div>

            <div className="space-y-4">
              <button type="button" onClick={() => navigate(`/my-account/bookings/${order.orderCode}`)} className="w-full py-4 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest shadow-xl hover:bg-[#1EB4D4] transition-all">
                Về trang chi tiết đơn hàng
              </button>
              <Link
                to={`/support?tab=create&orderCode=${encodeURIComponent(order.orderCode)}&category=${encodeURIComponent('Thanh toán & hoàn tiền')}&subject=${encodeURIComponent(`Theo dõi hoàn tiền cho đơn ${order.orderCode}`)}&content=${encodeURIComponent('Khách muốn support theo dõi trạng thái hủy / refund / settlement liên quan tới đơn hàng này.')}`}
                className="block w-full py-4 bg-white text-slate-700 border border-slate-200 rounded-2xl font-black text-xs uppercase tracking-widest shadow-sm hover:border-[#1EB4D4] hover:text-[#1EB4D4] transition-all"
              >
                Gửi support theo dõi refund
              </Link>
              <p className="text-[11px] font-bold text-slate-400">Nền tảng sẽ tiếp tục đồng bộ trạng thái hoàn / hủy cho customer, tenant và admin.</p>
            </div>
          </motion.div>
        )}
      </div>
    </div>
  );
}
