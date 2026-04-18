import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import {
  Compass,
  Search,
  Star,
  Clock,
  Users,
  ArrowRight,
  Heart,
  Filter,
  X,
} from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { listPublicTours } from '../../../services/tourService';
import { addWishlistItem, deleteWishlistItem, listWishlistItems } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  formatCurrency,
  formatDuration,
  getDifficultyLabel,
  getTourTypeLabel,
} from '../../tours/utils/presentation';

const SORT_OPTIONS = [
  'Phổ biến',
  'Giá thấp',
  'Giá cao',
  'Khởi hành sớm',
];

function sortTours(items, sortKey) {
  const next = [...items];

  switch (sortKey) {
    case 'Giá thấp':
      return next.sort((a, b) => Number(a.fromAdultPrice || 0) - Number(b.fromAdultPrice || 0));
    case 'Giá cao':
      return next.sort((a, b) => Number(b.fromAdultPrice || 0) - Number(a.fromAdultPrice || 0));
    case 'Khởi hành sớm':
      return next.sort((a, b) => String(a.nextDepartureDate || '').localeCompare(String(b.nextDepartureDate || '')));
    case 'Phổ biến':
    default:
      return next.sort((a, b) => Number(b.reviewAverage || 0) - Number(a.reviewAverage || 0));
  }
}

function buildCategoryList(items) {
  const values = Array.from(
    new Set(
      items
        .map((item) => item.province || item.city || '')
        .filter(Boolean),
    ),
  ).slice(0, 8);

  return ['Tất cả', ...values];
}

