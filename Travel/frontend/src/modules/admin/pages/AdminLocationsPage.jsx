import React, { useEffect, useMemo, useState } from 'react';
import { Edit2, MapPinned, Plus, RefreshCw, RotateCcw, Search, Trash2 } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import {
  createLocation,
  deleteLocation,
  getLocation,
  listGeoDistricts,
  listGeoProvinces,
  listGeoWards,
  listLocations,
  restoreLocation,
  updateLocation,
} from '../../../services/masterDataService';
import {
  LOCATION_TYPE_OPTIONS,
  formatDate,
  getActiveBadgeClass,
  getActiveBadgeLabel,
  getLocationTypeLabel,
} from '../master-data/utils/options';

function buildEmptyForm() {
  return {
    type: 1,
    name: '',
    shortName: '',
    code: '',
    airportIataCode: '',
    trainStationCode: '',
    busStationCode: '',
    timeZone: 'Asia/Ho_Chi_Minh',
    addressLine: '',
    provinceId: '',
    districtId: '',
    wardId: '',
    latitude: '',
    longitude: '',
    isActive: true,
  };
}

function mapLocationToForm(item) {
  return {
    type: Number(item.type || 1),
    name: item.name || '',
    shortName: item.shortName || '',
    code: item.code || '',
    airportIataCode: item.airportIataCode || '',
    trainStationCode: item.trainStationCode || '',
    busStationCode: item.busStationCode || '',
    timeZone: item.timeZone || 'Asia/Ho_Chi_Minh',
    addressLine: item.addressLine || '',
    provinceId: item.provinceId || '',
    districtId: item.districtId || '',
    wardId: item.wardId || '',
    latitude: item.latitude ?? '',
    longitude: item.longitude ?? '',
    isActive: item.isActive ?? true,
  };
}

