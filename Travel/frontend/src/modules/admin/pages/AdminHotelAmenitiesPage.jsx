import React from 'react';
import AdminHotelCatalogLinksPage from '../hotel/components/AdminHotelCatalogLinksPage';
import {
  createAdminHotelAmenity,
  deleteAdminHotelAmenity,
  getAdminHotelAmenity,
  getAdminHotelAmenityLinks,
  listAdminHotelAmenities,
  replaceAdminHotelAmenityLinks,
  restoreAdminHotelAmenity,
  updateAdminHotelAmenity,
} from '../../../services/hotelService';

function createEmptyForm() {
  return {
    code: '',
    name: '',
    description: '',
    category: '',
    iconUrl: '',
    sortOrder: 0,
    metadataJson: '',
    isActive: true,
  };
}

function hydrateForm(detail) {
  return {
    code: detail.code || '',
    name: detail.name || '',
    description: detail.description || '',
    category: detail.category || '',
    iconUrl: detail.iconUrl || '',
    sortOrder: detail.sortOrder ?? 0,
    metadataJson: detail.metadataJson || '',
    isActive: detail.isActive ?? true,
    rowVersionBase64: detail.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    code: form.code.trim().toUpperCase(),
    name: form.name.trim(),
    description: form.description.trim() || null,
    category: form.category.trim() || null,
    iconUrl: form.iconUrl.trim() || null,
    sortOrder: Number(form.sortOrder || 0),
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function AdminHotelAmenitiesPage() {
  return (
    <AdminHotelCatalogLinksPage
      pageKey="amenities"
      title="Tiện nghi khách sạn"
      subtitle="Admin quản lý thư viện tiện nghi cấp khách sạn và gán vào từng khách sạn trong tenant đang chọn."
      parentLabel="khách sạn"
      parentOptionsKey="hotels"
      catalogOptionsKey="hotelAmenities"
      listFn={listAdminHotelAmenities}
      getFn={getAdminHotelAmenity}
      createFn={createAdminHotelAmenity}
      updateFn={updateAdminHotelAmenity}
      deleteFn={deleteAdminHotelAmenity}
      restoreFn={restoreAdminHotelAmenity}
      getLinksFn={getAdminHotelAmenityLinks}
      replaceLinksFn={replaceAdminHotelAmenityLinks}
      buildEmptyForm={createEmptyForm}
      hydrateForm={hydrateForm}
      buildPayload={buildPayload}
      itemSubtitle={(item) => `${item.code || 'NO-CODE'} • ${item.category || 'General'}`}
      linkTitle="Liên kết tiện nghi với khách sạn"
      linkDescription="Danh sách JSON phải theo contract backend: amenityId, isHighlighted, sortOrder, notes."
      linkPlaceholder='[{"amenityId":"...","isHighlighted":true,"sortOrder":1,"notes":"Nằm ở tầng thượng"}]'
      renderFields={({ form, setForm }) => (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} placeholder="Mã tiện nghi" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên tiện nghi" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.category} onChange={(event) => setForm((current) => ({ ...current, category: event.target.value }))} placeholder="Nhóm tiện nghi" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.iconUrl} onChange={(event) => setForm((current) => ({ ...current, iconUrl: event.target.value }))} placeholder="Icon URL" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} placeholder="Thứ tự" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Mô tả tiện nghi" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Kích hoạt
          </label>
        </>
      )}
    />
  );
}
