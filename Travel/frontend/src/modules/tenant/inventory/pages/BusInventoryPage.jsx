import React, { useEffect, useState } from 'react';
import { Bus as BusIcon, Calendar, ChevronRight, Plus, RefreshCw, Route, ShieldCheck, Ticket } from 'lucide-react';
import { Link } from 'react-router-dom';
import BusManagementPageShell from '../../bus/components/BusManagementPageShell';
import { BUS_TRIP_STATUSES, formatDateTime, getTripStatusClass, getTripStatusLabel, toApiDateTimeValue, toDateTimeInputValue } from '../../bus/utils/presentation';
import { createBusTrip, deleteBusTrip, getBusManagerOptions, listBusTrips, restoreBusTrip, updateBusTrip } from '../../../../services/busService';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

function createEmptyForm() {
  return {
    providerId: '',
    routeId: '',
    vehicleId: '',
    code: '',
    name: '',
    status: 2,
    departureAt: '',
    arrivalAt: '',
    fareRulesJson: '',
    baggagePolicyJson: '',
    boardingPolicyJson: '',
    notes: '',
    isActive: true,
  };
}

function buildPayload(form) {
  return {
    providerId: form.providerId,
    routeId: form.routeId,
    vehicleId: form.vehicleId,
    code: form.code.trim(),
    name: form.name.trim(),
    status: Number(form.status),
    departureAt: toApiDateTimeValue(form.departureAt),
    arrivalAt: toApiDateTimeValue(form.arrivalAt),
    fareRulesJson: form.fareRulesJson || null,
    baggagePolicyJson: form.baggagePolicyJson || null,
    boardingPolicyJson: form.boardingPolicyJson || null,
    notes: form.notes || null,
    isActive: !!form.isActive,
  };
}

function hydrateForm(item) {
  return {
    providerId: item.providerId || '',
    routeId: item.routeId || '',
    vehicleId: item.vehicleId || '',
    code: item.code || '',
    name: item.name || '',
    status: item.status || 2,
    departureAt: toDateTimeInputValue(item.departureAt),
    arrivalAt: toDateTimeInputValue(item.arrivalAt),
    fareRulesJson: item.fareRulesJson || '',
    baggagePolicyJson: item.baggagePolicyJson || '',
    boardingPolicyJson: item.boardingPolicyJson || '',
    notes: item.notes || '',
    isActive: item.isActive ?? true,
  };
}

