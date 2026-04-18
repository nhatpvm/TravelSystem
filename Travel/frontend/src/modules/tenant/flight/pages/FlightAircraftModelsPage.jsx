import React, { useEffect, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminFlightAircraftModel,
  createFlightAircraftModel,
  deleteAdminFlightAircraftModel,
  deleteFlightAircraftModel,
  listAdminFlightAircraftModels,
  listFlightAircraftModels,
  restoreAdminFlightAircraftModel,
  restoreFlightAircraftModel,
  updateAdminFlightAircraftModel,
  updateFlightAircraftModel,
} from '../../../../services/flightService';

function createEmptyForm() {
  return {
    code: '',
    manufacturer: '',
    model: '',
    typicalSeatCapacity: '',
    metadataJson: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    code: item.code || '',
    manufacturer: item.manufacturer || '',
    model: item.model || '',
    typicalSeatCapacity: item.typicalSeatCapacity ?? '',
    metadataJson: item.metadataJson || '',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    code: form.code.trim(),
    manufacturer: form.manufacturer.trim(),
    model: form.model.trim(),
    typicalSeatCapacity: form.typicalSeatCapacity === '' ? null : Number(form.typicalSeatCapacity),
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
  };
}

export default function FlightAircraftModelsPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightAircraftModels(params, tenantId) : listFlightAircraftModels;
  const createFn = isAdmin ? (payload) => createAdminFlightAircraftModel(payload, tenantId) : createFlightAircraftModel;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightAircraftModel(id, payload, tenantId) : updateFlightAircraftModel;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightAircraftModel(id, tenantId) : deleteFlightAircraftModel;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightAircraftModel(id, tenantId) : restoreFlightAircraftModel;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listFn({ includeDeleted: true, pageSize: 100 });
      const nextItems = Array.isArray(response?.items) ? response.items : [];
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm(createEmptyForm());
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải mẫu tàu bay.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);
      if (selectedId) {
        await updateFn(selectedId, payload);
        setNotice('Đã cập nhật mẫu tàu bay.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo mẫu tàu bay mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được mẫu tàu bay.');
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
        setNotice('Đã khôi phục mẫu tàu bay.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn mẫu tàu bay.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái mẫu tàu bay.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="aircraft-models"
      title="Mẫu tàu bay"
      subtitle="Lớp dữ liệu chuẩn cho đội bay, seat map và cấu hình cabin của tenant hàng không."
      notice={notice}
      error={error}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={() => { setSelectedId(''); setForm(createEmptyForm()); setNotice(''); }} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm mẫu bay
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách mẫu tàu bay</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn model để cập nhật hoặc khôi phục.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải mẫu tàu bay...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có mẫu tàu bay nào.</div>
            ) : items.map((item) => (
              <div key={item.id} role="button" tabIndex={0} onClick={() => { setSelectedId(item.id); setForm(hydrateForm(item)); }} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); setForm(hydrateForm(item)); } }} className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.manufacturer} {item.model}</p>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.code}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.typicalSeatCapacity || 0} ghế tiêu chuẩn</p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật mẫu tàu bay' : 'Tạo mẫu tàu bay mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Thông tin này là nền cho cấu hình seat map và đội bay thực tế.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã model" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.typicalSeatCapacity} onChange={(event) => setForm((current) => ({ ...current, typicalSeatCapacity: event.target.value }))} placeholder="Sức chứa tiêu chuẩn" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.manufacturer} onChange={(event) => setForm((current) => ({ ...current, manufacturer: event.target.value }))} placeholder="Nhà sản xuất" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.model} onChange={(event) => setForm((current) => ({ ...current, model: event.target.value }))} placeholder="Model" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <textarea value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} rows={6} placeholder="Metadata JSON" className="md:col-span-2 w-full rounded-[1.8rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Cho phép dùng cho đội bay thực tế
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật model' : 'Tạo model')}
            </button>
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
