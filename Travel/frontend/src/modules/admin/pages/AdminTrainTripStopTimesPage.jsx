import React, { useEffect, useMemo, useState } from 'react';
import { Clock3, Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import AdminTrainPageShell from '../train/components/AdminTrainPageShell';
import useAdminTrainScope from '../train/hooks/useAdminTrainScope';
import { formatDateTime, toApiDateTimeValue, toDateTimeInputValue } from '../../tenant/train/utils/presentation';
import {
  createAdminTrainTripStopTime,
  generateAdminTrainTripStopTimesFromRoute,
  getAdminTrainOptions,
  listAdminTrainTrips,
  listAdminTrainTripStopTimes,
  restoreAdminTrainTripStopTime,
  updateAdminTrainTripStopTime,
  deleteAdminTrainTripStopTime,
} from '../../../services/trainService';

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

export default function AdminTrainTripStopTimesPage() {
  const [searchParams] = useSearchParams();
  const { tenantId, tenants, selectedTenantId, setSelectedTenantId, selectedTenant, scopeError } = useAdminTrainScope();
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

  useEffect(() => {
    let active = true;

    async function loadOptions() {
      if (!tenantId) {
        setTrips([]);
        setStopPoints([]);
        return;
      }

      try {
        const [optionsResponse, tripResponse] = await Promise.all([
          getAdminTrainOptions(tenantId),
          listAdminTrainTrips({ includeDeleted: true }, tenantId),
        ]);

        if (!active) return;

        const nextTrips = Array.isArray(tripResponse?.items) ? tripResponse.items.filter((item) => !item.isDeleted) : [];
        setTrips(nextTrips);
        setStopPoints(Array.isArray(optionsResponse?.stopPoints) ? optionsResponse.stopPoints : []);
        setSelectedTripId((current) => current || searchParams.get('tripId') || nextTrips[0]?.id || '');
      } catch (requestError) {
        if (active) {
          setError(requestError.message || 'Không tải được dữ liệu lịch dừng.');
        }
      }
    }

    loadOptions();
    return () => { active = false; };
  }, [tenantId]);

  async function loadStopTimes() {
    if (!selectedTripId || !tenantId) {
      setItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listAdminTrainTripStopTimes(selectedTripId, { includeDeleted: true }, tenantId);
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
    } catch (requestError) {
      setError(requestError.message || 'Không tải được lịch dừng của chuyến.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadStopTimes();
  }, [tenantId, selectedTripId]);

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm());
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!tenantId || !selectedTripId) return;

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
        await updateAdminTrainTripStopTime(selectedId, payload, tenantId);
        setNotice('Đã cập nhật lịch dừng.');
      } else {
        await createAdminTrainTripStopTime(selectedTripId, payload, tenantId);
        setNotice('Đã thêm lịch dừng mới.');
      }

      await loadStopTimes();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được lịch dừng.');
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerate() {
    if (!selectedTripId || !tenantId) return;

    setError('');
    setNotice('');

    try {
      const selectedTrip = trips.find((trip) => trip.id === selectedTripId);
      await generateAdminTrainTripStopTimesFromRoute(selectedTripId, {
        departureAt: selectedTrip?.departureAt,
        useRouteStopMinutes: true,
      }, tenantId);
      setNotice('Đã sinh lịch dừng từ tuyến đường.');
      await loadStopTimes();
    } catch (requestError) {
      setError(requestError.message || 'Không sinh được lịch dừng từ tuyến.');
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) return;

    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreAdminTrainTripStopTime(item.id, tenantId);
        setNotice('Đã khôi phục lịch dừng.');
      } else {
        await deleteAdminTrainTripStopTime(item.id, tenantId);
        setNotice('Đã ẩn lịch dừng.');
      }

      await loadStopTimes();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái lịch dừng.');
    }
  }

  const stopPointLookup = useMemo(() => Object.fromEntries(stopPoints.map((item) => [item.id, item])), [stopPoints]);

  return (
    <AdminTrainPageShell
      pageKey="trip-stop-times"
      title="Lịch dừng theo chuyến"
      subtitle="Admin kiểm tra trực tiếp stop times của từng chuyến để public detail và logic chặng luôn đúng."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <>
          <button type="button" onClick={loadStopTimes} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
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
              <p className="text-xs font-bold text-slate-400 mt-1">Có thể sinh tự động từ ga dừng của tuyến rồi chỉnh tay từng chuyến.</p>
            </div>
            <div className="flex items-center gap-3">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <select value={selectedTripId} onChange={(event) => setSelectedTripId(event.target.value)} className="bg-transparent text-sm font-bold text-slate-700 outline-none">
                  {trips.map((trip) => (
                    <option key={trip.id} value={trip.id}>{trip.name}</option>
                  ))}
                </select>
              </div>
              <button type="button" onClick={handleGenerate} className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600">
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
                onClick={() => { setSelectedId(item.id); setForm(hydrateForm(item)); }}
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
                        <p className="font-black text-slate-900">Stop #{item.stopIndex} • {stopPointLookup[item.stopPointId]?.name || item.stopPointId}</p>
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">Đến: {formatDateTime(item.arriveAt)} • Đi: {formatDateTime(item.departAt)}</p>
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
            <select value={form.stopPointId} onChange={(event) => setForm((current) => ({ ...current, stopPointId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
              <option value="">Chọn ga tàu</option>
              {stopPoints.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Stop index</span>
              <input type="number" value={form.stopIndex} onChange={(event) => setForm((current) => ({ ...current, stopIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phút từ đầu tuyến</span>
              <input type="number" value={form.minutesFromStart} onChange={(event) => setForm((current) => ({ ...current, minutesFromStart: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            </label>
            <label className="flex items-center gap-3 pt-8">
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" />
              <span className="text-sm font-bold text-slate-700">Cho phép hoạt động</span>
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ đến</span>
              <input type="datetime-local" value={form.arriveAt} onChange={(event) => setForm((current) => ({ ...current, arriveAt: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ đi</span>
              <input type="datetime-local" value={form.departAt} onChange={(event) => setForm((current) => ({ ...current, departAt: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
          </div>

          <button type="submit" disabled={saving || !tenantId || !selectedTripId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu cập nhật' : 'Thêm lịch dừng'}
          </button>
        </form>
      </div>
    </AdminTrainPageShell>
  );
}
