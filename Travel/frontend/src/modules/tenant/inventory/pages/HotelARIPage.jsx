import React, { useEffect, useMemo, useState } from 'react';
import { CalendarDays, RefreshCw, Save, Trash2 } from 'lucide-react';
import HotelModeShell from '../../hotel/components/HotelModeShell';
import {
  getAdminDailyRates,
  getAdminHotelOptions,
  getAdminRatePlan,
  getAdminRoomInventory,
  getHotelManagerOptions,
  getManagedDailyRates,
  getManagedRatePlan,
  getManagedRoomInventory,
  upsertAdminDailyRates,
  upsertAdminRoomInventory,
  upsertManagedDailyRates,
  upsertManagedRoomInventory,
  deleteAdminDailyRatesRange,
  deleteAdminRoomInventoryRange,
  deleteManagedDailyRatesRange,
  deleteManagedRoomInventoryRange,
} from '../../../../services/hotelService';
import { formatCurrency, formatDateOnly, getRatePlanTypeLabel } from '../../hotel/utils/presentation';

function toDateInput(value) {
  return value.toISOString().slice(0, 10);
}

function getDefaultDateRange() {
  const today = new Date();
  const twoWeeksLater = new Date(today);
  twoWeeksLater.setDate(today.getDate() + 13);

  return {
    fromDate: toDateInput(today),
    toDate: toDateInput(twoWeeksLater),
  };
}

