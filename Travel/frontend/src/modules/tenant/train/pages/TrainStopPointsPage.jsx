import React, { useEffect, useState } from 'react';
import { MapPinned, Plus, RefreshCw } from 'lucide-react';
import TrainManagementPageShell from '../components/TrainManagementPageShell';
import { getStopPointTypeLabel, TRAIN_STOP_POINT_TYPES } from '../utils/presentation';
import {
  createTrainStopPoint,
  deleteTrainStopPoint,
  getTrainManagerOptions,
  listTrainStopPoints,
  restoreTrainStopPoint,
  updateTrainStopPoint,
} from '../../../../services/trainService';

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

const TrainStopPointsPage = () => {
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
        getTrainManagerOptions(),
        listTrainStopPoints({ includeDeleted: true }),
      ]);

      setLocations(Array.isArray(optionsResponse?.locations) ? optionsResponse.locations : []);
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
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
      setError(err.message || 'Không tải được ga tàu.');
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
      const payload = {
        locationId: form.locationId,
        type: Number(form.type),
        name: form.name.trim(),
        addressLine: form.addressLine.trim() || null,
        latitude: form.latitude === '' ? null : Number(form.latitude),
        longitude: form.longitude === '' ? null : Number(form.longitude),
        notes: form.notes.trim() || null,
        sortOrder: Number(form.sortOrder || 0),
        isActive: !!form.isActive,
      };

      if (selectedId) {
        await updateTrainStopPoint(selectedId, payload);
        setNotice('Đã cập nhật ga tàu.');
      } else {
        await createTrainStopPoint(payload);
        setNotice('Đã tạo ga tàu mới.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không lưu được ga tàu.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreTrainStopPoint(item.id);
        setNotice('Đã khôi phục ga tàu.');
      } else {
        await deleteTrainStopPoint(item.id);
        setNotice('Đã ẩn ga tàu.');
      }

      await loadData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái ga tàu.');
    }
  };

  const locationLookup = Object.fromEntries(locations.map((item) => [item.id, item]));

  return (
    <TrainManagementPageShell
      pageKey="stop-points"
      title="Ga tàu"
      subtitle="Ga tàu là nền tảng để dựng tuyến, lịch dừng và tìm kiếm public."
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
            Thêm ga mới
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách ga tàu</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chỉ những ga đang hoạt động mới nên dùng cho public search.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải ga tàu...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có ga tàu nào.</div>
            ) : items.map((item) => (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => {
                  setSelectedId(item.id);
                  setForm(hydrateForm(item));
                }}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    setSelectedId(item.id);
                    setForm(hydrateForm(item));
                  }
                }}
                className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                      <MapPinned size={20} />
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
                        {locationLookup[item.locationId]?.name || 'Chưa gắn địa điểm'} • {getStopPointTypeLabel(item.type)}
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

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật ga tàu' : 'Tạo ga tàu mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Tên ga nên thống nhất với cách hiển thị cho khách hàng.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Địa điểm liên kết</span>
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
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại stop point</span>
              <select
                value={form.type}
                onChange={(event) => setForm((current) => ({ ...current, type: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              >
                {TRAIN_STOP_POINT_TYPES.map((item) => (
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
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên ga tàu</span>
            <input
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              placeholder="Ga Hà Nội"
              required
            />
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Địa chỉ</span>
            <input
              value={form.addressLine}
              onChange={(event) => setForm((current) => ({ ...current, addressLine: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            />
          </label>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Vĩ độ</span>
              <input
                type="number"
                step="any"
                value={form.latitude}
                onChange={(event) => setForm((current) => ({ ...current, latitude: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>

            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Kinh độ</span>
              <input
                type="number"
                step="any"
                value={form.longitude}
                onChange={(event) => setForm((current) => ({ ...current, longitude: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ghi chú</span>
            <textarea
              rows={3}
              value={form.notes}
              onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
              className="w-full rounded-[1.75rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-medium text-slate-700 outline-none"
            />
          </label>

          <label className="inline-flex items-center gap-3 text-sm font-bold text-slate-600">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              className="w-4 h-4 rounded border-slate-300 text-blue-600 focus:ring-blue-200"
            />
            Kích hoạt ga tàu này
          </label>

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-2xl px-5 py-4 text-sm font-black uppercase tracking-widest transition-all ${
              saving ? 'bg-slate-200 text-slate-500' : 'bg-slate-900 text-white hover:bg-[#1EB4D4]'
            }`}
          >
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu thay đổi' : 'Tạo ga tàu'}
          </button>
        </form>
      </div>
    </TrainManagementPageShell>
  );
};

export default TrainStopPointsPage;
