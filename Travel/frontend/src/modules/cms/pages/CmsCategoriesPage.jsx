import React, { useEffect, useState } from 'react';
import { Plus, RefreshCw, Tag } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { createCmsCategory, listCmsCategories } from '../../../services/cmsService';
import { slugifyCmsValue } from '../utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const CmsCategoriesPage = ({ mode = 'admin' }) => {
  const {
    isAdmin,
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    error,
    setError,
  } = useCmsWorkspaceData(mode);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState('');
  const [categories, setCategories] = useState([]);
  const [categoryForm, setCategoryForm] = useState({ name: '', slug: '', description: '' });

  const loadCategoriesRef = useLatestRef(loadCategories);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadCategoriesRef.current();
  }, [loadCategoriesRef, tenantId]);

  async function loadCategories() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listCmsCategories({ page: 1, pageSize: 100, includeDeleted: true }, tenantId);
      setCategories(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải danh mục CMS.');
      setCategories([]);
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateCategory() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      await createCmsCategory({
        name: categoryForm.name.trim(),
        slug: slugifyCmsValue(categoryForm.slug || categoryForm.name),
        description: categoryForm.description.trim() || null,
        sortOrder: categories.length + 1,
        isActive: true,
      }, tenantId);

      setCategoryForm({ name: '', slug: '', description: '' });
      setNotice('Danh mục mới đã được tạo.');
      await loadCategoriesRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo danh mục.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="categories"
      title="Danh mục CMS"
      subtitle="Tách riêng màn hình danh mục để marketing và SEO dễ quản lý hơn."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      notice={notice}
      actions={(
        <button onClick={loadCategories} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.8fr,1.2fr] gap-6">
        <div className="bg-white rounded-[2.5rem] p-6 border border-slate-100 shadow-sm space-y-4">
          <div className="flex items-center gap-2 text-slate-900">
            <Tag size={18} />
            <h4 className="font-black text-lg">Danh mục bài viết</h4>
          </div>
          <input value={categoryForm.name} onChange={(event) => setCategoryForm((current) => ({ ...current, name: event.target.value, slug: current.slug || slugifyCmsValue(event.target.value) }))} placeholder="Tên danh mục" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <input value={categoryForm.slug} onChange={(event) => setCategoryForm((current) => ({ ...current, slug: slugifyCmsValue(event.target.value) }))} placeholder="slug-danh-muc" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <textarea value={categoryForm.description} onChange={(event) => setCategoryForm((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Mô tả ngắn cho trang SEO của danh mục" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <button onClick={handleCreateCategory} disabled={saving || !tenantId} className="w-full px-6 py-3 bg-blue-600 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl shadow-blue-600/20 disabled:opacity-60">
            <Plus size={16} /> Thêm danh mục
          </button>
        </div>

        <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
          <table className="w-full text-left">
            <thead>
              <tr className="bg-slate-50/50 border-b border-slate-100">
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Tên</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Slug</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Mô tả</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {loading ? (
                <tr><td colSpan={4} className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải danh mục...</td></tr>
              ) : categories.length === 0 ? (
                <tr><td colSpan={4} className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có danh mục nào.</td></tr>
              ) : categories.map((item) => (
                <tr key={item.id} className="hover:bg-slate-50/50 transition-colors">
                  <td className="px-8 py-5 text-sm font-black text-slate-900">{item.name}</td>
                  <td className="px-8 py-5 text-sm font-bold text-blue-600">{item.slug}</td>
                  <td className="px-8 py-5 text-sm font-medium text-slate-500">{item.description || 'Không có mô tả'}</td>
                  <td className="px-8 py-5">
                    <span className={`px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest ${item.isDeleted ? 'bg-slate-100 text-slate-500' : item.isActive ? 'bg-emerald-100 text-emerald-600' : 'bg-amber-100 text-amber-600'}`}>
                      {item.isDeleted ? 'Đã xóa' : item.isActive ? 'Hoạt động' : 'Tạm dừng'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </CmsPageShell>
  );
};

export default CmsCategoriesPage;
