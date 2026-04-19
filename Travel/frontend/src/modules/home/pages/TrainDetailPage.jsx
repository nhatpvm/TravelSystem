import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import {
  Train,
  MapPin,
  ChevronLeft,
  ArrowRight,
  Wifi,
  Coffee,
  ShieldCheck,
  Info,
  CheckCircle,
  Heart,
} from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getTrainTripDetail } from '../../../services/trainService';
import { addWishlistItem, deleteWishlistItem, listWishlistItems, trackRecentView } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  formatCurrency,
  formatDateTime,
  formatTime,
  getCarTypeLabel,
} from '../../tenant/train/utils/presentation';

function resolveRouteTitle(stops) {
  const first = stops.find((item) => item.isSelectedOrigin) || stops[0];
  const last = stops.find((item) => item.isSelectedDestination) || stops[stops.length - 1];

  return {
    from: first?.location?.name || first?.stopPoint?.name || 'Ga đi',
    to: last?.location?.name || last?.stopPoint?.name || 'Ga đến',
  };
}

function buildCarOption(car, basePrice, currency) {
  const sampleModifiers = Array.isArray(car?.sampleSeats)
    ? car.sampleSeats.map((item) => Number(item.priceModifier || 0))
    : [];
  const minModifier = sampleModifiers.length > 0 ? Math.min(...sampleModifiers) : 0;

  return {
    id: car.id,
    name: [getCarTypeLabel(car.carType), car.cabinClass].filter(Boolean).join(' • ') || `Toa ${car.carNumber}`,
    price: Number(basePrice || 0) + minModifier,
    seats: Number(car.seatCount || 0),
    avail: Number(car.availableSeatCount || 0),
    icon: car.carType === 2 ? '🛏️' : '💺',
    currency,
  };
}

