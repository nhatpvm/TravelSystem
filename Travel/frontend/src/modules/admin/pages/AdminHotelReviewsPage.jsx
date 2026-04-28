import React, { useEffect, useMemo, useState } from 'react';
import { MessageSquareQuote, RefreshCw } from 'lucide-react';
import AdminHotelPageShell from '../hotel/components/AdminHotelPageShell';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  approveAdminHotelReview,
  deleteAdminHotelReview,
  getAdminHotelOptions,
  getAdminHotelReview,
  hideAdminHotelReview,
  listAdminHotelReviews,
  rejectAdminHotelReview,
  replyAdminHotelReview,
  restoreAdminHotelReview,
  showAdminHotelReview,
  updateAdminHotelReview,
} from '../../../services/hotelService';

function createEmptyForm(hotelId = '') {
  return {
    hotelId,
    rating: 5,
    title: '',
    content: '',
    reviewerName: '',
    status: 'Pending',
    replyContent: '',
    moderationReason: '',
    isVerifiedStay: false,
    helpfulCount: 0,
    metadataJson: '',
  };
}

function hydrateForm(detail) {
  return {
    hotelId: detail.hotelId || '',
    rating: detail.rating ?? 5,
    title: detail.title || '',
    content: detail.content || '',
    reviewerName: detail.reviewerName || '',
    status: detail.status || 'Pending',
    replyContent: detail.replyContent || '',
    moderationReason: '',
    isVerifiedStay: !!detail.isVerifiedStay,
    helpfulCount: detail.helpfulCount ?? 0,
    metadataJson: detail.metadataJson || '',
    rowVersionBase64: detail.rowVersionBase64 || '',
  };
}

