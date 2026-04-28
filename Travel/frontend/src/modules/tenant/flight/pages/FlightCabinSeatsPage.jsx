import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminManagedFlightCabinSeat,
  createManagedFlightCabinSeat,
  deleteAdminManagedFlightCabinSeat,
  deleteManagedFlightCabinSeat,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightCabinSeats,
  listFlightCabinSeats,
  restoreAdminManagedFlightCabinSeat,
  restoreManagedFlightCabinSeat,
  updateAdminManagedFlightCabinSeat,
  updateManagedFlightCabinSeat,
} from '../../../../services/flightService';
import { getCabinClassLabel } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

function createEmptyForm() {
  return {
    cabinSeatMapId: '',
    seatNumber: '',
    rowIndex: '1',
    columnIndex: '1',
    deckIndex: '1',
    isWindow: false,
    isAisle: false,
    seatType: 'Standard',
    seatClass: 'Standard',
    priceModifier: '0',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    cabinSeatMapId: item.cabinSeatMapId || '',
    seatNumber: item.seatNumber || '',
    rowIndex: String(item.rowIndex ?? 1),
    columnIndex: String(item.columnIndex ?? 1),
    deckIndex: String(item.deckIndex ?? 1),
    isWindow: item.isWindow ?? false,
    isAisle: item.isAisle ?? false,
    seatType: item.seatType || 'Standard',
    seatClass: item.seatClass || 'Standard',
    priceModifier: String(item.priceModifier ?? 0),
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    cabinSeatMapId: form.cabinSeatMapId,
    seatNumber: form.seatNumber.trim().toUpperCase(),
    rowIndex: Number(form.rowIndex || 0),
    columnIndex: Number(form.columnIndex || 0),
    deckIndex: Number(form.deckIndex || 1),
    isWindow: !!form.isWindow,
    isAisle: !!form.isAisle,
    seatType: form.seatType.trim() || null,
    seatClass: form.seatClass.trim() || null,
    priceModifier: form.priceModifier === '' ? null : Number(form.priceModifier),
    isActive: !!form.isActive,
  };
}

