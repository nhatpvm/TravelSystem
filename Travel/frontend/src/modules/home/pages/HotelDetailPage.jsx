import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowRight,
  CalendarDays,
  ChevronLeft,
  CheckCircle,
  Heart,
  Hotel,
  MapPin,
  Share2,
  ShieldCheck,
  Star,
  Users,
} from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getHotelAvailability, getHotelGallery, getHotelReviews, getPublicHotel } from '../../../services/hotelService';
import { addWishlistItem, deleteWishlistItem, listWishlistItems } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, formatDateOnly, formatTimeOnly } from '../../tenant/hotel/utils/presentation';

function getDefaultDates() {
  const checkIn = new Date();
  checkIn.setDate(checkIn.getDate() + 7);
  const checkOut = new Date(checkIn);
  checkOut.setDate(checkIn.getDate() + 1);

  return {
    checkInDate: checkIn.toISOString().slice(0, 10),
    checkOutDate: checkOut.toISOString().slice(0, 10),
  };
}

export default function HotelDetailPage() {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const session = useAuthSession();
  const [selectedPhoto, setSelectedPhoto] = useState(0);
  const [liked, setLiked] = useState(false);
  const [wishlistItemId, setWishlistItemId] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [hotel, setHotel] = useState(null);
  const [gallery, setGallery] = useState({ hotelImages: [], roomTypes: [] });
  const [availability, setAvailability] = useState(null);
  const [reviews, setReviews] = useState(null);
  const [query, setQuery] = useState({
    ...getDefaultDates(),
    adults: 2,
    children: 0,
    rooms: 1,
  });

  async function loadData(nextQuery = query) {
    if (!id) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [hotelResponse, galleryResponse, availabilityResponse, reviewsResponse] = await Promise.all([
        getPublicHotel(id),
        getHotelGallery(id),
        getHotelAvailability(id, nextQuery),
        getHotelReviews(id, { pageSize: 5 }),
      ]);

      setHotel(hotelResponse);
      setGallery(galleryResponse || { hotelImages: [], roomTypes: [] });
      setAvailability(availabilityResponse || null);
      setReviews(reviewsResponse || null);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [id]);

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
          (item) => Number(item.productType) === 4 && String(item.targetId || '') === String(id),
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

  const photos = useMemo(() => {
    const hotelImages = Array.isArray(gallery?.hotelImages) ? gallery.hotelImages : [];
    const roomImages = Array.isArray(gallery?.roomTypes)
      ? gallery.roomTypes.flatMap((item) => item.images || [])
      : [];
    const candidates = [
      ...(hotelImages.map((item) => item.url).filter(Boolean)),
      ...(roomImages.map((item) => item.url).filter(Boolean)),
      hotel?.coverImageUrl,
    ].filter(Boolean);

    return candidates.length > 0
      ? [...new Set(candidates)]
      : ['https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&q=80&w=2000'];
  }, [gallery, hotel]);

  const roomOptions = Array.isArray(availability?.roomTypes) && availability.roomTypes.length > 0
    ? availability.roomTypes
    : Array.isArray(hotel?.roomTypes) ? hotel.roomTypes.map((item) => ({
      roomTypeId: item.id,
      roomTypeCode: item.code,
      roomTypeName: item.name,
      areaSquareMeters: item.areaSquareMeters,
      maxGuests: item.maxGuests,
      defaultAdults: item.defaultAdults,
      defaultChildren: item.defaultChildren,
      coverImageUrl: item.coverImageUrl,
      availableUnits: 0,
      options: [],
    })) : [];

  function handleBookRoom(room, option) {
    if (!id || !room?.roomTypeId || !option?.ratePlanId) {
      return;
    }

    const checkoutParams = new URLSearchParams({
      product: 'hotel',
      hotelId: id,
      roomTypeId: room.roomTypeId,
      ratePlanId: option.ratePlanId,
      hotelName: hotel?.name || 'Khách sạn',
      roomTypeName: room.roomTypeName || room.name || 'Loại phòng',
      ratePlanName: option.ratePlanName || 'Gói giá',
      checkInDate: query.checkInDate,
      checkOutDate: query.checkOutDate,
      rooms: String(Number(query.rooms || 1)),
      adult: String(Number(query.adults || 1)),
      child: String(Number(query.children || 0)),
      totalPrice: String(Number(option.totalPrice || 0)),
      currencyCode: option.currencyCode || 'VND',
    });

    navigate(`/checkout?${checkoutParams.toString()}`);
  }

  async function handleToggleWishlist() {
    if (!hotel || !id) {
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
        productType: 'hotel',
        targetId: id,
        title: hotel.name,
        subtitle: hotel.shortDescription || hotel.email || 'Khách sạn trên nền tảng',
        locationText: [hotel.addressLine, hotel.city, hotel.province].filter(Boolean).join(', '),
        priceValue: availability?.roomTypes?.[0]?.options?.[0]?.totalPrice || undefined,
        priceText: availability?.roomTypes?.[0]?.options?.[0]?.totalPrice
          ? formatCurrency(availability.roomTypes[0].options[0].totalPrice, availability.roomTypes[0].options[0].currencyCode || 'VND')
          : undefined,
        currencyCode: availability?.roomTypes?.[0]?.options?.[0]?.currencyCode || 'VND',
        imageUrl: hotel.coverImageUrl || photos[0],
        targetUrl: `/hotel/${id}`,
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
        <div className="relative h-[650px] group overflow-hidden">
          <motion.img
            key={photos[selectedPhoto] || photos[0]}
            initial={{ opacity: 0, scale: 1.1 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.8 }}
            src={photos[selectedPhoto] || photos[0]}
            alt={hotel?.name || 'Hotel'}
            className="w-full h-full object-cover brightness-[0.6]"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-900/80 via-transparent to-slate-900/40" />

          <div className="absolute inset-0 flex flex-col justify-between p-12 pointer-events-none">
            <div className="flex items-center justify-between pointer-events-auto">
              <Link to="/hotel/results" className="flex items-center gap-3 bg-white/10 backdrop-blur-md px-6 py-3 rounded-2xl border border-white/20 text-white text-[10px] font-black uppercase tracking-widest hover:bg-white/20 transition-all">
                <ChevronLeft size={16} /> Quay lại kết quả
              </Link>
              <div className="flex gap-4">
                <button type="button" className="w-12 h-12 bg-white/10 backdrop-blur-md rounded-2xl border border-white/20 flex items-center justify-center text-white hover:bg-white/20 transition-all pointer-events-auto">
                  <Share2 size={18} />
                </button>
                <button type="button" onClick={handleToggleWishlist} className={`w-12 h-12 backdrop-blur-md rounded-2xl border border-white/20 flex items-center justify-center transition-all pointer-events-auto ${liked ? 'bg-rose-500 text-white' : 'bg-white/10 text-white hover:bg-rose-500'}`}>
                  <Heart size={18} className={liked ? 'fill-white' : ''} />
                </button>
              </div>
            </div>

            <div className="max-w-4xl pointer-events-auto">
              <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} className="flex items-center gap-2 mb-4">
                {Array.from({ length: Math.max(Number(hotel?.starRating || 0), 1) }).map((_, index) => <Star key={`hotel-star-${index}`} size={16} className="text-amber-400" fill="currentColor" />)}
                <span className="text-white/60 text-[10px] font-black uppercase tracking-[0.3em] ml-2">Hotel Marketplace</span>
              </motion.div>
              <motion.h1 initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }} className="text-5xl md:text-8xl font-black text-white tracking-tighter leading-none mb-8">
                {hotel?.name || 'Đang tải khách sạn'}
              </motion.h1>
              <div className="flex flex-wrap gap-8">
                <div className="flex items-center gap-3 text-white">
                  <div className="w-10 h-10 bg-white/10 rounded-xl flex items-center justify-center"><MapPin size={20} /></div>
                  <div>
                    <p className="text-[10px] font-black uppercase tracking-widest text-white/40">Vị trí</p>
                    <p className="text-sm font-bold">{[hotel?.addressLine, hotel?.city, hotel?.province].filter(Boolean).join(', ') || 'Đang cập nhật'}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3 text-white">
                  <div className="w-10 h-10 bg-[#1EB4D4] rounded-xl flex items-center justify-center shadow-lg shadow-[#1EB4D4]/30"><Star size={20} fill="white" /></div>
                  <div>
                    <p className="text-[10px] font-black uppercase tracking-widest text-white/40">Đánh giá</p>
                    <p className="text-sm font-bold">{reviews?.summary?.averageRating || '--'}/5 ({reviews?.summary?.totalReviews || 0} lượt)</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="absolute bottom-12 right-12 flex gap-4">
            {photos.map((photo, index) => (
              <button key={photo} type="button" onClick={() => setSelectedPhoto(index)} className={`w-20 h-20 rounded-2xl overflow-hidden border-2 transition-all hover:scale-105 ${selectedPhoto === index ? 'border-[#1EB4D4] scale-110 shadow-lg' : 'border-white/20 opacity-50'}`}>
                <img src={photo} alt="" className="w-full h-full object-cover" />
              </button>
            ))}
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-16 relative z-10">
          {loading ? (
            <div className="bg-white rounded-[3rem] border border-slate-100 p-12 text-center text-sm font-bold text-slate-500">Đang tải chi tiết khách sạn...</div>
          ) : error ? (
            <div className="bg-rose-50 rounded-[3rem] border border-rose-100 p-12 text-center text-sm font-bold text-rose-600">{error}</div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
              <div className="lg:col-span-2 space-y-12">
                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-8 flex items-center gap-4">
                    <div className="w-12 h-12 bg-slate-900 rounded-2xl flex items-center justify-center text-white"><Hotel size={24} /></div>
                    Thông tin lưu trú
                  </h2>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {[
                      { label: 'Nhận phòng', value: formatTimeOnly(hotel?.defaultCheckInTime) },
                      { label: 'Trả phòng', value: formatTimeOnly(hotel?.defaultCheckOutTime) },
                      { label: 'Điện thoại', value: hotel?.phone || 'Đang cập nhật' },
                      { label: 'Email', value: hotel?.email || 'Đang cập nhật' },
                    ].map((item) => (
                      <div key={item.label} className="rounded-[2rem] bg-slate-50 p-6">
                        <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">{item.label}</p>
                        <p className="text-lg font-black text-slate-900 mt-3">{item.value}</p>
                      </div>
                    ))}
                  </div>
                  <p className="text-sm font-bold text-slate-500 leading-relaxed mt-8">
                    {hotel?.descriptionMarkdown || hotel?.shortDescription || 'Khách sạn từ hệ thống đối tác, hỗ trợ kiểm tra phòng trống theo ngày lưu trú và cấu hình giá theo từng gói bán.'}
                  </p>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                    <div className="w-12 h-12 bg-[#1EB4D4] rounded-2xl flex items-center justify-center text-white"><CalendarDays size={24} /></div>
                    Phòng khả dụng
                  </h2>
                  <div className="space-y-6">
                    {roomOptions.length === 0 ? (
                      <div className="rounded-[2rem] bg-slate-50 p-8 text-sm font-bold text-slate-500">Chưa có dữ liệu phòng trống cho khoảng ngày đã chọn.</div>
                    ) : roomOptions.map((room, index) => {
                      const bestOption = room.options?.[0];
                      return (
                        <div key={room.roomTypeId || room.id || index} className="group relative flex flex-col md:flex-row gap-8 p-8 bg-slate-50 rounded-[3rem] border border-transparent hover:border-[#1EB4D4]/10 hover:bg-white transition-all overflow-hidden">
                          <div className="md:w-64 h-48 md:h-auto rounded-[2rem] overflow-hidden shrink-0">
                            <img src={room.coverImageUrl || photos[0]} alt={room.roomTypeName || room.name} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700" />
                          </div>
                          <div className="flex-1 space-y-4">
                            <div>
                              <h3 className="text-2xl font-black text-slate-900 tracking-tight mb-2">{room.roomTypeName || room.name}</h3>
                              <p className="text-sm font-bold text-slate-500 leading-relaxed">{room.descriptionMarkdown || `Diện tích ${room.areaSquareMeters || '--'}m², phù hợp tối đa ${room.maxGuests || room.defaultAdults || 2} khách.`}</p>
                            </div>
                            <div className="flex items-center gap-6 flex-wrap">
                              <div className="flex items-center gap-2 text-slate-400 font-bold text-xs uppercase tracking-widest">
                                <Users size={16} className="text-[#1EB4D4]" /> {room.maxGuests || room.defaultAdults || 2} khách
                              </div>
                              <div className="flex items-center gap-2 text-slate-400 font-bold text-xs uppercase tracking-widest">
                                <CheckCircle size={16} className="text-green-500" /> {room.availableUnits || 0} phòng trống
                              </div>
                            </div>
                            {bestOption ? (
                              <div className="rounded-[1.5rem] bg-white px-5 py-4">
                                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">{bestOption.ratePlanName}</p>
                                <p className="text-sm font-bold text-slate-600 mt-2">
                                  {bestOption.breakfastIncluded ? 'Có ăn sáng' : 'Không ăn sáng'} • {bestOption.refundable ? 'Hoàn hủy được' : 'Không hoàn hủy'}
                                </p>
                                <p className="text-xs font-bold text-slate-400 mt-2">
                                  {bestOption.nightlyRates?.slice(0, 3).map((item) => `${formatDateOnly(item.date)}: ${formatCurrency(item.price, item.currencyCode)}`).join(' • ')}
                                </p>
                              </div>
                            ) : null}
                          </div>
                          <div className="flex flex-col items-end justify-between shrink-0">
                            <div className="text-right">
                              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Tổng giá từ</p>
                              <p className="text-3xl font-black text-slate-900 tracking-tighter">
                                {formatCurrency(bestOption?.totalPrice || 0, bestOption?.currencyCode || 'VND')}
                              </p>
                            </div>
                            <button type="button" onClick={() => handleBookRoom(room, bestOption)} disabled={!bestOption} className="w-full md:w-auto h-14 px-8 bg-slate-900 text-white rounded-2xl font-black text-[10px] uppercase tracking-widest flex items-center justify-center gap-3 hover:bg-[#1EB4D4] transition-all shadow-lg hover:shadow-[#1EB4D4]/30 disabled:opacity-50 disabled:cursor-not-allowed">
                              Đặt ngay <ArrowRight size={16} />
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>

                <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
                  <h2 className="text-2xl font-black text-slate-900 mb-8">Đánh giá gần đây</h2>
                  <div className="space-y-6">
                    {(reviews?.items || []).length === 0 ? (
                      <div className="rounded-[2rem] bg-slate-50 p-8 text-sm font-bold text-slate-500">Chưa có đánh giá công khai nào.</div>
                    ) : reviews.items.map((item) => (
                      <div key={item.id} className="rounded-[2rem] bg-slate-50 p-6">
                        <div className="flex items-center justify-between gap-4">
                          <p className="font-black text-slate-900">{item.reviewerName || 'Khách lưu trú'}</p>
                          <p className="text-xs font-bold text-slate-400">{formatDateOnly(item.createdAt)}</p>
                        </div>
                        <div className="flex items-center gap-2 text-amber-400 mt-3">
                          {Array.from({ length: Math.max(Math.round(Number(item.rating || 0)), 1) }).map((_, index) => <Star key={`${item.id}-review-star-${index}`} size={14} fill="currentColor" />)}
                        </div>
                        <p className="text-sm font-bold text-slate-600 mt-4">{item.title || item.content}</p>
                        {item.replyContent ? <p className="text-xs font-bold text-slate-400 mt-3">Phản hồi từ khách sạn: {item.replyContent}</p> : null}
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              <div className="lg:col-span-1">
                <div className="sticky top-32 space-y-8">
                  <div className="bg-white rounded-[3.5rem] p-10 shadow-2xl shadow-slate-200/60 border border-slate-100 relative overflow-hidden">
                    <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16 -z-0" />
                    <div className="relative z-10">
                      <p className="text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2">Kiểm tra phòng trống</p>
                      <div className="space-y-4 mb-8">
                        <div className="grid grid-cols-1 gap-4">
                          <div className="grid grid-cols-2 gap-4">
                            <input type="date" value={query.checkInDate} onChange={(event) => setQuery((current) => ({ ...current, checkInDate: event.target.value }))} className="w-full h-14 bg-slate-50 rounded-2xl px-6 font-bold text-slate-900 border-none outline-none" />
                            <input type="date" value={query.checkOutDate} onChange={(event) => setQuery((current) => ({ ...current, checkOutDate: event.target.value }))} className="w-full h-14 bg-slate-50 rounded-2xl px-6 font-bold text-slate-900 border-none outline-none" />
                          </div>
                          <div className="grid grid-cols-3 gap-4">
                            <input type="number" min="1" value={query.adults} onChange={(event) => setQuery((current) => ({ ...current, adults: event.target.value }))} placeholder="Người lớn" className="w-full h-14 bg-slate-50 rounded-2xl px-4 font-bold text-slate-900 border-none outline-none" />
                            <input type="number" min="0" value={query.children} onChange={(event) => setQuery((current) => ({ ...current, children: event.target.value }))} placeholder="Trẻ em" className="w-full h-14 bg-slate-50 rounded-2xl px-4 font-bold text-slate-900 border-none outline-none" />
                            <input type="number" min="1" value={query.rooms} onChange={(event) => setQuery((current) => ({ ...current, rooms: event.target.value }))} placeholder="Phòng" className="w-full h-14 bg-slate-50 rounded-2xl px-4 font-bold text-slate-900 border-none outline-none" />
                          </div>
                        </div>
                      </div>

                      <button type="button" onClick={() => loadData(query)} className="w-full h-16 bg-slate-900 text-white rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30">
                        Kiểm tra phòng trống <ArrowRight size={18} />
                      </button>

                      <div className="mt-8 pt-8 border-t border-slate-50 flex items-center gap-3 text-[#1EB4D4]">
                        <ShieldCheck size={18} />
                        <p className="text-[10px] font-black uppercase tracking-widest">Giá hiển thị theo inventory của đối tác</p>
                      </div>
                    </div>
                  </div>

                  <div className="bg-slate-900 rounded-[3rem] p-10 text-white relative overflow-hidden group">
                    <div className="absolute top-0 right-0 w-40 h-40 bg-white/5 rounded-full translate-x-16 -translate-y-16 group-hover:scale-150 transition-transform duration-1000" />
                    <p className="text-[10px] font-black uppercase tracking-[0.3em] text-[#1EB4D4] mb-4">Thông tin giá</p>
                    <h3 className="text-2xl font-black tracking-tight mb-4">
                      {availability?.query?.nightCount || 1} đêm • {availability?.roomTypes?.length || 0} hạng phòng có giá
                    </h3>
                    <p className="text-white/50 text-xs font-bold leading-relaxed mb-8">
                      Lần kiểm tra gần nhất cho khoảng ngày {formatDateOnly(query.checkInDate)} đến {formatDateOnly(query.checkOutDate)}.
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
