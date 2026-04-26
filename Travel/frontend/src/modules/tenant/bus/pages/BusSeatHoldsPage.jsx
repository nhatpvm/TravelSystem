import React, { useEffect, useState } from 'react';
import { RefreshCw, ShieldCheck } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import BusManagementPageShell from '../components/BusManagementPageShell';
import { formatDateTime, getTripStatusClass, getTripStatusLabel } from '../utils/presentation';
import { listBusManagerSeatHolds, listBusTrips, releaseBusManagerSeatHold } from '../../../../services/busService';

const BusSeatHoldsPage = () => {
  const [searchParams] = useSearchParams();
  const [trips, setTrips] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [items, setItems] = useState([]);
  const [includeExpired, setIncludeExpired] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

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
          setError(err.message || 'Không tải được danh sách chuyến để xem giữ chỗ.');
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const loadHolds = async () => {
    if (!selectedTripId) {
      setItems([]);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listBusManagerSeatHolds(selectedTripId, { includeExpired });
      setItems(Array.isArray(response?.items) ? response.items : []);
    } catch (err) {
      setError(err.message || 'Không tải được danh sách giữ chỗ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHolds();
  }, [selectedTripId, includeExpired]);

  const handleRelease = async (holdToken) => {
    setError('');
    setNotice('');

    try {
      await releaseBusManagerSeatHold(holdToken);
      setNotice('Đã giải phóng giữ chỗ.');
      await loadHolds();
    } catch (err) {
      setError(err.message || 'Không giải phóng được giữ chỗ.');
    }
  };

  const selectedTrip = trips.find((trip) => trip.id === selectedTripId);

  return (
    <BusManagementPageShell
      pageKey="seat-holds"
      title="Giữ chỗ theo chuyến"
      subtitle="Theo dõi các lượt giữ chỗ đang còn hiệu lực hoặc đã hết hạn theo từng chuyến xe."
      error={error}
      notice={notice}
      actions={(
        <>
          <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-600">
            <input
              type="checkbox"
              checked={includeExpired}
              onChange={(event) => setIncludeExpired(event.target.checked)}
              className="h-4 w-4 rounded border-slate-300"
            />
            Hiển thị cả lượt giữ đã hết hạn
          </label>
          <button
            type="button"
            onClick={loadHolds}
            className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
        </>
      )}
    >
      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
        <div className="px-8 py-6 border-b border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div>
            <p className="text-lg font-black text-slate-900">Danh sách giữ chỗ</p>
            <p className="text-xs font-bold text-slate-400 mt-1">
              {selectedTrip ? `${selectedTrip.name} • ${getTripStatusLabel(selectedTrip.status)}` : 'Chọn chuyến xe cần theo dõi'}
            </p>
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
          {loading ? (
            <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải danh sách giữ chỗ...</div>
          ) : items.length === 0 ? (
            <div className="px-8 py-10 text-sm font-bold text-slate-500">Chuyến này chưa có lượt giữ chỗ nào.</div>
          ) : items.map((item) => (
            <div key={item.id} className="px-8 py-5 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div className="flex items-start gap-4">
                <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                  <ShieldCheck size={20} />
                </div>
                <div>
                  <div className="flex items-center gap-3 flex-wrap">
                    <p className="font-black text-slate-900">Ghế {item.seatNumber || item.seatId}</p>
                    <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${item.status === 1 ? 'bg-amber-100 text-amber-700' : item.status === 4 ? 'bg-slate-100 text-slate-600' : 'bg-rose-100 text-rose-700'}`}>
                      {item.status === 1 ? 'Đang giữ' : item.status === 4 ? 'Đã hết hạn' : 'Đã đóng'}
                    </span>
                    {selectedTrip ? (
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getTripStatusClass(selectedTrip.status)}`}>
                        {getTripStatusLabel(selectedTrip.status)}
                      </span>
                    ) : null}
                  </div>
                  <p className="text-xs font-bold text-slate-400 mt-2">
                    Hold token: {item.holdToken}
                  </p>
                  <p className="text-xs font-bold text-slate-400 mt-1">
                    Chặng từ điểm dừng số {Number(item.fromStopIndex) + 1} đến điểm dừng số {Number(item.toStopIndex) + 1} • Hết hạn: {formatDateTime(item.holdExpiresAt)}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => handleRelease(item.holdToken)}
                disabled={item.status !== 1}
                className="px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white disabled:opacity-40"
              >
                Giải phóng
              </button>
            </div>
          ))}
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusSeatHoldsPage;
