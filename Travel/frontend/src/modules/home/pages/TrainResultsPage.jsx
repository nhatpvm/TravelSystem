import React, { useEffect, useMemo, useState } from 'react';
import { Train, Clock, MapPin, ChevronRight, Plus, ShieldCheck, Search } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { motion } from 'framer-motion';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { searchTrainLocations, searchTrainTrips } from '../../../services/trainService';
import {
  buildTodayDateValue,
  formatCurrency,
  formatDate,
  formatTime,
  getCarTypeLabel,
} from '../../tenant/train/utils/presentation';

const SORT_TABS = [
  { key: 'all', label: 'Tất cả tàu' },
  { key: 'cheapest', label: 'Rẻ nhất' },
  { key: 'earliest', label: 'Sớm nhất' },
];

function buildInitialForm(searchParams) {
  return {
    fromLocationId: searchParams.get('fromLocationId') || '',
    toLocationId: searchParams.get('toLocationId') || '',
    departDate: searchParams.get('departDate') || buildTodayDateValue(),
    passengers: searchParams.get('passengers') || '1',
  };
}

function getLocationName(locations, id) {
  return locations.find((item) => item.id === id)?.name || 'Chưa chọn ga';
}

function formatDuration(start, end) {
  if (!start || !end) {
    return '--';
  }

  const durationMs = new Date(end).getTime() - new Date(start).getTime();
  if (Number.isNaN(durationMs) || durationMs <= 0) {
    return '--';
  }

  const totalMinutes = Math.round(durationMs / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours <= 0) {
    return `${minutes}p`;
  }

  if (!minutes) {
    return `${hours}h`;
  }

  return `${hours}h ${minutes}p`;
}

function sortTrips(items, activeTab) {
  const nextItems = [...items];

  if (activeTab === 'cheapest') {
    return nextItems.sort((left, right) => (left.price ?? Number.MAX_SAFE_INTEGER) - (right.price ?? Number.MAX_SAFE_INTEGER));
  }

  return nextItems.sort((left, right) => new Date(left.departureAt).getTime() - new Date(right.departureAt).getTime());
}

function getOptionLabel(option) {
  const parts = [getCarTypeLabel(option?.carType), option?.cabinClass].filter(Boolean);
  return parts.join(' • ') || 'Hạng vé tiêu chuẩn';
}

const TrainResultsPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [form, setForm] = useState(() => buildInitialForm(searchParams));
  const [activeTab, setActiveTab] = useState('all');
  const [locations, setLocations] = useState([]);
  const [trips, setTrips] = useState([]);
  const [loadingLocations, setLoadingLocations] = useState(true);
  const [loadingTrips, setLoadingTrips] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setForm(buildInitialForm(searchParams));
  }, [searchParams]);

  useEffect(() => {
    let active = true;

    setLoadingLocations(true);
    searchTrainLocations({ limit: 100 })
      .then((response) => {
        if (!active) {
          return;
        }

        const nextLocations = Array.isArray(response?.items) ? response.items : [];
        setLocations(nextLocations);

        const nextForm = buildInitialForm(searchParams);
        if (!nextForm.fromLocationId && !nextForm.toLocationId && nextLocations.length >= 2) {
          const params = new URLSearchParams();
          params.set('fromLocationId', nextLocations[0].id);
          params.set('toLocationId', nextLocations[1].id);
          params.set('departDate', nextForm.departDate || buildTodayDateValue());
          params.set('passengers', nextForm.passengers || '1');
          navigate(`/train/results?${params.toString()}`, { replace: true });
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được danh sách ga tàu.');
        }
      })
      .finally(() => {
        if (active) {
          setLoadingLocations(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    const nextForm = buildInitialForm(searchParams);

    if (!nextForm.fromLocationId || !nextForm.toLocationId || !nextForm.departDate) {
      setTrips([]);
      return undefined;
    }

    let active = true;
    setLoadingTrips(true);
    setError('');

    searchTrainTrips({
      fromLocationId: nextForm.fromLocationId,
      toLocationId: nextForm.toLocationId,
      departDate: nextForm.departDate,
      passengers: nextForm.passengers || 1,
    })
      .then((response) => {
        if (active) {
          setTrips(Array.isArray(response?.items) ? response.items : []);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được danh sách chuyến tàu.');
          setTrips([]);
        }
      })
      .finally(() => {
        if (active) {
          setLoadingTrips(false);
        }
      });

    return () => {
      active = false;
    };
  }, [searchParams]);

  const handleSubmit = (event) => {
    event.preventDefault();
    const params = new URLSearchParams();
    params.set('fromLocationId', form.fromLocationId);
    params.set('toLocationId', form.toLocationId);
    params.set('departDate', form.departDate);
    params.set('passengers', form.passengers || '1');
    navigate(`/train/results?${params.toString()}`);
  };

  const selectedFromName = getLocationName(locations, form.fromLocationId);
  const selectedToName = getLocationName(locations, form.toLocationId);
  const sortedTrips = useMemo(() => sortTrips(trips, activeTab), [trips, activeTab]);

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-12 overflow-hidden">
        <div className="bg-slate-900 h-[320px] relative flex items-center justify-center overflow-hidden">
          <div className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1474487585635-96811fca4c1c?auto=format&fit=crop&q=80&w=2000')] bg-cover bg-center opacity-30" />
          <div className="absolute inset-0 bg-gradient-to-b from-transparent to-slate-900/90" />
          <div className="container mx-auto px-4 relative z-10 text-center">
            <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="flex justify-center mb-6">
              <div className="bg-[#1EB4D4] p-4 rounded-[2rem] shadow-2xl shadow-[#1EB4D4]/30">
                <Train size={32} className="text-white" />
              </div>
            </motion.div>
            <div className="flex items-center justify-center gap-6 mb-4 flex-wrap">
              <h1 className="text-4xl md:text-6xl font-black text-white tracking-tighter">{selectedFromName}</h1>
              <ChevronRight size={32} className="text-[#1EB4D4]" />
              <h1 className="text-4xl md:text-6xl font-black text-white tracking-tighter">{selectedToName}</h1>
            </div>
            <p className="text-white/40 font-black uppercase tracking-[0.4em] text-[10px]">
              {formatDate(form.departDate)} • {sortedTrips.length} chuyến tàu đang mở bán
            </p>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-10 relative z-20">
          <div className="max-w-4xl mx-auto flex items-center justify-center gap-2 bg-white p-2 rounded-[2.5rem] shadow-2xl border border-slate-100 mb-12 flex-wrap">
            {SORT_TABS.map((tab) => (
              <button
                key={tab.key}
                type="button"
                onClick={() => setActiveTab(tab.key)}
                className={`px-10 py-4 rounded-[1.8rem] text-xs font-black uppercase tracking-widest transition-all ${activeTab === tab.key ? 'bg-slate-900 text-white shadow-xl' : 'text-slate-400 hover:text-slate-600'}`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <div className="flex flex-col lg:flex-row gap-12">
            <aside className="w-full lg:w-1/4">
              <div className="bg-white rounded-[3rem] p-10 border border-slate-100 shadow-sm sticky top-28 overflow-hidden group">
                <div className="absolute top-0 left-0 w-1 h-full bg-[#1EB4D4] group-hover:w-2 transition-all" />
                <h3 className="font-black text-slate-900 text-2xl tracking-tight mb-8">Tìm chuyến tàu</h3>
                <form onSubmit={handleSubmit} className="space-y-4">
                  <label className="block">
                    <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Ga đi</span>
                    <select
                      value={form.fromLocationId}
                      onChange={(event) => setForm((current) => ({ ...current, fromLocationId: event.target.value }))}
                      className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none"
                    >
                      <option value="">{loadingLocations ? 'Đang tải ga tàu...' : 'Chọn ga đi'}</option>
                      {locations.map((item) => (
                        <option key={item.id} value={item.id}>{item.name}</option>
                      ))}
                    </select>
                  </label>

                  <label className="block">
                    <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Ga đến</span>
                    <select
                      value={form.toLocationId}
                      onChange={(event) => setForm((current) => ({ ...current, toLocationId: event.target.value }))}
                      className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none"
                    >
                      <option value="">{loadingLocations ? 'Đang tải ga tàu...' : 'Chọn ga đến'}</option>
                      {locations.map((item) => (
                        <option key={item.id} value={item.id}>{item.name}</option>
                      ))}
                    </select>
                  </label>

                  <label className="block">
                    <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Ngày đi</span>
                    <input
                      type="date"
                      value={form.departDate}
                      onChange={(event) => setForm((current) => ({ ...current, departDate: event.target.value }))}
                      className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none"
                    />
                  </label>

                  <label className="block">
                    <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Số khách</span>
                    <input
                      type="number"
                      min="1"
                      max="9"
                      value={form.passengers}
                      onChange={(event) => setForm((current) => ({ ...current, passengers: event.target.value }))}
                      className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-4 py-4 text-sm font-bold outline-none"
                    />
                  </label>

                  <button
                    type="submit"
                    className="w-full bg-slate-900 text-white rounded-[1.8rem] py-4 text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all flex items-center justify-center gap-2"
                  >
                    <Search size={16} />
                    Tìm chuyến
                  </button>
                </form>

                <div className="mt-10 pt-8 border-t border-slate-50">
                  <div className="rounded-[2rem] bg-slate-900 text-white p-5">
                    <p className="text-[10px] font-black text-white/50 uppercase tracking-widest mb-2">Marketplace tàu hỏa</p>
                    <p className="text-sm font-black leading-relaxed">So sánh nhiều đối tác đường sắt trên cùng một hành trình và giữ chỗ theo thời gian thực.</p>
                  </div>
                </div>
              </div>
            </aside>

            <div className="flex-1 space-y-10">
              {error ? (
                <div className="bg-white rounded-[3rem] p-10 border border-rose-100 text-sm font-bold text-rose-600">
                  {error}
                </div>
              ) : null}

              {loadingTrips ? (
                <div className="bg-white rounded-[3rem] p-10 border border-slate-100 text-sm font-bold text-slate-500">
                  Đang tải danh sách chuyến tàu...
                </div>
              ) : sortedTrips.length === 0 ? (
                <div className="bg-white rounded-[3rem] p-12 border border-slate-100 text-center">
                  <div className="w-20 h-20 bg-slate-50 rounded-[2rem] flex items-center justify-center mx-auto mb-6">
                    <Train size={32} className="text-slate-300" />
                  </div>
                  <p className="text-lg font-black text-slate-900">Chưa có chuyến tàu phù hợp.</p>
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mt-3">
                    Hãy chọn lại ga đi, ga đến hoặc ngày khởi hành.
                  </p>
                </div>
              ) : sortedTrips.map((trip, tIdx) => (
                <motion.div
                  initial={{ opacity: 0, y: 30 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: tIdx * 0.08 }}
                  key={trip.tripId}
                  className="group bg-white rounded-[4rem] border border-slate-100 shadow-sm hover:shadow-2xl hover:shadow-blue-500/10 transition-all duration-700 overflow-hidden"
                >
                  <div className="p-8 lg:p-12 border-b border-dashed border-slate-100 relative">
                    <div className="flex flex-wrap gap-2 mb-6">
                      <span className="px-3 py-1 lg:px-4 lg:py-1.5 bg-slate-50 rounded-xl text-[8px] lg:text-[9px] font-black text-slate-400 uppercase tracking-widest border border-slate-100">
                        {trip.provider?.name || trip.tenant?.name || 'Đối tác đường sắt'}
                      </span>
                      <span className="px-3 py-1 lg:px-4 lg:py-1.5 bg-slate-50 rounded-xl text-[8px] lg:text-[9px] font-black text-slate-400 uppercase tracking-widest border border-slate-100">
                        Còn {trip.availableSeatCount} chỗ
                      </span>
                      <span className={`px-3 py-1 lg:px-4 lg:py-1.5 rounded-xl text-[8px] lg:text-[9px] font-black uppercase tracking-widest border ${trip.canBook ? 'bg-emerald-50 text-emerald-600 border-emerald-100' : 'bg-rose-50 text-rose-600 border-rose-100'}`}>
                        {trip.canBook ? 'Có thể đặt ngay' : 'Sắp hết chỗ'}
                      </span>
                    </div>

                    <div className="flex flex-col xl:flex-row items-center gap-16">
                      <div className="xl:w-64 text-left">
                        <div className="flex items-center gap-3 mb-2">
                          <h4 className="font-black text-slate-900 text-3xl tracking-tighter group-hover:text-blue-600 transition-colors">{trip.name}</h4>
                        </div>
                        <p className="text-[10px] text-slate-400 font-bold uppercase tracking-widest flex items-center gap-2">
                          <ShieldCheck size={14} className="text-emerald-500" /> {trip.trainNumber || trip.code}
                        </p>
                      </div>

                      <div className="flex-1 flex items-center justify-between relative w-full lg:max-w-2xl">
                        <div className="text-left">
                          <p className="text-4xl font-black text-slate-900 tracking-tighter">{formatTime(trip.departureAt)}</p>
                          <div className="flex items-center gap-2 mt-2">
                            <div className="w-2 h-2 rounded-full bg-blue-500 shadow-lg shadow-blue-500/40" />
                            <p className="text-xs font-black text-blue-600 uppercase tracking-widest">{selectedFromName}</p>
                          </div>
                        </div>

                        <div className="flex-1 flex flex-col items-center px-16 relative">
                          <div className="flex items-center gap-2 mb-3">
                            <Clock size={14} className="text-slate-300" />
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{formatDuration(trip.departureAt, trip.arrivalAt)}</p>
                          </div>
                          <div className="w-full h-1.5 bg-slate-50 rounded-full relative overflow-hidden group-hover:bg-blue-50 transition-all">
                            <div className="absolute top-0 left-0 h-full w-[60%] bg-[#1EB4D4] rounded-full group-hover:w-full transition-all duration-1000" />
                          </div>
                          <p className="text-[9px] font-black text-slate-300 mt-3 uppercase tracking-[0.3em]">Hành trình đang mở bán</p>
                        </div>

                        <div className="text-right">
                          <p className="text-4xl font-black text-slate-900 tracking-tighter">{formatTime(trip.arrivalAt)}</p>
                          <div className="flex items-center justify-end gap-2 mt-2">
                            <p className="text-xs font-black text-blue-600 uppercase tracking-widest">{selectedToName}</p>
                            <div className="w-2 h-2 rounded-full bg-rose-500 shadow-lg shadow-rose-500/40" />
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="p-6 lg:p-12 bg-slate-50/30">
                    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
                      {(trip.carOptions?.length ? trip.carOptions : [{
                        id: `${trip.tripId}-default`,
                        carType: 1,
                        cabinClass: 'Tiêu chuẩn',
                        availableSeatCount: trip.availableSeatCount,
                        price: trip.price,
                        currency: trip.currency,
                      }]).map((option) => (
                        <div key={option.id} className="bg-white p-6 lg:p-8 rounded-[3rem] border border-slate-100 flex flex-col shadow-sm hover:shadow-xl hover:shadow-blue-500/10 hover:border-[#1EB4D4] transition-all group/opt">
                          <div className="flex justify-between items-start mb-6">
                            <div>
                              <p className="text-xs font-black text-slate-900 uppercase tracking-tight leading-tight">{getOptionLabel(option)}</p>
                              <p className="text-[10px] text-rose-500 font-bold mt-2 uppercase tracking-widest flex items-center gap-1 leading-none">
                                <MapPin size={10} /> Chỉ còn {option.availableSeatCount} chỗ
                              </p>
                            </div>
                            <div className="bg-blue-50 p-2 lg:p-2.5 rounded-2xl group-hover/opt:bg-blue-600 transition-all">
                              <ShieldCheck size={18} className="text-blue-500 group-hover/opt:text-white" />
                            </div>
                          </div>
                          <div className="mt-auto flex items-end justify-between gap-4">
                            <div>
                              <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mb-1">Giá từ</p>
                              <p className="text-xl lg:text-2xl font-black text-slate-900 tracking-tighter whitespace-nowrap">
                                {option.price ? formatCurrency(option.price, option.currency) : 'Liên hệ'}
                              </p>
                            </div>
                            <Link
                              to={`/train/details?tripId=${trip.tripId}&fromTripStopTimeId=${trip.segment?.fromTripStopTimeId || ''}&toTripStopTimeId=${trip.segment?.toTripStopTimeId || ''}`}
                              className="bg-slate-900 text-white w-12 h-12 lg:w-14 lg:h-14 rounded-2xl flex items-center justify-center hover:bg-[#1EB4D4] hover:-translate-y-1 transition-all shadow-xl shadow-slate-900/10 shrink-0"
                            >
                              <Plus size={24} />
                            </Link>
                          </div>
                        </div>
                      ))}
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
};

export default TrainResultsPage;
