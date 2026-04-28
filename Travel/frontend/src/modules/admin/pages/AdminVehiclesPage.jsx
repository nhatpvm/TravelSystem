import React, { useEffect, useMemo, useState } from 'react';
import { BusFront, Edit2, Plus, RefreshCw, RotateCcw, Search, Trash2 } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  createVehicle,
  deleteVehicle,
  getVehicle,
  listProviders,
  listSeatMaps,
  listVehicleModels,
  listVehicles,
  restoreVehicle,
  updateVehicle,
} from '../../../services/masterDataService';
import {
  PROVIDER_TYPE_OPTIONS,
  VEHICLE_TYPE_OPTIONS,
  buildVehicleModelLabel,
  formatDate,
  getActiveBadgeClass,
  getActiveBadgeLabel,
  getVehicleTypeLabel,
} from '../master-data/utils/options';

function buildEmptyForm() {
  return {
    vehicleType: 1,
    providerId: '',
    vehicleModelId: '',
    seatMapId: '',
    code: '',
    name: '',
    seatCapacity: 45,
    plateNumber: '',
    registrationNumber: '',
    status: 'Hoạt động',
    inServiceFrom: '',
    inServiceTo: '',
    isActive: true,
  };
}

function mapVehicleToForm(item) {
  return {
    vehicleType: Number(item.vehicleType || 1),
    providerId: item.providerId || '',
    vehicleModelId: item.vehicleModelId || '',
    seatMapId: item.seatMapId || '',
    code: item.code || '',
    name: item.name || '',
    seatCapacity: item.seatCapacity || 1,
    plateNumber: item.plateNumber || '',
    registrationNumber: item.registrationNumber || '',
    status: item.status || 'Hoạt động',
    inServiceFrom: item.inServiceFrom ? item.inServiceFrom.slice(0, 10) : '',
    inServiceTo: item.inServiceTo ? item.inServiceTo.slice(0, 10) : '',
    isActive: item.isActive ?? true,
  };
}

function resolveProviderTypeForVehicle(vehicleType) {
  switch (Number(vehicleType)) {
    case 1:
      return [1];
    case 2:
      return [2];
    case 3:
      return [3];
    case 4:
      return [1, 4];
    default:
      return [];
  }
}

