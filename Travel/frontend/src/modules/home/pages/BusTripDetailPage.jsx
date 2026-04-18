import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { ArrowRight, Bus, ChevronLeft, Clock, Coffee, Heart, Info, MapPin, ShieldCheck, Snowflake, Star, Wifi } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getBusTripDetail } from '../../../services/busService';
import { addWishlistItem, deleteWishlistItem, listWishlistItems } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, formatDateTime, formatTime, parseAmenities } from '../../tenant/bus/utils/presentation';

const AMENITY_ICON_MAP = {
  wifi: <Wifi size={18} />,
  'wi-fi': <Wifi size={18} />,
  dieuhoa: <Snowflake size={18} />,
  'điều hòa': <Snowflake size={18} />,
  'nuoc uong': <Coffee size={18} />,
  'nước uống': <Coffee size={18} />,
  toilet: <Info size={18} />,
  wc: <Info size={18} />,
};

function getAmenityIcon(label) {
  const key = String(label || '').trim().toLowerCase();
  return AMENITY_ICON_MAP[key] || <Info size={18} />;
}

function resolveRouteTitle(stops) {
  const first = stops[0];
  const last = stops[stops.length - 1];

  return {
    from: first?.location?.name || first?.stopPoint?.name || 'Điểm đi',
    to: last?.location?.name || last?.stopPoint?.name || 'Điểm đến',
  };
}

