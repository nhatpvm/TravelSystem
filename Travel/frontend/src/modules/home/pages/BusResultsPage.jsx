import React, { useEffect, useState } from 'react';
import { Filter, SortAsc, Bus, Clock, MapPin, ChevronRight, Star, Info, ShieldCheck, Search } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { searchBusLocations, searchBusTrips } from '../../../services/busService';
import { trackRecentSearch } from '../../../services/customerCommerceService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { buildTodayDateValue, formatCurrency, formatDate, formatTime } from '../../tenant/bus/utils/presentation';

const SORT_TABS = [
  { key: 'default', label: 'Tất cả' },
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
  return locations.find((item) => item.id === id)?.name || 'Chưa chọn điểm';
}

function sortTrips(items, activeTab) {
  const list = [...items];

  if (activeTab === 'cheapest') {
    return list.sort((left, right) => (left.price ?? Number.MAX_SAFE_INTEGER) - (right.price ?? Number.MAX_SAFE_INTEGER));
  }

  return list.sort((left, right) => new Date(left.departureAt).getTime() - new Date(right.departureAt).getTime());
}

const BusResultsPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthSession();
  const [searchParams] = useSearchParams();
  const [form, setForm] = useState(() => buildInitialForm(searchParams));
  const [activeTab, setActiveTab] = useState('default');
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
    searchBusLocations({ limit: 100 })
      .then((response) => {
        if (active) {
          setLocations(Array.isArray(response?.items) ? response.items : []);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được điểm đi/đến.');
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

    searchBusTrips({
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
          setError(err.message || 'Không tải được danh sách chuyến xe.');
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

    navigate(`/bus/results?${params.toString()}`);
  };

  const selectedFromName = getLocationName(locations, form.fromLocationId);
  const selectedToName = getLocationName(locations, form.toLocationId);
  const sortedTrips = sortTrips(trips, activeTab);

  useEffect(() => {
    if (!isAuthenticated || !form.fromLocationId || !form.toLocationId || !form.departDate) {
      return;
    }

    trackRecentSearch({
      productType: 'bus',
      searchKey: `bus:${form.fromLocationId}:${form.toLocationId}:${form.departDate}:${form.passengers || '1'}`,
      queryText: `${selectedFromName} - ${selectedToName}`,
      summaryText: `${selectedFromName} -> ${selectedToName}`,
      searchUrl: `${location.pathname}${location.search}`,
      criteriaJson: JSON.stringify({
        fromLocationId: form.fromLocationId,
        toLocationId: form.toLocationId,
        departDate: form.departDate,
        passengers: form.passengers || '1',
      }),
    }).catch(() => {});
  }, [form.departDate, form.fromLocationId, form.passengers, form.toLocationId, isAuthenticated, location.pathname, location.search, selectedFromName, selectedToName]);

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-12 overflow-hidden">
        <div className="bg-[#1EB4D4] h-[250px] relative flex items-center">
          <div className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?auto=format&fit=crop&q=80&w=2000')] bg-cover bg-center brightness-50" />
          <div className="container mx-auto px-4 relative z-10">
            <h1 className="text-4xl md:text-5xl font-black text-white tracking-tighter">
              {selectedFromName} <ChevronRight className="inline mx-2" /> {selectedToName}
            </h1>
            <p className="text-white/80 font-bold mt-2 uppercase tracking-widest text-xs">
              {formatDate(form.departDate)} • {sortedTrips.length} chuyến xe đang mở bán
            </p>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-12 relative z-20">
          <form
            onSubmit={handleSubmit}
            className="mb-8 bg-white rounded-[2.5rem] shadow-xl shadow-slate-200/40 border border-slate-100 p-6 md:p-8"
          >
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <label className="bg-slate-50 rounded-[1.5rem] px-5 py-4 border border-slate-100">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Điểm đi</p>
                <select
                  value={form.fromLocationId}
                  onChange={(event) => setForm((current) => ({ ...current, fromLocationId: event.target.value }))}
                  className="w-full bg-transparent outline-none text-sm font-bold text-slate-700"
                  required
                >
                  <option value="">{loadingLocations ? 'Đang tải địa điểm...' : 'Chọn điểm đi'}</option>
                  {locations.map((item) => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </label>

              <label className="bg-slate-50 rounded-[1.5rem] px-5 py-4 border border-slate-100">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Điểm đến</p>
                <select
                  value={form.toLocationId}
                  onChange={(event) => setForm((current) => ({ ...current, toLocationId: event.target.value }))}
                  className="w-full bg-transparent outline-none text-sm font-bold text-slate-700"
                  required
                >
                  <option value="">{loadingLocations ? 'Đang tải địa điểm...' : 'Chọn điểm đến'}</option>
                  {locations.map((item) => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </label>

              <label className="bg-slate-50 rounded-[1.5rem] px-5 py-4 border border-slate-100">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Ngày đi</p>
                <input
                  type="date"
                  value={form.departDate}
                  onChange={(event) => setForm((current) => ({ ...current, departDate: event.target.value }))}
                  className="w-full bg-transparent outline-none text-sm font-bold text-slate-700"
                  required
                />
              </label>

              <div className="flex items-end gap-3">
                <label className="flex-1 bg-slate-50 rounded-[1.5rem] px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Số khách</p>
                  <input
                    type="number"
                    min="1"
                    max="9"
                    value={form.passengers}
                    onChange={(event) => setForm((current) => ({ ...current, passengers: event.target.value }))}
                    className="w-full bg-transparent outline-none text-sm font-bold text-slate-700"
                    required
                  />
                </label>
                <button
                  type="submit"
                  className="h-[58px] px-6 bg-slate-900 text-white rounded-[1.5rem] font-black text-xs uppercase tracking-widest hover:bg-[#1EB4D4] transition-all flex items-center gap-2"
                >
                  <Search size={16} />
                  Tìm chuyến
                </button>
              </div>
            </div>
          </form>

          <div className="flex flex-col lg:flex-row gap-8">
            <aside className="w-full lg:w-1/4">
              <div className="bg-white rounded-[2.5rem] shadow-xl shadow-slate-200/50 border border-slate-100 p-8 sticky top-28 overflow-hidden">
                <div className="absolute top-0 left-0 w-full h-1 bg-[#1EB4D4]" />
                <div className="flex items-center justify-between mb-8">
                  <h3 className="font-black text-slate-900 text-lg flex items-center gap-2 uppercase tracking-tight">
                    <Filter size={18} className="text-[#1EB4D4]" />
                    Tổng quan
                  </h3>
                </div>

                <div className="space-y-6">
                  <div className="rounded-[1.5rem] bg-slate-50 border border-slate-100 p-5">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Tuyến đang xem</p>
                    <p className="text-lg font-black text-slate-900">{selectedFromName}</p>
                    <p className="text-sm font-bold text-slate-500 flex items-center gap-2 mt-1">
                      <ChevronRight size={14} className="text-[#1EB4D4]" />
                      {selectedToName}
                    </p>
                  </div>

                  <div className="rounded-[1.5rem] bg-slate-50 border border-slate-100 p-5">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Ngày khởi hành</p>
                    <p className="text-lg font-black text-slate-900">{formatDate(form.departDate)}</p>
                    <p className="text-sm font-bold text-slate-500 mt-1">{form.passengers} khách</p>
                  </div>

                  <div className="rounded-[1.5rem] bg-slate-900 text-white p-5">
                    <p className="text-[10px] font-black text-white/50 uppercase tracking-widest mb-2">Sàn xe khách</p>
                    <p className="text-lg font-black">So sánh nhiều nhà xe trong cùng một hành trình.</p>
                  </div>
                </div>
              </div>
            </aside>

            <div className="flex-1">
              <div className="flex flex-wrap items-center justify-between gap-4 mb-8 bg-white p-2 rounded-[2rem] shadow-sm border border-slate-100">
                <div className="flex gap-1">
                  {SORT_TABS.map((tab) => (
                    <button
                      key={tab.key}
                      type="button"
                      onClick={() => setActiveTab(tab.key)}
                      className={`px-6 py-3 rounded-2xl text-xs font-black uppercase tracking-widest transition-all ${
                        activeTab === tab.key ? 'bg-slate-900 text-white shadow-lg' : 'text-slate-400 hover:text-slate-600'
                      }`}
                    >
                      {tab.label}
                    </button>
                  ))}
                </div>
                <div className="flex items-center gap-2 pr-4">
                  <SortAsc size={16} className="text-slate-400" />
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Sắp xếp</span>
                </div>
              </div>

              {error && (
                <div className="mb-6 rounded-[2rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
                  {error}
                </div>
              )}

              {loadingTrips ? (
                <div className="bg-white rounded-[2.5rem] border border-slate-100 p-10 text-center text-sm font-bold text-slate-500">
                  Đang tải danh sách chuyến xe...
                </div>
              ) : sortedTrips.length === 0 ? (
                <div className="bg-white rounded-[2.5rem] border border-slate-100 p-12 text-center">
                  <div className="w-20 h-20 rounded-[2rem] bg-slate-100 flex items-center justify-center text-slate-300 mb-6 mx-auto">
                    <Bus size={32} />
                  </div>
                  <p className="text-sm font-black text-slate-900">Chưa có chuyến xe phù hợp.</p>
                  <p className="text-xs font-bold text-slate-400 uppercase tracking-widest mt-2">
                    Hãy chọn lại điểm đi, điểm đến hoặc ngày khởi hành.
                  </p>
                </div>
              ) : (
                <div className="space-y-6">
                  {sortedTrips.map((trip, idx) => (
                    <motion.div
                      initial={{ opacity: 0, x: 20 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ delay: idx * 0.06 }}
                      key={trip.tripId}
                      className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 hover:shadow-2xl hover:shadow-blue-500/10 transition-all group overflow-hidden relative"
                    >
                      <div className="p-8 flex flex-col xl:flex-row justify-between gap-10">
                        <div className="flex-1">
                          <div className="flex items-center gap-4 mb-6">
                            <div className="w-14 h-14 bg-slate-50 rounded-2xl flex items-center justify-center border-2 border-slate-100 group-hover:bg-blue-50 group-hover:border-[#1EB4D4] transition-all">
                              <Bus size={28} className="text-slate-400 group-hover:text-[#1EB4D4] transition-all" />
                            </div>
                            <div>
                              <div className="flex items-center gap-3 flex-wrap">
                                <h4 className="font-black text-xl text-slate-900 tracking-tight">
                                  {trip.provider?.name || trip.tenant?.name || 'Nhà xe đối tác'}
                                </h4>
                                {trip.provider?.ratingAverage ? (
                                  <div className="flex items-center gap-1 text-amber-500 text-xs font-black bg-amber-50 px-2.5 py-1 rounded-xl">
                                    <Star size={14} fill="currentColor" />
                                    {Number(trip.provider.ratingAverage).toFixed(1)}
                                  </div>
                                ) : null}
                              </div>
                              <div className="flex items-center gap-2 mt-1 flex-wrap">
                                <span className="text-[10px] text-slate-400 font-bold uppercase tracking-widest">
                                  {trip.vehicle?.name || 'Xe đang phục vụ'}
                                </span>
                                <span className="text-slate-200">|</span>
                                <span className="text-[10px] text-blue-500 font-black uppercase tracking-widest">
                                  {trip.vehicle?.plateNumber || trip.code}
                                </span>
                              </div>
                            </div>
                          </div>

                          <div className="flex items-center justify-between lg:max-w-xl relative">
                            <div className="z-10">
                              <p className="text-3xl font-black text-slate-900 tracking-tighter">{formatTime(trip.departureAt)}</p>
                              <p className="text-[11px] text-slate-500 font-black mt-1 uppercase tracking-widest flex items-center gap-1">
                                <MapPin size={10} className="text-blue-500" />
                                {selectedFromName}
                              </p>
                            </div>
                            <div className="flex-1 flex flex-col items-center px-10">
                              <p className="text-[10px] font-black text-slate-300 uppercase tracking-widest mb-2 flex items-center gap-1">
                                <Clock size={10} />
                                Hành trình đang mở bán
                              </p>
                              <div className="w-full h-px bg-slate-100 relative group-hover:bg-[#1EB4D4]/30 transition-all duration-500">
                                <div className="absolute top-1/2 left-0 -translate-y-1/2 w-2.5 h-2.5 rounded-full border-2 border-slate-200 bg-white group-hover:border-[#1EB4D4] transition-all" />
                                <div className="absolute top-1/2 right-0 -translate-y-1/2 w-2.5 h-2.5 rounded-full bg-slate-100 group-hover:bg-blue-600 transition-all" />
                                <Bus size={16} className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 text-slate-300 group-hover:text-[#1EB4D4] bg-white px-2 box-content transition-transform" />
                              </div>
                            </div>
                            <div className="z-10 text-right">
                              <p className="text-3xl font-black text-slate-900 tracking-tighter">{formatTime(trip.arrivalAt)}</p>
                              <p className="text-[11px] text-slate-500 font-black mt-1 uppercase tracking-widest flex items-center justify-end gap-1">
                                {selectedToName}
                                <MapPin size={10} className="text-blue-500" />
                              </p>
                            </div>
                          </div>

                          <div className="mt-8 pt-6 border-t border-slate-50 flex items-center gap-6 flex-wrap">
                            <div className="flex items-center gap-2 text-[10px] font-black text-slate-400 uppercase tracking-widest">
                              <ShieldCheck size={14} className="text-emerald-500" />
                              Nhà xe: {trip.tenant?.name || 'Đối tác đang mở bán'}
                            </div>
                            <div className="flex items-center gap-2 text-[10px] font-black text-slate-400 uppercase tracking-widest">
                              <Info size={14} className="text-blue-400" />
                              Còn {trip.availableSeatCount} ghế trên chặng đã chọn
                            </div>
                          </div>
                        </div>

                        <div className="xl:w-64 flex flex-col items-center justify-center xl:border-l border-slate-50 xl:pl-12 w-full">
                          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1 italic">Giá từ</p>
                          <p className="text-3xl font-black text-[#1EB4D4] tracking-tighter">
                            {trip.price ? formatCurrency(trip.price, trip.currency) : 'Liên hệ'}
                          </p>
                          <Link
                            to={`/bus/trip/${trip.tripId}?fromTripStopTimeId=${trip.segment?.fromTripStopTimeId || ''}&toTripStopTimeId=${trip.segment?.toTripStopTimeId || ''}`}
                            className="mt-5 w-full bg-slate-900 text-white py-4 rounded-3xl font-black uppercase text-xs tracking-widest hover:bg-[#19a7c5] transition-all shadow-xl shadow-slate-900/10 hover:shadow-[#1EB4D4]/30 text-center"
                          >
                            Xem chi tiết
                          </Link>
                          <p className={`mt-4 text-[10px] font-black uppercase tracking-widest ${trip.canBook ? 'text-emerald-500' : 'text-rose-500'}`}>
                            {trip.canBook ? `Đủ ${form.passengers} chỗ để đặt` : 'Không đủ số ghế yêu cầu'}
                          </p>
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
};

export default BusResultsPage;
