import React, { useEffect, useState } from 'react';
import { RefreshCw, ShieldCheck } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import CmsPostSelectorCard from '../components/CmsPostSelectorCard';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { getCmsPostAudit } from '../../../services/cmsService';
import { getCmsSeoIssueLevel, getCmsSeoScore } from '../utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const CmsSeoAuditPage = ({ mode = 'admin' }) => {
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
    posts,
  } = useCmsWorkspaceData(mode);
  const [auditLoading, setAuditLoading] = useState(false);
  const [selectedPostId, setSelectedPostId] = useState('');
  const [audit, setAudit] = useState(null);

  const loadAuditRef = useLatestRef(loadAudit);

  useEffect(() => {
    if (!posts.length) {
      setSelectedPostId('');
      setAudit(null);
      return;
    }

    setSelectedPostId((current) => (current && posts.some((item) => item.id === current) ? current : posts[0].id));
  }, [posts]);

  useEffect(() => {
    if (!tenantId || !selectedPostId) {
      return;
    }

    loadAuditRef.current(selectedPostId);
  }, [tenantId, selectedPostId, loadAuditRef]);

  async function loadAudit(postId = selectedPostId) {
    if (!tenantId || !postId) {
      return;
    }

    setAuditLoading(true);
    setError('');

    try {
      const response = await getCmsPostAudit(postId, tenantId);
      setAudit(response);
    } catch (err) {
      setError(err.message || 'Không thể tải đánh giá SEO.');
      setAudit(null);
    } finally {
      setAuditLoading(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="seo-audit"
      title="Đánh giá SEO CMS"
      subtitle="Đánh giá mức độ sẵn sàng SEO của bài viết đã lưu trong tenant."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      actions={(
        <button onClick={() => loadAuditRef.current()} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.75fr,1.25fr] gap-6">
        <CmsPostSelectorCard
          posts={posts}
          selectedPostId={selectedPostId}
          onChange={setSelectedPostId}
          loading={loading || auditLoading}
          title="Chọn bài viết để đánh giá"
          subtitle="Điểm SEO và cảnh báo sẽ cập nhật theo bài viết được chọn."
        />

        <div className="space-y-6">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 space-y-4 shadow-sm">
            <div className="flex items-center gap-2 text-slate-900">
              <ShieldCheck size={18} />
              <h4 className="font-black">Điểm SEO</h4>
            </div>
            {auditLoading ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Đang tải kết quả đánh giá...</div>
            ) : !audit ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Chưa có dữ liệu đánh giá.</div>
            ) : (
              <>
                <div className="text-4xl font-black text-slate-900">
                  {getCmsSeoScore(audit) ?? '--'}
                  <span className="text-base text-slate-400 ml-1">/100</span>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="rounded-2xl bg-slate-50 p-4 border border-slate-100">
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Xếp hạng</p>
                    <p className="text-lg font-black text-slate-900 mt-2">{audit?.summary?.grade || 'n/a'}</p>
                  </div>
                  <div className="rounded-2xl bg-slate-50 p-4 border border-slate-100">
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Cảnh báo</p>
                    <p className="text-lg font-black text-amber-600 mt-2">{audit?.summary?.warningCount ?? 0}</p>
                  </div>
                  <div className="rounded-2xl bg-slate-50 p-4 border border-slate-100">
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Lỗi</p>
                    <p className="text-lg font-black text-rose-600 mt-2">{audit?.summary?.errorCount ?? 0}</p>
                  </div>
                </div>
              </>
            )}
          </div>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 space-y-4 shadow-sm">
            <h4 className="font-black text-slate-900">Vấn đề phát hiện</h4>
            {(audit?.issues || []).length === 0 ? (
              <div className="rounded-2xl bg-emerald-50 px-4 py-3 border border-emerald-100 text-sm font-bold text-emerald-700">Chưa có cảnh báo SEO nào.</div>
            ) : (
              <div className="space-y-3">
                {(audit?.issues || []).map((issue) => {
                  const level = getCmsSeoIssueLevel(issue);
                  return (
                    <div key={`${issue.code}-${level}`} className="rounded-2xl bg-slate-50 px-4 py-4 border border-slate-100">
                      <p className={`text-[10px] font-black uppercase tracking-widest ${level === 'error' ? 'text-rose-500' : level === 'warning' ? 'text-amber-500' : 'text-sky-500'}`}>{level}</p>
                      <p className="text-sm font-bold text-slate-700 mt-1">{issue.message}</p>
                      {issue.recommendation && <p className="text-sm text-slate-500 mt-2">{issue.recommendation}</p>}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </CmsPageShell>
  );
};

export default CmsSeoAuditPage;
