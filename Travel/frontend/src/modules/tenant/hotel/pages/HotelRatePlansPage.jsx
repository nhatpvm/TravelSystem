import React, { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Plus, RefreshCw } from 'lucide-react';
import HotelModeShell from '../components/HotelModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminRatePlan,
  createManagedRatePlan,
  deleteAdminRatePlan,
  deleteManagedRatePlan,
  getAdminHotelOptions,
  getAdminRatePlan,
  getHotelManagerOptions,
  getManagedRatePlan,
  listAdminRatePlans,
  listManagedRatePlans,
  restoreAdminRatePlan,
  restoreManagedRatePlan,
  updateAdminRatePlan,
  updateManagedRatePlan,
} from '../../../../services/hotelService';
import {
  parseEnumOptionValue,
  RATE_PLAN_STATUS_OPTIONS,
  RATE_PLAN_TYPE_OPTIONS,
  readJsonInput,
  toPrettyJson,
  getRatePlanStatusLabel,
  getStatusClass,
} from '../utils/presentation';

function createEmptyForm(hotelId = '') {
  return {
    hotelId,
    code: '',
    name: '',
    description: '',
    type: 1,
    status: 2,
    cancellationPolicyId: '',
    checkInOutRuleId: '',
    propertyPolicyId: '',
    refundable: true,
    breakfastIncluded: false,
    minNights: '',
    maxNights: '',
    minAdvanceDays: '',
    maxAdvanceDays: '',
    requiresGuarantee: false,
    metadataJson: '',
    isActive: true,
    roomTypesJson: '[]',
    policyJson: '{}',
  };
}

