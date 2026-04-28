import React, { useEffect, useMemo, useState } from 'react';
import { Armchair, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import TrainManagementPageShell from '../components/TrainManagementPageShell';
import { getSeatStatusClass, getSeatStatusLabel } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  getTrainManagerTripSeats,
  listTrainTrips,
  listTrainTripStopTimes,
} from '../../../../services/trainService';

function buildSeatGroups(seats) {
  const compartments = new Map();
  const normalSeats = [];

  seats.forEach((seat) => {
    if (seat.compartmentCode || seat.compartmentIndex !== null) {
      const key = `${seat.compartmentCode || 'Khoang'}-${seat.compartmentIndex ?? 'x'}`;
      if (!compartments.has(key)) {
        compartments.set(key, []);
      }

      compartments.get(key).push(seat);
      return;
    }

    normalSeats.push(seat);
  });

  return {
    compartments: [...compartments.entries()].map(([key, items]) => ({
      key,
      label: items[0]?.compartmentCode || `Khoang ${items[0]?.compartmentIndex ?? ''}`.trim(),
      items: [...items].sort((left, right) => {
        if (left.rowIndex !== right.rowIndex) {
          return left.rowIndex - right.rowIndex;
        }

        return left.columnIndex - right.columnIndex;
      }),
    })),
    seats: normalSeats.sort((left, right) => {
      if (left.rowIndex !== right.rowIndex) {
        return left.rowIndex - right.rowIndex;
      }

      return left.columnIndex - right.columnIndex;
    }),
  };
}

