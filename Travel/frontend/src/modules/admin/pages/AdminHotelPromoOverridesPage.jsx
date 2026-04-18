import React, { useEffect, useMemo, useState } from 'react';
import { BadgePercent, Plus, RefreshCw } from 'lucide-react';
import AdminHotelPageShell from '../hotel/components/AdminHotelPageShell';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';
import {
  createAdminPromoRateOverride,
  deleteAdminPromoRateOverride,
  getAdminHotelOptions,
  getAdminPromoRateOverride,
  getAdminRatePlan,
  listAdminPromoRateOverrides,
  restoreAdminPromoRateOverride,
  updateAdminPromoRateOverride,
} from '../../../services/hotelService';

function createEmptyForm(ratePlanId = '', ratePlanRoomTypeId = '') {
  return {
    ratePlanId,
    ratePlanRoomTypeId,
    code: '',
    promoCode: '',
    startDate: '',
    endDate: '',
    overridePrice: '',
    discountPercent: '',
    currencyCode: 'VND',
    conditionsJson: '{}',
    isActive: true,
  };
}

function hydrateForm(detail) {
  return {
    ratePlanId: detail.ratePlanId || '',
    ratePlanRoomTypeId: detail.ratePlanRoomTypeId || '',
    code: detail.code || '',
    promoCode: detail.promoCode || '',
    startDate: detail.startDate || '',
    endDate: detail.endDate || '',
    overridePrice: detail.overridePrice ?? '',
    discountPercent: detail.discountPercent ?? '',
    currencyCode: detail.currencyCode || 'VND',
    conditionsJson: detail.conditionsJson || '{}',
    isActive: detail.isActive ?? true,
    rowVersionBase64: detail.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    ratePlanRoomTypeId: form.ratePlanRoomTypeId || null,
    code: form.code.trim() || null,
    promoCode: form.promoCode.trim() || null,
    startDate: form.startDate || null,
    endDate: form.endDate || null,
    overridePrice: form.overridePrice === '' ? null : Number(form.overridePrice),
    discountPercent: form.discountPercent === '' ? null : Number(form.discountPercent),
    currencyCode: form.currencyCode.trim() || 'VND',
    conditionsJson: form.conditionsJson.trim() || null,
    isActive: !!form.isActive,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function AdminHotelPromoOverridesPage() {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminHotelScope();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [ratePlans, setRatePlans] = useState([]);
  const [mappings, setMappings] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedRatePlanId, setSelectedRatePlanId] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm());

  async function loadRatePlanMappings(ratePlanId) {
    if (!tenantId || !ratePlanId) {
      setMappings([]);
      return;
    }

    try {
      const detail = await getAdminRatePlan(ratePlanId, { includeDeleted: true }, tenantId);
      setMappings(Array.isArray(detail?.roomTypes) ? detail.roomTypes : []);
    } catch (requestError) {
      setMappings([]);
      setError(requestError.message || 'Không thể tải mapping gói giá - hạng phòng.');
    }
  }

  async function loadData() {
    if (!tenantId) {
      setRatePlans([]);
      setMappings([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getAdminHotelOptions(tenantId),
        listAdminPromoRateOverrides({ includeDeleted: true, pageSize: 100 }, tenantId),
      ]);

      const nextRatePlans = Array.isArray(optionsResponse?.ratePlans) ? optionsResponse.ratePlans : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      const nextRatePlanId = selectedRatePlanId || nextRatePlans[0]?.id || '';

      setRatePlans(nextRatePlans);
      setItems(nextItems);
      setSelectedRatePlanId(nextRatePlanId);

      if (!selectedId) {
        setForm(createEmptyForm(nextRatePlanId));
      }

      if (nextRatePlanId) {
        await loadRatePlanMappings(nextRatePlanId);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải promo override khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [tenantId]);

  useEffect(() => {
    loadRatePlanMappings(selectedRatePlanId);
  }, [selectedRatePlanId, tenantId]);

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedRatePlanId || item.ratePlanId === selectedRatePlanId),
    [items, selectedRatePlanId],
  );

  async function loadDetail(id) {
    try {
      const detail = await getAdminPromoRateOverride(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setSelectedRatePlanId(detail.ratePlanId || '');
      if (detail.ratePlanId) {
        await loadRatePlanMappings(detail.ratePlanId);
      }
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết promo override.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm(selectedRatePlanId, ''));
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);
      if (selectedId) {
        await updateAdminPromoRateOverride(selectedId, payload, tenantId);
        setNotice('Đã cập nhật promo override.');
      } else {
        await createAdminPromoRateOverride(payload, tenantId);
        setNotice('Đã tạo promo override mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu promo override.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreAdminPromoRateOverride(item.id, tenantId);
        setNotice('Đã khôi phục promo override.');
      } else {
        await deleteAdminPromoRateOverride(item.id, tenantId);
        setNotice('Đã ẩn promo override.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái promo override.');
    }
  }

  return (
    <AdminHotelPageShell
      pageKey="promo-overrides"
      title="Promo override"
      subtitle="Admin kiểm soát các mức giá override theo gói giá và hạng phòng trong tenant khách sạn đang chọn."
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
            Tạo override
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách override</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Lọc theo gói giá để kiểm tra các khoảng ngày đang được override.</p>
            </div>
            <select value={selectedRatePlanId} onChange={(event) => setSelectedRatePlanId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Tất cả gói giá</option>
              {ratePlans.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="divide-y divide-slate-50 max-h-[720px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải promo override...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có promo override nào.</div>
            ) : filteredItems.map((item) => (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => loadDetail(item.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    loadDetail(item.id);
                  }
                }}
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                      <BadgePercent size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{item.promoCode || item.code || 'Promo override'}</p>
                        {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">Tạm ngưng</span> : null}
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        {item.startDate} → {item.endDate} • {item.overridePrice ? `${item.overridePrice} ${item.currencyCode}` : `${item.discountPercent || 0}%`}
                      </p>
                    </div>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật promo override' : 'Tạo promo override mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn đúng rate plan mapping trước khi nhập giá override hoặc phần trăm giảm.</p>
          </div>

          <select value={form.ratePlanId} onChange={async (event) => {
            const nextRatePlanId = event.target.value;
            setForm((current) => ({ ...current, ratePlanId: nextRatePlanId, ratePlanRoomTypeId: '' }));
            setSelectedRatePlanId(nextRatePlanId);
            await loadRatePlanMappings(nextRatePlanId);
          }} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn gói giá</option>
            {ratePlans.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <select value={form.ratePlanRoomTypeId} onChange={(event) => setForm((current) => ({ ...current, ratePlanRoomTypeId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn mapping gói giá - hạng phòng</option>
            {mappings.map((item) => (
              <option key={item.id} value={item.id}>{item.roomTypeName} • {item.basePrice || 0} {item.currencyCode || 'VND'}</option>
            ))}
          </select>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} placeholder="Mã nội bộ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.promoCode} onChange={(event) => setForm((current) => ({ ...current, promoCode: event.target.value }))} placeholder="Promo code hiển thị" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="date" value={form.startDate} onChange={(event) => setForm((current) => ({ ...current, startDate: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="date" value={form.endDate} onChange={(event) => setForm((current) => ({ ...current, endDate: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.overridePrice} onChange={(event) => setForm((current) => ({ ...current, overridePrice: event.target.value }))} placeholder="Giá override" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.discountPercent} onChange={(event) => setForm((current) => ({ ...current, discountPercent: event.target.value }))} placeholder="Giảm giá %" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.currencyCode} onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))} placeholder="Tiền tệ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <textarea value={form.conditionsJson} onChange={(event) => setForm((current) => ({ ...current, conditionsJson: event.target.value }))} rows={8} placeholder='{"channel":"mobile","minStay":2}' className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Kích hoạt override
          </label>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : selectedId ? 'Cập nhật override' : 'Tạo override'}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </AdminHotelPageShell>
  );
}
