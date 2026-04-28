import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminFlight,
  createFlight,
  deleteAdminFlight,
  deleteFlight,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlights,
  listFlights,
  restoreAdminFlight,
  restoreFlight,
  updateAdminFlight,
  updateFlight,
} from '../../../../services/flightService';
import {
  FLIGHT_STATUS_OPTIONS,
  formatDateTime,
  getFlightStatusClass,
  getFlightStatusLabel,
  parseEnumOptionValue,
  toApiDateTimeValue,
  toDateTimeInputValue,
} from '../utils/presentation';

function createEmptyForm() {
  return {
    airlineId: '',
    aircraftId: '',
    fromAirportId: '',
    toAirportId: '',
    flightNumber: '',
    departureAt: '',
    arrivalAt: '',
    status: 2,
    notes: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    airlineId: item.airlineId || '',
    aircraftId: item.aircraftId || '',
    fromAirportId: item.fromAirportId || '',
    toAirportId: item.toAirportId || '',
    flightNumber: item.flightNumber || '',
    departureAt: toDateTimeInputValue(item.departureAt),
    arrivalAt: toDateTimeInputValue(item.arrivalAt),
    status: parseEnumOptionValue(FLIGHT_STATUS_OPTIONS, item.status, 2),
    notes: item.notes || '',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    airlineId: form.airlineId,
    aircraftId: form.aircraftId,
    fromAirportId: form.fromAirportId,
    toAirportId: form.toAirportId,
    flightNumber: form.flightNumber.trim().toUpperCase(),
    departureAt: toApiDateTimeValue(form.departureAt),
    arrivalAt: toApiDateTimeValue(form.arrivalAt),
    status: Number(form.status),
    notes: form.notes.trim() || null,
    isActive: !!form.isActive,
  };
}

export default function FlightFlightsPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [options, setOptions] = useState({ airlines: [], aircrafts: [], airports: [] });
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlights(params, tenantId) : listFlights;
  const createFn = isAdmin ? (payload) => createAdminFlight(payload, tenantId) : createFlight;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlight(id, payload, tenantId) : updateFlight;
  const deleteFn = isAdmin ? (id) => deleteAdminFlight(id, tenantId) : deleteFlight;
  const restoreFn = isAdmin ? (id) => restoreAdminFlight(id, tenantId) : restoreFlight;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setOptions({ airlines: [], aircrafts: [], airports: [] });
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

      const nextOptions = {
        airlines: Array.isArray(optionsResponse?.airlines) ? optionsResponse.airlines : [],
        aircrafts: Array.isArray(optionsResponse?.aircrafts) ? optionsResponse.aircrafts : [],
        airports: Array.isArray(optionsResponse?.airports) ? optionsResponse.airports : [],
      };
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setOptions(nextOptions);
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          airlineId: nextOptions.airlines[0]?.id || '',
          aircraftId: nextOptions.aircrafts[0]?.id || '',
          fromAirportId: nextOptions.airports[0]?.id || '',
          toAirportId: nextOptions.airports[1]?.id || nextOptions.airports[0]?.id || '',
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải lịch bay.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, loadDataRef, tenantId]);

  const airlineLookup = useMemo(
    () => Object.fromEntries(options.airlines.map((item) => [item.id, item])),
    [options.airlines],
  );
  const aircraftLookup = useMemo(
    () => Object.fromEntries(options.aircrafts.map((item) => [item.id, item])),
    [options.aircrafts],
  );
  const airportLookup = useMemo(
    () => Object.fromEntries(options.airports.map((item) => [item.id, item])),
    [options.airports],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      airlineId: options.airlines[0]?.id || '',
      aircraftId: options.aircrafts[0]?.id || '',
      fromAirportId: options.airports[0]?.id || '',
      toAirportId: options.airports[1]?.id || options.airports[0]?.id || '',
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
        setNotice('Đã cập nhật lịch bay.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo lịch bay mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được lịch bay.');
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
        setNotice('Đã khôi phục lịch bay.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn lịch bay.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái lịch bay.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="flights"
      title="Lịch bay"
      subtitle="Đây là nơi đối tác cấu hình chuyến bay thực tế trước khi mở bán fare class và offer trên sàn."
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
            Thêm lịch bay
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách lịch bay</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Theo dõi giờ cất cánh, hạ cánh, tàu bay và sân bay đi đến của từng chuyến.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải lịch bay...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có lịch bay nào.</div>
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
                      <p className="font-black text-slate-900">{item.flightNumber}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getFlightStatusClass(item.status)}`}>
                        {getFlightStatusLabel(item.status)}
                      </span>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng hiển thị</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {(airportLookup[item.fromAirportId]?.iataCode || airportLookup[item.fromAirportId]?.code || '---')} → {(airportLookup[item.toAirportId]?.iataCode || airportLookup[item.toAirportId]?.code || '---')}
                    </p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {formatDateTime(item.departureAt)} · {airlineLookup[item.airlineId]?.name || 'Chưa gắn hãng'} · {aircraftLookup[item.aircraftId]?.registration || aircraftLookup[item.aircraftId]?.code || 'Chưa gắn tàu bay'}
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật lịch bay' : 'Tạo lịch bay mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Lịch bay là dữ liệu gốc để tenant mở bán offer cho khách hàng cuối trên marketplace.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.airlineId} onChange={(event) => setForm((current) => ({ ...current, airlineId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn hãng bay</option>
              {options.airlines.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <select value={form.aircraftId} onChange={(event) => setForm((current) => ({ ...current, aircraftId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn tàu bay</option>
              {options.aircrafts.map((item) => <option key={item.id} value={item.id}>{item.registration || item.code}</option>)}
            </select>
            <select value={form.fromAirportId} onChange={(event) => setForm((current) => ({ ...current, fromAirportId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Sân bay đi</option>
              {options.airports.map((item) => <option key={item.id} value={item.id}>{item.iataCode || item.code} - {item.name}</option>)}
            </select>
            <select value={form.toAirportId} onChange={(event) => setForm((current) => ({ ...current, toAirportId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Sân bay đến</option>
              {options.airports.map((item) => <option key={item.id} value={item.id}>{item.iataCode || item.code} - {item.name}</option>)}
            </select>
            <input value={form.flightNumber} onChange={(event) => setForm((current) => ({ ...current, flightNumber: event.target.value.toUpperCase() }))} placeholder="Số hiệu chuyến bay" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              {FLIGHT_STATUS_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <input type="datetime-local" value={form.departureAt} onChange={(event) => setForm((current) => ({ ...current, departureAt: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="datetime-local" value={form.arrivalAt} onChange={(event) => setForm((current) => ({ ...current, arrivalAt: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <textarea value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} rows={4} placeholder="Ghi chú vận hành" className="md:col-span-2 w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Đưa chuyến bay vào khai thác hiển thị
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật lịch bay' : 'Tạo lịch bay')}
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
