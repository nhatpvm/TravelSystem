import React, { useEffect, useMemo, useState } from 'react';
import { Package, Plus, RefreshCw, WandSparkles } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import AdminTrainPageShell from '../train/components/AdminTrainPageShell';
import useAdminTrainScope from '../train/hooks/useAdminTrainScope';
import { getCarTypeLabel, TRAIN_CAR_TYPES } from '../../tenant/train/utils/presentation';
import {
  createAdminTrainCar,
  deleteAdminTrainCar,
  generateAdminTrainCarSeats,
  getAdminTrainOptions,
  listAdminTrainCars,
  listAdminTrainTrips,
  restoreAdminTrainCar,
  updateAdminTrainCar,
} from '../../../services/trainService';

function createEmptyForm() {
  return { tripId: '', carNumber: '', carType: 1, cabinClass: '', sortOrder: 0, isActive: true };
}

function createLayoutForm() {
  return { rows: 12, columns: 4, useCompartments: false, compartmentSize: 4, sleeperUpperLower: false, seatClass: '', priceModifier: '' };
}

function hydrateForm(item) {
  return {
    tripId: item.tripId || '',
    carNumber: item.carNumber || '',
    carType: item.carType || 1,
    cabinClass: item.cabinClass || '',
    sortOrder: item.sortOrder ?? 0,
    isActive: item.isActive ?? true,
  };
}

function getDefaultLayoutForCarType(carType) {
  if (Number(carType) === 2) {
    return { rows: 6, columns: 4, useCompartments: true, compartmentSize: 4, sleeperUpperLower: true, seatClass: 'Giường nằm', priceModifier: '' };
  }

  return { rows: 12, columns: 4, useCompartments: false, compartmentSize: 4, sleeperUpperLower: false, seatClass: 'Ghế mềm', priceModifier: '' };
}