export default function TrainDetailPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const session = useAuthSession();
  const [searchParams] = useSearchParams();
  const [detail, setDetail] = useState(null);
  const [selectedClass, setSelectedClass] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [liked, setLiked] = useState(false);
  const [wishlistItemId, setWishlistItemId] = useState('');

  const tripId = searchParams.get('tripId') || '';
  const fromTripStopTimeId = searchParams.get('fromTripStopTimeId') || '';
  const toTripStopTimeId = searchParams.get('toTripStopTimeId') || '';

  useEffect(() => {
    if (!tripId) {
      setError('Không tìm thấy chuyến tàu cần xem chi tiết.');
      setLoading(false);
      return undefined;
    }

    let active = true;
    setLoading(true);
    setError('');

    getTrainTripDetail(tripId, {
      fromTripStopTimeId: fromTripStopTimeId || undefined,
      toTripStopTimeId: toTripStopTimeId || undefined,
    })
      .then((response) => {
        if (active) {
          setDetail(response);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được chi tiết chuyến tàu.');
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
  }, [tripId, fromTripStopTimeId, toTripStopTimeId]);

  useEffect(() => {
    if (!session.isAuthenticated || !tripId) {
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
          (item) => Number(item.productType) === 2 && String(item.targetId || '') === String(tripId),
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
  }, [session.isAuthenticated, tripId]);

  useEffect(() => {
    if (!session.isAuthenticated || !detail || !tripId) {
      return;
    }

    trackRecentView({
      productType: 'train',
      targetId: tripId,
      title: `${routeTitle.from} - ${routeTitle.to}`,
      subtitle: detail?.provider?.name || 'Doi tac duong sat',
      locationText: `${routeTitle.from} -> ${routeTitle.to}`,
      priceValue: activeOption?.price || detail?.segment?.price || undefined,
      priceText: formatCurrency(activeOption?.price || detail?.segment?.price || 0, detail?.segment?.currency),
      currencyCode: detail?.segment?.currency || 'VND',
      imageUrl: 'https://images.unsplash.com/photo-1551009175-15bdf9dcb580?auto=format&fit=crop&q=80&w=2000',
      targetUrl: `${location.pathname}${location.search}`,
    }).catch(() => {});
  }, [activeOption?.price, detail, location.pathname, location.search, routeTitle.from, routeTitle.to, session.isAuthenticated, tripId]);

  const stops = detail?.stops || [];
  const routeTitle = resolveRouteTitle(stops);
  const carOptions = useMemo(
    () => (detail?.cars || []).map((item) => buildCarOption(item, detail?.segment?.price, detail?.segment?.currency)),
    [detail],
  );
  const activeOption = carOptions[selectedClass] || carOptions[0] || null;
  const seatSelectionQuery = new URLSearchParams({
    tripId: detail?.trip?.id || tripId,
    fromTripStopTimeId: detail?.segment?.fromTripStopTimeId || fromTripStopTimeId,
    toTripStopTimeId: detail?.segment?.toTripStopTimeId || toTripStopTimeId,
  }).toString();

  async function handleToggleWishlist() {
    if (!detail || !tripId) {
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
        productType: 'train',
        targetId: tripId,
        title: `${routeTitle.from} - ${routeTitle.to}`,
        subtitle: detail?.provider?.name || 'Đối tác đường sắt',
        locationText: `${routeTitle.from} → ${routeTitle.to}`,
        priceValue: activeOption?.price || detail?.segment?.price || undefined,
        priceText: formatCurrency(activeOption?.price || detail?.segment?.price || 0, detail?.segment?.currency),
        currencyCode: detail?.segment?.currency || 'VND',
        imageUrl: 'https://images.unsplash.com/photo-1551009175-15bdf9dcb580?auto=format&fit=crop&q=80&w=2000',
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
              src="https://images.unsplash.com/photo-1551009175-15bdf9dcb580?auto=format&fit=crop&q=80&w=2000"
              alt="Train header"
              className="w-full h-full object-cover"
            />
          </div>
          <div className="absolute inset-0 bg-gradient-to-t from-slate-900 via-transparent to-slate-900/60" />

          <div className="container mx-auto px-4 relative z-10 text-center">
            <motion.div
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex items-center justify-center gap-2 text-white/70 text-[10px] font-black uppercase tracking-[0.3em] mb-6"
            >
              <Link to="/" className="hover:text-white transition-colors">Trang chủ</Link>
              <ChevronLeft size={12} />
              <Link to="/train/results" className="hover:text-white transition-colors">Kết quả tìm kiếm</Link>
              <ChevronLeft size={12} className="rotate-180" />
              <span className="text-white">Chi tiết chuyến tàu</span>
            </motion.div>

            <div className="flex flex-col md:flex-row items-center justify-center gap-8 md:gap-24 mb-10">
              <div className="text-center">
                <motion.h2 initial={{ opacity: 0, x: -30 }} animate={{ opacity: 1, x: 0 }} className="text-6xl md:text-8xl font-black text-white tracking-tighter">
                  {routeTitle.from.slice(0, 3).toUpperCase()}
                </motion.h2>
                <p className="text-sm font-black text-[#1EB4D4] uppercase tracking-widest mt-2">{routeTitle.from}</p>
              </div>

              <div className="flex flex-col items-center">
                <motion.div initial={{ width: 0 }} animate={{ width: 120 }} className="h-px bg-white/20 relative">
                  <Train className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 text-[#1EB4D4]" size={24} />
                </motion.div>
                <p className="text-[10px] font-black text-white/40 uppercase tracking-[0.2em] mt-6">
                  {formatDateTime(detail?.segment?.departureAt)} • {detail?.segment?.availableSeatCount || 0} chỗ trống
                </p>
              </div>

              <div className="text-center">
                <motion.h2 initial={{ opacity: 0, x: 30 }} animate={{ opacity: 1, x: 0 }} className="text-6xl md:text-8xl font-black text-white tracking-tighter">
                  {routeTitle.to.slice(0, 3).toUpperCase()}
                </motion.h2>
                <p className="text-sm font-black text-[#1EB4D4] uppercase tracking-widest mt-2">{routeTitle.to}</p>
              </div>
            </div>

            <div className="flex flex-wrap items-center justify-center gap-6">
              <div className="flex items-center gap-3 px-6 py-2 bg-white/10 backdrop-blur-md rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-widest">
                {detail?.provider?.name || 'Đối tác đường sắt'}
              </div>
              <div className="flex items-center gap-3 px-6 py-2 bg-[#1EB4D4] rounded-full text-white text-[10px] font-black uppercase tracking-widest shadow-lg shadow-[#1EB4D4]/30">
                {detail?.trip?.trainNumber || detail?.trip?.code || 'Chuyến tàu'}
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-16 relative z-20">
          {loading ? (
            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100 text-center text-sm font-bold text-slate-500">
              Đang tải chi tiết chuyến tàu...
            </div>
          ) : error ? (
            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-rose-100 text-center text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
              <div className="lg:col-span-2 space-y-12">
                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                    <div className="w-12 h-12 bg-[#1EB4D4] rounded-2xl flex items-center justify-center text-white"><MapPin size={24} /></div>
                    Lộ trình các ga
                  </h2>
                  <div className="relative pl-12 border-l-2 border-dashed border-slate-100 ml-6 space-y-10">
                    {stops.map((stop) => (
                      <div key={stop.id} className="relative">
                        <div className={`absolute -left-[58px] top-1 w-6 h-6 rounded-full border-4 border-white shadow-md flex items-center justify-center ${
                          stop.isSelectedOrigin ? 'bg-[#1EB4D4]' : stop.isSelectedDestination ? 'bg-slate-900' : 'bg-slate-300'
                        }`}
                        />
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="text-lg font-black text-slate-900 leading-none mb-1">{stop.location?.name || stop.stopPoint?.name || 'Ga dừng'}</p>
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">
                              {stop.isSelectedOrigin ? 'Ga lên tàu' : stop.isSelectedDestination ? 'Ga xuống tàu' : 'Ga trung gian'}
                            </p>
                          </div>
                          <p className="text-xl font-black text-[#1EB4D4]">{formatTime(stop.departAt || stop.arriveAt)}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                    <div className="w-12 h-12 bg-slate-900 rounded-2xl flex items-center justify-center text-white"><Info size={24} /></div>
                    Lựa chọn toa / hạng chỗ
                  </h2>
                  <div className="space-y-6">
                    {carOptions.map((option, index) => (
                      <button
                        key={option.id}
                        type="button"
                        onClick={() => setSelectedClass(index)}
                        className={`w-full group relative flex flex-col md:flex-row gap-8 p-8 rounded-[3rem] border-2 transition-all overflow-hidden ${selectedClass === index ? 'bg-slate-50 border-[#1EB4D4] shadow-lg' : 'bg-white border-slate-100 hover:border-slate-200'}`}
                      >
                        <div className="w-20 h-20 bg-white rounded-[2rem] flex items-center justify-center text-4xl shadow-sm border border-slate-50 shrink-0">
                          {option.icon}
                        </div>
                        <div className="flex-1 space-y-4 text-left">
                          <div>
                            <h3 className="text-2xl font-black text-slate-900 tracking-tight mb-2 uppercase">{option.name}</h3>
                            <div className="flex items-center gap-4 flex-wrap">
                              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{option.avail} chỗ trống / {option.seats} tổng chỗ</p>
                              <div className="w-24 h-1.5 bg-slate-200 rounded-full overflow-hidden">
                                <div className={`${option.avail < 5 ? 'bg-rose-500' : 'bg-[#1EB4D4]'} h-full`} style={{ width: `${option.seats ? (option.avail / option.seats) * 100 : 0}%` }} />
                              </div>
                            </div>
                          </div>
                          <div className="flex flex-wrap gap-3">
                            <div className="flex items-center gap-2 px-4 py-2 bg-white rounded-xl shadow-sm border border-slate-50 text-[9px] font-black text-slate-600 uppercase tracking-widest">
                              <Wifi size={14} className="text-[#1EB4D4]" /> WiFi
                            </div>
                            <div className="flex items-center gap-2 px-4 py-2 bg-white rounded-xl shadow-sm border border-slate-50 text-[9px] font-black text-slate-600 uppercase tracking-widest">
                              <Coffee size={14} className="text-[#1EB4D4]" /> Dịch vụ đi tàu
                            </div>
                          </div>
                        </div>
                        <div className="flex flex-col items-end justify-center shrink-0">
                          <div className="text-right mb-4">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Giá mỗi khách</p>
                            <p className="text-3xl font-black text-slate-900 tracking-tighter">{formatCurrency(option.price, option.currency)}</p>
                          </div>
                          <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 transition-all ${selectedClass === index ? 'bg-[#1EB4D4] border-[#1EB4D4] text-white' : 'border-slate-200 text-transparent'}`}>
                            <CheckCircle size={20} />
                          </div>
                        </div>
                      </button>
                    ))}
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
                        <p className="text-5xl font-black text-[#1EB4D4] tracking-tighter">{formatCurrency(activeOption?.price || detail?.segment?.price || 0, detail?.segment?.currency)}</p>
                      </div>

                      <div className="space-y-6 mb-10">
                        <div className="bg-slate-50 p-6 rounded-[2.5rem] border border-slate-100 space-y-4">
                          <div className="flex justify-between items-center text-xs gap-4">
                            <span className="font-bold text-slate-400 uppercase tracking-widest">Hạng chỗ</span>
                            <span className="font-black text-slate-900 uppercase tracking-widest text-right">{activeOption?.name || 'Tiêu chuẩn'}</span>
                          </div>
                          <div className="flex justify-between items-center text-xs gap-4">
                            <span className="font-bold text-slate-400 uppercase tracking-widest">Tàu</span>
                            <span className="font-black text-slate-900 uppercase tracking-widest text-right">{detail?.trip?.trainNumber || detail?.trip?.code}</span>
                          </div>
                          <div className="flex justify-between items-center text-xs gap-4">
                            <span className="font-bold text-slate-400 uppercase tracking-widest">Trạng thái</span>
                            <span className="font-black text-green-500 uppercase tracking-widest text-right">Còn {activeOption?.avail || detail?.segment?.availableSeatCount || 0} chỗ</span>
                          </div>
                        </div>
                      </div>

                      <Link
                        to={`/train/seat-selection?${seatSelectionQuery}`}
                        className="w-full h-16 bg-slate-900 text-white rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30"
                      >
                        Tiếp tục chọn chỗ <ArrowRight size={18} />
                      </Link>

                      <div className="mt-8 pt-8 border-t border-slate-50 flex items-center gap-3 text-[#1EB4D4]">
                        <ShieldCheck size={18} />
                        <p className="text-[10px] font-black uppercase tracking-widest">Giữ chỗ ngay trong vài phút</p>
                      </div>
                    </div>
                  </div>

                  <div className="bg-[#1EB4D4]/5 p-8 rounded-[3rem] border border-[#1EB4D4]/10">
                    <div className="flex items-center gap-4 mb-4">
                      <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm"><Info size={20} /></div>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Thông tin chuyến tàu</p>
                    </div>
                    <p className="text-[11px] text-slate-600 font-bold leading-relaxed">
                      Đối tác vận hành: {detail?.provider?.name || 'Đơn vị đường sắt'}.
                      Quý khách nên có mặt tại ga trước giờ khởi hành tối thiểu 30 phút để hoàn tất thủ tục lên tàu.
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
}
