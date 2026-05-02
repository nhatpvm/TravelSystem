import React, { useEffect, useMemo, useState } from 'react';
import { Image, Plus, RefreshCw } from 'lucide-react';
import AdminImageUploadField from '../components/AdminImageUploadField';
import AdminHotelPageShell from '../hotel/components/AdminHotelPageShell';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';
import {
  createAdminHotelImage,
  deleteAdminHotelImage,
  getAdminHotelImage,
  getAdminHotelOptions,
  listAdminHotelImages,
  restoreAdminHotelImage,
  setAdminHotelImagePrimary,
  updateAdminHotelImage,
} from '../../../services/hotelService';
import { uploadAdminImage } from '../../../services/adminUploadService';
import useLatestRef from '../../../shared/hooks/useLatestRef';

function createEmptyForm(hotelId = '') {
  return {
    hotelId,
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
    hotelId: detail.hotelId || '',
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
    hotelId: form.hotelId,
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

export default function AdminHotelImagesPage() {
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
  const [hotels, setHotels] = useState([]);
  const [selectedHotelId, setSelectedHotelId] = useState('');
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm());
  const [uploadingImage, setUploadingImage] = useState(false);

  async function loadData() {
    if (!tenantId) {
      setHotels([]);
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
        listAdminHotelImages({ includeDeleted: true, pageSize: 100 }, tenantId),
      ]);

      const nextHotels = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      const nextHotelId = selectedHotelId || nextHotels[0]?.id || '';

      setHotels(nextHotels);
      setItems(nextItems);
      setSelectedHotelId(nextHotelId);

      if (!selectedId) {
        setForm(createEmptyForm(nextHotelId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải ảnh khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [loadDataRef, tenantId]);

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [items, selectedHotelId],
  );

  async function loadDetail(id) {
    try {
      const detail = await getAdminHotelImage(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setSelectedHotelId(detail.hotelId || selectedHotelId);
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết ảnh khách sạn.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm(selectedHotelId || hotels[0]?.id || ''));
    setNotice('');
  }

  async function handleUploadImage(file) {
    setUploadingImage(true);
    setError('');
    setNotice('');

    try {
      const response = await uploadAdminImage(file, {
        scope: 'hotel-image',
        tenantId,
      });

      setForm((current) => ({
        ...current,
        imageUrl: response?.url || '',
      }));
      setNotice('Đã tải ảnh khách sạn từ máy.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải ảnh khách sạn lên.');
    } finally {
      setUploadingImage(false);
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);
      if (selectedId) {
        await updateAdminHotelImage(selectedId, payload, tenantId);
        setNotice('Đã cập nhật ảnh khách sạn.');
      } else {
        await createAdminHotelImage(payload, tenantId);
        setNotice('Đã tạo ảnh khách sạn mới.');
      }

      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu ảnh khách sạn.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreAdminHotelImage(item.id, tenantId);
        setNotice('Đã khôi phục ảnh khách sạn.');
      } else {
        await deleteAdminHotelImage(item.id, tenantId);
        setNotice('Đã ẩn ảnh khách sạn.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái ảnh.');
    }
  }

  async function handleSetPrimary(item) {
    try {
      await setAdminHotelImagePrimary(item.id, tenantId);
      setNotice('Đã cập nhật ảnh đại diện.');
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể đặt ảnh đại diện.');
    }
  }

  return (
    <AdminHotelPageShell
      pageKey="images"
      title="Ảnh khách sạn"
      subtitle="Admin quản lý thư viện ảnh đại diện, caption và alt text cho từng khách sạn."
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
              <p className="text-lg font-black text-slate-900">Thư viện ảnh</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Kiểm soát ảnh đại diện, caption và alt text để public page hiển thị chuẩn.</p>
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
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải thư viện ảnh...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Tenant này chưa có ảnh khách sạn nào.</div>
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
                      {item.imageUrl ? <img src={item.imageUrl} alt={item.title || item.caption || 'Ảnh khách sạn'} className="w-full h-full object-cover" /> : <Image size={20} />}
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{item.title || item.caption || 'Ảnh khách sạn'}</p>
                        {item.isPrimary ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-blue-100 text-blue-700">Đại diện</span> : null}
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2 line-clamp-2">{item.imageUrl || 'Chưa có URL ảnh'}</p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật ảnh khách sạn' : 'Tạo ảnh khách sạn mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Giữ đúng alt text và caption để vừa SEO vừa không làm lệch gallery public.</p>
          </div>

          <select value={form.hotelId} onChange={(event) => setForm((current) => ({ ...current, hotelId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn khách sạn</option>
            {hotels.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <AdminImageUploadField
            label="Ảnh khách sạn"
            value={form.imageUrl}
            onChange={(value) => setForm((current) => ({ ...current, imageUrl: value }))}
            onUpload={handleUploadImage}
            uploading={uploadingImage}
            placeholder="URL ảnh"
            helperText="Có thể dán URL sẵn có hoặc tải ảnh trực tiếp từ máy."
            previewAlt={form.altText || form.title || 'Preview'}
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} placeholder="Tiêu đề ảnh" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.altText} onChange={(event) => setForm((current) => ({ ...current, altText: event.target.value }))} placeholder="Alt text" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.caption} onChange={(event) => setForm((current) => ({ ...current, caption: event.target.value }))} placeholder="Caption" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} placeholder="Thứ tự" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Ghi chú hình ảnh nội bộ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />

          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={form.isPrimary} onChange={(event) => setForm((current) => ({ ...current, isPrimary: event.target.checked }))} />
              Ảnh đại diện
            </label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
              Kích hoạt
            </label>
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
