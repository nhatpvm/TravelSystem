import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { ChevronRight, List, Map as MapIcon, MapPin, Search, Star } from 'lucide-react';
import { motion } from 'framer-motion';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { listPublicHotels } from '../../../services/hotelService';
import { formatCurrency } from '../../tenant/hotel/utils/presentation';

function estimateNightlyPrice(hotel) {
  return hotel.starRating >= 5 ? 2800000 : hotel.starRating >= 4 ? 1800000 : 1200000;
}

export default function HotelResultsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [viewType, setViewType] = useState('grid');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [items, setItems] = useState([]);
  const [filters, setFilters] = useState({
    q: searchParams.get('q') || '',
    city: searchParams.get('city') || '',
    starMin: searchParams.get('starMin') || '',
    starMax: searchParams.get('starMax') || '',
  });

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const response = await listPublicHotels({
        q: filters.q || undefined,
        city: filters.city || undefined,
        starMin: filters.starMin || undefined,
        starMax: filters.starMax || undefined,
        pageSize: 30,
      });

      setItems(Array.isArray(response?.items) ? response.items : []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  const filteredItems = useMemo(() => {
    let nextItems = items;

    if (filters.q.trim()) {
      const keyword = filters.q.trim().toLowerCase();
      nextItems = nextItems.filter((item) => [item.name, item.city, item.province, item.shortDescription]
        .some((value) => String(value || '').toLowerCase().includes(keyword)));
    }

    if (filters.city.trim()) {
      const keyword = filters.city.trim().toLowerCase();
      nextItems = nextItems.filter((item) => String(item.city || item.province || '').toLowerCase().includes(keyword));
    }

    if (filters.starMin) {
      nextItems = nextItems.filter((item) => Number(item.starRating || 0) >= Number(filters.starMin));
    }

    if (filters.starMax) {
      nextItems = nextItems.filter((item) => Number(item.starRating || 0) <= Number(filters.starMax));
    }

    return nextItems;
  }, [items, filters]);

  function handleSearch(event) {
    event.preventDefault();
    setSearchParams(Object.fromEntries(Object.entries(filters).filter(([, value]) => value)));
    loadData();
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-12 overflow-hidden">
        <div className="h-[400px] relative flex items-center justify-center">
          <div className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&q=80&w=2000')] bg-cover bg-center brightness-[0.4]" />
          <div className="container mx-auto px-4 relative z-10 text-center">
            <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} className="flex items-center justify-center gap-2 text-white/70 text-[10px] font-black uppercase tracking-[0.3em] mb-6">
              <span>Trang chủ</span> <ChevronRight size={12} /> <span className="text-white">Khách sạn</span>
            </motion.div>
            <motion.h1 initial={{ opacity: 0, scale: 0.95 }} animate={{ opacity: 1, scale: 1 }} className="text-5xl md:text-7xl font-black text-white tracking-tighter leading-none">
              LƯU TRÚ <span className="text-[#1EB4D4] italic">Chuẩn gu</span>
            </motion.h1>
            <p className="text-white/60 font-bold mt-6 uppercase tracking-widest text-xs">Khám phá khách sạn và resort từ nhiều đối tác trên cùng một nền tảng</p>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-16 relative z-20">
          <form onSubmit={handleSearch} className="bg-white p-4 rounded-[2.5rem] shadow-2xl shadow-slate-200/50 border border-slate-100 flex flex-wrap items-center justify-between gap-6 mb-12">
            <div className="flex flex-wrap items-center gap-4 flex-1">
              <div className="min-w-[220px] flex-1 rounded-2xl bg-slate-50 px-5 py-4 flex items-center gap-3">
                <Search size={18} className="text-slate-300" />
                <input value={filters.q} onChange={(event) => setFilters((current) => ({ ...current, q: event.target.value }))} placeholder="Tên khách sạn hoặc khu vực" className="w-full bg-transparent outline-none text-sm font-bold text-slate-700" />
              </div>
              <input value={filters.city} onChange={(event) => setFilters((current) => ({ ...current, city: event.target.value }))} placeholder="Thành phố" className="min-w-[180px] rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
              <select value={filters.starMin} onChange={(event) => setFilters((current) => ({ ...current, starMin: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                <option value="">Từ sao</option>
                {[2, 3, 4, 5].map((star) => <option key={star} value={star}>{star} sao</option>)}
              </select>
              <select value={filters.starMax} onChange={(event) => setFilters((current) => ({ ...current, starMax: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                <option value="">Đến sao</option>
                {[3, 4, 5].map((star) => <option key={star} value={star}>{star} sao</option>)}
              </select>
            </div>

            <div className="flex items-center gap-6">
              <div className="flex items-center gap-2 border-r pr-6 border-slate-100">
                <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Hiển thị:</span>
                <button type="button" onClick={() => setViewType('grid')} className={`p-2.5 rounded-xl transition-all ${viewType === 'grid' ? 'bg-blue-50 text-[#1EB4D4]' : 'text-slate-300 hover:text-slate-500'}`}><List size={18} /></button>
                <button type="button" onClick={() => setViewType('list')} className={`p-2.5 rounded-xl transition-all ${viewType === 'list' ? 'bg-blue-50 text-[#1EB4D4]' : 'text-slate-300 hover:text-slate-500'}`}><MapIcon size={18} /></button>
              </div>
              <button type="submit" className="px-8 py-4 rounded-2xl bg-slate-900 text-white text-xs font-black uppercase tracking-[0.2em]">
                Tìm khách sạn
              </button>
            </div>
          </form>

          <div className="flex flex-col lg:flex-row gap-12">
            <aside className="w-full lg:w-1/4">
              <div className="bg-white rounded-[3rem] p-10 border border-slate-100 shadow-sm sticky top-28 overflow-hidden">
                <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16 -z-10" />
                <h3 className="font-black text-slate-900 text-2xl tracking-tight mb-10">Bộ lọc nhanh</h3>
                <div className="space-y-10">
                  <div>
                    <p className="text-[11px] font-black text-slate-900 uppercase tracking-[.2em] mb-4">Số sao</p>
                    <div className="space-y-3">
                      {[5, 4, 3].map((star) => (
                        <button key={star} type="button" onClick={() => setFilters((current) => ({ ...current, starMin: String(star) }))} className="flex items-center gap-3 text-sm font-bold text-slate-600 hover:text-slate-900">
                          <div className="flex text-amber-400">{Array.from({ length: star }).map((_, index) => <Star key={`${star}-${index}`} size={14} fill="currentColor" />)}</div>
                          Trở lên
                        </button>
                      ))}
                    </div>
                  </div>
                  <div>
                    <p className="text-[11px] font-black text-slate-900 uppercase tracking-[.2em] mb-4">Tóm tắt</p>
                    <div className="space-y-2 text-sm font-bold text-slate-500">
                      <p>{loading ? '--' : filteredItems.length} khách sạn phù hợp</p>
                      <p>Đa dạng đối tác, nhiều mức sao</p>
                      <p>Kiểm tra phòng trống trực tiếp tại trang chi tiết</p>
                    </div>
                  </div>
                </div>
              </div>
            </aside>

            <div className={`flex-1 grid ${viewType === 'list' ? 'grid-cols-1' : 'grid-cols-1 md:grid-cols-2'} gap-10`}>
              {loading ? (
                <div className="col-span-full bg-white rounded-[3rem] border border-slate-100 p-12 text-center text-sm font-bold text-slate-500">Đang tải khách sạn...</div>
              ) : error ? (
                <div className="col-span-full bg-rose-50 rounded-[3rem] border border-rose-100 p-12 text-center text-sm font-bold text-rose-600">{error}</div>
              ) : filteredItems.length === 0 ? (
                <div className="col-span-full bg-white rounded-[3rem] border border-slate-100 p-12 text-center text-sm font-bold text-slate-500">Không tìm thấy khách sạn phù hợp.</div>
              ) : filteredItems.map((hotel, index) => (
                <motion.div key={hotel.id} initial={{ opacity: 0, scale: 0.95 }} animate={{ opacity: 1, scale: 1 }} transition={{ delay: index * 0.05 }} className="group bg-white rounded-[3.5rem] overflow-hidden shadow-sm border border-slate-100 hover:shadow-[0_40px_80px_-20px_rgba(0,0,0,0.1)] transition-all duration-700 flex flex-col relative">
                  <div className="relative h-72 overflow-hidden">
                    <img src={hotel.coverImageUrl || 'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&q=80&w=800'} alt={hotel.name} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-1000" />
                    <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent opacity-60" />
                    <div className="absolute bottom-6 left-8">
                      <div className="flex items-center gap-2 text-white/90 text-[10px] font-black uppercase tracking-widest">
                        <MapPin size={14} />
                        {[hotel.city, hotel.province].filter(Boolean).join(', ') || 'Việt Nam'}
                      </div>
                    </div>
                  </div>

                  <div className="p-10 flex-1 flex flex-col">
                    <div className="flex items-center gap-2 text-amber-400 mb-4">
                      {Array.from({ length: Math.max(Number(hotel.starRating || 0), 1) }).map((_, starIndex) => (
                        <Star key={`${hotel.id}-star-${starIndex}`} size={14} fill="currentColor" />
                      ))}
                    </div>
                    <h3 className="text-2xl font-black text-slate-900 mb-4 leading-none group-hover:text-[#1EB4D4] transition-colors tracking-tighter">{hotel.name}</h3>
                    <p className="text-sm font-bold text-slate-500 leading-relaxed mb-8">{hotel.shortDescription || hotel.addressLine || 'Khách sạn từ hệ thống đối tác, sẵn sàng kiểm tra phòng trống theo ngày lưu trú.'}</p>

                    <div className="mt-auto pt-8 border-t border-slate-50 flex items-center justify-between">
                      <div>
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Giá tham khảo từ</p>
                        <div className="flex items-baseline gap-1">
                          <p className="text-3xl font-black text-[#1EB4D4] tracking-tighter">{formatCurrency(estimateNightlyPrice(hotel))}</p>
                          <span className="text-[10px] font-bold text-slate-400">/ đêm</span>
                        </div>
                      </div>
                      <Link to={`/hotel/${hotel.id}`} className="w-14 h-14 bg-slate-900 text-white rounded-[1.5rem] flex items-center justify-center hover:bg-[#1EB4D4] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30">
                        <ChevronRight size={24} />
                      </Link>
                    </div>
                  </div>
                </motion.div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
