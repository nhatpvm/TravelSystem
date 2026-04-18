import React, { useState } from 'react';
import { Image as ImageIcon, RefreshCw, Trash2, Upload } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { createCmsMedia, deleteCmsMedia, restoreCmsMedia } from '../../../services/cmsService';

function buildEmptyMediaForm() {
  return {
    fileName: '',
    title: '',
    altText: '',
    publicUrl: '',
    mimeType: 'image/jpeg',
    storageProvider: 'local',
    storageKey: '',
    sizeBytes: 0,
    width: '',
    height: '',
    type: 1,
  };
}

const CmsMediaPage = ({ mode = 'admin' }) => {
  const {
    isAdmin,
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    loading,
    error,
    setError,
    options,
    mediaAssets,
    reload,
  } = useCmsWorkspaceData(mode);
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState('');
  const [mediaForm, setMediaForm] = useState(buildEmptyMediaForm());

  async function handleCreateMedia() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      await createCmsMedia({
        ...mediaForm,
        fileName: mediaForm.fileName.trim(),
        title: mediaForm.title.trim() || null,
        altText: mediaForm.altText.trim() || null,
        publicUrl: mediaForm.publicUrl.trim() || null,
        storageKey: mediaForm.storageKey.trim() || mediaForm.fileName.trim(),
        width: mediaForm.width ? Number(mediaForm.width) : null,
        height: mediaForm.height ? Number(mediaForm.height) : null,
        sizeBytes: Number(mediaForm.sizeBytes || 0),
      }, tenantId);

      setMediaForm(buildEmptyMediaForm());
      setNotice('Metadata của media đã được tạo.');
      await reload();
    } catch (err) {
      setError(err.message || 'Không thể tạo media.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleMedia(media) {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      if (media.isDeleted) {
        await restoreCmsMedia(media.id, tenantId);
        setNotice('Media đã được khôi phục.');
      } else {
        await deleteCmsMedia(media.id, tenantId);
        setNotice('Media đã được đưa vào thùng rác.');
      }
      await reload();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật media.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="media"
      title="Thư viện CMS"
      subtitle="Quản lý metadata media theo đúng style CMS hiện tại."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      notice={notice}
      actions={(
        <button onClick={reload} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.8fr,1.2fr] gap-6">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
          <h3 className="text-lg font-black text-slate-900">Thêm metadata cho media</h3>
          <input value={mediaForm.fileName} onChange={(event) => setMediaForm((current) => ({ ...current, fileName: event.target.value }))} placeholder="hero-banner.jpg" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
          <input value={mediaForm.publicUrl} onChange={(event) => setMediaForm((current) => ({ ...current, publicUrl: event.target.value }))} placeholder="https://cdn.example.com/hero.jpg" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <input value={mediaForm.title} onChange={(event) => setMediaForm((current) => ({ ...current, title: event.target.value }))} placeholder="Tiêu đề media" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <input value={mediaForm.altText} onChange={(event) => setMediaForm((current) => ({ ...current, altText: event.target.value }))} placeholder="Văn bản thay thế cho SEO" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          <select value={mediaForm.type} onChange={(event) => setMediaForm((current) => ({ ...current, type: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
            {(options.mediaTypes || []).map((item) => (
              <option key={item.value} value={item.value}>{item.name}</option>
            ))}
          </select>
          <button onClick={handleCreateMedia} disabled={saving || !tenantId} className="w-full px-6 py-3 bg-blue-600 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl shadow-blue-600/20 disabled:opacity-60">
            <Upload size={16} /> Tạo media
          </button>
        </div>

        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <h3 className="text-lg font-black text-slate-900">Thư viện media</h3>
            <div className="rounded-2xl bg-slate-50 px-4 py-3 border border-slate-100 text-[11px] font-black uppercase tracking-widest text-slate-400">
              {mediaAssets.length} mục
            </div>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {loading ? (
              <div className="col-span-full rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Đang tải media...</div>
            ) : mediaAssets.length === 0 ? (
              <div className="col-span-full rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Chưa có media nào.</div>
            ) : mediaAssets.map((asset) => (
              <div key={asset.id} className="group bg-white rounded-3xl p-4 border border-slate-100 shadow-sm hover:shadow-xl transition-all">
                <div className="aspect-square bg-slate-50 rounded-2xl mb-3 flex items-center justify-center text-slate-300 overflow-hidden">
                  {asset.publicUrl ? <img src={asset.publicUrl} alt={asset.altText || asset.fileName} className="w-full h-full object-cover" /> : <ImageIcon size={32} />}
                </div>
                <p className="text-[11px] font-black text-slate-900 truncate mb-1">{asset.title || asset.fileName}</p>
                <div className="flex justify-between items-center text-[9px] font-bold text-slate-400">
                  <span>{asset.mimeType}</span>
                  <button onClick={() => handleToggleMedia(asset)} disabled={saving} className="hover:text-red-500 transition-colors disabled:opacity-60"><Trash2 size={12} /></button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </CmsPageShell>
  );
};

export default CmsMediaPage;
