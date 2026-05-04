import React, { useEffect, useMemo, useState } from 'react';
import { Edit2, LocateFixed, Plus, RefreshCw, RotateCcw, Search, Trash2, Wand2 } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  createSeatMap,
  deleteSeatMap,
  generateSeatMapSeats,
  getSeatMap,
  listSeatMaps,
  restoreSeatMap,
  updateSeatMap,
} from '../../../services/masterDataService';
import {
  SEAT_CLASS_OPTIONS,
  SEAT_TYPE_OPTIONS,
  VEHICLE_TYPE_OPTIONS,
  formatDate,
  getActiveBadgeClass,
  getActiveBadgeLabel,
  getVehicleTypeLabel,
} from '../master-data/utils/options';

function buildEmptyForm() {
  return {
    vehicleType: 1,
    code: '',
    name: '',
    totalRows: 10,
    totalColumns: 4,
    deckCount: 1,
    layoutVersion: 'v1',
    seatLabelScheme: '',
    isActive: true,
  };
}

function buildGenerateForm() {
  return {
    prefix: '',
    seatType: 1,
    seatClass: 0,
    markWindow: true,
    markAisle: true,
    overwriteExisting: false,
  };
}

function mapSeatMapToForm(item) {
  return {
    vehicleType: Number(item.vehicleType || 1),
    code: item.code || '',
    name: item.name || '',
    totalRows: item.totalRows || 1,
    totalColumns: item.totalColumns || 1,
    deckCount: item.deckCount || 1,
    layoutVersion: item.layoutVersion || 'v1',
    seatLabelScheme: item.seatLabelScheme || '',
    isActive: item.isActive ?? true,
  };
}

