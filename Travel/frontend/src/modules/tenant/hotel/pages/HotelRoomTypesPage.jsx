import React, { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Plus, RefreshCw } from 'lucide-react';
import HotelModeShell from '../components/HotelModeShell';
import {
  createAdminRoomType,
  createManagedRoomType,
  deleteAdminRoomType,
  deleteManagedRoomType,
  getAdminHotelOptions,
  getAdminRoomType,
  getHotelManagerOptions,
  getManagedRoomType,
  listAdminRoomTypes,
  listManagedRoomTypes,
  restoreAdminRoomType,
  restoreManagedRoomType,
  updateAdminRoomType,
  updateManagedRoomType,
} from '../../../../services/hotelService';
import {
  getRoomTypeStatusLabel,
  getStatusClass,
  parseEnumOptionValue,
  readJsonInput,
  ROOM_TYPE_STATUS_OPTIONS,
  toPrettyJson,
} from '../utils/presentation';
import { uploadManagerImage } from '../../../../services/portalUploadService';
import ImageUploadField from '../../../../shared/components/forms/ImageUploadField';

function createEmptyForm(hotelId = '') {
  return {
    hotelId,
    code: '',
    name: '',
    descriptionMarkdown: '',
    areaSquareMeters: '',
    defaultAdults: 2,
    defaultChildren: 0,
    maxAdults: 2,
    maxChildren: 1,
    maxGuests: 3,
    totalUnits: 10,
    hasBalcony: false,
    hasWindow: true,
    smokingAllowed: false,
    coverImageUrl: '',
    status: 2,
    isActive: true,
    metadataJson: '',
    bedsJson: '[]',
    amenitiesJson: '[]',
    occupancyRulesJson: '[]',
  };
}