export default function AdminHotelReviewsPage() {
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
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [hotels, setHotels] = useState([]);
  const [selectedHotelId, setSelectedHotelId] = useState('');
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm());

  async function loadData() {
    if (!tenantId) {
      setHotels([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getAdminHotelOptions(tenantId),
        listAdminHotelReviews({ includeDeleted: true, pageSize: 100 }, tenantId),
      ]);

      const nextHotels = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      const nextHotelId = selectedHotelId || nextHotels[0]?.id || '';

      setHotels(nextHotels);
      setItems(nextItems);
      setSelectedHotelId(nextHotelId);

      if (!selectedId) {
        setForm(createEmptyForm(nextHotelId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải đánh giá khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [loadDataRef, tenantId]);

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [items, selectedHotelId],
  );

  async function loadDetail(id) {
    try {
      const detail = await getAdminHotelReview(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setSelectedHotelId(detail.hotelId || selectedHotelId);
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết đánh giá.');
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!selectedId) {
      setError('Đánh giá khách sạn được tạo từ phía khách hàng; admin chỉ có thể cập nhật và kiểm duyệt.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      await updateAdminHotelReview(selectedId, {
        rating: Number(form.rating || 5),
        title: form.title.trim() || null,
        content: form.content.trim() || null,
        reviewerName: form.reviewerName.trim() || null,
        status: form.status,
        replyContent: form.replyContent.trim() || null,
        isVerifiedStay: !!form.isVerifiedStay,
        helpfulCount: Number(form.helpfulCount || 0),
        metadataJson: form.metadataJson.trim() || null,
        rowVersionBase64: form.rowVersionBase64 || null,
      }, tenantId);
      setNotice('Đã cập nhật đánh giá khách sạn.');
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật đánh giá.');
    } finally {
      setSaving(false);
    }
  }

  async function handleModeration(action) {
    if (!selectedId) {
      setError('Hãy chọn một đánh giá trước.');
      return;
    }

    try {
      if (action === 'approve') await approveAdminHotelReview(selectedId, tenantId);
      if (action === 'reject') await rejectAdminHotelReview(selectedId, { reason: form.moderationReason || 'Nội dung chưa phù hợp.' }, tenantId);
      if (action === 'hide') await hideAdminHotelReview(selectedId, tenantId);
      if (action === 'show') await showAdminHotelReview(selectedId, tenantId);
      if (action === 'reply') await replyAdminHotelReview(selectedId, { replyContent: form.replyContent || '' }, tenantId);
      setNotice('Đã cập nhật kiểm duyệt đánh giá.');
      await loadDetail(selectedId);
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể xử lý kiểm duyệt đánh giá.');
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreAdminHotelReview(item.id, tenantId);
        setNotice('Đã khôi phục đánh giá.');
      } else {
        await deleteAdminHotelReview(item.id, tenantId);
        setNotice('Đã ẩn đánh giá.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái đánh giá.');
    }
  }

  return (
    <AdminHotelPageShell
      pageKey="reviews"
      title="Đánh giá khách sạn"
      subtitle="Admin kiểm duyệt review công khai, phản hồi chính thức và xử lý các review không phù hợp."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách review</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Chọn đánh giá để kiểm duyệt, trả lời hoặc ẩn khỏi public page.</p>
            </div>
            <select value={selectedHotelId} onChange={(event) => setSelectedHotelId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Tất cả khách sạn</option>
              {hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="divide-y divide-slate-50 max-h-[720px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải đánh giá...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có đánh giá nào.</div>
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
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                      <MessageSquareQuote size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{item.title || 'Đánh giá khách sạn'}</p>
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-700">{item.status}</span>
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">{item.rating}/5 • {item.helpfulCount || 0} lượt hữu ích</p>
                    </div>
                  </div>
                  <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật & kiểm duyệt review' : 'Chọn một review để kiểm duyệt'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Admin không tạo review mới ở đây; chỉ cập nhật, trả lời và kiểm duyệt review có sẵn.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input type="number" min="1" max="5" step="0.1" value={form.rating} onChange={(event) => setForm((current) => ({ ...current, rating: event.target.value }))} placeholder="Điểm rating" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))} placeholder="Trạng thái" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} placeholder="Tiêu đề review" className="md:col-span-2 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.reviewerName} onChange={(event) => setForm((current) => ({ ...current, reviewerName: event.target.value }))} placeholder="Tên người đánh giá" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.helpfulCount} onChange={(event) => setForm((current) => ({ ...current, helpfulCount: event.target.value }))} placeholder="Lượt hữu ích" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="md:col-span-2 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <textarea value={form.content} onChange={(event) => setForm((current) => ({ ...current, content: event.target.value }))} rows={6} placeholder="Nội dung review" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <textarea value={form.replyContent} onChange={(event) => setForm((current) => ({ ...current, replyContent: event.target.value }))} rows={4} placeholder="Phản hồi chính thức của khách sạn" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
          <textarea value={form.moderationReason} onChange={(event) => setForm((current) => ({ ...current, moderationReason: event.target.value }))} rows={3} placeholder="Lý do từ chối / ghi chú kiểm duyệt" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isVerifiedStay} onChange={(event) => setForm((current) => ({ ...current, isVerifiedStay: event.target.checked }))} />
            Đã xác minh lưu trú
          </label>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving || !selectedId} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black disabled:opacity-60">
              {saving ? 'Đang lưu...' : 'Lưu cập nhật'}
            </button>
            <button type="button" onClick={() => handleModeration('approve')} disabled={!selectedId} className="px-5 py-4 rounded-2xl bg-emerald-50 text-sm font-black text-emerald-700 disabled:opacity-60">Duyệt</button>
            <button type="button" onClick={() => handleModeration('reject')} disabled={!selectedId} className="px-5 py-4 rounded-2xl bg-amber-50 text-sm font-black text-amber-700 disabled:opacity-60">Từ chối</button>
            <button type="button" onClick={() => handleModeration('hide')} disabled={!selectedId} className="px-5 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-700 disabled:opacity-60">Ẩn</button>
            <button type="button" onClick={() => handleModeration('show')} disabled={!selectedId} className="px-5 py-4 rounded-2xl bg-blue-50 text-sm font-black text-blue-700 disabled:opacity-60">Hiện</button>
            <button type="button" onClick={() => handleModeration('reply')} disabled={!selectedId} className="px-5 py-4 rounded-2xl bg-purple-50 text-sm font-black text-purple-700 disabled:opacity-60">Gửi phản hồi</button>
          </div>
        </form>
      </div>
    </AdminHotelPageShell>
  );
}
