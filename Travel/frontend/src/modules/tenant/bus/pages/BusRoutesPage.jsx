import React, { useEffect, useState } from 'react';
import { Plus, RefreshCw, Route } from 'lucide-react';
import BusManagementPageShell from '../components/BusManagementPageShell';
import { createBusRoute, deleteBusRoute, getBusManagerOptions, getBusRoute, listBusRoutes, replaceBusRouteStops, restoreBusRoute, updateBusRoute } from '../../../../services/busService';

function createEmptyRouteForm() {
  return {
    providerId: '',
    code: '',
    name: '',
    fromStopPointId: '',
    toStopPointId: '',
    estimatedMinutes: 0,
    distanceKm: 0,
    isActive: true,
  };
}

function createEmptyStopRow(index = 0) {
  return {
    stopPointId: '',
    stopIndex: index,
    distanceFromStartKm: '',
    minutesFromStart: '',
    isActive: true,
  };
}

function hydrateRouteForm(item) {
  return {
    providerId: item.providerId || '',
    code: item.code || '',
    name: item.name || '',
    fromStopPointId: item.fromStopPointId || '',
    toStopPointId: item.toStopPointId || '',
    estimatedMinutes: item.estimatedMinutes ?? 0,
    distanceKm: item.distanceKm ?? 0,
    isActive: item.isActive ?? true,
  };
}

function buildRoutePayload(form) {
  return {
    providerId: form.providerId,
    code: form.code.trim(),
    name: form.name.trim(),
    fromStopPointId: form.fromStopPointId,
    toStopPointId: form.toStopPointId,
    estimatedMinutes: Number(form.estimatedMinutes || 0),
    distanceKm: Number(form.distanceKm || 0),
    isActive: !!form.isActive,
  };
}

