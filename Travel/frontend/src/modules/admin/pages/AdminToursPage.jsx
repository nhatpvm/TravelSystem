import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Loader2, Save } from 'lucide-react';
import AdminImageUploadField from '../components/AdminImageUploadField';
import AdminTourPageShell from '../tours/components/AdminTourPageShell';
import useAdminTourScope from '../tours/hooks/useAdminTourScope';
import { getAdminTour, listAdminTours, toggleAdminTourAction, updateAdminTour } from '../../../services/tourService';
import { uploadAdminImage } from '../../../services/adminUploadService';
import {
  buildSlug,
  formatDateTime,
  formatDuration,
  getDifficultyLabel,
  getTourStatusClass,
  getTourStatusLabel,
  getTourTypeLabel,
} from '../../tours/utils/presentation';
import { TOUR_DIFFICULTY_OPTIONS, TOUR_STATUS_OPTIONS, TOUR_TYPE_OPTIONS } from '../../tenant/tour/utils/options';

function buildFormFromTour(tour) {
  if (!tour) {
    return {
      name: '',
      slug: '',
      status: 0,
      type: 1,
      difficulty: 1,
      durationDays: 1,
      durationNights: 0,
      city: '',
      province: '',
      shortDescription: '',
      coverImageUrl: '',
      isFeatured: false,
      isFeaturedOnHome: false,
      isActive: true,
      rowVersionBase64: '',
    };
  }

  return {
    name: tour.name || '',
    slug: tour.slug || '',
    status: Number(tour.status ?? 0),
    type: Number(tour.type ?? 1),
    difficulty: Number(tour.difficulty ?? 1),
    durationDays: Number(tour.durationDays ?? 1),
    durationNights: Number(tour.durationNights ?? 0),
    city: tour.city || '',
    province: tour.province || '',
    shortDescription: tour.shortDescription || '',
    coverImageUrl: tour.coverImageUrl || '',
    isFeatured: Boolean(tour.isFeatured),
    isFeaturedOnHome: Boolean(tour.isFeaturedOnHome),
    isActive: Boolean(tour.isActive),
    rowVersionBase64: tour.rowVersionBase64 || '',
  };
}

