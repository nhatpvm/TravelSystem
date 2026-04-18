import React, { useEffect, useMemo, useState } from 'react';
import { Images, Plus, RefreshCw } from 'lucide-react';
import AdminHotelPageShell from '../hotel/components/AdminHotelPageShell';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';
import {
  createAdminRoomTypeImage,
  deleteAdminRoomTypeImage,
  getAdminHotelOptions,
  getAdminRoomTypeImage,
  listAdminRoomTypeImages,
  restoreAdminRoomTypeImage,
  setAdminRoomTypeImagePrimary,
  updateAdminRoomTypeImage,
} from '../../../services/hotelService';

function createEmptyForm(roomTypeId = '') {
  return {
    roomTypeId,
    imageUrl: '',
    caption: '',
    altText: '',
    title: '',
    isPrimary: false,
    sortOrder: 0,
    metadataJson: '',
    isActive: true,
  };
}

function hydrateForm(detail) {
  return {
    roomTypeId: detail.roomTypeId || '',
    imageUrl: detail.imageUrl || '',
    caption: detail.caption || '',
    altText: detail.altText || '',
    title: detail.title || '',
    isPrimary: !!detail.isPrimary,
    sortOrder: detail.sortOrder ?? 0,
    metadataJson: detail.metadataJson || '',
    isActive: detail.isActive ?? true,
    rowVersionBase64: detail.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    roomTypeId: form.roomTypeId,
    imageUrl: form.imageUrl.trim() || null,
    caption: form.caption.trim() || null,
    altText: form.altText.trim() || null,
    title: form.title.trim() || null,
    isPrimary: !!form.isPrimary,
    sortOrder: Number(form.sortOrder || 0),
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function AdminRoomTypeImagesPage() {
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
        listAdminRoomTypeImages({ includeDeleted: true, pageSize: 100 }, tenantId),
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
      setError(requestError.message || 'Không thể tải ảnh hạng phòng.');
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
      const detail = await getAdminRoomTypeImage(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setSelectedRoomTypeId(detail.roomTypeId || selectedRoomTypeId);
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết ảnh hạng phòng.');
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
        await updateAdminRoomTypeImage(selectedId, payload, tenantId);
        setNotice('Đã cập nhật ảnh hạng phòng.');
      } else {
        await createAdminRoomTypeImage(payload, tenantId);
        setNotice('Đã tạo ảnh hạng phòng mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu ảnh hạng phòng.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreAdminRoomTypeImage(item.id, tenantId);
        setNotice('Đã khôi phục ảnh hạng phòng.');
      } else {
        await deleteAdminRoomTypeImage(item.id, tenantId);
        setNotice('Đã ẩn ảnh hạng phòng.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái ảnh hạng phòng.');
    }
  }

  async function handleSetPrimary(item) {
    try {
      await setAdminRoomTypeImagePrimary(item.id, tenantId);
      setNotice('Đã cập nhật ảnh đại diện cho hạng phòng.');
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể đặt ảnh đại diện hạng phòng.');
    }
  }

  return (
    <AdminHotelPageShell
      pageKey="room-type-images"
      title="Ảnh hạng phòng"
      subtitle="Admin quản lý gallery ảnh theo từng room type để trang chi tiết khách sạn hiển thị đúng bộ ảnh."
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
            Thêm ảnh
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Gallery theo hạng phòng</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Lọc theo room type để kiểm soát ảnh đại diện và thứ tự hiển thị.</p>
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
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải ảnh hạng phòng...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có ảnh hạng phòng nào.</div>
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
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center overflow-hidden">
                      {item.imageUrl ? <img src={item.imageUrl} alt={item.title || item.caption || 'Ảnh hạng phòng'} className="w-full h-full object-cover" /> : <Images size={20} />}
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{item.title || item.caption || roomTypeLookup[item.roomTypeId]?.name || 'Ảnh hạng phòng'}</p>
                        {item.isPrimary ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-blue-100 text-blue-700">Đại diện</span> : null}
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">{roomTypeLookup[item.roomTypeId]?.name || 'Chưa rõ hạng phòng'}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button type="button" onClick={(event) => { event.stopPropagation(); handleSetPrimary(item); }} className="px-3 py-2 rounded-xl bg-blue-50 text-[10px] font-black uppercase tracking-widest text-blue-700">
                      Đặt đại diện
                    </button>
                    <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật ảnh hạng phòng' : 'Tạo ảnh hạng phòng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Ảnh hạng phòng nên khác ảnh khách sạn để gallery public không bị lặp.</p>
          </div>

          <select value={form.roomTypeId} onChange={(event) => setForm((current) => ({ ...current, roomTypeId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn hạng phòng</option>
            {roomTypes.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <input value={form.imageUrl} onChange={(event) => setForm((current) => ({ ...current, imageUrl: event.target.value }))} placeholder="URL ảnh" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          {form.imageUrl ? (
            <div className="rounded-[2rem] border border-slate-100 bg-slate-50 p-4">
              <img src={form.imageUrl} alt={form.altText || form.title || 'Preview'} className="h-52 w-full rounded-[1.5rem] object-cover" />
            </div>
          ) : null}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} placeholder="Tiêu đề ảnh" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.altText} onChange={(event) => setForm((current) => ({ ...current, altText: event.target.value }))} placeholder="Alt text" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.caption} onChange={(event) => setForm((current) => ({ ...current, caption: event.target.value }))} placeholder="Caption" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} placeholder="Thứ tự" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />

          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.isPrimary} onChange={(event) => setForm((current) => ({ ...current, isPrimary: event.target.checked }))} /> Ảnh đại diện</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /> Kích hoạt</label>
          </div>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : selectedId ? 'Cập nhật ảnh' : 'Tạo ảnh'}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </AdminHotelPageShell>
  );
}
