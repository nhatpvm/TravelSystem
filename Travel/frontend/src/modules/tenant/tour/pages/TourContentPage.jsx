import React, { useEffect, useMemo, useState } from 'react';
import { Loader2, Plus, Save } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import TourManagementShell from '../components/TourManagementShell';
import {
  createManagerTourContact,
  createManagerTourFaqEntry,
  createManagerTourImage,
  createManagerTourPolicy,
  getManagerTourContact,
  getManagerTourFaqEntry,
  getManagerTourImage,
  getManagerTourPolicy,
  listManagerTourContacts,
  listManagerTourFaqEntries,
  listManagerTourImages,
  listManagerTourPolicies,
  listManagerTours,
  toggleManagerTourContactAction,
  toggleManagerTourFaqAction,
  toggleManagerTourImageAction,
  toggleManagerTourPolicyAction,
  updateManagerTourContact,
  updateManagerTourFaqEntry,
  updateManagerTourImage,
  updateManagerTourPolicy,
} from '../../../../services/tourService';
import { formatDateTime } from '../../../tours/utils/presentation';
import {
  CONTACT_TYPE_OPTIONS,
  FAQ_TYPE_OPTIONS,
  POLICY_TYPE_OPTIONS,
  toNullableNumber,
  toNullableText,
  updateSearchParams,
} from '../utils/options';

const TABS = [
  { key: 'contacts', label: 'Liên hệ' },
  { key: 'images', label: 'Hình ảnh' },
  { key: 'policies', label: 'Chính sách' },
  { key: 'faqs', label: 'FAQ' },
];

const APIS = {
  contacts: {
    list: listManagerTourContacts,
    get: getManagerTourContact,
    create: createManagerTourContact,
    update: updateManagerTourContact,
    action: toggleManagerTourContactAction,
  },
  images: {
    list: listManagerTourImages,
    get: getManagerTourImage,
    create: createManagerTourImage,
    update: updateManagerTourImage,
    action: toggleManagerTourImageAction,
  },
  policies: {
    list: listManagerTourPolicies,
    get: getManagerTourPolicy,
    create: createManagerTourPolicy,
    update: updateManagerTourPolicy,
    action: toggleManagerTourPolicyAction,
  },
  faqs: {
    list: listManagerTourFaqEntries,
    get: getManagerTourFaqEntry,
    create: createManagerTourFaqEntry,
    update: updateManagerTourFaqEntry,
    action: toggleManagerTourFaqAction,
  },
};

function getEmptyForm(tab) {
  if (tab === 'contacts') return { name: '', title: '', department: '', phone: '', email: '', contactType: 1, isPrimary: false, sortOrder: 0, notes: '', isActive: true, rowVersionBase64: '' };
  if (tab === 'images') return { imageUrl: '', caption: '', altText: '', title: '', isPrimary: false, isCover: false, isFeatured: false, sortOrder: 0, notes: '', isActive: true, rowVersionBase64: '' };
  if (tab === 'policies') return { code: '', name: '', type: 1, shortDescription: '', descriptionMarkdown: '', policyJson: '', isHighlighted: false, sortOrder: 0, notes: '', isActive: true, rowVersionBase64: '' };
  return { question: '', answerMarkdown: '', type: 1, isHighlighted: false, sortOrder: 0, notes: '', isActive: true, rowVersionBase64: '' };
}

function mapDetailToForm(tab, detail) {
  if (!detail) return getEmptyForm(tab);
  if (tab === 'contacts') return { name: detail.name || '', title: detail.title || '', department: detail.department || '', phone: detail.phone || '', email: detail.email || '', contactType: Number(detail.contactType ?? 1), isPrimary: Boolean(detail.isPrimary), sortOrder: detail.sortOrder ?? 0, notes: detail.notes || '', isActive: Boolean(detail.isActive), rowVersionBase64: detail.rowVersionBase64 || '' };
  if (tab === 'images') return { imageUrl: detail.imageUrl || '', caption: detail.caption || '', altText: detail.altText || '', title: detail.title || '', isPrimary: Boolean(detail.isPrimary), isCover: Boolean(detail.isCover), isFeatured: Boolean(detail.isFeatured), sortOrder: detail.sortOrder ?? 0, notes: detail.notes || '', isActive: Boolean(detail.isActive), rowVersionBase64: detail.rowVersionBase64 || '' };
  if (tab === 'policies') return { code: detail.code || '', name: detail.name || '', type: Number(detail.type ?? 1), shortDescription: detail.shortDescription || '', descriptionMarkdown: detail.descriptionMarkdown || '', policyJson: detail.policyJson || '', isHighlighted: Boolean(detail.isHighlighted), sortOrder: detail.sortOrder ?? 0, notes: detail.notes || '', isActive: Boolean(detail.isActive), rowVersionBase64: detail.rowVersionBase64 || '' };
  return { question: detail.question || '', answerMarkdown: detail.answerMarkdown || '', type: Number(detail.type ?? 1), isHighlighted: Boolean(detail.isHighlighted), sortOrder: detail.sortOrder ?? 0, notes: detail.notes || '', isActive: Boolean(detail.isActive), rowVersionBase64: detail.rowVersionBase64 || '' };
}

