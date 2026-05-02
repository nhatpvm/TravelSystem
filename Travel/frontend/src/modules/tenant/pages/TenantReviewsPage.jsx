import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  BadgeCheck,
  EyeOff,
  Loader2,
  MessageSquare,
  RefreshCw,
  Search,
  ShieldCheck,
  Star,
} from 'lucide-react';
import { listTenantCommerceReviews } from '../../../services/commerceBackofficeService';

const MODULE_LABEL = {
  hotel: 'Khách sạn',
  tour: 'Tour',
};

function formatDateTime(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

function normalizeStatus(value) {
  return String(value || '').trim().toLowerCase();
}

function getStatusConfig(value, item) {
  const status = normalizeStatus(value);

  if (item?.isDeleted || status === 'deleted') {
    return { label: 'Đã xóa', color: 'bg-slate-100 text-slate-600', icon: <EyeOff size={12} /> };
  }

  if (status === 'pending') {
    return { label: 'Chờ duyệt', color: 'bg-amber-50 text-amber-700', icon: <ShieldCheck size={12} /> };
  }

  if (status === 'hidden' || status === 'rejected') {
    return { label: status === 'rejected' ? 'Từ chối' : 'Đã ẩn', color: 'bg-rose-50 text-rose-700', icon: <EyeOff size={12} /> };
  }

  if (item?.isPublic || status === 'published' || status === 'approved') {
    return { label: 'Công khai', color: 'bg-emerald-50 text-emerald-700', icon: <BadgeCheck size={12} /> };
  }

  return { label: 'Nội bộ', color: 'bg-slate-100 text-slate-600', icon: <ShieldCheck size={12} /> };
}

function ReviewStars({ rating, size = 14 }) {
  const value = Math.round(Number(rating || 0));

  return (
    <div className="flex gap-0.5">
      {[0, 1, 2, 3, 4].map((index) => (
        <Star
          key={index}
          size={size}
          className={index < value ? 'text-amber-400' : 'text-slate-200'}
          fill="currentColor"
        />
      ))}
    </div>
  );
}

export default function TenantReviewsPage() {
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [reviews, setReviews] = useState([]);
  const [summary, setSummary] = useState({});

  const loadData = useCallback(async () => {
    setError('');

    try {
      const response = await listTenantCommerceReviews({
        q: search.trim() || undefined,
        page: 1,
        pageSize: 200,
      });

      setReviews(Array.isArray(response?.items) ? response.items : []);
      setSummary(response?.summary || {});
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải đánh giá tenant.');
      setReviews([]);
      setSummary({});
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [search]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setLoading(true);
      loadData();
    }, 200);

    return () => window.clearTimeout(timer);
  }, [loadData]);

  const stats = useMemo(() => [
    {
      label: 'Điểm trung bình',
      value: Number(summary.averageRating || 0).toFixed(1),
      sub: `${summary.totalCount ?? reviews.length} đánh giá`,
      color: 'bg-slate-900 text-white',
      icon: <Star size={18} fill="currentColor" />,
    },
    {
      label: 'Chờ duyệt',
      value: summary.pendingCount ?? 0,
      sub: 'Cần kiểm tra hiển thị',
      color: 'bg-amber-50 text-amber-700',
      icon: <ShieldCheck size={18} />,
    },
    {
      label: 'Công khai',
      value: summary.publishedCount ?? 0,
      sub: 'Đang hiển thị với khách',
      color: 'bg-emerald-50 text-emerald-700',
      icon: <BadgeCheck size={18} />,
    },
    {
      label: 'Đã phản hồi',
      value: summary.repliedCount ?? 0,
      sub: 'Tenant đã trả lời',
      color: 'bg-blue-50 text-blue-700',
      icon: <MessageSquare size={18} />,
    },
  ], [reviews.length, summary]);

  function refresh() {
    setRefreshing(true);
    loadData();
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Đánh giá tenant</h1>
          <p className="mt-1 text-sm font-medium text-slate-500">
            Theo dõi phản hồi khách hàng theo đúng tenant hiện tại, gồm khách sạn và tour nếu tenant có dữ liệu.
          </p>
        </div>
        <button
          type="button"
          onClick={refresh}
          disabled={refreshing}
          className="inline-flex items-center justify-center gap-2 rounded-xl bg-slate-900 px-5 py-3 text-sm font-black text-white transition hover:bg-blue-600 disabled:opacity-60"
        >
          {refreshing ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
          Tải lại
        </button>
      </div>

      {error ? (
        <div className="rounded-xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {stats.map((item, index) => (
          <motion.div
            key={item.label}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.04 }}
            className={`rounded-xl border border-slate-100 p-5 shadow-sm ${item.color}`}
          >
            <div className={`mb-4 flex h-9 w-9 items-center justify-center rounded-xl ${item.color.includes('slate-900') ? 'bg-white/10' : 'bg-white'}`}>
              {item.icon}
            </div>
            <p className="text-3xl font-black">{loading ? '--' : item.value}</p>
            <p className={`mt-1 text-[10px] font-black uppercase tracking-widest ${item.color.includes('slate-900') ? 'text-white/60' : 'text-current/70'}`}>
              {item.label}
            </p>
            <p className={`mt-1 text-xs font-bold ${item.color.includes('slate-900') ? 'text-white/70' : 'text-slate-500'}`}>
              {item.sub}
            </p>
          </motion.div>
        ))}
      </div>

      <div className="rounded-xl border border-slate-100 bg-white p-4 shadow-sm">
        <div className="flex min-w-0 flex-1 items-center gap-2 rounded-xl bg-slate-50 px-4">
          <Search size={16} className="shrink-0 text-slate-400" />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm khách hàng, dịch vụ, tiêu đề hoặc nội dung..."
            className="min-w-0 flex-1 bg-transparent py-3 text-sm font-bold text-slate-700 outline-none"
          />
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-100 bg-white shadow-sm">
        <div className="hidden grid-cols-12 gap-4 border-b border-slate-100 bg-slate-50 px-5 py-3 text-[10px] font-black uppercase tracking-widest text-slate-400 md:grid">
          <div className="col-span-4">Dịch vụ</div>
          <div className="col-span-3">Đánh giá</div>
          <div className="col-span-2">Trạng thái</div>
          <div className="col-span-3">Thời gian</div>
        </div>

        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="flex items-center gap-3 px-5 py-10 text-sm font-bold text-slate-400">
              <Loader2 size={16} className="animate-spin" />
              Đang tải đánh giá...
            </div>
          ) : reviews.length === 0 ? (
            <div className="px-5 py-14 text-center">
              <p className="text-sm font-black text-slate-500">Chưa có đánh giá phù hợp.</p>
              <p className="mt-1 text-xs font-bold text-slate-400">Khi khách đánh giá dịch vụ của tenant, dữ liệu sẽ xuất hiện tại đây.</p>
            </div>
          ) : (
            reviews.map((review, index) => {
              const statusConfig = getStatusConfig(review.status, review);

              return (
                <motion.div
                  key={`${review.module}-${review.id}`}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.02 }}
                  className="grid grid-cols-1 gap-3 px-5 py-4 transition hover:bg-slate-50 md:grid-cols-12 md:items-center md:gap-4"
                >
                  <div className="md:col-span-4">
                    <p className="text-sm font-black text-slate-900">{review.entityName || '--'}</p>
                    <p className="mt-1 text-[10px] font-black uppercase tracking-widest text-slate-400">
                      {MODULE_LABEL[review.module] || review.module || 'Dịch vụ'}
                      {review.isVerifiedStay ? ' · Đã lưu trú' : ''}
                    </p>
                  </div>
                  <div className="md:col-span-3">
                    <div className="mb-1 flex items-center gap-2">
                      <ReviewStars rating={review.rating} />
                      <span className="text-xs font-black text-slate-700">{Number(review.rating || 0).toFixed(1)}</span>
                    </div>
                    <p className="text-sm font-bold text-slate-800">{review.title || 'Đánh giá không có tiêu đề'}</p>
                    <p className="mt-1 line-clamp-2 text-xs font-medium text-slate-500">{review.content || 'Khách chưa để lại nội dung chi tiết.'}</p>
                    <p className="mt-1 text-[10px] font-bold text-slate-400">{review.reviewerName || 'Khách hàng'}</p>
                  </div>
                  <div className="md:col-span-2">
                    <span className={`inline-flex items-center gap-1 rounded-lg px-2.5 py-1 text-[10px] font-black uppercase ${statusConfig.color}`}>
                      {statusConfig.icon}
                      {statusConfig.label}
                    </span>
                    {review.hasReply ? (
                      <p className="mt-1 inline-flex items-center gap-1 text-[10px] font-bold text-blue-600">
                        <MessageSquare size={11} />
                        Đã phản hồi
                      </p>
                    ) : null}
                  </div>
                  <div className="text-xs font-bold text-slate-500 md:col-span-3">
                    <p>Tạo: {formatDateTime(review.createdAt)}</p>
                    <p className="mt-1 text-slate-400">Cập nhật: {formatDateTime(review.updatedAt || review.replyAt)}</p>
                  </div>
                </motion.div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
