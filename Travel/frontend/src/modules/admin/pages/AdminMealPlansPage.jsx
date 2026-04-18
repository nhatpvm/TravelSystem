import React from 'react';
import AdminHotelCatalogLinksPage from '../hotel/components/AdminHotelCatalogLinksPage';
import {
  createAdminMealPlan,
  deleteAdminMealPlan,
  getAdminMealPlan,
  getAdminMealPlanLinks,
  listAdminMealPlans,
  replaceAdminMealPlanLinks,
  restoreAdminMealPlan,
  updateAdminMealPlan,
} from '../../../services/hotelService';

function createEmptyForm() {
  return {
    code: '',
    name: '',
    description: '',
    category: '',
    sortOrder: 0,
    includesBreakfast: false,
    includesLunch: false,
    includesDinner: false,
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
    sortOrder: detail.sortOrder ?? 0,
    includesBreakfast: !!detail.includesBreakfast,
    includesLunch: !!detail.includesLunch,
    includesDinner: !!detail.includesDinner,
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
    sortOrder: Number(form.sortOrder || 0),
    includesBreakfast: !!form.includesBreakfast,
    includesLunch: !!form.includesLunch,
    includesDinner: !!form.includesDinner,
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function AdminMealPlansPage() {
  return (
    <AdminHotelCatalogLinksPage
      pageKey="meal-plans"
      title="Meal plans"
      subtitle="Admin quản lý bộ meal plan dùng chung và gán vào từng hạng phòng của tenant khách sạn."
      parentLabel="hạng phòng"
      parentOptionsKey="roomTypes"
      catalogOptionsKey="mealPlans"
      listFn={listAdminMealPlans}
      getFn={getAdminMealPlan}
      createFn={createAdminMealPlan}
      updateFn={updateAdminMealPlan}
      deleteFn={deleteAdminMealPlan}
      restoreFn={restoreAdminMealPlan}
      getLinksFn={getAdminMealPlanLinks}
      replaceLinksFn={replaceAdminMealPlanLinks}
      buildEmptyForm={createEmptyForm}
      hydrateForm={hydrateForm}
      buildPayload={buildPayload}
      itemSubtitle={(item) => `${item.code || 'NO-CODE'} • ${item.category || 'Meal plan'}`}
      linkTitle="Liên kết meal plan với hạng phòng"
      linkDescription="Danh sách JSON phải theo contract backend: mealPlanId, additionalPrice, currencyCode, isDefault, isIncluded."
      linkPlaceholder='[{"mealPlanId":"...","additionalPrice":150000,"currencyCode":"VND","isDefault":true,"isIncluded":false}]'
      renderFields={({ form, setForm }) => (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} placeholder="Mã meal plan" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên meal plan" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.category} onChange={(event) => setForm((current) => ({ ...current, category: event.target.value }))} placeholder="Nhóm meal plan" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} placeholder="Thứ tự" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="md:col-span-2 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Mô tả meal plan" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.includesBreakfast} onChange={(event) => setForm((current) => ({ ...current, includesBreakfast: event.target.checked }))} /> Bao gồm bữa sáng</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.includesLunch} onChange={(event) => setForm((current) => ({ ...current, includesLunch: event.target.checked }))} /> Bao gồm bữa trưa</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.includesDinner} onChange={(event) => setForm((current) => ({ ...current, includesDinner: event.target.checked }))} /> Bao gồm bữa tối</label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} /> Kích hoạt</label>
          </div>
        </>
      )}
    />
  );
}