const BusInventoryPage = () => {
  const [options, setOptions] = useState({ providers: [], routes: [], vehicles: [] });
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
        getBusManagerOptions(),
        listBusTrips({ includeDeleted: true }),
      ]);

      setOptions({
        providers: Array.isArray(optionsResponse?.providers) ? optionsResponse.providers : [],
        routes: Array.isArray(optionsResponse?.routes) ? optionsResponse.routes : [],
        vehicles: Array.isArray(optionsResponse?.vehicles) ? optionsResponse.vehicles : [],
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
      setError(err.message || 'Không tải được dữ liệu kho xe khách.');
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
    const departureAt = toApiDateTimeValue(form.departureAt);
    const arrivalAt = toApiDateTimeValue(form.arrivalAt);

    if (!departureAt || !arrivalAt || new Date(arrivalAt) < new Date(departureAt)) {
      setError('Giờ đến nơi phải sau hoặc bằng giờ xuất bến.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);

      if (selectedTripId) {
        await updateBusTrip(selectedTripId, payload);
        setNotice('Đã cập nhật chuyến xe.');
      } else {
        await createBusTrip(payload);
        setNotice('Đã tạo chuyến xe mới.');
      }

      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không lưu được chuyến xe.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (trip) => {
    setError('');
    setNotice('');

    try {
      if (trip.isDeleted) {
        await restoreBusTrip(trip.id);
        setNotice('Đã khôi phục chuyến xe.');
      } else {
        await deleteBusTrip(trip.id);
        setNotice('Đã ẩn chuyến xe.');
      }

      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái chuyến xe.');
    }
  };

  const activeTrips = trips.filter((item) => !item.isDeleted);
  const publishedTrips = activeTrips.filter((item) => Number(item.status) === 2);
  const todayTrips = activeTrips.filter((item) => String(item.departureAt || '').slice(0, 10) === new Date().toISOString().slice(0, 10));

  return (
    <BusManagementPageShell
      pageKey="overview"
      title="Kho Xe khách"
      subtitle="Quản lý chuyến xe đang mở bán trên nền tảng."
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
          { label: 'Tổng chuyến đang quản lý', value: activeTrips.length, icon: BusIcon },
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

      <div className="grid grid-cols-1 xl:grid-cols-[1.1fr_0.9fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách chuyến xe</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Chuyến nào mở bán sẽ hiển thị cho khách hàng.</p>
            </div>
          </div>

          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải chuyến xe...</div>
            ) : trips.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đơn vị này chưa có chuyến xe nào.</div>
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
                      {trip.code} • {formatDateTime(trip.departureAt)} • {formatDateTime(trip.arrivalAt)}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Link
                      to={`/tenant/operations/bus/trip-stop-times?tripId=${trip.id}`}
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
              <p className="text-xl font-black text-slate-900">{selectedTripId ? 'Cập nhật chuyến xe' : 'Tạo chuyến xe mới'}</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Giữ đúng mã chuyến, tuyến, xe và thời gian để khách tìm chuyến chính xác.</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Nhà xe</span>
                <select
                  value={form.providerId}
                  onChange={(event) => setForm((current) => ({ ...current, providerId: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                >
                  <option value="">Chọn nhà xe</option>
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

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Xe khai thác</span>
                <select
                  value={form.vehicleId}
                  onChange={(event) => setForm((current) => ({ ...current, vehicleId: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                >
                  <option value="">Chọn xe</option>
                  {options.vehicles.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name} {item.plateNumber ? `• ${item.plateNumber}` : ''}
                    </option>
                  ))}
                </select>
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select
                  value={form.status}
                  onChange={(event) => setForm((current) => ({ ...current, status: Number(event.target.value) }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                >
                  {BUS_TRIP_STATUSES.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã chuyến</span>
                <input
                  value={form.code}
                  onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  placeholder="BUS-HN-1800"
                  required
                />
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên chuyến</span>
                <input
                  value={form.name}
                  onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  placeholder="Hà Nội - Hải Phòng 18:00"
                  required
                />
              </label>

              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giờ xuất bến</span>
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
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ghi chú vận hành</span>
              <textarea
                rows="4"
                value={form.notes}
                onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
                className="w-full rounded-[2rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-bold text-slate-700 outline-none"
                placeholder="Mô tả nhanh cho điều hành, quy định hành lý, điểm lên xe..."
              />
            </label>

            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
                className="h-4 w-4 rounded border-slate-300"
              />
              Kích hoạt chuyến xe này
            </label>

            <button
              type="submit"
              disabled={saving}
              className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
            >
              {saving ? 'Đang lưu...' : selectedTripId ? 'Lưu thay đổi chuyến xe' : 'Tạo chuyến xe'}
            </button>
          </form>

          <div className="bg-slate-900 rounded-[2.5rem] p-8 text-white space-y-4">
            <div className="flex items-center gap-3">
              <ShieldCheck size={20} className="text-[#1EB4D4]" />
              <p className="text-lg font-black">Đi nhanh sang chi tiết vận hành</p>
            </div>
            <div className="grid grid-cols-1 gap-3">
              <Link to="/tenant/operations/bus/routes" className="rounded-2xl bg-white/10 px-4 py-3 text-sm font-black">Tuyến đường & điểm dừng</Link>
              <Link to={selectedTripId ? `/tenant/operations/bus/trip-stop-times?tripId=${selectedTripId}` : '/tenant/operations/bus/trip-stop-times'} className="rounded-2xl bg-white/10 px-4 py-3 text-sm font-black">Lịch dừng theo chuyến</Link>
              <Link to={selectedTripId ? `/tenant/operations/bus/trip-segment-prices?tripId=${selectedTripId}` : '/tenant/operations/bus/trip-segment-prices'} className="rounded-2xl bg-white/10 px-4 py-3 text-sm font-black">Giá chặng & ma trận giá</Link>
              <Link to={selectedTripId ? `/tenant/providers/bus/seats?tripId=${selectedTripId}` : '/tenant/providers/bus/seats'} className="rounded-2xl bg-white/10 px-4 py-3 text-sm font-black">Sơ đồ ghế theo chặng</Link>
            </div>
          </div>
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusInventoryPage;