const AdminVehiclesPage = () => {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminMasterDataScope();
  const [items, setItems] = useState([]);
  const [providers, setProviders] = useState([]);
  const [vehicleModels, setVehicleModels] = useState([]);
  const [seatMaps, setSeatMaps] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [includeDeleted, setIncludeDeleted] = useState(true);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(buildEmptyForm());
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');

  const selectedItem = useMemo(
    () => items.find((item) => item.id === selectedId) || null,
    [items, selectedId],
  );

  const filteredProviders = useMemo(() => {
    const allowedTypes = resolveProviderTypeForVehicle(form.vehicleType);
    return providers.filter((item) => allowedTypes.includes(Number(item.type)));
  }, [form.vehicleType, providers]);

  const filteredVehicleModels = useMemo(
    () => vehicleModels.filter((item) => Number(item.vehicleType) === Number(form.vehicleType)),
    [form.vehicleType, vehicleModels],
  );

  const filteredSeatMaps = useMemo(
    () => seatMaps.filter((item) => Number(item.vehicleType) === Number(form.vehicleType)),
    [form.vehicleType, seatMaps],
  );

  const loadReferencesRef = useLatestRef(loadReferences);
  const loadItemsRef = useLatestRef(loadItems);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadReferencesRef.current();
  }, [loadReferencesRef, tenantId]);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadItemsRef.current();
  }, [tenantId, search, typeFilter, includeDeleted, loadItemsRef]);

  async function loadReferences() {
    if (!tenantId) {
      return;
    }

    try {
      const [providersResponse, modelsResponse, seatMapsResponse] = await Promise.all([
        listProviders({ includeDeleted: false }, tenantId),
        listVehicleModels({ includeDeleted: false }, tenantId),
        listSeatMaps({ includeDeleted: false }, tenantId),
      ]);

      setProviders(providersResponse.items || []);
      setVehicleModels(modelsResponse.items || []);
      setSeatMaps(seatMapsResponse.items || []);
    } catch {
      setProviders([]);
      setVehicleModels([]);
      setSeatMaps([]);
    }
  }

  async function loadItems() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listVehicles({
        q: search || undefined,
        vehicleType: typeFilter === 'all' ? undefined : typeFilter,
        includeDeleted,
      }, tenantId);

      setItems(response.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách phương tiện.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  function resetForm() {
    setSelectedId('');
    setForm(buildEmptyForm());
    setNotice('');
  }

  async function selectItem(item) {
    setSelectedId(item.id);
    setNotice('');
    setError('');

    try {
      const detail = await getVehicle(item.id, { includeDeleted }, tenantId);
      setForm(mapVehicleToForm(detail.vehicle || item));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết phương tiện.');
      setForm(mapVehicleToForm(item));
    }
  }

  function updateField(key, value) {
    setForm((current) => {
      if (key === 'vehicleType') {
        return {
          ...current,
          vehicleType: Number(value),
          providerId: '',
          vehicleModelId: '',
          seatMapId: '',
        };
      }

      return { ...current, [key]: value };
    });
  }

  async function handleSave() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setNotice('');
    setError('');

    const payload = {
      vehicleType: Number(form.vehicleType),
      providerId: form.providerId,
      vehicleModelId: form.vehicleModelId || null,
      seatMapId: form.seatMapId || null,
      code: form.code.trim(),
      name: form.name.trim(),
      plateNumber: form.plateNumber.trim() || null,
      registrationNumber: form.registrationNumber.trim() || null,
      seatCapacity: Number(form.seatCapacity || 1),
      inServiceFrom: form.inServiceFrom || null,
      inServiceTo: form.inServiceTo || null,
      status: form.status.trim() || null,
      isActive: form.isActive,
    };

    try {
      if (selectedId) {
        await updateVehicle(selectedId, payload, tenantId);
        setNotice('Phương tiện đã được cập nhật.');
      } else {
        await createVehicle(payload, tenantId);
        setNotice('Phương tiện mới đã được tạo.');
      }

      resetForm();
      await loadItemsRef.current();
      await loadReferencesRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu phương tiện.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) {
      return;
    }

    setNotice('');
    setError('');

    try {
      if (item.isDeleted) {
        await restoreVehicle(item.id, tenantId);
        setNotice('Phương tiện đã được khôi phục.');
      } else {
        await deleteVehicle(item.id, tenantId);
        setNotice('Phương tiện đã được chuyển vào thùng rác.');
      }

      if (selectedId === item.id) {
        resetForm();
      }

      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật phương tiện.');
    }
  }

  return (
    <MasterDataPageShell
      pageKey="vehicles"
      title="Phương tiện"
      subtitle="Gắn đối tác, mẫu phương tiện và sơ đồ ghế cho từng phương tiện vận hành."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <button onClick={loadItems} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
            <RefreshCw size={14} /> Tải lại
          </button>
          <button onClick={resetForm} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} /> Tạo phương tiện
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.85fr,1.15fr] gap-6">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
              <BusFront size={18} />
            </div>
            <div>
              <h3 className="text-lg font-black text-slate-900">{selectedItem ? 'Cập nhật phương tiện' : 'Tạo phương tiện mới'}</h3>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Tài sản đội xe</p>
            </div>
          </div>

          <select value={form.vehicleType} onChange={(event) => updateField('vehicleType', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
            {VEHICLE_TYPE_OPTIONS.map((item) => (
              <option key={item.value} value={item.value}>{item.label}</option>
            ))}
          </select>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <select value={form.providerId} onChange={(event) => updateField('providerId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn đối tác</option>
              {filteredProviders.map((item) => (
                <option key={item.id} value={item.id}>{item.name} | {PROVIDER_TYPE_OPTIONS.find((option) => option.value === Number(item.type))?.label || item.type}</option>
              ))}
            </select>
            <select value={form.vehicleModelId} onChange={(event) => updateField('vehicleModelId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn mẫu phương tiện</option>
              {filteredVehicleModels.map((item) => (
                <option key={item.id} value={item.id}>{buildVehicleModelLabel(item)}</option>
              ))}
            </select>
          </div>
          <select value={form.seatMapId} onChange={(event) => updateField('seatMapId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn sơ đồ ghế</option>
            {filteredSeatMaps.map((item) => (
              <option key={item.id} value={item.id}>{item.name} | {item.seatCount || 0} ghế</option>
            ))}
          </select>
          <div className="grid grid-cols-2 gap-4">
            <input value={form.code} onChange={(event) => updateField('code', event.target.value)} placeholder="Mã" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => updateField('name', event.target.value)} placeholder="Tên phương tiện" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <input value={form.plateNumber} onChange={(event) => updateField('plateNumber', event.target.value)} placeholder="Biển số" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.registrationNumber} onChange={(event) => updateField('registrationNumber', event.target.value)} placeholder="Số đăng kiểm" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <input value={form.seatCapacity} onChange={(event) => updateField('seatCapacity', event.target.value)} placeholder="Sức chứa ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.status} onChange={(event) => updateField('status', event.target.value)} placeholder="Hoạt động / Bảo trì" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <input type="date" value={form.inServiceFrom} onChange={(event) => updateField('inServiceFrom', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="date" value={form.inServiceTo} onChange={(event) => updateField('inServiceTo', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <button onClick={() => updateField('isActive', !form.isActive)} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${form.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
            {form.isActive ? 'Đang hoạt động' : 'Đang tạm dừng'}
          </button>
          <div className="flex flex-wrap gap-3 pt-2">
            <button onClick={resetForm} className="px-5 py-3 rounded-2xl bg-white text-slate-500 border border-slate-100 text-sm font-bold hover:text-slate-700 transition-all">
              Làm mới biểu mẫu
            </button>
            <button onClick={handleSave} disabled={saving || !tenantId} className="px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-60">
              {saving ? 'Đang lưu...' : selectedItem ? 'Cập nhật phương tiện' : 'Tạo phương tiện'}
            </button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm mã, tên, biển số..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
            </div>
            <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)} className="bg-slate-50 rounded-xl border border-slate-100 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="all">Tất cả loại phương tiện</option>
              {VEHICLE_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest ${includeDeleted ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
              {includeDeleted ? 'Đang xem đã xóa' : 'Ẩn đã xóa'}
            </button>
          </div>

          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 py-4 border-b border-slate-100 bg-slate-50/70 text-[10px] font-black uppercase tracking-widest text-slate-400">
              <div className="col-span-4">Phương tiện</div>
              <div className="col-span-2">Loại</div>
              <div className="col-span-2">Đối tác</div>
              <div className="col-span-2">Sơ đồ ghế</div>
              <div className="col-span-2">Thao tác</div>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải phương tiện...</div>
              ) : items.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có phương tiện nào.</div>
              ) : items.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-2 md:col-span-4">
                    <p className="font-black text-slate-900">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.code} | {item.plateNumber || item.registrationNumber || 'Chưa có biển số'} | {formatDate(item.createdAt)}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getVehicleTypeLabel(item.vehicleType)}</div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{item.providerName || 'Chưa gắn đối tác'}</div>
                  <div className="col-span-1 md:col-span-2 text-xs font-medium text-slate-500">
                    {item.seatMapName ? `${item.seatMapName} | ${item.seatMapSeatCount || 0} ghế` : 'Chưa gắn sơ đồ ghế'}
                  </div>
                  <div className="col-span-2 md:col-span-2 flex gap-2">
                    <button onClick={() => selectItem(item)} className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all" title="Sửa">
                      <Edit2 size={14} />
                    </button>
                    <button onClick={() => handleToggleDelete(item)} className={`p-2 rounded-xl transition-all ${item.isDeleted ? 'text-emerald-600 hover:bg-emerald-50' : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'}`} title={item.isDeleted ? 'Khôi phục' : 'Xóa mềm'}>
                      {item.isDeleted ? <RotateCcw size={14} /> : <Trash2 size={14} />}
                    </button>
                    <span className={`px-3 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest ${getActiveBadgeClass(item)}`}>
                      {getActiveBadgeLabel(item)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </MasterDataPageShell>
  );
};

export default AdminVehiclesPage;
