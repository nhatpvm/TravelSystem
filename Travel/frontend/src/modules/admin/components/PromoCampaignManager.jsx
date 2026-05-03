import React, { useEffect, useMemo, useState } from 'react';
import { BadgePercent, Loader2, Plus, RefreshCw } from 'lucide-react';
import { listAdminTenants } from '../../../services/adminIdentity';
import {
  activateAdminPromotion,
  activateTenantPromotion,
  createAdminPromotion,
  createTenantPromotion,
  deleteAdminPromotion,
  deleteTenantPromotion,
  listAdminPromotions,
  listTenantPromotions,
  pauseAdminPromotion,
  pauseTenantPromotion,
  restoreAdminPromotion,
  restoreTenantPromotion,
  updateAdminPromotion,
  updateTenantPromotion,
} from '../../../services/promotionService';
import { formatCurrency } from '../../tenant/train/utils/presentation';

const PRODUCT_SCOPE = [
  { value: 31, label: 'Tất cả' },
  { value: 1, label: 'Xe khách' },
  { value: 2, label: 'Tàu' },
  { value: 4, label: 'Máy bay' },
  { value: 8, label: 'Khách sạn' },
  { value: 16, label: 'Tour' },
];

const STATUS = {
  1: 'Nháp',
  2: 'Đang chạy',
  3: 'Tạm dừng',
  4: 'Hết hạn',
};

const DISCOUNT_TYPE = {
  1: 'Số tiền',
  2: 'Phần trăm',
};

function toDateTimeInput(value) {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toISOString().slice(0, 16);
}

function createEmptyForm(mode) {
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());

  return {
    ownerScope: mode === 'admin' ? '1' : '2',
    tenantId: '',
    productScope: '31',
    status: '2',
    discountType: '2',
    code: '',
    name: '',
    description: '',
    currencyCode: 'VND',
    discountValue: '',
    maxDiscountAmount: '',
    minOrderAmount: '',
    startsAt: now.toISOString().slice(0, 16),
    endsAt: '',
    globalUsageLimit: '',
    perUserUsageLimit: '',
    perTenantUsageLimit: '',
    budgetAmount: '',
    isPublic: true,
    requiresCode: true,
    rowVersionBase64: '',
  };
}

function hydrateForm(item, mode) {
  return {
    ownerScope: String(item.ownerScope || (mode === 'admin' ? 1 : 2)),
    tenantId: item.tenantId || '',
    productScope: String(item.productScope || 31),
    status: String(item.status || 2),
    discountType: String(item.discountType || 2),
    code: item.code || '',
    name: item.name || '',
    description: item.description || '',
    currencyCode: item.currencyCode || 'VND',
    discountValue: item.discountValue ?? '',
    maxDiscountAmount: item.maxDiscountAmount ?? '',
    minOrderAmount: item.minOrderAmount ?? '',
    startsAt: toDateTimeInput(item.startsAt),
    endsAt: toDateTimeInput(item.endsAt),
    globalUsageLimit: item.globalUsageLimit ?? '',
    perUserUsageLimit: item.perUserUsageLimit ?? '',
    perTenantUsageLimit: item.perTenantUsageLimit ?? '',
    budgetAmount: item.budgetAmount ?? '',
    isPublic: item.isPublic ?? true,
    requiresCode: item.requiresCode ?? true,
    rowVersionBase64: item.rowVersionBase64 || '',
  };
}

function numberOrNull(value) {
  return value === '' || value === null || value === undefined ? null : Number(value);
}

