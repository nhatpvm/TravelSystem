import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Loader2, MessageSquare, RefreshCw, Reply, Search, Star } from 'lucide-react';
import {
  approveManagerTourReview,
  getManagerTourReview,
  hideManagerTourReview,
  listManagerTourReviews,
  listManagerTours,
  makeManagerTourReviewPublic,
  rejectManagerTourReview,
  replyManagerTourReview,
} from '../../../services/tourService';
import {
  formatDateTime,
  getReviewStatusClass,
  getReviewStatusLabel,
} from '../../tours/utils/presentation';

export default function TenantReviewsPage() {
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [search, setSearch] = useState('');
  const [tourFilter, setTourFilter] = useState('all');
  const [tours, setTours] = useState([]);
  const [reviews, setReviews] = useState([]);
  const [expandedId, setExpandedId] = useState('');
  const [details, setDetails] = useState({});
  const [detailLoadingId, setDetailLoadingId] = useState('');
  const [replyDrafts, setReplyDrafts] = useState({});
  const [noteDrafts, setNoteDrafts] = useState({});
  const [submittingId, setSubmittingId] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  const filteredReviews = useMemo(() => {
    const keyword = search.trim().toLowerCase();

    return reviews.filter((review) => {
      const matchesTour = tourFilter === 'all' || review.tourId === tourFilter;
      const matchesSearch = !keyword || [
        review.reviewerName,
        review.title,
        review.tourName,
        details[review.id]?.content,
      ].filter(Boolean).some((value) => String(value).toLowerCase().includes(keyword));

      return matchesTour && matchesSearch;
    });
  }, [details, reviews, search, tourFilter]);

  const averageRating = useMemo(() => {
    if (!reviews.length) {
      return '0.0';
    }

    const total = reviews.reduce((sum, item) => sum + Number(item.rating || 0), 0);
    return (total / reviews.length).toFixed(1);
  }, [reviews]);

  async function loadData() {
    setLoading(true);
    setError('');
    setNotice('');

    try {
      const tourResponse = await listManagerTours({ page: 1, pageSize: 100, includeDeleted: true });
      const tourItems = tourResponse.items || [];
      setTours(tourItems);

      const reviewResponses = await Promise.all(
        tourItems.map(async (tour) => {
          const response = await listManagerTourReviews(tour.id, { page: 1, pageSize: 50, includeDeleted: true });
          return (response.items || []).map((item) => ({
            ...item,
            tourId: tour.id,
            tourName: tour.name,
          }));
        }),
      );

      setReviews(
        reviewResponses
          .flat()
          .sort((left, right) => new Date(right.updatedAt || right.createdAt) - new Date(left.updatedAt || left.createdAt)),
      );
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải đánh giá tour.');
    } finally {
      setLoading(false);
    }
  }

  async function handleRefresh() {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
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

    setDetailLoadingId(review.id);

    try {
      const detail = await getManagerTourReview(review.tourId, review.id, { includeDeleted: true });
      setDetails((current) => ({
        ...current,
        [review.id]: detail,
      }));
      setReplyDrafts((current) => ({ ...current, [review.id]: detail.replyContent || '' }));
      setNoteDrafts((current) => ({ ...current, [review.id]: detail.moderationNote || '' }));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết đánh giá.');
    } finally {
      setDetailLoadingId('');
    }
  }

  async function handleReviewAction(review, action) {
    setSubmittingId(review.id);
    setError('');
    setNotice('');

    try {
      if (action === 'reply') {
        await replyManagerTourReview(review.tourId, review.id, {
          replyContent: (replyDrafts[review.id] || '').trim(),
        });
      }

      if (action === 'approve') {
        await approveManagerTourReview(review.tourId, review.id, {
          moderationNote: (noteDrafts[review.id] || '').trim() || null,
        });
      }

      if (action === 'reject') {
        await rejectManagerTourReview(review.tourId, review.id, {
          moderationNote: (noteDrafts[review.id] || '').trim() || 'Chưa phù hợp để hiển thị công khai.',
        });
      }

      if (action === 'hide') {
        await hideManagerTourReview(review.tourId, review.id);
      }

      if (action === 'public') {
        await makeManagerTourReviewPublic(review.tourId, review.id);
      }

      setNotice('Đã cập nhật trạng thái đánh giá.');
      await loadData();

      if (expandedId === review.id) {
        const detail = await getManagerTourReview(review.tourId, review.id, { includeDeleted: true });
        setDetails((current) => ({ ...current, [review.id]: detail }));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật đánh giá.');
    } finally {
      setSubmittingId('');
    }
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Đánh giá tour</h1>
          <p className="text-slate-500 text-sm mt-1">Theo dõi phản hồi khách hàng, kiểm duyệt hiển thị và trả lời ngay trong cổng tenant.</p>
        </div>
        <button onClick={handleRefresh} disabled={refreshing} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm shadow-lg disabled:opacity-60">
          {refreshing ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
          Tải lại dữ liệu
        </button>
      </div>

      {notice && (
        <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4">Tổng quan đánh giá</p>
          <div className="flex items-center gap-5 mb-6">
            <div className="text-center">
              <p className="text-5xl font-black text-slate-900">{averageRating}</p>
              <div className="flex gap-0.5 justify-center mt-1">
                {[...Array(5)].map((_, index) => (
                  <Star key={index} size={14} className={index < Math.round(Number(averageRating)) ? 'text-amber-400' : 'text-slate-200'} fill="currentColor" />
                ))}
              </div>
              <p className="text-xs text-slate-400 font-bold mt-1">{reviews.length} đánh giá</p>
            </div>
            <div className="flex-1 space-y-3">
              <div className="rounded-xl bg-slate-50 px-4 py-3">
                <p className="font-black text-slate-900">{reviews.filter((item) => Number(item.status) === 0).length}</p>
                <p className="text-[10px] text-slate-400 font-bold uppercase mt-1">Chờ duyệt</p>
              </div>
              <div className="rounded-xl bg-emerald-50 px-4 py-3">
                <p className="font-black text-emerald-700">{reviews.filter((item) => Boolean(item.replyAt)).length}</p>
                <p className="text-[10px] text-emerald-500 font-bold uppercase mt-1">Đã phản hồi</p>
              </div>
            </div>
          </div>
        </div>

        <div className="lg:col-span-2 space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm theo khách, tour, tiêu đề..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
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
                Không tìm thấy đánh giá tour phù hợp.
              </div>
            ) : filteredReviews.map((review, index) => {
              const detail = details[review.id];
              const expanded = expandedId === review.id;

              return (
                <motion.div key={review.id} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: index * 0.04 }} className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden hover:shadow-md transition-all">
                  <button type="button" onClick={() => toggleExpanded(review)} className="w-full text-left p-5">
                    <div className="flex items-start justify-between gap-4 mb-3">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-amber-400 to-orange-500 flex items-center justify-center font-black text-white shrink-0">
                          {(review.reviewerName || '?').slice(0, 1).toUpperCase()}
                        </div>
                        <div>
                          <p className="font-black text-slate-900 text-sm">{review.reviewerName || 'Khách ẩn danh'}</p>
                          <div className="flex items-center gap-2 mt-0.5">
                            <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-lg text-[10px] font-black uppercase ${getReviewStatusClass(review.status)}`}>
                              {getReviewStatusLabel(review.status)}
                            </span>
                            <span className="text-[10px] text-slate-400 font-bold">{review.tourName}</span>
                          </div>
                        </div>
                      </div>
                      <div className="flex flex-col items-end gap-1 shrink-0">
                        <div className="flex gap-0.5">
                          {[...Array(5)].map((_, starIndex) => (
                            <Star key={starIndex} size={13} className={starIndex < Number(review.rating) ? 'text-amber-400' : 'text-slate-200'} fill="currentColor" />
                          ))}
                        </div>
                        <p className="text-[10px] text-slate-400 font-bold">{formatDateTime(review.updatedAt || review.createdAt)}</p>
                      </div>
                    </div>

                    <p className="text-sm text-slate-700 font-semibold">{review.title || 'Đánh giá không có tiêu đề'}</p>
                    <p className="text-xs text-slate-400 font-bold mt-1">
                      {detail?.content ? detail.content.slice(0, 160) : 'Mở chi tiết để xem nội dung và trả lời khách hàng.'}
                    </p>
                  </button>

                  {expanded && (
                    <div className="border-t border-slate-100 p-5 bg-slate-50">
                      {detailLoadingId === review.id ? (
                        <div className="text-sm font-bold text-slate-400 flex items-center gap-3">
                          <Loader2 size={16} className="animate-spin" />
                          Đang tải chi tiết đánh giá...
                        </div>
                      ) : !detail ? (
                        <div className="text-sm font-bold text-slate-400">Không tải được chi tiết đánh giá.</div>
                      ) : (
                        <div className="space-y-4">
                          <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4">
                            <p className="text-sm text-slate-700 italic leading-relaxed">{detail.content || 'Khách chưa để lại nội dung chi tiết.'}</p>
                            {detail.replyContent && (
                              <div className="mt-4 rounded-2xl bg-blue-50 border border-blue-100 px-4 py-4">
                                <p className="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-1 flex items-center gap-1">
                                  <Reply size={11} />
                                  Phản hồi hiện tại
                                </p>
                                <p className="text-sm text-blue-800 font-medium">{detail.replyContent}</p>
                              </div>
                            )}
                          </div>

                          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
                            <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4">
                              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Ghi chú kiểm duyệt</p>
                              <textarea
                                rows={4}
                                value={noteDrafts[review.id] || ''}
                                onChange={(event) => setNoteDrafts((current) => ({ ...current, [review.id]: event.target.value }))}
                                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none"
                                placeholder="Nhập ghi chú nội bộ hoặc lý do duyệt / từ chối..."
                              />
                              <div className="flex flex-wrap gap-2 mt-4">
                                <button type="button" disabled={submittingId === review.id} onClick={() => handleReviewAction(review, 'approve')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">
                                  Duyệt
                                </button>
                                <button type="button" disabled={submittingId === review.id} onClick={() => handleReviewAction(review, 'reject')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">
                                  Từ chối
                                </button>
                                <button type="button" disabled={submittingId === review.id} onClick={() => handleReviewAction(review, 'hide')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">
                                  Ẩn
                                </button>
                                <button type="button" disabled={submittingId === review.id} onClick={() => handleReviewAction(review, 'public')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">
                                  Công khai
                                </button>
                              </div>
                            </div>

                            <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4">
                              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Phản hồi khách hàng</p>
                              <textarea
                                rows={4}
                                value={replyDrafts[review.id] || ''}
                                onChange={(event) => setReplyDrafts((current) => ({ ...current, [review.id]: event.target.value }))}
                                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none"
                                placeholder="Cảm ơn bạn đã trải nghiệm tour cùng chúng tôi..."
                              />
                              <div className="flex items-center justify-between gap-3 mt-4">
                                <p className="text-[10px] text-slate-400 font-bold">Cập nhật: {formatDateTime(detail.replyAt || detail.updatedAt || detail.createdAt)}</p>
                                <button type="button" disabled={submittingId === review.id || !(replyDrafts[review.id] || '').trim()} onClick={() => handleReviewAction(review, 'reply')} className="px-4 py-2 rounded-xl bg-blue-600 text-white text-[11px] font-black uppercase tracking-widest disabled:opacity-60 flex items-center gap-2">
                                  {submittingId === review.id ? <Loader2 size={12} className="animate-spin" /> : <MessageSquare size={12} />}
                                  Gửi phản hồi
                                </button>
                              </div>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </motion.div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
