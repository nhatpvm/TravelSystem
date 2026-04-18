import React, { useEffect, useMemo, useState } from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import TourSidebar from '../components/TourSidebar';
import {
  ChevronRight,
  Star,
  Clock,
  Users,
  ArrowRight,
  Heart,
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { listPublicTours } from '../../../services/tourService';
import { addWishlistItem, deleteWishlistItem, listWishlistItems } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  formatCurrency,
  formatDuration,
  getDifficultyLabel,
  getTourTypeLabel,
} from '../utils/presentation';

function buildFilterOptions(items, field, allLabel) {
  const groups = new Map();
  groups.set('all', { value: 'all', label: allLabel, count: String(items.length).padStart(2, '0') });

  items.forEach((item) => {
    const value = item[field];
    if (!value) {
      return;
    }

    const current = groups.get(value) || { value, label: value, count: '00' };
    current.count = String(Number(current.count) + 1).padStart(2, '0');
    groups.set(value, current);
  });

  return Array.from(groups.values());
}

export default function OurTourPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const session = useAuthSession();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedDestination, setSelectedDestination] = useState('all');
  const [selectedType, setSelectedType] = useState('all');
  const [selectedDifficulty, setSelectedDifficulty] = useState('all');
  const [liked, setLiked] = useState([]);
  const [wishlistMap, setWishlistMap] = useState({});

  useEffect(() => {
    let active = true;

    async function loadTours() {
      setLoading(true);
      setError('');

      try {
        const response = await listPublicTours({
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
  }, []);

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

  const destinationOptions = useMemo(
    () => buildFilterOptions(items, 'province', 'Tất cả điểm đến'),
    [items],
  );

  const typeOptions = useMemo(() => {
    const counts = new Map();
    counts.set('all', { value: 'all', label: 'Tất cả loại tour', count: String(items.length).padStart(2, '0') });

    items.forEach((item) => {
      const value = String(item.type);
      const current = counts.get(value) || {
        value,
        label: getTourTypeLabel(item.type),
        count: '00',
      };
      current.count = String(Number(current.count) + 1).padStart(2, '0');
      counts.set(value, current);
    });

    return Array.from(counts.values());
  }, [items]);

  const difficultyOptions = useMemo(() => {
    const counts = new Map();
    counts.set('all', { value: 'all', label: 'Tất cả mức độ', count: String(items.length).padStart(2, '0') });

    items.forEach((item) => {
      const value = String(item.difficulty);
      const current = counts.get(value) || {
        value,
        label: getDifficultyLabel(item.difficulty),
        count: '00',
      };
      current.count = String(Number(current.count) + 1).padStart(2, '0');
      counts.set(value, current);
    });

    return Array.from(counts.values());
  }, [items]);

  const filteredItems = useMemo(() => items.filter((item) => {
    if (selectedDestination !== 'all' && item.province !== selectedDestination) {
      return false;
    }

    if (selectedType !== 'all' && String(item.type) !== String(selectedType)) {
      return false;
    }

    if (selectedDifficulty !== 'all' && String(item.difficulty) !== String(selectedDifficulty)) {
      return false;
    }

    return true;
  }), [items, selectedDestination, selectedType, selectedDifficulty]);

  const priceLabel = useMemo(() => {
    if (!items.length) {
      return 'Chưa có dữ liệu';
    }

    const prices = items.map((item) => Number(item.fromAdultPrice || 0)).filter((value) => value > 0);
    if (!prices.length) {
      return 'Liên hệ để báo giá';
    }

    return `${formatCurrency(Math.min(...prices))} - ${formatCurrency(Math.max(...prices))}`;
  }, [items]);

  const toggleLike = async (item) => {
    const targetId = String(item.id);

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
        title: item.name,
        subtitle: getTourTypeLabel(item.type),
        locationText: item.province || item.city || 'Điểm đến tour',
        priceValue: item.fromAdultPrice || undefined,
        priceText: formatCurrency(item.fromAdultPrice, 'VND'),
        currencyCode: 'VND',
        imageUrl: item.coverImageUrl,
        targetUrl: `/tour/${item.id}`,
      });

      setWishlistMap((prev) => ({ ...prev, [targetId]: created?.id || '' }));
      setLiked((prev) => [...prev, targetId]);
    } catch {
      // Keep current UI state when the request fails.
    }
  };

  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1467269204594-9661b134dd2b?q=80&w=2070"
            alt="Our Tour"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-black/30" />
        </div>

        <div className="container mx-auto px-4 relative z-10 text-center text-white">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-5xl md:text-7xl lg:text-8xl font-black mb-8 tracking-tighter"
          >
            Tour Của Chúng Tôi
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Tour của chúng tôi</span>
          </motion.div>
        </div>
      </section>

      <section className="py-24 bg-[#F8FBFB]">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="flex flex-col lg:flex-row gap-12">
            <TourSidebar
              destinations={destinationOptions}
              selectedDestination={selectedDestination}
              onDestinationChange={setSelectedDestination}
              tourTypes={typeOptions}
              selectedType={selectedType}
              onTypeChange={setSelectedType}
              difficulties={difficultyOptions}
              selectedDifficulty={selectedDifficulty}
              onDifficultyChange={setSelectedDifficulty}
              priceLabel={priceLabel}
            />

            <div className="w-full lg:w-3/4">
              {loading ? (
                <div className="bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-sm border border-slate-100">
                  Đang tải danh sách tour...
                </div>
              ) : error ? (
                <div className="bg-rose-50 rounded-[2rem] p-12 text-center text-sm font-bold text-rose-600 shadow-sm border border-rose-100">
                  {error}
                </div>
              ) : filteredItems.length === 0 ? (
                <div className="bg-white rounded-[2rem] p-12 text-center shadow-sm border border-slate-100">
                  <p className="text-lg font-black text-slate-900 mb-2">Chưa có tour phù hợp</p>
                  <p className="text-sm font-medium text-slate-400">Thử đổi điểm đến hoặc loại tour để xem thêm lựa chọn.</p>
                </div>
              ) : (
                <>
                  <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-8">
                    {filteredItems.map((item, index) => (
                      <motion.div
                        key={item.id}
                        initial={{ opacity: 0, y: 20 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        transition={{ delay: index * 0.05 }}
                        className="bg-white rounded-[2rem] border border-gray-100 shadow-xl overflow-hidden group hover:shadow-2xl transition-all duration-300"
                      >
                        <div className="relative h-[240px] overflow-hidden">
                          <img
                            src={item.coverImageUrl || 'https://images.unsplash.com/photo-1519046904884-53103b34b206?q=80&w=2070&auto=format&fit=crop'}
                            alt={item.name}
                            className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
                          />
                          <button
                            type="button"
                            onClick={() => toggleLike(item)}
                            className="absolute top-4 right-4 w-9 h-9 bg-black/20 hover:bg-black/40 backdrop-blur-md rounded-full flex items-center justify-center text-white transition-colors"
                          >
                            <Heart size={18} className={liked.includes(String(item.id)) ? 'fill-white' : ''} />
                          </button>
                          <div className="absolute top-4 left-4 bg-[#1EB4D4] text-white px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-wider">
                            {item.province || item.city || 'Tour nổi bật'}
                          </div>
                        </div>

                        <div className="p-6">
                          <div className="flex justify-between items-center mb-3">
                            <div className="flex items-center gap-1 text-[#1EB4D4] text-xs font-black">
                              <Star size={14} fill="currentColor" /> {(item.reviewAverage || 0).toFixed(1)}
                            </div>
                            <div className="text-gray-400 text-[10px] font-medium flex items-center gap-1">
                              <Clock size={14} /> {formatDuration(item.durationDays, item.durationNights)}
                            </div>
                          </div>

                          <h3 className="text-lg font-bold text-gray-900 mb-4 leading-tight group-hover:text-[#1EB4D4] transition-colors line-clamp-2 min-h-[3rem]">
                            {item.name}
                          </h3>

                          <div className="flex items-center justify-between text-[11px] font-bold text-slate-400 mb-4">
                            <span className="flex items-center gap-1">
                              <Users size={13} /> {item.availableSlots || 0} chỗ trống
                            </span>
                            <span>{getTourTypeLabel(item.type)}</span>
                          </div>

                          <div className="flex items-center justify-between pt-4 border-t border-dashed border-gray-100">
                            <span className="text-xl font-black text-gray-900">
                              {formatCurrency(item.fromAdultPrice, 'VND')}
                              <span className="text-[10px] text-gray-400 font-medium"> /khách</span>
                            </span>
                            <Link
                              to={`/tour/${item.id}`}
                              className="text-[#1EB4D4] hover:text-gray-900 font-black text-sm transition-all flex items-center gap-1"
                            >
                              Đặt ngay <ArrowRight size={14} />
                            </Link>
                          </div>
                        </div>
                      </motion.div>
                    ))}
                  </div>

                  <div className="flex justify-center mt-16">
                    <Link
                      to="/tour/results"
                      className="px-8 py-4 rounded-2xl bg-slate-900 text-white font-black text-xs uppercase tracking-[0.2em] hover:bg-[#1EB4D4] transition-all shadow-xl"
                    >
                      Xem trang kết quả chi tiết
                    </Link>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
}
