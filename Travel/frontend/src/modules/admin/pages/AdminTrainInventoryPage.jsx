import React, { useEffect, useState } from 'react';
import { Calendar, ChevronRight, Plus, RefreshCw, Ticket, Train as TrainIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import AdminTrainPageShell from '../train/components/AdminTrainPageShell';
import useAdminTrainScope from '../train/hooks/useAdminTrainScope';
import {
  TRAIN_TRIP_STATUSES,
  formatDateTime,
  getTripStatusClass,
  getTripStatusLabel,
  toApiDateTimeValue,
  toDateTimeInputValue,
} from '../../tenant/train/utils/presentation';
import {
  createAdminTrainTrip,
  deleteAdminTrainTrip,
  getAdminTrainOptions,
  listAdminTrainTrips,
  restoreAdminTrainTrip,
  updateAdminTrainTrip,
} from '../../../services/trainService';

function createEmptyForm() {
  return {
    providerId: '',
    routeId: '',
    trainNumber: '',
    code: '',
    name: '',
    status: 2,
    departureAt: '',
    arrivalAt: '',
    fareRulesJson: '',
    baggagePolicyJson: '',
    boardingPolicyJson: '',
    isActive: true,
  };
}

function buildPayload(form) {
  return {
    providerId: form.providerId,
    routeId: form.routeId,
    trainNumber: form.trainNumber.trim(),
    code: form.code.trim(),
    name: form.name.trim(),
    status: Number(form.status),
    departureAt: toApiDateTimeValue(form.departureAt),
    arrivalAt: toApiDateTimeValue(form.arrivalAt),
    fareRulesJson: form.fareRulesJson || null,
    baggagePolicyJson: form.baggagePolicyJson || null,
    boardingPolicyJson: form.boardingPolicyJson || null,
    isActive: !!form.isActive,
  };
}

function hydrateForm(item) {
  return {
    providerId: item.providerId || '',
    routeId: item.routeId || '',
    trainNumber: item.trainNumber || '',
    code: item.code || '',
    name: item.name || '',
    status: item.status || 2,
    departureAt: toDateTimeInputValue(item.departureAt),
    arrivalAt: toDateTimeInputValue(item.arrivalAt),
    fareRulesJson: item.fareRulesJson || '',
    baggagePolicyJson: item.baggagePolicyJson || '',
    boardingPolicyJson: item.boardingPolicyJson || '',
    isActive: item.isActive ?? true,
  };
}

export default function AdminTrainInventoryPage() {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeLoading,
    scopeError,
  } = useAdminTrainScope();
  const [options, setOptions] = useState({ providers: [], routes: [] });
  const [trips, setTrips] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  async function loadData() {
    if (!tenantId) {
      setOptions({ providers: [], routes: [] });
      setTrips([]);
      setSelectedTripId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, tripsResponse] = await Promise.all([
        getAdminTrainOptions(tenantId),
        listAdminTrainTrips({ includeDeleted: true }, tenantId),
      ]);

      setOptions({
        providers: Array.isArray(optionsResponse?.providers) ? optionsResponse.providers : [],
        routes: Array.isArray(optionsResponse?.routes) ? optionsResponse.routes : [],
      });

      const nextTrips = Array.isArray(tripsResponse?.items) ? tripsResponse.items : [];
      setTrips(nextTrips);

      if (nextTrips.length > 0) {
        const selected = nextTrips.find((item) => item.id === selectedTripId) || nextTrips[0];
        setSelectedTripId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedTripId('');
        setForm(createEmptyForm());
      }
    } catch (requestError) {
      setError(requestError.message || 'Không tải được kho vé tàu ở admin.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [tenantId]);

  function handleSelectTrip(trip) {
    setSelectedTripId(trip.id);
    setForm(hydrateForm(trip));
    setNotice('');
  }

  function handleCreateNew() {
    setSelectedTripId('');
    setForm(createEmptyForm());
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);

      if (selectedTripId) {
        await updateAdminTrainTrip(selectedTripId, payload, tenantId);
        setNotice('Đã cập nhật chuyến tàu ở admin.');
      } else {
        await createAdminTrainTrip(payload, tenantId);
        setNotice('Đã tạo chuyến tàu mới ở admin.');
      }

      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được chuyến tàu.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(trip) {
    if (!tenantId) {
      return;
    }

    setError('');
    setNotice('');

    try {
      if (trip.isDeleted) {
        await restoreAdminTrainTrip(trip.id, tenantId);
        setNotice('Đã khôi phục chuyến tàu.');
      } else {
        await deleteAdminTrainTrip(trip.id, tenantId);
        setNotice('Đã ẩn chuyến tàu.');
      }

      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái chuyến tàu.');
    }
  }

  const activeTrips = trips.filter((item) => !item.isDeleted);
  const publishedTrips = activeTrips.filter((item) => Number(item.status) === 2);
  const todayTrips = activeTrips.filter((item) => String(item.departureAt || '').slice(0, 10) === new Date().toISOString().slice(0, 10));

  return (
    <AdminTrainPageShell
      pageKey="overview"
      title="Kho vé tàu toàn hệ thống"
      subtitle="Admin theo dõi và chỉnh dữ liệu chuyến tàu theo từng tenant mà vẫn giữ nguyên pattern portal hiện có."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Tạo chuyến mới
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {[
          { label: 'Tổng chuyến đang quản lý', value: activeTrips.length, icon: TrainIcon },
          { label: 'Chuyến mở bán', value: publishedTrips.length, icon: Ticket },
          { label: 'Chuyến khởi hành hôm nay', value: todayTrips.length, icon: Calendar },
        ].map((item) => {
          const Icon = item.icon;
          return (
            <div key={item.label} className="bg-white rounded-[2.5rem] border border-slate-100 p-6 shadow-sm">
              <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center mb-5">
                <Icon size={22} />
              </div>
              <p className="text-3xl font-black text-slate-900">{scopeLoading || loading ? '--' : item.value}</p>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-2">{item.label}</p>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1.08fr_0.92fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách chuyến tàu</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Admin có thể rà toàn bộ chuyến của tenant đang chọn và nhảy nhanh sang các màn điều hành chi tiết.</p>
          </div>

          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải chuyến tàu...</div>
            ) : trips.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Tenant này chưa có chuyến tàu nào.</div>
            ) : trips.map((trip) => (
              <div
                key={trip.id}
                role="button"
                tabIndex={0}
                onClick={() => handleSelectTrip(trip)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    handleSelectTrip(trip);
                  }
                }}
                className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedTripId === trip.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{trip.name}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getTripStatusClass(trip.status)}`}>
                        {getTripStatusLabel(trip.status)}
                      </span>
                      {trip.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{trip.trainNumber} • {trip.code}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      Khởi hành: {formatDateTime(trip.departureAt)} • Đến: {formatDateTime(trip.arrivalAt)}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={(event) => {
                      event.stopPropagation();
                      handleDelete(trip);
                    }}
                    className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                  >
                    {trip.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>

                <div className="flex flex-wrap gap-3 mt-4">
                  <Link to={`/admin/train/trip-stop-times?tripId=${trip.id}`} className="inline-flex items-center gap-2 px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600">
                    Lịch dừng
                    <ChevronRight size={14} />
                  </Link>
                  <Link to={`/admin/train/trip-segment-prices?tripId=${trip.id}`} className="inline-flex items-center gap-2 px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600">
                    Giá chặng
                    <ChevronRight size={14} />
                  </Link>
                  <Link to={`/admin/train/cars?tripId=${trip.id}`} className="inline-flex items-center gap-2 px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white">
                    Toa tàu
                    <ChevronRight size={14} />
                  </Link>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedTripId ? 'Cập nhật chuyến tàu' : 'Tạo chuyến tàu mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Admin vẫn dùng cùng ngôn ngữ biểu mẫu như màn manager để không phá nhịp thao tác hiện có.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Đối tác đường sắt</span>
              <select value={form.providerId} onChange={(event) => setForm((current) => ({ ...current, providerId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
                <option value="">Chọn đối tác</option>
                {options.providers.map((item) => (
                  <option key={item.id} value={item.id}>{item.name}</option>
                ))}
              </select>
            </label>

            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tuyến đường</span>
              <select value={form.routeId} onChange={(event) => setForm((current) => ({ ...current, routeId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
                <option value="">Chọn tuyến</option>
                {options.routes.map((item) => (
                  <option key={item.id} value={item.id}>{item.name}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số hiệu tàu</span>
              <input value={form.trainNumber} onChange={(event) => setForm((current) => ({ ...current, trainNumber: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="SE1" required />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã chuyến</span>
              <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên hiển thị</span>
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="Chuyến SE1" required />
          </label>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
              <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
                {TRAIN_TRIP_STATUSES.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
            </label>

            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Khởi hành</span>
              <input type="datetime-local" value={form.departureAt} onChange={(event) => setForm((current) => ({ ...current, departureAt: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>

            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Đến nơi</span>
              <input type="datetime-local" value={form.arrivalAt} onChange={(event) => setForm((current) => ({ ...current, arrivalAt: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chính sách hành lý (JSON)</span>
            <textarea value={form.baggagePolicyJson} onChange={(event) => setForm((current) => ({ ...current, baggagePolicyJson: event.target.value }))} rows={3} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Quy tắc giá (JSON)</span>
            <textarea value={form.fareRulesJson} onChange={(event) => setForm((current) => ({ ...current, fareRulesJson: event.target.value }))} rows={3} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chính sách lên tàu (JSON)</span>
            <textarea value={form.boardingPolicyJson} onChange={(event) => setForm((current) => ({ ...current, boardingPolicyJson: event.target.value }))} rows={3} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </label>

          <label className="flex items-center gap-3 pt-1">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" />
            <span className="text-sm font-bold text-slate-700">Cho phép hoạt động</span>
          </label>

          <button type="submit" disabled={saving || !tenantId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
            {saving ? 'Đang lưu...' : selectedTripId ? 'Lưu cập nhật' : 'Tạo chuyến tàu'}
          </button>
        </form>
      </div>
    </AdminTrainPageShell>
  );
}
