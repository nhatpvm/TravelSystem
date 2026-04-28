import React, { useEffect, useState } from 'react';
import { Eye, RefreshCw, RotateCcw } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import CmsPostSelectorCard from '../components/CmsPostSelectorCard';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { getCmsRevision, listCmsRevisions, restoreCmsRevision } from '../../../services/cmsService';
import { formatCmsDate } from '../utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const CmsRevisionsPage = ({ mode = 'admin' }) => {
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
    reload,
  } = useCmsWorkspaceData(mode);
  const [notice, setNotice] = useState('');
  const [saving, setSaving] = useState(false);
  const [revisionsLoading, setRevisionsLoading] = useState(false);
  const [selectedPostId, setSelectedPostId] = useState('');
  const [selectedRevisionId, setSelectedRevisionId] = useState('');
  const [revisions, setRevisions] = useState([]);
  const [revisionDetail, setRevisionDetail] = useState(null);

  const loadRevisionsRef = useLatestRef(loadRevisions);
  const loadRevisionDetailRef = useLatestRef(loadRevisionDetail);

  useEffect(() => {
    if (!posts.length) {
      setSelectedPostId('');
      setSelectedRevisionId('');
      setRevisions([]);
      setRevisionDetail(null);
      return;
    }

    setSelectedPostId((current) => (current && posts.some((item) => item.id === current) ? current : posts[0].id));
  }, [posts]);

  useEffect(() => {
    if (!tenantId || !selectedPostId) {
      return;
    }

    loadRevisionsRef.current(selectedPostId);
  }, [tenantId, selectedPostId, loadRevisionsRef]);

  useEffect(() => {
    if (!tenantId || !selectedPostId || !selectedRevisionId) {
      setRevisionDetail(null);
      return;
    }

    loadRevisionDetailRef.current(selectedPostId, selectedRevisionId);
  }, [tenantId, selectedPostId, selectedRevisionId, loadRevisionDetailRef]);

  async function loadRevisions(postId = selectedPostId) {
    if (!tenantId || !postId) {
      return;
    }

    setRevisionsLoading(true);
    setError('');

    try {
      const response = await listCmsRevisions(postId, tenantId);
      const items = response.items || [];
      setRevisions(items);
      setSelectedRevisionId((current) => (current && items.some((item) => item.id === current) ? current : items[0]?.id || ''));
    } catch (err) {
      setError(err.message || 'Không thể tải lịch sử chỉnh sửa.');
      setRevisions([]);
      setSelectedRevisionId('');
    } finally {
      setRevisionsLoading(false);
    }
  }

  async function loadRevisionDetail(postId, revisionId) {
    try {
      const response = await getCmsRevision(postId, revisionId, tenantId);
      setRevisionDetail(response);
    } catch (err) {
      setError(err.message || 'Không thể tải chi tiết phiên bản.');
      setRevisionDetail(null);
    }
  }

  async function handleRestoreRevision() {
    if (!tenantId || !selectedPostId || !selectedRevisionId) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      await restoreCmsRevision(selectedPostId, selectedRevisionId, {
        setDraftAfterRestore: true,
        reviveDeletedPost: true,
        changeNote: 'Restore from revisions page',
      }, tenantId);
      setNotice('Phiên bản đã được khôi phục vào bài viết hiện tại.');
      await Promise.all([loadRevisionsRef.current(selectedPostId), reload()]);
    } catch (err) {
      setError(err.message || 'Không thể khôi phục phiên bản.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="revisions"
      title="Lịch sử chỉnh sửa CMS"
      subtitle="Xem lịch sử chỉnh sửa và khôi phục phiên bản bài viết đã lưu."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      notice={notice}
      actions={(
        <button onClick={() => loadRevisionsRef.current()} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.75fr,1.25fr] gap-6">
        <div className="space-y-6">
          <CmsPostSelectorCard
          posts={posts}
          selectedPostId={selectedPostId}
          onChange={setSelectedPostId}
          loading={loading || revisionsLoading}
          title="Chọn bài viết để xem lịch sử sửa"
          subtitle="Mỗi bài viết sẽ có lịch sử phiên bản riêng."
        />

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-slate-50/50 border-b border-slate-100">
                  <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase tracking-widest">Phiên bản</th>
                  <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase tracking-widest">Thời gian sửa</th>
                  <th className="px-6 py-4 text-[10px] font-black text-slate-400 uppercase tracking-widest">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {revisionsLoading ? (
                  <tr><td colSpan={3} className="px-6 py-5 text-sm font-bold text-slate-400">Đang tải lịch sử sửa...</td></tr>
                ) : revisions.length === 0 ? (
                  <tr><td colSpan={3} className="px-6 py-5 text-sm font-bold text-slate-400">Chưa có phiên bản nào.</td></tr>
                ) : revisions.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-5 text-sm font-black text-slate-900">v{item.versionNumber}</td>
                    <td className="px-6 py-5 text-sm font-medium text-slate-500">{formatCmsDate(item.editedAt || item.updatedAt || item.createdAt, true)}</td>
                    <td className="px-6 py-5">
                      <button onClick={() => setSelectedRevisionId(item.id)} className="text-blue-600 font-black text-xs uppercase tracking-widest">
                        Xem
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="space-y-6">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 shadow-sm space-y-4">
            <div className="flex items-center gap-2 text-slate-900">
              <Eye size={18} />
              <h4 className="font-black">Chi tiết phiên bản</h4>
            </div>

            {!revisionDetail ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Chọn một phiên bản để xem nội dung.</div>
            ) : (
              <>
                <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tiêu đề</p>
                  <p className="mt-2 text-lg font-black text-slate-900">{revisionDetail.title}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tóm tắt</p>
                  <p className="mt-2 text-sm font-medium text-slate-600 leading-7">{revisionDetail.summary || 'Không có tóm tắt.'}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ghi chú thay đổi</p>
                  <p className="mt-2 text-sm font-medium text-slate-600">{revisionDetail.changeNote || 'Không có ghi chú.'}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Nội dung</p>
                  <div className="mt-2 text-sm font-medium text-slate-600 leading-7 whitespace-pre-wrap">{revisionDetail.contentMarkdown || 'Không có nội dung markdown.'}</div>
                </div>
                <button onClick={handleRestoreRevision} disabled={saving} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl disabled:opacity-60">
                  <RotateCcw size={16} /> Khôi phục phiên bản
                </button>
              </>
            )}
          </div>
        </div>
      </div>
    </CmsPageShell>
  );
};

export default CmsRevisionsPage;