const TrainTripSeatsPage = () => {
  const [searchParams] = useSearchParams();
  const [trips, setTrips] = useState([]);
  const [stopTimes, setStopTimes] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [fromTripStopTimeId, setFromTripStopTimeId] = useState('');
  const [toTripStopTimeId, setToTripStopTimeId] = useState('');
  const [seatData, setSeatData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadSeatsRef = useLatestRef(loadSeats);

  useEffect(() => {
    let active = true;

    listTrainTrips()
      .then((response) => {
        if (!active) {
          return;
        }

        const nextTrips = Array.isArray(response?.items) ? response.items.filter((item) => !item.isDeleted) : [];
        setTrips(nextTrips);

        if (!selectedTripId) {
          setSelectedTripId(nextTrips[0]?.id || '');
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được danh sách chuyến tàu.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  useEffect(() => {
    if (!selectedTripId) {
      setStopTimes([]);
      return;
    }

    let active = true;

    listTrainTripStopTimes(selectedTripId)
      .then((response) => {
        if (!active) {
          return;
        }

        const nextItems = Array.isArray(response?.items) ? response.items : [];
        setStopTimes(nextItems);
        setFromTripStopTimeId(nextItems[0]?.id || '');
        setToTripStopTimeId(nextItems[1]?.id || nextItems[0]?.id || '');
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được lịch dừng của chuyến tàu.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  const loadSeats = async () => {
    if (!selectedTripId || !fromTripStopTimeId || !toTripStopTimeId) {
      setSeatData(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await getTrainManagerTripSeats(selectedTripId, {
        fromTripStopTimeId,
        toTripStopTimeId,
      });

      setSeatData(response);
    } catch (err) {
      setError(err.message || 'Không tải được sơ đồ chỗ theo chuyến.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSeatsRef.current();
  }, [selectedTripId, fromTripStopTimeId, toTripStopTimeId, loadSeatsRef]);

  const stopTimeLookup = useMemo(
    () => Object.fromEntries(stopTimes.map((item) => [item.id, item])),
    [stopTimes],
  );

  return (
    <TrainManagementPageShell
      pageKey="trip-seats"
      title="Sơ đồ chỗ theo chuyến"
      subtitle="Kiểm tra trực tiếp trạng thái ghế/giường trên đúng chặng mà khách hàng đang tìm mua."
      error={error}
      actions={(
        <button
          type="button"
          onClick={loadSeats}
          className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
        >
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
        <div className="px-8 py-6 border-b border-slate-100 grid grid-cols-1 lg:grid-cols-3 gap-4">
          <label className="space-y-2">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chuyến tàu</span>
            <select
              value={selectedTripId}
              onChange={(event) => setSelectedTripId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {trips.map((trip) => (
                <option key={trip.id} value={trip.id}>{trip.name}</option>
              ))}
            </select>
          </label>
          <label className="space-y-2">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga đi</span>
            <select
              value={fromTripStopTimeId}
              onChange={(event) => setFromTripStopTimeId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>
                  Ga #{item.stopIndex} - {item.stopPoint?.name || item.stopPointId}
                </option>
              ))}
            </select>
          </label>
          <label className="space-y-2">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga đến</span>
            <select
              value={toTripStopTimeId}
              onChange={(event) => setToTripStopTimeId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>
                  Ga #{item.stopIndex} - {item.stopPoint?.name || item.stopPointId}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="p-8 space-y-8">
          <div className="flex gap-4 flex-wrap">
            {['available', 'held_by_me', 'held', 'booked', 'inactive'].map((status) => (
              <div key={status} className="flex items-center gap-3">
                <div className={`w-5 h-5 rounded-lg ${getSeatStatusClass(status)}`} />
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">{getSeatStatusLabel(status)}</span>
              </div>
            ))}
          </div>

          {loading ? (
            <div className="text-sm font-bold text-slate-500">Đang tải sơ đồ chỗ...</div>
          ) : !seatData?.cars?.length ? (
            <div className="text-sm font-bold text-slate-500">Chưa có dữ liệu chỗ ngồi cho chuyến tàu này.</div>
          ) : (
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
              {seatData.cars.map((car) => {
                const grouped = buildSeatGroups(Array.isArray(car.seats) ? car.seats : []);
                const availableCount = (car.seats || []).filter((seat) => seat.status === 'available').length;

                return (
                  <div key={car.id} className="rounded-[2.5rem] border border-slate-100 bg-slate-50 p-6 space-y-5">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="text-lg font-black text-slate-900">Toa {car.carNumber}</p>
                        <p className="text-xs font-bold text-slate-400 mt-1">
                          {car.cabinClass || 'Chưa khai báo hạng'} • {availableCount}/{(car.seats || []).length} chỗ trống
                        </p>
                      </div>
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-white text-slate-600 border border-slate-100">
                        {car.carType}
                      </span>
                    </div>

                    {grouped.compartments.length > 0 ? (
                      <div className="space-y-4">
                        {grouped.compartments.map((group) => (
                          <div key={group.key} className="rounded-[2rem] border border-white bg-white p-5">
                            <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-4">{group.label || 'Khoang'}</p>
                            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                              {group.items.map((seat) => (
                                <div
                                  key={seat.id}
                                  className={`h-20 rounded-2xl border-2 flex flex-col items-center justify-center ${getSeatStatusClass(seat.status)}`}
                                >
                                  <div className="mb-1"><Armchair size={18} /></div>
                                  <span className="text-[10px] font-black uppercase tracking-widest">{seat.seatNumber}</span>
                                </div>
                              ))}
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                        {grouped.seats.map((seat) => (
                          <div
                            key={seat.id}
                            className={`h-20 rounded-2xl border-2 flex flex-col items-center justify-center ${getSeatStatusClass(seat.status)}`}
                          >
                            <div className="mb-1"><Armchair size={18} /></div>
                            <span className="text-[10px] font-black uppercase tracking-widest">{seat.seatNumber}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}

          {seatData?.segment ? (
            <div className="rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-600">
              Chặng đang xem: {stopTimeLookup[seatData.segment.fromTripStopTimeId]?.stopPoint?.name || 'Ga đi'} {'->'} {stopTimeLookup[seatData.segment.toTripStopTimeId]?.stopPoint?.name || 'Ga đến'}
            </div>
          ) : null}
        </div>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainTripSeatsPage;
