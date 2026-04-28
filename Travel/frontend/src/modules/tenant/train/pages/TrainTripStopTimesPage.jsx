import React, { useEffect, useState } from 'react';
import { Clock3, Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import TrainManagementPageShell from '../components/TrainManagementPageShell';
import { formatDateTime, toApiDateTimeValue, toDateTimeInputValue } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createTrainTripStopTime,
  generateTrainTripStopTimesFromRoute,
  getTrainManagerOptions,
  listTrainTrips,
  listTrainTripStopTimes,
  restoreTrainTripStopTime,
  updateTrainTripStopTime,
  deleteTrainTripStopTime,
} from '../../../../services/trainService';

function createEmptyForm() {
  return {
    stopPointId: '',
    stopIndex: 0,
    arriveAt: '',
    departAt: '',
    minutesFromStart: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    stopPointId: item.stopPointId || '',
    stopIndex: item.stopIndex ?? 0,
    arriveAt: toDateTimeInputValue(item.arriveAt),
    departAt: toDateTimeInputValue(item.departAt),
    minutesFromStart: item.minutesFromStart ?? '',
    isActive: item.isActive ?? true,
  };
}

const TrainTripStopTimesPage = () => {
  const [searchParams] = useSearchParams();
  const [trips, setTrips] = useState([]);
  const [stopPoints, setStopPoints] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadStopTimesRef = useLatestRef(loadStopTimes);

  useEffect(() => {
    let active = true;

    Promise.all([getTrainManagerOptions(), listTrainTrips()])
      .then(([optionsResponse, tripResponse]) => {
        if (!active) {
          return;
        }

        const nextTrips = Array.isArray(tripResponse?.items) ? tripResponse.items.filter((item) => !item.isDeleted) : [];
        setTrips(nextTrips);
        setStopPoints(Array.isArray(optionsResponse?.stopPoints) ? optionsResponse.stopPoints : []);
        if (!selectedTripId) {
          setSelectedTripId(nextTrips[0]?.id || '');
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được dữ liệu lịch dừng.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  const loadStopTimes = async () => {
    if (!selectedTripId) {
      setItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listTrainTripStopTimes(selectedTripId, { includeDeleted: true });
      const nextItems = Array.isArray(response?.items) ? response.items : [];
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm(createEmptyForm());
      }
    } catch (err) {
      setError(err.message || 'Không tải được lịch dừng của chuyến.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadStopTimesRef.current();
  }, [loadStopTimesRef, selectedTripId]);

  const handleCreateNew = () => {
    setSelectedId('');
    setForm(createEmptyForm());
    setNotice('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = {
        stopPointId: form.stopPointId,
        stopIndex: Number(form.stopIndex),
        arriveAt: toApiDateTimeValue(form.arriveAt),
        departAt: toApiDateTimeValue(form.departAt),
        minutesFromStart: form.minutesFromStart === '' ? null : Number(form.minutesFromStart),
        isActive: !!form.isActive,
      };

      if (selectedId) {
        await updateTrainTripStopTime(selectedId, payload);
        setNotice('Đã cập nhật lịch dừng.');
      } else {
        await createTrainTripStopTime(selectedTripId, payload);
        setNotice('Đã thêm lịch dừng mới.');
      }

      await loadStopTimesRef.current();
    } catch (err) {
      setError(err.message || 'Không lưu được lịch dừng.');
    } finally {
      setSaving(false);
    }
  };

  const handleGenerate = async () => {
    if (!selectedTripId) {
      return;
    }

    setError('');
    setNotice('');

    try {
      const selectedTrip = trips.find((trip) => trip.id === selectedTripId);
      await generateTrainTripStopTimesFromRoute(selectedTripId, {
        departureAt: selectedTrip?.departureAt,
        useRouteStopMinutes: true,
      });
      setNotice('Đã sinh lịch dừng từ tuyến đường.');
      await loadStopTimesRef.current();
    } catch (err) {
      setError(err.message || 'Không sinh được lịch dừng từ tuyến.');
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreTrainTripStopTime(item.id);
        setNotice('Đã khôi phục lịch dừng.');
      } else {
        await deleteTrainTripStopTime(item.id);
        setNotice('Đã ẩn lịch dừng.');
      }

      await loadStopTimesRef.current();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái lịch dừng.');
    }
  };

  const stopPointLookup = Object.fromEntries(stopPoints.map((item) => [item.id, item]));

  return (
    <TrainManagementPageShell
      pageKey="trip-stop-times"
      title="Lịch dừng theo chuyến"
      subtitle="Mỗi chuyến có thể có bộ stop times riêng để public detail và chọn chỗ theo chặng."
      error={error}
      notice={notice}
      actions={(
        <>
          <button
            type="button"
            onClick={loadStopTimes}
            className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button
            type="button"
            onClick={handleCreateNew}
            className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2"
          >
            <Plus size={16} />
            Thêm lịch dừng
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách lịch dừng</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Có thể sinh tự động từ các ga dừng của tuyến rồi chỉnh tay từng chuyến.</p>
            </div>
            <div className="flex items-center gap-3">
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
              <button
                type="button"
                onClick={handleGenerate}
                className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600"
              >
                Sinh từ tuyến
              </button>
            </div>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải lịch dừng...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có lịch dừng nào cho chuyến này.</div>
            ) : items.map((item) => (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => {
                  setSelectedId(item.id);
                  setForm(hydrateForm(item));
                }}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    setSelectedId(item.id);
                    setForm(hydrateForm(item));
                  }
                }}
                className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                      <Clock3 size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">
                          Stop #{item.stopIndex} • {stopPointLookup[item.stopPointId]?.name || item.stopPointId}
                        </p>
                        {item.isDeleted ? (
                          <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                            Đã ẩn
                          </span>
                        ) : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        Đến: {formatDateTime(item.arriveAt)} • Đi: {formatDateTime(item.departAt)}
                      </p>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={(event) => {
                      event.stopPropagation();
                      handleToggleDelete(item);
                    }}
                    className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                  >
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật lịch dừng' : 'Thêm lịch dừng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Ga dừng này sẽ sinh ra các chặng giá liên quan.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga tàu</span>
            <select
              value={form.stopPointId}
              onChange={(event) => setForm((current) => ({ ...current, stopPointId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              required
            >
              <option value="">Chọn ga dừng</option>
              {stopPoints.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thứ tự dừng</span>
            <input
              type="number"
              value={form.stopIndex}
              onChange={(event) => setForm((current) => ({ ...current, stopIndex: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            />
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ đến</span>
              <input
                type="datetime-local"
                value={form.arriveAt}
                onChange={(event) => setForm((current) => ({ ...current, arriveAt: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>

            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ rời ga</span>
              <input
                type="datetime-local"
                value={form.departAt}
                onChange={(event) => setForm((current) => ({ ...current, departAt: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phút đi từ điểm đầu</span>
            <input
              type="number"
              value={form.minutesFromStart}
              onChange={(event) => setForm((current) => ({ ...current, minutesFromStart: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            />
          </label>

          <label className="inline-flex items-center gap-3 text-sm font-bold text-slate-600">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              className="w-4 h-4 rounded border-slate-300 text-blue-600 focus:ring-blue-200"
            />
            Kích hoạt lịch dừng này
          </label>

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-2xl px-5 py-4 text-sm font-black uppercase tracking-widest transition-all ${
              saving ? 'bg-slate-200 text-slate-500' : 'bg-slate-900 text-white hover:bg-[#1EB4D4]'
            }`}
          >
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu thay đổi' : 'Thêm lịch dừng'}
          </button>
        </form>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainTripStopTimesPage;
