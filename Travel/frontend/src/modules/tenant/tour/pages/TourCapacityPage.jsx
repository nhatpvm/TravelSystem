import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Loader2, Save } from 'lucide-react';
import TourManagementShell from '../components/TourManagementShell';
import {
  createManagerTourCapacity,
  getManagerTourCapacity,
  listManagerTourSchedules,
  listManagerTours,
  toggleManagerTourCapacityAction,
  updateManagerTourCapacity,
} from '../../../../services/tourService';
import {
  formatDateTime,
  getCapacityStatusClass,
  getCapacityStatusLabel,
} from '../../../tours/utils/presentation';
import { CAPACITY_STATUS_OPTIONS, toNullableNumber, toNumberOrDefault, updateSearchParams } from '../utils/options';
import { getTourManagementSectionPath } from '../utils/navigation';

const EMPTY_FORM = {
  totalSlots: '',
  soldSlots: '',
  heldSlots: '',
  blockedSlots: '',
  minGuestsToOperate: '',
  maxGuestsPerBooking: '',
  warningThreshold: '',
  status: 1,
  allowWaitlist: false,
  autoCloseWhenFull: true,
  notes: '',
  isActive: true,
  rowVersionBase64: '',
};

function buildFormFromCapacity(capacity) {
  if (!capacity) {
    return EMPTY_FORM;
  }

  return {
    totalSlots: capacity.totalSlots ?? '',
    soldSlots: capacity.soldSlots ?? '',
    heldSlots: capacity.heldSlots ?? '',
    blockedSlots: capacity.blockedSlots ?? '',
    minGuestsToOperate: capacity.minGuestsToOperate ?? '',
    maxGuestsPerBooking: capacity.maxGuestsPerBooking ?? '',
    warningThreshold: capacity.warningThreshold ?? '',
    status: Number(capacity.status ?? 1),
    allowWaitlist: Boolean(capacity.allowWaitlist),
    autoCloseWhenFull: Boolean(capacity.autoCloseWhenFull),
    notes: capacity.notes || '',
    isActive: Boolean(capacity.isActive),
    rowVersionBase64: capacity.rowVersionBase64 || '',
  };
}

