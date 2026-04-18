import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import AdminHotelPageShell from './AdminHotelPageShell';
import useAdminHotelScope from '../hooks/useAdminHotelScope';
import { getAdminHotelOptions } from '../../../../services/hotelService';
import { readJsonInput, toPrettyJson } from '../../../tenant/hotel/utils/presentation';

export default function AdminHotelCatalogLinksPage({
  pageKey,
  title,
  subtitle,
  parentLabel,
  parentOptionsKey,
  catalogOptionsKey,
  listFn,
  getFn,
  createFn,
  updateFn,
  deleteFn,
  restoreFn,
  getLinksFn,
  replaceLinksFn,
  buildEmptyForm,
  hydrateForm,
  buildPayload,
  renderFields,
  itemTitle = (item) => item.name || item.code || 'Bản ghi',
  itemSubtitle = (item) => item.code || '',
  linkTitle,
  linkDescription,
  linkPlaceholder,
}) {
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
  const [savingLinks, setSavingLinks] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [parentItems, setParentItems] = useState([]);
  const [catalogItems, setCatalogItems] = useState([]);
  const [selectedParentId, setSelectedParentId] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(() => buildEmptyForm());
  const [linksJson, setLinksJson] = useState('[]');

  async function loadLinks(parentId = selectedParentId) {
    if (!tenantId || !parentId) {
      setLinksJson('[]');
      return;
    }

    try {
      const response = await getLinksFn(parentId, tenantId);
      setLinksJson(toPrettyJson(response?.items || response || []));
    } catch (requestError) {
      setError(requestError.message || `Không thể tải liên kết ${parentLabel.toLowerCase()}.`);
    }
  }

  async function loadData() {
    if (!tenantId) {
      setParentItems([]);
      setCatalogItems([]);
      setSelectedParentId('');
      setSelectedId('');
      setLinksJson('[]');
      setForm(buildEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getAdminHotelOptions(tenantId),
        listFn({ includeDeleted: true, pageSize: 100 }, tenantId),
      ]);

      const nextParentItems = Array.isArray(optionsResponse?.[parentOptionsKey]) ? optionsResponse[parentOptionsKey] : [];
      const nextCatalogItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      const nextParentId = selectedParentId || nextParentItems[0]?.id || '';

      setParentItems(nextParentItems);
      setCatalogItems(nextCatalogItems);
      setSelectedParentId(nextParentId);

      if (!selectedId) {
        setForm(buildEmptyForm());
      }

      if (nextParentId) {
        await loadLinks(nextParentId);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dữ liệu danh mục.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [tenantId]);

  useEffect(() => {
    loadLinks(selectedParentId);
  }, [selectedParentId, tenantId]);

  const filteredItems = useMemo(() => {
    if (catalogOptionsKey === 'roomAmenities' || catalogOptionsKey === 'mealPlans' || catalogOptionsKey === 'bedTypes') {
      return catalogItems;
    }

    return catalogItems;
  }, [catalogItems, catalogOptionsKey]);

  async function loadDetail(id) {
    try {
      const detail = await getFn(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết danh mục.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm(buildEmptyForm());
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
        await updateFn(selectedId, payload, tenantId);
        setNotice('Đã cập nhật danh mục.');
      } else {
        await createFn(payload, tenantId);
        setNotice('Đã tạo danh mục mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu danh mục.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreFn(item.id, tenantId);
        setNotice('Đã khôi phục danh mục.');
      } else {
        await deleteFn(item.id, tenantId);
        setNotice('Đã ẩn danh mục.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái danh mục.');
    }
  }

  async function handleSaveLinks() {
    if (!selectedParentId) {
      setError(`Hãy chọn ${parentLabel.toLowerCase()} trước khi lưu liên kết.`);
      return;
    }

    setSavingLinks(true);
    setError('');
    setNotice('');

    try {
      await replaceLinksFn(selectedParentId, { items: readJsonInput(linksJson, []) }, tenantId);
      setNotice(`Đã cập nhật liên kết cho ${parentLabel.toLowerCase()}.`);
      await loadLinks(selectedParentId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu liên kết.');
    } finally {
      setSavingLinks(false);
    }
  }

  return (
    <AdminHotelPageShell
      pageKey={pageKey}
      title={title}
      subtitle={subtitle}
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
            Thêm mới
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.8fr_1.2fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh mục</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn một bản ghi ở bên trái để cập nhật nhanh mà không rời trang.</p>
          </div>

          <div className="divide-y divide-slate-50 max-h-[760px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải danh mục...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Tenant này chưa có danh mục nào.</div>
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
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{itemTitle(item)}</p>
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{itemSubtitle(item)}</p>
                  </div>
                  <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="space-y-8">
          <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật danh mục' : 'Tạo danh mục mới'}</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mọi thay đổi ở đây áp dụng cho tenant khách sạn đang được admin chọn.</p>
            </div>

            {renderFields({ form, setForm, parentItems, catalogItems })}

            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
                {saving ? 'Đang lưu...' : selectedId ? 'Cập nhật danh mục' : 'Tạo danh mục'}
              </button>
              {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
            </div>
          </form>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-lg font-black text-slate-900">{linkTitle}</p>
              <p className="text-xs font-bold text-slate-400 mt-1">{linkDescription}</p>
            </div>

            <select value={selectedParentId} onChange={(event) => setSelectedParentId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn {parentLabel.toLowerCase()}</option>
              {parentItems.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>

            <textarea value={linksJson} onChange={(event) => setLinksJson(event.target.value)} rows={12} placeholder={linkPlaceholder} className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

            <button type="button" onClick={handleSaveLinks} disabled={savingLinks || !selectedParentId} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black disabled:opacity-60">
              {savingLinks ? 'Đang lưu liên kết...' : `Lưu liên kết ${parentLabel.toLowerCase()}`}
            </button>
          </div>
        </div>
      </div>
    </AdminHotelPageShell>
  );
}
