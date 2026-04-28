import React, { useEffect, useMemo, useState } from 'react';
import { Loader2, MessageSquare, Reply, Search, Star } from 'lucide-react';
import AdminTourPageShell from '../tours/components/AdminTourPageShell';
import useAdminTourScope from '../tours/hooks/useAdminTourScope';
import { getAdminTourReview, listAdminTourReviews, listAdminTours, approveAdminTourReview, rejectAdminTourReview, hideAdminTourReview, makeAdminTourReviewPublic, replyAdminTourReview } from '../../../services/tourService';
import { formatDateTime, getReviewStatusClass, getReviewStatusLabel } from '../../tours/utils/presentation';
import useLatestRef from '../../../shared/hooks/useLatestRef';

export default function AdminTourReviewsPage() {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminTourScope();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [search, setSearch] = useState('');
  const [tourFilter, setTourFilter] = useState('all');
  const [reviews, setReviews] = useState([]);
  const [tours, setTours] = useState([]);
  const [expandedId, setExpandedId] = useState('');
  const [details, setDetails] = useState({});
  const [replyDrafts, setReplyDrafts] = useState({});
  const [noteDrafts, setNoteDrafts] = useState({});
  const [submittingId, setSubmittingId] = useState('');

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    if (tenantId) {
      loadDataRef.current();
    }
  }, [loadDataRef, tenantId]);

  const filteredReviews = useMemo(() => {
    const keyword = search.trim().toLowerCase();

    return reviews.filter((review) => {
      const matchesTour = tourFilter === 'all' || review.tourId === tourFilter;
      const matchesSearch = !keyword || [review.reviewerName, review.title].filter(Boolean).some((value) => String(value).toLowerCase().includes(keyword));
      return matchesTour && matchesSearch;
    });
  }, [reviews, search, tourFilter]);

  async function loadData() {
    setLoading(true);
    setError('');
    setNotice('');

    try {
      const [tourResponse, reviewResponse] = await Promise.all([
        listAdminTours({ tenantId, page: 1, pageSize: 100, includeDeleted: true }),
        listAdminTourReviews({ tenantId, page: 1, pageSize: 100, includeDeleted: true }),
      ]);

      setTours(tourResponse.items || []);
      setReviews(reviewResponse.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải đánh giá tour.');
    } finally {
      setLoading(false);
    }
  }

  async function toggleExpanded(review) {
    if (expandedId === review.id) {
      setExpandedId('');
      return;
    }

    setExpandedId(review.id);

    if (details[review.id]) {
      return;
    }

    try {
      const detail = await getAdminTourReview(review.id, { includeDeleted: true });
      setDetails((current) => ({ ...current, [review.id]: detail }));
      setReplyDrafts((current) => ({ ...current, [review.id]: detail.replyContent || '' }));
      setNoteDrafts((current) => ({ ...current, [review.id]: detail.moderationNote || '' }));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết đánh giá.');
    }
  }

  async function handleAction(review, action) {
    setSubmittingId(review.id);
    setError('');
    setNotice('');

    try {
      if (action === 'approve') {
        await approveAdminTourReview(review.id, { moderationNote: (noteDrafts[review.id] || '').trim() || null }, tenantId);
      }
      if (action === 'reject') {
        await rejectAdminTourReview(review.id, { moderationNote: (noteDrafts[review.id] || '').trim() || 'Không đạt tiêu chuẩn hiển thị công khai.' }, tenantId);
      }
      if (action === 'hide') {
        await hideAdminTourReview(review.id, tenantId);
      }
      if (action === 'public') {
        await makeAdminTourReviewPublic(review.id, tenantId);
      }
      if (action === 'reply') {
        await replyAdminTourReview(review.id, { replyContent: (replyDrafts[review.id] || '').trim() }, tenantId);
      }

      setNotice('Đã cập nhật đánh giá tour.');
      await loadDataRef.current();
      const detail = await getAdminTourReview(review.id, { includeDeleted: true });
      setDetails((current) => ({ ...current, [review.id]: detail }));
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật đánh giá.');
    } finally {
      setSubmittingId('');
    }
  }

  return (
    <AdminTourPageShell
      pageKey="reviews"
      title="Đánh giá tour"
      subtitle="Admin duyệt review tour theo tenant, kiểm soát hiển thị công khai và hỗ trợ phản hồi khi cần."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
    >
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm theo khách hoặc tiêu đề..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <select value={tourFilter} onChange={(event) => setTourFilter(event.target.value)} className="rounded-xl bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none border border-slate-100">
          <option value="all">Tất cả tour</option>
          {tours.map((tour) => (
            <option key={tour.id} value={tour.id}>{tour.name}</option>
          ))}
        </select>
      </div>

      <div className="space-y-4">
        {loading ? (
          <div className="bg-white rounded-2xl p-12 text-center text-slate-400 font-bold shadow-sm border border-slate-100 flex items-center justify-center gap-3">
            <Loader2 size={16} className="animate-spin" />
            Đang tải đánh giá tour...
          </div>
        ) : filteredReviews.length === 0 ? (
          <div className="bg-white rounded-2xl p-12 text-center text-slate-400 font-bold shadow-sm border border-slate-100">
            Tenant này chưa có đánh giá tour phù hợp.
          </div>
        ) : filteredReviews.map((review) => {
          const detail = details[review.id];
          const expanded = expandedId === review.id;

          return (
            <div key={review.id} className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
              <button type="button" onClick={() => toggleExpanded(review)} className="w-full text-left p-5">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getReviewStatusClass(review.status)}`}>
                        {getReviewStatusLabel(review.status)}
                      </span>
                    </div>
                    <p className="text-lg font-black text-slate-900 mt-3">{review.title || 'Đánh giá không tiêu đề'}</p>
                    <p className="text-sm text-slate-500 font-medium mt-2">{review.reviewerName || 'Khách ẩn danh'}</p>
                  </div>
                  <div className="text-right">
                    <div className="flex gap-0.5 justify-end">
                      {[...Array(5)].map((_, index) => (
                        <Star key={index} size={13} className={index < Number(review.rating) ? 'text-amber-400' : 'text-slate-200'} fill="currentColor" />
                      ))}
                    </div>
                    <p className="text-[10px] text-slate-400 font-bold mt-2">{formatDateTime(review.updatedAt || review.createdAt)}</p>
                  </div>
                </div>
              </button>

              {expanded && detail && (
                <div className="border-t border-slate-100 p-5 bg-slate-50 space-y-4">
                  <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4">
                    <p className="text-sm text-slate-700 italic">{detail.content || 'Khách chưa để lại nội dung chi tiết.'}</p>
                  </div>

                  <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
                    <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Ghi chú kiểm duyệt</p>
                      <textarea rows={4} value={noteDrafts[review.id] || ''} onChange={(event) => setNoteDrafts((current) => ({ ...current, [review.id]: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
                      <div className="flex flex-wrap gap-2 mt-4">
                        <button type="button" disabled={submittingId === review.id} onClick={() => handleAction(review, 'approve')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Duyệt</button>
                        <button type="button" disabled={submittingId === review.id} onClick={() => handleAction(review, 'reject')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Từ chối</button>
                        <button type="button" disabled={submittingId === review.id} onClick={() => handleAction(review, 'hide')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Ẩn</button>
                        <button type="button" disabled={submittingId === review.id} onClick={() => handleAction(review, 'public')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Công khai</button>
                      </div>
                    </div>

                    <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4">
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Phản hồi quản trị</p>
                      <textarea rows={4} value={replyDrafts[review.id] || ''} onChange={(event) => setReplyDrafts((current) => ({ ...current, [review.id]: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
                      <div className="flex items-center justify-between gap-3 mt-4">
                        <p className="text-[10px] text-slate-400 font-bold">Cập nhật: {formatDateTime(detail.replyAt || detail.updatedAt || detail.createdAt)}</p>
                        <button type="button" disabled={submittingId === review.id || !(replyDrafts[review.id] || '').trim()} onClick={() => handleAction(review, 'reply')} className="px-4 py-2 rounded-xl bg-blue-600 text-white text-[11px] font-black uppercase tracking-widest disabled:opacity-60 flex items-center gap-2">
                          {submittingId === review.id ? <Loader2 size={12} className="animate-spin" /> : <MessageSquare size={12} />}
                          Gửi phản hồi
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </AdminTourPageShell>
  );
}
