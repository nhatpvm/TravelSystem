import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Loader2, Plus, Save } from 'lucide-react';
import TourManagementShell from '../components/TourManagementShell';
import {
  createManagerTourSchedule,
  listManagerTourSchedules,
  listManagerTours,
  toggleManagerTourScheduleAction,
  updateManagerTourSchedule,
} from '../../../../services/tourService';
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  formatTime,
  getScheduleStatusClass,
  getScheduleStatusLabel,
} from '../../../tours/utils/presentation';
import { SCHEDULE_STATUS_OPTIONS, toNullableNumber, toNullableText, toNumberOrDefault, updateSearchParams } from '../utils/options';
import { getTourManagementSectionPath } from '../utils/navigation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

function toDateInput(daysToAdd = 0) {
  const date = new Date();
  date.setDate(date.getDate() + daysToAdd);
  return date.toISOString().slice(0, 10);
}

const EMPTY_FORM = {
  code: '',
  name: '',
  departureDate: toDateInput(7),
  returnDate: toDateInput(9),
  departureTime: '08:00',
  returnTime: '18:00',
  status: 0,
  maxGuests: '',
  minGuestsToOperate: '',
  meetingPointSummary: '',
  pickupSummary: '',
  dropoffSummary: '',
  notes: '',
  isGuaranteedDeparture: false,
  isInstantConfirm: false,
  isFeatured: false,
  isActive: true,
  rowVersionBase64: '',
};

function buildFormFromSchedule(schedule) {
  if (!schedule) {
    return EMPTY_FORM;
  }

  return {
    code: schedule.code || '',
    name: schedule.name || '',
    departureDate: schedule.departureDate || toDateInput(7),
    returnDate: schedule.returnDate || toDateInput(9),
    departureTime: schedule.departureTime ? String(schedule.departureTime).slice(0, 5) : '08:00',
    returnTime: schedule.returnTime ? String(schedule.returnTime).slice(0, 5) : '18:00',
    status: Number(schedule.status ?? 0),
    maxGuests: schedule.maxGuests ?? '',
    minGuestsToOperate: schedule.minGuestsToOperate ?? '',
    meetingPointSummary: schedule.meetingPointSummary || '',
    pickupSummary: schedule.pickupSummary || '',
    dropoffSummary: schedule.dropoffSummary || '',
    notes: schedule.notes || '',
    isGuaranteedDeparture: Boolean(schedule.isGuaranteedDeparture),
    isInstantConfirm: Boolean(schedule.isInstantConfirm),
    isFeatured: Boolean(schedule.isFeatured),
    isActive: Boolean(schedule.isActive),
    rowVersionBase64: schedule.rowVersionBase64 || '',
  };
}

