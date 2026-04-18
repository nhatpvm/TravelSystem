import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw, WandSparkles } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminManagedFlightCabinSeatMap,
  createManagedFlightCabinSeatMap,
  deleteAdminManagedFlightCabinSeatMap,
  deleteManagedFlightCabinSeatMap,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightCabinSeatMaps,
  listFlightCabinSeatMaps,
  regenerateAdminFlightCabinSeatMapSeats,
  regenerateFlightCabinSeatMapSeats,
  restoreAdminManagedFlightCabinSeatMap,
  restoreManagedFlightCabinSeatMap,
  updateAdminManagedFlightCabinSeatMap,
  updateManagedFlightCabinSeatMap,
} from '../../../../services/flightService';
import {
  CABIN_CLASS_OPTIONS,
  getCabinClassLabel,
  parseEnumOptionValue,
} from '../utils/presentation';

function createEmptyForm() {
  return {
    aircraftModelId: '',
    cabinClass: 1,
    code: '',
    name: '',
    totalRows: '20',
    totalColumns: '6',
    deckCount: '1',
    layoutVersion: '',
    seatLabelScheme: 'ABCDEF',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    aircraftModelId: item.aircraftModelId || '',
    cabinClass: parseEnumOptionValue(CABIN_CLASS_OPTIONS, item.cabinClass, 1),
    code: item.code || '',
    name: item.name || '',
    totalRows: String(item.totalRows ?? 0),
    totalColumns: String(item.totalColumns ?? 0),
    deckCount: String(item.deckCount ?? 1),
    layoutVersion: item.layoutVersion || '',
    seatLabelScheme: item.seatLabelScheme || '',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    aircraftModelId: form.aircraftModelId,
    cabinClass: Number(form.cabinClass),
    code: form.code.trim().toUpperCase(),
    name: form.name.trim(),
    totalRows: Number(form.totalRows || 0),
    totalColumns: Number(form.totalColumns || 0),
    deckCount: Number(form.deckCount || 1),
    layoutVersion: form.layoutVersion.trim() || null,
    seatLabelScheme: form.seatLabelScheme.trim().toUpperCase(),
    isActive: !!form.isActive,
  };
}

export default function FlightCabinSeatMapsPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [aircraftModels, setAircraftModels] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightCabinSeatMaps(params, tenantId) : listFlightCabinSeatMaps;
  const createFn = isAdmin ? (payload) => createAdminManagedFlightCabinSeatMap(payload, tenantId) : createManagedFlightCabinSeatMap;
  const updateFn = isAdmin ? (id, payload) => updateAdminManagedFlightCabinSeatMap(id, payload, tenantId) : updateManagedFlightCabinSeatMap;
  const deleteFn = isAdmin ? (id) => deleteAdminManagedFlightCabinSeatMap(id, tenantId) : deleteManagedFlightCabinSeatMap;
  const restoreFn = isAdmin ? (id) => restoreAdminManagedFlightCabinSeatMap(id, tenantId) : restoreManagedFlightCabinSeatMap;
  const regenerateFn = isAdmin ? (id, payload) => regenerateAdminFlightCabinSeatMapSeats(id, payload, tenantId) : regenerateFlightCabinSeatMapSeats;

  async function loadData() {
    if (isAdmin && !tenantId) {
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

      const nextModels = Array.isArray(optionsResponse?.aircraftModels) ? optionsResponse.aircraftModels : [];
      const seatMapOptions = Array.isArray(optionsResponse?.seatMaps) ? optionsResponse.seatMaps : [];
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      const mergedItems = nextItems.map((item) => ({
        ...item,
        seatCount: seatMapOptions.find((optionItem) => optionItem.id === item.id)?.seatCount || 0,
      }));

      setAircraftModels(nextModels);
      setItems(mergedItems);

      if (mergedItems.length > 0) {
        const selected = mergedItems.find((item) => item.id === selectedId) || mergedItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          aircraftModelId: nextModels[0]?.id || '',
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải sơ đồ cabin.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  const aircraftModelLookup = useMemo(
    () => Object.fromEntries(aircraftModels.map((item) => [item.id, item])),
    [aircraftModels],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      aircraftModelId: aircraftModels[0]?.id || '',
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
        setNotice('Đã cập nhật sơ đồ cabin.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo sơ đồ cabin mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được sơ đồ cabin.');
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
        setNotice('Đã khôi phục sơ đồ cabin.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn sơ đồ cabin.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái sơ đồ cabin.');
    }
  }

  async function handleRegenerateSeats() {
    if (!selectedId) {
      setError('Hãy chọn hoặc tạo một sơ đồ cabin trước khi sinh ghế.');
      return;
    }

    setGenerating(true);
    setError('');
    setNotice('');

    try {
      await regenerateFn(selectedId, {
        softDeleteMissing: true,
        seatLabelScheme: form.seatLabelScheme.trim().toUpperCase(),
        defaultSeatType: 'Standard',
        defaultSeatClass: getCabinClassLabel(form.cabinClass),
        defaultPriceModifier: 0,
      });
      setNotice('Đã sinh lại danh sách ghế cho sơ đồ cabin.');
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không sinh được danh sách ghế.');
    } finally {
      setGenerating(false);
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="seat-maps"
      title="Sơ đồ cabin"
      subtitle="Quản lý layout ghế theo mẫu tàu bay để public seat selection và manager operations dùng chung."
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
            Thêm sơ đồ
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="space-y-8">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-8 py-6 border-b border-slate-100">
              <p className="text-lg font-black text-slate-900">Danh sách sơ đồ cabin</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mỗi sơ đồ đại diện cho một cấu hình cabin theo mẫu tàu bay.</p>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải sơ đồ cabin...</div>
              ) : items.length === 0 ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có sơ đồ cabin nào.</div>
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
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                          {getCabinClassLabel(item.cabinClass)}
                        </span>
                        {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        {item.code} · {aircraftModelLookup[item.aircraftModelId]?.manufacturer || ''} {aircraftModelLookup[item.aircraftModelId]?.model || ''}
                      </p>
                      <p className="text-xs font-bold text-slate-400 mt-1">
                        {item.totalRows} hàng · {item.totalColumns} cột · {item.deckCount} tầng · {item.seatCount || 0} ghế
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

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-lg font-black text-slate-900">Sinh ghế tự động</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Tạo lại toàn bộ ghế dựa trên kích thước cabin và sơ đồ nhãn ghế.</p>
              </div>
              <button type="button" onClick={handleRegenerateSeats} disabled={generating || !selectedId} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2 disabled:opacity-60">
                <WandSparkles size={16} />
                {generating ? 'Đang sinh...' : 'Sinh ghế'}
              </button>
            </div>
            <p className="text-sm font-medium text-slate-500">
              Với sơ đồ nhãn <span className="font-black text-slate-700">{form.seatLabelScheme || 'ABCDEF'}</span>, hệ thống sẽ sinh các ghế như <span className="font-black text-slate-700">1A, 1B, 1C...</span>
            </p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-6">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật sơ đồ cabin' : 'Tạo sơ đồ cabin mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Dữ liệu này được dùng cho public seat map và trang quản lý ghế cabin.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.aircraftModelId} onChange={(event) => setForm((current) => ({ ...current, aircraftModelId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn mẫu tàu bay</option>
              {aircraftModels.map((item) => <option key={item.id} value={item.id}>{item.manufacturer} {item.model}</option>)}
            </select>
            <select value={form.cabinClass} onChange={(event) => setForm((current) => ({ ...current, cabinClass: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              {CABIN_CLASS_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã sơ đồ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên sơ đồ cabin" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="1" value={form.totalRows} onChange={(event) => setForm((current) => ({ ...current, totalRows: event.target.value }))} placeholder="Số hàng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="1" value={form.totalColumns} onChange={(event) => setForm((current) => ({ ...current, totalColumns: event.target.value }))} placeholder="Số cột" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="1" value={form.deckCount} onChange={(event) => setForm((current) => ({ ...current, deckCount: event.target.value }))} placeholder="Số tầng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.seatLabelScheme} onChange={(event) => setForm((current) => ({ ...current, seatLabelScheme: event.target.value.toUpperCase() }))} placeholder="Sơ đồ nhãn ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.layoutVersion} onChange={(event) => setForm((current) => ({ ...current, layoutVersion: event.target.value }))} placeholder="Phiên bản layout" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Cho phép dùng sơ đồ này trong các flow public
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật sơ đồ cabin' : 'Tạo sơ đồ cabin')}
            </button>
            {selectedId ? (
              <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">
                Tạo bản mới
              </button>
            ) : null}
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
