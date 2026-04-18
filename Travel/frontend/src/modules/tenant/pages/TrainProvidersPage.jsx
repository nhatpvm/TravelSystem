import React, { useEffect, useState } from 'react';
import { Armchair, RefreshCw, ShieldCheck, Ticket, Train } from 'lucide-react';
import { Link } from 'react-router-dom';
import TrainManagementPageShell from '../train/components/TrainManagementPageShell';
import { getSeatStatusClass, getSeatStatusLabel } from '../train/utils/presentation';
import {
  getTrainManagerOptions,
  listTrainCars,
  listTrainManagerSeatHolds,
  listTrainTrips,
  releaseTrainManagerSeatHold,
} from '../../../services/trainService';

const TrainProvidersPage = () => {
  const [options, setOptions] = useState({ cars: [] });
  const [cars, setCars] = useState([]);
  const [trips, setTrips] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState('');
  const [holds, setHolds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadBaseData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, tripResponse, carResponse] = await Promise.all([
        getTrainManagerOptions(),
        listTrainTrips(),
        listTrainCars(),
      ]);

      const nextTrips = Array.isArray(tripResponse?.items) ? tripResponse.items.filter((item) => !item.isDeleted) : [];
      setTrips(nextTrips);
      setSelectedTripId((current) => current || nextTrips[0]?.id || '');
      setOptions({
        cars: Array.isArray(optionsResponse?.cars) ? optionsResponse.cars : [],
      });
      setCars(Array.isArray(carResponse?.items) ? carResponse.items.filter((item) => !item.isDeleted) : []);
    } catch (err) {
      setError(err.message || 'Không tải được dữ liệu toa tàu và lượt giữ chỗ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadBaseData();
  }, []);

  useEffect(() => {
    if (!selectedTripId) {
      setHolds([]);
      return;
    }

    let active = true;

    listTrainManagerSeatHolds(selectedTripId)
      .then((response) => {
        if (active) {
          setHolds(Array.isArray(response?.items) ? response.items : []);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được lượt giữ chỗ của chuyến đã chọn.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  const handleReleaseHold = async (holdToken) => {
    setError('');
    setNotice('');

    try {
      await releaseTrainManagerSeatHold(holdToken);
      setNotice('Đã giải phóng giữ chỗ.');
      const response = await listTrainManagerSeatHolds(selectedTripId);
      setHolds(Array.isArray(response?.items) ? response.items : []);
    } catch (err) {
      setError(err.message || 'Không giải phóng được giữ chỗ.');
    }
  };

  return (
    <TrainManagementPageShell
      pageKey="providers"
      title="Toa tàu & Giữ chỗ"
      subtitle="Theo dõi toa tàu, số chỗ bán được và các seat holds theo từng chuyến."
      error={error}
      notice={notice}
      actions={(
        <button
          type="button"
          onClick={loadBaseData}
          className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
        >
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {[
          { label: 'Toa tàu đang khai thác', value: cars.length, icon: Train },
          { label: 'Toa có sơ đồ chỗ', value: options.cars.filter((item) => Number(item.seatCount || 0) > 0).length, icon: Armchair },
          { label: 'Toa trong chuyến đang chọn', value: cars.filter((item) => item.tripId === selectedTripId).length, icon: ShieldCheck },
          { label: 'Lượt giữ chỗ hiện tại', value: holds.length, icon: Ticket },
        ].map((item) => {
          const Icon = item.icon;

          return (
            <div key={item.label} className="bg-white rounded-[2.5rem] border border-slate-100 p-6 shadow-sm">
              <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center mb-5">
                <Icon size={22} />
              </div>
              <p className="text-3xl font-black text-slate-900">{loading ? '--' : item.value}</p>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-2">{item.label}</p>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Toa tàu theo chuyến</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Từ đây có thể nhảy nhanh sang cấu hình ghế và giường.</p>
            </div>
            <Link to="/tenant/providers/train/cars" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở danh sách
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.cars.slice(0, 6).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <p className="font-black text-slate-900">Toa {item.carNumber}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {item.trip?.name || 'Chưa gắn chuyến'} • {item.cabinClass || 'Chưa khai báo hạng'}
                    </p>
                    <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">
                      {item.seatCount || 0} chỗ trên sơ đồ
                    </p>
                  </div>
                  <Link to={`/tenant/providers/train/car-seats?carId=${item.id}`} className="px-3 py-2 rounded-xl bg-slate-100 text-[10px] font-black uppercase tracking-widest text-slate-600">
                    Cấu hình chỗ
                  </Link>
                </div>
              </div>
            ))}
            {!loading && options.cars.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có toa tàu nào.</div>
            ) : null}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Giữ chỗ theo chuyến</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Chọn một chuyến để theo dõi các lượt giữ chỗ đang còn hiệu lực.</p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
              <select
                value={selectedTripId}
                onChange={(event) => setSelectedTripId(event.target.value)}
                className="bg-transparent text-sm font-bold text-slate-700 outline-none"
              >
                {trips.map((trip) => (
                  <option key={trip.id} value={trip.id}>{trip.name}</option>
                ))}
              </select>
            </div>
          </div>
          <div className="divide-y divide-slate-50">
            {holds.slice(0, 6).map((hold) => (
              <div key={hold.id} className="px-8 py-5 flex items-center justify-between gap-4">
                <div>
                  <p className="font-black text-slate-900">{hold.trainCarSeatId}</p>
                  <p className="text-xs font-bold text-slate-400 mt-1">Hold token: {hold.holdToken}</p>
                </div>
                <div className="flex items-center gap-3">
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getSeatStatusClass(hold.status === 1 ? 'held' : 'booked')}`}>
                    {hold.status === 1 ? 'Đang giữ' : getSeatStatusLabel('booked')}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleReleaseHold(hold.holdToken)}
                    className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                  >
                    Giải phóng
                  </button>
                </div>
              </div>
            ))}
            {!loading && holds.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chuyến đang chọn chưa có lượt giữ chỗ nào.</div>
            ) : null}
          </div>
          <div className="px-8 py-5 border-t border-slate-100">
            <Link to={selectedTripId ? `/tenant/providers/train/seat-holds?tripId=${selectedTripId}` : '/tenant/providers/train/seat-holds'} className="text-xs font-black uppercase tracking-widest text-blue-600">
              Xem toàn bộ lượt giữ chỗ
            </Link>
          </div>
        </div>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainProvidersPage;