export default function BusTripDetailPage() {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const session = useAuthSession();
  const [searchParams] = useSearchParams();
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [liked, setLiked] = useState(false);
  const [wishlistItemId, setWishlistItemId] = useState('');

  useEffect(() => {
    if (!id) {
      setError('Không tìm thấy chuyến xe.');
      setLoading(false);
      return undefined;
    }

    let active = true;
    setLoading(true);
    setError('');

    getBusTripDetail(id, {
      fromTripStopTimeId: searchParams.get('fromTripStopTimeId') || undefined,
      toTripStopTimeId: searchParams.get('toTripStopTimeId') || undefined,
    })
      .then((response) => {
        if (active) {
          setDetail(response);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được chi tiết chuyến xe.');
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
  }, [id, searchParams]);

  useEffect(() => {
    if (!session.isAuthenticated || !id) {
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
          (item) => Number(item.productType) === 1 && String(item.targetId || '') === String(id),
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
  }, [id, session.isAuthenticated]);

  const stops = detail?.stops || [];
  const routeTitle = resolveRouteTitle(stops);
  const amenities = parseAmenities(detail?.vehicleDetail?.amenitiesJson);
  const segmentParams = new URLSearchParams({
    tripId: detail?.trip?.id || id || '',
    fromTripStopTimeId: detail?.segment?.fromTripStopTimeId || '',
    toTripStopTimeId: detail?.segment?.toTripStopTimeId || '',
  });

  async function handleToggleWishlist() {
    if (!detail || !id) {
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
        productType: 'bus',
        targetId: id,
        title: `${routeTitle.from} - ${routeTitle.to}`,
        subtitle: detail?.provider?.name || detail?.tenant?.name || 'Nhà xe đối tác',
        locationText: `${routeTitle.from} → ${routeTitle.to}`,
        priceValue: detail?.segment?.price || undefined,
        priceText: detail?.segment?.price
          ? formatCurrency(detail.segment.price, detail.segment.currency)
          : undefined,
        currencyCode: detail?.segment?.currency || 'VND',
        imageUrl: 'https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?auto=format&fit=crop&q=80&w=2000',
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
        <div className="h-[450px] relative flex items-center justify-center overflow-hidden">
          <img
            src="https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?auto=format&fit=crop&q=80&w=2000"
            alt="Bus"
            className="absolute inset-0 w-full h-full object-cover brightness-[0.4]"
          />
          <div className="container mx-auto px-4 relative z-10 text-center">
            <motion.div
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex items-center justify-center gap-2 text-white/70 text-[10px] font-black uppercase tracking-[0.3em] mb-6"
            >
              <Link to="/" className="hover:text-white transition-colors">Trang chủ</Link>
              <ChevronLeft size={12} />
              <Link to="/bus/results" className="hover:text-white transition-colors">Kết quả tìm kiếm</Link>
              <ChevronLeft size={12} className="rotate-180" />
              <span className="text-white">Chi tiết chuyến xe</span>
            </motion.div>
            <motion.h1
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="text-5xl md:text-7xl font-black text-white tracking-tighter leading-none"
            >
              {routeTitle.from.toUpperCase()} <span className="text-[#1EB4D4] italic">đến</span> {routeTitle.to.toUpperCase()}
            </motion.h1>
            <div className="flex items-center justify-center gap-6 mt-8 flex-wrap">
              <div className="px-6 py-2 bg-white/10 backdrop-blur-md rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-widest">
                {detail?.provider?.name || detail?.tenant?.name || 'Nhà xe đối tác'}
              </div>
              <div className="px-6 py-2 bg-[#1EB4D4] rounded-full text-white text-[10px] font-black uppercase tracking-widest shadow-lg shadow-[#1EB4D4]/30">
                {detail?.vehicleDetail?.busType || detail?.vehicle?.name || 'Xe đang khai thác'}
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-20 relative z-20">
          {loading ? (
            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100 text-center text-sm font-bold text-slate-500">
              Đang tải chi tiết chuyến xe...
            </div>
          ) : error ? (
            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-rose-100 text-center text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="grid grid-cols-1 lg:grid-cols-3 gap-12"
            >
              <div className="lg:col-span-2 space-y-10">
                <div className="bg-white rounded-[3.5rem] p-10 shadow-xl shadow-slate-200/50 border border-slate-100 italic">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-12 items-center">
                    <div className="text-center md:text-left">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Khởi hành</p>
                      <p className="text-4xl font-black text-slate-900 tracking-tighter">{formatTime(detail?.segment?.departureAt)}</p>
                      <p className="text-sm font-bold text-slate-500 mt-1">{routeTitle.from}</p>
                    </div>

                    <div className="flex flex-col items-center justify-center">
                      <div className="w-full flex items-center gap-4">
                        <div className="h-px flex-1 bg-slate-100" />
                        <div className="w-12 h-12 bg-slate-50 rounded-2xl flex items-center justify-center text-[#1EB4D4]">
                          <Clock size={20} />
                        </div>
                        <div className="h-px flex-1 bg-slate-100" />
                      </div>
                      <p className="text-xs font-black text-slate-400 uppercase tracking-widest mt-3">
                        {formatDateTime(detail?.segment?.departureAt)} • {detail?.segment?.availableSeatCount} ghế trống
                      </p>
                    </div>

                    <div className="text-center md:text-right">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Điểm đến</p>
                      <p className="text-4xl font-black text-slate-900 tracking-tighter">{formatTime(detail?.segment?.arrivalAt)}</p>
                      <p className="text-sm font-bold text-slate-500 mt-1">{routeTitle.to}</p>
                    </div>
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-3">
                    <div className="w-10 h-10 bg-[#1EB4D4] rounded-2xl flex items-center justify-center text-white">
                      <MapPin size={20} />
                    </div>
                    Lộ trình chi tiết
                  </h2>
                  <div className="relative pl-12">
                    <div className="absolute left-[23px] top-4 bottom-4 w-px border-l-2 border-dashed border-slate-200" />
                    {stops.map((stop) => (
                      <div key={stop.id} className="relative mb-12 last:mb-0">
                        <div
                          className={`absolute -left-12 w-6 h-6 rounded-full flex items-center justify-center border-4 border-white shadow-md ${
                            stop.isSelectedOrigin ? 'bg-[#1EB4D4]' : stop.isSelectedDestination ? 'bg-slate-900' : 'bg-slate-300'
                          }`}
                        />
                        <div className="flex items-center justify-between gap-6">
                          <div>
                            <p className="text-lg font-black text-slate-900 leading-none mb-1">
                              {stop.location?.name || stop.stopPoint?.name || 'Điểm dừng'}
                            </p>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-widest">
                              {stop.stopPoint?.name || 'Điểm đón trả'}
                            </p>
                            {stop.pickupPoints?.length ? (
                              <p className="text-[10px] text-sky-500 font-black uppercase tracking-widest mt-2">
                                {stop.pickupPoints.length} điểm đón hỗ trợ
                              </p>
                            ) : null}
                            {stop.dropoffPoints?.length ? (
                              <p className="text-[10px] text-emerald-500 font-black uppercase tracking-widest mt-1">
                                {stop.dropoffPoints.length} điểm trả hỗ trợ
                              </p>
                            ) : null}
                          </div>
                          <p className="text-xl font-black text-[#1EB4D4]">
                            {formatTime(stop.departAt || stop.arriveAt)}
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-3">
                    <div className="w-10 h-10 bg-slate-900 rounded-2xl flex items-center justify-center text-white">
                      <Wifi size={20} />
                    </div>
                    Tiện ích trên xe
                  </h2>
                  {amenities.length > 0 ? (
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
                      {amenities.map((item) => (
                        <div key={item} className="group p-8 bg-slate-50 rounded-[2.5rem] border border-transparent hover:border-[#1EB4D4]/20 hover:bg-white transition-all text-center">
                          <div className="w-14 h-14 bg-white rounded-2xl flex items-center justify-center text-slate-400 group-hover:text-[#1EB4D4] shadow-sm mb-4 mx-auto transition-colors">
                            {getAmenityIcon(item)}
                          </div>
                          <span className="text-xs font-black text-slate-900 uppercase tracking-widest">{item}</span>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="rounded-[2rem] bg-slate-50 border border-slate-100 px-6 py-5 text-sm font-bold text-slate-500">
                      Nhà xe chưa cập nhật tiện ích cho chuyến này.
                    </div>
                  )}
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
                      <p className="text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2">Giá từ</p>
                      <div className="flex items-baseline gap-2 mb-8">
                        <p className="text-5xl font-black text-[#1EB4D4] tracking-tighter">
                          {detail?.segment?.price ? formatCurrency(detail.segment.price, detail.segment.currency) : 'Liên hệ'}
                        </p>
                      </div>

                      <div className="space-y-6 mb-10">
                        <div className="p-6 bg-slate-50 rounded-[2rem] border border-slate-100">
                          <div className="flex items-center gap-4 mb-4">
                            <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-slate-400">
                              <Bus size={18} />
                            </div>
                            <div>
                              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Loại xe</p>
                              <p className="text-sm font-black text-slate-900">
                                {detail?.vehicleDetail?.busType || detail?.vehicle?.name || 'Xe phục vụ chuyến'}
                              </p>
                            </div>
                          </div>
                          <div className="flex items-center gap-4">
                            <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-slate-400">
                              <Clock size={18} />
                            </div>
                            <div>
                              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Ghế còn trống</p>
                              <p className="text-sm font-black text-slate-900">{detail?.segment?.availableSeatCount || 0} chỗ</p>
                            </div>
                          </div>
                        </div>
                      </div>

                      <Link
                        to={`/bus/seat-selection?${segmentParams.toString()}`}
                        className="w-full h-16 bg-slate-900 text-white rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30"
                      >
                        Chọn ghế & Đặt vé <ArrowRight size={18} />
                      </Link>

                      <div className="mt-8 pt-8 border-t border-slate-50 flex items-center gap-3 text-[#1EB4D4]">
                        <ShieldCheck size={18} />
                        <p className="text-[10px] font-black uppercase tracking-widest">Đảm bảo giữ chỗ theo nhà xe</p>
                      </div>
                    </div>
                  </div>

                  <div className="bg-slate-900 rounded-[3rem] p-8 text-white relative overflow-hidden group">
                    <div className="absolute top-0 right-0 w-32 h-32 bg-white/5 rounded-full translate-x-16 -translate-y-16 group-hover:scale-110 transition-transform" />
                    <div className="flex items-center gap-5 relative z-10">
                      <div className="w-16 h-16 bg-white/10 backdrop-blur-xl rounded-[1.5rem] flex items-center justify-center">
                        <Bus size={28} className="text-[#1EB4D4]" />
                      </div>
                      <div>
                        <p className="text-lg font-black tracking-tight">{detail?.provider?.name || detail?.tenant?.name || 'Nhà xe đối tác'}</p>
                        {detail?.provider?.ratingAverage ? (
                          <div className="flex items-center gap-2 mt-1">
                            <div className="flex text-amber-400">
                              {[...Array(5)].map((_, index) => (
                                <Star key={index} size={10} fill="currentColor" />
                              ))}
                            </div>
                            <span className="text-[10px] font-black text-white/40 uppercase tracking-widest">
                              {Number(detail.provider.ratingAverage).toFixed(1)}★
                            </span>
                          </div>
                        ) : (
                          <p className="text-[10px] font-black text-white/40 uppercase tracking-widest mt-1">
                            {detail?.provider?.supportPhone || 'Nhà xe đang bán trên nền tảng'}
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </motion.div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
