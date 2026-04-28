import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import HotelModeShell from '../components/HotelModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminExtraService,
  createManagedExtraService,
  deleteAdminExtraService,
  deleteManagedExtraService,
  getAdminExtraService,
  getAdminHotelOptions,
  getHotelManagerOptions,
  getManagedExtraService,
  listAdminExtraServices,
  listManagedExtraServices,
  restoreAdminExtraService,
  restoreManagedExtraService,
  updateAdminExtraService,
  updateManagedExtraService,
} from '../../../../services/hotelService';
import {
  EXTRA_SERVICE_TYPE_OPTIONS,
  getExtraServiceTypeLabel,
  parseEnumOptionValue,
  readJsonInput,
  toPrettyJson,
} from '../utils/presentation';

function createEmptyForm(hotelId = '') {
  return {
    hotelId,
    code: '',
    name: '',
    type: 99,
    description: '',
    metadataJson: '',
    isActive: true,
    pricesJson: '[]',
  };
}

function hydrateForm(item) {
  return {
    hotelId: item.hotelId || '',
    code: item.code || '',
    name: item.name || '',
    type: parseEnumOptionValue(EXTRA_SERVICE_TYPE_OPTIONS, item.type, 99),
    description: item.description || '',
    metadataJson: item.metadataJson || '',
    isActive: item.isActive ?? true,
    pricesJson: toPrettyJson(item.prices || []),
    rowVersionBase64: item.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    hotelId: form.hotelId,
    code: form.code.trim(),
    name: form.name.trim(),
    type: Number(form.type || 99),
    description: form.description.trim() || null,
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
    prices: readJsonInput(form.pricesJson, []),
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function HotelExtraServicesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [hotels, setHotels] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedHotelId, setSelectedHotelId] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm());

  const listFn = isAdmin ? (params) => listAdminExtraServices(params, tenantId) : listManagedExtraServices;
  const getFn = isAdmin ? (id, params) => getAdminExtraService(id, params, tenantId) : getManagedExtraService;
  const createFn = isAdmin ? (payload) => createAdminExtraService(payload, tenantId) : createManagedExtraService;
  const updateFn = isAdmin ? (id, payload) => updateAdminExtraService(id, payload, tenantId) : updateManagedExtraService;
  const deleteFn = isAdmin ? (id) => deleteAdminExtraService(id, tenantId) : deleteManagedExtraService;
  const restoreFn = isAdmin ? (id) => restoreAdminExtraService(id, tenantId) : restoreManagedExtraService;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        isAdmin ? getAdminHotelOptions(tenantId) : getHotelManagerOptions(),
        listFn({ includeDeleted: true, pageSize: 100 }),
      ]);

      const nextHotels = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      setHotels(nextHotels);
      setItems(nextItems);

      const nextHotelId = selectedHotelId || nextHotels[0]?.id || '';
      setSelectedHotelId(nextHotelId);

      if (!selectedId) {
        setForm(createEmptyForm(nextHotelId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dịch vụ thêm.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, loadDataRef, tenantId]);

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [items, selectedHotelId],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm(selectedHotelId || hotels[0]?.id || ''));
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
        setNotice('Đã cập nhật dịch vụ thêm.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo dịch vụ thêm mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu dịch vụ thêm.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreFn(item.id);
      } else {
        await deleteFn(item.id);
      }
      setNotice('Đã cập nhật trạng thái dịch vụ thêm.');
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái dịch vụ thêm.');
    }
  }

  return (
    <HotelModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="extra-services"
      title="Dịch vụ thêm"
      subtitle="Quản lý breakfast, extra bed, airport pickup và các dịch vụ upsell khác."
      notice={notice}
      error={error}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm dịch vụ
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách dịch vụ</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Theo dõi các dịch vụ bán thêm của từng khách sạn.</p>
            </div>
            <select value={selectedHotelId} onChange={(event) => setSelectedHotelId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Tất cả khách sạn</option>
              {hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="divide-y divide-slate-50 max-h-[720px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải dịch vụ thêm...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có dịch vụ thêm nào.</div>
            ) : filteredItems.map((item) => (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={async () => {
                  setSelectedId(item.id);
                  const detail = await getFn(item.id, { includeDeleted: true });
                  setForm(hydrateForm(detail));
                }}
                onKeyDown={async (event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    setSelectedId(item.id);
                    const detail = await getFn(item.id, { includeDeleted: true });
                    setForm(hydrateForm(detail));
                  }
                }}
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.name}</p>
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                        {getExtraServiceTypeLabel(item.type)}
                      </span>
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.code}</p>
                  </div>
                  <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật dịch vụ thêm' : 'Tạo dịch vụ thêm mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Nhập bảng giá theo JSON để cover đủ contract giá theo ngày của backend.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <select value={form.hotelId} onChange={(event) => setForm((current) => ({ ...current, hotelId: event.target.value }))} className="md:col-span-2 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn khách sạn</option>
              {hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã dịch vụ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên dịch vụ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <select value={form.type} onChange={(event) => setForm((current) => ({ ...current, type: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              {EXTRA_SERVICE_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Mô tả dịch vụ" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <textarea value={form.pricesJson} onChange={(event) => setForm((current) => ({ ...current, pricesJson: event.target.value }))} rows={10} placeholder='Prices JSON, ví dụ: [{"startDate":"2026-04-13","endDate":"2026-04-30","currencyCode":"VND","price":250000}]' className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Kích hoạt dịch vụ này
          </label>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật dịch vụ' : 'Tạo dịch vụ')}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </HotelModeShell>
  );
}
