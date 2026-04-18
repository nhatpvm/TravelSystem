import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  CheckCircle,
  CreditCard,
  Lock,
  Shield,
  ShieldCheck,
  Wallet,
} from 'lucide-react';
import {
  getSupportedPaymentMethods,
  listCustomerPayments,
} from '../../../services/customerCommerceService';
import {
  formatCustomerPaymentStatusLabel,
  getCustomerPaymentStatusClass,
  isPaidOrder,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

export default function PaymentsPage() {
  const [methods, setMethods] = useState([]);
  const [badges, setBadges] = useState([]);
  const [recentPayments, setRecentPayments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    async function loadData() {
      setLoading(true);
      setError('');

      try {
        const [methodsResponse, paymentsResponse] = await Promise.all([
          getSupportedPaymentMethods(),
          listCustomerPayments(),
        ]);

        if (!active) {
          return;
        }

        setMethods(Array.isArray(methodsResponse?.methods) ? methodsResponse.methods : []);
        setBadges(Array.isArray(methodsResponse?.securityBadges) ? methodsResponse.securityBadges : []);
        setRecentPayments(Array.isArray(paymentsResponse) ? paymentsResponse.slice(0, 4) : []);
      } catch (requestError) {
        if (!active) {
          return;
        }

        setMethods([]);
        setBadges([]);
        setRecentPayments([]);
        setError(requestError.message || 'Không thể tải thông tin thanh toán.');
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    loadData();

    return () => {
      active = false;
    };
  }, []);

  const paidCount = useMemo(
    () => recentPayments.filter((item) => Number(item.paymentStatus || 0) === 2).length,
    [recentPayments],
  );

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <CreditCard size={14} className="text-[#1EB4D4]" />
            <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Trung tâm thanh toán</span>
          </div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">Thanh toán</h1>
          <p className="text-slate-400 text-sm font-medium mt-1">
            Bạn thanh toán qua nền tảng, admin quản lý dòng tiền tổng và nhà cung cấp chỉ xem phần doanh thu liên quan tới đơn của họ.
          </p>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div className="bg-slate-50 rounded-2xl px-5 py-4 min-w-[130px]">
            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Cổng mặc định</p>
            <p className="text-lg font-black text-slate-900 mt-2">SePay</p>
          </div>
          <div className="bg-slate-50 rounded-2xl px-5 py-4 min-w-[130px]">
            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Thanh toán gần đây</p>
            <p className="text-lg font-black text-slate-900 mt-2">{paidCount} thành công</p>
          </div>
        </div>
      </div>

      {error ? (
        <div className="rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
        {loading ? (
          <div className="lg:col-span-2 bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
            Đang tải cấu hình thanh toán...
          </div>
        ) : methods.length === 0 ? (
          <div className="lg:col-span-2 bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-500 shadow-xl shadow-slate-100/60">
            Chưa có phương thức thanh toán khả dụng.
          </div>
        ) : methods.map((method, index) => (
          <motion.div
            key={method.code}
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: index * 0.05 }}
            className={`relative p-8 rounded-[2rem] overflow-hidden shadow-2xl border ${
              method.isDefault
                ? 'bg-gradient-to-br from-slate-800 via-slate-900 to-slate-950 text-white border-slate-900'
                : 'bg-white border-slate-100 text-slate-900'
            }`}
          >
            <div className={`absolute -top-10 -right-10 w-40 h-40 rounded-full blur-3xl ${method.isDefault ? 'bg-[#1EB4D4]/20' : 'bg-slate-100'}`} />
            <div className="relative z-10">
              <div className="flex items-start justify-between mb-10">
                <div>
                  <p className="font-black text-xl tracking-tight">{method.name}</p>
                  <p className={`text-[10px] font-bold uppercase tracking-[0.2em] mt-1 ${method.isDefault ? 'text-white/50' : 'text-slate-400'}`}>{method.code}</p>
                </div>
                <div className={`w-12 h-12 rounded-2xl flex items-center justify-center ${method.isDefault ? 'bg-white/10 text-[#1EB4D4]' : 'bg-slate-50 text-[#1EB4D4]'}`}>
                  {method.code === 'sepay' ? <Wallet size={24} /> : <CreditCard size={24} />}
                </div>
              </div>

              <p className={`text-sm font-medium leading-relaxed ${method.isDefault ? 'text-white/70' : 'text-slate-500'}`}>
                {method.description}
              </p>

              <div className="mt-8 flex items-center justify-between">
                <div>
                  <p className={`text-[9px] font-black uppercase tracking-widest ${method.isDefault ? 'text-white/40' : 'text-slate-400'}`}>Mô hình thu tiền</p>
                  <p className={`text-sm font-black mt-1 ${method.isDefault ? 'text-white' : 'text-slate-900'}`}>Nền tảng thu tiền / đối soát cho nhà cung cấp</p>
                </div>
                {method.isDefault ? (
                  <span className="inline-flex items-center gap-1 px-3 py-1.5 bg-emerald-400/20 text-emerald-300 rounded-xl text-[10px] font-black uppercase tracking-widest">
                    <CheckCircle size={12} /> Mặc định
                  </span>
                ) : null}
              </div>
            </div>
          </motion.div>
        ))}
      </div>

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-6">
          <Wallet size={16} className="text-[#1EB4D4]" />
          <h2 className="text-sm font-black text-slate-900 uppercase tracking-widest">Giao dịch gần đây</h2>
        </div>

        {recentPayments.length === 0 ? (
          <div className="rounded-[2rem] bg-slate-50 p-8 text-sm font-bold text-slate-500">
            Chưa có giao dịch thanh toán nào trong tài khoản của bạn.
          </div>
        ) : (
          <div className="space-y-3">
            {recentPayments.map((payment) => (
              <div key={payment.paymentId} className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 p-5 rounded-[2rem] bg-slate-50 border border-slate-100">
                <div>
                  <p className="text-sm font-black text-slate-900">{payment.title}</p>
                  <p className="text-[11px] font-bold text-slate-400 mt-1">{payment.paymentCode} • {payment.orderCode}</p>
                  <p className="text-[11px] font-bold text-slate-400 mt-1">{formatDateTime(payment.createdAt)}</p>
                </div>
                <div className="flex items-center gap-4">
                  <span className={`inline-flex items-center gap-1 px-3 py-1.5 rounded-xl text-[10px] font-black uppercase tracking-widest ${getCustomerPaymentStatusClass(payment.paymentStatus)}`}>
                    {formatCustomerPaymentStatusLabel(payment.paymentStatus)}
                  </span>
                  <div className="text-right">
                    <p className="font-black text-slate-900">{formatCurrency(payment.amount, payment.currencyCode)}</p>
                    <p className="text-[10px] font-bold text-slate-400">{isPaidOrder(payment) ? 'Đã đối soát vào đơn hàng' : 'Đang chờ đồng bộ'}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="relative rounded-[2rem] overflow-hidden bg-gradient-to-r from-emerald-50 to-teal-50 border border-emerald-100 p-8">
        <div className="relative z-10 flex items-start gap-6">
          <div className="w-14 h-14 bg-white rounded-2xl flex items-center justify-center text-emerald-500 shadow-xl shadow-emerald-200/50 shrink-0">
            <ShieldCheck size={28} strokeWidth={1.5} />
          </div>
          <div>
            <h3 className="font-black text-emerald-900 text-lg mb-1">Bảo mật thanh toán</h3>
            <p className="text-emerald-700/80 text-sm font-medium leading-relaxed max-w-xl">
              Hệ thống không lưu thẻ cục bộ trên frontend. Thanh toán được xử lý qua SePay và backend chỉ xác nhận đơn khi nhận được trạng thái thành công từ cổng thanh toán.
            </p>
            <div className="flex flex-wrap gap-3 mt-4">
              {(badges.length > 0 ? badges : ['Webhook xác thực', 'Tiền về tài khoản platform', 'Đối soát theo tenant']).map((badge) => (
                <span key={badge} className="flex items-center gap-1 px-3 py-1.5 bg-white text-emerald-700 rounded-xl text-[10px] font-black uppercase tracking-widest shadow-sm border border-emerald-100">
                  <Lock size={10} /> {badge}
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>
    </motion.div>
  );
}
