import React, { useEffect, useState } from 'react';
import { Plus, RefreshCw, Tag } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { createCmsTag, listCmsTags } from '../../../services/cmsService';
import { slugifyCmsValue } from '../utils/presentation';

const CmsTagsPage = ({ mode = 'admin' }) => {
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
  const [tags, setTags] = useState([]);
  const [tagForm, setTagForm] = useState({ name: '', slug: '' });

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadTags();
  }, [tenantId]);

  async function loadTags() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listCmsTags({ page: 1, pageSize: 100, includeDeleted: true }, tenantId);
      setTags(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải thẻ CMS.');
      setTags([]);
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateTag() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      await createCmsTag({
        name: tagForm.name.trim(),
        slug: slugifyCmsValue(tagForm.slug || tagForm.name),
        isActive: true,
      }, tenantId);

      setTagForm({ name: '', slug: '' });
      setNotice('Thẻ mới đã được tạo.');
      await loadTags();
    } catch (err) {
      setError(err.message || 'Không thể tạo thẻ.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="tags"
      title="Thẻ CMS"
      subtitle="Quản lý thẻ riêng để tổ chức bài viết và gom nhóm nội dung SEO."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      notice={notice}
      actions={(
        <button onClick={loadTags} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.8fr,1.2fr] gap-6">
        <div className="bg-white rounded-[2.5rem] p-6 border border-slate-100 shadow-sm space-y-4">
          <div className="flex items-center gap-2 text-slate-900">
            <Tag size={18} />
            <h4 className="font-black text-lg">Thẻ và điều khiển SEO</h4>
          </div>
          <input value={tagForm.name} onChange={(event) => setTagForm((current) => ({ ...current, name: event.target.value, slug: current.slug || slugifyCmsValue(event.target.value) }))} placeholder="Tên thẻ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <input value={tagForm.slug} onChange={(event) => setTagForm((current) => ({ ...current, slug: slugifyCmsValue(event.target.value) }))} placeholder="slug-tag" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <button onClick={handleCreateTag} disabled={saving || !tenantId} className="w-full px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl disabled:opacity-60">
            <Plus size={16} /> Thêm thẻ
          </button>
        </div>

        <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
          <table className="w-full text-left">
            <thead>
              <tr className="bg-slate-50/50 border-b border-slate-100">
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Tên</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Slug</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {loading ? (
                <tr><td colSpan={3} className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải thẻ...</td></tr>
              ) : tags.length === 0 ? (
                <tr><td colSpan={3} className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có thẻ nào.</td></tr>
              ) : tags.map((item) => (
                <tr key={item.id} className="hover:bg-slate-50/50 transition-colors">
                  <td className="px-8 py-5 text-sm font-black text-slate-900">{item.name}</td>
                  <td className="px-8 py-5 text-sm font-bold text-blue-600">{item.slug}</td>
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

export default CmsTagsPage;
