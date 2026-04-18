import React, { useEffect, useMemo, useState } from 'react';
import { Edit2, Layers3, Plus, RefreshCw, RotateCcw, Search, Trash2 } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import {
  createVehicleModel,
  deleteVehicleModel,
  listVehicleModels,
  restoreVehicleModel,
  updateVehicleModel,
} from '../../../services/masterDataService';
import {
  VEHICLE_TYPE_OPTIONS,
  formatDate,
  getActiveBadgeClass,
  getActiveBadgeLabel,
  getVehicleTypeLabel,
} from '../master-data/utils/options';

function buildEmptyForm() {
  return {
    vehicleType: 1,
    manufacturer: '',
    modelName: '',
    modelYear: '',
    isActive: true,
  };
}

function mapModelToForm(item) {
  return {
    vehicleType: Number(item.vehicleType || 1),
    manufacturer: item.manufacturer || '',
    modelName: item.modelName || '',
    modelYear: item.modelYear ?? '',
    isActive: item.isActive ?? true,
  };
}

const AdminVehicleModelsPage = () => {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminMasterDataScope();
  const [items, setItems] = useState([]);
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

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadItems();
  }, [tenantId, search, typeFilter, includeDeleted]);

  async function loadItems() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listVehicleModels({
        q: search || undefined,
        vehicleType: typeFilter === 'all' ? undefined : typeFilter,
        includeDeleted,
      }, tenantId);

      setItems(response.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách mẫu phương tiện.');
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

  function selectItem(item) {
    setSelectedId(item.id);
    setForm(mapModelToForm(item));
    setNotice('');
    setError('');
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
      manufacturer: form.manufacturer.trim(),
      modelName: form.modelName.trim(),
      modelYear: form.modelYear === '' ? null : Number(form.modelYear),
      isActive: form.isActive,
    };

    try {
      if (selectedId) {
        await updateVehicleModel(selectedId, payload, tenantId);
        setNotice('Mẫu phương tiện đã được cập nhật.');
      } else {
        await createVehicleModel(payload, tenantId);
        setNotice('Mẫu phương tiện mới đã được tạo.');
      }

      resetForm();
      await loadItems();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu mẫu phương tiện.');
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
        await restoreVehicleModel(item.id, tenantId);
        setNotice('Mẫu phương tiện đã được khôi phục.');
      } else {
        await deleteVehicleModel(item.id, tenantId);
        setNotice('Mẫu phương tiện đã được chuyển vào thùng rác.');
      }

      if (selectedId === item.id) {
        resetForm();
      }

      await loadItems();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật mẫu phương tiện.');
    }
  }

  return (
    <MasterDataPageShell
      pageKey="vehicle-models"
      title="Mẫu phương tiện"
      subtitle="Quản lý hãng, tên mẫu và năm sản xuất theo từng loại phương tiện."
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
            <Plus size={14} /> Tạo mẫu
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.75fr,1.25fr] gap-6">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
              <Layers3 size={18} />
            </div>
            <div>
              <h3 className="text-lg font-black text-slate-900">{selectedItem ? 'Cập nhật mẫu phương tiện' : 'Tạo mẫu phương tiện mới'}</h3>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Chuẩn đội xe</p>
            </div>
          </div>

          <select value={form.vehicleType} onChange={(event) => setForm((current) => ({ ...current, vehicleType: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
            {VEHICLE_TYPE_OPTIONS.map((item) => (
              <option key={item.value} value={item.value}>{item.label}</option>
            ))}
          </select>
          <input value={form.manufacturer} onChange={(event) => setForm((current) => ({ ...current, manufacturer: event.target.value }))} placeholder="Hãng sản xuất" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <input value={form.modelName} onChange={(event) => setForm((current) => ({ ...current, modelName: event.target.value }))} placeholder="Tên mẫu" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <input value={form.modelYear} onChange={(event) => setForm((current) => ({ ...current, modelYear: event.target.value }))} placeholder="Năm sản xuất" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <button onClick={() => setForm((current) => ({ ...current, isActive: !current.isActive }))} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${form.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
            {form.isActive ? 'Đang hoạt động' : 'Đang tạm dừng'}
          </button>
          <div className="flex flex-wrap gap-3 pt-2">
            <button onClick={resetForm} className="px-5 py-3 rounded-2xl bg-white text-slate-500 border border-slate-100 text-sm font-bold hover:text-slate-700 transition-all">
              Làm mới biểu mẫu
            </button>
            <button onClick={handleSave} disabled={saving || !tenantId} className="px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-60">
              {saving ? 'Đang lưu...' : selectedItem ? 'Cập nhật mẫu' : 'Tạo mẫu'}
            </button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm hãng, tên mẫu..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
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
              <div className="col-span-5">Mẫu</div>
              <div className="col-span-2">Loại phương tiện</div>
              <div className="col-span-2">Năm SX</div>
              <div className="col-span-1">Trạng thái</div>
              <div className="col-span-2">Thao tác</div>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải mẫu phương tiện...</div>
              ) : items.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có mẫu phương tiện nào.</div>
              ) : items.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-2 md:col-span-5">
                    <p className="font-black text-slate-900">{item.manufacturer} {item.modelName}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{formatDate(item.createdAt)}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getVehicleTypeLabel(item.vehicleType)}</div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{item.modelYear || 'Chưa có'}</div>
                  <div className="col-span-1 md:col-span-1">
                    <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getActiveBadgeClass(item)}`}>
                      {getActiveBadgeLabel(item)}
                    </span>
                  </div>
                  <div className="col-span-2 md:col-span-2 flex gap-2">
                    <button onClick={() => selectItem(item)} className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all" title="Sửa">
                      <Edit2 size={14} />
                    </button>
                    <button onClick={() => handleToggleDelete(item)} className={`p-2 rounded-xl transition-all ${item.isDeleted ? 'text-emerald-600 hover:bg-emerald-50' : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'}`} title={item.isDeleted ? 'Khôi phục' : 'Xóa mềm'}>
                      {item.isDeleted ? <RotateCcw size={14} /> : <Trash2 size={14} />}
                    </button>
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

export default AdminVehicleModelsPage;