const AdminLocationsPage = () => {
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
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);
  const [wards, setWards] = useState([]);

  const selectedItem = useMemo(
    () => items.find((item) => item.id === selectedId) || null,
    [items, selectedId],
  );

  useEffect(() => {
    loadProvinces();
  }, []);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadLocations();
  }, [tenantId, search, typeFilter, includeDeleted]);

  useEffect(() => {
    if (!form.provinceId) {
      setDistricts([]);
      setWards([]);
      return;
    }

    loadDistricts(form.provinceId);
  }, [form.provinceId]);

  useEffect(() => {
    if (!form.districtId) {
      setWards([]);
      return;
    }

    loadWards(form.districtId);
  }, [form.districtId]);

  async function loadLocations() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listLocations({
        q: search || undefined,
        type: typeFilter === 'all' ? undefined : typeFilter,
        includeDeleted,
      }, tenantId);

      setItems(response.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách địa điểm.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadProvinces() {
    try {
      const response = await listGeoProvinces();
      setProvinces(response.items || []);
    } catch {
      setProvinces([]);
    }
  }

  async function loadDistricts(provinceId) {
    const province = provinces.find((item) => item.id === provinceId);
    if (!province?.code) {
      setDistricts([]);
      return;
    }

    try {
      const response = await listGeoDistricts({ provinceCode: province.code });
      setDistricts(response.items || []);
    } catch {
      setDistricts([]);
    }
  }

  async function loadWards(districtId) {
    const district = districts.find((item) => item.id === districtId);
    if (!district?.code) {
      setWards([]);
      return;
    }

    try {
      const response = await listGeoWards({ districtCode: district.code });
      setWards(response.items || []);
    } catch {
      setWards([]);
    }
  }

  function resetForm() {
    setSelectedId('');
    setForm(buildEmptyForm());
    setNotice('');
  }

  function updateField(key, value) {
    setForm((current) => {
      if (key === 'provinceId') {
        return { ...current, provinceId: value, districtId: '', wardId: '' };
      }

      if (key === 'districtId') {
        return { ...current, districtId: value, wardId: '' };
      }

      return { ...current, [key]: value };
    });
  }

  async function selectItem(item) {
    setSelectedId(item.id);
    setNotice('');
    setError('');

    try {
      const detail = await getLocation(item.id, { includeDeleted }, tenantId);
      setForm(mapLocationToForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết địa điểm.');
      setForm(mapLocationToForm(item));
    }
  }

  async function handleSave() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setNotice('');
    setError('');

    const payload = {
      type: Number(form.type),
      name: form.name.trim(),
      shortName: form.shortName.trim() || null,
      code: form.code.trim() || null,
      airportIataCode: form.airportIataCode.trim() || null,
      trainStationCode: form.trainStationCode.trim() || null,
      busStationCode: form.busStationCode.trim() || null,
      timeZone: form.timeZone.trim() || null,
      addressLine: form.addressLine.trim() || null,
      provinceId: form.provinceId || null,
      districtId: form.districtId || null,
      wardId: form.wardId || null,
      latitude: form.latitude === '' ? null : Number(form.latitude),
      longitude: form.longitude === '' ? null : Number(form.longitude),
      isActive: form.isActive,
    };

    try {
      if (selectedId) {
        await updateLocation(selectedId, payload, tenantId);
        setNotice('Địa điểm đã được cập nhật.');
      } else {
        await createLocation(payload, tenantId);
        setNotice('Địa điểm mới đã được tạo.');
      }

      resetForm();
      await loadLocations();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu địa điểm.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) {
      return;
    }

    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreLocation(item.id, tenantId);
        setNotice('Địa điểm đã được khôi phục.');
      } else {
        await deleteLocation(item.id, tenantId);
        setNotice('Địa điểm đã được chuyển vào thùng rác.');
      }

      if (selectedId === item.id) {
        resetForm();
      }

      await loadLocations();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật địa điểm.');
    }
  }

  return (
    <MasterDataPageShell
      pageKey="locations"
      title="Địa điểm"
      subtitle="Quản lý bến xe, ga tàu, sân bay và các điểm dùng chung cho toàn hệ thống."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <button onClick={loadLocations} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
            <RefreshCw size={14} /> Tải lại
          </button>
          <button onClick={resetForm} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} /> Tạo địa điểm
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.85fr,1.15fr] gap-6">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
              <MapPinned size={18} />
            </div>
            <div>
              <h3 className="text-lg font-black text-slate-900">{selectedItem ? 'Cập nhật địa điểm' : 'Tạo địa điểm mới'}</h3>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Dữ liệu nền theo tenant</p>
            </div>
          </div>

          <select value={form.type} onChange={(event) => updateField('type', Number(event.target.value))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
            {LOCATION_TYPE_OPTIONS.map((item) => (
              <option key={item.value} value={item.value}>{item.label}</option>
            ))}
          </select>
          <input value={form.name} onChange={(event) => updateField('name', event.target.value)} placeholder="Tên địa điểm" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <div className="grid grid-cols-2 gap-4">
            <input value={form.shortName} onChange={(event) => updateField('shortName', event.target.value)} placeholder="Tên ngắn" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.code} onChange={(event) => updateField('code', event.target.value)} placeholder="Mã" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <input value={form.airportIataCode} onChange={(event) => updateField('airportIataCode', event.target.value.toUpperCase())} placeholder="Mã IATA" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.trainStationCode} onChange={(event) => updateField('trainStationCode', event.target.value)} placeholder="Mã ga tàu" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.busStationCode} onChange={(event) => updateField('busStationCode', event.target.value)} placeholder="Mã bến xe" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <input value={form.timeZone} onChange={(event) => updateField('timeZone', event.target.value)} placeholder="Múi giờ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <textarea value={form.addressLine} onChange={(event) => updateField('addressLine', event.target.value)} rows={3} placeholder="Địa chỉ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <select value={form.provinceId} onChange={(event) => updateField('provinceId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn tỉnh/thành</option>
              {provinces.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <select value={form.districtId} onChange={(event) => updateField('districtId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn quận/huyện</option>
              {districts.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <select value={form.wardId} onChange={(event) => updateField('wardId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn phường/xã</option>
              {wards.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <input value={form.latitude} onChange={(event) => updateField('latitude', event.target.value)} placeholder="Vĩ độ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.longitude} onChange={(event) => updateField('longitude', event.target.value)} placeholder="Kinh độ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <button onClick={() => updateField('isActive', !form.isActive)} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${form.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
            {form.isActive ? 'Đang hoạt động' : 'Đang tạm dừng'}
          </button>
          <div className="flex flex-wrap gap-3 pt-2">
            <button onClick={resetForm} className="px-5 py-3 rounded-2xl bg-white text-slate-500 border border-slate-100 text-sm font-bold hover:text-slate-700 transition-all">
              Làm mới biểu mẫu
            </button>
            <button onClick={handleSave} disabled={saving || !tenantId} className="px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-60">
              {saving ? 'Đang lưu...' : selectedItem ? 'Cập nhật địa điểm' : 'Tạo địa điểm'}
            </button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm theo tên, mã..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
            </div>
            <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)} className="bg-slate-50 rounded-xl border border-slate-100 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="all">Tất cả loại</option>
              {LOCATION_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest ${includeDeleted ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
              {includeDeleted ? 'Đang xem đã xóa' : 'Ẩn đã xóa'}
            </button>
          </div>

          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 py-4 border-b border-slate-100 bg-slate-50/70 text-[10px] font-black uppercase tracking-widest text-slate-400">
              <div className="col-span-4">Địa điểm</div>
              <div className="col-span-2">Loại</div>
              <div className="col-span-3">Địa giới</div>
              <div className="col-span-1">Trạng thái</div>
              <div className="col-span-2">Thao tác</div>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải địa điểm...</div>
              ) : items.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có địa điểm nào.</div>
              ) : items.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-2 md:col-span-4">
                    <p className="font-black text-slate-900">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.code || 'Chưa có mã'} | {formatDate(item.createdAt)}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getLocationTypeLabel(item.type)}</div>
                  <div className="col-span-1 md:col-span-3 text-xs font-medium text-slate-500">
                    {[item.provinceName, item.districtName, item.wardName].filter(Boolean).join(' | ') || 'Chưa gắn địa giới'}
                  </div>
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

export default AdminLocationsPage;
