import React, { useEffect, useMemo, useState } from 'react';
import { User, Mail, Phone, Ticket, FileText, CheckCircle2, ChevronRight, ShieldCheck, Clock } from 'lucide-react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  confirmTourBooking,
  createTourReservation,
  getPublicTourById,
  quoteTour,
  releaseTourReservation,
} from '../../../services/tourService';
import {
  formatCurrency,
  formatDate,
  formatTime,
} from '../../tours/utils/presentation';

function buildPassengerGroups(adults, children) {
  const groups = [];

  if (adults > 0) {
    groups.push({ priceType: 1, quantity: adults });
  }

  if (children > 0) {
    groups.push({ priceType: 2, quantity: children });
  }

  return groups;
}

function buildPassengerCards(adults, children) {
  return [
    ...Array.from({ length: adults }, (_, index) => ({
      id: `adult-${index + 1}`,
      label: `Người lớn ${index + 1}`,
      type: 'Người lớn',
    })),
    ...Array.from({ length: children }, (_, index) => ({
      id: `child-${index + 1}`,
      label: `Trẻ em ${index + 1}`,
      type: 'Trẻ em',
    })),
  ];
}

export default function TourCheckoutContent() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const session = useAuthSession();
  const { user, isAuthenticated } = session;
  const tourId = searchParams.get('tourId') || '';
  const scheduleId = searchParams.get('scheduleId') || '';
  const packageId = searchParams.get('packageId') || '';
  const adults = Math.max(1, Number(searchParams.get('adult') || 2));
  const children = Math.max(0, Number(searchParams.get('child') || 0));
  const totalPax = adults + children;

  const [useVAT, setUseVAT] = useState(false);
  const [tour, setTour] = useState(null);
  const [quote, setQuote] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [contact, setContact] = useState({
    fullName: '',
    phone: '',
    email: '',
    notes: '',
    companyName: '',
    taxCode: '',
    companyAddress: '',
  });

  useEffect(() => {
    setContact((prev) => ({
      ...prev,
      fullName: user?.fullName || prev.fullName || '',
      phone: user?.phoneNumber || prev.phone || '',
      email: user?.email || prev.email || '',
    }));
  }, [user]);

  useEffect(() => {
    let active = true;

    async function loadCheckoutData() {
      if (!tourId || !scheduleId) {
        setError('Thiếu thông tin lịch khởi hành cho tour.');
        setLoading(false);
        return;
      }

      setLoading(true);
      setError('');

      try {
        const [tourResponse, quoteResponse] = await Promise.all([
          getPublicTourById(tourId),
          quoteTour(tourId, {
            scheduleId,
            packageId: packageId || undefined,
            includeDefaultAddons: true,
            includeDefaultPackageOptions: true,
            paxGroups: buildPassengerGroups(adults, children),
          }),
        ]);

        if (!active) {
          return;
        }

        setTour(tourResponse);
        setQuote(quoteResponse);
      } catch (err) {
        if (!active) {
          return;
        }

        setTour(null);
        setQuote(null);
        setError(err.message || 'Không thể tải dữ liệu đặt tour.');
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    loadCheckoutData();

    return () => {
      active = false;
    };
  }, [adults, children, packageId, scheduleId, tourId]);

  const selectedSchedule = useMemo(
    () => tour?.upcomingSchedules?.find((item) => item.id === scheduleId) || null,
    [tour, scheduleId],
  );

  const passengerCards = useMemo(
    () => buildPassengerCards(adults, children),
    [adults, children],
  );

  async function handleSubmit() {
    if (!tourId || !scheduleId) {
      setError('Thiếu thông tin tour để xác nhận đặt chỗ.');
      return;
    }

    if (!isAuthenticated) {
      navigate('/auth/login', {
        state: {
          returnTo: `${location.pathname}${location.search}`,
        },
      });
      return;
    }

    if (!contact.fullName.trim() || !contact.phone.trim() || !contact.email.trim()) {
      setError('Vui lòng nhập đầy đủ họ tên, số điện thoại và email liên hệ.');
      return;
    }

    setSubmitting(true);
    setError('');
    let reservationId = null;

    try {
      const notes = JSON.stringify({
        contact: {
          fullName: contact.fullName.trim(),
          phone: contact.phone.trim(),
          email: contact.email.trim(),
        },
        vat: useVAT
          ? {
            companyName: contact.companyName.trim(),
            taxCode: contact.taxCode.trim(),
            companyAddress: contact.companyAddress.trim(),
          }
          : null,
        passengerSummary: {
          adults,
          children,
          totalPax,
        },
        customerNote: contact.notes.trim() || null,
      });

      const reservationResponse = await createTourReservation(tourId, {
        scheduleId,
        packageId: packageId || quote?.package?.packageId || undefined,
        totalPax,
        includeDefaultPackageOptions: true,
        notes,
      });

      reservationId = reservationResponse.reservation?.id;
      if (!reservationId) {
        throw new Error('Không tạo được giữ chỗ tour.');
      }

      const bookingResponse = await confirmTourBooking(tourId, {
        reservationId,
        notes,
      });

      const bookingId = bookingResponse.booking?.id;
      if (!bookingId) {
        throw new Error('Không xác nhận được booking tour.');
      }

      navigate(`/ticket/success?type=tour&tourId=${tourId}&bookingId=${bookingId}`);
    } catch (err) {
      if (reservationId) {
        try {
          await releaseTourReservation(tourId, reservationId);
        } catch {
          // Keep primary error for the user.
        }
      }

      setError(err.message || 'Không thể xác nhận booking tour.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-6xl">
          <div className="bg-white rounded-[2.5rem] p-10 text-center text-sm font-bold text-slate-400 shadow-sm border border-slate-100">
            Đang tải dữ liệu thanh toán tour...
          </div>
        </div>
      </div>
    );
  }

  if (error && !tour) {
    return (
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-6xl">
          <div className="bg-rose-50 rounded-[2.5rem] p-10 text-center text-sm font-bold text-rose-600 shadow-sm border border-rose-100">
            {error}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 pt-32 pb-20">
      <div className="container mx-auto px-4 max-w-6xl">
        <h1 className="text-3xl font-black text-slate-900 mb-8">Xác nhận và Thanh toán</h1>

        <div className="flex flex-col lg:flex-row gap-12">
          <div className="flex-1 space-y-8">
            <section className="bg-white p-8 rounded-[2.5rem] shadow-sm border border-slate-100">
              <div className="flex items-center gap-3 mb-8">
                <div className="w-10 h-10 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center font-bold">1</div>
                <h2 className="text-xl font-bold text-slate-900">Thông tin liên hệ</h2>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-2">
                  <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Họ và tên</label>
                  <div className="relative">
                    <User className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                    <input
                      type="text"
                      value={contact.fullName}
                      onChange={(event) => setContact((prev) => ({ ...prev, fullName: event.target.value }))}
                      placeholder="Nhập họ và tên"
                      className="w-full pl-12 pr-4 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all"
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Số điện thoại</label>
                  <div className="relative">
                    <Phone className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                    <input
                      type="tel"
                      value={contact.phone}
                      onChange={(event) => setContact((prev) => ({ ...prev, phone: event.target.value }))}
                      placeholder="Nhập số điện thoại"
                      className="w-full pl-12 pr-4 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all"
                    />
                  </div>
                </div>
                <div className="md:col-span-2 space-y-2">
                  <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Email nhận xác nhận</label>
                  <div className="relative">
                    <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                    <input
                      type="email"
                      value={contact.email}
                      onChange={(event) => setContact((prev) => ({ ...prev, email: event.target.value }))}
                      placeholder="Nhập địa chỉ email"
                      className="w-full pl-12 pr-4 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all"
                    />
                  </div>
                </div>
                <div className="md:col-span-2 space-y-2">
                  <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Ghi chú cho đơn vị tổ chức</label>
                  <textarea
                    rows={3}
                    value={contact.notes}
                    onChange={(event) => setContact((prev) => ({ ...prev, notes: event.target.value }))}
                    placeholder="Ví dụ: cần hỗ trợ ăn chay, liên hệ trước giờ khởi hành..."
                    className="w-full px-5 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all resize-none"
                  />
                </div>
              </div>
            </section>

            <section className="bg-white p-8 rounded-[2.5rem] shadow-sm border border-slate-100">
              <div className="flex items-center justify-between mb-8">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center font-bold">2</div>
                  <h2 className="text-xl font-bold text-slate-900">Danh sách hành khách</h2>
                </div>
                <div className="text-xs font-black text-blue-600 bg-blue-50 px-4 py-2 rounded-xl">
                  {totalPax} khách
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {passengerCards.map((passenger) => (
                  <div key={passenger.id} className="p-5 bg-slate-50 rounded-3xl border border-slate-100">
                    <div className="flex items-center gap-3 mb-3">
                      <div className="w-10 h-10 bg-white text-slate-400 rounded-xl flex items-center justify-center shadow-sm">
                        <User size={18} />
                      </div>
                      <div>
                        <p className="font-black text-slate-900 text-sm">{passenger.label}</p>
                        <p className="text-[11px] font-bold text-slate-400">{passenger.type}</p>
                      </div>
                    </div>
                    <p className="text-xs font-medium text-slate-500 leading-relaxed">
                      Booking tour hiện đang xác nhận theo số lượng khách. Thông tin manifest chi tiết sẽ được đơn vị tổ chức tiếp nhận trong bước vận hành.
                    </p>
                  </div>
                ))}
              </div>
            </section>

            <section className="bg-white p-8 rounded-[3rem] shadow-sm border border-slate-100">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 bg-slate-50 text-slate-400 rounded-2xl flex items-center justify-center">
                    <FileText size={24} />
                  </div>
                  <div>
                    <h3 className="font-bold text-slate-900">Yêu cầu xuất hóa đơn VAT</h3>
                    <p className="text-xs text-slate-500 font-medium mt-1">Xuất hóa đơn điện tử cho doanh nghiệp</p>
                  </div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input type="checkbox" className="sr-only peer" checked={useVAT} onChange={() => setUseVAT(!useVAT)} />
                  <div className="w-14 h-8 bg-slate-100 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[4px] after:left-[4px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-6 after:w-6 after:transition-all peer-checked:bg-blue-600" />
                </label>
              </div>

              {useVAT && (
                <div className="mt-8 grid grid-cols-1 md:grid-cols-2 gap-6 animate-slide-down">
                  <input
                    type="text"
                    value={contact.companyName}
                    onChange={(event) => setContact((prev) => ({ ...prev, companyName: event.target.value }))}
                    placeholder="Tên công ty"
                    className="w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium"
                  />
                  <input
                    type="text"
                    value={contact.taxCode}
                    onChange={(event) => setContact((prev) => ({ ...prev, taxCode: event.target.value }))}
                    placeholder="Mã số thuế"
                    className="w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium"
                  />
                  <input
                    type="text"
                    value={contact.companyAddress}
                    onChange={(event) => setContact((prev) => ({ ...prev, companyAddress: event.target.value }))}
                    placeholder="Địa chỉ công ty"
                    className="md:col-span-2 w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium"
                  />
                </div>
              )}
            </section>
          </div>

          <aside className="w-full lg:w-96">
            <div className="space-y-6 sticky top-28">
              <div className="bg-white rounded-[3rem] shadow-xl border border-slate-100 p-8 overflow-hidden relative">
                <div className="absolute top-0 left-0 w-2 h-full bg-blue-600" />

                <h3 className="font-black text-slate-900 text-xl mb-6">Chi tiết thanh toán</h3>
                <div className="space-y-4 mb-8">
                  {quote?.passengerLines?.map((line) => (
                    <div key={`${line.code}-${line.name}`} className="flex justify-between items-start">
                      <span className="text-sm font-bold text-slate-500 italic">{line.name} x{line.quantity}</span>
                      <p className="font-black text-slate-900">{formatCurrency(line.lineBaseAmount, line.currencyCode)}</p>
                    </div>
                  ))}
                  <div className="flex justify-between items-start">
                    <span className="text-sm font-bold text-slate-500 italic">Phí dịch vụ</span>
                    <p className="font-black text-slate-900">{formatCurrency(quote?.feeAmount || 0, quote?.currencyCode || 'VND')}</p>
                  </div>
                  <div className="h-px bg-slate-50 w-full relative my-2">
                    <div className="absolute -left-10 -top-2 w-4 h-4 rounded-full bg-slate-50 border border-slate-100" />
                    <div className="absolute -right-10 -top-2 w-4 h-4 rounded-full bg-slate-50 border border-slate-100" />
                  </div>

                  <div className="pt-6 flex justify-between items-center">
                    <span className="font-black text-slate-900">Tổng cộng</span>
                    <p className="text-3xl font-black text-blue-600">
                      {formatCurrency(quote?.totalAmount || 0, quote?.currencyCode || 'VND')}
                    </p>
                  </div>
                </div>

                {error && (
                  <div className="mb-6 rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
                    {error}
                  </div>
                )}

                <button
                  type="button"
                  onClick={handleSubmit}
                  disabled={submitting || !tour || !quote}
                  className="w-full flex items-center justify-center gap-3 bg-blue-600 text-white py-5 rounded-[2rem] font-black text-lg shadow-2xl shadow-blue-500/40 hover:scale-105 transition-all disabled:opacity-60 disabled:hover:scale-100"
                >
                  {submitting ? 'Đang xác nhận booking...' : 'Xác nhận đặt tour'} <ChevronRight size={24} />
                </button>

                <div className="mt-6 flex flex-col gap-3">
                  <div className="flex items-start gap-2 text-[10px] text-slate-400 font-bold leading-tight">
                    <CheckCircle2 size={14} className="text-green-500 shrink-0" />
                    <p>Bằng cách xác nhận, bạn đồng ý với điều khoản và chính sách hiện hành của nền tảng và đơn vị tổ chức tour.</p>
                  </div>
                  <div className="flex items-center gap-2 text-[10px] text-slate-400 font-bold leading-tight">
                    <ShieldCheck size={14} className="text-blue-500 shrink-0" />
                    <p>Booking đi qua nền tảng trung gian 2TMNY</p>
                  </div>
                </div>
              </div>

              <div className="bg-slate-900 text-white rounded-[2.5rem] p-8 shadow-2xl relative overflow-hidden group">
                <div className="absolute top-[-20%] right-[-10%] w-32 h-32 bg-blue-500/20 rounded-full blur-2xl group-hover:bg-blue-500/30 transition-all" />
                <h4 className="font-black text-blue-400 text-xs uppercase tracking-widest mb-4">Thông tin chuyến đi</h4>
                <div className="space-y-4">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 bg-white/10 rounded-xl flex items-center justify-center">
                      <Ticket size={20} className="text-blue-400" />
                    </div>
                    <div>
                      <p className="font-bold text-sm">{tour?.name || 'Tour du lịch'}</p>
                      <p className="text-[10px] opacity-60 font-bold uppercase tracking-widest">{quote?.package?.packageName || quote?.schedule?.scheduleName || 'Gói mặc định'}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 bg-white/10 rounded-xl flex items-center justify-center">
                      <Clock size={20} className="text-blue-400" />
                    </div>
                    <div>
                      <p className="font-bold text-sm">{selectedSchedule ? formatDate(selectedSchedule.departureDate) : '--'}</p>
                      <p className="text-[10px] opacity-60 font-bold uppercase tracking-widest">
                        {selectedSchedule ? `${formatTime(selectedSchedule.departureTime)} - ${selectedSchedule.availableSlots || 0} chỗ còn lại` : 'Đang cập nhật lịch'}
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-blue-50 rounded-[2rem] border border-blue-100 p-5">
                <p className="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-2">Lưu ý</p>
                <p className="text-xs font-medium text-slate-600 leading-relaxed">
                  Nền tảng 2TMNY là bên trung gian kết nối khách hàng với đơn vị tổ chức. Sau khi booking thành công, thông tin xác nhận sẽ được lưu trong tài khoản của bạn và gửi qua email.
                </p>
                {!isAuthenticated && (
                  <Link
                    to="/auth/login"
                    state={{ returnTo: `${location.pathname}${location.search}` }}
                    className="mt-4 inline-flex items-center gap-2 text-xs font-black text-blue-600"
                  >
                    Đăng nhập để hoàn tất booking <ChevronRight size={14} />
                  </Link>
                )}
              </div>
            </div>
          </aside>
        </div>
      </div>
    </div>
  );
}
