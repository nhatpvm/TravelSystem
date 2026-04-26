import React, { useEffect, useMemo, useState } from 'react';
import { Bus, Plus, RefreshCw } from 'lucide-react';
import BusManagementPageShell from '../components/BusManagementPageShell';
import {
  createBusFleetVehicle,
  deleteBusFleetVehicle,
  getBusManagerOptions,
  listBusFleetVehicles,
  restoreBusFleetVehicle,
  updateBusFleetVehicle,
} from '../../../../services/busService';

const VEHICLE_TYPES = [
  { value: 1, label: 'Xe khách' },
  { value: 4, label: 'Xe tour' },
];

function createEmptyForm() {
  return {
    vehicleType: 1,
    providerId: '',
    seatMapId: '',
    code: '',
    name: '',
    plateNumber: '',
    registrationNumber: '',
    seatCapacity: 40,
    status: 'Ready',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    vehicleType: item.vehicleType ?? 1,
    providerId: item.providerId || '',
    seatMapId: item.seatMapId || '',
    code: item.code || '',
    name: item.name || '',
    plateNumber: item.plateNumber || '',
    registrationNumber: item.registrationNumber || '',
    seatCapacity: item.seatCapacity ?? 40,
    status: item.status || 'Ready',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    vehicleType: Number(form.vehicleType || 1),
    providerId: form.providerId,
    vehicleModelId: null,
    seatMapId: form.seatMapId || null,
    code: form.code.trim(),
    name: form.name.trim(),
    plateNumber: form.plateNumber || null,
    registrationNumber: form.registrationNumber || null,
    seatCapacity: Number(form.seatCapacity || 0),
    status: form.status || null,
    isActive: !!form.isActive,
    metadataJson: null,
  };
}

const BusFleetVehiclesPage = () => {
  const [providers, setProviders] = useState([]);
  const [seatMaps, setSeatMaps] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const providerById = useMemo(() => Object.fromEntries(providers.map((item) => [item.id, item])), [providers]);
  const seatMapById = useMemo(() => Object.fromEntries(seatMaps.map((item) => [item.id, item])), [seatMaps]);

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getBusManagerOptions(),
        listBusFleetVehicles({ includeDeleted: true }),
      ]);

      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      setProviders(Array.isArray(optionsResponse?.providers) ? optionsResponse.providers : []);
      setSeatMaps(Array.isArray(optionsResponse?.seatMaps) ? optionsResponse.seatMaps : []);
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm(createEmptyForm());
      }
    } catch (err) {
      setError(err.message || 'Không tải được danh sách xe.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);

      if (selectedId) {
        await updateBusFleetVehicle(selectedId, payload);
        setNotice('Đã cập nhật xe.');
      } else {
        await createBusFleetVehicle(payload);
        setNotice('Đã tạo xe mới.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không lưu được xe.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusFleetVehicle(item.id);
        setNotice('Đã khôi phục xe.');
      } else {
        await deleteBusFleetVehicle(item.id);
        setNotice('Đã ẩn xe.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái xe.');
    }
  };

  return (
    <BusManagementPageShell
      pageKey="vehicles"
      title="Xe khai thác"
      subtitle="Quản lý xe thật của nhà xe, biển số, sức chứa và sơ đồ ghế dùng cho chuyến bán vé."
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
            onClick={() => {
              setSelectedId('');
              setForm(createEmptyForm());
            }}
            className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2"
          >
            <Plus size={16} />
            Thêm xe
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách xe</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chỉ hiển thị xe khách và xe tour thuộc tenant hiện tại.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải danh sách xe...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có xe nào.</div>
            ) : items.map((item) => {
              const provider = providerById[item.providerId];
              const seatMap = item.seatMapId ? seatMapById[item.seatMapId] : null;
              const typeLabel = VEHICLE_TYPES.find((type) => type.value === item.vehicleType)?.label || 'Xe khách';

              return (
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
                  className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                        <Bus size={20} />
                      </div>
                      <div>
                        <div className="flex items-center gap-3 flex-wrap">
                          <p className="font-black text-slate-900">{item.name}</p>
                          {item.isDeleted ? (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span>
                          ) : null}
                          {!item.isActive ? (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm ngưng</span>
                          ) : null}
                        </div>
                        <p className="text-xs font-bold text-slate-400 mt-2">
                          {typeLabel} • {provider?.name || 'Chưa rõ nhà xe'} • {item.plateNumber || 'Chưa có biển số'}
                        </p>
                        <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">
                          {seatMap?.name || 'Chưa gán sơ đồ ghế'} • {item.seatCapacity} ghế • {item.status || 'Chưa có trạng thái'}
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
              );
            })}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật xe' : 'Tạo xe mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Các trường này dùng trực tiếp khi tạo chuyến và hiển thị sơ đồ ghế.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại xe</span>
              <select
                value={form.vehicleType}
                onChange={(event) => setForm((current) => ({ ...current, vehicleType: Number(event.target.value), seatMapId: '' }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              >
                {VEHICLE_TYPES.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Nhà xe</span>
              <select
                value={form.providerId}
                onChange={(event) => setForm((current) => ({ ...current, providerId: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                required
              >
                <option value="">Chọn nhà xe</option>
                {providers.map((item) => (
                  <option key={item.id} value={item.id}>{item.name}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã xe nội bộ</span>
              <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="BUS-001" required />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên xe</span>
              <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="Limousine 9 chỗ" required />
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Biển số</span>
              <input value={form.plateNumber} onChange={(event) => setForm((current) => ({ ...current, plateNumber: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="51B-123.45" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số đăng kiểm</span>
              <input value={form.registrationNumber} onChange={(event) => setForm((current) => ({ ...current, registrationNumber: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="Mã hồ sơ đăng kiểm" />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Sơ đồ ghế</span>
            <select
              value={form.seatMapId}
              onChange={(event) => setForm((current) => ({ ...current, seatMapId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              <option value="">Chưa gán sơ đồ ghế</option>
              {seatMaps.filter((item) => Number(item.vehicleType) === Number(form.vehicleType)).map((item) => (
                <option key={item.id} value={item.id}>{item.name} • {item.totalRows}x{item.totalColumns}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Sức chứa</span>
              <input type="number" min="1" value={form.seatCapacity} onChange={(event) => setForm((current) => ({ ...current, seatCapacity: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Trạng thái vận hành</span>
              <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
                <option value="Ready">Sẵn sàng chạy</option>
                <option value="Maintenance">Đang bảo trì</option>
                <option value="Inactive">Ngưng khai thác</option>
              </select>
            </label>
          </div>

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="h-4 w-4 rounded border-slate-300" />
            Cho phép dùng xe này khi tạo chuyến
          </label>

          <button type="submit" disabled={saving} className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70">
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu xe' : 'Tạo xe'}
          </button>
        </form>
      </div>
    </BusManagementPageShell>
  );
};

export default BusFleetVehiclesPage;
