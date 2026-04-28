import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminFlightAirport,
  createFlightAirport,
  deleteAdminFlightAirport,
  deleteFlightAirport,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightAirports,
  listFlightAirports,
  restoreAdminFlightAirport,
  restoreFlightAirport,
  updateAdminFlightAirport,
  updateFlightAirport,
} from '../../../../services/flightService';

function createEmptyForm() {
  return {
    locationId: '',
    code: '',
    name: '',
    iataCode: '',
    icaoCode: '',
    timeZone: 'Asia/Ho_Chi_Minh',
    latitude: '',
    longitude: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    locationId: item.locationId || '',
    code: item.code || '',
    name: item.name || '',
    iataCode: item.iataCode || '',
    icaoCode: item.icaoCode || '',
    timeZone: item.timeZone || 'Asia/Ho_Chi_Minh',
    latitude: item.latitude ?? '',
    longitude: item.longitude ?? '',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    locationId: form.locationId,
    code: form.code.trim(),
    name: form.name.trim(),
    iataCode: form.iataCode.trim() || null,
    icaoCode: form.icaoCode.trim() || null,
    timeZone: form.timeZone.trim() || null,
    latitude: form.latitude === '' ? null : Number(form.latitude),
    longitude: form.longitude === '' ? null : Number(form.longitude),
    isActive: !!form.isActive,
  };
}

export default function FlightAirportsPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [locations, setLocations] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightAirports(params, tenantId) : listFlightAirports;
  const createFn = isAdmin ? (payload) => createAdminFlightAirport(payload, tenantId) : createFlightAirport;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightAirport(id, payload, tenantId) : updateFlightAirport;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightAirport(id, tenantId) : deleteFlightAirport;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightAirport(id, tenantId) : restoreFlightAirport;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLocations([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, airportsResponse] = await Promise.all([
        isAdmin ? getAdminFlightOptions(tenantId) : getFlightManagerOptions(),
        listFn({ includeDeleted: true, pageSize: 100 }),
      ]);

      const nextLocations = Array.isArray(optionsResponse?.locations) ? optionsResponse.locations : [];
      const nextItems = Array.isArray(airportsResponse?.items) ? airportsResponse.items : [];
      setLocations(nextLocations);
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          locationId: nextLocations[0]?.id || '',
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách sân bay.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, loadDataRef, tenantId]);

  const locationLookup = useMemo(
    () => Object.fromEntries(locations.map((item) => [item.id, item])),
    [locations],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      locationId: locations[0]?.id || '',
    });
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);
      if (selectedId) {
        await updateFn(selectedId, payload);
        setNotice('Đã cập nhật sân bay.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo sân bay mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được sân bay.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreFn(item.id);
        setNotice('Đã khôi phục sân bay.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn sân bay.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái sân bay.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="airports"
      title="Sân bay"
      subtitle="Map giữa catalog location và điểm bay để public search có thể dùng đúng điểm đi, điểm đến."
      notice={notice}
      error={error}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm sân bay
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách sân bay</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Mỗi sân bay liên kết với một location airport trong master data.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải sân bay...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có sân bay nào.</div>
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
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.name}</p>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.iataCode || item.code} · {item.icaoCode || '---'}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{locationLookup[item.locationId]?.name || item.timeZone || 'Asia/Ho_Chi_Minh'}</p>
                  </div>
                  <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-6">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật sân bay' : 'Tạo sân bay mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Giữ mã IATA/ICAO đúng chuẩn để public search hoạt động ổn định.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.locationId} onChange={(event) => setForm((current) => ({ ...current, locationId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn location airport</option>
              {locations.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã sân bay nội bộ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên sân bay" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.iataCode} onChange={(event) => setForm((current) => ({ ...current, iataCode: event.target.value.toUpperCase() }))} placeholder="Mã IATA" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.icaoCode} onChange={(event) => setForm((current) => ({ ...current, icaoCode: event.target.value.toUpperCase() }))} placeholder="Mã ICAO" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.timeZone} onChange={(event) => setForm((current) => ({ ...current, timeZone: event.target.value }))} placeholder="Time zone" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.latitude} onChange={(event) => setForm((current) => ({ ...current, latitude: event.target.value }))} placeholder="Vĩ độ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.longitude} onChange={(event) => setForm((current) => ({ ...current, longitude: event.target.value }))} placeholder="Kinh độ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Cho phép dùng trong public search
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật sân bay' : 'Tạo sân bay')}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
