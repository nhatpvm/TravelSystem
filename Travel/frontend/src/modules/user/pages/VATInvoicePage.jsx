import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { AlertCircle, CheckCircle2, Eye, FileText } from 'lucide-react';
import { Link } from 'react-router-dom';
import {
  createCustomerVatInvoice,
  listCustomerOrders,
  listCustomerVatInvoices,
} from '../../../services/customerCommerceService';
import {
  CUSTOMER_PAYMENT_STATUS,
  CUSTOMER_TICKET_STATUS,
  formatCustomerVatStatusLabel,
  getCustomerVatStatusClass,
} from '../../booking/utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

function buildInitialForm(orderCode = '') {
  return {
    orderCode,
    companyName: '',
    taxCode: '',
    companyAddress: '',
    invoiceEmail: '',
    notes: '',
  };
}

export default function VATInvoicePage() {
  const [tab, setTab] = useState('list');
  const [items, setItems] = useState([]);
  const [eligibleOrders, setEligibleOrders] = useState([]);
  const [form, setForm] = useState(buildInitialForm());
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const [invoiceResponse, orderResponse] = await Promise.all([
        listCustomerVatInvoices(),
        listCustomerOrders({ page: 1, pageSize: 100 }),
      ]);

      const invoices = Array.isArray(invoiceResponse) ? invoiceResponse : [];
      const orders = Array.isArray(orderResponse?.items) ? orderResponse.items : [];

      const activeOrderCodes = new Set(
        invoices
          .filter((item) => Number(item.status || 0) !== 3)
          .map((item) => item.orderCode),
      );

      setItems(invoices);
      setEligibleOrders(
        orders.filter((item) => (
          Number(item.paymentStatus || 0) !== CUSTOMER_PAYMENT_STATUS.PENDING
          && !activeOrderCodes.has(item.orderCode)
        )),
      );
    } catch (requestError) {
      setItems([]);
      setEligibleOrders([]);
      setError(requestError.message || 'Không thể tải dữ liệu hóa đơn VAT.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  const selectedOrder = useMemo(
    () => eligibleOrders.find((item) => item.orderCode === form.orderCode) || null,
    [eligibleOrders, form.orderCode],
  );

  async function handleSubmit(event) {
    event.preventDefault();

    if (!form.orderCode || !form.companyName || !form.taxCode || !form.companyAddress || !form.invoiceEmail) {
      setError('Vui lòng nhập đầy đủ thông tin doanh nghiệp và chọn đơn hàng.');
      return;
    }

    setSubmitting(true);
    setError('');
    setSuccess('');

    try {
      await createCustomerVatInvoice({
        orderCode: form.orderCode,
        companyName: form.companyName.trim(),
        taxCode: form.taxCode.trim(),
        companyAddress: form.companyAddress.trim(),
        invoiceEmail: form.invoiceEmail.trim(),
        notes: form.notes.trim() || undefined,
      });

      setSuccess('Đã gửi yêu cầu xuất hóa đơn VAT.');
      setForm(buildInitialForm());
      setTab('list');
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể gửi yêu cầu xuất hóa đơn VAT.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-1">
          <FileText size={14} className="text-[#1EB4D4]" />
          <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">VAT & Invoice</span>
        </div>
        <h1 className="text-3xl font-black text-slate-900">Hóa đơn VAT</h1>
        <p className="text-slate-400 text-sm mt-1">Gửi yêu cầu và theo dõi trạng thái hóa đơn điện tử cho các đơn hàng đã thanh toán.</p>
      </div>

      <div className="flex bg-white rounded-2xl p-1 border border-slate-100 shadow-sm gap-1 w-fit">
        {[
          { key: 'list', label: 'Danh sách hóa đơn' },
          { key: 'request', label: 'Tạo yêu cầu mới' },
        ].map((item) => (
          <button
            key={item.key}
            type="button"
            onClick={() => setTab(item.key)}
            className={`px-5 py-3 rounded-xl text-xs font-black uppercase tracking-widest transition-all ${
              tab === item.key ? 'bg-slate-900 text-white shadow-md' : 'text-slate-400 hover:text-slate-700'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {error ? (
        <div className="rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="rounded-[1.75rem] border border-emerald-100 bg-emerald-50 px-6 py-4 text-sm font-bold text-emerald-700">
          {success}
        </div>
      ) : null}

      {loading ? (
        <div className="bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
          Đang tải dữ liệu hóa đơn VAT...
        </div>
      ) : null}

      {!loading && tab === 'list' ? (
        items.length === 0 ? (
          <div className="bg-white rounded-[2.5rem] p-12 text-center shadow-xl shadow-slate-100/60">
            <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center mx-auto mb-4">
              <FileText size={28} className="text-slate-300" />
            </div>
            <h3 className="text-xl font-black text-slate-900 mb-2">Chưa có yêu cầu hóa đơn VAT</h3>
            <p className="text-slate-400 text-sm mb-6">Bạn có thể tạo yêu cầu sau khi đơn hàng đã thanh toán thành công.</p>
            <button type="button" onClick={() => setTab('request')} className="inline-flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
              Tạo yêu cầu đầu tiên
            </button>
          </div>
        ) : (
          <div className="space-y-3">
            {items.map((item) => (
              <div key={item.id} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 flex flex-col md:flex-row items-start md:items-center gap-4 hover:shadow-md transition-all">
                <div className="w-10 h-10 bg-slate-100 rounded-xl flex items-center justify-center shrink-0">
                  <FileText size={18} className="text-slate-600" />
                </div>
                <div className="flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="font-black text-slate-900 text-sm">{item.requestCode}</p>
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-xl text-[10px] font-black uppercase ${getCustomerVatStatusClass(item.status)}`}>
                      {formatCustomerVatStatusLabel(item.status)}
                    </span>
                  </div>
                  <p className="text-xs text-slate-500 font-bold mt-0.5">
                    {item.companyName} • MST: {item.taxCode}
                  </p>
                  <p className="text-[10px] text-slate-400 font-bold">
                    {item.orderCode} • {formatDateTime(item.createdAt)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-black text-slate-900">{item.invoiceNumber || 'Chưa xuất số hóa đơn'}</p>
                  {item.pdfUrl ? (
                    <a href={item.pdfUrl} target="_blank" rel="noreferrer" className="mt-1 inline-flex items-center gap-1 text-[#1EB4D4] text-[10px] font-black uppercase hover:underline">
                      <Eye size={10} /> Xem PDF
                    </a>
                  ) : null}
                </div>
              </div>
            ))}
          </div>
        )
      ) : null}

      {!loading && tab === 'request' ? (
        <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
          <form onSubmit={handleSubmit} className="space-y-5">
            <h2 className="font-black text-slate-900 text-lg">Tạo yêu cầu xuất hóa đơn VAT</h2>

            <label className="space-y-2 block">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Đơn hàng đã thanh toán</span>
              <select value={form.orderCode} onChange={(event) => setForm((current) => ({ ...current, orderCode: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all">
                <option value="">Chọn đơn hàng</option>
                {eligibleOrders.map((item) => (
                  <option key={item.orderCode} value={item.orderCode}>
                    {item.orderCode} - {formatCurrency(item.payableAmount, item.currencyCode)}
                  </option>
                ))}
              </select>
            </label>

            {selectedOrder ? (
              <div className="rounded-2xl bg-slate-50 p-4 text-sm font-bold text-slate-600">
                Đơn hàng đã chọn: <span className="text-slate-900">{selectedOrder.orderCode}</span> • Giá trị {formatCurrency(selectedOrder.payableAmount, selectedOrder.currencyCode)} • {Number(selectedOrder.ticketStatus || 0) === CUSTOMER_TICKET_STATUS.ISSUED ? 'Đã phát hành vé' : 'Đã thanh toán'}
              </div>
            ) : null}

            {[
              { label: 'Tên công ty', key: 'companyName', placeholder: 'Công ty TNHH ABC' },
              { label: 'Mã số thuế', key: 'taxCode', placeholder: '0123456789' },
              { label: 'Địa chỉ công ty', key: 'companyAddress', placeholder: '123 Đường ABC, Quận 1, TP.HCM' },
              { label: 'Email nhận hóa đơn', key: 'invoiceEmail', placeholder: 'ketoan@company.vn' },
            ].map((field) => (
              <label key={field.key} className="space-y-2 block">
                <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{field.label}</span>
                <input
                  type="text"
                  value={form[field.key]}
                  onChange={(event) => setForm((current) => ({ ...current, [field.key]: event.target.value }))}
                  placeholder={field.placeholder}
                  className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all"
                />
              </label>
            ))}

            <label className="space-y-2 block">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Ghi chú thêm</span>
              <textarea
                rows={3}
                value={form.notes}
                onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
                placeholder="Ví dụ: vui lòng xuất theo đúng tên đăng ký doanh nghiệp"
                className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all resize-none"
              />
            </label>

            <div className="flex items-start gap-3 p-4 bg-amber-50 rounded-2xl">
              <AlertCircle size={16} className="text-amber-600 shrink-0 mt-0.5" />
              <p className="text-xs text-amber-700 font-medium">
                Hóa đơn sẽ được xử lý theo thông tin doanh nghiệp bạn cung cấp. Luồng xét duyệt và xuất hóa đơn được admin nền tảng quản lý tập trung.
              </p>
            </div>

            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={submitting || eligibleOrders.length === 0} className="px-8 py-4 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl hover:-translate-y-0.5 transition-all disabled:opacity-60 disabled:hover:translate-y-0">
                {submitting ? 'Đang gửi yêu cầu...' : 'Gửi yêu cầu'}
              </button>
              <Link to="/my-account/bookings" className="px-8 py-4 bg-slate-900 text-white rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl hover:bg-slate-700 transition-all">
                Xem đơn hàng
              </Link>
            </div>

            {eligibleOrders.length === 0 ? (
              <p className="text-xs font-bold text-slate-400">
                Hiện chưa có đơn hàng đủ điều kiện để tạo yêu cầu VAT mới.
              </p>
            ) : null}
          </form>
        </div>
      ) : null}
    </motion.div>
  );
}
