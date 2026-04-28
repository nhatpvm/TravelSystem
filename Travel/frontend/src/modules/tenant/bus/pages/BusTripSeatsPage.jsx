import React, { useEffect, useState } from 'react';
import { Armchair, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import BusManagementPageShell from '../components/BusManagementPageShell';
import { getSeatStatusClass, getSeatStatusLabel } from '../utils/presentation';
import { getBusManagerTripSeats, listBusTrips, listBusTripStopTimes } from '../../../../services/busService';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

function buildSeatGrid(seatMap, seats) {
  const totalRows = Number(seatMap?.totalRows || 0);
  const totalColumns = Number(seatMap?.totalColumns || 0);
  const deckCount = Number(seatMap?.deckCount || 1);
  const grouped = [];

  for (let deck = 1; deck <= deckCount; deck += 1) {
    const rows = [];

    for (let rowIndex = 0; rowIndex < totalRows; rowIndex += 1) {
      const columns = [];

      for (let columnIndex = 0; columnIndex < totalColumns; columnIndex += 1) {
        const seat = seats.find((item) => item.deckIndex === deck && item.rowIndex === rowIndex && item.columnIndex === columnIndex) || null;
        columns.push(seat);
      }

      rows.push(columns);
    }

    grouped.push({ deck, rows });
  }

  return grouped;
}

const BusTripSeatsPage = () => {
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

    listBusTrips()
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
          setError(err.message || 'Không tải được danh sách chuyến.');
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

    listBusTripStopTimes(selectedTripId)
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
          setError(err.message || 'Không tải được lịch dừng của chuyến.');
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
      const response = await getBusManagerTripSeats(selectedTripId, { fromTripStopTimeId, toTripStopTimeId });
      setSeatData(response);
    } catch (err) {
      setError(err.message || 'Không tải được sơ đồ ghế theo chuyến.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSeatsRef.current();
  }, [selectedTripId, fromTripStopTimeId, toTripStopTimeId, loadSeatsRef]);

  const seats = seatData?.seats || [];
  const seatGroups = buildSeatGrid(seatData?.seatMap, seats);

  return (
    <BusManagementPageShell
      pageKey="trip-seats"
      title="Sơ đồ ghế theo chuyến"
      subtitle="Xem trạng thái ghế theo đúng chặng đang chọn của chuyến xe."
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
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chuyến xe</span>
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
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điểm lên</span>
            <select
              value={fromTripStopTimeId}
              onChange={(event) => setFromTripStopTimeId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>Điểm dừng số {Number(item.stopIndex) + 1}</option>
              ))}
            </select>
          </label>
          <label className="space-y-2">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điểm xuống</span>
            <select
              value={toTripStopTimeId}
              onChange={(event) => setToTripStopTimeId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>Điểm dừng số {Number(item.stopIndex) + 1}</option>
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
            <div className="text-sm font-bold text-slate-500">Đang tải sơ đồ ghế...</div>
          ) : seatGroups.length === 0 ? (
            <div className="text-sm font-bold text-slate-500">Chưa có dữ liệu sơ đồ ghế cho chuyến này.</div>
          ) : (
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-10">
              {seatGroups.map((deck) => (
                <div key={deck.deck} className="space-y-6">
                  <p className="text-center font-black text-slate-900 uppercase tracking-[0.2em] text-[10px] bg-slate-50 py-3 rounded-2xl">
                    {seatGroups.length > 1 ? `Tầng ${deck.deck}` : 'Sơ đồ ghế'}
                  </p>
                  <div className="space-y-4 bg-slate-50 p-8 rounded-[2.5rem] border border-slate-100">
                    {deck.rows.map((columns, rowIndex) => (
                      <div
                        key={`${deck.deck}-${rowIndex}`}
                        className="grid gap-4"
                        style={{ gridTemplateColumns: `repeat(${columns.length}, minmax(0, 1fr))` }}
                      >
                        {columns.map((seat, columnIndex) => (
                          <div
                            key={seat?.id || `${deck.deck}-${rowIndex}-${columnIndex}`}
                            className={`h-20 rounded-2xl flex flex-col items-center justify-center border-2 ${
                              seat ? getSeatStatusClass(seat.status) : 'border-dashed border-slate-100 bg-transparent'
                            }`}
                          >
                            {seat ? (
                              <>
                                <div className="p-2 rounded-lg mb-1 bg-white/20">
                                  <Armchair size={20} />
                                </div>
                                <span className="text-[10px] font-black uppercase tracking-tighter">{seat.seatNumber}</span>
                              </>
                            ) : null}
                          </div>
                        ))}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusTripSeatsPage;
