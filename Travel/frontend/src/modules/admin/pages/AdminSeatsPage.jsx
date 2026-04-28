import React, { useEffect, useMemo, useState } from 'react';
import { RefreshCw, RotateCcw, ScanLine, Search, Trash2 } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  bulkUpdateSeats,
  deleteSeat,
  listSeatMaps,
  listSeats,
  restoreSeat,
  updateSeat,
} from '../../../services/masterDataService';
import {
  SEAT_CLASS_OPTIONS,
  SEAT_TYPE_OPTIONS,
  formatDateTime,
  getActiveBadgeClass,
  getActiveBadgeLabel,
  getSeatClassLabel,
  getSeatTypeLabel,
} from '../master-data/utils/options';

function buildBulkForm() {
  return {
    seatType: '',
    seatClass: '',
    isActive: '',
    priceModifier: '',
    setPriceModifier: false,
  };
}

function mapSeatToForm(item) {
  return {
    seatNumber: item.seatNumber || '',
    rowIndex: item.rowIndex ?? 0,
    columnIndex: item.columnIndex ?? 0,
    deckIndex: item.deckIndex ?? 1,
    seatType: Number(item.seatType || 1),
    seatClass: Number(item.seatClass || 0),
    isAisle: !!item.isAisle,
    isWindow: !!item.isWindow,
    priceModifier: item.priceModifier ?? '',
    isActive: item.isActive ?? true,
  };
}