function getTitle(tab, item) {
  if (tab === 'contacts') return item.name;
  if (tab === 'images') return item.title || item.caption || item.imageUrl || 'Hình ảnh';
  if (tab === 'policies') return item.name;
  return item.question;
}

function getSubtitle(tab, item) {
  if (tab === 'contacts') return [item.title, item.department, item.email || item.phone].filter(Boolean).join(' · ');
  if (tab === 'images') return [item.altText, item.caption].filter(Boolean).join(' · ');
  if (tab === 'policies') return [item.code, item.shortDescription].filter(Boolean).join(' · ');
  return formatDateTime(item.updatedAt || item.createdAt);
}

export default function TourContentPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tours, setTours] = useState([]);
  const [items, setItems] = useState([]);
  const [detail, setDetail] = useState(null);
  const [form, setForm] = useState(getEmptyForm('contacts'));
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const tab = searchParams.get('tab') || 'contacts';
  const tourId = searchParams.get('tourId') || '';
  const itemId = searchParams.get('itemId') || '';
  const selectedTour = useMemo(() => tours.find((item) => item.id === tourId) || null, [tours, tourId]);

  useEffect(() => {
    loadTours();
  }, []);

  useEffect(() => {
    setForm(getEmptyForm(tab));
    setDetail(null);
    if (tourId) loadItems(tourId, tab);
    else {
      setItems([]);
      setLoading(false);
    }
  }, [tourId, tab]);

  useEffect(() => {
    if (tourId && itemId) loadDetail(tourId, tab, itemId);
    else {
      setDetail(null);
      setForm(getEmptyForm(tab));
    }
  }, [tourId, tab, itemId]);

  async function loadTours() {
    try {
      const response = await listManagerTours({ page: 1, pageSize: 100, includeDeleted: true });
      const nextTours = response.items || [];
      setTours(nextTours);
      if (!tourId && nextTours.length) updateSearchParams(setSearchParams, { tourId: nextTours[0].id, tab, itemId: '' });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách tour.');
    }
  }

  async function loadItems(nextTourId, nextTab) {
    setLoading(true);
    setError('');
    try {
      const response = await APIS[nextTab].list(nextTourId, { page: 1, pageSize: 100, includeDeleted: true });
      const nextItems = response.items || [];
      setItems(nextItems);
      if (!itemId && nextItems.length) updateSearchParams(setSearchParams, { itemId: nextItems[0].id });
      if (itemId && !nextItems.some((item) => item.id === itemId)) updateSearchParams(setSearchParams, { itemId: nextItems[0]?.id || '' });
    } catch (requestError) {
      setItems([]);
      setError(requestError.message || 'Không thể tải dữ liệu.');
    } finally {
      setLoading(false);
    }
  }

  async function loadDetail(nextTourId, nextTab, nextItemId) {
    try {
      const nextDetail = await APIS[nextTab].get(nextTourId, nextItemId, { includeDeleted: true });
      setDetail(nextDetail);
      setForm(mapDetailToForm(nextTab, nextDetail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết dữ liệu.');
    }
  }

  function handleFieldChange(event) {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!tourId) {
      setError('Vui lòng chọn tour trước khi lưu.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      let payload = {};
      if (tab === 'contacts') payload = { name: form.name.trim(), title: toNullableText(form.title), department: toNullableText(form.department), phone: toNullableText(form.phone), email: toNullableText(form.email), contactType: Number(form.contactType), isPrimary: Boolean(form.isPrimary), sortOrder: toNullableNumber(form.sortOrder) ?? 0, notes: toNullableText(form.notes), isActive: Boolean(form.isActive), rowVersionBase64: form.rowVersionBase64 || undefined };
      if (tab === 'images') payload = { imageUrl: form.imageUrl.trim(), caption: toNullableText(form.caption), altText: toNullableText(form.altText), title: toNullableText(form.title), isPrimary: Boolean(form.isPrimary), isCover: Boolean(form.isCover), isFeatured: Boolean(form.isFeatured), sortOrder: toNullableNumber(form.sortOrder) ?? 0, notes: toNullableText(form.notes), isActive: Boolean(form.isActive), rowVersionBase64: form.rowVersionBase64 || undefined };
      if (tab === 'policies') payload = { code: form.code.trim(), name: form.name.trim(), type: Number(form.type), shortDescription: toNullableText(form.shortDescription), descriptionMarkdown: toNullableText(form.descriptionMarkdown), policyJson: toNullableText(form.policyJson), isHighlighted: Boolean(form.isHighlighted), sortOrder: toNullableNumber(form.sortOrder) ?? 0, notes: toNullableText(form.notes), isActive: Boolean(form.isActive), rowVersionBase64: form.rowVersionBase64 || undefined };
      if (tab === 'faqs') payload = { question: form.question.trim(), answerMarkdown: form.answerMarkdown.trim(), type: Number(form.type), isHighlighted: Boolean(form.isHighlighted), sortOrder: toNullableNumber(form.sortOrder) ?? 0, notes: toNullableText(form.notes), isActive: Boolean(form.isActive), rowVersionBase64: form.rowVersionBase64 || undefined };

      if (detail?.id) {
        await APIS[tab].update(tourId, detail.id, payload);
        setNotice('Đã cập nhật dữ liệu.');
      } else {
        const created = await APIS[tab].create(tourId, payload);
        updateSearchParams(setSearchParams, { itemId: created.id });
        setNotice('Đã tạo dữ liệu mới.');
      }

      await loadItems(tourId, tab);
      if (detail?.id) await loadDetail(tourId, tab, detail.id);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu dữ liệu.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(item, action) {
    setError('');
    setNotice('');
    try {
      await APIS[tab].action(tourId, item.id, action);
      setNotice('Đã cập nhật trạng thái dữ liệu.');
      await loadItems(tourId, tab);
      if (itemId === item.id) await loadDetail(tourId, tab, item.id);
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái.');
    }
  }

  const extraFlags = tab === 'contacts' ? [['isPrimary', 'Liên hệ chính']] : tab === 'images' ? [['isPrimary', 'Ảnh chính'], ['isCover', 'Ảnh bìa'], ['isFeatured', 'Nổi bật']] : tab === 'policies' || tab === 'faqs' ? [['isHighlighted', 'Nổi bật']] : [];
  const actionGroups = (item) => [
    item.isDeleted ? ['restore', 'Khôi phục', 'bg-emerald-50 text-emerald-700'] : ['delete', 'Xóa mềm', 'bg-rose-50 text-rose-700'],
    [item.isActive ? 'deactivate' : 'activate', item.isActive ? 'Tạm khóa' : 'Kích hoạt', 'bg-slate-100 text-slate-700'],
    ...(tab === 'contacts' ? [[item.isPrimary ? 'unset-primary' : 'set-primary', item.isPrimary ? 'Bỏ chính' : 'Đặt chính', 'bg-sky-50 text-sky-700']] : []),
    ...(tab === 'images' ? [[item.isPrimary ? 'unset-primary' : 'set-primary', item.isPrimary ? 'Bỏ ảnh chính' : 'Ảnh chính', 'bg-sky-50 text-sky-700'], [item.isCover ? 'unset-cover' : 'set-cover', item.isCover ? 'Bỏ ảnh bìa' : 'Ảnh bìa', 'bg-indigo-50 text-indigo-700'], [item.isFeatured ? 'unfeature' : 'feature', item.isFeatured ? 'Bỏ nổi bật' : 'Nổi bật', 'bg-amber-50 text-amber-700']] : []),
    ...(tab === 'policies' || tab === 'faqs' ? [[item.isHighlighted ? 'unhighlight' : 'highlight', item.isHighlighted ? 'Bỏ nổi bật' : 'Nổi bật', 'bg-amber-50 text-amber-700']] : []),
  ];

  return (
    <TourManagementShell
      pageKey="content"
      title="Nội dung tour"
      subtitle="Quản lý liên hệ, hình ảnh, chính sách và FAQ theo tour đang chọn."
      error={error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <select value={tourId} onChange={(event) => updateSearchParams(setSearchParams, { tourId: event.target.value, itemId: '' })} className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none">
            {tours.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
          <button type="button" onClick={() => updateSearchParams(setSearchParams, { itemId: '' })} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} />
            Tạo mới
          </button>
        </div>
      )}
    >
      <div className="flex flex-wrap gap-3">
        {TABS.map((item) => (
          <button key={item.key} type="button" onClick={() => updateSearchParams(setSearchParams, { tab: item.key, itemId: '' })} className={`px-5 py-3 rounded-2xl text-[11px] font-black uppercase tracking-widest transition-all ${tab === item.key ? 'bg-white text-blue-600 shadow-sm border border-slate-100' : 'bg-slate-100 text-slate-400 hover:text-slate-600 border border-transparent'}`}>
            {item.label}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1fr,0.95fr] gap-6">
        <div className="space-y-4">
          {loading ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400 flex items-center gap-3"><Loader2 size={16} className="animate-spin" />Đang tải dữ liệu...</div>
          ) : items.length === 0 ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400">Tour này chưa có dữ liệu cho mục hiện tại.</div>
          ) : items.map((item) => (
            <div key={item.id} className={`bg-white rounded-[2rem] border shadow-sm ${itemId === item.id ? 'border-blue-200' : 'border-slate-100'}`}>
              <button type="button" onClick={() => updateSearchParams(setSearchParams, { itemId: item.id })} className="w-full text-left p-6">
                <div className="flex flex-wrap items-center gap-2">
                  {item.isDeleted && <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã xóa mềm</span>}
                  {item.isPrimary && <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-sky-100 text-sky-700">Chính</span>}
                  {item.isCover && <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-indigo-100 text-indigo-700">Bìa</span>}
                  {(item.isFeatured || item.isHighlighted) && <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Nổi bật</span>}
                </div>
                <p className="text-xl font-black text-slate-900 mt-3 truncate">{getTitle(tab, item)}</p>
                <p className="text-xs font-black uppercase tracking-widest text-slate-400 mt-1">{getSubtitle(tab, item)}</p>
              </button>
              <div className="px-6 pb-6 flex flex-wrap items-center gap-2">
                {actionGroups(item).map(([key, label, cls]) => (
                  <button key={key} type="button" onClick={() => handleAction(item, key)} className={`px-4 py-2 rounded-xl text-[11px] font-black uppercase tracking-widest ${cls}`}>{label}</button>
                ))}
              </div>
            </div>
          ))}
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">{detail ? 'Cập nhật dữ liệu' : 'Tạo dữ liệu mới'}</h2>
            <p className="text-slate-500 font-medium mt-1">{selectedTour ? `Tour đang chọn: ${selectedTour.name}` : 'Chọn tour để quản lý nội dung.'}</p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            {tab === 'contacts' && (
              <>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tên liên hệ</span><input name="name" value={form.name} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required /></label>
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Loại liên hệ</span><select name="contactType" value={form.contactType} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">{CONTACT_TYPE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Chức danh</span><input name="title" value={form.title} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Bộ phận</span><input name="department" value={form.department} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Điện thoại</span><input name="phone" value={form.phone} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Email</span><input name="email" value={form.email} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
                </div>
              </>
            )}

            {tab === 'images' && (
              <>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">URL hình ảnh</span><input name="imageUrl" value={form.imageUrl} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required /></label>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tiêu đề</span><input name="title" value={form.title} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Alt text</span><input name="altText" value={form.altText} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
                </div>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Caption</span><textarea name="caption" value={form.caption} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" /></label>
              </>
            )}

            {tab === 'policies' && (
              <>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mã chính sách</span><input name="code" value={form.code} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required /></label>
                  <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Loại chính sách</span><select name="type" value={form.type} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">{POLICY_TYPE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
                </div>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tên chính sách</span><input name="name" value={form.name} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required /></label>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mô tả ngắn</span><textarea name="shortDescription" value={form.shortDescription} onChange={handleFieldChange} rows={2} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" /></label>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Nội dung chi tiết</span><textarea name="descriptionMarkdown" value={form.descriptionMarkdown} onChange={handleFieldChange} rows={5} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" /></label>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Policy JSON</span><textarea name="policyJson" value={form.policyJson} onChange={handleFieldChange} rows={3} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" /></label>
              </>
            )}

            {tab === 'faqs' && (
              <>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Câu hỏi</span><input name="question" value={form.question} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required /></label>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Loại FAQ</span><select name="type" value={form.type} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">{FAQ_TYPE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
                <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Câu trả lời</span><textarea name="answerMarkdown" value={form.answerMarkdown} onChange={handleFieldChange} rows={6} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" required /></label>
              </>
            )}

            <div className="grid grid-cols-2 gap-3">
              {extraFlags.concat([['isActive', 'Đang hoạt động']]).map(([key, label]) => (
                <label key={key} className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700">
                  <input type="checkbox" name={key} checked={Boolean(form[key])} onChange={handleFieldChange} className="h-4 w-4 rounded border-slate-300 text-blue-600" />
                  {label}
                </label>
              ))}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Thứ tự</span><input type="number" min="0" name="sortOrder" value={form.sortOrder} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" /></label>
              <div className="rounded-2xl bg-slate-50 border border-slate-100 px-5 py-4 text-xs font-bold text-slate-500 flex items-center">{detail ? `Cập nhật lần cuối: ${formatDateTime(detail.updatedAt || detail.createdAt)}` : 'Mục mới sẽ được lưu ngay vào tour đang chọn.'}</div>
            </div>

            <label className="space-y-2 block"><span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ghi chú nội bộ</span><textarea name="notes" value={form.notes} onChange={handleFieldChange} rows={3} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" /></label>

            <button type="submit" disabled={saving || !tourId} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
              {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
              {detail ? 'Lưu thay đổi' : 'Tạo mới'}
            </button>
          </form>
        </div>
      </div>
    </TourManagementShell>
  );
}