export default function AdminTrainCarsPage() {
  const [searchParams] = useSearchParams();
  const { tenantId, tenants, selectedTenantId, setSelectedTenantId, selectedTenant, scopeError } = useAdminTrainScope();
  const [trips, setTrips] = useState([]);
  const [cars, setCars] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [form, setForm] = useState(createEmptyForm);
  const [layoutForm, setLayoutForm] = useState(createLayoutForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  async function loadData() {
    if (!tenantId) {
      setTrips([]);
      setCars([]);
      setSelectedId('');
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, tripResponse, carResponse] = await Promise.all([
        getAdminTrainOptions(tenantId),
        listAdminTrainTrips({ includeDeleted: true }, tenantId),
        listAdminTrainCars({ includeDeleted: true, tripId: selectedTripId || undefined }, tenantId),
      ]);

      const nextTrips = Array.isArray(tripResponse?.items) ? tripResponse.items.filter((item) => !item.isDeleted) : [];
      const optionCars = Array.isArray(optionsResponse?.cars) ? optionsResponse.cars : [];
      const nextCars = Array.isArray(carResponse?.items) ? carResponse.items : [];

      setTrips(nextTrips);
      if (!selectedTripId) {
        setSelectedTripId(nextTrips[0]?.id || '');
      }

      const mergedCars = nextCars.map((item) => ({
        ...item,
        seatCount: optionCars.find((optionItem) => optionItem.id === item.id)?.seatCount || 0,
        trip: optionCars.find((optionItem) => optionItem.id === item.id)?.trip || null,
      }));

      setCars(mergedCars);

      if (mergedCars.length > 0) {
        const selected = mergedCars.find((item) => item.id === selectedId) || mergedCars[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
        setLayoutForm(getDefaultLayoutForCarType(selected.carType));
      } else {
        setSelectedId('');
        setForm((current) => ({ ...createEmptyForm(), tripId: selectedTripId || nextTrips[0]?.id || '' }));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không tải được danh sách toa tàu.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [tenantId, selectedTripId]);

  const tripLookup = useMemo(() => Object.fromEntries(trips.map((item) => [item.id, item])), [trips]);

  function handleCreateNew() {
    const nextTripId = selectedTripId || trips[0]?.id || '';
    setSelectedId('');
    setForm({ ...createEmptyForm(), tripId: nextTripId });
    setLayoutForm(getDefaultLayoutForCarType(1));
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!tenantId) return;

    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = {
        tripId: form.tripId,
        carNumber: form.carNumber.trim(),
        carType: Number(form.carType),
        cabinClass: form.cabinClass.trim() || null,
        sortOrder: Number(form.sortOrder || 0),
        isActive: !!form.isActive,
      };

      if (selectedId) {
        await updateAdminTrainCar(selectedId, payload, tenantId);
        setNotice('Đã cập nhật toa tàu.');
      } else {
        await createAdminTrainCar(payload, tenantId);
        setNotice('Đã tạo toa tàu mới.');
      }

      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được toa tàu.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) return;

    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreAdminTrainCar(item.id, tenantId);
        setNotice('Đã khôi phục toa tàu.');
      } else {
        await deleteAdminTrainCar(item.id, tenantId);
        setNotice('Đã ẩn toa tàu.');
      }

      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái toa tàu.');
    }
  }

  async function handleGenerateSeats() {
    if (!selectedId || !tenantId) {
      setError('Hãy chọn hoặc tạo một toa tàu trước khi sinh ghế/giường.');
      return;
    }

    setGenerating(true);
    setError('');
    setNotice('');

    try {
      await generateAdminTrainCarSeats(selectedId, {
        rows: Number(layoutForm.rows || 0),
        columns: Number(layoutForm.columns || 0),
        useCompartments: !!layoutForm.useCompartments,
        compartmentSize: Number(layoutForm.compartmentSize || 0),
        sleeperUpperLower: !!layoutForm.sleeperUpperLower,
        seatClass: layoutForm.seatClass || null,
        priceModifier: layoutForm.priceModifier === '' ? null : Number(layoutForm.priceModifier),
      }, tenantId);

      setNotice('Đã sinh sơ đồ ghế/giường cho toa tàu.');
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không sinh được sơ đồ ghế/giường.');
    } finally {
      setGenerating(false);
    }
  }

  const filteredCars = selectedTripId ? cars.filter((item) => item.tripId === selectedTripId) : cars;

  return (
    <AdminTrainPageShell
      pageKey="cars"
      title="Toa tàu toàn hệ thống"
      subtitle="Admin kiểm soát cấu hình toa và sinh sơ đồ ghế/giường theo tenant mà không đổi pattern UI hiện có."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm toa tàu
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="space-y-8">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-8 py-6 border-b border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div>
                <p className="text-lg font-black text-slate-900">Danh sách toa tàu</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Mỗi toa gắn với một chuyến tàu cụ thể trong tenant hiện tại.</p>
              </div>
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <select value={selectedTripId} onChange={(event) => setSelectedTripId(event.target.value)} className="bg-transparent text-sm font-bold text-slate-700 outline-none">
                  {trips.map((trip) => (
                    <option key={trip.id} value={trip.id}>{trip.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải toa tàu...</div>
              ) : filteredCars.length === 0 ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có toa tàu nào cho chuyến đã chọn.</div>
              ) : filteredCars.map((item) => (
                <div
                  key={item.id}
                  role="button"
                  tabIndex={0}
                  onClick={() => {
                    setSelectedId(item.id);
                    setForm(hydrateForm(item));
                    setLayoutForm(getDefaultLayoutForCarType(item.carType));
                  }}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      setSelectedId(item.id);
                      setForm(hydrateForm(item));
                      setLayoutForm(getDefaultLayoutForCarType(item.carType));
                    }
                  }}
                  className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                        <Package size={20} />
                      </div>
                      <div>
                        <div className="flex items-center gap-3 flex-wrap">
                          <p className="font-black text-slate-900">Toa {item.carNumber}</p>
                          <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">{getCarTypeLabel(item.carType)}</span>
                          {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                        </div>
                        <p className="text-xs font-bold text-slate-400 mt-2">{tripLookup[item.tripId]?.name || item.trip?.name || 'Chưa gắn chuyến'} • {item.cabinClass || 'Chưa khai báo hạng'}</p>
                        <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">{item.seatCount || 0} chỗ trên sơ đồ</p>
                        <div className="mt-3">
                          <Link
                            to={`/admin/train/car-seats?carId=${item.id}`}
                            onClick={(event) => event.stopPropagation()}
                            className="inline-flex items-center gap-2 px-3 py-2 rounded-xl bg-slate-100 text-[10px] font-black uppercase tracking-widest text-slate-600"
                          >
                            Ghế & giường
                          </Link>
                        </div>
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

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xl font-black text-slate-900">Sinh sơ đồ ghế/giường</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Dùng chung pattern với màn manager để admin hỗ trợ tenant nhanh hơn.</p>
              </div>
              <button type="button" onClick={handleGenerateSeats} disabled={generating || !selectedId} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2 disabled:opacity-60">
                <WandSparkles size={16} />
                {generating ? 'Đang sinh...' : 'Sinh sơ đồ'}
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số hàng</span><input type="number" value={layoutForm.rows} onChange={(event) => setLayoutForm((current) => ({ ...current, rows: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
              <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số cột</span><input type="number" value={layoutForm.columns} onChange={(event) => setLayoutForm((current) => ({ ...current, columns: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
              <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Khoang / buồng</span><input type="number" value={layoutForm.compartmentSize} onChange={(event) => setLayoutForm((current) => ({ ...current, compartmentSize: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="flex items-center gap-3"><input type="checkbox" checked={layoutForm.useCompartments} onChange={(event) => setLayoutForm((current) => ({ ...current, useCompartments: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" /><span className="text-sm font-bold text-slate-700">Dùng khoang</span></label>
              <label className="flex items-center gap-3"><input type="checkbox" checked={layoutForm.sleeperUpperLower} onChange={(event) => setLayoutForm((current) => ({ ...current, sleeperUpperLower: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" /><span className="text-sm font-bold text-slate-700">Giường trên / dưới</span></label>
              <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Seat class</span><input value={layoutForm.seatClass} onChange={(event) => setLayoutForm((current) => ({ ...current, seatClass: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật toa tàu' : 'Tạo toa tàu mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Admin có thể chỉnh tay toa nếu tenant cần hỗ trợ nhanh.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chuyến tàu</span>
            <select value={form.tripId} onChange={(event) => setForm((current) => ({ ...current, tripId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
              <option value="">Chọn chuyến</option>
              {trips.map((trip) => (
                <option key={trip.id} value={trip.id}>{trip.name}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số toa</span><input value={form.carNumber} onChange={(event) => setForm((current) => ({ ...current, carNumber: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại toa</span><select value={form.carType} onChange={(event) => { setForm((current) => ({ ...current, carType: event.target.value })); setLayoutForm(getDefaultLayoutForCarType(event.target.value)); }} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">{TRAIN_CAR_TYPES.map((item) => (<option key={item.value} value={item.value}>{item.label}</option>))}</select></label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hạng khoang</span><input value={form.cabinClass} onChange={(event) => setForm((current) => ({ ...current, cabinClass: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thứ tự</span><input type="number" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
          </div>

          <label className="flex items-center gap-3 pt-1"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" /><span className="text-sm font-bold text-slate-700">Cho phép hoạt động</span></label>

          <button type="submit" disabled={saving || !tenantId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu cập nhật' : 'Tạo toa tàu'}
          </button>
        </form>
      </div>
    </AdminTrainPageShell>
  );
}