function buildPayload(form, mode) {
  const ownerScope = Number(form.ownerScope);

  return {
    ...(mode === 'admin' ? { ownerScope, tenantId: ownerScope === 2 ? form.tenantId || null : null } : {}),
    productScope: Number(form.productScope),
    status: Number(form.status),
    discountType: Number(form.discountType),
    code: form.code.trim(),
    name: form.name.trim(),
    description: form.description.trim() || null,
    currencyCode: form.currencyCode.trim() || 'VND',
    discountValue: numberOrNull(form.discountValue),
    maxDiscountAmount: numberOrNull(form.maxDiscountAmount),
    minOrderAmount: numberOrNull(form.minOrderAmount),
    startsAt: form.startsAt || null,
    endsAt: form.endsAt || null,
    globalUsageLimit: numberOrNull(form.globalUsageLimit),
    perUserUsageLimit: numberOrNull(form.perUserUsageLimit),
    perTenantUsageLimit: numberOrNull(form.perTenantUsageLimit),
    budgetAmount: numberOrNull(form.budgetAmount),
    isPublic: !!form.isPublic,
    requiresCode: !!form.requiresCode,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

function getApi(mode) {
  if (mode === 'admin') {
    return {
      list: listAdminPromotions,
      create: createAdminPromotion,
      update: updateAdminPromotion,
      remove: deleteAdminPromotion,
      restore: restoreAdminPromotion,
      activate: activateAdminPromotion,
      pause: pauseAdminPromotion,
    };
  }

  return {
    list: listTenantPromotions,
    create: createTenantPromotion,
    update: updateTenantPromotion,
    remove: deleteTenantPromotion,
    restore: restoreTenantPromotion,
    activate: activateTenantPromotion,
    pause: pauseTenantPromotion,
  };
}

export default function PromoCampaignManager({ mode = 'admin' }) {
  const api = useMemo(() => getApi(mode), [mode]);
  const [items, setItems] = useState([]);
  const [tenants, setTenants] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm(mode));
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const [campaignsResponse, tenantsResponse] = await Promise.all([
        api.list({ includeDeleted: true, pageSize: 100 }),
        mode === 'admin' ? listAdminTenants({ page: 1, pageSize: 200 }) : Promise.resolve(null),
      ]);

      setItems(Array.isArray(campaignsResponse?.items) ? campaignsResponse.items : []);
      setTenants(Array.isArray(tenantsResponse?.items) ? tenantsResponse.items : []);
    } catch (requestError) {
      setError(requestError.message || 'Không tải được danh sách khuyến mãi.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [mode]);

  function handleNew() {
    setSelectedId('');
    setForm(createEmptyForm(mode));
    setNotice('');
  }

  function handleSelect(item) {
    setSelectedId(item.id);
    setForm(hydrateForm(item, mode));
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form, mode);
      if (selectedId) {
        await api.update(selectedId, payload);
        setNotice('Đã cập nhật khuyến mãi.');
      } else {
        await api.create(payload);
        setNotice('Đã tạo khuyến mãi mới.');
      }

      await loadData();
      if (!selectedId) {
        setForm(createEmptyForm(mode));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được khuyến mãi.');
    } finally {
      setSaving(false);
    }
  }

  async function toggleDeleted(item) {
    try {
      if (item.isDeleted) {
        await api.restore(item.id);
        setNotice('Đã khôi phục khuyến mãi.');
      } else {
        await api.remove(item.id);
        setNotice('Đã ẩn khuyến mãi.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái.');
    }
  }

  async function toggleActive(item) {
    try {
      if (Number(item.status) === 2) {
        await api.pause(item.id);
        setNotice('Đã tạm dừng khuyến mãi.');
      } else {
        await api.activate(item.id);
        setNotice('Đã kích hoạt khuyến mãi.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái.');
    }
  }

  const activeCount = items.filter((item) => Number(item.status) === 2 && !item.isDeleted).length;
  const tenantScopedCount = items.filter((item) => Number(item.ownerScope) === 2).length;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <div className="rounded-xl border border-slate-100 bg-white p-5 shadow-sm">
          <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Campaign</p>
          <p className="mt-1 text-2xl font-black text-slate-900">{items.length}</p>
        </div>
        <div className="rounded-xl border border-emerald-100 bg-emerald-50 p-5 shadow-sm">
          <p className="text-[10px] font-black uppercase tracking-widest text-emerald-600">Đang chạy</p>
          <p className="mt-1 text-2xl font-black text-emerald-700">{activeCount}</p>
        </div>
        <div className="rounded-xl border border-blue-100 bg-blue-50 p-5 shadow-sm">
          <p className="text-[10px] font-black uppercase tracking-widest text-blue-600">Tenant scope</p>
          <p className="mt-1 text-2xl font-black text-blue-700">{tenantScopedCount}</p>
        </div>
      </div>

      {error ? <div className="rounded-xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-700">{error}</div> : null}
      {notice ? <div className="rounded-xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">{notice}</div> : null}

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <div className="overflow-hidden rounded-xl border border-slate-100 bg-white shadow-sm">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
            <div>
              <h2 className="font-black text-slate-900">Danh sách khuyến mãi</h2>
              <p className="mt-0.5 text-xs font-bold text-slate-400">Coupon dùng thật từ backend promo engine.</p>
            </div>
            <button type="button" onClick={loadData} className="rounded-xl border border-slate-200 bg-white p-3 text-slate-500">
              <RefreshCw size={16} />
            </button>
          </div>

          <div className="max-h-[720px] divide-y divide-slate-50 overflow-y-auto">
            {loading ? (
              <div className="flex items-center gap-2 px-5 py-8 text-sm font-bold text-slate-400">
                <Loader2 size={16} className="animate-spin" />
                Đang tải khuyến mãi...
              </div>
            ) : items.length === 0 ? (
              <div className="px-5 py-8 text-sm font-bold text-slate-400">Chưa có khuyến mãi nào.</div>
            ) : items.map((item) => (
              <div key={item.id} className={`px-5 py-4 transition hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}>
                <button type="button" onClick={() => handleSelect(item)} className="w-full text-left">
                  <div className="flex items-start gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-100 text-blue-600">
                      <BadgePercent size={18} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="font-black text-slate-900">{item.code}</p>
                        <span className="rounded-lg bg-slate-100 px-2 py-1 text-[10px] font-black uppercase text-slate-500">{STATUS[Number(item.status)] || item.status}</span>
                        {item.isDeleted ? <span className="rounded-lg bg-rose-50 px-2 py-1 text-[10px] font-black uppercase text-rose-600">Đã ẩn</span> : null}
                      </div>
                      <p className="mt-1 text-sm font-bold text-slate-600">{item.name}</p>
                      <p className="mt-1 text-xs font-bold text-slate-400">
                        {item.tenantName || 'Toàn sàn'} · {DISCOUNT_TYPE[Number(item.discountType)]}: {Number(item.discountType) === 2 ? `${item.discountValue}%` : formatCurrency(item.discountValue || 0, item.currencyCode || 'VND')}
                      </p>
                      <p className="mt-1 text-xs font-bold text-slate-400">
                        Đã dùng {item.redemptionCount || 0} · Discount {formatCurrency(item.discountGrantedAmount || 0, item.currencyCode || 'VND')}
                      </p>
                    </div>
                  </div>
                </button>
                <div className="mt-3 flex flex-wrap gap-2">
                  <button type="button" onClick={() => toggleActive(item)} className="rounded-lg bg-slate-900 px-3 py-2 text-[10px] font-black uppercase text-white">
                    {Number(item.status) === 2 ? 'Tạm dừng' : 'Kích hoạt'}
                  </button>
                  <button type="button" onClick={() => toggleDeleted(item)} className="rounded-lg bg-slate-100 px-3 py-2 text-[10px] font-black uppercase text-slate-600">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
          <div className="mb-5 flex items-start justify-between gap-4">
            <div>
              <h2 className="font-black text-slate-900">{selectedId ? 'Cập nhật khuyến mãi' : 'Tạo khuyến mãi'}</h2>
              <p className="mt-1 text-xs font-bold text-slate-400">
                Tenant chỉ tạo campaign trong tenant của mình; admin có thể tạo toàn sàn hoặc tenant scope.
              </p>
            </div>
            <button type="button" onClick={handleNew} className="inline-flex items-center gap-2 rounded-xl bg-slate-100 px-4 py-3 text-xs font-black text-slate-700">
              <Plus size={14} />
              Mới
            </button>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            {mode === 'admin' ? (
              <>
                <select value={form.ownerScope} onChange={(event) => setForm((current) => ({ ...current, ownerScope: event.target.value, tenantId: event.target.value === '1' ? '' : current.tenantId }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
                  <option value="1">Toàn sàn</option>
                  <option value="2">Tenant riêng</option>
                </select>
                <select value={form.tenantId} disabled={form.ownerScope !== '2'} onChange={(event) => setForm((current) => ({ ...current, tenantId: event.target.value }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none disabled:opacity-60">
                  <option value="">Chọn tenant</option>
                  {tenants.map((tenant) => (
                    <option key={tenant.id} value={tenant.id}>{tenant.code} - {tenant.name}</option>
                  ))}
                </select>
              </>
            ) : null}

            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã coupon" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên khuyến mãi" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <select value={form.productScope} onChange={(event) => setForm((current) => ({ ...current, productScope: event.target.value }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              {PRODUCT_SCOPE.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="1">Nháp</option>
              <option value="2">Đang chạy</option>
              <option value="3">Tạm dừng</option>
              <option value="4">Hết hạn</option>
            </select>
            <select value={form.discountType} onChange={(event) => setForm((current) => ({ ...current, discountType: event.target.value }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="2">Giảm theo %</option>
              <option value="1">Giảm số tiền</option>
            </select>
            <input type="number" value={form.discountValue} onChange={(event) => setForm((current) => ({ ...current, discountValue: event.target.value }))} placeholder="Giá trị giảm" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="number" value={form.maxDiscountAmount} onChange={(event) => setForm((current) => ({ ...current, maxDiscountAmount: event.target.value }))} placeholder="Giảm tối đa" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="number" value={form.minOrderAmount} onChange={(event) => setForm((current) => ({ ...current, minOrderAmount: event.target.value }))} placeholder="Đơn tối thiểu" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input value={form.currencyCode} onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))} placeholder="Tiền tệ" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="datetime-local" value={form.startsAt} onChange={(event) => setForm((current) => ({ ...current, startsAt: event.target.value }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="datetime-local" value={form.endsAt} onChange={(event) => setForm((current) => ({ ...current, endsAt: event.target.value }))} className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="number" value={form.globalUsageLimit} onChange={(event) => setForm((current) => ({ ...current, globalUsageLimit: event.target.value }))} placeholder="Giới hạn toàn chiến dịch" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="number" value={form.perUserUsageLimit} onChange={(event) => setForm((current) => ({ ...current, perUserUsageLimit: event.target.value }))} placeholder="Giới hạn mỗi user" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="number" value={form.perTenantUsageLimit} onChange={(event) => setForm((current) => ({ ...current, perTenantUsageLimit: event.target.value }))} placeholder="Giới hạn mỗi tenant" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            <input type="number" value={form.budgetAmount} onChange={(event) => setForm((current) => ({ ...current, budgetAmount: event.target.value }))} placeholder="Ngân sách giảm giá" className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
          </div>

          <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Ghi chú nội bộ hoặc mô tả ngắn" className="mt-4 w-full resize-none rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />

          <div className="mt-4 flex flex-wrap gap-4">
            <label className="flex items-center gap-2 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={form.isPublic} onChange={(event) => setForm((current) => ({ ...current, isPublic: event.target.checked }))} />
              Public
            </label>
            <label className="flex items-center gap-2 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={form.requiresCode} onChange={(event) => setForm((current) => ({ ...current, requiresCode: event.target.checked }))} />
              Cần nhập mã
            </label>
          </div>

          <button type="submit" disabled={saving} className="mt-6 rounded-xl bg-slate-900 px-6 py-4 text-sm font-black text-white disabled:opacity-60">
            {saving ? 'Đang lưu...' : selectedId ? 'Cập nhật khuyến mãi' : 'Tạo khuyến mãi'}
          </button>
        </form>
      </div>
    </div>
  );
}
