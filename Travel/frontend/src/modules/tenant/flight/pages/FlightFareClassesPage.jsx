import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminFlightFareClass,
  createFlightFareClass,
  deleteAdminFlightFareClass,
  deleteFlightFareClass,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightFareClasses,
  listFlightFareClasses,
  restoreAdminFlightFareClass,
  restoreFlightFareClass,
  updateAdminFlightFareClass,
  updateFlightFareClass,
} from '../../../../services/flightService';
import { CABIN_CLASS_OPTIONS, getCabinClassLabel, parseEnumOptionValue } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

function createEmptyForm() {
  return {
    airlineId: '',
    code: '',
    name: '',
    cabinClass: 1,
    isRefundable: false,
    isChangeable: false,
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    airlineId: item.airlineId || '',
    code: item.code || '',
    name: item.name || '',
    cabinClass: parseEnumOptionValue(CABIN_CLASS_OPTIONS, item.cabinClass, 1),
    isRefundable: item.isRefundable ?? false,
    isChangeable: item.isChangeable ?? false,
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    airlineId: form.airlineId,
    code: form.code.trim(),
    name: form.name.trim(),
    cabinClass: Number(form.cabinClass),
    isRefundable: !!form.isRefundable,
    isChangeable: !!form.isChangeable,
    isActive: !!form.isActive,
  };
}

export default function FlightFareClassesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [airlines, setAirlines] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightFareClasses(params, tenantId) : listFlightFareClasses;
  const createFn = isAdmin ? (payload) => createAdminFlightFareClass(payload, tenantId) : createFlightFareClass;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightFareClass(id, payload, tenantId) : updateFlightFareClass;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightFareClass(id, tenantId) : deleteFlightFareClass;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightFareClass(id, tenantId) : restoreFlightFareClass;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setAirlines([]);
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
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setAirlines(nextAirlines);
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
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải hạng vé.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, loadDataRef, tenantId]);

  const airlineLookup = useMemo(
    () => Object.fromEntries(airlines.map((item) => [item.id, item])),
    [airlines],
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
        setNotice('Đã cập nhật hạng vé.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo hạng vé mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được hạng vé.');
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
        setNotice('Đã khôi phục hạng vé.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn hạng vé.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái hạng vé.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      airlineId: airlines[0]?.id || '',
    });
    setNotice('');
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="fare-classes"
      title="Hạng vé"
      subtitle="Khởi tạo hạng vé chuẩn để mở bán offer cho từng chuyến bay."
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
            Thêm hạng vé
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách hạng vé</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Mỗi hạng vé gắn với một hãng bay và cabin class.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải hạng vé...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có hạng vé nào.</div>
            ) : items.map((item) => (
              <div key={item.id} role="button" tabIndex={0} onClick={() => { setSelectedId(item.id); setForm(hydrateForm(item)); }} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); setForm(hydrateForm(item)); } }} className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.name}</p>
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">{getCabinClassLabel(item.cabinClass)}</span>
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.code} · {airlineLookup[item.airlineId]?.name || 'Chưa gắn hãng bay'}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.isRefundable ? 'Hoàn được' : 'Không hoàn'} · {item.isChangeable ? 'Đổi được' : 'Không đổi'}</p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật hạng vé' : 'Tạo hạng vé mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Hạng vé ảnh hưởng trực tiếp tới public detail, refundability và pricing của offer.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.airlineId} onChange={(event) => setForm((current) => ({ ...current, airlineId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn hãng bay</option>
              {airlines.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Code" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên hạng vé" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <select value={form.cabinClass} onChange={(event) => setForm((current) => ({ ...current, cabinClass: Number(event.target.value) }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              {CABIN_CLASS_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
          </div>
          <div className="flex flex-wrap gap-6 text-sm font-bold text-slate-600">
            <label className="flex items-center gap-3">
              <input type="checkbox" checked={form.isRefundable} onChange={(event) => setForm((current) => ({ ...current, isRefundable: event.target.checked }))} />
              Cho phép hoàn vé
            </label>
            <label className="flex items-center gap-3">
              <input type="checkbox" checked={form.isChangeable} onChange={(event) => setForm((current) => ({ ...current, isChangeable: event.target.checked }))} />
              Cho phép đổi vé
            </label>
            <label className="flex items-center gap-3">
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
              Kích hoạt
            </label>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật hạng vé' : 'Tạo hạng vé')}
            </button>
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