function hydrateForm(item) {
  return {
    hotelId: item.hotelId || '',
    code: item.code || '',
    name: item.name || '',
    descriptionMarkdown: item.descriptionMarkdown || '',
    areaSquareMeters: item.areaSquareMeters ?? '',
    defaultAdults: item.defaultAdults ?? 2,
    defaultChildren: item.defaultChildren ?? 0,
    maxAdults: item.maxAdults ?? 2,
    maxChildren: item.maxChildren ?? 0,
    maxGuests: item.maxGuests ?? 2,
    totalUnits: item.totalUnits ?? 0,
    hasBalcony: !!item.hasBalcony,
    hasWindow: item.hasWindow !== false,
    smokingAllowed: !!item.smokingAllowed,
    coverImageUrl: item.coverImageUrl || '',
    status: parseEnumOptionValue(ROOM_TYPE_STATUS_OPTIONS, item.status, 2),
    isActive: item.isActive ?? true,
    metadataJson: item.metadataJson || '',
    bedsJson: toPrettyJson(item.beds || []),
    amenitiesJson: toPrettyJson(item.amenities || []),
    occupancyRulesJson: toPrettyJson(item.occupancyRules || []),
    rowVersionBase64: item.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    hotelId: form.hotelId,
    code: form.code.trim(),
    name: form.name.trim(),
    descriptionMarkdown: form.descriptionMarkdown.trim() || null,
    areaSquareMeters: form.areaSquareMeters === '' ? null : Number(form.areaSquareMeters),
    defaultAdults: Number(form.defaultAdults || 0),
    defaultChildren: Number(form.defaultChildren || 0),
    maxAdults: Number(form.maxAdults || 0),
    maxChildren: Number(form.maxChildren || 0),
    maxGuests: Number(form.maxGuests || 0),
    totalUnits: Number(form.totalUnits || 0),
    hasBalcony: !!form.hasBalcony,
    hasWindow: !!form.hasWindow,
    smokingAllowed: !!form.smokingAllowed,
    coverImageUrl: form.coverImageUrl.trim() || null,
    status: Number(form.status || 2),
    isActive: !!form.isActive,
    metadataJson: form.metadataJson.trim() || null,
    beds: readJsonInput(form.bedsJson, []),
    amenities: readJsonInput(form.amenitiesJson, []),
    occupancyRules: readJsonInput(form.occupancyRulesJson, []),
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function HotelRoomTypesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [searchParams] = useSearchParams();
  const initialHotelId = searchParams.get('hotelId') || '';
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [hotels, setHotels] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedHotelId, setSelectedHotelId] = useState(initialHotelId);
  const [form, setForm] = useState(createEmptyForm(initialHotelId));
  const [uploadingCoverImage, setUploadingCoverImage] = useState(false);

  const listFn = isAdmin ? (params) => listAdminRoomTypes(params, tenantId) : listManagedRoomTypes;
  const getFn = isAdmin ? (id, params) => getAdminRoomType(id, params, tenantId) : getManagedRoomType;
  const createFn = isAdmin ? (payload) => createAdminRoomType(payload, tenantId) : createManagedRoomType;
  const updateFn = isAdmin ? (id, payload) => updateAdminRoomType(id, payload, tenantId) : updateManagedRoomType;
  const deleteFn = isAdmin ? (id) => deleteAdminRoomType(id, tenantId) : deleteManagedRoomType;
  const restoreFn = isAdmin ? (id) => restoreAdminRoomType(id, tenantId) : restoreManagedRoomType;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLoading(false);
      setHotels([]);
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

      const nextHotels = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      setHotels(nextHotels);
      setItems(nextItems);

      const nextHotelId = selectedHotelId || nextHotels[0]?.id || '';
      setSelectedHotelId(nextHotelId);

      if (selectedId) {
        await loadDetail(selectedId, nextItems);
      } else {
        setForm(createEmptyForm(nextHotelId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải hạng phòng.');
    } finally {
      setLoading(false);
    }
  }

  async function loadDetail(id, currentItems = items) {
    const listItem = currentItems.find((item) => item.id === id);
    if (listItem?.hotelId) {
      setSelectedHotelId(listItem.hotelId);
    }

    const detail = await getFn(id, { includeDeleted: true });
    setSelectedId(id);
    setForm(hydrateForm(detail));
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [items, selectedHotelId],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm(selectedHotelId || hotels[0]?.id || ''));
    setNotice('');
  }

  async function handleUploadCoverImage(file) {
    if (isAdmin && !tenantId) {
      setError('KhÃ´ng thá»ƒ táº£i áº£nh khi chÆ°a chá»n tenant.');
      return;
    }

    setUploadingCoverImage(true);
    setError('');
    setNotice('');

    try {
      const response = await uploadManagerImage(file, {
        scope: 'room-type-image',
        tenantId: isAdmin ? tenantId : undefined,
      });
      setForm((current) => ({ ...current, coverImageUrl: response?.url || '' }));
    } catch (requestError) {
      setError(requestError.message || 'KhÃ´ng thá»ƒ táº£i áº£nh háº¡ng phÃ²ng.');
    } finally {
      setUploadingCoverImage(false);
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
        await updateFn(selectedId, payload);
        setNotice('Đã cập nhật hạng phòng.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo hạng phòng mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu hạng phòng.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreFn(item.id);
        setNotice('Đã khôi phục hạng phòng.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn hạng phòng.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái hạng phòng.');
    }
  }

  return (
    <HotelModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="room-types"
      title="Hạng phòng"
      subtitle="Quản lý inventory logic, sức chứa và cấu trúc dữ liệu của từng hạng phòng."
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
            Thêm hạng phòng
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách hạng phòng</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mỗi hạng phòng gắn với một khách sạn trong tenant đang chọn.</p>
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
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải hạng phòng...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có hạng phòng nào.</div>
            ) : filteredItems.map((item) => (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => loadDetail(item.id, filteredItems)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    loadDetail(item.id, filteredItems);
                  }
                }}
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.name}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getStatusClass(item.status)}`}>
                        {getRoomTypeStatusLabel(item.status)}
                      </span>
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {item.code} • {item.totalUnits || 0} phòng • Tối đa {item.maxGuests || item.maxAdults || 1} khách
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật hạng phòng' : 'Tạo hạng phòng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Bạn có thể nhập cấu trúc bed, amenity và occupancy bằng JSON để khớp contract backend.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <select value={form.hotelId} onChange={(event) => setForm((current) => ({ ...current, hotelId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn khách sạn</option>
              {hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã hạng phòng" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên hạng phòng" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.areaSquareMeters} onChange={(event) => setForm((current) => ({ ...current, areaSquareMeters: event.target.value }))} placeholder="Diện tích m²" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.totalUnits} onChange={(event) => setForm((current) => ({ ...current, totalUnits: event.target.value }))} placeholder="Tổng số phòng" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.defaultAdults} onChange={(event) => setForm((current) => ({ ...current, defaultAdults: event.target.value }))} placeholder="Người lớn mặc định" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.defaultChildren} onChange={(event) => setForm((current) => ({ ...current, defaultChildren: event.target.value }))} placeholder="Trẻ em mặc định" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.maxAdults} onChange={(event) => setForm((current) => ({ ...current, maxAdults: event.target.value }))} placeholder="Max người lớn" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.maxChildren} onChange={(event) => setForm((current) => ({ ...current, maxChildren: event.target.value }))} placeholder="Max trẻ em" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.maxGuests} onChange={(event) => setForm((current) => ({ ...current, maxGuests: event.target.value }))} placeholder="Max khách" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <div className="md:col-span-2">
              <ImageUploadField
                label=""
                value={form.coverImageUrl}
                onChange={(value) => setForm((current) => ({ ...current, coverImageUrl: value }))}
                onUpload={handleUploadCoverImage}
                uploading={uploadingCoverImage}
                placeholder="Ảnh đại diện"
                helperText="Hỗ trợ JPG, PNG, WEBP tối đa 10MB."
                previewAlt={form.name || 'Ảnh hạng phòng'}
              />
            </div>
          </div>

          <textarea value={form.descriptionMarkdown} onChange={(event) => setForm((current) => ({ ...current, descriptionMarkdown: event.target.value }))} rows={3} placeholder="Mô tả ngắn" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              {ROOM_TYPE_STATUS_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.hasBalcony} onChange={(event) => setForm((current) => ({ ...current, hasBalcony: event.target.checked }))} /> Có ban công</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.hasWindow} onChange={(event) => setForm((current) => ({ ...current, hasWindow: event.target.checked }))} /> Có cửa sổ</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.smokingAllowed} onChange={(event) => setForm((current) => ({ ...current, smokingAllowed: event.target.checked }))} /> Hút thuốc</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /> Kích hoạt</label>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
            <textarea value={form.bedsJson} onChange={(event) => setForm((current) => ({ ...current, bedsJson: event.target.value }))} rows={8} placeholder='Beds JSON, ví dụ: [{"bedTypeId":"...","quantity":1}]' className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <textarea value={form.amenitiesJson} onChange={(event) => setForm((current) => ({ ...current, amenitiesJson: event.target.value }))} rows={8} placeholder='Amenities JSON, ví dụ: [{"amenityId":"...","isHighlighted":true}]' className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <textarea value={form.occupancyRulesJson} onChange={(event) => setForm((current) => ({ ...current, occupancyRulesJson: event.target.value }))} rows={8} placeholder='Occupancy JSON, ví dụ: [{"minAdults":1,"maxAdults":2,"minGuests":1,"maxGuests":3}]' className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          </div>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật hạng phòng' : 'Tạo hạng phòng')}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </HotelModeShell>
  );
}