const AdminSeatsPage = () => {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminMasterDataScope();
  const [seatMaps, setSeatMaps] = useState([]);
  const [selectedSeatMapId, setSelectedSeatMapId] = useState('');
  const [items, setItems] = useState([]);
  const [selectedSeatId, setSelectedSeatId] = useState('');
  const [selectedSeatIds, setSelectedSeatIds] = useState([]);
  const [seatForm, setSeatForm] = useState(null);
  const [bulkForm, setBulkForm] = useState(buildBulkForm());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [bulkSaving, setBulkSaving] = useState(false);
  const [includeDeleted, setIncludeDeleted] = useState(true);
  const [search, setSearch] = useState('');
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');

  const filteredItems = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    if (!keyword) {
      return items;
    }

    return items.filter((item) => String(item.seatNumber || '').toLowerCase().includes(keyword));
  }, [items, search]);

  const selectedSeat = useMemo(
    () => items.find((item) => item.id === selectedSeatId) || null,
    [items, selectedSeatId],
  );

  const loadSeatMapsRef = useLatestRef(loadSeatMaps);
  const loadSeatsRef = useLatestRef(loadSeats);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadSeatMapsRef.current();
  }, [loadSeatMapsRef, tenantId]);

  useEffect(() => {
    if (!tenantId || !selectedSeatMapId) {
      setItems([]);
      setSelectedSeatId('');
      setSelectedSeatIds([]);
      setSeatForm(null);
      return;
    }

    loadSeatsRef.current();
  }, [tenantId, selectedSeatMapId, includeDeleted, loadSeatsRef]);

  async function loadSeatMaps() {
    if (!tenantId) {
      return;
    }

    try {
      const response = await listSeatMaps({ includeDeleted: false }, tenantId);
      const nextSeatMaps = response.items || [];
      setSeatMaps(nextSeatMaps);
      if (!selectedSeatMapId && nextSeatMaps.length > 0) {
        setSelectedSeatMapId(nextSeatMaps[0].id);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách sơ đồ ghế.');
      setSeatMaps([]);
      setSelectedSeatMapId('');
    }
  }

  async function loadSeats() {
    if (!tenantId || !selectedSeatMapId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listSeats({
        seatMapId: selectedSeatMapId,
        includeDeleted,
      }, tenantId);

      setItems(response.items || []);
      setSelectedSeatIds([]);
      if (selectedSeatId) {
        const nextSeat = (response.items || []).find((item) => item.id === selectedSeatId);
        setSeatForm(nextSeat ? mapSeatToForm(nextSeat) : null);
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách ghế.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  function toggleSeatSelection(id) {
    setSelectedSeatIds((current) => (
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id]
    ));
  }

  function selectSeat(item) {
    setSelectedSeatId(item.id);
    setSeatForm(mapSeatToForm(item));
    setNotice('');
    setError('');
  }

  async function handleSaveSeat() {
    if (!tenantId || !selectedSeatId || !seatForm) {
      return;
    }

    setSaving(true);
    setNotice('');
    setError('');

    try {
      await updateSeat(selectedSeatId, {
        seatNumber: seatForm.seatNumber.trim(),
        rowIndex: Number(seatForm.rowIndex || 0),
        columnIndex: Number(seatForm.columnIndex || 0),
        deckIndex: Number(seatForm.deckIndex || 1),
        seatType: Number(seatForm.seatType),
        seatClass: Number(seatForm.seatClass),
        isAisle: seatForm.isAisle,
        isWindow: seatForm.isWindow,
        priceModifier: seatForm.priceModifier === '' ? null : Number(seatForm.priceModifier),
        isActive: seatForm.isActive,
      }, tenantId);

      setNotice('Ghế đã được cập nhật.');
      await loadSeatsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật ghế.');
    } finally {
      setSaving(false);
    }
  }

  async function handleBulkUpdate() {
    if (!tenantId || !selectedSeatMapId || selectedSeatIds.length === 0) {
      return;
    }

    setBulkSaving(true);
    setNotice('');
    setError('');

    try {
      await bulkUpdateSeats({
        seatMapId: selectedSeatMapId,
        seatIds: selectedSeatIds,
        seatType: bulkForm.seatType === '' ? null : Number(bulkForm.seatType),
        seatClass: bulkForm.seatClass === '' ? null : Number(bulkForm.seatClass),
        isActive: bulkForm.isActive === '' ? null : bulkForm.isActive === 'true',
        priceModifier: bulkForm.priceModifier === '' ? null : Number(bulkForm.priceModifier),
        setPriceModifier: bulkForm.setPriceModifier,
      }, tenantId);

      setNotice(`Đã cập nhật ${selectedSeatIds.length} ghế.`);
      setBulkForm(buildBulkForm());
      await loadSeatsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật hàng loạt ghế.');
    } finally {
      setBulkSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    if (!tenantId) {
      return;
    }

    setNotice('');
    setError('');

    try {
      if (item.isDeleted) {
        await restoreSeat(item.id, tenantId);
        setNotice('Ghế đã được khôi phục.');
      } else {
        await deleteSeat(item.id, tenantId);
        setNotice('Ghế đã được chuyển vào thùng rác.');
      }

      await loadSeatsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật ghế.');
    }
  }

  return (
    <MasterDataPageShell
      pageKey="seats"
      title="Ghế"
      subtitle="Chỉnh sửa chi tiết lưới ghế và cập nhật hàng loạt theo sơ đồ ghế đã tạo."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <button onClick={loadSeats} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="space-y-4">
        <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
          <select value={selectedSeatMapId} onChange={(event) => setSelectedSeatMapId(event.target.value)} className="bg-slate-50 rounded-xl border border-slate-100 px-4 py-3 text-sm font-bold text-slate-700 outline-none min-w-64">
            <option value="">Chọn sơ đồ ghế</option>
            {seatMaps.map((item) => (
              <option key={item.id} value={item.id}>{item.name} | {item.seatCount || 0} ghế</option>
            ))}
          </select>
          <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
            <Search size={15} className="text-slate-400" />
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm số ghế..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
          </div>
          <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest ${includeDeleted ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
            {includeDeleted ? 'Đang xem đã xóa' : 'Ẩn đã xóa'}
          </button>
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-[1.2fr,0.8fr] gap-6">
          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 py-4 border-b border-slate-100 bg-slate-50/70 text-[10px] font-black uppercase tracking-widest text-slate-400">
              <div className="col-span-1"></div>
              <div className="col-span-3">Ghế</div>
              <div className="col-span-2">Loại</div>
              <div className="col-span-2">Hạng</div>
              <div className="col-span-2">Vị trí</div>
              <div className="col-span-2">Thao tác</div>
            </div>
            <div className="divide-y divide-slate-50 max-h-[760px] overflow-y-auto">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải ghế...</div>
              ) : !selectedSeatMapId ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chọn sơ đồ ghế để xem danh sách ghế.</div>
              ) : filteredItems.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có ghế nào cho sơ đồ này.</div>
              ) : filteredItems.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-1 md:col-span-1">
                    <input type="checkbox" checked={selectedSeatIds.includes(item.id)} onChange={() => toggleSeatSelection(item.id)} className="w-4 h-4 rounded border-slate-300" />
                  </div>
                  <div className="col-span-1 md:col-span-3">
                    <button onClick={() => selectSeat(item)} className="text-left">
                      <p className="font-black text-slate-900">{item.seatNumber}</p>
                      <p className="text-xs font-bold text-slate-400 mt-1">{formatDateTime(item.updatedAt || item.createdAt)}</p>
                    </button>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getSeatTypeLabel(item.seatType)}</div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getSeatClassLabel(item.seatClass)}</div>
                  <div className="col-span-1 md:col-span-2 text-xs font-medium text-slate-500">
                    Hàng {item.rowIndex} | Cột {item.columnIndex} | Tầng {item.deckIndex}
                  </div>
                  <div className="col-span-2 md:col-span-2 flex gap-2">
                    <button onClick={() => handleToggleDelete(item)} className={`p-2 rounded-xl transition-all ${item.isDeleted ? 'text-emerald-600 hover:bg-emerald-50' : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'}`} title={item.isDeleted ? 'Khôi phục' : 'Xóa mềm'}>
                      {item.isDeleted ? <RotateCcw size={14} /> : <Trash2 size={14} />}
                    </button>
                    <span className={`px-3 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest ${getActiveBadgeClass(item)}`}>
                      {getActiveBadgeLabel(item)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="space-y-6">
            <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
              <div className="flex items-center gap-3">
                <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
                  <ScanLine size={18} />
                </div>
                <div>
                  <h3 className="text-lg font-black text-slate-900">Chỉnh sửa ghế đã chọn</h3>
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Chọn 1 ghế ở bên trái</p>
                </div>
              </div>
              {!selectedSeat || !seatForm ? (
                <div className="text-sm font-bold text-slate-400">Chưa chọn ghế nào.</div>
              ) : (
                <>
                  <input value={seatForm.seatNumber} onChange={(event) => setSeatForm((current) => ({ ...current, seatNumber: event.target.value }))} placeholder="Số ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
                  <div className="grid grid-cols-3 gap-4">
                    <input value={seatForm.rowIndex} onChange={(event) => setSeatForm((current) => ({ ...current, rowIndex: event.target.value }))} placeholder="Hàng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <input value={seatForm.columnIndex} onChange={(event) => setSeatForm((current) => ({ ...current, columnIndex: event.target.value }))} placeholder="Cột" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <input value={seatForm.deckIndex} onChange={(event) => setSeatForm((current) => ({ ...current, deckIndex: event.target.value }))} placeholder="Tầng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <select value={seatForm.seatType} onChange={(event) => setSeatForm((current) => ({ ...current, seatType: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                      {SEAT_TYPE_OPTIONS.map((item) => (
                        <option key={item.value} value={item.value}>{item.label}</option>
                      ))}
                    </select>
                    <select value={seatForm.seatClass} onChange={(event) => setSeatForm((current) => ({ ...current, seatClass: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                      {SEAT_CLASS_OPTIONS.map((item) => (
                        <option key={item.value} value={item.value}>{item.label}</option>
                      ))}
                    </select>
                  </div>
                  <input value={seatForm.priceModifier} onChange={(event) => setSeatForm((current) => ({ ...current, priceModifier: event.target.value }))} placeholder="Điều chỉnh giá" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                  <div className="flex flex-wrap gap-2">
                    {[
                      ['isAisle', 'Lối đi'],
                      ['isWindow', 'Cửa sổ'],
                      ['isActive', 'Hoạt động'],
                    ].map(([key, label]) => (
                      <button
                        key={key}
                        onClick={() => setSeatForm((current) => ({ ...current, [key]: !current[key] }))}
                        className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${seatForm[key] ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                  <button onClick={handleSaveSeat} disabled={saving} className="w-full px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-60">
                    {saving ? 'Đang lưu...' : 'Cập nhật ghế'}
                  </button>
                </>
              )}
            </div>

            <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
              <h3 className="text-lg font-black text-slate-900">Cập nhật hàng loạt</h3>
              <p className="text-sm font-medium text-slate-500">Đang chọn {selectedSeatIds.length} ghế.</p>
              <div className="grid grid-cols-2 gap-4">
                <select value={bulkForm.seatType} onChange={(event) => setBulkForm((current) => ({ ...current, seatType: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                  <option value="">Không đổi loại ghế</option>
                  {SEAT_TYPE_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
                <select value={bulkForm.seatClass} onChange={(event) => setBulkForm((current) => ({ ...current, seatClass: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                  <option value="">Không đổi hạng ghế</option>
                  {SEAT_CLASS_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <select value={bulkForm.isActive} onChange={(event) => setBulkForm((current) => ({ ...current, isActive: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                  <option value="">Không đổi trạng thái</option>
                  <option value="true">Bật hoạt động</option>
                  <option value="false">Tạm dừng</option>
                </select>
                <input value={bulkForm.priceModifier} onChange={(event) => setBulkForm((current) => ({ ...current, priceModifier: event.target.value }))} placeholder="Điều chỉnh giá" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
              </div>
              <button onClick={() => setBulkForm((current) => ({ ...current, setPriceModifier: !current.setPriceModifier }))} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${bulkForm.setPriceModifier ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
                {bulkForm.setPriceModifier ? 'Đang đặt điều chỉnh giá' : 'Bỏ qua điều chỉnh giá'}
              </button>
              <button onClick={handleBulkUpdate} disabled={bulkSaving || selectedSeatIds.length === 0} className="w-full px-5 py-3 rounded-2xl bg-amber-500 text-white text-sm font-bold hover:bg-amber-600 transition-all shadow-lg disabled:opacity-60">
                {bulkSaving ? 'Đang cập nhật...' : 'Cập nhật các ghế đã chọn'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </MasterDataPageShell>
  );
};

export default AdminSeatsPage;
