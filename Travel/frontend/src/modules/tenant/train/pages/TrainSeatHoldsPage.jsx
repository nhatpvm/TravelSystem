import React, { useEffect, useState } from 'react';
import { RefreshCw, ShieldCheck } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import TrainManagementPageShell from '../components/TrainManagementPageShell';
import { formatDateTime } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  listTrainManagerSeatHolds,
  listTrainTrips,
  releaseTrainManagerSeatHold,
} from '../../../../services/trainService';

function getHoldStatusLabel(status) {
  switch (Number(status)) {
    case 1:
      return 'Đang giữ';
    case 2:
      return 'Đã xác nhận';
    case 3:
      return 'Đã hủy';
    case 4:
      return 'Đã hết hạn';
    default:
      return 'Không rõ';
  }
}

function getHoldStatusClass(status) {
  switch (Number(status)) {
    case 1:
      return 'bg-amber-100 text-amber-700';
    case 2:
      return 'bg-rose-100 text-rose-700';
    case 4:
      return 'bg-slate-100 text-slate-600';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

const TrainSeatHoldsPage = () => {
  const [searchParams] = useSearchParams();
  const [trips, setTrips] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [items, setItems] = useState([]);
  const [includeExpired, setIncludeExpired] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadHoldsRef = useLatestRef(loadHolds);

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
          setError(err.message || 'Không tải được danh sách chuyến tàu để xem giữ chỗ.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  const loadHolds = async () => {
    if (!selectedTripId) {
      setItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listTrainManagerSeatHolds(selectedTripId, { includeExpired });
      setItems(Array.isArray(response?.items) ? response.items : []);
    } catch (err) {
      setError(err.message || 'Không tải được danh sách giữ chỗ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHoldsRef.current();
  }, [selectedTripId, includeExpired, loadHoldsRef]);

  const handleRelease = async (holdToken) => {
    setError('');
    setNotice('');

    try {
      await releaseTrainManagerSeatHold(holdToken);
      setNotice('Đã giải phóng lượt giữ chỗ.');
      await loadHoldsRef.current();
    } catch (err) {
      setError(err.message || 'Không giải phóng được lượt giữ chỗ.');
    }
  };

  const selectedTrip = trips.find((trip) => trip.id === selectedTripId);

  return (
    <TrainManagementPageShell
      pageKey="seat-holds"
      title="Giữ chỗ theo chuyến tàu"
      subtitle="Theo dõi các lượt giữ chỗ còn hiệu lực hoặc đã hết hạn để đội vận hành chủ động xử lý."
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
              {selectedTrip ? `${selectedTrip.name} • ${selectedTrip.trainNumber}` : 'Chọn chuyến tàu cần theo dõi'}
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
            <div className="px-8 py-10 text-sm font-bold text-slate-500">Chuyến tàu này chưa có lượt giữ chỗ nào.</div>
          ) : items.map((item) => (
            <div key={item.id} className="px-8 py-5 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div className="flex items-start gap-4">
                <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                  <ShieldCheck size={20} />
                </div>
                <div>
                  <div className="flex items-center gap-3 flex-wrap">
                    <p className="font-black text-slate-900">{item.trainCarSeatId}</p>
                    <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getHoldStatusClass(item.status)}`}>
                      {getHoldStatusLabel(item.status)}
                    </span>
                  </div>
                  <p className="text-xs font-bold text-slate-400 mt-2">Hold token: {item.holdToken}</p>
                  <p className="text-xs font-bold text-slate-400 mt-1">
                    Chặng {item.fromStopIndex} {'->'} {item.toStopIndex} • Hết hạn: {formatDateTime(item.holdExpiresAt)}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => handleRelease(item.holdToken)}
                disabled={Number(item.status) !== 1}
                className="px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white disabled:opacity-40"
              >
                Giải phóng
              </button>
            </div>
          ))}
        </div>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainSeatHoldsPage;
