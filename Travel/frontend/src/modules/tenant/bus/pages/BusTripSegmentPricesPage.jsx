import React, { useEffect, useState } from 'react';
import { CircleDollarSign, Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import BusManagementPageShell from '../components/BusManagementPageShell';
import { formatCurrency } from '../utils/presentation';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createBusTripSegmentPrice,
  deleteBusTripSegmentPrice,
  generateBusTripSegmentPrices,
  listBusTrips,
  listBusTripSegmentPrices,
  listBusTripStopTimes,
  restoreBusTripSegmentPrice,
  updateBusTripSegmentPrice,
} from '../../../../services/busService';

function createEmptyForm() {
  return {
    fromTripStopTimeId: '',
    toTripStopTimeId: '',
    currencyCode: 'VND',
    baseFare: 0,
    taxesFees: 0,
    totalPrice: 0,
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    fromTripStopTimeId: item.fromTripStopTimeId || '',
    toTripStopTimeId: item.toTripStopTimeId || '',
    currencyCode: item.currencyCode || 'VND',
    baseFare: item.baseFare ?? 0,
    taxesFees: item.taxesFees ?? 0,
    totalPrice: item.totalPrice ?? 0,
    isActive: item.isActive ?? true,
  };
}

const BusTripSegmentPricesPage = () => {
  const [searchParams] = useSearchParams();
  const [trips, setTrips] = useState([]);
  const [stopTimes, setStopTimes] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedTripId, setSelectedTripId] = useState(searchParams.get('tripId') || '');
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadItemsRef = useLatestRef(loadItems);

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
  }, [selectedTripId]);

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
        setForm((current) => ({
          ...current,
          fromTripStopTimeId: current.fromTripStopTimeId || nextItems[0]?.id || '',
          toTripStopTimeId: current.toTripStopTimeId || nextItems[1]?.id || nextItems[0]?.id || '',
        }));
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được lịch dừng của chuyến.');
        }
      });

    return () => {
      active = false;
    };
  }, [selectedTripId]);

  const loadItems = async () => {
    if (!selectedTripId) {
      setItems([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listBusTripSegmentPrices(selectedTripId, { includeDeleted: true });
      const nextItems = Array.isArray(response?.items) ? response.items : [];
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
      setError(err.message || 'Không tải được giá chặng.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadItemsRef.current();
  }, [loadItemsRef, selectedTripId]);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = {
        tripId: selectedTripId,
        fromTripStopTimeId: form.fromTripStopTimeId,
        toTripStopTimeId: form.toTripStopTimeId,
        currencyCode: form.currencyCode,
        baseFare: Number(form.baseFare || 0),
        taxesFees: Number(form.taxesFees || 0),
        totalPrice: Number(form.baseFare || 0) + Number(form.taxesFees || 0),
        isActive: !!form.isActive,
      };

      if (selectedId) {
        await updateBusTripSegmentPrice(selectedId, payload);
        setNotice('Đã cập nhật giá chặng.');
      } else {
        await createBusTripSegmentPrice(payload);
        setNotice('Đã tạo giá chặng mới.');
      }

      await loadItemsRef.current();
    } catch (err) {
      setError(err.message || 'Không lưu được giá chặng.');
    } finally {
      setSaving(false);
    }
  };

  const handleGenerate = async () => {
    if (!selectedTripId) {
      return;
    }

    setError('');
    setNotice('');

    try {
      await generateBusTripSegmentPrices({
        tripId: selectedTripId,
        currencyCode: 'VND',
        defaultBaseFare: 100000,
        defaultTaxesFees: 0,
        isActive: true,
      });

      setNotice('Đã sinh ma trận giá chặng mặc định.');
      await loadItemsRef.current();
    } catch (err) {
      setError(err.message || 'Không sinh được ma trận giá chặng.');
    }
  };

  const handleToggleDelete = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusTripSegmentPrice(item.id);
        setNotice('Đã khôi phục giá chặng.');
      } else {
        await deleteBusTripSegmentPrice(item.id);
        setNotice('Đã ẩn giá chặng.');
      }

      await loadItemsRef.current();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái giá chặng.');
    }
  };

  const computedTotalPrice = Number(form.baseFare || 0) + Number(form.taxesFees || 0);

  return (
    <BusManagementPageShell
      pageKey="trip-segment-prices"
      title="Giá chặng i → j"
      subtitle="Thiết lập mức giá theo từng cặp lịch dừng để public search và giữ chỗ tính đúng giá."
      error={error}
      notice={notice}
      actions={(
        <>
          <button
            type="button"
            onClick={loadItems}
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
            Thêm giá chặng
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh sách giá chặng</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mỗi dòng là một cặp lịch dừng từ điểm lên đến điểm xuống.</p>
            </div>
            <div className="flex items-center gap-3">
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <select
                  value={selectedTripId}
                  onChange={(event) => setSelectedTripId(event.target.value)}
                  className="bg-transparent text-sm font-bold text-slate-700 outline-none"
                >
                  {trips.map((trip) => (
                    <option key={trip.id} value={trip.id}>{trip.name}</option>
                  ))}
                </select>
              </div>
              <button
                type="button"
                onClick={handleGenerate}
                className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600"
              >
                Sinh ma trận giá
              </button>
            </div>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải giá chặng...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có giá chặng nào cho chuyến này.</div>
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
                      <CircleDollarSign size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">
                          Điểm dừng số {Number(item.fromStopIndex) + 1} → Điểm dừng số {Number(item.toStopIndex) + 1}
                        </p>
                        {item.isDeleted ? (
                          <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                            Đã ẩn
                          </span>
                        ) : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        Giá bán: {formatCurrency(item.totalPrice, item.currencyCode)}
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
            <p className="text-xl font-black text-slate-900">{selectedId ? 'Cập nhật giá chặng' : 'Tạo giá chặng mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Tổng giá nên bằng giá cơ bản cộng thuế/phí để lưu đúng snapshot giá.</p>
          </div>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điểm lên</span>
            <select
              value={form.fromTripStopTimeId}
              onChange={(event) => setForm((current) => ({ ...current, fromTripStopTimeId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              required
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>Điểm dừng số {Number(item.stopIndex) + 1}</option>
              ))}
            </select>
          </label>

          <label className="space-y-2 block">
            <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Điểm xuống</span>
            <select
              value={form.toTripStopTimeId}
              onChange={(event) => setForm((current) => ({ ...current, toTripStopTimeId: event.target.value }))}
              className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              required
            >
              {stopTimes.map((item) => (
                <option key={item.id} value={item.id}>Điểm dừng số {Number(item.stopIndex) + 1}</option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Giá cơ bản</span>
              <input
                type="number"
                value={form.baseFare}
                onChange={(event) => setForm((current) => ({ ...current, baseFare: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thuế / Phí</span>
              <input
                type="number"
                value={form.taxesFees}
                onChange={(event) => setForm((current) => ({ ...current, taxesFees: event.target.value }))}
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tổng giá</span>
              <input
                type="number"
                value={computedTotalPrice}
                readOnly
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none"
              />
            </label>
          </div>

          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              className="h-4 w-4 rounded border-slate-300"
            />
            Kích hoạt giá chặng này
          </label>

          <button
            type="submit"
            disabled={saving}
            className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70"
          >
            {saving ? 'Đang lưu...' : selectedId ? 'Lưu giá chặng' : 'Tạo giá chặng'}
          </button>
        </form>
      </div>
    </BusManagementPageShell>
  );
};

export default BusTripSegmentPricesPage;
