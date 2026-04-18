import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { Plane, ChevronLeft, Briefcase, ArrowRight, Heart, Info, ShieldCheck, CheckCircle, Clock } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getFlightOfferAncillaries, getFlightOfferDetails } from '../../../services/flightService';
import { addWishlistItem, deleteWishlistItem, listWishlistItems } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, formatDateTime, formatTime, getCabinClassLabel } from '../../tenant/flight/utils/presentation';

function getRoute(detail) {
  const segments = detail?.segments || [];
  const first = segments[0];
  const last = segments[segments.length - 1];

  return {
    fromCode: first?.from?.iataCode || first?.from?.code || '---',
    toCode: last?.to?.iataCode || last?.to?.code || '---',
    fromName: first?.from?.name || 'Sân bay đi',
    toName: last?.to?.name || 'Sân bay đến',
    departureAt: first?.departureAt,
    arrivalAt: last?.arrivalAt,
  };
}

function getAncillaryPreview(items) {
  return items.filter((item) => item.isActive !== false).slice(0, 3);
}

function getDuration(detail) {
  const route = getRoute(detail);
  if (!route.departureAt || !route.arrivalAt) {
    return '--';
  }

  const durationMs = new Date(route.arrivalAt).getTime() - new Date(route.departureAt).getTime();
  if (Number.isNaN(durationMs) || durationMs <= 0) {
    return '--';
  }

  const totalMinutes = Math.round(durationMs / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return minutes ? `${hours}h ${minutes}m` : `${hours}h`;
}

const FlightDetailPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const session = useAuthSession();
  const [searchParams] = useSearchParams();
  const [detail, setDetail] = useState(null);
  const [ancillaries, setAncillaries] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [liked, setLiked] = useState(false);
  const [wishlistItemId, setWishlistItemId] = useState('');

  const offerId = searchParams.get('offerId') || '';

  useEffect(() => {
    if (!offerId) {
      setError('Không tìm thấy offer chuyến bay cần xem chi tiết.');
      setLoading(false);
      return undefined;
    }

    let active = true;
    setLoading(true);
    setError('');

    Promise.all([
      getFlightOfferDetails(offerId),
      getFlightOfferAncillaries(offerId),
    ])
      .then(([detailResponse, ancillaryResponse]) => {
        if (!active) {
          return;
        }

        setDetail(detailResponse);
        setAncillaries(Array.isArray(ancillaryResponse?.items) ? ancillaryResponse.items : []);
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải chi tiết chuyến bay.');
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
  }, [offerId]);

  useEffect(() => {
    if (!session.isAuthenticated || !offerId) {
      setLiked(false);
      setWishlistItemId('');
      return;
    }

    let active = true;

    listWishlistItems()
      .then((response) => {
        if (!active) {
          return;
        }

        const existing = (Array.isArray(response) ? response : []).find(
          (item) => Number(item.productType) === 3 && String(item.targetId || '') === String(offerId),
        );

        setLiked(!!existing);
        setWishlistItemId(existing?.id || '');
      })
      .catch(() => {
        if (active) {
          setLiked(false);
          setWishlistItemId('');
        }
      });

    return () => {
      active = false;
    };
  }, [offerId, session.isAuthenticated]);

  const route = useMemo(() => getRoute(detail), [detail]);
  const ancillaryPreview = useMemo(() => getAncillaryPreview(ancillaries), [ancillaries]);

  async function handleToggleWishlist() {
    if (!detail || !offerId) {
      return;
    }

    if (!session.isAuthenticated) {
      navigate('/auth/login', {
        state: {
          returnTo: `${location.pathname}${location.search}`,
        },
      });
      return;
    }

    if (wishlistItemId) {
      try {
        await deleteWishlistItem(wishlistItemId);
        setWishlistItemId('');
        setLiked(false);
      } catch {
        // Keep current UI state when the request fails.
      }
      return;
    }

    try {
      const created = await addWishlistItem({
        productType: 'flight',
        targetId: offerId,
        title: `${route.fromCode} - ${route.toCode}`,
        subtitle: detail?.airline?.name || 'Hãng bay',
        locationText: `${route.fromName} → ${route.toName}`,
        priceValue: detail?.offer?.totalPrice || undefined,
        priceText: formatCurrency(detail?.offer?.totalPrice || 0, detail?.offer?.currencyCode),
        currencyCode: detail?.offer?.currencyCode || 'VND',
        imageUrl: 'https://images.unsplash.com/photo-1436491865332-7a61a109cc05?auto=format&fit=crop&q=80&w=2000',
        targetUrl: `${location.pathname}${location.search}`,
      });

      setWishlistItemId(created?.id || '');
      setLiked(true);
    } catch {
      // Keep current UI state when the request fails.
    }
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-24">
        <div className="h-[450px] relative flex items-center justify-center overflow-hidden bg-slate-900">
          <div className="absolute inset-0 opacity-40">
            <img
              src="https://images.unsplash.com/photo-1436491865332-7a61a109cc05?auto=format&fit=crop&q=80&w=2000"
              alt="Flight header"
              className="w-full h-full object-cover"
            />
          </div>
          <div className="absolute inset-0 bg-gradient-to-t from-slate-900 via-transparent to-slate-900/60" />

          <div className="container mx-auto px-4 relative z-10 text-center">
            <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} className="flex items-center justify-center gap-2 text-white/70 text-[10px] font-black uppercase tracking-[0.3em] mb-6">
              <Link to="/" className="hover:text-white transition-colors">Trang chủ</Link>
              <ChevronLeft size={12} />
              <Link to="/flight/results" className="hover:text-white transition-colors">Kết quả tìm kiếm</Link>
              <ChevronLeft size={12} className="rotate-180" />
              <span className="text-white">Chi tiết chuyến bay</span>
            </motion.div>

            <div className="flex flex-col md:flex-row items-center justify-center gap-8 md:gap-24 mb-10">
              <div className="text-center">
                <motion.h2 initial={{ opacity: 0, x: -30 }} animate={{ opacity: 1, x: 0 }} className="text-6xl md:text-8xl font-black text-white tracking-tighter">{route.fromCode}</motion.h2>
                <p className="text-sm font-black text-[#1EB4D4] uppercase tracking-widest mt-2">{route.fromName}</p>
              </div>

              <div className="flex flex-col items-center">
                <motion.div initial={{ width: 0 }} animate={{ width: 120 }} className="h-px bg-white/20 relative">
                  <Plane className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 text-[#1EB4D4] animate-pulse" size={24} />
                </motion.div>
                <p className="text-[10px] font-black text-white/40 uppercase tracking-[0.2em] mt-6">{getDuration(detail)} bay thẳng</p>
              </div>

              <div className="text-center">
                <motion.h2 initial={{ opacity: 0, x: 30 }} animate={{ opacity: 1, x: 0 }} className="text-6xl md:text-8xl font-black text-white tracking-tighter">{route.toCode}</motion.h2>
                <p className="text-sm font-black text-[#1EB4D4] uppercase tracking-widest mt-2">{route.toName}</p>
              </div>
            </div>

            <div className="flex flex-wrap items-center justify-center gap-6">
              <div className="flex items-center gap-3 px-6 py-2 bg-white/10 backdrop-blur-md rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-widest">
                {detail?.airline?.name || 'Hãng bay'}
              </div>
              <div className="flex items-center gap-3 px-6 py-2 bg-white/10 backdrop-blur-md rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-widest">
                {detail?.flight?.flightNumber || 'Chuyến bay'}
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-16 relative z-20">
          {loading ? (
            <div className="bg-white rounded-[3.5rem] p-12 shadow-xl shadow-slate-200/50 border border-slate-100 text-center text-sm font-bold text-slate-500">
              Đang tải chi tiết chuyến bay...
            </div>
          ) : error ? (
            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-rose-100 text-center text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
              <div className="lg:col-span-2 space-y-12">
                <div className="bg-white rounded-[3.5rem] p-12 shadow-xl shadow-slate-200/50 border border-slate-100">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-12">
                    <div className="space-y-2">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Giờ cất cánh</p>
                      <p className="text-4xl font-black text-slate-900 tracking-tighter">{formatTime(route.departureAt)}</p>
                      <p className="text-xs font-bold text-slate-500">{formatDateTime(route.departureAt)}</p>
                    </div>
                    <div className="flex flex-col items-center justify-center border-x border-slate-50 px-8">
                      <div className="w-12 h-12 bg-slate-50 rounded-2xl flex items-center justify-center text-[#1EB4D4] mb-3">
                        <Clock size={20} />
                      </div>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Thời gian bay</p>
                      <p className="text-sm font-black text-slate-900 mt-1">{getDuration(detail)}</p>
                    </div>
                    <div className="space-y-2 md:text-right">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Giờ hạ cánh</p>
                      <p className="text-4xl font-black text-slate-900 tracking-tighter">{formatTime(route.arrivalAt)}</p>
                      <p className="text-xs font-bold text-slate-500">{formatDateTime(route.arrivalAt)}</p>
                    </div>
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                    <div className="w-12 h-12 bg-[#1EB4D4] rounded-2xl flex items-center justify-center text-white"><ShieldCheck size={24} /></div>
                    Thông tin hạng vé
                  </h2>
                  <div className="rounded-[3rem] border-2 border-[#1EB4D4] bg-slate-50 p-8 flex flex-col md:flex-row gap-8">
                    <div className="flex-1 space-y-4">
                      <div>
                        <h3 className="text-2xl font-black text-slate-900 tracking-tight mb-2 uppercase">{detail?.fareClass?.name || 'Fare class'}</h3>
                        <p className="text-xs font-black text-[#1EB4D4] uppercase tracking-widest">{getCabinClassLabel(detail?.fareClass?.cabinClass)}</p>
                      </div>
                      <div className="flex flex-wrap gap-4">
                        <div className="flex items-center gap-2 px-4 py-2 bg-white rounded-xl shadow-sm border border-slate-50">
                          <CheckCircle size={14} className="text-[#1EB4D4]" />
                          <span className="text-[10px] font-black text-slate-600 uppercase tracking-widest">
                            {detail?.fareClass?.isRefundable ? 'Cho phép hoàn vé' : 'Không hoàn vé'}
                          </span>
                        </div>
                        <div className="flex items-center gap-2 px-4 py-2 bg-white rounded-xl shadow-sm border border-slate-50">
                          <CheckCircle size={14} className="text-[#1EB4D4]" />
                          <span className="text-[10px] font-black text-slate-600 uppercase tracking-widest">
                            {detail?.fareClass?.isChangeable ? 'Cho phép đổi vé' : 'Không đổi vé'}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div className="flex flex-col items-end justify-center shrink-0">
                      <div className="text-right">
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Giá mỗi khách</p>
                        <p className="text-3xl font-black text-slate-900 tracking-tighter">{formatCurrency(detail?.offer?.totalPrice || 0, detail?.offer?.currencyCode)}</p>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                    <div className="w-12 h-12 bg-slate-900 rounded-2xl flex items-center justify-center text-white"><Briefcase size={24} /></div>
                    Phụ phí & dịch vụ kèm theo
                  </h2>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    {detail?.taxFeeLines?.map((item) => (
                      <div key={item.id || `${item.code}-${item.sortOrder}`} className="p-8 bg-slate-50 rounded-[2.5rem] border border-transparent group hover:bg-white hover:border-[#1EB4D4]/10 transition-all">
                        <h4 className="text-sm font-black text-slate-900 uppercase tracking-widest mb-2">{item.name}</h4>
                        <p className="text-xs font-bold text-slate-500 leading-relaxed">{item.code} • {item.lineType}</p>
                        <p className="text-base font-black text-[#1EB4D4] mt-4">{formatCurrency(item.amount, item.currencyCode)}</p>
                      </div>
                    ))}
                    {detail?.taxFeeLines?.length === 0 ? <div className="md:col-span-2 p-8 bg-slate-50 rounded-[2.5rem] border border-dashed border-slate-200 text-center text-sm font-bold text-slate-500">Chưa có dòng thuế và phí riêng cho offer này.</div> : null}
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                    <div className="w-12 h-12 bg-amber-500 rounded-2xl flex items-center justify-center text-white"><Info size={24} /></div>
                    Gợi ý dịch vụ thêm
                  </h2>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                    {ancillaryPreview.map((item) => (
                      <div key={item.id} className="rounded-[2.5rem] border border-slate-100 bg-slate-50 p-6">
                        <p className="text-xs font-black text-[#1EB4D4] uppercase tracking-widest mb-2">{item.type}</p>
                        <p className="font-black text-slate-900">{item.name}</p>
                        <p className="text-xs font-bold text-slate-400 mt-2">{formatCurrency(item.price, item.currencyCode)}</p>
                      </div>
                    ))}
                    {ancillaryPreview.length === 0 ? <div className="md:col-span-3 rounded-[2.5rem] border border-dashed border-slate-200 bg-slate-50 p-6 text-sm font-bold text-slate-500">Offer này hiện chưa có ancillary đi kèm.</div> : null}
                  </div>
                </div>
              </div>

              <div className="lg:col-span-1">
                <div className="sticky top-32 space-y-8">
                  <div className="bg-white rounded-[3.5rem] p-10 shadow-2xl shadow-slate-200/60 border border-slate-100 relative overflow-hidden">
                    <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16 -z-0" />

                    <div className="relative z-10">
                      <button
                        type="button"
                        onClick={handleToggleWishlist}
                        className={`absolute right-0 top-0 w-12 h-12 rounded-2xl border transition-all flex items-center justify-center ${liked ? 'bg-rose-500 border-rose-500 text-white' : 'bg-white border-slate-100 text-slate-400 hover:border-rose-200 hover:text-rose-500'}`}
                      >
                        <Heart size={18} className={liked ? 'fill-white' : ''} />
                      </button>
                      <p className="text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2">Giá vé 01 khách</p>
                      <div className="flex items-baseline gap-2 mb-10">
                        <p className="text-5xl font-black text-[#1EB4D4] tracking-tighter">{formatCurrency(detail?.offer?.totalPrice || 0, detail?.offer?.currencyCode)}</p>
                      </div>

                      <div className="space-y-6 mb-10">
                        <div className="bg-slate-50 p-6 rounded-[2.5rem] border border-slate-100 space-y-4">
                          <div className="flex justify-between items-center text-xs gap-4">
                            <span className="font-bold text-slate-400 uppercase tracking-widest">Loại vé</span>
                            <span className="font-black text-slate-900 uppercase tracking-widest text-right">{detail?.fareClass?.name || 'Fare class'}</span>
                          </div>
                          <div className="flex justify-between items-center text-xs gap-4">
                            <span className="font-bold text-slate-400 uppercase tracking-widest">Ghế còn lại</span>
                            <span className="font-black text-slate-900 uppercase tracking-widest text-right">{detail?.offer?.seatsAvailable || 0}</span>
                          </div>
                          <div className="flex justify-between items-center text-xs gap-4">
                            <span className="font-bold text-slate-400 uppercase tracking-widest">Hết hạn offer</span>
                            <span className="font-black text-green-500 uppercase tracking-widest text-right">{formatDateTime(detail?.offer?.expiresAt)}</span>
                          </div>
                        </div>
                      </div>

                      <Link to={`/flight/seat-selection?offerId=${offerId}`} className="w-full h-16 bg-slate-900 text-white rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30">
                        Chọn ghế & tiếp tục <ArrowRight size={18} />
                      </Link>

                      <div className="mt-8 pt-8 border-t border-slate-50 flex items-center gap-3 text-[#1EB4D4]">
                        <ShieldCheck size={18} />
                        <p className="text-[10px] font-black uppercase tracking-widest">Dữ liệu chuyến bay lấy trực tiếp từ tenant</p>
                      </div>
                    </div>
                  </div>

                  <div className="bg-amber-50 p-10 rounded-[3rem] border border-amber-100 relative overflow-hidden">
                    <div className="flex items-center gap-4 mb-4">
                      <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center text-amber-500 shadow-sm"><Info size={24} /></div>
                      <p className="text-[10px] font-black text-amber-600 uppercase tracking-widest">Lưu ý quan trọng</p>
                    </div>
                    <p className="text-[11px] text-amber-900/60 font-bold leading-relaxed">
                      Vui lòng có mặt tại sân bay ít nhất 120 phút trước giờ khởi hành để làm thủ tục check-in và kiểm tra an ninh.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
};

export default FlightDetailPage;
