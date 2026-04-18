import React, { useEffect, useMemo, useState } from 'react';
import { ClipboardList, Plus, RefreshCw } from 'lucide-react';
import AdminHotelPageShell from '../hotel/components/AdminHotelPageShell';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';
import {
  createAdminRoomTypePolicy,
  deleteAdminRoomTypePolicy,
  getAdminHotelOptions,
  getAdminRoomTypePolicy,
  listAdminRoomTypePolicies,
  restoreAdminRoomTypePolicy,
  updateAdminRoomTypePolicy,
} from '../../../services/hotelService';

function createEmptyForm(roomTypeId = '') {
  return {
    roomTypeId,
    policyJson: '{}',
    isActive: true,
  };
}

function hydrateForm(detail) {
  return {
    roomTypeId: detail.roomTypeId || '',
    policyJson: detail.policyJson || '{}',
    isActive: detail.isActive ?? true,
    rowVersionBase64: detail.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    roomTypeId: form.roomTypeId,
    policyJson: form.policyJson.trim() || '{}',
    isActive: !!form.isActive,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function AdminRoomTypePoliciesPage() {
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
  const [roomTypes, setRoomTypes] = useState([]);
  const [selectedRoomTypeId, setSelectedRoomTypeId] = useState('');
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm());

  async function loadData() {
    if (!tenantId) {
      setRoomTypes([]);
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
        listAdminRoomTypePolicies({ includeDeleted: true, pageSize: 100 }, tenantId),
      ]);

      const nextRoomTypes = Array.isArray(optionsResponse?.roomTypes) ? optionsResponse.roomTypes : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      const nextRoomTypeId = selectedRoomTypeId || nextRoomTypes[0]?.id || '';

      setRoomTypes(nextRoomTypes);
      setItems(nextItems);
      setSelectedRoomTypeId(nextRoomTypeId);

      if (!selectedId) {
        setForm(createEmptyForm(nextRoomTypeId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải policy hạng phòng.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [tenantId]);

  const roomTypeLookup = useMemo(
    () => Object.fromEntries(roomTypes.map((item) => [item.id, item])),
    [roomTypes],
  );

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedRoomTypeId || item.roomTypeId === selectedRoomTypeId),
    [items, selectedRoomTypeId],
  );

  async function loadDetail(id) {
    try {
      const detail = await getAdminRoomTypePolicy(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setSelectedRoomTypeId(detail.roomTypeId || selectedRoomTypeId);
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết policy hạng phòng.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm(selectedRoomTypeId || roomTypes[0]?.id || ''));
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
        await updateAdminRoomTypePolicy(selectedId, payload, tenantId);
        setNotice('Đã cập nhật policy hạng phòng.');
      } else {
        await createAdminRoomTypePolicy(payload, tenantId);
        setNotice('Đã tạo policy hạng phòng mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu policy hạng phòng.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreAdminRoomTypePolicy(item.id, tenantId);
        setNotice('Đã khôi phục policy hạng phòng.');
      } else {
        await deleteAdminRoomTypePolicy(item.id, tenantId);
        setNotice('Đã ẩn policy hạng phòng.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái policy hạng phòng.');
    }
  }

  return (
    <AdminHotelPageShell
      pageKey="room-type-policies"
      title="Policy hạng phòng"
      subtitle="Admin quản lý policy JSON riêng cho từng room type như phụ thu, nội quy và điều kiện đặc thù."
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
            Tạo policy
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách policy</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mỗi room type có thể có một policy riêng để quản trị linh hoạt hơn.</p>
            </div>
            <select value={selectedRoomTypeId} onChange={(event) => setSelectedRoomTypeId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Tất cả hạng phòng</option>
              {roomTypes.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="divide-y divide-slate-50 max-h-[720px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải policy hạng phòng...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có policy hạng phòng nào.</div>
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
                      <ClipboardList size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{roomTypeLookup[item.roomTypeId]?.name || 'Policy hạng phòng'}</p>
                        {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">Tạm ngưng</span> : null}
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2 line-clamp-2">{item.policyJson || '{}'}</p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật policy hạng phòng' : 'Tạo policy hạng phòng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Trang này giữ JSON raw để admin có thể bám sát contract backend hiện tại.</p>
          </div>

          <select value={form.roomTypeId} onChange={(event) => setForm((current) => ({ ...current, roomTypeId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn hạng phòng</option>
            {roomTypes.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <textarea value={form.policyJson} onChange={(event) => setForm((current) => ({ ...current, policyJson: event.target.value }))} rows={14} placeholder='{"minStay":1,"allowPets":false}' className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Kích hoạt
          </label>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : selectedId ? 'Cập nhật policy' : 'Tạo policy'}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </AdminHotelPageShell>
  );
}