export default function HotelARIPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [options, setOptions] = useState({
    hotels: [],
    roomTypes: [],
    ratePlans: [],
  });
  const [selectedHotelId, setSelectedHotelId] = useState('');
  const [selectedRoomTypeId, setSelectedRoomTypeId] = useState('');
  const [selectedRatePlanId, setSelectedRatePlanId] = useState('');
  const [selectedMappingId, setSelectedMappingId] = useState('');
  const [selectedRatePlanDetail, setSelectedRatePlanDetail] = useState(null);
  const [inventoryItems, setInventoryItems] = useState([]);
  const [dailyRateItems, setDailyRateItems] = useState([]);
  const [dateRange, setDateRange] = useState(getDefaultDateRange);
  const [inventoryForm, setInventoryForm] = useState({
    totalUnits: 10,
    soldUnits: 0,
    heldUnits: 0,
    status: 1,
    minNights: '',
    maxNights: '',
    notes: '',
  });
  const [rateForm, setRateForm] = useState({
    price: '',
    basePrice: '',
    taxes: '',
    fees: '',
    currencyCode: 'VND',
    isActive: true,
    metadataJson: '',
  });

  async function loadOptions() {
    const optionsResponse = isAdmin ? await getAdminHotelOptions(tenantId) : await getHotelManagerOptions();
    const hotels = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
    const roomTypes = Array.isArray(optionsResponse?.roomTypes) ? optionsResponse.roomTypes : [];
    const ratePlans = Array.isArray(optionsResponse?.ratePlans) ? optionsResponse.ratePlans : [];

    setOptions({ hotels, roomTypes, ratePlans });

    if (!hotels.length) {
      setSelectedHotelId('');
      setSelectedRoomTypeId('');
      setSelectedRatePlanId('');
      return;
    }

    const nextHotelId = selectedHotelId && hotels.some((item) => item.id === selectedHotelId)
      ? selectedHotelId
      : hotels[0].id;
    setSelectedHotelId(nextHotelId);

    const hotelRoomTypes = roomTypes.filter((item) => item.hotelId === nextHotelId && !item.isDeleted);
    const nextRoomTypeId = selectedRoomTypeId && hotelRoomTypes.some((item) => item.id === selectedRoomTypeId)
      ? selectedRoomTypeId
      : hotelRoomTypes[0]?.id || '';
    setSelectedRoomTypeId(nextRoomTypeId);

    const hotelRatePlans = ratePlans.filter((item) => item.hotelId === nextHotelId && !item.isDeleted);
    const nextRatePlanId = selectedRatePlanId && hotelRatePlans.some((item) => item.id === selectedRatePlanId)
      ? selectedRatePlanId
      : hotelRatePlans[0]?.id || '';
    setSelectedRatePlanId(nextRatePlanId);
  }

  async function loadRatePlanDetail(ratePlanId) {
    if (!ratePlanId) {
      setSelectedRatePlanDetail(null);
      setSelectedMappingId('');
      return;
    }

    const detail = isAdmin ? await getAdminRatePlan(ratePlanId, { includeDeleted: true }, tenantId) : await getManagedRatePlan(ratePlanId, { includeDeleted: true });
    setSelectedRatePlanDetail(detail);

    const mappings = Array.isArray(detail?.roomTypes) ? detail.roomTypes : [];
    const preferredMapping = mappings.find((item) => item.roomTypeId === selectedRoomTypeId) || mappings[0];
    setSelectedMappingId(preferredMapping?.id || '');
  }

  async function loadCalendars(roomTypeId, mappingId) {
    if (!roomTypeId) {
      setInventoryItems([]);
      setDailyRateItems([]);
      return;
    }

    const inventoryPromise = isAdmin
      ? getAdminRoomInventory(roomTypeId, dateRange, tenantId)
      : getManagedRoomInventory(roomTypeId, dateRange);

    const dailyRatePromise = mappingId
      ? (isAdmin ? getAdminDailyRates(mappingId, dateRange, tenantId) : getManagedDailyRates(mappingId, dateRange))
      : Promise.resolve([]);

    const [inventoryResponse, dailyRateResponse] = await Promise.all([inventoryPromise, dailyRatePromise]);
    setInventoryItems(Array.isArray(inventoryResponse) ? inventoryResponse : []);
    setDailyRateItems(Array.isArray(dailyRateResponse) ? dailyRateResponse : []);
  }

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLoading(false);
      setOptions({ hotels: [], roomTypes: [], ratePlans: [] });
      return;
    }

    setLoading(true);
    setError('');

    try {
      await loadOptions();
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải ARI khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  useEffect(() => {
    if (!selectedRatePlanId) {
      setSelectedRatePlanDetail(null);
      setSelectedMappingId('');
      return;
    }

    loadRatePlanDetail(selectedRatePlanId).catch((requestError) => {
      setError(requestError.message || 'Không thể tải chi tiết gói giá.');
    });
  }, [selectedRatePlanId, isAdmin, tenantId]);

  useEffect(() => {
    loadCalendars(selectedRoomTypeId, selectedMappingId).catch((requestError) => {
      setError(requestError.message || 'Không thể tải lịch ARI.');
    });
  }, [selectedRoomTypeId, selectedMappingId, dateRange.fromDate, dateRange.toDate, isAdmin, tenantId]);

  const hotelRoomTypes = useMemo(
    () => options.roomTypes.filter((item) => item.hotelId === selectedHotelId && !item.isDeleted),
    [options.roomTypes, selectedHotelId],
  );

  const hotelRatePlans = useMemo(
    () => options.ratePlans.filter((item) => item.hotelId === selectedHotelId && !item.isDeleted),
    [options.ratePlans, selectedHotelId],
  );

  const selectedMappings = useMemo(
    () => Array.isArray(selectedRatePlanDetail?.roomTypes) ? selectedRatePlanDetail.roomTypes : [],
    [selectedRatePlanDetail],
  );

  async function handleSaveInventory() {
    if (!selectedRoomTypeId) {
      setError('Hãy chọn hạng phòng trước khi lưu tồn kho.');
      return;
    }

    setError('');
    setNotice('');

    try {
      const payload = {
        fromDate: dateRange.fromDate,
        toDate: dateRange.toDate,
        totalUnits: Number(inventoryForm.totalUnits || 0),
        soldUnits: Number(inventoryForm.soldUnits || 0),
        heldUnits: Number(inventoryForm.heldUnits || 0),
        status: Number(inventoryForm.status || 1),
        minNights: inventoryForm.minNights === '' ? null : Number(inventoryForm.minNights),
        maxNights: inventoryForm.maxNights === '' ? null : Number(inventoryForm.maxNights),
        notes: inventoryForm.notes || null,
      };

      if (isAdmin) {
        await upsertAdminRoomInventory(selectedRoomTypeId, payload, tenantId);
      } else {
        await upsertManagedRoomInventory(selectedRoomTypeId, payload);
      }

      setNotice('Đã lưu tồn kho phòng theo khoảng ngày.');
      await loadCalendars(selectedRoomTypeId, selectedMappingId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu tồn kho phòng.');
    }
  }

  async function handleDeleteInventory() {
    if (!selectedRoomTypeId) {
      return;
    }

    try {
      const payload = { fromDate: dateRange.fromDate, toDate: dateRange.toDate };
      if (isAdmin) {
        await deleteAdminRoomInventoryRange(selectedRoomTypeId, payload, tenantId);
      } else {
        await deleteManagedRoomInventoryRange(selectedRoomTypeId, payload);
      }
      setNotice('Đã xóa tồn kho trong khoảng ngày đã chọn.');
      await loadCalendars(selectedRoomTypeId, selectedMappingId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể xóa tồn kho phòng.');
    }
  }

  async function handleSaveRates() {
    if (!selectedMappingId) {
      setError('Hãy chọn mapping gói giá và hạng phòng trước khi lưu giá.');
      return;
    }

    try {
      const payload = {
        fromDate: dateRange.fromDate,
        toDate: dateRange.toDate,
        price: Number(rateForm.price || 0),
        basePrice: rateForm.basePrice === '' ? null : Number(rateForm.basePrice),
        taxes: rateForm.taxes === '' ? null : Number(rateForm.taxes),
        fees: rateForm.fees === '' ? null : Number(rateForm.fees),
        currencyCode: rateForm.currencyCode || 'VND',
        isActive: !!rateForm.isActive,
        metadataJson: rateForm.metadataJson || null,
      };

      if (isAdmin) {
        await upsertAdminDailyRates(selectedMappingId, payload, tenantId);
      } else {
        await upsertManagedDailyRates(selectedMappingId, payload);
      }

      setNotice('Đã lưu bảng giá theo khoảng ngày.');
      await loadCalendars(selectedRoomTypeId, selectedMappingId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu giá bán.');
    }
  }

  async function handleDeleteRates() {
    if (!selectedMappingId) {
      return;
    }

    try {
      const payload = { fromDate: dateRange.fromDate, toDate: dateRange.toDate };
      if (isAdmin) {
        await deleteAdminDailyRatesRange(selectedMappingId, payload, tenantId);
      } else {
        await deleteManagedDailyRatesRange(selectedMappingId, payload);
      }
      setNotice('Đã xóa bảng giá trong khoảng ngày đã chọn.');
      await loadCalendars(selectedRoomTypeId, selectedMappingId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể xóa bảng giá.');
    }
  }

  return (
    <HotelModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="ari"
      title="ARI khách sạn"
      subtitle="Điều chỉnh tồn kho hạng phòng và giá bán theo ngày cho marketplace khách sạn."
      notice={notice}
      error={error}
      actions={(
        <button
          type="button"
          onClick={loadData}
          className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
        >
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
        <div className="xl:col-span-1 bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-lg font-black text-slate-900">Bộ lọc ARI</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn khách sạn, hạng phòng và gói giá cần cập nhật.</p>
          </div>

          <select
            value={selectedHotelId}
            onChange={(event) => {
              const nextHotelId = event.target.value;
              setSelectedHotelId(nextHotelId);
              setSelectedRoomTypeId(options.roomTypes.find((item) => item.hotelId === nextHotelId && !item.isDeleted)?.id || '');
              setSelectedRatePlanId(options.ratePlans.find((item) => item.hotelId === nextHotelId && !item.isDeleted)?.id || '');
            }}
            className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none"
          >
            <option value="">Chọn khách sạn</option>
            {options.hotels.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <select
            value={selectedRoomTypeId}
            onChange={(event) => setSelectedRoomTypeId(event.target.value)}
            className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none"
          >
            <option value="">Chọn hạng phòng</option>
            {hotelRoomTypes.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <select
            value={selectedRatePlanId}
            onChange={(event) => setSelectedRatePlanId(event.target.value)}
            className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none"
          >
            <option value="">Chọn gói giá</option>
            {hotelRatePlans.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <select
            value={selectedMappingId}
            onChange={(event) => setSelectedMappingId(event.target.value)}
            className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none"
          >
            <option value="">Chọn mapping giá bán</option>
            {selectedMappings.map((item) => (
              <option key={item.id} value={item.id}>
                {item.roomTypeName} • {formatCurrency(item.basePrice, item.currencyCode)}
              </option>
            ))}
          </select>

          <div className="grid grid-cols-2 gap-4">
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Từ ngày</span>
              <input
                type="date"
                value={dateRange.fromDate}
                onChange={(event) => setDateRange((current) => ({ ...current, fromDate: event.target.value }))}
                className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none"
              />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Đến ngày</span>
              <input
                type="date"
                value={dateRange.toDate}
                onChange={(event) => setDateRange((current) => ({ ...current, toDate: event.target.value }))}
                className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none"
              />
            </label>
          </div>

          {selectedRatePlanDetail ? (
            <div className="rounded-[2rem] bg-slate-50 p-5">
              <p className="text-sm font-black text-slate-900">{selectedRatePlanDetail.name}</p>
              <p className="text-xs font-bold text-slate-400 mt-2">
                {getRatePlanTypeLabel(selectedRatePlanDetail.type)} • {selectedMappings.length} mapping hạng phòng
              </p>
            </div>
          ) : null}
        </div>

        <div className="xl:col-span-2 space-y-8">
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
            <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-lg font-black text-slate-900">Kho phòng theo ngày</p>
                  <p className="text-xs font-bold text-slate-400 mt-1">Bulk update số phòng mở bán cho hạng phòng đang chọn.</p>
                </div>
                <CalendarDays size={20} className="text-[#1EB4D4]" />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <input value={inventoryForm.totalUnits} onChange={(event) => setInventoryForm((current) => ({ ...current, totalUnits: event.target.value }))} placeholder="Tổng phòng" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <input value={inventoryForm.soldUnits} onChange={(event) => setInventoryForm((current) => ({ ...current, soldUnits: event.target.value }))} placeholder="Đã bán" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <input value={inventoryForm.heldUnits} onChange={(event) => setInventoryForm((current) => ({ ...current, heldUnits: event.target.value }))} placeholder="Đang giữ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <select value={inventoryForm.status} onChange={(event) => setInventoryForm((current) => ({ ...current, status: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
                  <option value={1}>Mở bán</option>
                  <option value={2}>Đóng bán</option>
                </select>
                <input value={inventoryForm.minNights} onChange={(event) => setInventoryForm((current) => ({ ...current, minNights: event.target.value }))} placeholder="Min nights" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <input value={inventoryForm.maxNights} onChange={(event) => setInventoryForm((current) => ({ ...current, maxNights: event.target.value }))} placeholder="Max nights" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
              </div>
              <textarea value={inventoryForm.notes} onChange={(event) => setInventoryForm((current) => ({ ...current, notes: event.target.value }))} rows={3} placeholder="Ghi chú vận hành" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

              <div className="flex flex-wrap gap-3">
                <button type="button" onClick={handleSaveInventory} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
                  <Save size={16} />
                  Lưu tồn kho
                </button>
                <button type="button" onClick={handleDeleteInventory} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600 flex items-center gap-2">
                  <Trash2 size={16} />
                  Xóa khoảng ngày
                </button>
              </div>
            </div>

            <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-lg font-black text-slate-900">Giá bán theo ngày</p>
                  <p className="text-xs font-bold text-slate-400 mt-1">Áp giá cho mapping hạng phòng - gói giá đang chọn.</p>
                </div>
                <CalendarDays size={20} className="text-[#1EB4D4]" />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <input value={rateForm.price} onChange={(event) => setRateForm((current) => ({ ...current, price: event.target.value }))} placeholder="Giá bán" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <input value={rateForm.basePrice} onChange={(event) => setRateForm((current) => ({ ...current, basePrice: event.target.value }))} placeholder="Giá gốc" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <input value={rateForm.taxes} onChange={(event) => setRateForm((current) => ({ ...current, taxes: event.target.value }))} placeholder="Thuế" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <input value={rateForm.fees} onChange={(event) => setRateForm((current) => ({ ...current, fees: event.target.value }))} placeholder="Phí" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <input value={rateForm.currencyCode} onChange={(event) => setRateForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))} placeholder="Mã tiền tệ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
                <label className="flex items-center gap-3 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-600">
                  <input type="checkbox" checked={rateForm.isActive} onChange={(event) => setRateForm((current) => ({ ...current, isActive: event.target.checked }))} />
                  Cho phép bán
                </label>
              </div>
              <textarea value={rateForm.metadataJson} onChange={(event) => setRateForm((current) => ({ ...current, metadataJson: event.target.value }))} rows={3} placeholder="Metadata JSON" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

              <div className="flex flex-wrap gap-3">
                <button type="button" onClick={handleSaveRates} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
                  <Save size={16} />
                  Lưu bảng giá
                </button>
                <button type="button" onClick={handleDeleteRates} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600 flex items-center gap-2">
                  <Trash2 size={16} />
                  Xóa khoảng ngày
                </button>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
            <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
              <div className="px-8 py-6 border-b border-slate-100">
                <p className="text-lg font-black text-slate-900">Lịch tồn kho</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Các ngày đang mở bán cho hạng phòng đã chọn.</p>
              </div>
              <div className="divide-y divide-slate-50 max-h-[420px] overflow-y-auto">
                {inventoryItems.length === 0 ? (
                  <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có dữ liệu tồn kho trong khoảng ngày này.</div>
                ) : inventoryItems.map((item) => (
                  <div key={item.id} className="px-8 py-5 flex items-center justify-between gap-4">
                    <div>
                      <p className="font-black text-slate-900">{formatDateOnly(item.date)}</p>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        Tổng {item.totalUnits} • Đã bán {item.soldUnits} • Giữ {item.heldUnits}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-xl font-black text-slate-900">{item.availableUnits}</p>
                      <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">
                        {Number(item.status) === 2 ? 'Đóng bán' : 'Mở bán'}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
              <div className="px-8 py-6 border-b border-slate-100">
                <p className="text-lg font-black text-slate-900">Lịch giá bán</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Giá theo ngày của mapping gói giá đang chọn.</p>
              </div>
              <div className="divide-y divide-slate-50 max-h-[420px] overflow-y-auto">
                {dailyRateItems.length === 0 ? (
                  <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có dữ liệu giá trong khoảng ngày này.</div>
                ) : dailyRateItems.map((item) => (
                  <div key={item.id} className="px-8 py-5 flex items-center justify-between gap-4">
                    <div>
                      <p className="font-black text-slate-900">{formatDateOnly(item.date)}</p>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        Giá gốc {formatCurrency(item.basePrice || item.price, item.currencyCode)}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-xl font-black text-[#1EB4D4]">{formatCurrency(item.price, item.currencyCode)}</p>
                      <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">
                        Thuế {formatCurrency(item.taxes || 0, item.currencyCode)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </HotelModeShell>
  );
}
