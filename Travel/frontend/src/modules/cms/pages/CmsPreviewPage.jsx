import React, { useEffect, useState } from 'react';
import { Eye, RefreshCw } from 'lucide-react';
import CmsPageShell from '../components/CmsPageShell';
import CmsPostSelectorCard from '../components/CmsPostSelectorCard';
import useCmsWorkspaceData from '../hooks/useCmsWorkspaceData';
import { getCmsPostPreview } from '../../../services/cmsService';

const CmsPreviewPage = ({ mode = 'admin' }) => {
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
  const [previewLoading, setPreviewLoading] = useState(false);
  const [selectedPostId, setSelectedPostId] = useState('');
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    if (!posts.length) {
      setSelectedPostId('');
      setPreview(null);
      return;
    }

    setSelectedPostId((current) => (current && posts.some((item) => item.id === current) ? current : posts[0].id));
  }, [posts]);

  useEffect(() => {
    if (!tenantId || !selectedPostId) {
      return;
    }

    loadPreview(selectedPostId);
  }, [tenantId, selectedPostId]);

  async function loadPreview(postId = selectedPostId) {
    if (!tenantId || !postId) {
      return;
    }

    setPreviewLoading(true);
    setError('');

    try {
      const response = await getCmsPostPreview(postId, tenantId);
      setPreview(response);
    } catch (err) {
      setError(err.message || 'Không thể tải phần xem trước bài viết.');
      setPreview(null);
    } finally {
      setPreviewLoading(false);
    }
  }

  return (
    <CmsPageShell
      mode={mode}
      pageKey="preview"
      title="Xem trước CMS"
      subtitle="Xem trước bài viết đã lưu theo đúng dữ liệu SEO và social metadata của tenant."
      isAdmin={isAdmin}
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={error}
      actions={(
        <button onClick={() => loadPreview()} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.75fr,1.25fr] gap-6">
        <CmsPostSelectorCard
          posts={posts}
          selectedPostId={selectedPostId}
          onChange={setSelectedPostId}
          loading={loading || previewLoading}
          title="Chọn bài viết để xem trước"
          subtitle="Bản xem trước sẽ dùng dữ liệu đã lưu và cấu hình SEO mặc định của tenant."
        />

        <div className="space-y-6">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 space-y-4 shadow-sm">
            <div className="flex items-center gap-2 text-slate-900">
              <Eye size={18} />
              <h4 className="font-black">Xem trước trên kết quả tìm kiếm</h4>
            </div>
            {previewLoading ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Đang tải bản xem trước...</div>
            ) : !preview ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Chưa có dữ liệu xem trước.</div>
            ) : (
              <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
                <p className="text-xs font-bold text-emerald-600 break-all">{preview?.seo?.canonicalUrl || preview?.post?.publicUrl}</p>
                <h5 className="font-black text-blue-700 text-lg mt-1 line-clamp-2">{preview?.seo?.title || preview?.post?.title}</h5>
                <p className="text-sm text-slate-500 mt-2 line-clamp-3">{preview?.seo?.description || preview?.content?.excerpt || 'Mô tả xem trước sẽ hiển thị tại đây.'}</p>
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 space-y-4 shadow-sm">
              <h4 className="font-black text-slate-900">Open Graph</h4>
              <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100 space-y-2">
                <p className="text-sm font-black text-slate-900">{preview?.openGraph?.title || 'Chưa có tiêu đề'}</p>
                <p className="text-sm font-medium text-slate-500">{preview?.openGraph?.description || 'Chưa có mô tả'}</p>
                <p className="text-xs font-bold text-slate-400 break-all">{preview?.openGraph?.imageUrl || 'Chưa có URL hình ảnh'}</p>
              </div>
            </div>

            <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 space-y-4 shadow-sm">
              <h4 className="font-black text-slate-900">Twitter Card</h4>
              <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100 space-y-2">
                <p className="text-sm font-black text-slate-900">{preview?.twitter?.title || 'Chưa có tiêu đề'}</p>
                <p className="text-sm font-medium text-slate-500">{preview?.twitter?.description || 'Chưa có mô tả'}</p>
                <p className="text-xs font-bold text-slate-400 break-all">{preview?.twitter?.imageUrl || 'Chưa có URL hình ảnh'}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 p-6 space-y-4 shadow-sm">
            <h4 className="font-black text-slate-900">Nội dung tóm tắt</h4>
            <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
              <p className="text-sm font-medium text-slate-600 leading-7">{preview?.content?.excerpt || 'Nội dung tóm tắt sẽ hiển thị tại đây khi chọn bài viết.'}</p>
            </div>
          </div>
        </div>
      </div>
    </CmsPageShell>
  );
};

export default CmsPreviewPage;
