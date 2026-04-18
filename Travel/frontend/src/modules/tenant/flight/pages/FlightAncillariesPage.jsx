import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminManagedFlightAncillary,
  createManagedFlightAncillary,
  deleteAdminManagedFlightAncillary,
  deleteManagedFlightAncillary,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightAncillaries,
  listFlightAncillaries,
  restoreAdminManagedFlightAncillary,
  restoreManagedFlightAncillary,
  updateAdminManagedFlightAncillary,
  updateManagedFlightAncillary,
} from '../../../../services/flightService';
import {
  ANCILLARY_TYPE_OPTIONS,
  formatCurrency,
  getAncillaryTypeLabel,
  parseEnumOptionValue,
} from '../utils/presentation';

function createEmptyForm() {
  return {
    airlineId: '',
    code: '',
    name: '',
    type: 99,
    currencyCode: 'VND',
    price: '0',
    rulesJson: '{\n  "note": "Áp dụng theo điều kiện hãng"\n}',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    airlineId: item.airlineId || '',
    code: item.code || '',
    name: item.name || '',
    type: parseEnumOptionValue(ANCILLARY_TYPE_OPTIONS, item.type, 99),
    currencyCode: item.currencyCode || 'VND',
    price: String(item.price ?? 0),
    rulesJson: item.rulesJson || '{}',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    airlineId: form.airlineId,
    code: form.code.trim().toUpperCase(),
    name: form.name.trim(),
    type: Number(form.type),
    currencyCode: String(form.currencyCode || 'VND').trim().toUpperCase(),
    price: Number(form.price || 0),
    rulesJson: form.rulesJson?.trim() || null,
    isActive: !!form.isActive,
  };
}

export default function FlightAncillariesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [airlines, setAirlines] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightAncillaries(params, tenantId) : listFlightAncillaries;
  const createFn = isAdmin ? (payload) => createAdminManagedFlightAncillary(payload, tenantId) : createManagedFlightAncillary;
  const updateFn = isAdmin ? (id, payload) => updateAdminManagedFlightAncillary(id, payload, tenantId) : updateManagedFlightAncillary;
  const deleteFn = isAdmin ? (id) => deleteAdminManagedFlightAncillary(id, tenantId) : deleteManagedFlightAncillary;
  const restoreFn = isAdmin ? (id) => restoreAdminManagedFlightAncillary(id, tenantId) : restoreManagedFlightAncillary;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setAirlines([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, itemsResponse] = await Promise.all([
        isAdmin ? getAdminFlightOptions(tenantId) : getFlightManagerOptions(),
        listFn({ includeDeleted: true, pageSize: 100 }),
      ]);

      const nextAirlines = Array.isArray(optionsResponse?.airlines) ? optionsResponse.airlines : [];
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setAirlines(nextAirlines);
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          airlineId: nextAirlines[0]?.id || '',
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dịch vụ thêm.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  const airlineLookup = useMemo(
    () => Object.fromEntries(airlines.map((item) => [item.id, item])),
    [airlines],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      airlineId: airlines[0]?.id || '',
    });
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
        await updateFn(selectedId, payload);
        setNotice('Đã cập nhật dịch vụ thêm.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo dịch vụ thêm mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được dịch vụ thêm.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreFn(item.id);
        setNotice('Đã khôi phục dịch vụ thêm.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn dịch vụ thêm.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái dịch vụ thêm.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="ancillaries"
      title="Dịch vụ thêm"
      subtitle="Quản lý hành lý, suất ăn, bảo hiểm và các quyền lợi mua thêm để tăng doanh thu cho marketplace."
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
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách dịch vụ thêm</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Mỗi dịch vụ gắn với một hãng bay và có thể hiển thị cho offer phù hợp.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải dịch vụ thêm...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có dịch vụ thêm nào.</div>
            ) : items.map((item) => (
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
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.name}</p>
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                        {getAncillaryTypeLabel(item.type)}
                      </span>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.code} · {airlineLookup[item.airlineId]?.name || 'Chưa gắn hãng bay'}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{formatCurrency(item.price, item.currencyCode)}</p>
                  </div>
                  <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-6">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật dịch vụ thêm' : 'Tạo dịch vụ thêm mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Dịch vụ thêm cần rõ loại, giá và rules để frontend public trình bày chính xác.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.airlineId} onChange={(event) => setForm((current) => ({ ...current, airlineId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn hãng bay</option>
              {airlines.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã dịch vụ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên dịch vụ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <select value={form.type} onChange={(event) => setForm((current) => ({ ...current, type: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              {ANCILLARY_TYPE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <input value={form.currencyCode} onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))} placeholder="Mã tiền tệ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="0" step="1000" value={form.price} onChange={(event) => setForm((current) => ({ ...current, price: event.target.value }))} placeholder="Giá bán" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <textarea value={form.rulesJson} onChange={(event) => setForm((current) => ({ ...current, rulesJson: event.target.value }))} rows={10} className="md:col-span-2 w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none font-mono" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Cho phép hiển thị trong flow public
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật dịch vụ' : 'Tạo dịch vụ')}
            </button>
            {selectedId ? (
              <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">
                Tạo bản mới
              </button>
            ) : null}
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
