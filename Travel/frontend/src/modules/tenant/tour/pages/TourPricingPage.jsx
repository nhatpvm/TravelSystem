import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { CircleDollarSign, Loader2, Plus, Save } from 'lucide-react';
import TourManagementShell from '../components/TourManagementShell';
import {
  createManagerTourPrice,
  listManagerTourPrices,
  listManagerTourSchedules,
  listManagerTours,
  toggleManagerTourPriceAction,
  updateManagerTourPrice,
} from '../../../../services/tourService';
import { formatCurrency, getPriceTypeLabel } from '../../../tours/utils/presentation';
import { PRICE_TYPE_OPTIONS, toNullableNumber, toNullableText, toNumberOrDefault, updateSearchParams } from '../utils/options';
import { getTourManagementSectionPath } from '../utils/navigation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

const EMPTY_FORM = {
  priceType: 1,
  currencyCode: 'VND',
  price: '',
  originalPrice: '',
  taxes: '',
  fees: '',
  minAge: '',
  maxAge: '',
  minQuantity: '',
  maxQuantity: '',
  isDefault: false,
  isIncludedTax: true,
  isIncludedFee: true,
  isActive: true,
  label: '',
  notes: '',
  rowVersionBase64: '',
};

function buildFormFromPrice(price) {
  if (!price) {
    return EMPTY_FORM;
  }

  return {
    priceType: Number(price.priceType ?? 1),
    currencyCode: price.currencyCode || 'VND',
    price: price.price ?? '',
    originalPrice: price.originalPrice ?? '',
    taxes: price.taxes ?? '',
    fees: price.fees ?? '',
    minAge: price.minAge ?? '',
    maxAge: price.maxAge ?? '',
    minQuantity: price.minQuantity ?? '',
    maxQuantity: price.maxQuantity ?? '',
    isDefault: Boolean(price.isDefault),
    isIncludedTax: Boolean(price.isIncludedTax),
    isIncludedFee: Boolean(price.isIncludedFee),
    isActive: Boolean(price.isActive),
    label: price.label || '',
    notes: price.notes || '',
    rowVersionBase64: price.rowVersionBase64 || '',
  };
}

