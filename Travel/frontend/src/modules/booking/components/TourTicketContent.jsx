import React, { useEffect, useMemo, useState } from 'react';
import { Ticket, Download, Share2, Printer, MapPin, Calendar, Clock, User, ShieldCheck, CheckCircle2, Info } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { getTourBooking, getPublicTourById } from '../../../services/tourService';
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  formatTime,
  getBookingStatusClass,
  getBookingStatusLabel,
} from '../../tours/utils/presentation';

export default function TourTicketContent() {
  const [searchParams] = useSearchParams();
  const tourId = searchParams.get('tourId') || '';
  const bookingId = searchParams.get('bookingId') || '';
  const [tour, setTour] = useState(null);
  const [booking, setBooking] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    async function loadData() {
      if (!tourId || !bookingId) {
        setError('Thiếu thông tin booking tour.');
        setLoading(false);
        return;
      }

      setLoading(true);
      setError('');

      try {
        const [bookingResponse, tourResponse] = await Promise.all([
          getTourBooking(tourId, bookingId),
          getPublicTourById(tourId).catch(() => null),
        ]);

        if (!active) {
          return;
        }

        setBooking(bookingResponse);
        setTour(tourResponse);
      } catch (err) {
        if (!active) {
          return;
        }

        setBooking(null);
        setTour(null);
        setError(err.message || 'Không thể tải vé tour.');
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
  }, [bookingId, tourId]);

  const bookingStatusClass = useMemo(
    () => getBookingStatusClass(booking?.status),
    [booking?.status],
  );

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-4xl">
          <div className="bg-white rounded-[2.5rem] p-10 text-center text-sm font-bold text-slate-400 shadow-sm border border-slate-100">
            Đang tải vé tour...
          </div>
        </div>
      </div>
    );
  }

  if (error || !booking) {
    return (
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-4xl">
          <div className="bg-rose-50 rounded-[2.5rem] p-10 text-center text-sm font-bold text-rose-600 shadow-sm border border-rose-100">
            {error || 'Không tìm thấy booking tour.'}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 pt-32 pb-20">
      <div className="container mx-auto px-4 max-w-4xl">
        <div className="flex flex-col md:flex-row items-center justify-between gap-8 mb-12">
          <div className="flex items-center gap-6">
            <div className="w-16 h-16 bg-green-500 text-white rounded-[2rem] flex items-center justify-center shadow-xl shadow-green-500/20">
              <CheckCircle2 size={32} />
            </div>
            <div>
              <h1 className="text-3xl font-black text-slate-900">Đặt tour thành công!</h1>
              <p className="text-slate-500 font-medium">
                Mã đơn hàng: <span className="text-blue-600 font-bold">#{booking.code}</span>
              </p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <button type="button" className="p-4 bg-white text-slate-400 hover:text-blue-600 rounded-2xl shadow-sm border border-slate-100 transition-all">
              <Share2 size={20} />
            </button>
            <button type="button" className="p-4 bg-white text-slate-400 hover:text-blue-600 rounded-2xl shadow-sm border border-slate-100 transition-all">
              <Printer size={20} />
            </button>
            <button type="button" className="px-8 py-4 bg-slate-900 text-white rounded-2xl font-bold flex items-center gap-2 shadow-xl hover:bg-blue-600 transition-all">
              <Download size={20} /> Tải xác nhận
            </button>
          </div>
        </div>

        <div className="bg-white rounded-[4rem] shadow-2xl shadow-slate-200 overflow-hidden border border-slate-100 flex flex-col md:flex-row relative">
          <div className="md:w-72 bg-slate-900 text-white p-12 flex flex-col items-center justify-center relative">
            <div className="absolute -top-6 -right-6 w-12 h-12 bg-slate-50 rounded-full z-10" />
            <div className="absolute -bottom-6 -right-6 w-12 h-12 bg-slate-50 rounded-full z-10" />

            <div className="w-40 h-40 rounded-3xl bg-white flex items-center justify-center mb-8 shadow-2xl">
              <Ticket size={72} className="text-slate-900" />
            </div>
            <p className="text-[10px] font-black text-blue-400 uppercase tracking-[0.3em] mb-1">Mã booking tour</p>
            <p className="text-2xl font-black tracking-tighter text-center break-all">{booking.code}</p>

            <div className="mt-12 flex items-center gap-2 text-[10px] font-bold text-slate-500 italic">
              <ShieldCheck size={14} /> Xác thực bởi 2TMNY
            </div>
          </div>

          <div className="flex-1 p-12 relative">
            <div className="flex justify-between items-start mb-12 gap-6">
              <div>
                <h3 className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Tour / gói dịch vụ</h3>
                <div className="space-y-2">
                  <p className="text-2xl font-black text-slate-900">{tour?.name || 'Tour du lịch'}</p>
                  <p className="text-sm font-bold text-slate-500">{booking.packageName || booking.packageCode}</p>
                </div>
              </div>
              <div className="text-right">
                <h3 className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Trạng thái</h3>
                <span className={`inline-flex items-center px-3 py-1.5 rounded-full text-[10px] font-black uppercase tracking-wider ${bookingStatusClass}`}>
                  {getBookingStatusLabel(booking.status)}
                </span>
              </div>
            </div>

            <div className="grid grid-cols-2 md:grid-cols-3 gap-10">
              <div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><Calendar size={12} /> Ngày đi</p>
                <p className="font-bold text-slate-900">{tour ? formatDate(tour.upcomingSchedules?.find((item) => item.id === booking.scheduleId)?.departureDate) : '--'}</p>
              </div>
              <div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><Clock size={12} /> Giờ khởi hành</p>
                <p className="font-bold text-slate-900">{tour ? formatTime(tour.upcomingSchedules?.find((item) => item.id === booking.scheduleId)?.departureTime) : '--:--'}</p>
              </div>
              <div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><User size={12} /> Số khách</p>
                <p className="font-bold text-slate-900">{booking.requestedPax} khách</p>
              </div>
              <div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Lịch khởi hành</p>
                <p className="font-black text-blue-600 text-xl">{booking.scheduleCode}</p>
              </div>
              <div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Xác nhận lúc</p>
                <p className="text-xs font-bold text-slate-600">{formatDateTime(booking.confirmedAt || booking.createdAt)}</p>
              </div>
              <div>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Tổng tiền</p>
                <p className="font-black text-slate-900 text-lg">{formatCurrency(booking.packageSubtotalAmount, booking.currencyCode)}</p>
              </div>
              <div className="md:col-span-2">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-1"><MapPin size={12} /> Điểm đến</p>
                <p className="text-xs font-bold text-slate-600">{tour?.city || tour?.province || 'Đơn vị tổ chức sẽ cập nhật chi tiết trong lịch trình.'}</p>
              </div>
            </div>

            <div className="my-10 h-px w-full border-t border-dashed border-slate-200" />

            <div className="flex items-center justify-between gap-6">
              <div className="flex items-center gap-4">
                <img
                  src={tour?.coverImageUrl || 'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&q=80&w=100'}
                  alt={tour?.name || 'Tour'}
                  className="w-12 h-12 rounded-xl object-cover"
                />
                <div>
                  <p className="text-xs font-black text-slate-900">Dịch vụ bởi {tour?.name || 'đơn vị tổ chức tour'}</p>
                  <p className="text-[10px] text-slate-400 font-bold uppercase tracking-widest">Xác nhận lúc {formatDateTime(booking.confirmedAt || booking.createdAt)}</p>
                </div>
              </div>
              <Link to={`/my-account/bookings/${booking.id}?tourId=${booking.tourId}`} className="text-xs font-black text-slate-900 uppercase tracking-widest hover:text-blue-600 border-b-2 border-slate-900 hover:border-blue-600 transition-all">
                Xem chi tiết booking
              </Link>
            </div>
          </div>
        </div>

        <div className="mt-12 p-6 bg-blue-50/50 rounded-3xl border border-blue-100 flex items-center gap-6">
          <div className="w-12 h-12 bg-white text-blue-600 rounded-2xl flex items-center justify-center shadow-sm">
            <Info size={24} />
          </div>
          <p className="text-xs font-bold text-slate-600 leading-relaxed">
            Xác nhận booking đã được lưu trong tài khoản của bạn. 2TMNY đóng vai trò nền tảng trung gian, và đơn vị tổ chức tour sẽ tiếp nhận vận hành chi tiết theo lịch khởi hành.
          </p>
        </div>
      </div>
    </div>
  );
}
