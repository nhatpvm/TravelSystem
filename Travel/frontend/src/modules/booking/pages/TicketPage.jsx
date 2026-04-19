import React, { useEffect, useMemo, useState } from 'react';
import {
  CheckCircle2,
  Download,
  MapPin,
  Calendar,
  Clock,
  User,
  ShieldCheck,
  Share2,
  Printer,
  Info,
} from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getCustomerOrder, getCustomerTicket } from '../../../services/customerCommerceService';
import {
  formatCustomerProductLabel,
  getOrderSnapshot,
} from '../utils/customerCommerce';
import { formatCurrency, formatDateTime } from '../../tenant/train/utils/presentation';

export default function TicketPage() {
  const [searchParams] = useSearchParams();
  const orderCode = searchParams.get('orderCode') || '';
  const [order, setOrder] = useState(null);
  const [ticket, setTicket] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!orderCode) {
      setLoading(false);
      setError('Thiếu mã đơn hàng để hiển thị vé.');
      return;
    }

    let active = true;
    setLoading(true);
    setError('');

    Promise.all([
      getCustomerOrder(orderCode),
      getCustomerTicket(orderCode),
    ])
      .then(([orderResponse, ticketResponse]) => {
        if (!active) {
          return;
        }

        setOrder(orderResponse);
        setTicket(ticketResponse);
      })
      .catch((requestError) => {
        if (!active) {
          return;
        }

        setError(requestError.message || 'Không thể tải vé điện tử.');
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [orderCode]);

  const snapshot = useMemo(() => getOrderSnapshot(order), [order]);
  const ticketSnapshot = ticket?.snapshot || {};
  const title = ticket?.title || snapshot?.title || order?.orderCode || 'Vé điện tử';
  const subtitle = ticket?.subtitle || snapshot?.subtitle || snapshot?.providerName || '';
  const departureText = snapshot?.departureAt ? formatDateTime(snapshot.departureAt) : '--';
  const arrivalText = snapshot?.arrivalAt ? formatDateTime(snapshot.arrivalAt) : '--';

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-4xl">
          {loading ? (
            <div className="bg-white rounded-[3rem] shadow-xl p-12 text-center text-sm font-bold text-slate-400 border border-slate-100">
              Đang tải vé điện tử...
            </div>
          ) : error ? (
            <div className="bg-rose-50 rounded-[3rem] shadow-sm p-12 text-center text-sm font-bold text-rose-600 border border-rose-100">
              {error}
            </div>
          ) : (
            <>
              <div className="flex flex-col md:flex-row items-center justify-between gap-8 mb-12">
                <div className="flex items-center gap-6">
                  <div className="w-16 h-16 bg-green-500 text-white rounded-[2rem] flex items-center justify-center shadow-xl shadow-green-500/20">
                    <CheckCircle2 size={32} />
                  </div>
                  <div>
                    <h1 className="text-3xl font-black text-slate-900">Đặt dịch vụ thành công!</h1>
                    <p className="text-slate-500 font-medium">Mã đơn hàng: <span className="text-blue-600 font-bold">#{order?.orderCode}</span></p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <button type="button" className="p-4 bg-white text-slate-400 hover:text-blue-600 rounded-2xl shadow-sm border border-slate-100 transition-all"><Share2 size={20} /></button>
                  <button type="button" className="p-4 bg-white text-slate-400 hover:text-blue-600 rounded-2xl shadow-sm border border-slate-100 transition-all"><Printer size={20} /></button>
                  <button type="button" className="px-8 py-4 bg-slate-900 text-white rounded-2xl font-bold flex items-center gap-2 shadow-xl hover:bg-blue-600 transition-all">
                    <Download size={20} /> Tải thông tin vé
                  </button>
                </div>
              </div>

              <div className="bg-white rounded-[4rem] shadow-2xl shadow-slate-200 overflow-hidden border border-slate-100 flex flex-col md:flex-row relative">
                <div className="md:w-72 bg-slate-900 text-white p-12 flex flex-col items-center justify-center relative">
                  <div className="absolute -top-6 -right-6 w-12 h-12 bg-slate-50 rounded-full z-10" />
                  <div className="absolute -bottom-6 -right-6 w-12 h-12 bg-slate-50 rounded-full z-10" />

                  <div className="bg-white p-4 rounded-3xl mb-8 shadow-2xl">
                    <img
                      src={`https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(ticket?.ticketCode || order?.orderCode || '')}`}
                      alt="Ticket QR"
                      className="w-32 h-32"
                    />
                  </div>
                  <p className="text-[10px] font-black text-blue-400 uppercase tracking-[0.3em] mb-1">Mã vé điện tử</p>
                  <p className="text-2xl font-black tracking-tighter text-center">{ticket?.ticketCode || order?.orderCode}</p>

                  <div className="mt-12 flex items-center gap-2 text-[10px] font-bold text-slate-500 italic">
                    <ShieldCheck size={14} /> Xác thực bởi nền tảng
                  </div>
                </div>

                <div className="flex-1 p-12 relative">
                  <div className="flex justify-between items-start mb-12 gap-6">
                    <div>
                      <h3 className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Dịch vụ</h3>
                      <div className="flex items-center gap-4 flex-wrap">
                        <p className="text-2xl font-black text-slate-900">{title}</p>
                      </div>
                      <p className="text-sm font-bold text-slate-500 mt-3">{subtitle}</p>
                    </div>
                    <div className="text-right">
                      <h3 className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Loại sản phẩm</h3>
                      <p className="font-black text-slate-900">{formatCustomerProductLabel(order?.productType)}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 md:grid-cols-3 gap-10">
                    <div>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><Calendar size={12} /> Khởi hành / nhận dịch vụ</p>
                      <p className="font-bold text-slate-900">{departureText}</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><Clock size={12} /> Hoàn tất / đến nơi</p>
                      <p className="font-bold text-slate-900">{arrivalText}</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><User size={12} /> Người đặt</p>
                      <p className="font-bold text-slate-900">{order?.contactFullName}</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Tổng tiền</p>
                      <p className="font-black text-blue-600 text-xl">{formatCurrency(order?.payableAmount || 0, order?.currencyCode || 'VND')}</p>
                    </div>
                    <div className="md:col-span-2">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><MapPin size={12} /> Ghi chú vận hành</p>
                      <p className="text-xs font-bold text-slate-600">{snapshot?.ticketNote || ticketSnapshot?.ticketNote || 'Vui lòng xuất trình mã đơn hàng hoặc mã vé khi làm thủ tục với đối tác.'}</p>
                    </div>
                  </div>

                  <div className="my-10 h-px w-full border-t border-dashed border-slate-200" />

                  <div className="flex items-center justify-between gap-6">
                    <div>
                      <p className="text-xs font-black text-slate-900">{snapshot?.providerName || 'Đối tác cung cấp dịch vụ'}</p>
                      <p className="text-[10px] text-slate-400 font-bold uppercase tracking-widest">Phát hành lúc {ticket?.issuedAt ? formatDateTime(ticket.issuedAt) : '--'}</p>
                    </div>
                    <Link to="/my-account/bookings" className="text-xs font-black text-slate-900 uppercase tracking-widest hover:text-blue-600 border-b-2 border-slate-900 hover:border-blue-600 transition-all">
                      Xem tất cả đơn của tôi
                    </Link>
                  </div>
                </div>
              </div>

              <div className="mt-12 p-6 bg-blue-50/50 rounded-3xl border border-blue-100 flex items-center gap-6">
                <div className="w-12 h-12 bg-white text-blue-600 rounded-2xl flex items-center justify-center shadow-sm">
                  <Info size={24} />
                </div>
                <div className="flex-1">
                  <p className="text-xs font-bold text-slate-600 leading-relaxed">
                    Bạn đã nhận được xác nhận thanh toán thành công. Tenant tương ứng và admin hệ thống đã nhìn thấy giao dịch này trong dashboard tài chính/đơn hàng theo đúng phạm vi dữ liệu.
                  </p>
                  <Link
                    to={`/support?tab=create&orderCode=${encodeURIComponent(order?.orderCode || orderCode)}&category=${encodeURIComponent('Vé xe / tàu / máy bay')}&subject=${encodeURIComponent(`Hỗ trợ vé / voucher cho đơn ${order?.orderCode || orderCode}`)}&content=${encodeURIComponent('Khách cần hỗ trợ hậu mãi liên quan tới vé hoặc voucher đã phát hành.')}`}
                    className="inline-flex mt-4 px-4 py-2 bg-white text-blue-600 rounded-2xl text-[10px] font-black uppercase tracking-widest hover:bg-blue-600 hover:text-white transition-all"
                  >
                    Báo support về vé / voucher
                  </Link>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
