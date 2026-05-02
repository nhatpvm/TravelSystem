import React, { useEffect, useState } from 'react';
import { Armchair, Bus, RefreshCw, ShieldCheck, Ticket } from 'lucide-react';
import { Link } from 'react-router-dom';
import BusManagementPageShell from '../bus/components/BusManagementPageShell';
import { getSeatStatusClass, getSeatStatusLabel, parseAmenities } from '../bus/utils/presentation';
import { getBusManagerOptions, listBusManagerSeatHolds, listBusTrips, listBusVehicleDetails, releaseBusManagerSeatHold } from '../../../services/busService';

const BusProvidersPage = () => {
  const [options, setOptions] = useState({ vehicles: [], seatMaps: [] });
  const [vehicleDetails, setVehicleDetails] = useState([]);
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
      const [optionsResponse, tripResponse, detailResponse] = await Promise.all([
        getBusManagerOptions(),
        listBusTrips(),
        listBusVehicleDetails(),
      ]);

      const nextTrips = Array.isArray(tripResponse?.items) ? tripResponse.items.filter((item) => !item.isDeleted) : [];
      setTrips(nextTrips);
      setSelectedTripId((current) => current || nextTrips[0]?.id || '');
      setOptions({
        vehicles: Array.isArray(optionsResponse?.vehicles) ? optionsResponse.vehicles : [],
        seatMaps: Array.isArray(optionsResponse?.seatMaps) ? optionsResponse.seatMaps : [],
      });
      setVehicleDetails(Array.isArray(detailResponse?.items) ? detailResponse.items.filter((item) => !item.isDeleted) : []);
    } catch (err) {
      setError(err.message || 'Không tải được dữ liệu đội xe và lượt giữ chỗ.');
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

    listBusManagerSeatHolds(selectedTripId)
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
      await releaseBusManagerSeatHold(holdToken);
      setNotice('Đã giải phóng giữ chỗ.');
      const response = await listBusManagerSeatHolds(selectedTripId);
      setHolds(Array.isArray(response?.items) ? response.items : []);
    } catch (err) {
      setError(err.message || 'Không giải phóng được giữ chỗ.');
    }
  };

  const seatMapAttachedVehicles = options.vehicles.filter((item) => item.seatMapId);

  return (
    <BusManagementPageShell
      pageKey="providers"
      title="Đội xe & Giữ chỗ"
      subtitle="Theo dõi phương tiện, tiện ích xe và lượt giữ chỗ theo từng chuyến."
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
          { label: 'Xe đang khai thác', value: options.vehicles.length, icon: Bus },
          { label: 'Xe có sơ đồ ghế', value: seatMapAttachedVehicles.length, icon: Armchair },
          { label: 'Chi tiết xe đã khai báo', value: vehicleDetails.length, icon: ShieldCheck },
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
              <p className="text-lg font-black text-slate-900">Chi tiết xe khách</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Tiện ích và loại xe đang được hiển thị cho khách hàng.</p>
            </div>
            <Link to="/tenant/providers/bus/vehicle-details" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở chi tiết
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {vehicleDetails.slice(0, 5).map((item) => {
              const vehicle = options.vehicles.find((vehicleItem) => vehicleItem.id === item.vehicleId);
              const amenities = parseAmenities(item.amenitiesJson).slice(0, 3);

              return (
                <div key={item.id} className="px-8 py-5">
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="font-black text-slate-900">{vehicle?.name || item.vehicleId}</p>
                      <p className="text-xs font-bold text-slate-400 mt-1">
                        {item.busType || 'Chưa khai báo loại xe'} • {vehicle?.plateNumber || 'Chưa có biển số'}
                      </p>
                      {amenities.length > 0 ? (
                        <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">
                          {amenities.join(' • ')}
                        </p>
                      ) : null}
                    </div>
                    <Link to="/tenant/providers/bus/vehicle-details" className="px-3 py-2 rounded-xl bg-slate-100 text-[10px] font-black uppercase tracking-widest text-slate-600">
                      Sửa
                    </Link>
                  </div>
                </div>
              );
            })}
            {!loading && vehicleDetails.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có cấu hình chi tiết xe khách.</div>
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
                <option value="">Chọn chuyến xe</option>
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
                  <p className="font-black text-slate-900">{hold.seatId}</p>
                  <p className="text-xs font-bold text-slate-400 mt-1">Mã giữ chỗ: {hold.holdToken}</p>
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
            <Link to={selectedTripId ? `/tenant/providers/bus/seat-holds?tripId=${selectedTripId}` : '/tenant/providers/bus/seat-holds'} className="text-xs font-black uppercase tracking-widest text-blue-600">
              Xem toàn bộ lượt giữ chỗ
            </Link>
          </div>
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusProvidersPage;