const BusRoutesPage = () => {
  const [options, setOptions] = useState({ providers: [], stopPoints: [] });
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [routeForm, setRouteForm] = useState(createEmptyRouteForm);
  const [routeStops, setRouteStops] = useState([createEmptyStopRow(0), createEmptyStopRow(1)]);
  const [loading, setLoading] = useState(true);
  const [savingRoute, setSavingRoute] = useState(false);
  const [savingStops, setSavingStops] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const stopPointLookup = Object.fromEntries(options.stopPoints.map((item) => [item.id, item]));

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getBusManagerOptions(),
        listBusRoutes({ includeDeleted: true }),
      ]);

      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];

      setOptions({
        providers: Array.isArray(optionsResponse?.providers) ? optionsResponse.providers : [],
        stopPoints: Array.isArray(optionsResponse?.stopPoints) ? optionsResponse.stopPoints : [],
      });
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        await loadRouteDetail(selected.id, nextItems);
      } else {
        setSelectedId('');
        setRouteForm(createEmptyRouteForm());
        setRouteStops([createEmptyStopRow(0), createEmptyStopRow(1)]);
      }
    } catch (err) {
      setError(err.message || 'Không tải được tuyến đường.');
    } finally {
      setLoading(false);
    }
  };

  const loadRouteDetail = async (routeId, currentItems = items) => {
    const route = currentItems.find((item) => item.id === routeId);
    setSelectedId(routeId);
    if (route) {
      setRouteForm(hydrateRouteForm(route));
    }

    try {
      const detail = await getBusRoute(routeId, { includeDeleted: true });
      const stops = Array.isArray(detail?.stops) ? detail.stops : [];
      setRouteStops(
        stops.length > 0
          ? stops.map((item, index) => ({
            stopPointId: item.stopPointId || '',
            stopIndex: item.stopIndex ?? index,
            distanceFromStartKm: item.distanceFromStartKm ?? '',
            minutesFromStart: item.minutesFromStart ?? '',
            isActive: item.isActive ?? true,
          }))
          : [createEmptyStopRow(0), createEmptyStopRow(1)],
      );
    } catch (err) {
      setError(err.message || 'Không tải được chi tiết tuyến đường.');
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreateNew = () => {
    setSelectedId('');
    setRouteForm(createEmptyRouteForm());
    setRouteStops([createEmptyStopRow(0), createEmptyStopRow(1)]);
    setNotice('');
  };

  const handleSaveRoute = async (event) => {
    event.preventDefault();
    setSavingRoute(true);
    setError('');
    setNotice('');

    try {
      const payload = buildRoutePayload(routeForm);

      if (selectedId) {
        await updateBusRoute(selectedId, payload);
        setNotice('Đã cập nhật tuyến đường.');
      } else {
        await createBusRoute(payload);
        setNotice('Đã tạo tuyến đường mới.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không lưu được tuyến đường.');
    } finally {
      setSavingRoute(false);
    }
  };

  const handleSaveStops = async () => {
    if (!selectedId) {
      setError('Hãy lưu tuyến đường trước khi cấu hình các điểm dừng.');
      return;
    }

    setSavingStops(true);
    setError('');
    setNotice('');

    try {
      await replaceBusRouteStops(selectedId, {
        stops: routeStops.map((item, index) => ({
          stopPointId: item.stopPointId,
          stopIndex: index,
          distanceFromStartKm: item.distanceFromStartKm === '' ? null : Number(item.distanceFromStartKm),
          minutesFromStart: item.minutesFromStart === '' ? null : Number(item.minutesFromStart),
          isActive: !!item.isActive,
        })),
      });

      setNotice('Đã lưu danh sách điểm dừng của tuyến.');
      await loadData();
    } catch (err) {
      setError(err.message || 'Không lưu được điểm dừng tuyến.');
    } finally {
      setSavingStops(false);
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusRoute(item.id);
        setNotice('Đã khôi phục tuyến đường.');
      } else {
        await deleteBusRoute(item.id);
        setNotice('Đã ẩn tuyến đường.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái tuyến đường.');
    }
  };

  return (
    <BusManagementPageShell
      pageKey="routes"
      title="Tuyến đường"
      subtitle="Tuyến đường là nền tảng để sinh lịch dừng, chặng giá và chuyến xe marketplace."
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
            Thêm tuyến mới
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_1fr] gap-8">
        <div className="space-y-8">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-8 py-6 border-b border-slate-100">
              <p className="text-lg font-black text-slate-900">Danh sách tuyến đường</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Tuyến nào đủ stop points mới nên đưa vào chuyến xe.</p>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải tuyến đường...</div>
              ) : items.length === 0 ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có tuyến đường nào.</div>
              ) : items.map((item) => (
                <div
                  key={item.id}
                  role="button"
                  tabIndex={0}
                  onClick={() => loadRouteDetail(item.id)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      loadRouteDetail(item.id);
                    }
                  }}
                  className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                        <Route size={20} />
                      </div>
                      <div>
                        <div className="flex items-center gap-3 flex-wrap">
                          <p className="font-black text-slate-900">{item.name}</p>
                          <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${item.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>
                            {item.isActive ? 'Đang dùng' : 'Tạm ngưng'}
                          </span>
                          {item.isDeleted ? (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                              Đã ẩn
                            </span>
                          ) : null}
                        </div>
                        <p className="text-xs font-bold text-slate-400 mt-2">
                          {stopPointLookup[item.fromStopPointId]?.name || 'Điểm đầu'} → {stopPointLookup[item.toStopPointId]?.name || 'Điểm cuối'}
                        </p>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={(event) => {
                        event.stopPropagation();
                        handleToggleDelete(item);
                      }}
                      className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                    >
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật tuyến đường' : 'Tạo tuyến đường mới'}</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Route code phải ổn định để đội vận hành dễ tra cứu.</p>
            </div>

            <form onSubmit={handleSaveRoute} className="space-y-5">
              <label className="space-y-2 block">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Nhà xe</span>
                <select
                  value={routeForm.providerId}
                  onChange={(event) => setRouteForm((current) => ({ ...current, providerId: event.target.value }))}
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  required
                >
                  <option value="">Chọn nhà xe</option>
                  {options.providers.map((item) => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </label>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <label className="space-y-2">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã tuyến</span>
                  <input
                    value={routeForm.code}
                    onChange={(event) => setRouteForm((current) => ({ ...current, code: event.target.value }))}
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                    required
                  />
                </label>
                <label className="space-y-2">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên tuyến</span>
                  <input
                    value={routeForm.name}
                    onChange={(event) => setRouteForm((current) => ({ ...current, name: event.target.value }))}
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                    required
                  />
                </label>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <label className="space-y-2">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điểm đầu</span>
                  <select
                    value={routeForm.fromStopPointId}
                    onChange={(event) => setRouteForm((current) => ({ ...current, fromStopPointId: event.target.value }))}
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                    required
                  >
                    <option value="">Chọn điểm đầu</option>
                    {options.stopPoints.map((item) => (
                      <option key={item.id} value={item.id}>{item.name}</option>
                    ))}
                  </select>
                </label>
                <label className="space-y-2">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điểm cuối</span>
                  <select
                    value={routeForm.toStopPointId}
                    onChange={(event) => setRouteForm((current) => ({ ...current, toStopPointId: event.target.value }))}
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                    required
                  >
                    <option value="">Chọn điểm cuối</option>
                    {options.stopPoints.map((item) => (
                      <option key={item.id} value={item.id}>{item.name}</option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <label className="space-y-2">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tổng phút</span>
                  <input
                    type="number"
                    value={routeForm.estimatedMinutes}
                    onChange={(event) => setRouteForm((current) => ({ ...current, estimatedMinutes: event.target.value }))}
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  />
                </label>
                <label className="space-y-2">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Quãng đường (km)</span>
                  <input
                    type="number"
                    value={routeForm.distanceKm}
                    onChange={(event) => setRouteForm((current) => ({ ...current, distanceKm: event.target.value }))}
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  />
                </label>
              </div>

              <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
                <input
                  type="checkbox"
                  checked={routeForm.isActive}
                  onChange={(event) => setRouteForm((current) => ({ ...current, isActive: event.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300"
                />
                Kích hoạt tuyến đường này
              </label>

              <button
                type="submit"
                disabled={savingRoute}
                className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
              >
                {savingRoute ? 'Đang lưu...' : selectedId ? 'Lưu tuyến đường' : 'Tạo tuyến đường'}
              </button>
            </form>
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-xl font-black text-slate-900">Danh sách điểm dừng của tuyến</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Dùng để sinh lịch dừng và tính giá chặng i → j.</p>
            </div>
            <button
              type="button"
              onClick={() => setRouteStops((current) => [...current, createEmptyStopRow(current.length)])}
              className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600"
            >
              Thêm dòng
            </button>
          </div>

          <div className="space-y-4">
            {routeStops.map((item, index) => (
              <div key={`route-stop-${index}`} className="rounded-[2rem] border border-slate-100 bg-slate-50 p-5 space-y-4">
                <div className="flex items-center justify-between gap-4">
                  <p className="text-sm font-black text-slate-900">Stop #{index}</p>
                  <button
                    type="button"
                    onClick={() => setRouteStops((current) => current.filter((_, currentIndex) => currentIndex !== index).map((row, rowIndex) => ({ ...row, stopIndex: rowIndex })))}
                    className="text-[10px] font-black uppercase tracking-widest text-rose-600"
                  >
                    Xóa dòng
                  </button>
                </div>

                <label className="space-y-2 block">
                  <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Stop point</span>
                  <select
                    value={item.stopPointId}
                    onChange={(event) => setRouteStops((current) => current.map((row, rowIndex) => (rowIndex === index ? { ...row, stopPointId: event.target.value } : row)))}
                    className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                  >
                    <option value="">Chọn stop point</option>
                    {options.stopPoints.map((stopPoint) => (
                      <option key={stopPoint.id} value={stopPoint.id}>{stopPoint.name}</option>
                    ))}
                  </select>
                </label>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <label className="space-y-2">
                    <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Km từ đầu tuyến</span>
                    <input
                      type="number"
                      value={item.distanceFromStartKm}
                      onChange={(event) => setRouteStops((current) => current.map((row, rowIndex) => (rowIndex === index ? { ...row, distanceFromStartKm: event.target.value } : row)))}
                      className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                    />
                  </label>
                  <label className="space-y-2">
                    <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phút từ đầu tuyến</span>
                    <input
                      type="number"
                      value={item.minutesFromStart}
                      onChange={(event) => setRouteStops((current) => current.map((row, rowIndex) => (rowIndex === index ? { ...row, minutesFromStart: event.target.value } : row)))}
                      className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                    />
                  </label>
                </div>
              </div>
            ))}
          </div>

          <button
            type="button"
            onClick={handleSaveStops}
            disabled={savingStops || !selectedId}
            className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
          >
            {savingStops ? 'Đang lưu điểm dừng...' : 'Lưu danh sách điểm dừng'}
          </button>
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusRoutesPage;
