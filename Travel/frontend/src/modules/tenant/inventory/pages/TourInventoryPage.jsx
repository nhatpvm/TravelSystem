import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { CalendarDays, CircleDollarSign, Compass, Loader2, MapPin, Plus, Save, Star, Trash2, UsersRound } from 'lucide-react';
import TourManagementShell from '../../tour/components/TourManagementShell';
import {
  createManagerTour,
  getManagerTour,
  listManagerTours,
  toggleManagerTourAction,
  updateManagerTour,
} from '../../../../services/tourService';
import {
  buildSlug,
  formatDateTime,
  formatDuration,
  getDifficultyLabel,
  getTourStatusClass,
  getTourStatusLabel,
  getTourTypeLabel,
} from '../../../tours/utils/presentation';
import {
  TOUR_DIFFICULTY_OPTIONS,
  TOUR_STATUS_OPTIONS,
  TOUR_TYPE_OPTIONS,
  toNullableNumber,
  toNullableText,
  toNumberOrDefault,
  updateSearchParams,
} from '../../tour/utils/options';
import { getTourManagementSectionPath } from '../../tour/utils/navigation';

const EMPTY_FORM = {
  code: '',
  name: '',
  slug: '',
  type: 1,
  status: 0,
  difficulty: 1,
  durationDays: 3,
  durationNights: 2,
  minGuests: '',
  maxGuests: '',
  province: '',
  city: '',
  meetingPointSummary: '',
  shortDescription: '',
  coverImageUrl: '',
  currencyCode: 'VND',
  isActive: true,
  isFeatured: false,
  isFeaturedOnHome: false,
  isPrivateTourSupported: false,
  isInstantConfirm: false,
  rowVersionBase64: '',
};

function buildFormFromTour(tour) {
  if (!tour) {
    return EMPTY_FORM;
  }

  return {
    code: tour.code || '',
    name: tour.name || '',
    slug: tour.slug || '',
    type: Number(tour.type ?? 1),
    status: Number(tour.status ?? 0),
    difficulty: Number(tour.difficulty ?? 1),
    durationDays: Number(tour.durationDays ?? 1),
    durationNights: Number(tour.durationNights ?? 0),
    minGuests: tour.minGuests ?? '',
    maxGuests: tour.maxGuests ?? '',
    province: tour.province || '',
    city: tour.city || '',
    meetingPointSummary: tour.meetingPointSummary || '',
    shortDescription: tour.shortDescription || '',
    coverImageUrl: tour.coverImageUrl || '',
    currencyCode: tour.currencyCode || 'VND',
    isActive: Boolean(tour.isActive),
    isFeatured: Boolean(tour.isFeatured),
    isFeaturedOnHome: Boolean(tour.isFeaturedOnHome),
    isPrivateTourSupported: Boolean(tour.isPrivateTourSupported),
    isInstantConfirm: Boolean(tour.isInstantConfirm),
    rowVersionBase64: tour.rowVersionBase64 || '',
  };
}