const AdminSeatMapsPage = ({ scope = 'admin' }) => {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
    showTenantSelector,
    scopeHint,
  } = useAdminMasterDataScope({ scope });
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [includeDeleted, setIncludeDeleted] = useState(true);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(buildEmptyForm());
  const [generateForm, setGenerateForm] = useState(buildGenerateForm());
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');

  const selectedItem = useMemo(
    () => items.find((item) => item.id === selectedId) || null,
    [items, selectedId],
  );

  const loadItemsRef = useLatestRef(loadItems);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadItemsRef.current();
  }, [tenantId, search, typeFilter, includeDeleted, loadItemsRef]);

  async function loadItems() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listSeatMaps({
        q: search || undefined,
        vehicleType: typeFilter === 'all' ? undefined : typeFilter,
        includeDeleted,
      }, tenantId, scope);

      setItems(response.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách sơ đồ ghế.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  function resetForm() {
    setSelectedId('');
    setForm(buildEmptyForm());
    setGenerateForm(buildGenerateForm());
    setNotice('');
  }

  async function selectItem(item) {
    setSelectedId(item.id);
    setNotice('');
    setError('');

    try {
      const detail = await getSeatMap(item.id, { includeDeleted }, tenantId, scope);
      setForm(mapSeatMapToForm(detail.seatMap || item));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết sơ đồ ghế.');
      setForm(mapSeatMapToForm(item));
    }
  }

  async function handleSave() {
    if (!tenantId) {
      return;
    }

    setSaving(true);
    setNotice('');
    setError('');

    const payload = {
      vehicleType: Number(form.vehicleType),
      code: form.code.trim(),
      name: form.name.trim(),
      totalRows: Number(form.totalRows || 1),
      totalColumns: Number(form.totalColumns || 1),
      deckCount: Number(form.deckCount || 1),
      layoutVersion: form.layoutVersion.trim() || null,
      seatLabelScheme: form.seatLabelScheme.trim() || null,
      isActive: form.isActive,
    };

    try {
      if (selectedId) {
        await updateSeatMap(selectedId, payload, tenantId, scope);
        setNotice('Sơ đồ ghế đã được cập nhật.');
      } else {
        await createSeatMap(payload, tenantId, scope);
        setNotice('Sơ đồ ghế mới đã được tạo.');
      }

      resetForm();
      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu sơ đồ ghế.');
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerateSeats() {
    if (!tenantId || !selectedId) {
      return;
    }

    setGenerating(true);
    setNotice('');
    setError('');

    try {
      const response = await generateSeatMapSeats(selectedId, {
        prefix: generateForm.prefix,
        seatType: Number(generateForm.seatType),
        seatClass: Number(generateForm.seatClass),
        markWindow: generateForm.markWindow,
        markAisle: generateForm.markAisle,
        overwriteExisting: generateForm.overwriteExisting,
      }, tenantId, scope);

      setNotice(`Đã tạo ${response.created || 0} ghế cho sơ đồ ghế.`);
      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể sinh ghế.');
    } finally {
      setGenerating(false);
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
        await restoreSeatMap(item.id, tenantId, scope);
        setNotice('Sơ đồ ghế đã được khôi phục.');
      } else {
        await deleteSeatMap(item.id, tenantId, scope);
        setNotice('Sơ đồ ghế đã được chuyển vào thùng rác.');
      }

      if (selectedId === item.id) {
        resetForm();
      }

      await loadItemsRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật sơ đồ ghế.');
    }
  }

  return (
    <MasterDataPageShell
      pageKey="seat-maps"
      title="Sơ đồ ghế"
      subtitle="Quản lý layout ghế theo loại phương tiện và sinh nhanh lưới ghế cho các module booking."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      showTenantSelector={showTenantSelector}
      scopeHint={scopeHint}
      navScope={scope}
      error={scopeError || error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <button onClick={loadItems} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
            <RefreshCw size={14} /> Tải lại
          </button>
          <button onClick={resetForm} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} /> Tạo sơ đồ ghế
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 lg:grid-cols-[0.85fr,1.15fr] gap-6">
        <div className="space-y-6">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
                <LocateFixed size={18} />
              </div>
              <div>
                <h3 className="text-lg font-black text-slate-900">{selectedItem ? 'Cập nhật sơ đồ ghế' : 'Tạo sơ đồ ghế mới'}</h3>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Khung layout</p>
              </div>
            </div>

            <select value={form.vehicleType} onChange={(event) => setForm((current) => ({ ...current, vehicleType: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
              {VEHICLE_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <div className="grid grid-cols-2 gap-4">
              <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} placeholder="Mã" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
              <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên sơ đồ ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            </div>
            <div className="grid grid-cols-3 gap-4">
              <input value={form.totalRows} onChange={(event) => setForm((current) => ({ ...current, totalRows: event.target.value }))} placeholder="Số hàng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
              <input value={form.totalColumns} onChange={(event) => setForm((current) => ({ ...current, totalColumns: event.target.value }))} placeholder="Số cột" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
              <input value={form.deckCount} onChange={(event) => setForm((current) => ({ ...current, deckCount: event.target.value }))} placeholder="Số tầng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <input value={form.layoutVersion} onChange={(event) => setForm((current) => ({ ...current, layoutVersion: event.target.value }))} placeholder="Phiên bản layout" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
              <input value={form.seatLabelScheme} onChange={(event) => setForm((current) => ({ ...current, seatLabelScheme: event.target.value }))} placeholder="Quy tắc đặt tên ghế" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            </div>
            <button onClick={() => setForm((current) => ({ ...current, isActive: !current.isActive }))} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${form.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
              {form.isActive ? 'Đang hoạt động' : 'Đang tạm dừng'}
            </button>
            <div className="flex flex-wrap gap-3 pt-2">
              <button onClick={resetForm} className="px-5 py-3 rounded-2xl bg-white text-slate-500 border border-slate-100 text-sm font-bold hover:text-slate-700 transition-all">
                Làm mới biểu mẫu
              </button>
              <button onClick={handleSave} disabled={saving || !tenantId} className="px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-60">
                {saving ? 'Đang lưu...' : selectedItem ? 'Cập nhật sơ đồ ghế' : 'Tạo sơ đồ ghế'}
              </button>
            </div>
          </div>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-11 h-11 rounded-2xl bg-amber-500 text-white flex items-center justify-center">
                <Wand2 size={18} />
              </div>
              <div>
                <h3 className="text-lg font-black text-slate-900">Sinh ghế</h3>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Cần chọn sơ đồ ghế ở bên trên trước khi sinh</p>
              </div>
            </div>
            <input value={generateForm.prefix} onChange={(event) => setGenerateForm((current) => ({ ...current, prefix: event.target.value }))} placeholder="Tiền tố, ví dụ: A" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <div className="grid grid-cols-2 gap-4">
              <select value={generateForm.seatType} onChange={(event) => setGenerateForm((current) => ({ ...current, seatType: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                {SEAT_TYPE_OPTIONS.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
              <select value={generateForm.seatClass} onChange={(event) => setGenerateForm((current) => ({ ...current, seatClass: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
                {SEAT_CLASS_OPTIONS.map((item) => (
                  <option key={item.value} value={item.value}>{item.label}</option>
                ))}
              </select>
            </div>
            <div className="flex flex-wrap gap-2">
              {[
                ['markWindow', 'Đánh dấu cửa sổ'],
                ['markAisle', 'Đánh dấu lối đi'],
                ['overwriteExisting', 'Ghi đè ghế cũ'],
              ].map(([key, label]) => (
                <button
                  key={key}
                  onClick={() => setGenerateForm((current) => ({ ...current, [key]: !current[key] }))}
                  className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${generateForm[key] ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}
                >
                  {label}
                </button>
              ))}
            </div>
            <button onClick={handleGenerateSeats} disabled={!selectedId || generating || !tenantId} className="w-full px-5 py-3 rounded-2xl bg-amber-500 text-white text-sm font-bold hover:bg-amber-600 transition-all shadow-lg disabled:opacity-60">
              {generating ? 'Đang sinh ghế...' : 'Sinh ghế cho sơ đồ đang chọn'}
            </button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tìm mã, tên sơ đồ ghế..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
            </div>
            <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)} className="bg-slate-50 rounded-xl border border-slate-100 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="all">Tất cả loại phương tiện</option>
              {VEHICLE_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest ${includeDeleted ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
              {includeDeleted ? 'Đang xem đã xóa' : 'Ẩn đã xóa'}
            </button>
          </div>

          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 py-4 border-b border-slate-100 bg-slate-50/70 text-[10px] font-black uppercase tracking-widest text-slate-400">
              <div className="col-span-4">Sơ đồ ghế</div>
              <div className="col-span-2">Loại phương tiện</div>
              <div className="col-span-2">Lưới</div>
              <div className="col-span-2">Số ghế</div>
              <div className="col-span-2">Thao tác</div>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải sơ đồ ghế...</div>
              ) : items.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có sơ đồ ghế nào.</div>
              ) : items.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-2 md:col-span-4">
                    <p className="font-black text-slate-900">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.code} | {formatDate(item.createdAt)}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{getVehicleTypeLabel(item.vehicleType)}</div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{item.totalRows} x {item.totalColumns} | {item.deckCount} tầng</div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">{item.seatCount || 0} ghế</div>
                  <div className="col-span-2 md:col-span-2 flex gap-2">
                    <button onClick={() => selectItem(item)} className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all" title="Sửa">
                      <Edit2 size={14} />
                    </button>
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
        </div>
      </div>
    </MasterDataPageShell>
  );
};

export default AdminSeatMapsPage;
