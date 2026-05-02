import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import FlightModeShell from '../components/FlightModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminFlightFareRule,
  createFlightFareRule,
  deleteAdminFlightFareRule,
  deleteFlightFareRule,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightFareRules,
  listFlightFareRules,
  restoreAdminFlightFareRule,
  restoreFlightFareRule,
  updateAdminFlightFareRule,
  updateFlightFareRule,
} from '../../../../services/flightService';

function createEmptyForm() {
  return {
    fareClassId: '',
    isActive: true,
    rulesJson: '{\n  "refund": "Theo điều kiện của hãng",\n  "change": "Cho phép đổi nếu còn chỗ"\n}',
  };
}

function hydrateForm(item) {
  return {
    fareClassId: item.fareClassId || '',
    isActive: item.isActive ?? true,
    rulesJson: item.rulesJson || '{}',
  };
}

function buildPayload(form) {
  return {
    fareClassId: form.fareClassId,
    isActive: !!form.isActive,
    rulesJson: form.rulesJson?.trim() || '{}',
  };
}

export default function FlightFareRulesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [searchParams] = useSearchParams();
  const [fareClasses, setFareClasses] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedFareClassId, setSelectedFareClassId] = useState(searchParams.get('fareClassId') || '');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightFareRules(params, tenantId) : listFlightFareRules;
  const createFn = isAdmin ? (payload) => createAdminFlightFareRule(payload, tenantId) : createFlightFareRule;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightFareRule(id, payload, tenantId) : updateFlightFareRule;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightFareRule(id, tenantId) : deleteFlightFareRule;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightFareRule(id, tenantId) : restoreFlightFareRule;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setFareClasses([]);
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
        listFn({
          includeDeleted: true,
          pageSize: 100,
          fareClassId: selectedFareClassId || undefined,
        }),
      ]);

      const nextFareClasses = Array.isArray(optionsResponse?.fareClasses) ? optionsResponse.fareClasses : [];
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setFareClasses(nextFareClasses);
      setItems(nextItems);

      if (!selectedFareClassId && nextFareClasses.length > 0) {
        setSelectedFareClassId(nextFareClasses[0].id);
      }

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          fareClassId: selectedFareClassId || nextFareClasses[0]?.id || '',
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải điều kiện vé.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, tenantId, selectedFareClassId, loadDataRef]);

  const fareClassLookup = useMemo(
    () => Object.fromEntries(fareClasses.map((item) => [item.id, item])),
    [fareClasses],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      fareClassId: selectedFareClassId || fareClasses[0]?.id || '',
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
        setNotice('Đã cập nhật điều kiện vé.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo điều kiện vé mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được điều kiện vé.');
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
        setNotice('Đã khôi phục điều kiện vé.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn điều kiện vé.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái điều kiện vé.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="fare-rules"
      title="Điều kiện vé"
      subtitle="Mỗi hạng vé có một bộ điều kiện riêng để public detail và checkout hiển thị đúng chính sách hoàn, đổi vé, hành lý."
      notice={notice}
      error={error}
      actions={(
        <>
          <div className="px-4 py-3 rounded-2xl border border-slate-200 bg-white">
            <select
              value={selectedFareClassId}
              onChange={(event) => setSelectedFareClassId(event.target.value)}
              className="bg-transparent text-sm font-bold text-slate-700 outline-none"
            >
              {fareClasses.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm điều kiện
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách điều kiện vé</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn một rule để chỉnh sửa hoặc khôi phục.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải điều kiện vé...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có điều kiện vé nào.</div>
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
                      <p className="font-black text-slate-900">{fareClassLookup[item.fareClassId]?.name || 'Hạng vé chưa xác định'}</p>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {fareClassLookup[item.fareClassId]?.code || '---'} · Cập nhật gần nhất: {item.updatedAt || item.createdAt ? new Date(item.updatedAt || item.createdAt).toLocaleDateString('vi-VN') : '--'}
                    </p>
                    <p className="text-xs font-bold text-slate-400 mt-1 line-clamp-2">{item.rulesJson}</p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật điều kiện vé' : 'Tạo điều kiện vé mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Có thể lưu điều kiện giá chi tiết để kênh bán hiển thị linh hoạt hơn.</p>
          </div>
          <div className="space-y-5">
            <select value={form.fareClassId} onChange={(event) => setForm((current) => ({ ...current, fareClassId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn hạng vé</option>
              {fareClasses.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <textarea value={form.rulesJson} onChange={(event) => setForm((current) => ({ ...current, rulesJson: event.target.value }))} rows={16} className="w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none font-mono" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Áp dụng điều kiện vé này trên frontend public
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật điều kiện vé' : 'Tạo điều kiện vé')}
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
