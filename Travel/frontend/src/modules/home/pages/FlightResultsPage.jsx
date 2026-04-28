import React, { useEffect, useMemo, useState } from 'react';
import { Plane, Calendar, Users, Filter, MapPin, Search, ChevronRight, ShieldCheck, Zap } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { searchFlightAirports, searchFlights } from '../../../services/flightService';
import { trackRecentSearch } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, formatDate, formatTime, getCabinClassLabel } from '../../tenant/flight/utils/presentation';

const SORT_TABS = [
  { key: 'all', label: 'Tất cả chuyến bay' },
  { key: 'cheapest', label: 'Giá tốt nhất' },
  { key: 'earliest', label: 'Cất cánh sớm' },
];

function buildTodayValue() {
  return new Date().toISOString().slice(0, 10);
}

function getAirportOptionValue(airport) {
  return airport?.iataCode || airport?.code || '';
}

function buildInitialForm(searchParams) {
  return {
    from: searchParams.get('from') || '',
    to: searchParams.get('to') || '',
    date: searchParams.get('date') || buildTodayValue(),
    passengers: searchParams.get('passengers') || '1',
  };
}

function getAirportLabel(airport) {
  if (!airport) {
    return 'Chưa chọn sân bay';
  }

  const code = airport.iataCode || airport.code;
  return code ? `${airport.name} (${code})` : airport.name;
}

function findAirportByCode(airports, code) {
  return airports.find((item) => (item.iataCode || item.code || '').toUpperCase() === String(code || '').toUpperCase()) || null;
}

function sortFlights(items, activeTab) {
  const nextItems = [...items];

  if (activeTab === 'cheapest') {
    return nextItems.sort((left, right) => Number(left.totalPrice || 0) - Number(right.totalPrice || 0));
  }

  return nextItems.sort(
    (left, right) => new Date(left.flight?.departureAt || 0).getTime() - new Date(right.flight?.departureAt || 0).getTime(),
  );
}

function getFlightTag(item) {
  const seats = Number(item?.seatsAvailable || 0);

  if (seats <= 3) {
    return 'Sắp hết chỗ';
  }

  if (Number(item?.taxesFees || 0) === 0) {
    return 'Giá tốt';
  }

  return 'Đang mở bán';
}