export default function TourInventoryPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [tours, setTours] = useState([]);
  const [selectedTour, setSelectedTour] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);

  const selectedTourId = searchParams.get('tourId') || '';

  useEffect(() => {
    loadTours();
  }, [statusFilter]);

  useEffect(() => {
    if (!selectedTourId) {
      setSelectedTour(null);
      setForm(EMPTY_FORM);
      return;
    }

    loadTourDetail(selectedTourId);
  }, [selectedTourId]);

  const filteredTours = useMemo(() => {
    const keyword = search.trim().toLowerCase();

    return tours.filter((tour) => {
      if (!keyword) {
        return true;
      }

      return [tour.name, tour.code, tour.city, tour.province, tour.slug]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(keyword));
    });
  }, [search, tours]);

  const stats = useMemo(() => ({
    total: tours.length,
    active: tours.filter((tour) => Number(tour.status) === 1 && !tour.isDeleted).length,
    featured: tours.filter((tour) => Boolean(tour.isFeatured) && !tour.isDeleted).length,
    featuredHome: tours.filter((tour) => Boolean(tour.isFeaturedOnHome) && !tour.isDeleted).length,
  }), [tours]);

  async function loadTours() {
    setLoading(true);
    setError('');

    try {
      const response = await listManagerTours({
        page: 1,
        pageSize: 100,
        includeDeleted: true,
        status: statusFilter === 'all' ? undefined : statusFilter,
      });

      const items = response.items || [];
      setTours(items);

      if (!selectedTourId && items.length) {
        updateSearchParams(setSearchParams, { tourId: items[0].id });
      }

      if (selectedTourId && !items.some((item) => item.id === selectedTourId)) {
        updateSearchParams(setSearchParams, { tourId: items[0]?.id || '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách tour.');
    } finally {
      setLoading(false);
    }
  }

  async function loadTourDetail(tourId) {
    setError('');

    try {
      const detail = await getManagerTour(tourId, { includeDeleted: true });
      setSelectedTour(detail);
      setForm(buildFormFromTour(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết tour.');
      setSelectedTour(null);
      setForm(EMPTY_FORM);
    }
  }

  function handleFieldChange(event) {
    const { name, value, type, checked } = event.target;

    setForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value,
      ...(name === 'name' && !selectedTour ? { slug: buildSlug(value) } : {}),
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    const payload = {
      code: form.code.trim(),
      name: form.name.trim(),
      slug: (form.slug || buildSlug(form.name)).trim(),
      type: toNumberOrDefault(form.type, 1),
      status: toNumberOrDefault(form.status, 0),
      difficulty: toNumberOrDefault(form.difficulty, 1),
      durationDays: toNumberOrDefault(form.durationDays, 1),
      durationNights: toNumberOrDefault(form.durationNights, 0),
      minGuests: toNullableNumber(form.minGuests),
      maxGuests: toNullableNumber(form.maxGuests),
      province: toNullableText(form.province),
      city: toNullableText(form.city),
      meetingPointSummary: toNullableText(form.meetingPointSummary),
      shortDescription: toNullableText(form.shortDescription),
      coverImageUrl: toNullableText(form.coverImageUrl),
      currencyCode: (form.currencyCode || 'VND').trim() || 'VND',
      isActive: Boolean(form.isActive),
      isFeatured: Boolean(form.isFeatured),
      isFeaturedOnHome: Boolean(form.isFeaturedOnHome),
      isPrivateTourSupported: Boolean(form.isPrivateTourSupported),
      isInstantConfirm: Boolean(form.isInstantConfirm),
      rowVersionBase64: form.rowVersionBase64 || undefined,
    };

    try {
      if (selectedTour?.id) {
        await updateManagerTour(selectedTour.id, payload);
        setNotice('Đã cập nhật thông tin tour.');
        await loadTourDetail(selectedTour.id);
      } else {
        const created = await createManagerTour(payload);
        setNotice('Đã tạo tour mới.');
        await loadTours();
        updateSearchParams(setSearchParams, { tourId: created.id });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu thông tin tour.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(tour, action) {
    setError('');
    setNotice('');

    try {
      await toggleManagerTourAction(tour.id, action);
      setNotice('Đã cập nhật trạng thái tour.');
      await loadTours();
      if (selectedTourId === tour.id) {
        await loadTourDetail(tour.id);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái tour.');
    }
  }

  function startCreateTour() {
    setSelectedTour(null);
    setForm(EMPTY_FORM);
    updateSearchParams(setSearchParams, { tourId: '' });
  }

  return (
    <TourManagementShell
      pageKey="overview"
      title="Quản lý tour"
      subtitle="Tạo tour, chỉnh sửa thông tin bán và điều hướng nhanh sang lịch khởi hành, giá, sức chứa và gói tour."
      error={error}
      notice={notice}
      actions={(
        <button
          type="button"
          onClick={startCreateTour}
          className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm"
        >
          <Plus size={14} />
          Tạo tour mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {[
          { label: 'Tổng tour', value: stats.total, icon: Compass },
          { label: 'Đang bán', value: stats.active, icon: CalendarDays },
          { label: 'Đang nổi bật', value: stats.featured, icon: Star },
          { label: 'Trang chủ', value: stats.featuredHome, icon: UsersRound },
        ].map((item) => {
          const Icon = item.icon;

          return (
            <div key={item.label} className="bg-white rounded-[2rem] border border-slate-100 shadow-sm p-5">
              <div className="w-11 h-11 rounded-2xl bg-slate-100 text-slate-700 flex items-center justify-center">
                <Icon size={18} />
              </div>
              <p className="text-3xl font-black text-slate-900 mt-5">{item.value}</p>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1.2fr,0.8fr] gap-6">
        <div className="space-y-5">
          <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm p-5 flex flex-col md:flex-row gap-3">
            <div className="flex-1">
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Tìm theo tên tour, mã tour, điểm đến..."
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none"
              />
            </div>
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value)}
              className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              <option value="all">Tất cả trạng thái</option>
              {TOUR_STATUS_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
          </div>

          <div className="space-y-4">
            {loading ? (
              <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400 flex items-center gap-3">
                <Loader2 size={16} className="animate-spin" />
                Đang tải danh sách tour...
              </div>
            ) : filteredTours.length === 0 ? (
              <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400">
                Chưa có tour nào phù hợp với bộ lọc hiện tại.
              </div>
            ) : filteredTours.map((tour) => (
              <div
                key={tour.id}
                className={`bg-white rounded-[2rem] border shadow-sm overflow-hidden transition-all ${
                  selectedTourId === tour.id ? 'border-blue-200 shadow-md' : 'border-slate-100'
                }`}
              >
                <button
                  type="button"
                  onClick={() => updateSearchParams(setSearchParams, { tourId: tour.id })}
                  className="w-full text-left p-6"
                >
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <img
                        src={tour.coverImageUrl || 'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=300&q=80'}
                        alt={tour.name}
                        className="w-24 h-24 rounded-[1.5rem] object-cover border border-slate-100"
                      />
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getTourStatusClass(tour.status)}`}>
                            {getTourStatusLabel(tour.status)}
                          </span>
                          {tour.isFeatured && (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">
                              Nổi bật
                            </span>
                          )}
                          {tour.isDeleted && (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                              Đã xóa mềm
                            </span>
                          )}
                        </div>
                        <p className="text-xl font-black text-slate-900 mt-3">{tour.name}</p>
                        <p className="text-xs font-black text-slate-400 uppercase tracking-widest mt-1">
                          {tour.code} · {getTourTypeLabel(tour.type)} · {getDifficultyLabel(tour.difficulty)}
                        </p>
                        <div className="flex flex-wrap items-center gap-4 mt-4 text-sm font-medium text-slate-500">
                          <span>{formatDuration(tour.durationDays, tour.durationNights)}</span>
                          <span className="flex items-center gap-1">
                            <MapPin size={14} />
                            {[tour.city, tour.province].filter(Boolean).join(', ') || 'Chưa khai báo'}
                          </span>
                          <span>{tour.currencyCode || 'VND'}</span>
                        </div>
                      </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                      <Link
                        to={getTourManagementSectionPath('schedules', { tourId: tour.id })}
                        className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest"
                      >
                        Lịch
                      </Link>
                      <Link
                        to={getTourManagementSectionPath('pricing', { tourId: tour.id })}
                        className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest"
                      >
                        Giá
                      </Link>
                      <Link
                        to={getTourManagementSectionPath('capacity', { tourId: tour.id })}
                        className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest"
                      >
                        Chỗ
                      </Link>
                    </div>
                  </div>
                </button>

                <div className="px-6 pb-6 flex flex-wrap items-center gap-2">
                  {tour.isDeleted ? (
                    <button type="button" onClick={() => handleAction(tour, 'restore')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest">
                      Khôi phục
                    </button>
                  ) : (
                    <button type="button" onClick={() => handleAction(tour, 'delete')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest flex items-center gap-2">
                      <Trash2 size={12} />
                      Xóa mềm
                    </button>
                  )}
                  <button type="button" onClick={() => handleAction(tour, tour.isActive ? 'deactivate' : 'activate')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest">
                    {tour.isActive ? 'Tạm khóa' : 'Kích hoạt'}
                  </button>
                  <button type="button" onClick={() => handleAction(tour, tour.isFeatured ? 'unfeature' : 'feature')} className="px-4 py-2 rounded-xl bg-amber-50 text-amber-700 text-[11px] font-black uppercase tracking-widest">
                    {tour.isFeatured ? 'Bỏ nổi bật' : 'Đánh dấu nổi bật'}
                  </button>
                  <button type="button" onClick={() => handleAction(tour, tour.isFeaturedOnHome ? 'unfeature-home' : 'feature-home')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest">
                    {tour.isFeaturedOnHome ? 'Bỏ trang chủ' : 'Đưa lên trang chủ'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              {selectedTour ? 'Cập nhật tour' : 'Tạo tour mới'}
            </h2>
            <p className="text-slate-500 font-medium mt-1">
              Giữ nguyên cấu trúc giao diện hiện tại, chỉ nối form thật vào backend quản lý tour.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mã tour</span>
                <input name="code" value={form.code} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Slug</span>
                <input name="slug" value={form.slug} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tên tour</span>
              <input name="name" value={form.name} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
            </label>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Loại tour</span>
                <select name="type" value={form.type} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {TOUR_TYPE_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select name="status" value={form.status} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {TOUR_STATUS_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Độ khó</span>
                <select name="difficulty" value={form.difficulty} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {TOUR_DIFFICULTY_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Số ngày</span>
                <input name="durationDays" type="number" min="1" value={form.durationDays} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Số đêm</span>
                <input name="durationNights" type="number" min="0" value={form.durationNights} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tỉnh / Thành</span>
                <input name="province" value={form.province} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Khu vực / Thành phố</span>
                <input name="city" value={form.city} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Khách tối thiểu</span>
                <input name="minGuests" type="number" min="0" value={form.minGuests} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Khách tối đa</span>
                <input name="maxGuests" type="number" min="0" value={form.maxGuests} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Điểm tập trung</span>
              <input name="meetingPointSummary" value={form.meetingPointSummary} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
            </label>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ảnh bìa</span>
              <input name="coverImageUrl" value={form.coverImageUrl} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
            </label>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mô tả ngắn</span>
              <textarea name="shortDescription" value={form.shortDescription} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <div className="grid grid-cols-2 gap-3">
              {[
                { name: 'isActive', label: 'Đang hoạt động' },
                { name: 'isFeatured', label: 'Nổi bật' },
                { name: 'isFeaturedOnHome', label: 'Hiện trang chủ' },
                { name: 'isInstantConfirm', label: 'Xác nhận tức thì' },
                { name: 'isPrivateTourSupported', label: 'Hỗ trợ tour riêng' },
              ].map((item) => (
                <label key={item.name} className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700">
                  <input type="checkbox" name={item.name} checked={Boolean(form[item.name])} onChange={handleFieldChange} className="h-4 w-4 rounded border-slate-300 text-blue-600" />
                  {item.label}
                </label>
              ))}
            </div>

            {selectedTour && (
              <div className="rounded-2xl bg-slate-50 border border-slate-100 px-5 py-4 text-xs font-bold text-slate-500">
                Cập nhật lần cuối: {formatDateTime(selectedTour.updatedAt || selectedTour.createdAt)}
              </div>
            )}

            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={saving} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
                {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                {selectedTour ? 'Lưu thay đổi' : 'Tạo tour'}
              </button>
              {selectedTour && (
                <Link
                  to={getTourManagementSectionPath('schedules', { tourId: selectedTour.id })}
                  className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest border border-slate-100 shadow-sm"
                >
                  Sang lịch khởi hành
                </Link>
              )}
            </div>
          </form>
        </div>
      </div>
    </TourManagementShell>
  );
}
