import React, { useEffect, useMemo, useState } from 'react';
import { Armchair, Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import AdminTrainPageShell from '../train/components/AdminTrainPageShell';
import useAdminTrainScope from '../train/hooks/useAdminTrainScope';
import { getSeatTypeLabel, TRAIN_SEAT_TYPES } from '../../tenant/train/utils/presentation';
import {
  createAdminTrainCarSeat,
  deleteAdminTrainCarSeat,
  getAdminTrainCar,
  listAdminTrainCars,
  restoreAdminTrainCarSeat,
  updateAdminTrainCarSeat,
} from '../../../services/trainService';

function createEmptyForm() {
  return {
    carId: '',
    seatNumber: '',
    seatType: 1,
    compartmentCode: '',
    compartmentIndex: '',
    rowIndex: 0,
    columnIndex: 0,
    isWindow: false,
    isAisle: false,
    seatClass: '',
    priceModifier: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    carId: item.carId || '',
    seatNumber: item.seatNumber || '',
    seatType: item.seatType || 1,
    compartmentCode: item.compartmentCode || '',
    compartmentIndex: item.compartmentIndex ?? '',
    rowIndex: item.rowIndex ?? 0,
    columnIndex: item.columnIndex ?? 0,
    isWindow: item.isWindow ?? false,
    isAisle: item.isAisle ?? false,
    seatClass: item.seatClass || '',
    priceModifier: item.priceModifier ?? '',
    isActive: item.isActive ?? true,
  };
}

export default function AdminTrainCarSeatsPage() {
  const [searchParams] = useSearchParams();
  const { tenantId, tenants, selectedTenantId, setSelectedTenantId, selectedTenant, scopeError } = useAdminTrainScope();
  const [cars, setCars] = useState([]);
  const [selectedCarId, setSelectedCarId] = useState(searchParams.get('carId') || '');
  const [selectedId, setSelectedId] = useState('');
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  async function loadCars() {
    if (!tenantId) {
      setCars([]);
      return;
    }

    try {
      const response = await listAdminTrainCars({ includeDeleted: true }, tenantId);
      const nextCars = Array.isArray(response?.items) ? response.items.filter((item) => !item.isDeleted) : [];
      setCars(nextCars);
      if (!selectedCarId) {
        setSelectedCarId(nextCars[0]?.id || '');
      }
    } catch (requestError) {
      setError(requestError.message || 'Không tải được danh sách toa tàu.');
    }
  }

  async function loadSeats() {
    if (!selectedCarId || !tenantId) {
      setItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await getAdminTrainCar(selectedCarId, { includeDeleted: true }, tenantId);
      const nextItems = Array.isArray(response?.seats) ? response.seats : [];
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({ ...createEmptyForm(), carId: selectedCarId });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không tải được danh sách ghế/giường.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadCars();
  }, [tenantId]);

  useEffect(() => {
    loadSeats();
  }, [tenantId, selectedCarId]);

  const selectedCar = useMemo(() => cars.find((item) => item.id === selectedCarId) || null, [cars, selectedCarId]);

  function handleCreateNew() {
    setSelectedId('');
    setForm({ ...createEmptyForm(), carId: selectedCarId });
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
        carId: form.carId || selectedCarId,
        seatNumber: form.seatNumber.trim(),
        seatType: Number(form.seatType),
        compartmentCode: form.compartmentCode.trim() || null,
        compartmentIndex: form.compartmentIndex === '' ? null : Number(form.compartmentIndex),
        rowIndex: Number(form.rowIndex || 0),
        columnIndex: Number(form.columnIndex || 0),
        isWindow: !!form.isWindow,
        isAisle: !!form.isAisle,
        seatClass: form.seatClass.trim() || null,
        priceModifier: form.priceModifier === '' ? null : Number(form.priceModifier),
        isActive: !!form.isActive,
      };

      if (selectedId) {
        await updateAdminTrainCarSeat(selectedId, payload, tenantId);
        setNotice('Đã cập nhật ghế/giường.');
      } else {
        await createAdminTrainCarSeat(payload, tenantId);
        setNotice('Đã thêm ghế/giường mới.');
      }

      await loadSeats();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được ghế/giường.');
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
        await restoreAdminTrainCarSeat(item.id, tenantId);
        setNotice('Đã khôi phục ghế/giường.');
      } else {
        await deleteAdminTrainCarSeat(item.id, tenantId);
        setNotice('Đã ẩn ghế/giường.');
      }

      await loadSeats();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái ghế/giường.');
    }
  }

  return (
    <AdminTrainPageShell
      pageKey="car-seats"
      title="Ghế & giường theo toa"
      subtitle="Admin tinh chỉnh từng chỗ của toa tàu khi cần hỗ trợ tenant chỉnh nhanh dữ liệu live."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <>
          <button type="button" onClick={loadSeats} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm chỗ mới
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách ghế / giường</p>
              <p className="text-xs font-bold text-slate-400 mt-1">{selectedCar ? `Toa ${selectedCar.carNumber} • ${selectedCar.cabinClass || 'Chưa khai báo hạng'}` : 'Chọn toa tàu để xem chi tiết'}</p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
              <select value={selectedCarId} onChange={(event) => setSelectedCarId(event.target.value)} className="bg-transparent text-sm font-bold text-slate-700 outline-none">
                {cars.map((car) => (
                  <option key={car.id} value={car.id}>Toa {car.carNumber}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải sơ đồ chỗ...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Toa này chưa có ghế/giường nào.</div>
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
                      <Armchair size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{item.seatNumber}</p>
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">{getSeatTypeLabel(item.seatType)}</span>
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">Khoang: {item.compartmentCode || '--'} • Hàng {item.rowIndex} • Cột {item.columnIndex}</p>
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
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật chỗ ngồi / giường' : 'Tạo chỗ ngồi / giường mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Có thể dùng để chỉnh riêng từng ghế hoặc từng giường trong một toa cụ thể.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Toa tàu</span>
            <select value={form.carId || selectedCarId} onChange={(event) => setForm((current) => ({ ...current, carId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
              <option value="">Chọn toa</option>
              {cars.map((car) => (
                <option key={car.id} value={car.id}>Toa {car.carNumber}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số ghế / giường</span><input value={form.seatNumber} onChange={(event) => setForm((current) => ({ ...current, seatNumber: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại chỗ</span><select value={form.seatType} onChange={(event) => setForm((current) => ({ ...current, seatType: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">{TRAIN_SEAT_TYPES.map((item) => (<option key={item.value} value={item.value}>{item.label}</option>))}</select></label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Khoang</span><input value={form.compartmentCode} onChange={(event) => setForm((current) => ({ ...current, compartmentCode: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">STT khoang</span><input type="number" value={form.compartmentIndex} onChange={(event) => setForm((current) => ({ ...current, compartmentIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hàng</span><input type="number" value={form.rowIndex} onChange={(event) => setForm((current) => ({ ...current, rowIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Cột</span><input type="number" value={form.columnIndex} onChange={(event) => setForm((current) => ({ ...current, columnIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hạng chỗ</span><input value={form.seatClass} onChange={(event) => setForm((current) => ({ ...current, seatClass: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
            <label className="space-y-2"><span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phụ thu</span><input type="number" value={form.priceModifier} onChange={(event) => setForm((current) => ({ ...current, priceModifier: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" /></label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <label className="flex items-center gap-3 pt-2"><input type="checkbox" checked={form.isWindow} onChange={(event) => setForm((current) => ({ ...current, isWindow: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" /><span className="text-sm font-bold text-slate-700">Sát cửa sổ</span></label>
            <label className="flex items-center gap-3 pt-2"><input type="checkbox" checked={form.isAisle} onChange={(event) => setForm((current) => ({ ...current, isAisle: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" /><span className="text-sm font-bold text-slate-700">Sát lối đi</span></label>
            <label className="flex items-center gap-3 pt-2"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" /><span className="text-sm font-bold text-slate-700">Cho phép hoạt động</span></label>
          </div>

          <button type="submit" disabled={saving || !tenantId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu cập nhật' : 'Tạo chỗ mới'}
          </button>
        </form>
      </div>
    </AdminTrainPageShell>
  );
}
