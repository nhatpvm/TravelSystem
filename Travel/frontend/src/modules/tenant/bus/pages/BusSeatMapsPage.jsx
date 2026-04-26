import React, { useEffect, useMemo, useState } from 'react';
import { Armchair, Plus, RefreshCw } from 'lucide-react';
import BusManagementPageShell from '../components/BusManagementPageShell';
import {
  createBusSeatMap,
  deleteBusSeat,
  deleteBusSeatMap,
  generateBusSeatMapSeats,
  listBusSeatMapSeats,
  listBusSeatMaps,
  restoreBusSeat,
  restoreBusSeatMap,
  updateBusSeat,
  updateBusSeatMap,
} from '../../../../services/busService';

const VEHICLE_TYPES = [
  { value: 1, label: 'Xe khách' },
  { value: 4, label: 'Xe tour' },
];

const SEAT_TYPES = [
  { value: 1, label: 'Ghế thường' },
  { value: 2, label: 'Ghế VIP' },
  { value: 3, label: 'Giường dưới' },
  { value: 4, label: 'Giường trên' },
];

function createEmptyMapForm() {
  return {
    vehicleType: 1,
    code: '',
    name: '',
    totalRows: 10,
    totalColumns: 4,
    deckCount: 1,
    seatLabelScheme: '',
    isActive: true,
  };
}

function hydrateMapForm(item) {
  return {
    vehicleType: item.vehicleType ?? 1,
    code: item.code || '',
    name: item.name || '',
    totalRows: item.totalRows ?? 10,
    totalColumns: item.totalColumns ?? 4,
    deckCount: item.deckCount ?? 1,
    seatLabelScheme: item.seatLabelScheme || '',
    isActive: item.isActive ?? true,
  };
}

function buildMapPayload(form) {
  return {
    vehicleType: Number(form.vehicleType || 1),
    code: form.code.trim(),
    name: form.name.trim(),
    totalRows: Number(form.totalRows || 0),
    totalColumns: Number(form.totalColumns || 0),
    deckCount: Number(form.deckCount || 1),
    layoutVersion: null,
    seatLabelScheme: form.seatLabelScheme || null,
    isActive: !!form.isActive,
    metadataJson: null,
  };
}

function createGenerateForm() {
  return {
    prefix: 'A',
    seatType: 1,
    overwriteExisting: false,
  };
}

function hydrateSeatForm(item) {
  return {
    seatNumber: item.seatNumber || '',
    rowIndex: item.rowIndex ?? 0,
    columnIndex: item.columnIndex ?? 0,
    deckIndex: item.deckIndex ?? 1,
    seatType: item.seatType ?? 1,
    seatClass: item.seatClass ?? 0,
    isAisle: item.isAisle ?? false,
    isWindow: item.isWindow ?? false,
    priceModifier: item.priceModifier ?? 0,
    isActive: item.isActive ?? true,
  };
}

function buildSeatPayload(form) {
  return {
    seatNumber: form.seatNumber.trim(),
    rowIndex: Number(form.rowIndex || 0),
    columnIndex: Number(form.columnIndex || 0),
    deckIndex: Number(form.deckIndex || 1),
    seatType: Number(form.seatType || 1),
    seatClass: Number(form.seatClass || 0),
    isAisle: !!form.isAisle,
    isWindow: !!form.isWindow,
    priceModifier: Number(form.priceModifier || 0),
    isActive: !!form.isActive,
  };
}

