import React, { useEffect, useState } from 'react';
import { Calendar, ChevronRight, Plus, RefreshCw, Ticket, Train as TrainIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import TrainManagementPageShell from '../../train/components/TrainManagementPageShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  TRAIN_TRIP_STATUSES,
  formatDateTime,
  getTripStatusClass,
  getTripStatusLabel,
  toApiDateTimeValue,
  toDateTimeInputValue,
} from '../../train/utils/presentation';
import {
  createTrainTrip,
  deleteTrainTrip,
  getTrainManagerOptions,
  listTrainTrips,
  restoreTrainTrip,
  updateTrainTrip,
} from '../../../../services/trainService';

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

const TrainInventoryPage = () => {
  const [options, setOptions] = useState({ providers: [], routes: [] });
  const [trips, setTrips] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, tripsResponse] = await Promise.all([
        getTrainManagerOptions(),
        listTrainTrips(),
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
    } catch (err) {
      setError(err.message || 'Không tải được dữ liệu kho vé tàu.');
    } finally {
      setLoading(false);
    }
  };

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [loadDataRef]);

  const handleSelectTrip = (trip) => {
    setSelectedTripId(trip.id);
    setForm(hydrateForm(trip));
    setNotice('');
  };

  const handleCreateNew = () => {
    setSelectedTripId('');
    setForm(createEmptyForm());
    setNotice('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);

      if (selectedTripId) {
        await updateTrainTrip(selectedTripId, payload);
        setNotice('Đã cập nhật chuyến tàu.');
      } else {
        await createTrainTrip(payload);
        setNotice('Đã tạo chuyến tàu mới.');
      }

      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không lưu được chuyến tàu.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (trip) => {
    setError('');
    setNotice('');

    try {
      if (trip.isDeleted) {
        await restoreTrainTrip(trip.id);
        setNotice('Đã khôi phục chuyến tàu.');
      } else {
        await deleteTrainTrip(trip.id);
        setNotice('Đã ẩn chuyến tàu.');
      }

      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái chuyến tàu.');
    }
  };

  const activeTrips = trips.filter((item) => !item.isDeleted);
  const publishedTrips = activeTrips.filter((item) => Number(item.status) === 2);
  const todayTrips = activeTrips.filter((item) => String(item.departureAt || '').slice(0, 10) === new Date().toISOString().slice(0, 10));

  return (
    <TrainManagementPageShell
      pageKey="overview"
      title="Kho Vé tàu"
      subtitle="Quản lý các chuyến tàu đang mở bán trên nền tảng."
      error={error}
      notice={notice}
      actions={(
        <>
          <button
            type="button"
            onClick={loadData}
            className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button
            type="button"
            onClick={handleCreateNew}
            className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2"
          >
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
              <p className="text-3xl font-black text-slate-900">{item.value}</p>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-2">{item.label}</p>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1.08fr_0.92fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách chuyến tàu</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Các chuyến mở bán sẽ xuất hiện trên public search cho khách hàng.</p>
          </div>

          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải chuyến tàu...</div>
            ) : trips.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có chuyến tàu nào trong tenant.</div>
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
                      {trip.isDeleted ? (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                          Đã ẩn
                        </span>
                      ) : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {trip.trainNumber} • {trip.code} • {formatDateTime(trip.departureAt)} • {formatDateTime(trip.arrivalAt)}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Link
                      to={`/tenant/operations/train/trip-stop-times?tripId=${trip.id}`}
                      className="px-3 py-2 rounded-xl bg-slate-100 text-[10px] font-black uppercase tracking-widest text-slate-600"
                      onClick={(event) => event.stopPropagation()}
                    >
                      Lịch dừng
                    </Link>
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
                    <ChevronRight size={18} className="text-slate-300" />
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="space-y-6">
          <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-xl font-black text-slate-900">{selectedTripId ? 'Cập nhật chuyến tàu' : 'Tạo chuyến tàu mới'}</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Giữ đúng mã tàu, tuyến và thời gian để public search hiển thị chính xác.</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Nhà vận hành</span>
                <select
                  value={form.providerId}
                  onChange={(event) => setForm((current) => ({ ...current, providerId: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                >
                  <option value="">Chọn nhà vận hành</option>
                  {options.providers.map((item) => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tuyến đường</span>
                <select
                  value={form.routeId}
                  onChange={(event) => setForm((current) => ({ ...current, routeId: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                >
                  <option value="">Chọn tuyến đường</option>
                  {options.routes.map((item) => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số hiệu tàu</span>
                <input
                  value={form.trainNumber}
                  onChange={(event) => setForm((current) => ({ ...current, trainNumber: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  placeholder="SE1"
                  required
                />
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã chuyến</span>
                <input
                  value={form.code}
                  onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  placeholder="VT001-TRIP-NEW"
                  required
                />
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select
                  value={form.status}
                  onChange={(event) => setForm((current) => ({ ...current, status: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                >
                  {TRAIN_TRIP_STATUSES.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên hiển thị</span>
              <input
                value={form.name}
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                placeholder="Chuyến SE1 Hà Nội - Đà Nẵng"
                required
              />
            </label>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ khởi hành</span>
                <input
                  type="datetime-local"
                  value={form.departureAt}
                  onChange={(event) => setForm((current) => ({ ...current, departureAt: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                />
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ đến nơi</span>
                <input
                  type="datetime-local"
                  value={form.arrivalAt}
                  onChange={(event) => setForm((current) => ({ ...current, arrivalAt: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điều kiện vé</span>
              <textarea
                rows={3}
                value={form.fareRulesJson}
                onChange={(event) => setForm((current) => ({ ...current, fareRulesJson: event.target.value }))}
                className="w-full rounded-[1.75rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-medium text-slate-700 outline-none"
                placeholder='{"refund":"Theo điều kiện từng hạng vé"}'
              />
            </label>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chính sách hành lý</span>
                <textarea
                  rows={3}
                  value={form.baggagePolicyJson}
                  onChange={(event) => setForm((current) => ({ ...current, baggagePolicyJson: event.target.value }))}
                  className="w-full rounded-[1.75rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-medium text-slate-700 outline-none"
                  placeholder='{"carryOn":"Theo quy định nhà ga"}'
                />
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chính sách lên tàu</span>
                <textarea
                  rows={3}
                  value={form.boardingPolicyJson}
                  onChange={(event) => setForm((current) => ({ ...current, boardingPolicyJson: event.target.value }))}
                  className="w-full rounded-[1.75rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-medium text-slate-700 outline-none"
                  placeholder='{"checkIn":"Có mặt trước 30 phút"}'
                />
              </label>
            </div>

            <label className="inline-flex items-center gap-3 text-sm font-bold text-slate-600">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
                className="w-4 h-4 rounded border-slate-300 text-blue-600 focus:ring-blue-200"
              />
              Kích hoạt chuyến tàu này
            </label>

            <button
              type="submit"
              disabled={saving}
              className={`w-full rounded-2xl px-5 py-4 text-sm font-black uppercase tracking-widest transition-all ${
                saving ? 'bg-slate-200 text-slate-500' : 'bg-slate-900 text-white hover:bg-[#1EB4D4]'
              }`}
            >
              {saving ? 'Đang lưu...' : selectedTripId ? 'Lưu thay đổi' : 'Tạo chuyến tàu'}
            </button>
          </form>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-4">
            <p className="text-lg font-black text-slate-900">Đi tắt cho vận hành</p>
            <p className="text-sm font-medium text-slate-500">
              Sau khi tạo chuyến, tiếp tục cấu hình lịch dừng, giá chặng, toa tàu và sơ đồ chỗ.
            </p>

            {selectedTripId ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <Link to={`/tenant/operations/train/trip-stop-times?tripId=${selectedTripId}`} className="px-4 py-4 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-700">
                  Lịch dừng
                </Link>
                <Link to={`/tenant/operations/train/trip-segment-prices?tripId=${selectedTripId}`} className="px-4 py-4 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-700">
                  Giá chặng
                </Link>
                <Link to={`/tenant/providers/train/cars?tripId=${selectedTripId}`} className="px-4 py-4 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-700">
                  Toa tàu
                </Link>
                <Link to={`/tenant/providers/train/seats?tripId=${selectedTripId}`} className="px-4 py-4 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white">
                  Sơ đồ chỗ
                </Link>
              </div>
            ) : (
              <div className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-4 text-sm font-bold text-slate-500">
                Hãy tạo hoặc chọn một chuyến tàu để đi tiếp sang các bước vận hành.
              </div>
            )}
          </div>
        </div>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainInventoryPage;
