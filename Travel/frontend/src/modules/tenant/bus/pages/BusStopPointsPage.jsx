import React, { useEffect, useState } from 'react';
import { MapPin, Plus, RefreshCw } from 'lucide-react';
import BusManagementPageShell from '../components/BusManagementPageShell';
import { BUS_STOP_POINT_TYPES, getStopPointTypeLabel } from '../utils/presentation';
import { createBusStopPoint, deleteBusStopPoint, getBusManagerOptions, listBusStopPoints, restoreBusStopPoint, updateBusStopPoint } from '../../../../services/busService';

function createEmptyForm() {
  return {
    locationId: '',
    type: 1,
    name: '',
    addressLine: '',
    latitude: '',
    longitude: '',
    notes: '',
    sortOrder: 0,
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    locationId: item.locationId || '',
    type: item.type || 1,
    name: item.name || '',
    addressLine: item.addressLine || '',
    latitude: item.latitude ?? '',
    longitude: item.longitude ?? '',
    notes: item.notes || '',
    sortOrder: item.sortOrder ?? 0,
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    locationId: form.locationId,
    type: Number(form.type),
    name: form.name.trim(),
    addressLine: form.addressLine || null,
    latitude: form.latitude === '' ? null : Number(form.latitude),
    longitude: form.longitude === '' ? null : Number(form.longitude),
    notes: form.notes || null,
    sortOrder: Number(form.sortOrder || 0),
    isActive: !!form.isActive,
  };
}

const BusStopPointsPage = () => {
  const [locations, setLocations] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getBusManagerOptions(),
        listBusStopPoints(),
      ]);

      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      setLocations(Array.isArray(optionsResponse?.locations) ? optionsResponse.locations : []);
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm(createEmptyForm());
      }
    } catch (err) {
      setError(err.message || 'Không tải được điểm đón/trả.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreateNew = () => {
    setSelectedId('');
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

      if (selectedId) {
        await updateBusStopPoint(selectedId, payload);
        setNotice('Đã cập nhật điểm đón/trả.');
      } else {
        await createBusStopPoint(payload);
        setNotice('Đã tạo điểm đón/trả mới.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không lưu được điểm đón/trả.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusStopPoint(item.id);
        setNotice('Đã khôi phục điểm đón/trả.');
      } else {
        await deleteBusStopPoint(item.id);
        setNotice('Đã ẩn điểm đón/trả.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái điểm đón/trả.');
    }
  };

  return (
    <BusManagementPageShell
      pageKey="stop-points"
      title="Điểm đón/trả"
      subtitle="Quản lý stop points của nhà xe để dùng cho tuyến đường và public search."
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
            Thêm điểm mới
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách điểm dừng</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Mỗi điểm dừng gắn với một địa điểm nội bộ của tenant.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải điểm dừng...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có điểm dừng nào.</div>
            ) : items.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => {
                  setSelectedId(item.id);
                  setForm(hydrateForm(item));
                }}
                className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                      <MapPin size={20} />
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
                        {item.location?.name || 'Chưa gắn location'} • {getStopPointTypeLabel(item.type)}
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
              </button>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật điểm dừng' : 'Tạo điểm dừng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Thông tin này sẽ được dùng khi dựng tuyến đường và lịch dừng.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Địa điểm</span>
            <select
              value={form.locationId}
              onChange={(event) => setForm((current) => ({ ...current, locationId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              required
            >
              <option value="">Chọn địa điểm</option>
              {locations.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại điểm dừng</span>
              <select
                value={form.type}
                onChange={(event) => setForm((current) => ({ ...current, type: Number(event.target.value) }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              >
                {BUS_STOP_POINT_TYPES.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
            </label>

            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thứ tự hiển thị</span>
              <input
                type="number"
                value={form.sortOrder}
                onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên hiển thị</span>
            <input
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              placeholder="Bến xe Mỹ Đình"
              required
            />
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Địa chỉ chi tiết</span>
            <input
              value={form.addressLine}
              onChange={(event) => setForm((current) => ({ ...current, addressLine: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              placeholder="Số nhà, đường..."
            />
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Vĩ độ</span>
              <input
                value={form.latitude}
                onChange={(event) => setForm((current) => ({ ...current, latitude: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                placeholder="21.028"
              />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Kinh độ</span>
              <input
                value={form.longitude}
                onChange={(event) => setForm((current) => ({ ...current, longitude: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                placeholder="105.783"
              />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ghi chú</span>
            <textarea
              rows="4"
              value={form.notes}
              onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
              className="w-full rounded-[2rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-bold text-slate-700 outline-none"
              placeholder="Ví dụ: dùng cho tuyến miền Bắc, có hỗ trợ đón khách tại cổng sau..."
            />
          </label>

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              className="h-4 w-4 rounded border-slate-300"
            />
            Kích hoạt điểm dừng này
          </label>

          <button
            type="submit"
            disabled={saving}
            className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
          >
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu thay đổi' : 'Tạo stop point'}
          </button>
        </form>
      </div>
    </BusManagementPageShell>
  );
};

export default BusStopPointsPage;
