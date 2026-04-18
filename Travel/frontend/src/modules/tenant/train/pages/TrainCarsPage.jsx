import React, { useEffect, useMemo, useState } from 'react';
import { Package, Plus, RefreshCw, WandSparkles } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import TrainManagementPageShell from '../components/TrainManagementPageShell';
import { getCarTypeLabel, TRAIN_CAR_TYPES } from '../utils/presentation';
import {
  createTrainCar,
  deleteTrainCar,
  generateTrainCarSeats,
  getTrainManagerOptions,
  listTrainCars,
  listTrainTrips,
  restoreTrainCar,
  updateTrainCar,
} from '../../../../services/trainService';

function createEmptyForm() {
  return {
    tripId: '',
    carNumber: '',
    carType: 1,
    cabinClass: '',
    sortOrder: 0,
    isActive: true,
  };
}

function createLayoutForm() {
  return {
    rows: 12,
    columns: 4,
    useCompartments: false,
    compartmentSize: 4,
    sleeperUpperLower: false,
    seatClass: '',
    priceModifier: '',
  };
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
    return {
      rows: 6,
      columns: 4,
      useCompartments: true,
      compartmentSize: 4,
      sleeperUpperLower: true,
      seatClass: 'Giường nằm',
      priceModifier: '',
    };
  }

  return {
    rows: 12,
    columns: 4,
    useCompartments: false,
    compartmentSize: 4,
    sleeperUpperLower: false,
    seatClass: 'Ghế mềm',
    priceModifier: '',
  };
}