function hydrateForm(item) {
  return {
    hotelId: item.hotelId || '',
    code: item.code || '',
    name: item.name || '',
    description: item.description || '',
    type: parseEnumOptionValue(RATE_PLAN_TYPE_OPTIONS, item.type, 1),
    status: parseEnumOptionValue(RATE_PLAN_STATUS_OPTIONS, item.status, 2),
    cancellationPolicyId: item.cancellationPolicyId || '',
    checkInOutRuleId: item.checkInOutRuleId || '',
    propertyPolicyId: item.propertyPolicyId || '',
    refundable: item.refundable ?? true,
    breakfastIncluded: item.breakfastIncluded ?? false,
    minNights: item.minNights ?? '',
    maxNights: item.maxNights ?? '',
    minAdvanceDays: item.minAdvanceDays ?? '',
    maxAdvanceDays: item.maxAdvanceDays ?? '',
    requiresGuarantee: item.requiresGuarantee ?? false,
    metadataJson: item.metadataJson || '',
    isActive: item.isActive ?? true,
    roomTypesJson: toPrettyJson(item.roomTypes || []),
    policyJson: toPrettyJson(item.policy || {}),
    rowVersionBase64: item.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    hotelId: form.hotelId,
    code: form.code.trim(),
    name: form.name.trim(),
    description: form.description.trim() || null,
    type: Number(form.type || 1),
    status: Number(form.status || 2),
    cancellationPolicyId: form.cancellationPolicyId || null,
    checkInOutRuleId: form.checkInOutRuleId || null,
    propertyPolicyId: form.propertyPolicyId || null,
    refundable: !!form.refundable,
    breakfastIncluded: !!form.breakfastIncluded,
    minNights: form.minNights === '' ? null : Number(form.minNights),
    maxNights: form.maxNights === '' ? null : Number(form.maxNights),
    minAdvanceDays: form.minAdvanceDays === '' ? null : Number(form.minAdvanceDays),
    maxAdvanceDays: form.maxAdvanceDays === '' ? null : Number(form.maxAdvanceDays),
    requiresGuarantee: !!form.requiresGuarantee,
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
    roomTypes: readJsonInput(form.roomTypesJson, []),
    policy: readJsonInput(form.policyJson, {}),
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function HotelRatePlansPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [searchParams] = useSearchParams();
  const initialHotelId = searchParams.get('hotelId') || '';
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [options, setOptions] = useState({
    hotels: [],
    cancellationPolicies: [],
    checkInOutRules: [],
    propertyPolicies: [],
  });
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedHotelId, setSelectedHotelId] = useState(initialHotelId);
  const [form, setForm] = useState(createEmptyForm(initialHotelId));

  const listFn = isAdmin ? (params) => listAdminRatePlans(params, tenantId) : listManagedRatePlans;
  const getFn = isAdmin ? (id, params) => getAdminRatePlan(id, params, tenantId) : getManagedRatePlan;
  const createFn = isAdmin ? (payload) => createAdminRatePlan(payload, tenantId) : createManagedRatePlan;
  const updateFn = isAdmin ? (id, payload) => updateAdminRatePlan(id, payload, tenantId) : updateManagedRatePlan;
  const deleteFn = isAdmin ? (id) => deleteAdminRatePlan(id, tenantId) : deleteManagedRatePlan;
  const restoreFn = isAdmin ? (id) => restoreAdminRatePlan(id, tenantId) : restoreManagedRatePlan;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLoading(false);
      setItems([]);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        isAdmin ? getAdminHotelOptions(tenantId) : getHotelManagerOptions(),
        listFn({ includeDeleted: true, pageSize: 100 }),
      ]);

      setOptions({
        hotels: Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [],
        cancellationPolicies: Array.isArray(optionsResponse?.cancellationPolicies) ? optionsResponse.cancellationPolicies : [],
        checkInOutRules: Array.isArray(optionsResponse?.checkInOutRules) ? optionsResponse.checkInOutRules : [],
        propertyPolicies: Array.isArray(optionsResponse?.propertyPolicies) ? optionsResponse.propertyPolicies : [],
      });

      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      setItems(nextItems);

      if (selectedId) {
        const detail = await getFn(selectedId, { includeDeleted: true });
        setForm(hydrateForm(detail));
      } else {
        setForm(createEmptyForm(selectedHotelId || optionsResponse?.hotels?.[0]?.id || ''));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải gói giá.');
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
    setForm(createEmptyForm(selectedHotelId || options.hotels[0]?.id || ''));
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
        setNotice('Đã cập nhật gói giá.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo gói giá mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu gói giá.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreFn(item.id);
        setNotice('Đã khôi phục gói giá.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn gói giá.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái gói giá.');
    }
  }

  return (
    <HotelModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="rate-plans"
      title="Gói giá"
      subtitle="Quản lý cấu hình bán phòng, điều kiện hoàn hủy và mapping giá với từng hạng phòng."
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
            Thêm gói giá
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách gói giá</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Chọn gói giá cần sửa để cập nhật chính sách và mapping hạng phòng.</p>
            </div>
            <select value={selectedHotelId} onChange={(event) => setSelectedHotelId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Tất cả khách sạn</option>
              {options.hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="divide-y divide-slate-50 max-h-[720px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải gói giá...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có gói giá nào.</div>
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
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getStatusClass(item.status)}`}>
                        {getRatePlanStatusLabel(item.status)}
                      </span>
                      {item.breakfastIncluded ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-sky-100 text-sky-700">Kèm ăn sáng</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {item.code} • {item.refundable ? 'Hoàn hủy được' : 'Không hoàn hủy'}
                    </p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật gói giá' : 'Tạo gói giá mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Dùng JSON để map roomTypes và policy đúng theo contract backend.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <select value={form.hotelId} onChange={(event) => setForm((current) => ({ ...current, hotelId: event.target.value }))} className="md:col-span-2 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn khách sạn</option>
              {options.hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã gói giá" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên gói giá" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <select value={form.type} onChange={(event) => setForm((current) => ({ ...current, type: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              {RATE_PLAN_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              {RATE_PLAN_STATUS_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
          </div>

          <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={3} placeholder="Mô tả gói giá" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <select value={form.cancellationPolicyId} onChange={(event) => setForm((current) => ({ ...current, cancellationPolicyId: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chính sách hủy</option>
              {options.cancellationPolicies.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <select value={form.checkInOutRuleId} onChange={(event) => setForm((current) => ({ ...current, checkInOutRuleId: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Rule check-in/out</option>
              {options.checkInOutRules.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <select value={form.propertyPolicyId} onChange={(event) => setForm((current) => ({ ...current, propertyPolicyId: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Property policy</option>
              {options.propertyPolicies.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <input value={form.minNights} onChange={(event) => setForm((current) => ({ ...current, minNights: event.target.value }))} placeholder="Min nights" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.maxNights} onChange={(event) => setForm((current) => ({ ...current, maxNights: event.target.value }))} placeholder="Max nights" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.minAdvanceDays} onChange={(event) => setForm((current) => ({ ...current, minAdvanceDays: event.target.value }))} placeholder="Min advance days" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.maxAdvanceDays} onChange={(event) => setForm((current) => ({ ...current, maxAdvanceDays: event.target.value }))} placeholder="Max advance days" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.refundable} onChange={(event) => setForm((current) => ({ ...current, refundable: event.target.checked }))} /> Hoàn hủy</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.breakfastIncluded} onChange={(event) => setForm((current) => ({ ...current, breakfastIncluded: event.target.checked }))} /> Kèm ăn sáng</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.requiresGuarantee} onChange={(event) => setForm((current) => ({ ...current, requiresGuarantee: event.target.checked }))} /> Cần guarantee</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /> Kích hoạt</label>
          </div>

          <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <textarea value={form.roomTypesJson} onChange={(event) => setForm((current) => ({ ...current, roomTypesJson: event.target.value }))} rows={10} placeholder='RoomTypes JSON, ví dụ: [{"roomTypeId":"...","basePrice":1500000,"currencyCode":"VND"}]' className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <textarea value={form.policyJson} onChange={(event) => setForm((current) => ({ ...current, policyJson: event.target.value }))} rows={10} placeholder='Policy JSON, ví dụ: {"policyJson":"...","isActive":true}' className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          </div>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật gói giá' : 'Tạo gói giá')}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </HotelModeShell>
  );
}
