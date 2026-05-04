import React, { useEffect, useMemo, useState } from 'react';
import { Building2, Edit2, Globe2, Plus, RefreshCw, RotateCcw, Search, Trash2 } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  createProvider,
  deleteProvider,
  getProvider,
  listGeoDistricts,
  listGeoProvinces,
  listGeoWards,
  listLocations,
  listProviders,
  restoreProvider,
  updateProvider,
} from '../../../services/masterDataService';
import {
  PROVIDER_TYPE_OPTIONS,
  formatDate,
  getActiveBadgeClass,
  getActiveBadgeLabel,
  getProviderTypeLabel,
} from '../master-data/utils/options';

function buildEmptyForm() {
  return {
    type: 1,
    code: '',
    name: '',
    slug: '',
    legalName: '',
    supportPhone: '',
    supportEmail: '',
    websiteUrl: '',
    locationId: '',
    provinceId: '',
    districtId: '',
    wardId: '',
    addressLine: '',
    ratingAverage: '',
    ratingCount: 0,
    description: '',
    isActive: true,
  };
}

function slugifyValue(value) {
  return String(value || '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function mapProviderToForm(item) {
  return {
    type: Number(item.type || 1),
    code: item.code || '',
    name: item.name || '',
    slug: item.slug || '',
    legalName: item.legalName || '',
    supportPhone: item.supportPhone || '',
    supportEmail: item.supportEmail || '',
    websiteUrl: item.websiteUrl || '',
    locationId: item.locationId || '',
    provinceId: item.provinceId || '',
    districtId: item.districtId || '',
    wardId: item.wardId || '',
    addressLine: item.addressLine || '',
    ratingAverage: item.ratingAverage ?? '',
    ratingCount: item.ratingCount ?? 0,
    description: item.description || '',
    isActive: item.isActive ?? true,
  };
}

const AdminProvidersPage = ({ scope = 'admin' }) => {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
    showTenantSelector,
    scopeHint,
  } = useAdminMasterDataScope({ scope });
  const [items, setItems] = useState([]);
  const [locationOptions, setLocationOptions] = useState([]);
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);
  const [wards, setWards] = useState([]);
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

  const loadLocationOptionsRef = useLatestRef(loadLocationOptions);
  const loadProvidersRef = useLatestRef(loadProviders);
  const loadDistrictsRef = useLatestRef(loadDistricts);
  const loadWardsRef = useLatestRef(loadWards);

  useEffect(() => {
    loadProvinces();
  }, []);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadProvidersRef.current();
    loadLocationOptionsRef.current();
  }, [tenantId, search, typeFilter, includeDeleted, loadProvidersRef, loadLocationOptionsRef]);

  useEffect(() => {
    if (!form.provinceId) {
      setDistricts([]);
      setWards([]);
      return;
    }

    loadDistrictsRef.current(form.provinceId);
  }, [form.provinceId, loadDistrictsRef]);

  useEffect(() => {
    if (!form.districtId) {
      setWards([]);
      return;
    }

    loadWardsRef.current(form.districtId);
  }, [form.districtId, loadWardsRef]);

  async function loadProviders() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listProviders({
        q: search || undefined,
        type: typeFilter === 'all' ? undefined : typeFilter,
        includeDeleted,
      }, tenantId, scope);

      setItems(response.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách đối tác.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadLocationOptions() {
    if (!tenantId) {
      return;
    }

    try {
      const response = await listLocations({ includeDeleted: false }, tenantId, scope);
      setLocationOptions(response.items || []);
    } catch {
      setLocationOptions([]);
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

  function updateField(key, value) {
    setForm((current) => {
      if (key === 'name' && !selectedId) {
        return { ...current, name: value, slug: current.slug || slugifyValue(value) };
      }

      if (key === 'provinceId') {
        return { ...current, provinceId: value, districtId: '', wardId: '' };
      }

      if (key === 'districtId') {
        return { ...current, districtId: value, wardId: '' };
      }

      return { ...current, [key]: value };
    });
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
      const detail = await getProvider(item.id, { includeDeleted }, tenantId, scope);
      setForm(mapProviderToForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết đối tác.');
      setForm(mapProviderToForm(item));
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
      code: form.code.trim(),
      name: form.name.trim(),
      slug: slugifyValue(form.slug || form.name),
      legalName: form.legalName.trim() || null,
      supportPhone: form.supportPhone.trim() || null,
      supportEmail: form.supportEmail.trim() || null,
      websiteUrl: form.websiteUrl.trim() || null,
      locationId: form.locationId || null,
      provinceId: form.provinceId || null,
      districtId: form.districtId || null,
      wardId: form.wardId || null,
      addressLine: form.addressLine.trim() || null,
      ratingAverage: form.ratingAverage === '' ? null : Number(form.ratingAverage),
      ratingCount: Number(form.ratingCount || 0),
      description: form.description.trim() || null,
      isActive: form.isActive,
    };

    try {
      if (selectedId) {
        await updateProvider(selectedId, payload, tenantId, scope);
        setNotice('Đối tác đã được cập nhật.');
      } else {
        await createProvider(payload, tenantId, scope);
        setNotice('Đối tác mới đã được tạo.');
      }

      resetForm();
      await loadProvidersRef.current();
      await loadLocationOptionsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu đối tác.');
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
        await restoreProvider(item.id, tenantId, scope);
        setNotice('Đối tác đã được khôi phục.');
      } else {
        await deleteProvider(item.id, tenantId, scope);
        setNotice('Đối tác đã được chuyển vào thùng rác.');
      }

      if (selectedId === item.id) {
        resetForm();
      }

      await loadProvidersRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật đối tác.');
    }
  }

  return (
    <MasterDataPageShell
      pageKey="providers"
      title="Đối tác"
      subtitle="Quản lý đối tác vận hành theo tenant: nhà xe, tàu, bay, tour và khách sạn."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      showTenantSelector={showTenantSelector}
      scopeHint={scopeHint}
      navScope={scope}
      error={scopeError || error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <button onClick={loadProviders} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
            <RefreshCw size={14} /> Tải lại
          </button>
          <button onClick={resetForm} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} /> Tạo đối tác
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.85fr,1.15fr] gap-6">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
              <Building2 size={18} />
            </div>
            <div>
              <h3 className="text-lg font-black text-slate-900">{selectedItem ? 'Cập nhật đối tác' : 'Tạo đối tác mới'}</h3>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Đối tác + slug SEO</p>
            </div>
          </div>

          <select value={form.type} onChange={(event) => updateField('type', Number(event.target.value))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
            {PROVIDER_TYPE_OPTIONS.map((item) => (
              <option key={item.value} value={item.value}>{item.label}</option>
            ))}
          </select>
          <div className="grid grid-cols-2 gap-4">
            <input value={form.code} onChange={(event) => updateField('code', event.target.value)} placeholder="Mã" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            <input value={form.slug} onChange={(event) => updateField('slug', slugifyValue(event.target.value))} placeholder="slug-seo" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <input value={form.name} onChange={(event) => updateField('name', event.target.value)} placeholder="Tên đối tác" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <input value={form.legalName} onChange={(event) => updateField('legalName', event.target.value)} placeholder="Tên pháp lý" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.supportPhone} onChange={(event) => updateField('supportPhone', event.target.value)} placeholder="Số hotline" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.supportEmail} onChange={(event) => updateField('supportEmail', event.target.value)} placeholder="support@example.com" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <div className="relative">
            <Globe2 size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
            <input value={form.websiteUrl} onChange={(event) => updateField('websiteUrl', event.target.value)} placeholder="https://website.com" className="w-full rounded-2xl border border-slate-100 bg-slate-50 pl-11 pr-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <select value={form.locationId} onChange={(event) => updateField('locationId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn địa điểm có sẵn</option>
            {locationOptions.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
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
          <textarea value={form.addressLine} onChange={(event) => updateField('addressLine', event.target.value)} rows={3} placeholder="Địa chỉ chi tiết" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <div className="grid grid-cols-2 gap-4">
            <input value={form.ratingAverage} onChange={(event) => updateField('ratingAverage', event.target.value)} placeholder="Điểm đánh giá 0..5" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.ratingCount} onChange={(event) => updateField('ratingCount', event.target.value)} placeholder="Số lượt đánh giá" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <textarea value={form.description} onChange={(event) => updateField('description', event.target.value)} rows={4} placeholder="Mô tả ngắn về đối tác" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <button onClick={() => updateField('isActive', !form.isActive)} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${form.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
            {form.isActive ? 'Đang hoạt động' : 'Đang tạm dừng'}
          </button>
          <div className="flex flex-wrap gap-3 pt-2">
            <button onClick={resetForm} className="px-5 py-3 rounded-2xl bg-white text-slate-500 border border-slate-100 text-sm font-bold hover:text-slate-700 transition-all">
              Làm mới biểu mẫu
            </button>
            <button onClick={handleSave} disabled={saving || !tenantId} className="px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-60">
              {saving ? 'Đang lưu...' : selectedItem ? 'Cập nhật đối tác' : 'Tạo đối tác'}
            </button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm theo tên, mã, slug..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
            </div>
            <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)} className="bg-slate-50 rounded-xl border border-slate-100 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="all">Tất cả loại</option>
              {PROVIDER_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest ${includeDeleted ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
              {includeDeleted ? 'Đang xem đã xóa' : 'Ẩn đã xóa'}
            </button>
          </div>

          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 py-4 border-b border-slate-100 bg-slate-50/70 text-[10px] font-black uppercase tracking-widest text-slate-400">
              <div className="col-span-4">Đối tác</div>
              <div className="col-span-2">Loại</div>
              <div className="col-span-3">Địa chỉ</div>
              <div className="col-span-1">Trạng thái</div>
              <div className="col-span-2">Thao tác</div>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải đối tác...</div>
              ) : items.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có đối tác nào.</div>
              ) : items.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-2 md:col-span-4">
                    <p className="font-black text-slate-900">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.code} | {item.slug} | {formatDate(item.createdAt)}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getProviderTypeLabel(item.type)}</div>
                  <div className="col-span-1 md:col-span-3 text-xs font-medium text-slate-500">
                    {item.locationName || [item.provinceName, item.districtName, item.wardName].filter(Boolean).join(' | ') || 'Chưa gắn địa chỉ'}
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

export default AdminProvidersPage;