export default function TourCapacityPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tours, setTours] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [capacity, setCapacity] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedTourId = searchParams.get('tourId') || '';
  const selectedScheduleId = searchParams.get('scheduleId') || '';

  useEffect(() => {
    loadTours();
  }, []);

  useEffect(() => {
    if (selectedTourId) {
      loadSchedules(selectedTourId);
    } else {
      setSchedules([]);
    }
  }, [selectedTourId]);

  useEffect(() => {
    if (selectedTourId && selectedScheduleId) {
      loadCapacity(selectedTourId, selectedScheduleId);
    } else {
      setCapacity(null);
      setForm(EMPTY_FORM);
    }
  }, [selectedTourId, selectedScheduleId]);

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
        updateSearchParams(setSearchParams, { tourId: items[0].id, scheduleId: '' });
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
        updateSearchParams(setSearchParams, { scheduleId: items[0].id });
      }

      if (selectedScheduleId && !items.some((item) => item.id === selectedScheduleId)) {
        updateSearchParams(setSearchParams, { scheduleId: items[0]?.id || '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải lịch khởi hành.');
    }
  }

  async function loadCapacity(tourId, scheduleId) {
    setLoading(true);
    setError('');

    try {
      const response = await getManagerTourCapacity(tourId, scheduleId, { includeDeleted: true });
      setCapacity(response);
      setForm(buildFormFromCapacity(response));
    } catch (requestError) {
      if (requestError.status === 404) {
        setCapacity(null);
        setForm(EMPTY_FORM);
      } else {
        setError(requestError.message || 'Không thể tải sức chứa.');
      }
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
      setError('Vui lòng chọn tour và lịch khởi hành trước khi lưu sức chứa.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    const payload = {
      totalSlots: toNumberOrDefault(form.totalSlots, 0),
      soldSlots: toNullableNumber(form.soldSlots),
      heldSlots: toNullableNumber(form.heldSlots),
      blockedSlots: toNullableNumber(form.blockedSlots),
      minGuestsToOperate: toNullableNumber(form.minGuestsToOperate),
      maxGuestsPerBooking: toNullableNumber(form.maxGuestsPerBooking),
      warningThreshold: toNullableNumber(form.warningThreshold),
      status: toNumberOrDefault(form.status, 1),
      allowWaitlist: Boolean(form.allowWaitlist),
      autoCloseWhenFull: Boolean(form.autoCloseWhenFull),
      notes: form.notes || null,
      isActive: Boolean(form.isActive),
      rowVersionBase64: form.rowVersionBase64 || undefined,
    };

    try {
      if (capacity?.id) {
        await updateManagerTourCapacity(selectedTourId, selectedScheduleId, payload);
        setNotice('Đã cập nhật sức chứa.');
      } else {
        await createManagerTourCapacity(selectedTourId, selectedScheduleId, payload);
        setNotice('Đã tạo cấu hình sức chứa.');
      }

      await loadCapacity(selectedTourId, selectedScheduleId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu sức chứa.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(action) {
    setError('');
    setNotice('');

    try {
      await toggleManagerTourCapacityAction(selectedTourId, selectedScheduleId, action);
      setNotice('Đã cập nhật trạng thái sức chứa.');
      await loadCapacity(selectedTourId, selectedScheduleId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái sức chứa.');
    }
  }

  return (
    <TourManagementShell
      pageKey="capacity"
      title="Sức chứa tour"
      subtitle="Quản lý tổng chỗ, chỗ đã bán, chỗ đang giữ và ngưỡng cảnh báo cho từng lịch khởi hành."
      error={error}
      notice={notice}
      actions={(
        <div className="flex flex-wrap items-center gap-3">
          <select value={selectedTourId} onChange={(event) => updateSearchParams(setSearchParams, { tourId: event.target.value, scheduleId: '' })} className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none">
            {tours.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          <select value={selectedScheduleId} onChange={(event) => updateSearchParams(setSearchParams, { scheduleId: event.target.value })} className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none">
            {schedules.map((item) => (
              <option key={item.id} value={item.id}>{item.name || item.code}</option>
            ))}
          </select>
        </div>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr,1.05fr] gap-6">
        <div className="space-y-4">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <div className="p-8 border-b border-slate-100">
              <h2 className="text-2xl font-black text-slate-900 tracking-tight">Tổng quan sức chứa</h2>
              <p className="text-slate-500 font-medium mt-1">
                {selectedTour && selectedSchedule ? `${selectedTour.name} · ${selectedSchedule.name || selectedSchedule.code}` : 'Chọn lịch khởi hành để xem sức chứa.'}
              </p>
            </div>

            {loading ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-400 flex items-center gap-3">
                <Loader2 size={16} className="animate-spin" />
                Đang tải sức chứa...
              </div>
            ) : !capacity ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-400">
                Lịch này chưa có cấu hình sức chứa. Bạn có thể tạo mới ngay ở khung bên phải.
              </div>
            ) : (
              <div className="p-8 space-y-5">
                <div className="flex flex-wrap items-center gap-2">
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getCapacityStatusClass(capacity.status)}`}>
                    {getCapacityStatusLabel(capacity.status)}
                  </span>
                  {!capacity.isActive && (
                    <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                      Tạm khóa
                    </span>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-4">
                  {[
                    { label: 'Tổng chỗ', value: capacity.totalSlots },
                    { label: 'Đã bán', value: capacity.soldSlots },
                    { label: 'Đang giữ', value: capacity.heldSlots },
                    { label: 'Khả dụng', value: capacity.availableSlots },
                  ].map((item) => (
                    <div key={item.label} className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4">
                      <p className="text-3xl font-black text-slate-900">{item.value}</p>
                      <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
                    </div>
                  ))}
                </div>

                <div className="rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-500 space-y-2">
                  <p>Khách tối đa mỗi booking: {capacity.maxGuestsPerBooking ?? '--'}</p>
                  <p>Ngưỡng cảnh báo: {capacity.warningThreshold ?? '--'}</p>
                  <p>Cho phép chờ: {capacity.allowWaitlist ? 'Có' : 'Không'}</p>
                  <p>Tự đóng khi đầy: {capacity.autoCloseWhenFull ? 'Có' : 'Không'}</p>
                  <p>Cập nhật lần cuối: {formatDateTime(capacity.updatedAt || capacity.createdAt)}</p>
                </div>

                <div className="flex flex-wrap gap-2">
                  <button type="button" onClick={() => handleAction('open')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest">Mở bán</button>
                  <button type="button" onClick={() => handleAction('limited')} className="px-4 py-2 rounded-xl bg-amber-50 text-amber-700 text-[11px] font-black uppercase tracking-widest">Sắp đầy</button>
                  <button type="button" onClick={() => handleAction('full')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest">Đầy</button>
                  <button type="button" onClick={() => handleAction('close')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest">Đóng</button>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              {capacity ? 'Cập nhật sức chứa' : 'Tạo sức chứa'}
            </h2>
            <p className="text-slate-500 font-medium mt-1">Giữ dữ liệu sức chứa chuẩn để quote và booking không bị vượt chỗ.</p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tổng chỗ</span>
                <input type="number" min="0" name="totalSlots" value={form.totalSlots} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Đã bán</span>
                <input type="number" min="0" name="soldSlots" value={form.soldSlots} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Đang giữ</span>
                <input type="number" min="0" name="heldSlots" value={form.heldSlots} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Đang chặn</span>
                <input type="number" min="0" name="blockedSlots" value={form.blockedSlots} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select name="status" value={form.status} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {CAPACITY_STATUS_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tối đa / booking</span>
                <input type="number" min="0" name="maxGuestsPerBooking" value={form.maxGuestsPerBooking} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ngưỡng cảnh báo</span>
                <input type="number" min="0" name="warningThreshold" value={form.warningThreshold} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ghi chú</span>
              <textarea name="notes" value={form.notes} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <div className="grid grid-cols-2 gap-3">
              {[
                { name: 'allowWaitlist', label: 'Cho phép danh sách chờ' },
                { name: 'autoCloseWhenFull', label: 'Tự đóng khi đầy' },
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
                {capacity ? 'Lưu sức chứa' : 'Tạo sức chứa'}
              </button>
              {selectedSchedule && (
                <Link to={getTourManagementSectionPath('packages', { tourId: selectedTourId, scheduleId: selectedSchedule.id })} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest border border-slate-100 shadow-sm">
                  Sang gói tour
                </Link>
              )}
            </div>
          </form>
        </div>
      </div>
    </TourManagementShell>
  );
}
