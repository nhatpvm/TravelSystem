import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminFlightOffer,
  createFlightOffer,
  deleteAdminFlightOffer,
  deleteFlightOffer,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightOffers,
  listFlightOffers,
  restoreAdminFlightOffer,
  restoreFlightOffer,
  updateAdminFlightOffer,
  updateFlightOffer,
} from '../../../../services/flightService';
import {
  formatCurrency,
  formatDateTime,
  getOfferStatusClass,
  getOfferStatusLabel,
  OFFER_STATUS_OPTIONS,
  parseEnumOptionValue,
  toApiDateTimeValue,
  toDateTimeInputValue,
} from '../utils/presentation';

function createEmptyForm() {
  return {
    airlineId: '',
    flightId: '',
    fareClassId: '',
    status: 1,
    currencyCode: 'VND',
    baseFare: '0',
    taxesFees: '0',
    totalPrice: '0',
    seatsAvailable: '9',
    requestedAt: '',
    expiresAt: '',
    conditionsJson: '{\n  "refundable": false,\n  "changeable": true\n}',
    metadataJson: '{\n  "channel": "marketplace"\n}',
  };
}

function hydrateForm(item) {
  const baseFare = Number(item.baseFare || 0);
  const taxesFees = Number(item.taxesFees || 0);
  const totalPrice = Number(item.totalPrice || baseFare + taxesFees);

  return {
    airlineId: item.airlineId || '',
    flightId: item.flightId || '',
    fareClassId: item.fareClassId || '',
    status: parseEnumOptionValue(OFFER_STATUS_OPTIONS, item.status, 1),
    currencyCode: item.currencyCode || 'VND',
    baseFare: String(baseFare),
    taxesFees: String(taxesFees),
    totalPrice: String(totalPrice),
    seatsAvailable: String(item.seatsAvailable ?? 0),
    requestedAt: toDateTimeInputValue(item.requestedAt),
    expiresAt: toDateTimeInputValue(item.expiresAt),
    conditionsJson: item.conditionsJson || '{}',
    metadataJson: item.metadataJson || '{}',
  };
}

function buildPayload(form) {
  const baseFare = Number(form.baseFare || 0);
  const taxesFees = Number(form.taxesFees || 0);

  return {
    airlineId: form.airlineId,
    flightId: form.flightId,
    fareClassId: form.fareClassId,
    status: Number(form.status),
    currencyCode: String(form.currencyCode || 'VND').trim().toUpperCase(),
    baseFare,
    taxesFees,
    totalPrice: baseFare + taxesFees,
    seatsAvailable: Number(form.seatsAvailable || 0),
    requestedAt: toApiDateTimeValue(form.requestedAt),
    expiresAt: toApiDateTimeValue(form.expiresAt),
    conditionsJson: form.conditionsJson?.trim() || null,
    metadataJson: form.metadataJson?.trim() || null,
  };
}

