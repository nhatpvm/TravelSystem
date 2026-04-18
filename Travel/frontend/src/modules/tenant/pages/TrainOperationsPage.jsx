import React, { useEffect, useState } from 'react';
import { Clock3, MapPinned, RefreshCw, Route, Ticket } from 'lucide-react';
import { Link } from 'react-router-dom';
import TrainManagementPageShell from '../train/components/TrainManagementPageShell';
import { formatDateTime, getStopPointTypeLabel, getTripStatusClass, getTripStatusLabel } from '../train/utils/presentation';
import { getTrainManagerOptions, listTrainTrips } from '../../../services/trainService';

const TrainOperationsPage = () => {
  const [options, setOptions] = useState({ stopPoints: [], routes: [] });
  const [trips, setTrips] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, tripsResponse] = await Promise.all([
        getTrainManagerOptions(),
        listTrainTrips(),
      ]);

      setOptions({
        stopPoints: Array.isArray(optionsResponse?.stopPoints) ? optionsResponse.stopPoints : [],
        routes: Array.isArray(optionsResponse?.routes) ? optionsResponse.routes : [],
      });
      setTrips(Array.isArray(tripsResponse?.items) ? tripsResponse.items : []);
    } catch (err) {
      setError(err.message || 'Không tải được dữ liệu vận hành tàu.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const activeTrips = trips.filter((item) => !item.isDeleted);

  return (
    <TrainManagementPageShell
      pageKey="operations"
      title="Vận hành Tàu"
      subtitle="Ga tàu, tuyến đường và lịch dừng đều được điều phối theo tenant hiện tại."
      error={error}
      actions={(
        <button
          type="button"
          onClick={loadData}
          className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
        >
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {[
          { label: 'Ga tàu', value: options.stopPoints.length, icon: MapPinned },
          { label: 'Tuyến đường', value: options.routes.length, icon: Route },
          { label: 'Chuyến đang chạy', value: activeTrips.length, icon: Ticket },
          { label: 'Chuyến có lịch dừng', value: trips.filter((item) => Number(item.status) === 2).length, icon: Clock3 },
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
              <p className="text-lg font-black text-slate-900">Ga tàu nổi bật</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Danh sách lấy trực tiếp từ stop points của tenant.</p>
            </div>
            <Link to="/tenant/operations/train/stop-points" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Quản lý ngay
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.stopPoints.slice(0, 5).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <p className="font-black text-slate-900">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {item.location?.name || 'Chưa gắn địa điểm'} • {getStopPointTypeLabel(item.type)}
                    </p>
                  </div>
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${item.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>
                    {item.isActive ? 'Đang dùng' : 'Tạm ngưng'}
                  </span>
                </div>
              </div>
            ))}
            {!loading && options.stopPoints.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có ga tàu nào.</div>
            ) : null}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Tuyến đường gần đây</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Dùng để sinh lịch dừng và ma trận giá chặng.</p>
            </div>
            <Link to="/tenant/operations/train/routes" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở tuyến đường
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.routes.slice(0, 5).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <p className="font-black text-slate-900">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {item.fromStopPoint?.name || 'Ga đầu'} → {item.toStopPoint?.name || 'Ga cuối'}
                    </p>
                  </div>
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${item.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>
                    {item.isActive ? 'Đang dùng' : 'Tạm ngưng'}
                  </span>
                </div>
              </div>
            ))}
            {!loading && options.routes.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có tuyến đường nào.</div>
            ) : null}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
        <div className="px-8 py-6 border-b border-slate-100">
          <p className="text-lg font-black text-slate-900">Chuyến cần xử lý tiếp</p>
          <p className="text-xs font-bold text-slate-400 mt-1">Từ đây có thể nhảy nhanh sang lịch dừng, giá chặng và toa tàu.</p>
        </div>
        <div className="divide-y divide-slate-50">
          {activeTrips.slice(0, 6).map((trip) => (
            <div key={trip.id} className="px-8 py-5 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div>
                <div className="flex items-center gap-3 flex-wrap">
                  <p className="font-black text-slate-900">{trip.name}</p>
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getTripStatusClass(trip.status)}`}>
                    {getTripStatusLabel(trip.status)}
                  </span>
                </div>
                <p className="text-xs font-bold text-slate-400 mt-2">{formatDateTime(trip.departureAt)}</p>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <Link to={`/tenant/operations/train/trip-stop-times?tripId=${trip.id}`} className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600">
                  Lịch dừng
                </Link>
                <Link to={`/tenant/operations/train/trip-segment-prices?tripId=${trip.id}`} className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600">
                  Giá chặng
                </Link>
                <Link to={`/tenant/providers/train/cars?tripId=${trip.id}`} className="px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white">
                  Toa tàu
                </Link>
              </div>
            </div>
          ))}
          {!loading && activeTrips.length === 0 ? (
            <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có chuyến tàu nào để điều hành.</div>
          ) : null}
        </div>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainOperationsPage;
