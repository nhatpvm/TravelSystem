import React, { useEffect, useState } from 'react';
import { Globe, RefreshCw, Save } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { getCmsSiteSettings, upsertCmsSiteSettings } from '../../../services/cmsService';

function buildEmptySiteSettings() {
  return {
    siteName: '',
    siteUrl: '',
    defaultRobots: 'index,follow',
    defaultOgImageUrl: '',
    defaultTwitterCard: 'summary_large_image',
    defaultTwitterSite: '',
    defaultSchemaJsonLd: '',
    brandLogoUrl: '',
    supportEmail: '',
    supportPhone: '',
    isActive: true,
  };
}

const CmsSiteSettingsPage = ({ mode = 'admin' }) => {
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
  const [form, setForm] = useState(buildEmptySiteSettings());

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadSiteSettings();
  }, [tenantId]);

  async function loadSiteSettings() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await getCmsSiteSettings(tenantId).catch(() => null);
      setForm({
        siteName: response?.siteName || '',
        siteUrl: response?.siteUrl || '',
        defaultRobots: response?.defaultRobots || 'index,follow',
        defaultOgImageUrl: response?.defaultOgImageUrl || '',
        defaultTwitterCard: response?.defaultTwitterCard || 'summary_large_image',
        defaultTwitterSite: response?.defaultTwitterSite || '',
        defaultSchemaJsonLd: response?.defaultSchemaJsonLd || '',
        brandLogoUrl: response?.brandLogoUrl || '',
        supportEmail: response?.supportEmail || '',
        supportPhone: response?.supportPhone || '',
        isActive: response?.isActive !== false,
      });
    } catch (err) {
      setError(err.message || 'Không thể tải cấu hình site.');
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      await upsertCmsSiteSettings(form, tenantId);
      setNotice('Thông tin SEO của site đã được cập nhật.');
      await loadSiteSettings();
    } catch (err) {
      setError(err.message || 'Không thể lưu cấu hình site.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="site-settings"
      title="Cấu hình site CMS"
      subtitle="Cấu hình thông tin site và SEO mặc định theo tenant."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      notice={notice}
      actions={(
        <button onClick={loadSiteSettings} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="bg-white rounded-[2.5rem] p-6 border border-slate-100 shadow-sm space-y-5">
        <div className="flex items-center gap-2 text-slate-900">
          <Globe size={18} />
          <h4 className="font-black text-lg">Cấu hình SEO của site</h4>
        </div>

        {loading ? (
          <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Đang tải cấu hình site...</div>
        ) : (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input value={form.siteName} onChange={(event) => setForm((current) => ({ ...current, siteName: event.target.value }))} placeholder="Tên site" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
              <input value={form.siteUrl} onChange={(event) => setForm((current) => ({ ...current, siteUrl: event.target.value }))} placeholder="https://example.com" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input value={form.defaultOgImageUrl} onChange={(event) => setForm((current) => ({ ...current, defaultOgImageUrl: event.target.value }))} placeholder="Ảnh OG mặc định" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
              <input value={form.brandLogoUrl} onChange={(event) => setForm((current) => ({ ...current, brandLogoUrl: event.target.value }))} placeholder="URL logo thương hiệu" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <select value={form.defaultRobots} onChange={(event) => setForm((current) => ({ ...current, defaultRobots: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                <option value="index,follow">index,follow</option>
                <option value="noindex,follow">noindex,follow</option>
                <option value="index,nofollow">index,nofollow</option>
                <option value="noindex,nofollow">noindex,nofollow</option>
              </select>
              <select value={form.defaultTwitterCard} onChange={(event) => setForm((current) => ({ ...current, defaultTwitterCard: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                <option value="summary_large_image">summary_large_image</option>
                <option value="summary">summary</option>
              </select>
              <input value={form.defaultTwitterSite} onChange={(event) => setForm((current) => ({ ...current, defaultTwitterSite: event.target.value }))} placeholder="@brand" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input value={form.supportEmail} onChange={(event) => setForm((current) => ({ ...current, supportEmail: event.target.value }))} placeholder="support@email.com" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
              <input value={form.supportPhone} onChange={(event) => setForm((current) => ({ ...current, supportPhone: event.target.value }))} placeholder="0900 000 000" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            </div>

            <textarea value={form.defaultSchemaJsonLd} onChange={(event) => setForm((current) => ({ ...current, defaultSchemaJsonLd: event.target.value }))} rows={6} placeholder="Schema JSON-LD mặc định" className="w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-5 text-sm font-medium text-slate-700 outline-none resize-y" />

            <button onClick={handleSave} disabled={saving || !tenantId} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl disabled:opacity-60">
              <Save size={16} /> Lưu cấu hình SEO
            </button>
          </>
        )}
      </div>
    </CmsPageShell>
  );
};

export default CmsSiteSettingsPage;
