import React, { useEffect, useState } from 'react';
import { LocateFixed, Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import BusManagementPageShell from '../components/BusManagementPageShell';
import {
  createDropoffPoint,
  createPickupPoint,
  deleteDropoffPoint,
  deletePickupPoint,
  listBusTrips,
  listBusTripStopTimes,
  listDropoffPoints,
  listPickupPoints,
  restoreDropoffPoint,
  restorePickupPoint,
  updateDropoffPoint,
  updatePickupPoint,
} from '../../../../services/busService';

function createEmptyPointForm() {
  return {
    name: '',
    addressLine: '',
    latitude: '',
    longitude: '',
    isDefault: false,
    sortOrder: 0,
    isActive: true,
  };
}

function hydratePointForm(item) {
  return {
    name: item.name || '',
    addressLine: item.addressLine || '',
    latitude: item.latitude ?? '',
    longitude: item.longitude ?? '',
    isDefault: item.isDefault ?? false,
    sortOrder: item.sortOrder ?? 0,
    isActive: item.isActive ?? true,
  };
}

function buildPointPayload(form) {
  return {
    name: form.name.trim(),
    addressLine: form.addressLine || null,
    latitude: form.latitude === '' ? null : Number(form.latitude),
    longitude: form.longitude === '' ? null : Number(form.longitude),
    isDefault: !!form.isDefault,
    sortOrder: Number(form.sortOrder || 0),
    isActive: !!form.isActive,
  };
}

const BusTripStopPointsPage = () => {
  const [searchParams] = useSearchParams();
  const [trips, setTrips] = useState([]);
  const [stopTimes, setStopTimes] = useState([]);
  const [pickupItems, setPickupItems] = useState([]);
  const [dropoffItems, setDropoffItems] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [selectedStopTimeId, setSelectedStopTimeId] = useState('');
  const [pickupSelectedId, setPickupSelectedId] = useState('');
  const [dropoffSelectedId, setDropoffSelectedId] = useState('');
  const [pickupForm, setPickupForm] = useState(createEmptyPointForm);
  const [dropoffForm, setDropoffForm] = useState(createEmptyPointForm);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  useEffect(() => {
    let active = true;

    listBusTrips()
      .then((response) => {
        if (!active) {
          return;
        }

        const nextTrips = Array.isArray(response?.items) ? response.items.filter((item) => !item.isDeleted) : [];
        setTrips(nextTrips);
        if (!selectedTripId) {
          setSelectedTripId(nextTrips[0]?.id || '');
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được danh sách chuyến.');
        }
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!selectedTripId) {
      setStopTimes([]);
      return;
    }

    let active = true;

    listBusTripStopTimes(selectedTripId)
      .then((response) => {
        if (!active) {
          return;
        }

        const nextItems = Array.isArray(response?.items) ? response.items : [];
        setStopTimes(nextItems);
        setSelectedStopTimeId(nextItems[0]?.id || '');
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được stop times của chuyến.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  const loadPointData = async () => {
    if (!selectedStopTimeId) {
      setPickupItems([]);
      setDropoffItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [pickupResponse, dropoffResponse] = await Promise.all([
        listPickupPoints(selectedStopTimeId, { includeDeleted: true }),
        listDropoffPoints(selectedStopTimeId, { includeDeleted: true }),
      ]);

      const nextPickup = Array.isArray(pickupResponse?.items) ? pickupResponse.items : [];
      const nextDropoff = Array.isArray(dropoffResponse?.items) ? dropoffResponse.items : [];
      setPickupItems(nextPickup);
      setDropoffItems(nextDropoff);

      if (nextPickup.length > 0) {
        const selected = nextPickup.find((item) => item.id === pickupSelectedId) || nextPickup[0];
        setPickupSelectedId(selected.id);
        setPickupForm(hydratePointForm(selected));
      } else {
        setPickupSelectedId('');
        setPickupForm(createEmptyPointForm());
      }

      if (nextDropoff.length > 0) {
        const selected = nextDropoff.find((item) => item.id === dropoffSelectedId) || nextDropoff[0];
        setDropoffSelectedId(selected.id);
        setDropoffForm(hydratePointForm(selected));
      } else {
        setDropoffSelectedId('');
        setDropoffForm(createEmptyPointForm());
      }
    } catch (err) {
      setError(err.message || 'Không tải được pickup/dropoff points.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPointData();
  }, [selectedStopTimeId]);

  const handleSavePickup = async (event) => {
    event.preventDefault();
    setError('');
    setNotice('');

    try {
      const payload = buildPointPayload(pickupForm);
      if (pickupSelectedId) {
        await updatePickupPoint(pickupSelectedId, payload);
        setNotice('Đã cập nhật điểm đón.');
      } else {
        await createPickupPoint(selectedStopTimeId, payload);
        setNotice('Đã tạo điểm đón.');
      }

      await loadPointData();
    } catch (err) {
      setError(err.message || 'Không lưu được điểm đón.');
    }
  };

  const handleSaveDropoff = async (event) => {
    event.preventDefault();
    setError('');
    setNotice('');

    try {
      const payload = buildPointPayload(dropoffForm);
      if (dropoffSelectedId) {
        await updateDropoffPoint(dropoffSelectedId, payload);
        setNotice('Đã cập nhật điểm trả.');
      } else {
        await createDropoffPoint(selectedStopTimeId, payload);
        setNotice('Đã tạo điểm trả.');
      }

      await loadPointData();
    } catch (err) {
      setError(err.message || 'Không lưu được điểm trả.');
    }
  };

  const handleTogglePickup = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restorePickupPoint(item.id);
        setNotice('Đã khôi phục điểm đón.');
      } else {
        await deletePickupPoint(item.id);
        setNotice('Đã ẩn điểm đón.');
      }

      await loadPointData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được điểm đón.');
    }
  };

  const handleToggleDropoff = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreDropoffPoint(item.id);
        setNotice('Đã khôi phục điểm trả.');
      } else {
        await deleteDropoffPoint(item.id);
        setNotice('Đã ẩn điểm trả.');
      }

      await loadPointData();
    } catch (err) {
      setError(err.message || 'Không cập nhật được điểm trả.');
    }
  };

  return (
    <BusManagementPageShell
      pageKey="trip-stop-points"
      title="Điểm đón / trả"
      subtitle="Mỗi lịch dừng có thể có nhiều điểm đón và trả linh hoạt cho từng chuyến."
      error={error}
      notice={notice}
      actions={(
        <button
          type="button"
          onClick={loadPointData}
          className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
        >
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <label className="space-y-2">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chuyến xe</span>
            <select
              value={selectedTripId}
              onChange={(event) => setSelectedTripId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {trips.map((trip) => (
                <option key={trip.id} value={trip.id}>{trip.name}</option>
              ))}
            </select>
          </label>
          <label className="space-y-2">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Lịch dừng</span>
            <select
              value={selectedStopTimeId}
              onChange={(event) => setSelectedStopTimeId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>Stop #{item.stopIndex}</option>
              ))}
            </select>
          </label>
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <p className="text-lg font-black text-slate-900">Điểm đón</p>
              <button
                type="button"
                onClick={() => {
                  setPickupSelectedId('');
                  setPickupForm(createEmptyPointForm());
                }}
                className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600 flex items-center gap-2"
              >
                <Plus size={14} />
                Điểm đón mới
              </button>
            </div>
            <div className="bg-slate-50 rounded-[2rem] border border-slate-100 divide-y divide-slate-100">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-500">Đang tải điểm đón...</div>
              ) : pickupItems.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-500">Chưa có điểm đón nào.</div>
              ) : pickupItems.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => {
                    setPickupSelectedId(item.id);
                    setPickupForm(hydratePointForm(item));
                  }}
                  className={`w-full px-6 py-5 text-left ${pickupSelectedId === item.id ? 'bg-white' : ''}`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <div className="w-10 h-10 rounded-2xl bg-white text-[#1EB4D4] flex items-center justify-center">
                        <LocateFixed size={18} />
                      </div>
                      <div>
                        <p className="font-black text-slate-900">{item.name}</p>
                        <p className="text-xs font-bold text-slate-400 mt-1">{item.addressLine || 'Chưa có địa chỉ chi tiết'}</p>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={(event) => {
                        event.stopPropagation();
                        handleTogglePickup(item);
                      }}
                      className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                    >
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </button>
              ))}
            </div>

            <form onSubmit={handleSavePickup} className="bg-white rounded-[2rem] border border-slate-100 p-6 space-y-4">
              <p className="font-black text-slate-900">{pickupSelectedId ? 'Cập nhật điểm đón' : 'Tạo điểm đón'}</p>
              <input value={pickupForm.name} onChange={(event) => setPickupForm((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none" placeholder="Tên điểm đón" required />
              <input value={pickupForm.addressLine} onChange={(event) => setPickupForm((current) => ({ ...current, addressLine: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none" placeholder="Địa chỉ chi tiết" />
              <button type="submit" className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white">
                Lưu điểm đón
              </button>
            </form>
          </div>

          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <p className="text-lg font-black text-slate-900">Điểm trả</p>
              <button
                type="button"
                onClick={() => {
                  setDropoffSelectedId('');
                  setDropoffForm(createEmptyPointForm());
                }}
                className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600 flex items-center gap-2"
              >
                <Plus size={14} />
                Điểm trả mới
              </button>
            </div>
            <div className="bg-slate-50 rounded-[2rem] border border-slate-100 divide-y divide-slate-100">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-500">Đang tải điểm trả...</div>
              ) : dropoffItems.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-500">Chưa có điểm trả nào.</div>
              ) : dropoffItems.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => {
                    setDropoffSelectedId(item.id);
                    setDropoffForm(hydratePointForm(item));
                  }}
                  className={`w-full px-6 py-5 text-left ${dropoffSelectedId === item.id ? 'bg-white' : ''}`}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <div className="w-10 h-10 rounded-2xl bg-white text-[#1EB4D4] flex items-center justify-center">
                        <LocateFixed size={18} />
                      </div>
                      <div>
                        <p className="font-black text-slate-900">{item.name}</p>
                        <p className="text-xs font-bold text-slate-400 mt-1">{item.addressLine || 'Chưa có địa chỉ chi tiết'}</p>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={(event) => {
                        event.stopPropagation();
                        handleToggleDropoff(item);
                      }}
                      className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                    >
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </button>
              ))}
            </div>

            <form onSubmit={handleSaveDropoff} className="bg-white rounded-[2rem] border border-slate-100 p-6 space-y-4">
              <p className="font-black text-slate-900">{dropoffSelectedId ? 'Cập nhật điểm trả' : 'Tạo điểm trả'}</p>
              <input value={dropoffForm.name} onChange={(event) => setDropoffForm((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none" placeholder="Tên điểm trả" required />
              <input value={dropoffForm.addressLine} onChange={(event) => setDropoffForm((current) => ({ ...current, addressLine: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none" placeholder="Địa chỉ chi tiết" />
              <button type="submit" className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white">
                Lưu điểm trả
              </button>
            </form>
          </div>
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusTripStopPointsPage;