export default function FlightOffersPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [searchParams] = useSearchParams();
  const [options, setOptions] = useState({ airlines: [], flights: [], fareClasses: [] });
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedFlightId, setSelectedFlightId] = useState(searchParams.get('flightId') || '');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightOffers(params, tenantId) : listFlightOffers;
  const createFn = isAdmin ? (payload) => createAdminFlightOffer(payload, tenantId) : createFlightOffer;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightOffer(id, payload, tenantId) : updateFlightOffer;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightOffer(id, tenantId) : deleteFlightOffer;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightOffer(id, tenantId) : restoreFlightOffer;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setOptions({ airlines: [], flights: [], fareClasses: [] });
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
        listFn({ includeDeleted: true, pageSize: 100, flightId: selectedFlightId || undefined }),
      ]);

      const nextOptions = {
        airlines: Array.isArray(optionsResponse?.airlines) ? optionsResponse.airlines : [],
        flights: Array.isArray(optionsResponse?.flights) ? optionsResponse.flights : [],
        fareClasses: Array.isArray(optionsResponse?.fareClasses) ? optionsResponse.fareClasses : [],
      };
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setOptions(nextOptions);
      setItems(nextItems);

      if (!selectedFlightId && nextOptions.flights.length > 0) {
        setSelectedFlightId(nextOptions.flights[0].id);
      }

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        const selectedFlight = nextOptions.flights.find((item) => item.id === (selectedFlightId || nextOptions.flights[0]?.id)) || nextOptions.flights[0];
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          airlineId: selectedFlight?.airlineId || nextOptions.airlines[0]?.id || '',
          flightId: selectedFlight?.id || '',
          fareClassId: nextOptions.fareClasses.find((item) => item.airlineId === (selectedFlight?.airlineId || nextOptions.airlines[0]?.id))?.id || nextOptions.fareClasses[0]?.id || '',
          requestedAt: toDateTimeInputValue(new Date().toISOString()),
          expiresAt: toDateTimeInputValue(new Date(Date.now() + 1000 * 60 * 60 * 24 * 7).toISOString()),
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải offer chuyến bay.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId, selectedFlightId]);

  useEffect(() => {
    const selectedFlight = options.flights.find((item) => item.id === form.flightId);
    if (!selectedFlight) {
      return;
    }

    setForm((current) => {
      if (current.airlineId === selectedFlight.airlineId) {
        return current;
      }

      const nextFareClassId = options.fareClasses.find((item) => item.airlineId === selectedFlight.airlineId)?.id || current.fareClassId;
      return {
        ...current,
        airlineId: selectedFlight.airlineId || current.airlineId,
        fareClassId: nextFareClassId,
      };
    });
  }, [form.flightId, options.flights, options.fareClasses]);

  const flightLookup = useMemo(
    () => Object.fromEntries(options.flights.map((item) => [item.id, item])),
    [options.flights],
  );
  const fareClassLookup = useMemo(
    () => Object.fromEntries(options.fareClasses.map((item) => [item.id, item])),
    [options.fareClasses],
  );

  const availableFareClasses = useMemo(() => {
    const airlineId = form.airlineId || flightLookup[form.flightId]?.airlineId || '';
    return options.fareClasses.filter((item) => !airlineId || item.airlineId === airlineId);
  }, [form.airlineId, form.flightId, flightLookup, options.fareClasses]);

  function handleCreateNew() {
    const selectedFlight = options.flights.find((item) => item.id === (selectedFlightId || options.flights[0]?.id)) || options.flights[0];
    const selectedAirlineId = selectedFlight?.airlineId || options.airlines[0]?.id || '';

    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      airlineId: selectedAirlineId,
      flightId: selectedFlight?.id || '',
      fareClassId: options.fareClasses.find((item) => item.airlineId === selectedAirlineId)?.id || options.fareClasses[0]?.id || '',
      requestedAt: toDateTimeInputValue(new Date().toISOString()),
      expiresAt: toDateTimeInputValue(new Date(Date.now() + 1000 * 60 * 60 * 24 * 7).toISOString()),
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
        setNotice('Đã cập nhật offer chuyến bay.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo offer chuyến bay mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được offer chuyến bay.');
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
        setNotice('Đã khôi phục offer chuyến bay.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn offer chuyến bay.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái offer chuyến bay.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="offers"
      title="Offer chuyến bay"
      subtitle="Offer là bản chào bán cuối cùng để khách hàng thấy giá, số chỗ còn lại, điều kiện vé và thời gian hiệu lực."
      notice={notice}
      error={error}
      actions={(
        <>
          <div className="px-4 py-3 rounded-2xl border border-slate-200 bg-white">
            <select
              value={selectedFlightId}
              onChange={(event) => setSelectedFlightId(event.target.value)}
              className="bg-transparent text-sm font-bold text-slate-700 outline-none"
            >
              {options.flights.map((item) => (
                <option key={item.id} value={item.id}>{item.flightNumber}</option>
              ))}
            </select>
          </div>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm offer
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách offer</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Theo dõi bản chào bán theo từng chuyến bay và hạng vé.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải offer...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có offer nào.</div>
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
                      <p className="font-black text-slate-900">{flightLookup[item.flightId]?.flightNumber || 'Offer'}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getOfferStatusClass(item.status)}`}>
                        {getOfferStatusLabel(item.status)}
                      </span>
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {fareClassLookup[item.fareClassId]?.name || 'Hạng vé chưa xác định'} · {formatCurrency(item.totalPrice, item.currencyCode)}
                    </p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {item.seatsAvailable || 0} chỗ còn lại · Hết hạn: {formatDateTime(item.expiresAt)}
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật offer' : 'Tạo offer mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Giá tổng được tính tự động bằng giá cơ bản cộng thuế và phí.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.flightId} onChange={(event) => setForm((current) => ({ ...current, flightId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn chuyến bay</option>
              {options.flights.map((item) => <option key={item.id} value={item.id}>{item.flightNumber}</option>)}
            </select>
            <select value={form.fareClassId} onChange={(event) => setForm((current) => ({ ...current, fareClassId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn hạng vé</option>
              {availableFareClasses.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              {OFFER_STATUS_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <input value={form.currencyCode} onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))} placeholder="Mã tiền tệ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="0" step="1000" value={form.baseFare} onChange={(event) => setForm((current) => ({ ...current, baseFare: event.target.value, totalPrice: String(Number(event.target.value || 0) + Number(current.taxesFees || 0)) }))} placeholder="Giá cơ bản" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="0" step="1000" value={form.taxesFees} onChange={(event) => setForm((current) => ({ ...current, taxesFees: event.target.value, totalPrice: String(Number(current.baseFare || 0) + Number(event.target.value || 0)) }))} placeholder="Thuế và phí" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={formatCurrency(form.totalPrice, form.currencyCode)} disabled className="w-full rounded-2xl border border-slate-100 bg-slate-100 px-5 py-4 text-sm font-black text-slate-700 outline-none" />
            <input type="number" min="0" value={form.seatsAvailable} onChange={(event) => setForm((current) => ({ ...current, seatsAvailable: event.target.value }))} placeholder="Số chỗ còn lại" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="datetime-local" value={form.requestedAt} onChange={(event) => setForm((current) => ({ ...current, requestedAt: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="datetime-local" value={form.expiresAt} onChange={(event) => setForm((current) => ({ ...current, expiresAt: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <textarea value={form.conditionsJson} onChange={(event) => setForm((current) => ({ ...current, conditionsJson: event.target.value }))} rows={6} placeholder="Conditions JSON" className="md:col-span-2 w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none font-mono" />
            <textarea value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} rows={6} placeholder="Metadata JSON" className="md:col-span-2 w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none font-mono" />
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật offer' : 'Tạo offer')}
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