export default function TourPricingPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tours, setTours] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [prices, setPrices] = useState([]);
  const [selectedPrice, setSelectedPrice] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedTourId = searchParams.get('tourId') || '';
  const selectedScheduleId = searchParams.get('scheduleId') || '';
  const selectedPriceId = searchParams.get('priceId') || '';

  const loadToursRef = useLatestRef(loadTours);
  const loadSchedulesRef = useLatestRef(loadSchedules);
  const loadPricesRef = useLatestRef(loadPrices);

  useEffect(() => {
    loadToursRef.current();
  }, [loadToursRef]);

  useEffect(() => {
    if (selectedTourId) {
      loadSchedulesRef.current(selectedTourId);
    } else {
      setSchedules([]);
      setPrices([]);
    }
  }, [loadSchedulesRef, selectedTourId]);

  useEffect(() => {
    if (selectedTourId && selectedScheduleId) {
      loadPricesRef.current(selectedTourId, selectedScheduleId);
    } else {
      setPrices([]);
      setSelectedPrice(null);
      setForm(EMPTY_FORM);
    }
  }, [selectedTourId, selectedScheduleId, loadPricesRef]);

  useEffect(() => {
    const price = prices.find((item) => item.id === selectedPriceId) || null;
    setSelectedPrice(price);
    setForm(buildFormFromPrice(price));
  }, [prices, selectedPriceId]);

  const selectedTour = useMemo(
    () => tours.find((item) => item.id === selectedTourId) || null,
    [selectedTourId, tours],
  );

  const selectedSchedule = useMemo(
    () => schedules.find((item) => item.id === selectedScheduleId) || null,
    [selectedScheduleId, schedules],
  );

  async function loadTours() {
    try {
      const response = await listManagerTours({ page: 1, pageSize: 100, includeDeleted: true });
      const items = response.items || [];
      setTours(items);

      if (!selectedTourId && items.length) {
        updateSearchParams(setSearchParams, { tourId: items[0].id, scheduleId: '', priceId: '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách tour.');
    }
  }

  async function loadSchedules(tourId) {
    try {
      const response = await listManagerTourSchedules(tourId, { page: 1, pageSize: 100, includeDeleted: true });
      const items = response.items || [];
      setSchedules(items);

      if (!selectedScheduleId && items.length) {
        updateSearchParams(setSearchParams, { scheduleId: items[0].id, priceId: '' });
      }

      if (selectedScheduleId && !items.some((item) => item.id === selectedScheduleId)) {
        updateSearchParams(setSearchParams, { scheduleId: items[0]?.id || '', priceId: '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải lịch khởi hành.');
    }
  }

  async function loadPrices(tourId, scheduleId) {
    setLoading(true);
    setError('');

    try {
      const response = await listManagerTourPrices(tourId, scheduleId, { page: 1, pageSize: 100, includeDeleted: true });
      const items = response.items || [];
      setPrices(items);

      if (!selectedPriceId && items.length) {
        updateSearchParams(setSearchParams, { priceId: items[0].id });
      }

      if (selectedPriceId && !items.some((item) => item.id === selectedPriceId)) {
        updateSearchParams(setSearchParams, { priceId: items[0]?.id || '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải bảng giá.');
      setPrices([]);
    } finally {
      setLoading(false);
    }
  }

  function handleFieldChange(event) {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!selectedTourId || !selectedScheduleId) {
      setError('Vui lòng chọn tour và lịch khởi hành trước khi lưu giá.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    const payload = {
      priceType: toNumberOrDefault(form.priceType, 1),
      currencyCode: (form.currencyCode || 'VND').trim() || 'VND',
      price: toNumberOrDefault(form.price, 0),
      originalPrice: toNullableNumber(form.originalPrice),
      taxes: toNullableNumber(form.taxes),
      fees: toNullableNumber(form.fees),
      minAge: toNullableNumber(form.minAge),
      maxAge: toNullableNumber(form.maxAge),
      minQuantity: toNullableNumber(form.minQuantity),
      maxQuantity: toNullableNumber(form.maxQuantity),
      isDefault: Boolean(form.isDefault),
      isIncludedTax: Boolean(form.isIncludedTax),
      isIncludedFee: Boolean(form.isIncludedFee),
      isActive: Boolean(form.isActive),
      label: toNullableText(form.label),
      notes: toNullableText(form.notes),
      rowVersionBase64: form.rowVersionBase64 || undefined,
    };

    try {
      if (selectedPrice?.id) {
        await updateManagerTourPrice(selectedTourId, selectedScheduleId, selectedPrice.id, payload);
        setNotice('Đã cập nhật mức giá.');
      } else {
        const created = await createManagerTourPrice(selectedTourId, selectedScheduleId, payload);
        setNotice('Đã tạo mức giá mới.');
        updateSearchParams(setSearchParams, { priceId: created.id });
      }

      await loadPricesRef.current(selectedTourId, selectedScheduleId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu mức giá.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(price, action) {
    setError('');
    setNotice('');

    try {
      await toggleManagerTourPriceAction(selectedTourId, selectedScheduleId, price.id, action);
      setNotice('Đã cập nhật trạng thái mức giá.');
      await loadPricesRef.current(selectedTourId, selectedScheduleId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật mức giá.');
    }
  }

  return (
    <TourManagementShell
      pageKey="pricing"
      title="Bảng giá tour"
      subtitle="Quản lý giá người lớn, trẻ em, phụ thu và mức giá mặc định cho từng lịch khởi hành."
      error={error}
      notice={notice}
      actions={(
        <div className="flex flex-wrap items-center gap-3">
          <select value={selectedTourId} onChange={(event) => updateSearchParams(setSearchParams, { tourId: event.target.value, scheduleId: '', priceId: '' })} className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none">
            {tours.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          <select value={selectedScheduleId} onChange={(event) => updateSearchParams(setSearchParams, { scheduleId: event.target.value, priceId: '' })} className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none">
            {schedules.map((item) => (
              <option key={item.id} value={item.id}>{item.name || item.code}</option>
            ))}
          </select>
          <button type="button" onClick={() => updateSearchParams(setSearchParams, { priceId: '' })} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} />
            Thêm mức giá
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1.1fr,0.9fr] gap-6">
        <div className="space-y-4">
          {loading ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400 flex items-center gap-3">
              <Loader2 size={16} className="animate-spin" />
              Đang tải bảng giá...
            </div>
          ) : prices.length === 0 ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400">
              Lịch khởi hành này chưa có mức giá nào.
            </div>
          ) : prices.map((price) => (
            <div key={price.id} className={`bg-white rounded-[2rem] border shadow-sm ${selectedPriceId === price.id ? 'border-blue-200' : 'border-slate-100'}`}>
              <button type="button" onClick={() => updateSearchParams(setSearchParams, { priceId: price.id })} className="w-full text-left p-6">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                        {getPriceTypeLabel(price.priceType)}
                      </span>
                      {price.isDefault && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-emerald-100 text-emerald-700">
                          Mặc định
                        </span>
                      )}
                      {price.isDeleted && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                          Đã xóa mềm
                        </span>
                      )}
                    </div>
                    <p className="text-xl font-black text-slate-900 mt-3">{formatCurrency(price.price, price.currencyCode)}</p>
                    <p className="text-sm font-medium text-slate-500 mt-2">{price.label || 'Giá áp dụng mặc định'}</p>
                    <div className="flex flex-wrap items-center gap-4 mt-3 text-sm font-medium text-slate-500">
                      <span>Thuế: {formatCurrency(price.taxes, price.currencyCode)}</span>
                      <span>Phí: {formatCurrency(price.fees, price.currencyCode)}</span>
                      <span>{price.minAge ?? 0} - {price.maxAge ?? '--'} tuổi</span>
                    </div>
                  </div>
                  <div className="w-12 h-12 rounded-2xl bg-slate-100 text-slate-700 flex items-center justify-center">
                    <CircleDollarSign size={18} />
                  </div>
                </div>
              </button>
              <div className="px-6 pb-6 flex flex-wrap items-center gap-2">
                {price.isDeleted ? (
                  <button type="button" onClick={() => handleAction(price, 'restore')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest">
                    Khôi phục
                  </button>
                ) : (
                  <button type="button" onClick={() => handleAction(price, 'delete')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest">
                    Xóa mềm
                  </button>
                )}
                <button type="button" onClick={() => handleAction(price, price.isActive ? 'deactivate' : 'activate')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest">
                  {price.isActive ? 'Tạm khóa' : 'Kích hoạt'}
                </button>
                {!price.isDefault && (
                  <button type="button" onClick={() => handleAction(price, 'set-default')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest">
                    Chọn mặc định
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              {selectedPrice ? 'Cập nhật mức giá' : 'Tạo mức giá'}
            </h2>
            <p className="text-slate-500 font-medium mt-1">
              {selectedTour && selectedSchedule ? `${selectedTour.name} · ${selectedSchedule.name || selectedSchedule.code}` : 'Chọn tour và lịch khởi hành để quản lý giá.'}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Nhóm giá</span>
                <select name="priceType" value={form.priceType} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {PRICE_TYPE_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mã tiền tệ</span>
                <input name="currencyCode" value={form.currencyCode} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Giá bán</span>
                <input type="number" min="0" name="price" value={form.price} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Giá gốc</span>
                <input type="number" min="0" name="originalPrice" value={form.originalPrice} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Thuế</span>
                <input type="number" min="0" name="taxes" value={form.taxes} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Phí</span>
                <input type="number" min="0" name="fees" value={form.fees} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Độ tuổi từ</span>
                <input type="number" min="0" name="minAge" value={form.minAge} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Độ tuổi đến</span>
                <input type="number" min="0" name="maxAge" value={form.maxAge} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Nhãn hiển thị</span>
              <input name="label" value={form.label} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
            </label>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ghi chú</span>
              <textarea name="notes" value={form.notes} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <div className="grid grid-cols-2 gap-3">
              {[
                { name: 'isDefault', label: 'Đặt làm mặc định' },
                { name: 'isIncludedTax', label: 'Giá đã gồm thuế' },
                { name: 'isIncludedFee', label: 'Giá đã gồm phí' },
                { name: 'isActive', label: 'Đang hoạt động' },
              ].map((item) => (
                <label key={item.name} className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700">
                  <input type="checkbox" name={item.name} checked={Boolean(form[item.name])} onChange={handleFieldChange} className="h-4 w-4 rounded border-slate-300 text-blue-600" />
                  {item.label}
                </label>
              ))}
            </div>

            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={saving || !selectedTourId || !selectedScheduleId} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
                {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                {selectedPrice ? 'Lưu mức giá' : 'Tạo mức giá'}
              </button>
              {selectedSchedule && (
                <Link to={getTourManagementSectionPath('capacity', { tourId: selectedTourId, scheduleId: selectedSchedule.id })} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest border border-slate-100 shadow-sm">
                  Sang sức chứa
                </Link>
              )}
            </div>
          </form>
        </div>
      </div>
    </TourManagementShell>
  );
}
