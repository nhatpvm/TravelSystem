import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw, Route } from 'lucide-react';
import AdminTrainPageShell from '../train/components/AdminTrainPageShell';
import useAdminTrainScope from '../train/hooks/useAdminTrainScope';
import {
  createAdminTrainRoute,
  deleteAdminTrainRoute,
  getAdminTrainOptions,
  getAdminTrainRoute,
  listAdminTrainRoutes,
  replaceAdminTrainRouteStops,
  restoreAdminTrainRoute,
  updateAdminTrainRoute,
} from '../../../services/trainService';

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

export default function AdminTrainRoutesPage() {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminTrainScope();
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

  const stopPointLookup = useMemo(() => Object.fromEntries(options.stopPoints.map((item) => [item.id, item])), [options.stopPoints]);

  async function loadRouteDetail(routeId, currentItems = items) {
    if (!tenantId) {
      return;
    }

    const route = currentItems.find((item) => item.id === routeId);
    setSelectedId(routeId);

    if (route) {
      setRouteForm(hydrateRouteForm(route));
    }

    try {
      const detail = await getAdminTrainRoute(routeId, { includeDeleted: true }, tenantId);
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
    } catch (requestError) {
      setError(requestError.message || 'Không tải được chi tiết tuyến đường.');
    }
  }

  async function loadData() {
    if (!tenantId) {
      setOptions({ providers: [], stopPoints: [] });
      setItems([]);
      setSelectedId('');
      setRouteForm(createEmptyRouteForm());
      setRouteStops([createEmptyStopRow(0), createEmptyStopRow(1)]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getAdminTrainOptions(tenantId),
        listAdminTrainRoutes({ includeDeleted: true }, tenantId),
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
    } catch (requestError) {
      setError(requestError.message || 'Không tải được tuyến đường.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [tenantId]);

  function handleCreateNew() {
    setSelectedId('');
    setRouteForm(createEmptyRouteForm());
    setRouteStops([createEmptyStopRow(0), createEmptyStopRow(1)]);
    setNotice('');
  }

  async function handleSaveRoute(event) {
    event.preventDefault();
    if (!tenantId) {
      return;
    }

    setSavingRoute(true);
    setError('');
    setNotice('');

    try {
      const payload = buildRoutePayload(routeForm);

      if (selectedId) {
        await updateAdminTrainRoute(selectedId, payload, tenantId);
        setNotice('Đã cập nhật tuyến đường.');
      } else {
        await createAdminTrainRoute(payload, tenantId);
        setNotice('Đã tạo tuyến đường mới.');
      }

      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được tuyến đường.');
    } finally {
      setSavingRoute(false);
    }
  }

  async function handleSaveStops() {
    if (!selectedId || !tenantId) {
      setError('Hãy lưu tuyến đường trước khi cấu hình các ga dừng.');
      return;
    }

    setSavingStops(true);
    setError('');
    setNotice('');

    try {
      await replaceAdminTrainRouteStops(selectedId, {
        stops: routeStops.map((item, index) => ({
          stopPointId: item.stopPointId,
          stopIndex: index,
          distanceFromStartKm: item.distanceFromStartKm === '' ? null : Number(item.distanceFromStartKm),
          minutesFromStart: item.minutesFromStart === '' ? null : Number(item.minutesFromStart),
          isActive: !!item.isActive,
        })),
      }, tenantId);

      setNotice('Đã lưu danh sách ga dừng của tuyến.');
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được ga dừng tuyến.');
    } finally {
      setSavingStops(false);
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) {
      return;
    }

    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreAdminTrainRoute(item.id, tenantId);
        setNotice('Đã khôi phục tuyến đường.');
      } else {
        await deleteAdminTrainRoute(item.id, tenantId);
        setNotice('Đã ẩn tuyến đường.');
      }

      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái tuyến đường.');
    }
  }

  return (
    <AdminTrainPageShell
      pageKey="routes"
      title="Tuyến đường toàn hệ thống"
      subtitle="Admin theo dõi tuyến theo tenant để đảm bảo logic ga dừng, lịch dừng và giá chặng không bị lệch."
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
              <p className="text-xs font-bold text-slate-400 mt-1">Tuyến nào đủ ga dừng mới nên đưa vào chuyến tàu.</p>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải tuyến đường...</div>
              ) : items.length === 0 ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Tenant này chưa có tuyến đường nào.</div>
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
                          {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                        </div>
                        <p className="text-xs font-bold text-slate-400 mt-2">
                          {stopPointLookup[item.fromStopPointId]?.name || 'Ga đầu'} → {stopPointLookup[item.toStopPointId]?.name || 'Ga cuối'}
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

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xl font-black text-slate-900">Ga dừng của tuyến</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Danh sách phải liên tục từ 0..n-1 để backend chấp nhận.</p>
              </div>
              <button type="button" onClick={() => setRouteStops((current) => [...current, createEmptyStopRow(current.length)])} className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600">
                Thêm ga dừng
              </button>
            </div>

            <div className="space-y-4">
              {routeStops.map((item, index) => (
                <div key={`stop-${index}`} className="grid grid-cols-1 md:grid-cols-[1.5fr_1fr_1fr_auto] gap-4 items-end">
                  <label className="space-y-2 block">
                    <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga #{index}</span>
                    <select value={item.stopPointId} onChange={(event) => setRouteStops((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, stopPointId: event.target.value } : row))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
                      <option value="">Chọn ga</option>
                      {options.stopPoints.map((stopPoint) => (
                        <option key={stopPoint.id} value={stopPoint.id}>{stopPoint.name}</option>
                      ))}
                    </select>
                  </label>

                  <label className="space-y-2 block">
                    <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Km từ đầu tuyến</span>
                    <input type="number" value={item.distanceFromStartKm} onChange={(event) => setRouteStops((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, distanceFromStartKm: event.target.value } : row))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
                  </label>

                  <label className="space-y-2 block">
                    <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phút từ đầu tuyến</span>
                    <input type="number" value={item.minutesFromStart} onChange={(event) => setRouteStops((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, minutesFromStart: event.target.value } : row))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
                  </label>

                  <button
                    type="button"
                    onClick={() => setRouteStops((current) => current.length <= 2 ? current : current.filter((_, rowIndex) => rowIndex !== index).map((row, rowIndex) => ({ ...row, stopIndex: rowIndex })))}
                    className="px-4 py-3 rounded-2xl bg-rose-50 text-rose-700 text-xs font-black uppercase tracking-widest"
                  >
                    Xóa
                  </button>
                </div>
              ))}
            </div>

            <button type="button" onClick={handleSaveStops} disabled={savingStops || !selectedId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
              {savingStops ? 'Đang lưu...' : 'Lưu ga dừng tuyến'}
            </button>
          </div>
        </div>

        <form onSubmit={handleSaveRoute} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật tuyến đường' : 'Tạo tuyến đường mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Tuyến đường là nền tảng để sinh lịch dừng và giá chặng.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Đối tác</span>
            <select value={routeForm.providerId} onChange={(event) => setRouteForm((current) => ({ ...current, providerId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
              <option value="">Chọn đối tác</option>
              {options.providers.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã tuyến</span>
              <input value={routeForm.code} onChange={(event) => setRouteForm((current) => ({ ...current, code: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên tuyến</span>
              <input value={routeForm.name} onChange={(event) => setRouteForm((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga đầu</span>
              <select value={routeForm.fromStopPointId} onChange={(event) => setRouteForm((current) => ({ ...current, fromStopPointId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
                <option value="">Chọn ga đầu</option>
                {options.stopPoints.map((item) => (
                  <option key={item.id} value={item.id}>{item.name}</option>
                ))}
              </select>
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ga cuối</span>
              <select value={routeForm.toStopPointId} onChange={(event) => setRouteForm((current) => ({ ...current, toStopPointId: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required>
                <option value="">Chọn ga cuối</option>
                {options.stopPoints.map((item) => (
                  <option key={item.id} value={item.id}>{item.name}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thời gian dự kiến (phút)</span>
              <input type="number" value={routeForm.estimatedMinutes} onChange={(event) => setRouteForm((current) => ({ ...current, estimatedMinutes: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Khoảng cách (km)</span>
              <input type="number" value={routeForm.distanceKm} onChange={(event) => setRouteForm((current) => ({ ...current, distanceKm: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" />
            </label>
          </div>

          <label className="flex items-center gap-3 pt-1">
            <input type="checkbox" checked={routeForm.isActive} onChange={(event) => setRouteForm((current) => ({ ...current, isActive: event.target.checked }))} className="w-4 h-4 rounded border-slate-300" />
            <span className="text-sm font-bold text-slate-700">Cho phép hoạt động</span>
          </label>

          <button type="submit" disabled={savingRoute || !tenantId} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60">
            {savingRoute ? 'Đang lưu...' : selectedId ? 'Lưu cập nhật' : 'Tạo tuyến đường'}
          </button>
        </form>
      </div>
    </AdminTrainPageShell>
  );
}
