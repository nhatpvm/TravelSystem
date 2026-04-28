import React, { useEffect, useMemo, useState } from 'react';
import { CircleDollarSign, Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import AdminTrainPageShell from '../train/components/AdminTrainPageShell';
import useAdminTrainScope from '../train/hooks/useAdminTrainScope';
import { formatCurrency } from '../../tenant/train/utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  createAdminTrainTripSegmentPrice,
  deleteAdminTrainTripSegmentPrice,
  generateAdminTrainTripSegmentPrices,
  listAdminTrainTrips,
  listAdminTrainTripSegmentPrices,
  listAdminTrainTripStopTimes,
  restoreAdminTrainTripSegmentPrice,
  updateAdminTrainTripSegmentPrice,
} from '../../../services/trainService';

function createEmptyForm() {
  return {
    fromTripStopTimeId: '',
    toTripStopTimeId: '',
    currencyCode: 'VND',
    baseFare: 0,
    taxesFees: 0,
    totalPrice: 0,
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    fromTripStopTimeId: item.fromTripStopTimeId || '',
    toTripStopTimeId: item.toTripStopTimeId || '',
    currencyCode: item.currencyCode || 'VND',
    baseFare: item.baseFare ?? 0,
    taxesFees: item.taxesFees ?? 0,
    totalPrice: item.totalPrice ?? 0,
    isActive: item.isActive ?? true,
  };
}

function getStopLabel(stopTimeLookup, id) {
  const item = stopTimeLookup[id];
  if (!item) {
    return 'Chưa chọn ga';
  }

  return `Ga #${item.stopIndex} - ${item.stopPoint?.name || item.stopPointName || 'Ga dừng'}`;
}