const BusSeatMapsPage = () => {
  const [maps, setMaps] = useState([]);
  const [seats, setSeats] = useState([]);
  const [selectedMapId, setSelectedMapId] = useState('');
  const [selectedSeatId, setSelectedSeatId] = useState('');
  const [mapForm, setMapForm] = useState(createEmptyMapForm);
  const [generateForm, setGenerateForm] = useState(createGenerateForm);
  const [seatForm, setSeatForm] = useState(() => hydrateSeatForm({}));
  const [loadingMaps, setLoadingMaps] = useState(true);
  const [loadingSeats, setLoadingSeats] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedMap = useMemo(() => maps.find((item) => item.id === selectedMapId) || null, [maps, selectedMapId]);

  const loadMaps = async () => {
    setLoadingMaps(true);
    setError('');

    try {
      const response = await listBusSeatMaps({ includeDeleted: true });
      const nextMaps = Array.isArray(response?.items) ? response.items : [];
      setMaps(nextMaps);

      if (nextMaps.length > 0) {
        const selected = nextMaps.find((item) => item.id === selectedMapId) || nextMaps[0];
        setSelectedMapId(selected.id);
        setMapForm(hydrateMapForm(selected));
      } else {
        setSelectedMapId('');
        setMapForm(createEmptyMapForm());
      }
    } catch (err) {
      setError(err.message || 'Không tải được sơ đồ ghế.');
    } finally {
      setLoadingMaps(false);
    }
  };

  const loadSeats = async () => {
    if (!selectedMapId) {
      setSeats([]);
      setSelectedSeatId('');
      setSeatForm(hydrateSeatForm({}));
      return;
    }

    setLoadingSeats(true);
    setError('');

    try {
      const response = await listBusSeatMapSeats({ seatMapId: selectedMapId, includeDeleted: true });
      const nextSeats = Array.isArray(response?.items) ? response.items : [];
      setSeats(nextSeats);

      if (nextSeats.length > 0) {
        const selected = nextSeats.find((item) => item.id === selectedSeatId) || nextSeats[0];
        setSelectedSeatId(selected.id);
        setSeatForm(hydrateSeatForm(selected));
      } else {
        setSelectedSeatId('');
        setSeatForm(hydrateSeatForm({}));
      }
    } catch (err) {
      setError(err.message || 'Không tải được danh sách ghế.');
    } finally {
      setLoadingSeats(false);
    }
  };

  useEffect(() => {
    loadMaps();
  }, []);

  useEffect(() => {
    loadSeats();
  }, [selectedMapId]);

  const handleSaveMap = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildMapPayload(mapForm);

      if (selectedMapId) {
        await updateBusSeatMap(selectedMapId, payload);
        setNotice('Đã cập nhật sơ đồ ghế.');
      } else {
        await createBusSeatMap(payload);
        setNotice('Đã tạo sơ đồ ghế mới.');
      }

      await loadMaps();
    } catch (err) {
      setError(err.message || 'Không lưu được sơ đồ ghế.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleMap = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusSeatMap(item.id);
        setNotice('Đã khôi phục sơ đồ ghế.');
      } else {
        await deleteBusSeatMap(item.id);
        setNotice('Đã ẩn sơ đồ ghế.');
      }

      await loadMaps();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái sơ đồ ghế.');
    }
  };

  const handleGenerateSeats = async (event) => {
    event.preventDefault();
    if (!selectedMapId) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      await generateBusSeatMapSeats(selectedMapId, {
        prefix: generateForm.prefix,
        seatType: Number(generateForm.seatType || 1),
        seatClass: 0,
        markWindow: true,
        markAisle: true,
        overwriteExisting: !!generateForm.overwriteExisting,
      });
      setNotice('Đã sinh ghế theo sơ đồ.');
      await loadSeats();
      await loadMaps();
    } catch (err) {
      setError(err.message || 'Không sinh được ghế.');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveSeat = async (event) => {
    event.preventDefault();
    if (!selectedSeatId) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      await updateBusSeat(selectedSeatId, buildSeatPayload(seatForm));
      setNotice('Đã cập nhật ghế.');
      await loadSeats();
    } catch (err) {
      setError(err.message || 'Không lưu được ghế.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleSeat = async (item) => {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreBusSeat(item.id);
        setNotice('Đã khôi phục ghế.');
      } else {
        await deleteBusSeat(item.id);
        setNotice('Đã ẩn ghế.');
      }

      await loadSeats();
      await loadMaps();
    } catch (err) {
      setError(err.message || 'Không cập nhật được trạng thái ghế.');
    }
  };

  return (
    <BusManagementPageShell
      pageKey="seat-maps"
      title="Sơ đồ ghế xe"
      subtitle="Tạo sơ đồ ghế, sinh danh sách ghế và chỉnh thông tin ghế dùng cho xe/chuyến bán vé."
      error={error}
      notice={notice}
      actions={(
        <>
          <button
            type="button"
            onClick={() => {
              loadMaps();
              loadSeats();
            }}
            className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button
            type="button"
            onClick={() => {
              setSelectedMapId('');
              setSelectedSeatId('');
              setSeats([]);
              setMapForm(createEmptyMapForm());
              setSeatForm(hydrateSeatForm({}));
            }}
            className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2"
          >
            <Plus size={16} />
            Thêm sơ đồ
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1fr_0.95fr] gap-8">
        <div className="space-y-8">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-8 py-6 border-b border-slate-100">
              <p className="text-lg font-black text-slate-900">Danh sách sơ đồ</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Một sơ đồ có thể gán cho nhiều xe cùng cấu hình.</p>
            </div>
            <div className="divide-y divide-slate-50">
              {loadingMaps ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải sơ đồ ghế...</div>
              ) : maps.length === 0 ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có sơ đồ ghế nào.</div>
              ) : maps.map((item) => {
                const typeLabel = VEHICLE_TYPES.find((type) => type.value === item.vehicleType)?.label || 'Xe khách';

                return (
                  <div
                    key={item.id}
                    role="button"
                    tabIndex={0}
                    onClick={() => {
                      setSelectedMapId(item.id);
                      setMapForm(hydrateMapForm(item));
                    }}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        setSelectedMapId(item.id);
                        setMapForm(hydrateMapForm(item));
                      }
                    }}
                    className={`w-full px-8 py-6 text-left transition-all hover:bg-slate-50 ${selectedMapId === item.id ? 'bg-slate-50' : ''}`}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex items-start gap-4">
                        <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                          <Armchair size={20} />
                        </div>
                        <div>
                          <div className="flex items-center gap-3 flex-wrap">
                            <p className="font-black text-slate-900">{item.name}</p>
                            {item.isDeleted ? (
                              <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span>
                            ) : null}
                            {!item.isActive ? (
                              <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm ngưng</span>
                            ) : null}
                          </div>
                          <p className="text-xs font-bold text-slate-400 mt-2">
                            {typeLabel} • {item.totalRows} hàng x {item.totalColumns} cột • {item.deckCount} tầng
                          </p>
                          <p className="text-[10px] font-black uppercase tracking-widest text-sky-500 mt-2">
                            {item.seatCount ?? 0} ghế đang khai báo
                          </p>
                        </div>
                      </div>
                      <button
                        type="button"
                        onClick={(event) => {
                          event.stopPropagation();
                          handleToggleMap(item);
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

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-8 py-6 border-b border-slate-100">
              <p className="text-lg font-black text-slate-900">Ghế trong sơ đồ</p>
              <p className="text-xs font-bold text-slate-400 mt-1">{selectedMap?.name || 'Chọn sơ đồ ghế để xem danh sách ghế.'}</p>
            </div>
            <div className="divide-y divide-slate-50 max-h-[520px] overflow-y-auto">
              {loadingSeats ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải ghế...</div>
              ) : seats.length === 0 ? (
                <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có ghế. Hãy dùng nút sinh ghế ở khung bên phải.</div>
              ) : seats.map((item) => (
                <div key={item.id} className={`px-8 py-5 transition-all ${selectedSeatId === item.id ? 'bg-slate-50' : ''}`}>
                  <div className="flex items-center justify-between gap-4">
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedSeatId(item.id);
                        setSeatForm(hydrateSeatForm(item));
                      }}
                      className="min-w-0 flex items-center gap-4 text-left"
                    >
                      <span className="w-10 h-10 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center text-xs font-black">
                        {item.seatNumber}
                      </span>
                      <span className="min-w-0">
                        <span className="block font-black text-slate-900">{item.seatNumber}</span>
                        <span className="block text-xs font-bold text-slate-400 mt-1">
                          Hàng {Number(item.rowIndex) + 1} • Cột {Number(item.columnIndex) + 1} • Tầng {item.deckIndex}
                        </span>
                      </span>
                    </button>
                    <button
                      type="button"
                      onClick={() => handleToggleSeat(item)}
                      className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white"
                    >
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="space-y-8">
          <form onSubmit={handleSaveMap} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-xl font-black text-slate-900">{selectedMapId ? 'Cập nhật sơ đồ ghế' : 'Tạo sơ đồ ghế mới'}</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Số hàng, cột và tầng quyết định cách sinh ghế tự động.</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại xe</span>
                <select value={mapForm.vehicleType} onChange={(event) => setMapForm((current) => ({ ...current, vehicleType: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
                  {VEHICLE_TYPES.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã sơ đồ</span>
                <input value={mapForm.code} onChange={(event) => setMapForm((current) => ({ ...current, code: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="BUS-40S" required />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tên sơ đồ</span>
              <input value={mapForm.name} onChange={(event) => setMapForm((current) => ({ ...current, name: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="Xe giường nằm 40 chỗ" required />
            </label>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số hàng</span>
                <input type="number" min="1" value={mapForm.totalRows} onChange={(event) => setMapForm((current) => ({ ...current, totalRows: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số cột</span>
                <input type="number" min="1" value={mapForm.totalColumns} onChange={(event) => setMapForm((current) => ({ ...current, totalColumns: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Số tầng</span>
                <input type="number" min="1" value={mapForm.deckCount} onChange={(event) => setMapForm((current) => ({ ...current, deckCount: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" required />
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Ghi chú cách đánh số ghế</span>
              <input value={mapForm.seatLabelScheme} onChange={(event) => setMapForm((current) => ({ ...current, seatLabelScheme: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="Ví dụ: A01-A20 tầng dưới, B01-B20 tầng trên" />
            </label>

            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={mapForm.isActive} onChange={(event) => setMapForm((current) => ({ ...current, isActive: event.target.checked }))} className="h-4 w-4 rounded border-slate-300" />
              Cho phép dùng sơ đồ này
            </label>

            <button type="submit" disabled={saving} className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70">
              {saving ? 'Đang lưu...' : selectedMapId ? 'Lưu sơ đồ' : 'Tạo sơ đồ'}
            </button>
          </form>

          <form onSubmit={handleGenerateSeats} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-xl font-black text-slate-900">Sinh ghế tự động</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Dùng sau khi đã lưu số hàng, số cột và số tầng.</p>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tiền tố ghế</span>
                <input value={generateForm.prefix} onChange={(event) => setGenerateForm((current) => ({ ...current, prefix: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="A" />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại ghế mặc định</span>
                <select value={generateForm.seatType} onChange={(event) => setGenerateForm((current) => ({ ...current, seatType: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
                  {SEAT_TYPES.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
            </div>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={generateForm.overwriteExisting} onChange={(event) => setGenerateForm((current) => ({ ...current, overwriteExisting: event.target.checked }))} className="h-4 w-4 rounded border-slate-300" />
              Xóa mềm ghế cũ và sinh lại
            </label>
            <button type="submit" disabled={saving || !selectedMapId} className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70">
              Sinh ghế
            </button>
          </form>

          <form onSubmit={handleSaveSeat} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
            <div>
              <p className="text-xl font-black text-slate-900">Chỉnh ghế</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Chọn một ghế ở danh sách bên trái để chỉnh mã ghế, vị trí và phụ thu.</p>
            </div>
            <label className="space-y-2 block">
              <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Mã ghế</span>
              <input value={seatForm.seatNumber} onChange={(event) => setSeatForm((current) => ({ ...current, seatNumber: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" placeholder="A01" disabled={!selectedSeatId} required />
            </label>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hàng</span>
                <input type="number" min="0" value={seatForm.rowIndex} onChange={(event) => setSeatForm((current) => ({ ...current, rowIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" disabled={!selectedSeatId} />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Cột</span>
                <input type="number" min="0" value={seatForm.columnIndex} onChange={(event) => setSeatForm((current) => ({ ...current, columnIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" disabled={!selectedSeatId} />
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tầng</span>
                <input type="number" min="1" value={seatForm.deckIndex} onChange={(event) => setSeatForm((current) => ({ ...current, deckIndex: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" disabled={!selectedSeatId} />
              </label>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Loại ghế</span>
                <select value={seatForm.seatType} onChange={(event) => setSeatForm((current) => ({ ...current, seatType: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" disabled={!selectedSeatId}>
                  {SEAT_TYPES.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phụ thu ghế</span>
                <input type="number" value={seatForm.priceModifier} onChange={(event) => setSeatForm((current) => ({ ...current, priceModifier: event.target.value }))} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none" disabled={!selectedSeatId} />
              </label>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
                <input type="checkbox" checked={seatForm.isWindow} onChange={(event) => setSeatForm((current) => ({ ...current, isWindow: event.target.checked }))} className="h-4 w-4 rounded border-slate-300" disabled={!selectedSeatId} />
                Ghế cạnh cửa sổ
              </label>
              <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
                <input type="checkbox" checked={seatForm.isAisle} onChange={(event) => setSeatForm((current) => ({ ...current, isAisle: event.target.checked }))} className="h-4 w-4 rounded border-slate-300" disabled={!selectedSeatId} />
                Ghế cạnh lối đi
              </label>
              <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
                <input type="checkbox" checked={seatForm.isActive} onChange={(event) => setSeatForm((current) => ({ ...current, isActive: event.target.checked }))} className="h-4 w-4 rounded border-slate-300" disabled={!selectedSeatId} />
                Đang dùng
              </label>
            </div>
            <button type="submit" disabled={saving || !selectedSeatId} className="w-full rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-widest text-white disabled:opacity-70">
              Lưu ghế
            </button>
          </form>
        </div>
      </div>
    </BusManagementPageShell>
  );
};

export default BusSeatMapsPage;