export default function TourSchedulesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tours, setTours] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [selectedSchedule, setSelectedSchedule] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedTourId = searchParams.get('tourId') || '';
  const selectedScheduleId = searchParams.get('scheduleId') || '';

  const loadToursRef = useLatestRef(loadTours);
  const loadSchedulesRef = useLatestRef(loadSchedules);

  useEffect(() => {
    loadToursRef.current();
  }, [loadToursRef]);

  useEffect(() => {
    if (selectedTourId) {
      loadSchedulesRef.current(selectedTourId);
    } else {
      setSchedules([]);
      setSelectedSchedule(null);
      setForm(EMPTY_FORM);
    }
  }, [loadSchedulesRef, selectedTourId]);

  useEffect(() => {
    const schedule = schedules.find((item) => item.id === selectedScheduleId) || null;
    setSelectedSchedule(schedule);
    setForm(buildFormFromSchedule(schedule));
  }, [schedules, selectedScheduleId]);

  const selectedTour = useMemo(
    () => tours.find((item) => item.id === selectedTourId) || null,
    [selectedTourId, tours],
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
    setLoading(true);
    setError('');

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
      setSchedules([]);
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
    if (!selectedTourId) {
      setError('Vui lòng chọn tour trước khi lưu lịch khởi hành.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    const payload = {
      code: form.code.trim(),
      name: toNullableText(form.name),
      departureDate: form.departureDate,
      returnDate: form.returnDate,
      departureTime: form.departureTime || null,
      returnTime: form.returnTime || null,
      status: toNumberOrDefault(form.status, 0),
      maxGuests: toNullableNumber(form.maxGuests),
      minGuestsToOperate: toNullableNumber(form.minGuestsToOperate),
      meetingPointSummary: toNullableText(form.meetingPointSummary),
      pickupSummary: toNullableText(form.pickupSummary),
      dropoffSummary: toNullableText(form.dropoffSummary),
      notes: toNullableText(form.notes),
      isGuaranteedDeparture: Boolean(form.isGuaranteedDeparture),
      isInstantConfirm: Boolean(form.isInstantConfirm),
      isFeatured: Boolean(form.isFeatured),
      isActive: Boolean(form.isActive),
      rowVersionBase64: form.rowVersionBase64 || undefined,
    };

    try {
      if (selectedSchedule?.id) {
        await updateManagerTourSchedule(selectedTourId, selectedSchedule.id, payload);
        setNotice('Đã cập nhật lịch khởi hành.');
      } else {
        const created = await createManagerTourSchedule(selectedTourId, payload);
        setNotice('Đã tạo lịch khởi hành mới.');
        updateSearchParams(setSearchParams, { scheduleId: created.id });
      }

      await loadSchedulesRef.current(selectedTourId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu lịch khởi hành.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(schedule, action) {
    setError('');
    setNotice('');

    try {
      await toggleManagerTourScheduleAction(selectedTourId, schedule.id, action);
      setNotice('Đã cập nhật trạng thái lịch khởi hành.');
      await loadSchedulesRef.current(selectedTourId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật lịch khởi hành.');
    }
  }

  return (
    <TourManagementShell
      pageKey="schedules"
      title="Lịch khởi hành"
      subtitle="Quản lý ngày đi, ngày về, giờ xuất phát và trạng thái mở bán của từng đợt tour."
      error={error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <select
            value={selectedTourId}
            onChange={(event) => updateSearchParams(setSearchParams, { tourId: event.target.value, scheduleId: '' })}
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none"
          >
            {tours.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          <button type="button" onClick={() => updateSearchParams(setSearchParams, { scheduleId: '' })} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} />
            Tạo lịch mới
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1.1fr,0.9fr] gap-6">
        <div className="space-y-4">
          {loading ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400 flex items-center gap-3">
              <Loader2 size={16} className="animate-spin" />
              Đang tải lịch khởi hành...
            </div>
          ) : schedules.length === 0 ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400">
              Tour này chưa có lịch khởi hành nào.
            </div>
          ) : schedules.map((schedule) => (
            <div key={schedule.id} className={`bg-white rounded-[2rem] border shadow-sm ${selectedScheduleId === schedule.id ? 'border-blue-200' : 'border-slate-100'}`}>
              <button type="button" onClick={() => updateSearchParams(setSearchParams, { scheduleId: schedule.id })} className="w-full text-left p-6">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getScheduleStatusClass(schedule.status)}`}>
                        {getScheduleStatusLabel(schedule.status)}
                      </span>
                      {schedule.isFeatured && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">
                          Nổi bật
                        </span>
                      )}
                      {schedule.isDeleted && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                          Đã xóa mềm
                        </span>
                      )}
                    </div>
                    <p className="text-xl font-black text-slate-900 mt-3">{schedule.name || schedule.code}</p>
                    <p className="text-xs font-black uppercase tracking-widest text-slate-400 mt-1">{schedule.code}</p>
                    <div className="flex flex-wrap items-center gap-4 mt-4 text-sm font-medium text-slate-500">
                      <span>{formatDate(schedule.departureDate)} - {formatDate(schedule.returnDate)}</span>
                      <span>{formatTime(schedule.departureTime)} - {formatTime(schedule.returnTime)}</span>
                      <span>{schedule.availableSlots ?? '--'} chỗ trống</span>
                      <span>{formatCurrency(schedule.adultPrice, schedule.currencyCode)}</span>
                    </div>
                  </div>
                  <Link
                    to={getTourManagementSectionPath('pricing', { tourId: selectedTourId, scheduleId: schedule.id })}
                    className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest"
                  >
                    Xem giá
                  </Link>
                </div>
              </button>
              <div className="px-6 pb-6 flex flex-wrap items-center gap-2">
                {schedule.isDeleted ? (
                  <button type="button" onClick={() => handleAction(schedule, 'restore')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest">
                    Khôi phục
                  </button>
                ) : (
                  <button type="button" onClick={() => handleAction(schedule, 'delete')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest">
                    Xóa mềm
                  </button>
                )}
                <button type="button" onClick={() => handleAction(schedule, schedule.isActive ? 'deactivate' : 'activate')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest">
                  {schedule.isActive ? 'Tạm khóa' : 'Kích hoạt'}
                </button>
                <button type="button" onClick={() => handleAction(schedule, schedule.status === 1 ? 'close' : 'open')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest">
                  {schedule.status === 1 ? 'Đóng bán' : 'Mở bán'}
                </button>
                <button type="button" onClick={() => handleAction(schedule, schedule.isFeatured ? 'unfeature' : 'feature')} className="px-4 py-2 rounded-xl bg-amber-50 text-amber-700 text-[11px] font-black uppercase tracking-widest">
                  {schedule.isFeatured ? 'Bỏ nổi bật' : 'Nổi bật'}
                </button>
              </div>
            </div>
          ))}
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              {selectedSchedule ? 'Cập nhật lịch khởi hành' : 'Tạo lịch khởi hành'}
            </h2>
            <p className="text-slate-500 font-medium mt-1">
              {selectedTour ? `Tour đang chọn: ${selectedTour.name}` : 'Vui lòng chọn tour để tạo lịch.'}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mã lịch</span>
                <input name="code" value={form.code} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tên hiển thị</span>
                <input name="name" value={form.name} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ngày đi</span>
                <input type="date" name="departureDate" value={form.departureDate} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ngày về</span>
                <input type="date" name="returnDate" value={form.returnDate} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Giờ đi</span>
                <input type="time" name="departureTime" value={form.departureTime} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Giờ về</span>
                <input type="time" name="returnTime" value={form.returnTime} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select name="status" value={form.status} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {SCHEDULE_STATUS_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Khách tối đa</span>
                <input type="number" min="0" name="maxGuests" value={form.maxGuests} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Vận hành từ</span>
                <input type="number" min="0" name="minGuestsToOperate" value={form.minGuestsToOperate} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Điểm đón / tập trung</span>
              <input name="meetingPointSummary" value={form.meetingPointSummary} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
            </label>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tóm tắt đón</span>
                <input name="pickupSummary" value={form.pickupSummary} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tóm tắt trả</span>
                <input name="dropoffSummary" value={form.dropoffSummary} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Ghi chú vận hành</span>
              <textarea name="notes" value={form.notes} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <div className="grid grid-cols-2 gap-3">
              {[
                { name: 'isGuaranteedDeparture', label: 'Đảm bảo khởi hành' },
                { name: 'isInstantConfirm', label: 'Xác nhận tức thì' },
                { name: 'isFeatured', label: 'Nổi bật' },
                { name: 'isActive', label: 'Đang hoạt động' },
              ].map((item) => (
                <label key={item.name} className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700">
                  <input type="checkbox" name={item.name} checked={Boolean(form[item.name])} onChange={handleFieldChange} className="h-4 w-4 rounded border-slate-300 text-blue-600" />
                  {item.label}
                </label>
              ))}
            </div>

            {selectedSchedule && (
              <div className="rounded-2xl bg-slate-50 border border-slate-100 px-5 py-4 text-xs font-bold text-slate-500">
                Cập nhật lần cuối: {formatDateTime(selectedSchedule.updatedAt || selectedSchedule.createdAt)}
              </div>
            )}

            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={saving || !selectedTourId} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
                {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                {selectedSchedule ? 'Lưu lịch' : 'Tạo lịch'}
              </button>
              {selectedSchedule && (
                <>
                  <Link to={getTourManagementSectionPath('pricing', { tourId: selectedTourId, scheduleId: selectedSchedule.id })} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest border border-slate-100 shadow-sm">
                    Sang bảng giá
                  </Link>
                  <Link to={getTourManagementSectionPath('capacity', { tourId: selectedTourId, scheduleId: selectedSchedule.id })} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest border border-slate-100 shadow-sm">
                    Sang sức chứa
                  </Link>
                </>
              )}
            </div>
          </form>
        </div>
      </div>
    </TourManagementShell>
  );
}