function getFlightDuration(item) {
  const segments = Array.isArray(item?.segments) ? item.segments : [];
  const first = segments[0];
  const last = segments[segments.length - 1];

  if (!first?.departureAt || !last?.arrivalAt) {
    return '--';
  }

  const ms = new Date(last.arrivalAt).getTime() - new Date(first.departureAt).getTime();
  if (Number.isNaN(ms) || ms <= 0) {
    return '--';
  }

  const totalMinutes = Math.round(ms / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return minutes ? `${hours}h ${minutes}m` : `${hours}h`;
}

const FlightResultsPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthSession();
  const [searchParams] = useSearchParams();
  const [form, setForm] = useState(() => buildInitialForm(searchParams));
  const [activeTab, setActiveTab] = useState('all');
  const [airports, setAirports] = useState([]);
  const [flights, setFlights] = useState([]);
  const [loadingAirports, setLoadingAirports] = useState(true);
  const [loadingFlights, setLoadingFlights] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setForm(buildInitialForm(searchParams));
  }, [searchParams]);

  useEffect(() => {
    let active = true;
    setLoadingAirports(true);

    searchFlightAirports({ limit: 100 })
      .then((response) => {
        if (!active) {
          return;
        }

        const items = Array.isArray(response?.items) ? response.items : [];
        setAirports(items);

        const initialForm = buildInitialForm(searchParams);
        if (!initialForm.from && !initialForm.to && items.length >= 2) {
          const params = new URLSearchParams();
          params.set('from', getAirportOptionValue(items[0]));
          params.set('to', getAirportOptionValue(items[1]));
          params.set('date', initialForm.date || buildTodayValue());
          params.set('passengers', initialForm.passengers || '1');
          navigate(`/flight/results?${params.toString()}`, { replace: true });
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải danh sách sân bay.');
        }
      })
      .finally(() => {
        if (active) {
          setLoadingAirports(false);
        }
      });

    return () => {
      active = false;
    };
  }, [navigate, searchParams]);

  useEffect(() => {
    const nextForm = buildInitialForm(searchParams);

    if (!nextForm.from || !nextForm.to || !nextForm.date) {
      setFlights([]);
      return undefined;
    }

    let active = true;
    setLoadingFlights(true);
    setError('');

    searchFlights({
      from: nextForm.from,
      to: nextForm.to,
      date: nextForm.date,
    })
      .then((response) => {
        if (active) {
          setFlights(Array.isArray(response?.items) ? response.items : []);
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải danh sách chuyến bay.');
          setFlights([]);
        }
      })
      .finally(() => {
        if (active) {
          setLoadingFlights(false);
        }
      });

    return () => {
      active = false;
    };
  }, [searchParams]);

  const handleSubmit = (event) => {
    event.preventDefault();

    const params = new URLSearchParams();
    params.set('from', form.from);
    params.set('to', form.to);
    params.set('date', form.date);
    params.set('passengers', form.passengers || '1');
    navigate(`/flight/results?${params.toString()}`);
  };

  const sortedFlights = useMemo(() => sortFlights(flights, activeTab), [flights, activeTab]);
  const fromAirport = findAirportByCode(airports, form.from);
  const toAirport = findAirportByCode(airports, form.to);

  useEffect(() => {
    if (!isAuthenticated || !form.from || !form.to || !form.date) {
      return;
    }

    trackRecentSearch({
      productType: 'flight',
      searchKey: `flight:${form.from}:${form.to}:${form.date}:${form.passengers || '1'}`,
      queryText: `${fromAirport?.name || form.from} - ${toAirport?.name || form.to}`,
      summaryText: `${fromAirport?.iataCode || form.from} -> ${toAirport?.iataCode || form.to}`,
      searchUrl: `${location.pathname}${location.search}`,
      criteriaJson: JSON.stringify({
        from: form.from,
        to: form.to,
        date: form.date,
        passengers: form.passengers || '1',
      }),
    }).catch(() => {});
  }, [form.date, form.from, form.passengers, form.to, fromAirport?.iataCode, fromAirport?.name, isAuthenticated, location.pathname, location.search, toAirport?.iataCode, toAirport?.name]);

  return (
    <MainLayout>
      <div className="min-h-screen bg-[#F8FAFC] pb-24 overflow-hidden">
        <div className="relative h-[420px] flex items-center justify-center overflow-hidden">
          <motion.div
            initial={{ scale: 1.1 }}
            animate={{ scale: 1 }}
            transition={{ duration: 10, repeat: Infinity, repeatType: 'reverse' }}
            className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1436491865332-7a61a109cc05?auto=format&fit=crop&q=80&w=2000')] bg-cover bg-center brightness-[0.3]"
          />
          <div className="absolute inset-0 bg-gradient-to-b from-transparent via-slate-900/50 to-[#F8FAFC]" />

          <div className="w-[90%] mx-auto px-4 relative z-10 text-center">
            <motion.div
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              className="inline-flex items-center gap-2 px-6 py-2 bg-white/10 backdrop-blur-xl rounded-full border border-white/20 text-white text-[10px] font-black uppercase tracking-[0.3em] mb-8"
            >
              <Plane size={14} className="text-[#1EB4D4]" />
              <span>Tìm chuyến bay toàn hệ thống marketplace</span>
            </motion.div>

            <h1 className="text-5xl md:text-7xl font-black text-white tracking-tighter leading-none mb-6">
              {fromAirport?.iataCode || form.from || '---'} <span className="text-[#1EB4D4] italic mx-4">→</span> {toAirport?.iataCode || form.to || '---'}
            </h1>

            <div className="flex flex-wrap items-center justify-center gap-8 text-white/60 font-black tracking-widest text-[11px] uppercase">
              <div className="flex items-center gap-2 px-4 py-2 bg-white/5 rounded-2xl">
                <Calendar size={14} className="text-[#1EB4D4]" /> {formatDate(form.date)}
              </div>
              <div className="flex items-center gap-2 px-4 py-2 bg-white/5 rounded-2xl">
                <Users size={14} className="text-[#1EB4D4]" /> {form.passengers} hành khách
              </div>
              <div className="flex items-center gap-2 px-4 py-2 bg-white/5 rounded-2xl">
                <MapPin size={14} className="text-[#1EB4D4]" /> {sortedFlights.length} offer phù hợp
              </div>
            </div>
          </div>
        </div>

        <div className="w-[90%] mx-auto -mt-20 relative z-20">
          <div className="bg-white/80 backdrop-blur-3xl p-6 rounded-[3rem] shadow-2xl shadow-slate-200/50 border border-white mb-12">
            <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <select value={form.from} onChange={(event) => setForm((current) => ({ ...current, from: event.target.value }))} className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none">
                <option value="">{loadingAirports ? 'Đang tải sân bay đi...' : 'Chọn sân bay đi'}</option>
                {airports.map((item) => <option key={`${item.id}-from`} value={getAirportOptionValue(item)}>{getAirportLabel(item)}</option>)}
              </select>

              <select value={form.to} onChange={(event) => setForm((current) => ({ ...current, to: event.target.value }))} className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none">
                <option value="">{loadingAirports ? 'Đang tải sân bay đến...' : 'Chọn sân bay đến'}</option>
                {airports.map((item) => <option key={`${item.id}-to`} value={getAirportOptionValue(item)}>{getAirportLabel(item)}</option>)}
              </select>

              <input type="date" value={form.date} onChange={(event) => setForm((current) => ({ ...current, date: event.target.value }))} className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none" />

              <button type="submit" className="w-full bg-slate-900 text-white rounded-2xl py-4 text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all flex items-center justify-center gap-2">
                <Search size={16} />
                Tìm chuyến bay
              </button>
            </form>
          </div>

          <div className="flex flex-col lg:flex-row gap-12">
            <aside className="lg:w-[300px]">
              <div className="bg-white rounded-[3rem] p-10 border border-slate-100 shadow-sm sticky top-32 overflow-hidden">
                <h3 className="font-black text-slate-900 text-2xl tracking-tight mb-8 flex items-center gap-4">
                  <div className="w-10 h-10 bg-slate-900 rounded-2xl flex items-center justify-center text-white">
                    <Filter size={18} />
                  </div>
                  Bộ lọc nhanh
                </h3>

                <div className="space-y-10">
                  <div className="flex items-center gap-2 bg-slate-50 rounded-[2rem] p-2">
                    {SORT_TABS.map((tab) => (
                      <button key={tab.key} type="button" onClick={() => setActiveTab(tab.key)} className={`flex-1 px-4 py-3 rounded-[1.6rem] text-[10px] font-black uppercase tracking-[0.2em] transition-all ${activeTab === tab.key ? 'bg-slate-900 text-white shadow-lg' : 'text-slate-400 hover:text-slate-700'}`}>
                        {tab.label}
                      </button>
                    ))}
                  </div>

                  <div className="space-y-4">
                    <p className="text-[11px] font-black text-slate-900 uppercase tracking-[0.2em]">Tuyến bay</p>
                    <div className="bg-slate-50 rounded-[2rem] p-5 space-y-3">
                      <p className="text-sm font-black text-slate-900">{fromAirport?.name || 'Chưa chọn điểm đi'}</p>
                      <p className="text-xs font-bold text-slate-400">{toAirport?.name || 'Chưa chọn điểm đến'}</p>
                      <p className="text-xs font-bold text-slate-400">{formatDate(form.date)}</p>
                    </div>
                  </div>

                  <div className="space-y-4">
                    <p className="text-[11px] font-black text-slate-900 uppercase tracking-[0.2em]">Thông tin giá</p>
                    <div className="bg-slate-900 rounded-[2rem] p-5 text-white">
                      <p className="text-[10px] font-black uppercase tracking-widest text-white/50 mb-2">Giá từ</p>
                      <p className="text-2xl font-black text-[#1EB4D4]">
                        {sortedFlights.length > 0 ? formatCurrency(sortedFlights[0].totalPrice, sortedFlights[0].currencyCode) : '--'}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </aside>

            <div className="flex-1 space-y-8">
              {error ? <div className="bg-white rounded-[3rem] p-8 border border-rose-100 text-sm font-bold text-rose-600">{error}</div> : null}

              {loadingFlights ? (
                <div className="bg-white rounded-[3rem] p-10 border border-slate-100 text-sm font-bold text-slate-500">Đang tải danh sách chuyến bay...</div>
              ) : sortedFlights.length === 0 ? (
                <div className="bg-white rounded-[3rem] p-12 border border-slate-100 text-center">
                  <div className="w-24 h-24 bg-slate-100 rounded-[3rem] flex items-center justify-center mx-auto text-slate-300 mb-8 border-4 border-white shadow-xl">
                    <Search size={32} />
                  </div>
                  <p className="text-lg font-black text-slate-900">Chưa có offer nào phù hợp.</p>
                  <p className="text-xs font-black text-slate-400 uppercase tracking-[0.3em] mt-3">Hãy chọn lại sân bay đi, sân bay đến hoặc ngày bay.</p>
                </div>
              ) : sortedFlights.map((item, index) => {
                const firstSegment = item.segments?.[0];
                const lastSegment = item.segments?.[item.segments.length - 1];

                return (
                  <motion.div key={item.offerId} initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: index * 0.06 }} className="relative bg-white rounded-[3rem] border border-slate-100 p-8 lg:p-12 shadow-sm hover:shadow-[0_40px_100px_-20px_rgba(0,0,0,0.08)] transition-all duration-700 flex flex-col lg:flex-row items-center gap-8 lg:gap-12">
                    <div className="absolute top-0 right-10 px-6 py-2 bg-slate-900 text-white rounded-b-2xl text-[9px] font-black uppercase tracking-widest">{getFlightTag(item)}</div>

                    <div className="w-48 text-center shrink-0">
                      <div className="w-24 h-24 mx-auto bg-slate-50 rounded-[2.5rem] p-4 flex items-center justify-center mb-6 shadow-sm border border-slate-100 overflow-hidden">
                        {item.airline?.logoUrl ? <img src={item.airline.logoUrl} alt={item.airline.name} className="max-w-full max-h-full object-contain" /> : <Plane size={32} className="text-[#1EB4D4]" />}
                      </div>
                      <h4 className="font-black text-slate-900 text-sm tracking-tighter uppercase">{item.airline?.name || 'Hãng bay'}</h4>
                      <p className="text-[9px] text-slate-300 font-black uppercase tracking-widest mt-2">{getCabinClassLabel(item.fareClass?.cabinClass)}</p>
                    </div>

                    <div className="flex-1 flex items-center justify-between relative w-full lg:max-w-xl">
                      <div className="text-left">
                        <p className="text-4xl lg:text-5xl font-black text-slate-900 tracking-tighter mb-2">{formatTime(firstSegment?.departureAt)}</p>
                        <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full bg-[#1EB4D4]" />
                          <p className="text-xs font-black text-slate-400 uppercase tracking-widest">{firstSegment?.from?.iataCode || firstSegment?.from?.code || '---'}</p>
                        </div>
                      </div>

                      <div className="flex-1 flex flex-col items-center px-12 relative">
                        <div className="w-full h-px bg-slate-100 relative mb-6">
                          <div className="absolute top-0 left-0 h-px w-full bg-gradient-to-r from-transparent via-[#1EB4D4] to-transparent" />
                          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-white px-4">
                            <Plane size={18} className="text-[#1EB4D4]" />
                          </div>
                        </div>
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.3em]">{getFlightDuration(item)}</p>
                        <div className="mt-4 flex gap-4">
                          <ShieldCheck size={14} className="text-emerald-500" />
                          <Zap size={14} className="text-amber-500" />
                        </div>
                      </div>

                      <div className="text-right">
                        <p className="text-4xl lg:text-5xl font-black text-slate-900 tracking-tighter mb-2">{formatTime(lastSegment?.arrivalAt)}</p>
                        <p className="text-xs font-black text-slate-400 uppercase tracking-widest">{lastSegment?.to?.iataCode || lastSegment?.to?.code || '---'}</p>
                      </div>
                    </div>

                    <div className="w-full md:w-px h-1 bg-slate-50 md:h-24 md:w-px" />

                    <div className="w-full lg:w-52 text-center lg:text-right flex flex-col items-center lg:items-end justify-center">
                      <p className="text-[10px] font-black text-slate-300 uppercase tracking-widest mb-1">Giá trọn gói</p>
                      <p className="text-3xl font-black text-slate-900 tracking-tighter whitespace-nowrap">{formatCurrency(item.totalPrice, item.currencyCode)}</p>
                      <p className="text-xs font-bold text-slate-400 mt-2">{item.seatsAvailable || 0} chỗ còn lại</p>
                      <Link to={`/flight/detail?offerId=${item.offerId}`} className="mt-6 px-6 h-12 bg-slate-900 text-white rounded-2xl font-black text-[10px] uppercase tracking-[0.2em] hover:bg-[#1EB4D4] transition-all inline-flex items-center justify-center gap-2 shadow-xl shadow-slate-900/10">
                        Xem chi tiết
                        <ChevronRight size={16} />
                      </Link>
                    </div>
                  </motion.div>
                );
              })}
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
};

export default FlightResultsPage;
