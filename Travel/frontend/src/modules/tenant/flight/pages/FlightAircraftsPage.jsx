import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminFlightAircraft,
  createFlightAircraft,
  deleteAdminFlightAircraft,
  deleteFlightAircraft,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightAircrafts,
  listFlightAircrafts,
  restoreAdminFlightAircraft,
  restoreFlightAircraft,
  updateAdminFlightAircraft,
  updateFlightAircraft,
} from '../../../../services/flightService';

function createEmptyForm() {
  return {
    aircraftModelId: '',
    airlineId: '',
    code: '',
    registration: '',
    name: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    aircraftModelId: item.aircraftModelId || '',
    airlineId: item.airlineId || '',
    code: item.code || '',
    registration: item.registration || '',
    name: item.name || '',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    aircraftModelId: form.aircraftModelId,
    airlineId: form.airlineId,
    code: form.code.trim(),
    registration: form.registration.trim() || null,
    name: form.name.trim() || null,
    isActive: !!form.isActive,
  };
}

export default function FlightAircraftsPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [airlines, setAirlines] = useState([]);
  const [aircraftModels, setAircraftModels] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightAircrafts(params, tenantId) : listFlightAircrafts;
  const createFn = isAdmin ? (payload) => createAdminFlightAircraft(payload, tenantId) : createFlightAircraft;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightAircraft(id, payload, tenantId) : updateFlightAircraft;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightAircraft(id, tenantId) : deleteFlightAircraft;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightAircraft(id, tenantId) : restoreFlightAircraft;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setAirlines([]);
      setAircraftModels([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, itemsResponse] = await Promise.all([
        isAdmin ? getAdminFlightOptions(tenantId) : getFlightManagerOptions(),
        listFn({ includeDeleted: true, pageSize: 100 }),
      ]);

      const nextAirlines = Array.isArray(optionsResponse?.airlines) ? optionsResponse.airlines : [];
      const nextAircraftModels = Array.isArray(optionsResponse?.aircraftModels) ? optionsResponse.aircraftModels : [];
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setAirlines(nextAirlines);
      setAircraftModels(nextAircraftModels);
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          airlineId: nextAirlines[0]?.id || '',
          aircraftModelId: nextAircraftModels[0]?.id || '',
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải đội tàu bay.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  const airlineLookup = useMemo(
    () => Object.fromEntries(airlines.map((item) => [item.id, item])),
    [airlines],
  );
  const aircraftModelLookup = useMemo(
    () => Object.fromEntries(aircraftModels.map((item) => [item.id, item])),
    [aircraftModels],
  );

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);
      if (selectedId) {
        await updateFn(selectedId, payload);
        setNotice('Đã cập nhật tàu bay.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo tàu bay mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được tàu bay.');
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
        setNotice('Đã khôi phục tàu bay.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn tàu bay.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái tàu bay.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      airlineId: airlines[0]?.id || '',
      aircraftModelId: aircraftModels[0]?.id || '',
    });
    setNotice('');
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="aircrafts"
      title="Tàu bay"
      subtitle="Liên kết giữa hãng bay và mẫu tàu bay để tạo lịch bay, offer và seat map dùng chung."
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
            Thêm tàu bay
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách tàu bay</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Theo dõi registration và model đang khai thác.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải tàu bay...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có tàu bay nào.</div>
            ) : items.map((item) => (
              <div key={item.id} role="button" tabIndex={0} onClick={() => { setSelectedId(item.id); setForm(hydrateForm(item)); }} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); setForm(hydrateForm(item)); } }} className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.registration || item.code}</p>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.name || 'Chưa đặt tên tàu bay'}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {airlineLookup[item.airlineId]?.name || 'Chưa gắn hãng bay'} · {aircraftModelLookup[item.aircraftModelId]?.manufacturer || ''} {aircraftModelLookup[item.aircraftModelId]?.model || ''}
                    </p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật tàu bay' : 'Tạo tàu bay mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Mỗi tàu bay dùng để gắn lịch bay thực tế và sơ đồ cabin tương ứng.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.airlineId} onChange={(event) => setForm((current) => ({ ...current, airlineId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn hãng bay</option>
              {airlines.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <select value={form.aircraftModelId} onChange={(event) => setForm((current) => ({ ...current, aircraftModelId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn mẫu tàu bay</option>
              {aircraftModels.map((item) => <option key={item.id} value={item.id}>{item.manufacturer} {item.model}</option>)}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã tàu bay" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.registration} onChange={(event) => setForm((current) => ({ ...current, registration: event.target.value.toUpperCase() }))} placeholder="Registration" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên nội bộ tàu bay" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Đưa vào khai thác
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật tàu bay' : 'Tạo tàu bay')}
            </button>
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