const TrainCarsPage = () => {
  const [searchParams] = useSearchParams();
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

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, tripResponse, carResponse] = await Promise.all([
        getTrainManagerOptions(),
        listTrainTrips(),
        listTrainCars({ includeDeleted: true, tripId: selectedTripId || undefined }),
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
        setForm((current) => ({
          ...createEmptyForm(),
          tripId: selectedTripId || nextTrips[0]?.id || '',
        }));
      }
    } catch (err) {
      setError(err.message || 'Không tải được danh sách toa tàu.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [selectedTripId]);

  const tripLookup = useMemo(
    () => Object.fromEntries(trips.map((item) => [item.id, item])),
    [trips],
  );

  const handleCreateNew = () => {
    const nextTripId = selectedTripId || trips[0]?.id || '';
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      tripId: nextTripId,
    });
    setLayoutForm(getDefaultLayoutForCarType(1));
    setNotice('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
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
        await updateTrainCar(selectedId, payload);
        setNotice('Đã cập nhật toa tàu.');
      } else {
        await createTrainCar(payload);
        setNotice('Đã tạo toa tàu mới.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không lưu được toa tàu.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreTrainCar(item.id);
        setNotice('Đã khôi phục toa tàu.');
      } else {
        await deleteTrainCar(item.id);
        setNotice('Đã ẩn toa tàu.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái toa tàu.');
    }
  };

  const handleGenerateSeats = async () => {
    if (!selectedId) {
      setError('Hãy chọn hoặc tạo một toa tàu trước khi sinh ghế/giường.');
      return;
    }

    setGenerating(true);
    setError('');
    setNotice('');

    try {
      await generateTrainCarSeats(selectedId, {
        rows: Number(layoutForm.rows || 0),
        columns: Number(layoutForm.columns || 0),
        useCompartments: !!layoutForm.useCompartments,
        compartmentSize: Number(layoutForm.compartmentSize || 0),
        sleeperUpperLower: !!layoutForm.sleeperUpperLower,
        seatClass: layoutForm.seatClass || null,
        priceModifier: layoutForm.priceModifier === '' ? null : Number(layoutForm.priceModifier),
      });

      setNotice('Đã sinh sơ đồ ghế/giường cho toa tàu.');
      await loadData();
    } catch (err) {
      setError(err.message || 'Không sinh được sơ đồ ghế/giường.');
    } finally {
      setGenerating(false);
    }
  };

  const filteredCars = selectedTripId
    ? cars.filter((item) => item.tripId === selectedTripId)
    : cars;

  return (
    <TrainManagementPageShell
      pageKey="cars"
      title="Toa tàu"
      subtitle="Mỗi chuyến tàu có thể có nhiều toa khác nhau. Đây là nơi cấu hình toa và sinh sơ đồ ghế/giường."
      error={error}
      notice={notice}
      actions={(
        <>
          <button
            type="button"
            onClick={loadData}
            className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button
            type="button"
            onClick={handleCreateNew}
            className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2"
          >
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
                <select
                  value={selectedTripId}
                  onChange={(event) => setSelectedTripId(event.target.value)}
                  className="bg-transparent text-sm font-bold text-slate-700 outline-none"
                >
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
                          <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                            {getCarTypeLabel(item.carType)}
                          </span>
                          {item.isDeleted ? (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                              Đã ẩn
                            </span>
                          ) : null}
                        </div>
                        <p className="text-xs font-bold text-slate-400 mt-2">
                          {tripLookup[item.tripId]?.name || item.trip?.name || 'Chưa gắn chuyến'} • {item.cabinClass || 'Chưa khai báo hạng'}
                        </p>
                        <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">
                          {item.seatCount || 0} chỗ trên sơ đồ
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

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-xl font-black text-slate-900">Sinh sơ đồ ghế / giường</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Dùng khi muốn tạo nhanh layout cho toa tàu thay vì thêm từng chỗ thủ công.</p>
              </div>
              <button
                type="button"
                onClick={handleGenerateSeats}
                disabled={generating || !selectedId}
                className="px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white disabled:opacity-60 flex items-center gap-2"
              >
                <WandSparkles size={14} />
                {generating ? 'Đang sinh...' : 'Sinh layout'}
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số hàng</span>
                <input
                  type="number"
                  min="1"
                  value={layoutForm.rows}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, rows: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số cột</span>
                <input
                  type="number"
                  min="1"
                  value={layoutForm.columns}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, columns: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Kích thước khoang</span>
                <input
                  type="number"
                  min="2"
                  value={layoutForm.compartmentSize}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, compartmentSize: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  disabled={!layoutForm.useCompartments}
                />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hạng chỗ</span>
                <input
                  value={layoutForm.seatClass}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, seatClass: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  placeholder="Ghế mềm / Giường nằm"
                />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="inline-flex items-center gap-3 text-sm font-bold text-slate-600">
                <input
                  type="checkbox"
                  checked={layoutForm.useCompartments}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, useCompartments: event.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300"
                />
                Chia theo khoang
              </label>
              <label className="inline-flex items-center gap-3 text-sm font-bold text-slate-600">
                <input
                  type="checkbox"
                  checked={layoutForm.sleeperUpperLower}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, sleeperUpperLower: event.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300"
                />
                Giường trên / dưới
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phụ thu mỗi chỗ</span>
                <input
                  type="number"
                  value={layoutForm.priceModifier}
                  onChange={(event) => setLayoutForm((current) => ({ ...current, priceModifier: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                />
              </label>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật toa tàu' : 'Tạo toa tàu mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Mã toa nên ngắn gọn, ổn định để bước chọn chỗ hiển thị rõ ràng cho khách hàng.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chuyến tàu</span>
            <select
              value={form.tripId}
              onChange={(event) => setForm((current) => ({ ...current, tripId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              required
            >
              <option value="">Chọn chuyến tàu</option>
              {trips.map((trip) => (
                <option key={trip.id} value={trip.id}>{trip.name}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số toa</span>
              <input
                value={form.carNumber}
                onChange={(event) => setForm((current) => ({ ...current, carNumber: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                placeholder="01 / B2 / VIP1"
                required
              />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại toa</span>
              <select
                value={form.carType}
                onChange={(event) => {
                  const nextValue = Number(event.target.value);
                  setForm((current) => ({ ...current, carType: nextValue }));
                  setLayoutForm(getDefaultLayoutForCarType(nextValue));
                }}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              >
                {TRAIN_CAR_TYPES.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hạng cabin</span>
              <input
                value={form.cabinClass}
                onChange={(event) => setForm((current) => ({ ...current, cabinClass: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                placeholder="Economy / Sleeper / Business"
              />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thứ tự hiển thị</span>
              <input
                type="number"
                value={form.sortOrder}
                onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
          </div>

          <label className="inline-flex items-center gap-3 text-sm font-bold text-slate-600">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              className="h-4 w-4 rounded border-slate-300"
            />
            Kích hoạt toa tàu này
          </label>

          <button
            type="submit"
            disabled={saving}
            className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
          >
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu thay đổi' : 'Tạo toa tàu'}
          </button>
        </form>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainCarsPage;