export default function FlightCabinSeatsPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [searchParams] = useSearchParams();
  const [seatMaps, setSeatMaps] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedSeatMapId, setSelectedSeatMapId] = useState(searchParams.get('cabinSeatMapId') || '');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightCabinSeats(params, tenantId) : listFlightCabinSeats;
  const createFn = isAdmin ? (payload) => createAdminManagedFlightCabinSeat(payload, tenantId) : createManagedFlightCabinSeat;
  const updateFn = isAdmin ? (id, payload) => updateAdminManagedFlightCabinSeat(id, payload, tenantId) : updateManagedFlightCabinSeat;
  const deleteFn = isAdmin ? (id) => deleteAdminManagedFlightCabinSeat(id, tenantId) : deleteManagedFlightCabinSeat;
  const restoreFn = isAdmin ? (id) => restoreAdminManagedFlightCabinSeat(id, tenantId) : restoreManagedFlightCabinSeat;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setSeatMaps([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const optionsResponse = isAdmin ? await getAdminFlightOptions(tenantId) : await getFlightManagerOptions();
      const nextSeatMaps = Array.isArray(optionsResponse?.seatMaps) ? optionsResponse.seatMaps : [];
      setSeatMaps(nextSeatMaps);

      const effectiveSeatMapId = selectedSeatMapId || nextSeatMaps[0]?.id || '';
      if (!selectedSeatMapId && effectiveSeatMapId) {
        setSelectedSeatMapId(effectiveSeatMapId);
      }

      if (!effectiveSeatMapId) {
        setItems([]);
        setSelectedId('');
        setForm(createEmptyForm());
        return;
      }

      const itemsResponse = await listFn({ includeDeleted: true, cabinSeatMapId: effectiveSeatMapId, pageSize: 200 });
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          cabinSeatMapId: effectiveSeatMapId,
          seatClass: getCabinClassLabel(nextSeatMaps.find((item) => item.id === effectiveSeatMapId)?.cabinClass),
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải ghế cabin.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, tenantId, selectedSeatMapId, loadDataRef]);

  const seatMapLookup = useMemo(
    () => Object.fromEntries(seatMaps.map((item) => [item.id, item])),
    [seatMaps],
  );

  function handleCreateNew() {
    const currentMap = seatMapLookup[selectedSeatMapId] || seatMaps[0];
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      cabinSeatMapId: selectedSeatMapId || seatMaps[0]?.id || '',
      seatClass: getCabinClassLabel(currentMap?.cabinClass),
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
        setNotice('Đã cập nhật ghế cabin.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo ghế cabin mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được ghế cabin.');
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
        setNotice('Đã khôi phục ghế cabin.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn ghế cabin.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái ghế cabin.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="seats"
      title="Ghế cabin"
      subtitle="Chi tiết từng ghế theo cabin seat map, dùng cho public seat selection và cấu hình chênh lệch giá."
      notice={notice}
      error={error}
      actions={(
        <>
          <div className="px-4 py-3 rounded-2xl border border-slate-200 bg-white">
            <select
              value={selectedSeatMapId}
              onChange={(event) => setSelectedSeatMapId(event.target.value)}
              className="bg-transparent text-sm font-bold text-slate-700 outline-none"
            >
              {seatMaps.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm ghế
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách ghế cabin</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Ghế được nhóm theo sơ đồ cabin đang chọn và có thể tinh chỉnh riêng từng ghế.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải ghế cabin...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có ghế nào cho sơ đồ cabin này.</div>
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
                      <p className="font-black text-slate-900">{item.seatNumber}</p>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Không bán</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      Hàng {item.rowIndex} · Cột {item.columnIndex} · Tầng {item.deckIndex}
                    </p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {item.seatType || 'Standard'} · {item.seatClass || 'Standard'} · Phụ thu: {item.priceModifier || 0}
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật ghế cabin' : 'Tạo ghế cabin mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Có thể dùng để chỉnh phụ thu cho ghế đẹp, ghế cửa sổ hoặc ghế lối đi.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.cabinSeatMapId} onChange={(event) => setForm((current) => ({ ...current, cabinSeatMapId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn sơ đồ cabin</option>
              {seatMaps.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <input value={form.seatNumber} onChange={(event) => setForm((current) => ({ ...current, seatNumber: event.target.value.toUpperCase() }))} placeholder="Số ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.seatType} onChange={(event) => setForm((current) => ({ ...current, seatType: event.target.value }))} placeholder="Loại ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="1" value={form.rowIndex} onChange={(event) => setForm((current) => ({ ...current, rowIndex: event.target.value }))} placeholder="Hàng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="1" value={form.columnIndex} onChange={(event) => setForm((current) => ({ ...current, columnIndex: event.target.value }))} placeholder="Cột" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="1" value={form.deckIndex} onChange={(event) => setForm((current) => ({ ...current, deckIndex: event.target.value }))} placeholder="Tầng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.seatClass} onChange={(event) => setForm((current) => ({ ...current, seatClass: event.target.value }))} placeholder="Hạng ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" step="1000" value={form.priceModifier} onChange={(event) => setForm((current) => ({ ...current, priceModifier: event.target.value }))} placeholder="Phụ thu" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <div className="flex flex-wrap gap-6 text-sm font-bold text-slate-600">
            <label className="flex items-center gap-3">
              <input type="checkbox" checked={form.isWindow} onChange={(event) => setForm((current) => ({ ...current, isWindow: event.target.checked }))} />
              Ghế cửa sổ
            </label>
            <label className="flex items-center gap-3">
              <input type="checkbox" checked={form.isAisle} onChange={(event) => setForm((current) => ({ ...current, isAisle: event.target.checked }))} />
              Ghế lối đi
            </label>
            <label className="flex items-center gap-3">
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
              Được bán
            </label>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật ghế cabin' : 'Tạo ghế cabin')}
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