export default function AdminToursPage() {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminTourScope();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [tours, setTours] = useState([]);
  const [selectedTourId, setSelectedTourId] = useState('');
  const [selectedTour, setSelectedTour] = useState(null);
  const [form, setForm] = useState(buildFormFromTour(null));
  const [uploadingCoverImage, setUploadingCoverImage] = useState(false);
  const pendingCoverImageUrlRef = useRef('');

  useEffect(() => {
    if (tenantId) {
      loadTours();
    }
  }, [tenantId]);

  useEffect(() => {
    if (selectedTourId && tenantId) {
      pendingCoverImageUrlRef.current = '';
      loadTourDetail(selectedTourId);
    } else {
      setSelectedTour(null);
      setForm(buildFormFromTour(null));
      pendingCoverImageUrlRef.current = '';
    }
  }, [selectedTourId, tenantId]);

  const stats = useMemo(() => ({
    total: tours.length,
    active: tours.filter((tour) => Number(tour.status) === 1 && !tour.isDeleted).length,
    featured: tours.filter((tour) => tour.isFeatured && !tour.isDeleted).length,
  }), [tours]);

  async function loadTours() {
    setLoading(true);
    setError('');

    try {
      const response = await listAdminTours({ tenantId, page: 1, pageSize: 100, includeDeleted: true });
      const items = response.items || [];
      setTours(items);
      setSelectedTourId((current) => current && items.some((item) => item.id === current) ? current : items[0]?.id || '');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách tour.');
      setTours([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadTourDetail(tourId) {
    setError('');

    try {
      const detail = await getAdminTour(tourId, { includeDeleted: true });
      setSelectedTour(detail);
      const nextForm = buildFormFromTour(detail);
      if (pendingCoverImageUrlRef.current) {
        nextForm.coverImageUrl = pendingCoverImageUrlRef.current;
      }

      setForm(nextForm);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết tour.');
    }
  }

  function handleFieldChange(event) {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value,
      ...(name === 'name' ? { slug: buildSlug(value) } : {}),
    }));
  }

  async function handleUploadCoverImage(file) {
    setUploadingCoverImage(true);
    setError('');
    setNotice('');

    try {
      const response = await uploadAdminImage(file, {
        scope: 'tour-cover',
        tenantId,
      });
      pendingCoverImageUrlRef.current = response?.url || '';

      setForm((current) => ({
        ...current,
        coverImageUrl: pendingCoverImageUrlRef.current,
      }));
      setNotice('Đã tải ảnh bìa tour từ máy.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải ảnh bìa tour lên.');
    } finally {
      setUploadingCoverImage(false);
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!selectedTour?.id || !tenantId) {
      setError('Vui lòng chọn tour để cập nhật.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      await updateAdminTour(selectedTour.id, {
        name: form.name.trim(),
        slug: form.slug.trim(),
        status: Number(form.status),
        type: Number(form.type),
        difficulty: Number(form.difficulty),
        durationDays: Number(form.durationDays),
        durationNights: Number(form.durationNights),
        city: form.city || null,
        province: form.province || null,
        shortDescription: form.shortDescription || null,
        coverImageUrl: form.coverImageUrl || null,
        isFeatured: Boolean(form.isFeatured),
        isFeaturedOnHome: Boolean(form.isFeaturedOnHome),
        isActive: Boolean(form.isActive),
        rowVersionBase64: form.rowVersionBase64 || undefined,
      }, tenantId);

      pendingCoverImageUrlRef.current = '';
      setNotice('Đã cập nhật thông tin tour.');
      await loadTours();
      await loadTourDetail(selectedTour.id);
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật tour.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(tour, action) {
    setError('');
    setNotice('');

    try {
      await toggleAdminTourAction(tour.id, action, tenantId);
      setNotice('Đã cập nhật trạng thái tour.');
      await loadTours();
      if (selectedTourId === tour.id) {
        await loadTourDetail(tour.id);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái tour.');
    }
  }

  return (
    <AdminTourPageShell
      pageKey="tours"
      title="Tour toàn hệ thống"
      subtitle="Admin giám sát và điều phối nội dung tour theo từng tenant mà không thay đổi layout admin hiện có."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
    >
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {[
          { label: 'Tổng tour', value: stats.total },
          { label: 'Đang bán', value: stats.active },
          { label: 'Đang nổi bật', value: stats.featured },
        ].map((item) => (
          <div key={item.label} className="bg-white rounded-[2rem] border border-slate-100 shadow-sm p-5">
            <p className="text-3xl font-black text-slate-900">{item.value}</p>
            <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1fr,0.95fr] gap-6">
        <div className="space-y-4">
          {loading ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400 flex items-center gap-3">
              <Loader2 size={16} className="animate-spin" />
              Đang tải danh sách tour...
            </div>
          ) : tours.length === 0 ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400">
              Tenant này chưa có tour nào.
            </div>
          ) : tours.map((tour) => (
            <div key={tour.id} className={`bg-white rounded-[2rem] border shadow-sm ${selectedTourId === tour.id ? 'border-blue-200' : 'border-slate-100'}`}>
              <button type="button" onClick={() => setSelectedTourId(tour.id)} className="w-full text-left p-6">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getTourStatusClass(tour.status)}`}>
                        {getTourStatusLabel(tour.status)}
                      </span>
                      {tour.isFeatured && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Nổi bật</span>
                      )}
                    </div>
                    <p className="text-xl font-black text-slate-900 mt-3">{tour.name}</p>
                    <p className="text-xs font-black text-slate-400 uppercase tracking-widest mt-1">{tour.code} · {getTourTypeLabel(tour.type)} · {getDifficultyLabel(tour.difficulty)}</p>
                    <div className="flex flex-wrap gap-4 mt-4 text-sm text-slate-500 font-medium">
                      <span>{formatDuration(tour.durationDays, tour.durationNights)}</span>
                      <span>{[tour.city, tour.province].filter(Boolean).join(', ') || 'Chưa khai báo'}</span>
                      <span>{formatDateTime(tour.updatedAt || tour.createdAt)}</span>
                    </div>
                  </div>
                </div>
              </button>
              <div className="px-6 pb-6 flex flex-wrap items-center gap-2">
                <button type="button" onClick={() => handleAction(tour, tour.isActive ? 'deactivate' : 'activate')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest">
                  {tour.isActive ? 'Tạm khóa' : 'Kích hoạt'}
                </button>
                <button type="button" onClick={() => handleAction(tour, tour.isFeatured ? 'unfeature' : 'feature')} className="px-4 py-2 rounded-xl bg-amber-50 text-amber-700 text-[11px] font-black uppercase tracking-widest">
                  {tour.isFeatured ? 'Bỏ nổi bật' : 'Nổi bật'}
                </button>
                <button type="button" onClick={() => handleAction(tour, tour.isFeaturedOnHome ? 'unfeature-home' : 'feature-home')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest">
                  {tour.isFeaturedOnHome ? 'Bỏ trang chủ' : 'Đưa trang chủ'}
                </button>
                <button type="button" onClick={() => handleAction(tour, tour.isDeleted ? 'restore' : 'delete')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest">
                  {tour.isDeleted ? 'Khôi phục' : 'Xóa mềm'}
                </button>
              </div>
            </div>
          ))}
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">Chỉnh sửa nhanh</h2>
            <p className="text-slate-500 font-medium mt-1">Cập nhật nhanh thông tin cốt lõi của tour theo tenant đang chọn.</p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tên tour</span>
                <input name="name" value={form.name} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Slug</span>
                <input name="slug" value={form.slug} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select name="status" value={form.status} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {TOUR_STATUS_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Loại tour</span>
                <select name="type" value={form.type} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {TOUR_TYPE_OPTIONS.map((item) => (
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
                <input type="number" min="1" name="durationDays" value={form.durationDays} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Số đêm</span>
                <input type="number" min="0" name="durationNights" value={form.durationNights} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tỉnh / Thành</span>
                <input name="province" value={form.province} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Thành phố / Khu vực</span>
                <input name="city" value={form.city} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <AdminImageUploadField
              label="Ảnh bìa"
              value={form.coverImageUrl}
              onChange={(value) => setForm((current) => ({ ...current, coverImageUrl: value }))}
              onUpload={handleUploadCoverImage}
              uploading={uploadingCoverImage}
              placeholder="URL ảnh bìa"
              helperText="Giữ nguyên field cover image hiện có, nhưng cho phép admin tải ảnh trực tiếp từ máy."
              previewAlt={form.name || 'Ảnh bìa tour'}
            />

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mô tả ngắn</span>
              <textarea name="shortDescription" value={form.shortDescription} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <div className="grid grid-cols-2 gap-3">
              {[
                { name: 'isFeatured', label: 'Nổi bật' },
                { name: 'isFeaturedOnHome', label: 'Hiện trang chủ' },
                { name: 'isActive', label: 'Đang hoạt động' },
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

            <button type="submit" disabled={saving || !selectedTour} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
              {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
              Lưu thay đổi
            </button>
          </form>
        </div>
      </div>
    </AdminTourPageShell>
  );
}