export default function AdminTrainTripSegmentPricesPage() {
  const [searchParams] = useSearchParams();
  const { tenantId, tenants, selectedTenantId, setSelectedTenantId, selectedTenant, scopeError } = useAdminTrainScope();
  const [trips, setTrips] = useState([]);
  const [stopTimes, setStopTimes] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadItemsRef = useLatestRef(loadItems);

  useEffect(() => {
    let active = true;

    async function loadTrips() {
      if (!tenantId) {
        setTrips([]);
        return;
      }

      try {
        const response = await listAdminTrainTrips({ includeDeleted: true }, tenantId);
        if (!active) return;

        const nextTrips = Array.isArray(response?.items) ? response.items.filter((item) => !item.isDeleted) : [];
        setTrips(nextTrips);
        setSelectedTripId((current) => current || searchParams.get('tripId') || nextTrips[0]?.id || '');
      } catch (requestError) {
        if (active) {
          setError(requestError.message || 'Không tải được danh sách chuyến tàu.');
        }
      }
    }

    loadTrips();
    return () => { active = false; };
  }, [searchParams, tenantId]);

  useEffect(() => {
    if (!selectedTripId || !tenantId) {
      setStopTimes([]);
      return;
    }

    let active = true;

    listAdminTrainTripStopTimes(selectedTripId, {}, tenantId)
      .then((response) => {
        if (!active) return;

        const nextStopTimes = Array.isArray(response?.items) ? response.items : [];
        setStopTimes(nextStopTimes);
        setForm((current) => ({
          ...current,
          fromTripStopTimeId: current.fromTripStopTimeId || nextStopTimes[0]?.id || '',
          toTripStopTimeId: current.toTripStopTimeId || nextStopTimes[1]?.id || nextStopTimes[0]?.id || '',
        }));
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không tải được lịch dừng của chuyến tàu.');
        }
      });

    return () => { active = false; };
  }, [tenantId, selectedTripId]);

  const stopTimeLookup = useMemo(() => Object.fromEntries(stopTimes.map((item) => [item.id, item])), [stopTimes]);

  async function loadItems() {
    if (!selectedTripId || !tenantId) {
      setItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listAdminTrainTripSegmentPrices(selectedTripId, { includeDeleted: true }, tenantId);
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
      setError(requestError.message || 'Không tải được giá chặng tàu.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadItemsRef.current();
  }, [tenantId, selectedTripId, loadItemsRef]);

  useEffect(() => {
    setForm((current) => ({
      ...current,
      totalPrice: Number(current.baseFare || 0) + Number(current.taxesFees || 0),
    }));
  }, [form.baseFare, form.taxesFees]);

  async function handleSubmit(event) {
    event.preventDefault();
    if (!tenantId || !selectedTripId) return;

    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = {
        tripId: selectedTripId,
        fromTripStopTimeId: form.fromTripStopTimeId,
        toTripStopTimeId: form.toTripStopTimeId,
        currencyCode: form.currencyCode || 'VND',
        baseFare: Number(form.baseFare || 0),
        taxesFees: Number(form.taxesFees || 0),
        totalPrice: Number(form.totalPrice || 0),
        isActive: !!form.isActive,
      };

      if (selectedId) {
        await updateAdminTrainTripSegmentPrice(selectedId, payload, tenantId);
        setNotice('Đã cập nhật giá chặng.');
      } else {
        await createAdminTrainTripSegmentPrice(payload, tenantId);
        setNotice('Đã tạo giá chặng mới.');
      }

      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được giá chặng.');
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerate() {
    if (!selectedTripId || !tenantId) return;

    setError('');
    setNotice('');

    try {
      await generateAdminTrainTripSegmentPrices({
        tripId: selectedTripId,
        currencyCode: 'VND',
        defaultBaseFare: 350000,
        defaultTaxesFees: 0,
        isActive: true,
      }, tenantId);

      setNotice('Đã sinh ma trận giá mặc định cho tất cả chặng.');
      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không sinh được ma trận giá chặng.');
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) return;

    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreAdminTrainTripSegmentPrice(item.id, tenantId);
        setNotice('Đã khôi phục giá chặng.');
      } else {
        await deleteAdminTrainTripSegmentPrice(item.id, tenantId);
        setNotice('Đã ẩn giá chặng.');
      }

      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái giá chặng.');
    }
  }

  return (
    <AdminTrainPageShell
      pageKey="trip-segment-prices"
      title="Giá chặng tàu"
      subtitle="Admin rà giá theo từng cặp ga để public search và giữ chỗ luôn tính đúng tiền."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <>
          <button type="button" onClick={loadItems} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={() => { setSelectedId(''); setForm(createEmptyForm()); }} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm giá chặng
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách giá chặng</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mỗi dòng tương ứng một cặp ga đi - ga đến trong cùng một chuyến tàu.</p>
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
                Sinh ma trận giá
              </button>
            </div>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải giá chặng...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chuyến tàu này chưa có giá chặng nào.</div>
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
                      <CircleDollarSign size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{getStopLabel(stopTimeLookup, item.fromTripStopTimeId)} → {getStopLabel(stopTimeLookup, item.toTripStopTimeId)}</p>
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        Cơ bản: {formatCurrency(item.baseFare, item.currencyCode)} • Thuế phí: {formatCurrency(item.taxesFees, item.currencyCode)} • Tổng: {formatCurrency(item.totalPrice, item.currencyCode)}
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
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật giá chặng' : 'Tạo giá chặng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Giá được lưu theo từng cặp ga để khớp logic marketplace.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga đi</span>
              <select value={form.fromTripStopTimeId} onChange={(event) => setForm((current) => ({ ...current, fromTripStopTimeId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
                <option value="">Chọn ga đi</option>
                {stopTimes.map((item) => (
                  <option key={item.id} value={item.id}>{getStopLabel(stopTimeLookup, item.id)}</option>
                ))}
              </select>
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga đến</span>
              <select value={form.toTripStopTimeId} onChange={(event) => setForm((current) => ({ ...current, toTripStopTimeId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
                <option value="">Chọn ga đến</option>
                {stopTimes.map((item) => (
                  <option key={item.id} value={item.id}>{getStopLabel(stopTimeLookup, item.id)}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tiền tệ</span>
              <input value={form.currencyCode} onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giá cơ bản</span>
              <input type="number" value={form.baseFare} onChange={(event) => setForm((current) => ({ ...current, baseFare: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thuế phí</span>
              <input type="number" value={form.taxesFees} onChange={(event) => setForm((current) => ({ ...current, taxesFees: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tổng tiền</span>
            <input type="number" value={form.totalPrice} onChange={(event) => setForm((current) => ({ ...current, totalPrice: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
          </label>

          <label className="flex items-center gap-3 pt-1">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" />
            <span className="text-sm font-bold text-slate-700">Cho phép hoạt động</span>
          </label>

          <button type="submit" disabled={saving || !tenantId || !selectedTripId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu cập nhật' : 'Tạo giá chặng'}
          </button>
        </form>
      </div>
    </AdminTrainPageShell>
  );
}
