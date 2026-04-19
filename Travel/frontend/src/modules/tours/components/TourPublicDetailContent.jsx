import React, { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  ChevronLeft,
  Users,
  Clock,
  Star,
  MapPin,
  Calendar,
  ArrowRight,
  Plus,
  Minus,
  ShieldCheck,
  Info,
  ChevronDown,
  CheckCircle2,
  Camera,
  Mountain,
  MessageSquare,
  Heart,
} from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  addWishlistItem,
  deleteWishlistItem,
  listWishlistItems,
  trackRecentView,
} from '../../../services/customerCommerceService';
import { getCustomerLocale } from '../../../services/customerPreferences';
import {
  getPublicTourById,
  getPublicTourGallery,
  listPublicTourAddons,
  listPublicTourFaqs,
  listPublicTourItinerary,
  listPublicTourPolicies,
  listPublicTourReviews,
  listPublicTours,
  quoteTour,
} from '../../../services/tourService';
import {
  formatCurrency,
  formatDate,
  formatDuration,
  formatTime,
  getDifficultyLabel,
  getTourTypeLabel,
  parseJsonList,
} from '../utils/presentation';

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

function formatDateTime(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat(getCustomerLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}

export default function TourPublicDetailContent({ tourId, useFeaturedFallback = false }) {
  const navigate = useNavigate();
  const location = useLocation();
  const session = useAuthSession();
  const [resolvedTourId, setResolvedTourId] = useState(tourId || '');
  const [tour, setTour] = useState(null);
  const [gallery, setGallery] = useState([]);
  const [itinerary, setItinerary] = useState([]);
  const [policies, setPolicies] = useState([]);
  const [addons, setAddons] = useState([]);
  const [faqs, setFaqs] = useState([]);
  const [reviews, setReviews] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeDay, setActiveDay] = useState(null);
  const [activeImage, setActiveImage] = useState('');
  const [selectedScheduleId, setSelectedScheduleId] = useState('');
  const [adults, setAdults] = useState(2);
  const [children, setChildren] = useState(0);
  const [quote, setQuote] = useState(null);
  const [quoteLoading, setQuoteLoading] = useState(false);
  const [quoteError, setQuoteError] = useState('');
  const [wishlistItemId, setWishlistItemId] = useState('');

  useEffect(() => {
    let active = true;

    async function resolveInitialTour() {
      if (tourId) {
        setResolvedTourId(tourId);
        return;
      }

      if (!useFeaturedFallback) {
        setResolvedTourId('');
        return;
      }

      try {
        const response = await listPublicTours({
          featuredOnly: true,
          page: 1,
          pageSize: 1,
        });

        if (active) {
          setResolvedTourId(response.items?.[0]?.id || '');
        }
      } catch {
        if (active) {
          setResolvedTourId('');
        }
      }
    }

    resolveInitialTour();

    return () => {
      active = false;
    };
  }, [tourId, useFeaturedFallback]);

  useEffect(() => {
    if (!resolvedTourId) {
      setLoading(false);
      setTour(null);
      setError('Không tìm thấy tour.');
      return undefined;
    }

    let active = true;

    async function loadData() {
      setLoading(true);
      setError('');

      try {
        const [tourResponse, galleryResponse, itineraryResponse, policyResponse, addonResponse, faqResponse, reviewResponse] = await Promise.all([
          getPublicTourById(resolvedTourId),
          getPublicTourGallery(resolvedTourId),
          listPublicTourItinerary(resolvedTourId, { includeItems: true }),
          listPublicTourPolicies(resolvedTourId, { highlightedOnly: false }),
          listPublicTourAddons(resolvedTourId, { activeOnly: true }),
          listPublicTourFaqs(resolvedTourId, { highlightedOnly: true, pageSize: 6 }),
          listPublicTourReviews(resolvedTourId, { pageSize: 5 }),
        ]);

        if (!active) {
          return;
        }

        const imageUrl = tourResponse.images?.[0]?.imageUrl
          || tourResponse.coverImageUrl
          || 'https://images.unsplash.com/photo-1559592413-7cea732639f5?auto=format&fit=crop&q=80&w=2000';

        setTour(tourResponse);
        setGallery(galleryResponse.items || galleryResponse || []);
        setItinerary(itineraryResponse.items || []);
        setPolicies(policyResponse.items || []);
        setAddons(addonResponse.items || []);
        setFaqs(faqResponse.items || []);
        setReviews(reviewResponse.items || []);
        setActiveDay(itineraryResponse.items?.[0]?.id || null);
        setActiveImage(imageUrl);
        setSelectedScheduleId(tourResponse.upcomingSchedules?.[0]?.id || '');
      } catch (err) {
        if (!active) {
          return;
        }

        setTour(null);
        setGallery([]);
        setItinerary([]);
        setPolicies([]);
        setAddons([]);
        setFaqs([]);
        setReviews([]);
        setError(err.message || 'Không thể tải chi tiết tour.');
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
  }, [resolvedTourId]);

  useEffect(() => {
    if (!tour || !selectedScheduleId) {
      setQuote(null);
      return undefined;
    }

    const paxGroups = buildPassengerGroups(adults, children);
    if (!paxGroups.length) {
      setQuote(null);
      return undefined;
    }

    let active = true;
    setQuoteLoading(true);
    setQuoteError('');

    quoteTour(tour.id, {
      scheduleId: selectedScheduleId,
      includeDefaultAddons: true,
      includeDefaultPackageOptions: true,
      paxGroups,
    })
      .then((response) => {
        if (active) {
          setQuote(response);
        }
      })
      .catch((err) => {
        if (!active) {
          return;
        }

        setQuote(null);
        setQuoteError(err.message || 'Không thể báo giá tour.');
      })
      .finally(() => {
        if (active) {
          setQuoteLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [adults, children, selectedScheduleId, tour]);

  useEffect(() => {
    if (!session.isAuthenticated || !resolvedTourId) {
      setWishlistItemId('');
      return undefined;
    }

    let active = true;

    listWishlistItems()
      .then((response) => {
        if (!active) {
          return;
        }

        const existing = (Array.isArray(response) ? response : []).find(
          (item) => Number(item.productType) === 5 && String(item.targetId || '') === String(resolvedTourId),
        );

        setWishlistItemId(existing?.id || '');
      })
      .catch(() => {
        if (active) {
          setWishlistItemId('');
        }
      });

    return () => {
      active = false;
    };
  }, [resolvedTourId, session.isAuthenticated]);

  useEffect(() => {
    if (!session.isAuthenticated || !tour || !resolvedTourId) {
      return;
    }

    const activeSchedule = tour?.upcomingSchedules?.find((item) => item.id === selectedScheduleId) || null;
    const priceValue = quote?.totalAmount || activeSchedule?.adultPrice || tour.upcomingSchedules?.[0]?.adultPrice || undefined;
    const currencyCode = quote?.currencyCode || activeSchedule?.currencyCode || 'VND';

    trackRecentView({
      productType: 'tour',
      targetId: resolvedTourId,
      title: tour.name,
      subtitle: getTourTypeLabel(tour.type),
      locationText: [tour.province, tour.city].filter(Boolean).join(', '),
      priceValue,
      priceText: priceValue ? formatCurrency(priceValue, currencyCode) : undefined,
      currencyCode,
      imageUrl: activeImage || gallery[0] || tour.coverImageUrl,
      targetUrl: `${location.pathname}${location.search}`,
    }).catch(() => {});
  }, [activeImage, gallery, location.pathname, location.search, quote, resolvedTourId, selectedScheduleId, session.isAuthenticated, tour]);

  const selectedSchedule = useMemo(
    () => tour?.upcomingSchedules?.find((item) => item.id === selectedScheduleId) || null,
    [tour, selectedScheduleId],
  );

  const highlights = useMemo(() => parseJsonList(tour?.highlightsJson), [tour?.highlightsJson]);
  const includes = useMemo(() => parseJsonList(tour?.includesJson), [tour?.includesJson]);
  const excludes = useMemo(() => parseJsonList(tour?.excludesJson), [tour?.excludesJson]);

  function handleQuantityChange(setter, nextValue) {
    const value = Math.max(0, nextValue);
    if (setter === setAdults && value === 0) {
      return;
    }

    setter(value);
  }

  function handleBookNow() {
    if (!tour || !selectedScheduleId) {
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

    const params = new URLSearchParams({
      type: 'tour',
      tourId: tour.id,
      scheduleId: selectedScheduleId,
      adult: String(adults),
      child: String(children),
    });

    if (quote?.package?.packageId) {
      params.set('packageId', quote.package.packageId);
    }

    navigate(`/checkout?${params.toString()}`);
  }

  async function handleToggleWishlist() {
    if (!tour) {
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
      } catch {
        // Keep current UI state when the request fails.
      }
      return;
    }

    const priceValue = quote?.totalAmount || selectedSchedule?.adultPrice || tour.upcomingSchedules?.[0]?.adultPrice || undefined;
    const currencyCode = quote?.currencyCode || selectedSchedule?.currencyCode || 'VND';

    try {
      const created = await addWishlistItem({
        productType: 'tour',
        targetId: tour.id,
        title: tour.name,
        subtitle: getTourTypeLabel(tour.type),
        locationText: tour.city || tour.province || 'Điểm đến tour',
        priceValue,
        priceText: priceValue ? formatCurrency(priceValue, currencyCode) : undefined,
        currencyCode,
        imageUrl: tour.coverImageUrl || activeImage,
        targetUrl: `${location.pathname}${location.search}`,
      });

      setWishlistItemId(created?.id || '');
    } catch {
      // Keep current UI state when the request fails.
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 pt-32 pb-24">
        <div className="container mx-auto px-4">
          <div className="bg-white rounded-[3rem] border border-slate-100 shadow-sm p-12 text-center text-sm font-bold text-slate-400">
            Đang tải chi tiết tour...
          </div>
        </div>
      </div>
    );
  }

  if (error || !tour) {
    return (
      <div className="min-h-screen bg-slate-50 pt-32 pb-24">
        <div className="container mx-auto px-4">
          <div className="bg-rose-50 rounded-[3rem] border border-rose-100 shadow-sm p-12 text-center text-sm font-bold text-rose-600">
            {error || 'Không tìm thấy tour.'}
          </div>
        </div>
      </div>
    );
  }

  const galleryItems = gallery.length
    ? gallery
    : tour.images?.length
    ? tour.images
    : [{ imageUrl: tour.coverImageUrl, altText: tour.name }];

  return (
    <div className="min-h-screen bg-slate-50 pb-24">
      <div className="h-[550px] relative flex items-center justify-center overflow-hidden">
        <img
          src={tour.coverImageUrl || activeImage || 'https://images.unsplash.com/photo-1559592413-7cea732639f5?auto=format&fit=crop&q=80&w=2000'}
          alt={tour.name}
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
            <Link to="/tours" className="hover:text-white transition-colors">Tour du lịch</Link>
            <ChevronLeft size={12} className="rotate-180" />
            <span className="text-white">Chi tiết tour</span>
          </motion.div>
          <motion.h1
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="text-5xl md:text-8xl font-black text-white tracking-tighter leading-none"
          >
            {tour.name}
          </motion.h1>
          <div className="flex flex-wrap items-center justify-center gap-6 mt-10">
            <div className="flex items-center gap-3 px-6 py-2 bg-white/10 backdrop-blur-md rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-widest">
              <Clock size={14} className="text-[#1EB4D4]" /> {formatDuration(tour.durationDays, tour.durationNights)}
            </div>
            <div className="flex items-center gap-3 px-6 py-2 bg-white/10 backdrop-blur-md rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-widest">
              <Users size={14} className="text-[#1EB4D4]" /> {tour.minGuests || 1}-{tour.maxGuests || 30} khách
            </div>
            <div className="flex items-center gap-3 px-6 py-2 bg-[#1EB4D4] rounded-full text-white text-[10px] font-black uppercase tracking-widest shadow-lg shadow-[#1EB4D4]/30">
              <Star size={14} fill="white" /> {(tour.reviewSummary?.average || 0).toFixed(1)} ({tour.reviewSummary?.count || 0} đánh giá)
            </div>
          </div>
        </div>
      </div>

      <div className="container mx-auto px-4 -mt-20 relative z-20">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
          <div className="lg:col-span-2 space-y-12">
            <div className="bg-white rounded-[3.5rem] p-12 shadow-xl shadow-slate-200/50 border border-slate-100 italic">
              <p className="text-2xl font-black text-slate-900 leading-relaxed tracking-tight">
                {tour.shortDescription || 'Khám phá hành trình được thiết kế tối ưu cho trải nghiệm, lưu trú và khám phá bản sắc địa phương.'}
              </p>
              <div className="flex items-center gap-4 mt-8 pt-8 border-t border-slate-50">
                <div className="w-12 h-12 bg-slate-50 rounded-2xl flex items-center justify-center text-[#1EB4D4]">
                  <MapPin size={24} />
                </div>
                <div>
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Điểm đến</p>
                  <p className="text-lg font-black text-slate-900">
                    {tour.city || tour.province || 'Liên hệ để cập nhật lộ trình'}
                  </p>
                </div>
              </div>
            </div>

            <div className="mb-12">
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                className="rounded-[2.5rem] overflow-hidden mb-6 shadow-2xl h-[500px] bg-white"
              >
                <img src={activeImage} alt={tour.name} className="w-full h-full object-cover" />
              </motion.div>
              <div className="flex gap-4 overflow-x-auto">
                {galleryItems.map((image, index) => (
                  <button
                    key={`${image.imageUrl || 'image'}-${index}`}
                    type="button"
                    onClick={() => setActiveImage(image.imageUrl || tour.coverImageUrl || activeImage)}
                    className="w-32 h-24 rounded-2xl overflow-hidden border-4 border-transparent hover:border-[#1EB4D4] transition-all shrink-0"
                  >
                    <img
                      src={image.imageUrl || tour.coverImageUrl || activeImage}
                      alt={image.altText || tour.name}
                      className="w-full h-full object-cover"
                    />
                  </button>
                ))}
              </div>
            </div>

            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
              <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                <div className="w-12 h-12 bg-slate-900 rounded-2xl flex items-center justify-center text-white">
                  <Mountain size={24} />
                </div>
                Điểm nổi bật & dịch vụ
              </h2>

              {highlights.length > 0 && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                  {highlights.map((item, index) => (
                    <div
                      key={`${item}-${index}`}
                      className="flex items-center gap-5 p-6 bg-slate-50 rounded-[2.5rem] border border-transparent hover:border-[#1EB4D4]/20 hover:bg-white transition-all group"
                    >
                      <div className="w-14 h-14 bg-white rounded-2xl flex items-center justify-center text-slate-400 group-hover:text-[#1EB4D4] shadow-sm transition-colors">
                        <Camera size={20} />
                      </div>
                      <span className="text-sm font-black text-slate-900 uppercase tracking-widest leading-snug">{item}</span>
                    </div>
                  ))}
                </div>
              )}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="bg-slate-50 rounded-[2rem] p-6">
                  <p className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.2em] mb-3">Bao gồm</p>
                  <div className="space-y-3">
                    {includes.length === 0 ? (
                      <div className="text-sm font-bold text-slate-400">Đơn vị tổ chức sẽ cập nhật danh sách dịch vụ bao gồm sau.</div>
                    ) : includes.map((item, index) => (
                      <div key={`${item}-${index}`} className="flex items-start gap-3">
                        <CheckCircle2 size={16} className="text-[#1EB4D4] mt-0.5 shrink-0" />
                        <span className="text-sm font-bold text-slate-600">{item}</span>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="bg-slate-50 rounded-[2rem] p-6">
                  <p className="text-[10px] font-black text-rose-500 uppercase tracking-[0.2em] mb-3">Không bao gồm</p>
                  <div className="space-y-3">
                    {excludes.length === 0 ? (
                      <div className="text-sm font-bold text-slate-400">Hiện chưa có ghi chú loại trừ riêng cho tour này.</div>
                    ) : excludes.map((item, index) => (
                      <div key={`${item}-${index}`} className="flex items-start gap-3">
                        <Info size={16} className="text-rose-400 mt-0.5 shrink-0" />
                        <span className="text-sm font-bold text-slate-600">{item}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
              <div className="bg-white rounded-[3rem] p-10 shadow-sm border border-slate-100">
                <h2 className="text-2xl font-black text-slate-900 mb-6">Chính sách tour</h2>
                <div className="space-y-4">
                  {policies.length === 0 ? (
                    <div className="p-5 bg-slate-50 rounded-2xl border border-slate-100 text-sm font-medium text-slate-400">
                      Tour này chưa công bố chính sách chi tiết.
                    </div>
                  ) : policies.map((policy) => (
                    <div key={policy.id} className="p-5 bg-slate-50 rounded-2xl border border-slate-100">
                      <p className="font-black text-slate-900">{policy.name}</p>
                      <p className="text-sm text-slate-500 font-medium mt-2">{policy.shortDescription || policy.descriptionMarkdown || policy.code}</p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="bg-white rounded-[3rem] p-10 shadow-sm border border-slate-100">
                <h2 className="text-2xl font-black text-slate-900 mb-6">Dịch vụ thêm</h2>
                <div className="space-y-4">
                  {addons.length === 0 ? (
                    <div className="p-5 bg-slate-50 rounded-2xl border border-slate-100 text-sm font-medium text-slate-400">
                      Chưa có addon công khai cho tour này.
                    </div>
                  ) : addons.map((addon) => (
                    <div key={addon.id} className="p-5 bg-slate-50 rounded-2xl border border-slate-100 flex items-start justify-between gap-4">
                      <div>
                        <p className="font-black text-slate-900">{addon.name}</p>
                        <p className="text-sm text-slate-500 font-medium mt-2">{addon.shortDescription || addon.descriptionMarkdown || addon.code}</p>
                      </div>
                      <div className="text-right shrink-0">
                        <p className="text-lg font-black text-[#1EB4D4]">{formatCurrency(addon.basePrice || 0, addon.currencyCode)}</p>
                        <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{addon.isPerPerson ? 'Theo khách' : 'Theo booking'}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
              <h2 className="text-2xl font-black text-slate-900 mb-12 flex items-center gap-4">
                <div className="w-12 h-12 bg-[#1EB4D4] rounded-2xl flex items-center justify-center text-white">
                  <Clock size={24} />
                </div>
                Lịch trình chi tiết
              </h2>
              <div className="space-y-6">
                {itinerary.length === 0 ? (
                  <div className="p-6 bg-slate-50 rounded-2xl border border-slate-100 text-sm font-medium text-slate-400">
                    Đơn vị tổ chức đang cập nhật lịch trình chi tiết cho tour này.
                  </div>
                ) : itinerary.map((day, index) => (
                  <div key={day.id} className="border border-slate-100 rounded-[1.8rem] overflow-hidden">
                    <button
                      type="button"
                      onClick={() => setActiveDay(activeDay === day.id ? null : day.id)}
                      className={`w-full px-8 py-6 flex items-center justify-between text-left transition-colors ${
                        activeDay === day.id ? 'bg-[#1EB4D4] text-white' : 'bg-slate-50 text-slate-900'
                      }`}
                    >
                      <div>
                        <p className="text-[10px] font-black uppercase tracking-[0.2em] opacity-70 mb-2">Ngày {day.dayNumber || index + 1}</p>
                        <span className="font-bold text-lg">{day.title || `Ngày ${day.dayNumber || index + 1}`}</span>
                      </div>
                      <ChevronDown size={20} className={activeDay === day.id ? 'rotate-180' : ''} />
                    </button>

                    <AnimatePresence>
                      {activeDay === day.id && (
                        <motion.div
                          initial={{ height: 0, opacity: 0 }}
                          animate={{ height: 'auto', opacity: 1 }}
                          exit={{ height: 0, opacity: 0 }}
                          className="bg-white px-8 py-8 overflow-hidden"
                        >
                          <p className="text-gray-500 font-medium leading-relaxed mb-6">
                            {day.shortDescription || day.descriptionMarkdown || 'Lịch trình đang được cập nhật chi tiết.'}
                          </p>
                          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {(day.items || []).map((item) => (
                              <div
                                key={item.id}
                                className="flex items-start gap-3 p-4 bg-slate-50 rounded-2xl border border-transparent hover:border-[#1EB4D4]/10 hover:bg-white transition-all"
                              >
                                <CheckCircle2 size={16} className="text-[#1EB4D4] mt-0.5 shrink-0" />
                                <div>
                                  <p className="text-sm font-black text-slate-900">{item.title}</p>
                                  <p className="text-xs font-medium text-slate-500 mt-1">
                                    {item.locationName || item.shortDescription || item.descriptionMarkdown}
                                  </p>
                                </div>
                              </div>
                            ))}
                          </div>
                        </motion.div>
                      )}
                    </AnimatePresence>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
              <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                <div className="w-12 h-12 bg-slate-900 rounded-2xl flex items-center justify-center text-white">
                  <MessageSquare size={22} />
                </div>
                Câu hỏi thường gặp
              </h2>
              <div className="space-y-4">
                {faqs.length === 0 ? (
                  <div className="p-6 bg-slate-50 rounded-2xl border border-slate-100 text-sm font-medium text-slate-400">
                    Chưa có câu hỏi thường gặp nổi bật cho tour này.
                  </div>
                ) : faqs.map((faq) => (
                  <div key={faq.id} className="p-6 bg-slate-50 rounded-2xl border border-slate-100">
                    <p className="font-black text-slate-900 mb-2">{faq.question}</p>
                    <p className="text-sm text-slate-500 font-medium">{faq.answerMarkdown}</p>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-white rounded-[3.5rem] p-12 shadow-sm border border-slate-100">
              <h2 className="text-2xl font-black text-slate-900 mb-10 flex items-center gap-4">
                <div className="w-12 h-12 bg-[#1EB4D4] rounded-2xl flex items-center justify-center text-white">
                  <Star size={22} />
                </div>
                Đánh giá khách hàng
              </h2>
              <div className="space-y-4">
                {reviews.length === 0 ? (
                  <div className="p-6 bg-slate-50 rounded-2xl border border-slate-100 text-sm font-medium text-slate-400">
                    Chưa có đánh giá công khai cho tour này.
                  </div>
                ) : reviews.map((review) => (
                  <div key={review.id} className="p-6 bg-slate-50 rounded-2xl border border-slate-100">
                    <div className="flex items-center justify-between gap-4 mb-3">
                      <div>
                        <p className="font-black text-slate-900">{review.reviewerName || 'Khách hàng ẩn danh'}</p>
                        <p className="text-[11px] font-bold text-slate-400">{formatDateTime(review.publishedAt || review.createdAt)}</p>
                      </div>
                      <div className="flex items-center gap-1 text-amber-500 font-black">
                        <Star size={14} fill="currentColor" />
                        {Number(review.rating || 0).toFixed(1)}
                      </div>
                    </div>
                    <p className="text-sm font-bold text-slate-800 mb-2">{review.title || 'Trải nghiệm tour'}</p>
                    <p className="text-sm text-slate-500 font-medium">{review.content}</p>
                    {review.replyContent && (
                      <div className="mt-4 p-4 bg-white rounded-2xl border border-slate-100">
                        <p className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.2em] mb-2">Phản hồi từ đơn vị tổ chức</p>
                        <p className="text-sm font-medium text-slate-500">{review.replyContent}</p>
                      </div>
                    )}
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
                  <button
                    type="button"
                    onClick={handleToggleWishlist}
                    className={`absolute right-0 top-0 w-12 h-12 rounded-2xl border transition-all flex items-center justify-center ${
                      wishlistItemId
                        ? 'bg-rose-500 border-rose-500 text-white'
                        : 'bg-white border-slate-100 text-slate-400 hover:border-rose-200 hover:text-rose-500'
                    }`}
                  >
                    <Heart size={18} className={wishlistItemId ? 'fill-white' : ''} />
                  </button>
                  <p className="text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2">Giá từ</p>
                  <div className="flex items-baseline gap-2 mb-8">
                    <p className="text-5xl font-black text-[#1EB4D4] tracking-tighter">
                      {formatCurrency(quote?.totalAmount || selectedSchedule?.adultPrice || tour.upcomingSchedules?.[0]?.adultPrice || 0)}
                    </p>
                    <span className="text-xs font-bold text-slate-400">/ đặt chỗ</span>
                  </div>

                  <div className="space-y-6 mb-8">
                    <div>
                      <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest ml-1 mb-2 block">Lịch khởi hành</label>
                      <div className="relative">
                        <Calendar className="absolute left-5 top-1/2 -translate-y-1/2 text-[#1EB4D4]" size={18} />
                        <select
                          value={selectedScheduleId}
                          onChange={(event) => setSelectedScheduleId(event.target.value)}
                          className="w-full h-14 bg-slate-50 rounded-2xl pl-14 pr-6 font-bold text-slate-900 border-none outline-none appearance-none"
                        >
                          {(tour.upcomingSchedules || []).map((schedule) => (
                            <option key={schedule.id} value={schedule.id}>
                              {formatDate(schedule.departureDate)} - {formatTime(schedule.departureTime)}
                            </option>
                          ))}
                        </select>
                      </div>
                      {selectedSchedule && (
                        <p className="text-[11px] font-bold text-slate-400 mt-2">
                          Còn {selectedSchedule.availableSlots || 0} chỗ, {selectedSchedule.isInstantConfirm ? 'xác nhận ngay' : 'xác nhận theo lịch'}
                        </p>
                      )}
                    </div>

                    <div className="space-y-4">
                      {[
                        { label: 'Người lớn', count: adults, setter: setAdults },
                        { label: 'Trẻ em', count: children, setter: setChildren },
                      ].map((group) => (
                        <div key={group.label} className="flex items-center justify-between p-4 bg-slate-50 rounded-2xl">
                          <span className="text-sm font-black text-slate-900">{group.label}</span>
                          <div className="flex items-center gap-4">
                            <button
                              type="button"
                              onClick={() => handleQuantityChange(group.setter, group.count - 1)}
                              className="w-8 h-8 rounded-xl bg-white flex items-center justify-center text-slate-600 hover:text-[#1EB4D4] shadow-sm"
                            >
                              <Minus size={14} />
                            </button>
                            <span className="w-4 text-center font-black text-slate-900">{group.count}</span>
                            <button
                              type="button"
                              onClick={() => handleQuantityChange(group.setter, group.count + 1)}
                              className="w-8 h-8 rounded-xl bg-white flex items-center justify-center text-slate-600 hover:text-[#1EB4D4] shadow-sm"
                            >
                              <Plus size={14} />
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="pt-6 border-t border-slate-50 mb-8">
                    {quoteLoading ? (
                      <p className="text-sm font-bold text-slate-400">Đang cập nhật báo giá...</p>
                    ) : quoteError ? (
                      <p className="text-sm font-bold text-rose-500">{quoteError}</p>
                    ) : quote ? (
                      <>
                        <div className="space-y-3 mb-4">
                          {quote.passengerLines.map((line) => (
                            <div key={`${line.code}-${line.name}`} className="flex justify-between text-sm font-bold text-slate-500">
                              <span>{line.name} x{line.quantity}</span>
                              <span>{formatCurrency(line.lineBaseAmount, line.currencyCode)}</span>
                            </div>
                          ))}
                        </div>
                        <div className="flex justify-between items-center mb-2">
                          <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Tổng chi phí</span>
                          <p className="text-3xl font-black text-[#1EB4D4] tracking-tighter">
                            {formatCurrency(quote.totalAmount, quote.currencyCode)}
                          </p>
                        </div>
                        <p className="text-[10px] text-slate-400 font-bold italic">
                          {quote.notes?.[0] || 'Giá đã bao gồm các khoản thuế và phí theo lịch khởi hành.'}
                        </p>
                      </>
                    ) : (
                      <p className="text-sm font-bold text-slate-400">Chọn lịch khởi hành để xem báo giá.</p>
                    )}
                  </div>

                  <button
                    type="button"
                    onClick={handleBookNow}
                    disabled={!selectedScheduleId || quoteLoading}
                    className="w-full h-16 bg-slate-900 text-white rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30 disabled:opacity-60"
                  >
                    Đặt tour ngay <ArrowRight size={18} />
                  </button>

                  <div className="mt-8 pt-8 border-t border-slate-50 flex items-center gap-3 text-[#1EB4D4]">
                    <ShieldCheck size={18} />
                    <p className="text-[10px] font-black uppercase tracking-widest">
                      {tour.isInstantConfirm ? 'Lịch này xác nhận ngay' : 'Giữ chỗ theo lịch khởi hành'}
                    </p>
                  </div>
                </div>
              </div>

              <div className="bg-[#1EB4D4]/5 p-8 rounded-[3rem] border border-[#1EB4D4]/10">
                <div className="flex items-center gap-4 mb-4">
                  <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm">
                    <Info size={20} />
                  </div>
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Thông tin nhanh</p>
                </div>
                <div className="space-y-3 text-[11px] text-slate-600 font-bold leading-relaxed">
                  <p>Loại tour: {getTourTypeLabel(tour.type)}</p>
                  <p>Độ khó: {getDifficultyLabel(tour.difficulty)}</p>
                  <p>Khởi hành từ: {tour.meetingPointSummary || 'Điểm hẹn sẽ được cập nhật theo lịch'}</p>
                  <p>Chính sách riêng: {tour.isPrivateTourSupported ? 'Có hỗ trợ tour riêng' : 'Tour ghép đoàn tiêu chuẩn'}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