export default function TourResultsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const session = useAuthSession();
  const [searchParams, setSearchParams] = useSearchParams();
  const [search, setSearch] = useState(searchParams.get('q') || '');
  const [category, setCategory] = useState(searchParams.get('province') || 'Tất cả');
  const [sort, setSort] = useState('Phổ biến');
  const [showFilter, setShowFilter] = useState(false);
  const [liked, setLiked] = useState([]);
  const [wishlistMap, setWishlistMap] = useState({});
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    async function loadTours() {
      setLoading(true);
      setError('');

      try {
        const response = await listPublicTours({
          q: search || undefined,
          province: category !== 'Tất cả' ? category : undefined,
          page: 1,
          pageSize: 24,
          upcomingOnly: true,
        });

        if (!active) {
          return;
        }

        setItems(response.items || []);
      } catch (err) {
        if (!active) {
          return;
        }

        setItems([]);
        setError(err.message || 'Không thể tải danh sách tour.');
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    loadTours();

    return () => {
      active = false;
    };
  }, [category, search]);

  useEffect(() => {
    if (!session.isAuthenticated) {
      setLiked([]);
      setWishlistMap({});
      return;
    }

    let active = true;

    listWishlistItems()
      .then((response) => {
        if (!active) {
          return;
        }

        const nextMap = {};
        const nextLiked = [];

        (Array.isArray(response) ? response : [])
          .filter((item) => Number(item.productType) === 5)
          .forEach((item) => {
            const targetId = String(item.targetId || '');
            if (!targetId) {
              return;
            }

            nextMap[targetId] = item.id;
            nextLiked.push(targetId);
          });

        setWishlistMap(nextMap);
        setLiked(nextLiked);
      })
      .catch(() => {
        if (active) {
          setWishlistMap({});
          setLiked([]);
        }
      });

    return () => {
      active = false;
    };
  }, [session.isAuthenticated]);

  useEffect(() => {
    const next = new URLSearchParams(searchParams);

    if (search) {
      next.set('q', search);
    } else {
      next.delete('q');
    }

    if (category && category !== 'Tất cả') {
      next.set('province', category);
    } else {
      next.delete('province');
    }

    setSearchParams(next, { replace: true });
  }, [category, search, searchParams, setSearchParams]);

  const categories = useMemo(() => buildCategoryList(items), [items]);
  const filteredTours = useMemo(() => sortTours(items, sort), [items, sort]);

  const toggleLike = async (tour) => {
    const targetId = String(tour.id);

    if (!session.isAuthenticated) {
      navigate('/auth/login', {
        state: {
          returnTo: `${location.pathname}${location.search}`,
        },
      });
      return;
    }

    const existingId = wishlistMap[targetId];
    if (existingId) {
      try {
        await deleteWishlistItem(existingId);
        setWishlistMap((prev) => {
          const next = { ...prev };
          delete next[targetId];
          return next;
        });
        setLiked((prev) => prev.filter((value) => value !== targetId));
      } catch {
        // Keep current UI state when the request fails.
      }
      return;
    }

    try {
      const created = await addWishlistItem({
        productType: 'tour',
        targetId,
        title: tour.name,
        subtitle: getTourTypeLabel(tour.type),
        locationText: tour.province || tour.city || 'Điểm đến tour',
        priceValue: tour.fromAdultPrice || undefined,
        priceText: formatCurrency(tour.fromAdultPrice, 'VND'),
        currencyCode: 'VND',
        imageUrl: tour.coverImageUrl,
        targetUrl: `/tour/${tour.id}`,
      });

      setWishlistMap((prev) => ({ ...prev, [targetId]: created?.id || '' }));
      setLiked((prev) => [...prev, targetId]);
    } catch {
      // Keep current UI state when the request fails.
    }
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-24">
        <div className="h-[450px] relative flex items-center justify-center overflow-hidden">
          <motion.div
            initial={{ scale: 1.1 }}
            animate={{ scale: 1 }}
            transition={{ duration: 15, repeat: Infinity, repeatType: 'reverse' }}
            className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1534008843454-b310df66932c?auto=format&fit=crop&q=80&w=2000')] bg-cover bg-center brightness-[0.4]"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-50 via-slate-900/40 to-slate-900/60" />

          <div className="container mx-auto px-4 relative z-10 text-center">
            <motion.h1
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              className="text-6xl md:text-8xl font-black text-white tracking-tighter leading-none mb-8"
            >
              KHÁM PHÁ <span className="text-[#1EB4D4] italic">Tour Việt</span>
            </motion.h1>

            <div className="max-w-4xl mx-auto flex flex-col md:flex-row gap-4 bg-white/10 backdrop-blur-3xl p-4 rounded-[3rem] border border-white/20 shadow-2xl">
              <div className="flex-1 flex items-center gap-4 px-8 py-4 bg-white rounded-[2rem] shadow-inner">
                <Search size={24} className="text-[#1EB4D4]" />
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Bạn muốn đi đâu hôm nay?"
                  className="w-full bg-transparent border-none outline-none font-black text-slate-900 text-sm placeholder:text-slate-300"
                />
              </div>
              <button
                type="button"
                onClick={() => setShowFilter((prev) => !prev)}
                className={`px-10 py-4 rounded-[2rem] font-black text-[10px] uppercase tracking-[0.2em] transition-all flex items-center justify-center gap-3 ${
                  showFilter ? 'bg-[#1EB4D4] text-white' : 'bg-slate-900 text-white'
                }`}
              >
                {showFilter ? <X size={18} /> : <Filter size={18} />} Bộ lọc
              </button>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-10 relative z-20">
          <div className="flex flex-wrap items-center justify-center gap-2 mb-16 bg-white p-2 rounded-[2.5rem] shadow-xl border border-slate-100 max-w-fit mx-auto">
            {categories.map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setCategory(item)}
                className={`flex items-center gap-2 px-8 py-4 rounded-[1.8rem] text-[10px] font-black uppercase tracking-[0.2em] transition-all relative overflow-hidden ${
                  category === item ? 'bg-slate-900 text-white shadow-lg' : 'text-slate-400 hover:text-slate-900'
                }`}
              >
                {category === item && (
                  <motion.div layoutId="tour-tab-glow" className="absolute inset-0 bg-gradient-to-r from-[#1EB4D4]/20 to-transparent pointer-events-none" />
                )}
                {item}
              </button>
            ))}
          </div>

          <div className="flex flex-col lg:flex-row gap-12">
            <aside className="w-full lg:w-1/4 space-y-8">
              <div className="bg-white rounded-[3.5rem] p-10 border border-slate-100 shadow-sm sticky top-32 overflow-hidden">
                <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16" />
                <h3 className="font-black text-slate-900 text-xl tracking-tighter mb-8">Sắp xếp theo</h3>
                <div className="space-y-4">
                  {SORT_OPTIONS.map((item) => (
                    <button
                      key={item}
                      type="button"
                      onClick={() => setSort(item)}
                      className={`w-full flex items-center justify-between p-5 rounded-2xl border-2 transition-all ${
                        sort === item ? 'bg-slate-50 border-[#1EB4D4] text-[#1EB4D4]' : 'bg-white border-slate-50 text-slate-400 hover:border-slate-100'
                      }`}
                    >
                      <span className="text-[11px] font-black uppercase tracking-widest">{item}</span>
                      {sort === item && <div className="w-2 h-2 rounded-full bg-[#1EB4D4] shadow-lg shadow-[#1EB4D4]/50" />}
                    </button>
                  ))}
                </div>

                {showFilter && (
                  <div className="mt-12 pt-12 border-t border-slate-50">
                    <div className="p-8 bg-slate-900 rounded-[2.5rem] text-white italic">
                      <p className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-widest mb-3">Gợi ý</p>
                      <p className="text-xs font-bold text-white/60 leading-relaxed">
                        Ưu tiên các lịch khởi hành sớm và tour có đánh giá cao để chốt booking nhanh hơn.
                      </p>
                    </div>
                  </div>
                )}
              </div>
            </aside>

            <div className="flex-1">
              {loading ? (
                <div className="bg-white rounded-[3rem] border border-slate-100 shadow-sm p-12 text-center text-sm font-bold text-slate-400">
                  Đang tải danh sách tour...
                </div>
              ) : error ? (
                <div className="bg-rose-50 rounded-[3rem] border border-rose-100 shadow-sm p-12 text-center text-sm font-bold text-rose-600">
                  {error}
                </div>
              ) : filteredTours.length === 0 ? (
                <div className="bg-white rounded-[3rem] border border-slate-100 shadow-sm p-12 text-center">
                  <p className="text-lg font-black text-slate-900 mb-2">Chưa có tour phù hợp</p>
                  <p className="text-sm font-medium text-slate-400">Thử thay đổi điểm đến hoặc từ khóa tìm kiếm để xem thêm kết quả.</p>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
                  {filteredTours.map((tour, index) => (
                    <motion.div
                      key={tour.id}
                      initial={{ opacity: 0, y: 30 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: index * 0.05 }}
                      className="group bg-white rounded-[3.5rem] overflow-hidden shadow-sm border border-slate-100 hover:shadow-[0_40px_80px_-20px_rgba(0,0,0,0.1)] transition-all duration-700 flex flex-col"
                    >
                      <div className="relative h-72 overflow-hidden">
                        <img
                          src={tour.coverImageUrl || 'https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=1200&auto=format&fit=crop'}
                          alt={tour.name}
                          className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-1000"
                        />
                        <div className="absolute inset-0 bg-gradient-to-t from-slate-900/80 via-transparent to-transparent opacity-60" />
                        <button
                          type="button"
                          onClick={() => toggleLike(tour)}
                          className={`absolute top-8 right-8 w-12 h-12 rounded-2xl backdrop-blur-xl border border-white/20 flex items-center justify-center transition-all ${
                            liked.includes(String(tour.id)) ? 'bg-rose-500 text-white' : 'bg-white/10 text-white hover:bg-white/20'
                          }`}
                        >
                          <Heart size={20} className={liked.includes(String(tour.id)) ? 'fill-white' : ''} />
                        </button>
                        <div className="absolute bottom-8 left-8 flex flex-wrap gap-2">
                          <span className="px-5 py-2 bg-white/10 backdrop-blur-md rounded-2xl text-[9px] font-black text-white border border-white/20 uppercase tracking-widest">
                            {tour.province || tour.city || getTourTypeLabel(tour.type)}
                          </span>
                          <span className="px-5 py-2 bg-white/10 backdrop-blur-md rounded-2xl text-[9px] font-black text-white border border-white/20 uppercase tracking-widest">
                            {getDifficultyLabel(tour.difficulty)}
                          </span>
                        </div>
                      </div>

                      <div className="p-10 flex-1 flex flex-col">
                        <div className="flex items-center gap-2 text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.2em] mb-4">
                          <Compass size={14} /> {getTourTypeLabel(tour.type)}
                        </div>
                        <h3 className="text-2xl font-black text-slate-900 mb-6 leading-tight tracking-tighter group-hover:text-[#1EB4D4] transition-colors">
                          {tour.name}
                        </h3>

                        <div className="grid grid-cols-2 gap-4 mb-8">
                          <div className="flex items-center gap-3 text-slate-500">
                            <Clock size={16} className="text-slate-300" />
                            <span className="text-xs font-bold leading-none">
                              {formatDuration(tour.durationDays, tour.durationNights)}
                            </span>
                          </div>
                          <div className="flex items-center gap-3 text-slate-500">
                            <Users size={16} className="text-slate-300" />
                            <span className="text-xs font-bold leading-none">
                              {tour.availableSlots || 0} chỗ trống
                            </span>
                          </div>
                          <div className="flex items-center gap-3 text-slate-500">
                            <Star size={16} className="text-amber-400" fill="currentColor" />
                            <span className="text-xs font-bold leading-none">
                              {(tour.reviewAverage || 0).toFixed(1)} ({tour.reviewCount || 0} đánh giá)
                            </span>
                          </div>
                          <div className="flex items-center gap-3 text-slate-500">
                            <Clock size={16} className="text-slate-300" />
                            <span className="text-xs font-bold leading-none">
                              Khởi hành {tour.nextDepartureDate ? `từ ${tour.nextDepartureDate}` : 'linh hoạt'}
                            </span>
                          </div>
                        </div>

                        <div className="mt-auto pt-8 border-t border-slate-50 flex items-center justify-between">
                          <div>
                            <p className="text-[10px] font-black text-slate-300 uppercase tracking-widest mb-1">Giá từ</p>
                            <p className="text-3xl font-black text-slate-900 tracking-tighter">
                              {formatCurrency(tour.fromAdultPrice, 'VND')}
                            </p>
                          </div>
                          <Link
                            to={`/tour/${tour.id}`}
                            className="w-14 h-14 bg-slate-900 text-white rounded-[1.5rem] flex items-center justify-center hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30"
                          >
                            <ArrowRight size={24} />
                          </Link>
                        </div>
                      </div>
                    </motion.div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
