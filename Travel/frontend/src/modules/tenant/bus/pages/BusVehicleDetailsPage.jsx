import React, { useEffect, useState } from 'react';
import { Bus, Plus, RefreshCw } from 'lucide-react';
import BusManagementPageShell from '../components/BusManagementPageShell';
import { createBusVehicleDetail, deleteBusVehicleDetail, getBusManagerOptions, listBusVehicleDetails, restoreBusVehicleDetail, updateBusVehicleDetail } from '../../../../services/busService';
import { parseAmenities } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';

const AMENITY_OPTIONS = ['Wifi', 'Điều hòa', 'Nước uống', 'Khăn lạnh', 'Cổng sạc', 'Chăn mền', 'Màn hình', 'Ghế massage'];

function createEmptyForm() {
  return {
    vehicleId: '',
    busType: '',
    amenitiesJson: JSON.stringify(['Wifi', 'Điều hòa']),
  };
}

function hydrateForm(item) {
  return {
    vehicleId: item.vehicleId || '',
    busType: item.busType || '',
    amenitiesJson: item.amenitiesJson || '[]',
  };
}

const BusVehicleDetailsPage = () => {
  const [vehicles, setVehicles] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [customAmenity, setCustomAmenity] = useState('');

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getBusManagerOptions(),
        listBusVehicleDetails({ includeDeleted: true }),
      ]);

      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      setVehicles(Array.isArray(optionsResponse?.vehicles) ? optionsResponse.vehicles : []);
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
      setError(err.message || 'Không tải được chi tiết xe khách.');
    } finally {
      setLoading(false);
    }
  };

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [loadDataRef]);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = {
        vehicleId: form.vehicleId,
        busType: form.busType || null,
        amenitiesJson: form.amenitiesJson || null,
      };

      if (selectedId) {
        await updateBusVehicleDetail(selectedId, payload);
        setNotice('Đã cập nhật chi tiết xe.');
      } else {
        await createBusVehicleDetail(payload);
        setNotice('Đã tạo chi tiết xe.');
      }

      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không lưu được chi tiết xe.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusVehicleDetail(item.id);
        setNotice('Đã khôi phục chi tiết xe.');
      } else {
        await deleteBusVehicleDetail(item.id);
        setNotice('Đã ẩn chi tiết xe.');
      }

      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái chi tiết xe.');
    }
  };

  const selectedAmenities = parseAmenities(form.amenitiesJson);

  const handleToggleAmenity = (amenity) => {
    setForm((current) => {
      const nextAmenities = new Set(parseAmenities(current.amenitiesJson));
      if (nextAmenities.has(amenity)) {
        nextAmenities.delete(amenity);
      } else {
        nextAmenities.add(amenity);
      }

      return { ...current, amenitiesJson: JSON.stringify(Array.from(nextAmenities)) };
    });
  };

  const handleAddCustomAmenity = () => {
    const value = customAmenity.trim();
    if (!value) {
      return;
    }

    setForm((current) => {
      const nextAmenities = new Set(parseAmenities(current.amenitiesJson));
      nextAmenities.add(value);
      return { ...current, amenitiesJson: JSON.stringify(Array.from(nextAmenities)) };
    });
    setCustomAmenity('');
  };

  return (
    <BusManagementPageShell
      pageKey="vehicle-details"
      title="Chi tiết xe khách"
      subtitle="Khai báo loại xe và tiện ích để trang chi tiết hiển thị đúng cho khách hàng."
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
            onClick={() => {
              setSelectedId('');
              setForm(createEmptyForm());
            }}
            className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2"
          >
            <Plus size={16} />
            Thêm cấu hình xe
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách cấu hình xe</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Các tiện ích này được hiển thị trên trang chi tiết xe.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải cấu hình xe...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có chi tiết xe nào.</div>
            ) : items.map((item) => {
              const vehicle = vehicles.find((vehicleItem) => vehicleItem.id === item.vehicleId);
              const amenities = parseAmenities(item.amenitiesJson);

              return (
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
                        <Bus size={20} />
                      </div>
                      <div>
                        <div className="flex items-center gap-3 flex-wrap">
                          <p className="font-black text-slate-900">{vehicle?.name || item.vehicleId}</p>
                          {item.isDeleted ? (
                            <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                              Đã ẩn
                            </span>
                          ) : null}
                        </div>
                        <p className="text-xs font-bold text-slate-400 mt-2">
                          {item.busType || 'Chưa khai báo loại xe'} • {vehicle?.plateNumber || 'Chưa có biển số'}
                        </p>
                        {amenities.length > 0 ? (
                          <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">
                            {amenities.join(' • ')}
                          </p>
                        ) : null}
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
              );
            })}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật chi tiết xe' : 'Tạo chi tiết xe mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn tiện ích có sẵn hoặc thêm tiện ích riêng để đồng bộ ra trang khách hàng.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Xe</span>
            <select
              value={form.vehicleId}
              onChange={(event) => setForm((current) => ({ ...current, vehicleId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              required
            >
              <option value="">Chọn xe</option>
              {vehicles.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} {item.plateNumber ? `• ${item.plateNumber}` : ''}
                </option>
              ))}
            </select>
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại xe</span>
            <input
              value={form.busType}
              onChange={(event) => setForm((current) => ({ ...current, busType: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              placeholder="Limousine 9 chỗ / Giường nằm 40 chỗ"
            />
          </label>

          <div className="space-y-3">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tiện ích trên xe</span>
            <div className="flex flex-wrap gap-2">
              {AMENITY_OPTIONS.map((amenity) => {
                const active = selectedAmenities.includes(amenity);

                return (
                  <button
                    key={amenity}
                    type="button"
                    onClick={() => handleToggleAmenity(amenity)}
                    className={`px-4 py-2 rounded-2xl text-xs font-black transition-all ${
                      active ? 'bg-slate-900 text-white' : 'bg-slate-50 text-slate-500 border border-slate-100'
                    }`}
                  >
                    {amenity}
                  </button>
                );
              })}
            </div>
            <div className="flex gap-3">
              <input
                value={customAmenity}
                onChange={(event) => setCustomAmenity(event.target.value)}
                className="min-w-0 flex-1 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
                placeholder="Thêm tiện ích khác"
              />
              <button
                type="button"
                onClick={handleAddCustomAmenity}
                className="px-5 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600"
              >
                Thêm
              </button>
            </div>
          </div>

          <button
            type="submit"
            disabled={saving}
            className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
          >
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu thay đổi' : 'Tạo chi tiết xe'}
          </button>
        </form>
      </div>
    </BusManagementPageShell>
  );
};

export default BusVehicleDetailsPage;
