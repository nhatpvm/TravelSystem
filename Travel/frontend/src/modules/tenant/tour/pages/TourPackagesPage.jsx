import React, { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Boxes, Loader2, Plus, Save } from 'lucide-react';
import TourManagementShell from '../components/TourManagementShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createManagerTourPackage,
  listManagerTourPackages,
  listManagerTours,
  toggleManagerTourPackageAction,
  updateManagerTourPackage,
} from '../../../../services/tourService';
import {
  formatDateTime,
  getHoldStrategyLabel,
  getPackageModeLabel,
  getPackageStatusLabel,
} from '../../../tours/utils/presentation';
import {
  HOLD_STRATEGY_OPTIONS,
  PACKAGE_MODE_OPTIONS,
  PACKAGE_STATUS_OPTIONS,
  toNullableText,
  toNumberOrDefault,
  updateSearchParams,
} from '../utils/options';

const EMPTY_FORM = {
  code: '',
  name: '',
  mode: 1,
  status: 0,
  currencyCode: 'VND',
  isDefault: false,
  autoRepriceBeforeConfirm: true,
  holdStrategy: 2,
  pricingRuleJson: '',
  metadataJson: '',
  isActive: true,
  rowVersionBase64: '',
};

function buildFormFromPackage(item) {
  if (!item) {
    return EMPTY_FORM;
  }

  return {
    code: item.code || '',
    name: item.name || '',
    mode: Number(item.mode ?? 1),
    status: Number(item.status ?? 0),
    currencyCode: item.currencyCode || 'VND',
    isDefault: Boolean(item.isDefault),
    autoRepriceBeforeConfirm: Boolean(item.autoRepriceBeforeConfirm),
    holdStrategy: Number(item.holdStrategy ?? 2),
    pricingRuleJson: item.pricingRuleJson || '',
    metadataJson: item.metadataJson || '',
    isActive: Boolean(item.isActive),
    rowVersionBase64: item.rowVersionBase64 || '',
  };
}

export default function TourPackagesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tours, setTours] = useState([]);
  const [packages, setPackages] = useState([]);
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const selectedTourId = searchParams.get('tourId') || '';
  const selectedPackageId = searchParams.get('packageId') || '';

  const loadToursRef = useLatestRef(loadTours);
  const loadPackagesRef = useLatestRef(loadPackages);

  useEffect(() => {
    loadToursRef.current();
  }, [loadToursRef]);

  useEffect(() => {
    if (selectedTourId) {
      loadPackagesRef.current(selectedTourId);
    } else {
      setPackages([]);
      setSelectedPackage(null);
      setForm(EMPTY_FORM);
    }
  }, [loadPackagesRef, selectedTourId]);

  useEffect(() => {
    const selected = packages.find((item) => item.id === selectedPackageId) || null;
    setSelectedPackage(selected);
    setForm(buildFormFromPackage(selected));
  }, [packages, selectedPackageId]);

  const selectedTour = useMemo(
    () => tours.find((item) => item.id === selectedTourId) || null,
    [selectedTourId, tours],
  );

  async function loadTours() {
    try {
      const response = await listManagerTours({ page: 1, pageSize: 100, includeDeleted: true });
      const items = response.items || [];
      setTours(items);

      if (!selectedTourId && items.length) {
        updateSearchParams(setSearchParams, { tourId: items[0].id, packageId: '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách tour.');
    }
  }

  async function loadPackages(tourId) {
    setLoading(true);
    setError('');

    try {
      const response = await listManagerTourPackages(tourId, { page: 1, pageSize: 100, includeDeleted: true });
      const items = response.items || [];
      setPackages(items);

      if (!selectedPackageId && items.length) {
        updateSearchParams(setSearchParams, { packageId: items[0].id });
      }

      if (selectedPackageId && !items.some((item) => item.id === selectedPackageId)) {
        updateSearchParams(setSearchParams, { packageId: items[0]?.id || '' });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải gói tour.');
      setPackages([]);
    } finally {
      setLoading(false);
    }
  }

  function handleFieldChange(event) {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (!selectedTourId) {
      setError('Vui lòng chọn tour trước khi lưu gói tour.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    const payload = {
      code: form.code.trim(),
      name: form.name.trim(),
      mode: toNumberOrDefault(form.mode, 1),
      status: toNumberOrDefault(form.status, 0),
      currencyCode: (form.currencyCode || 'VND').trim() || 'VND',
      isDefault: Boolean(form.isDefault),
      autoRepriceBeforeConfirm: Boolean(form.autoRepriceBeforeConfirm),
      holdStrategy: toNumberOrDefault(form.holdStrategy, 2),
      pricingRuleJson: toNullableText(form.pricingRuleJson),
      metadataJson: toNullableText(form.metadataJson),
      isActive: Boolean(form.isActive),
      rowVersionBase64: form.rowVersionBase64 || undefined,
    };

    try {
      if (selectedPackage?.id) {
        await updateManagerTourPackage(selectedTourId, selectedPackage.id, payload);
        setNotice('Đã cập nhật gói tour.');
      } else {
        const created = await createManagerTourPackage(selectedTourId, payload);
        setNotice('Đã tạo gói tour mới.');
        updateSearchParams(setSearchParams, { packageId: created.id });
      }

      await loadPackagesRef.current(selectedTourId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu gói tour.');
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(item, action) {
    setError('');
    setNotice('');

    try {
      await toggleManagerTourPackageAction(selectedTourId, item.id, action);
      setNotice('Đã cập nhật trạng thái gói tour.');
      await loadPackagesRef.current(selectedTourId);
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật gói tour.');
    }
  }

  return (
    <TourManagementShell
      pageKey="packages"
      title="Gói tour"
      subtitle="Quản lý gói bán, chiến lược giữ chỗ và trạng thái thương mại của từng gói tour."
      error={error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <select value={selectedTourId} onChange={(event) => updateSearchParams(setSearchParams, { tourId: event.target.value, packageId: '' })} className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 outline-none">
            {tours.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
          <button type="button" onClick={() => updateSearchParams(setSearchParams, { packageId: '' })} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm">
            <Plus size={14} />
            Tạo gói mới
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1.05fr,0.95fr] gap-6">
        <div className="space-y-4">
          {loading ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400 flex items-center gap-3">
              <Loader2 size={16} className="animate-spin" />
              Đang tải gói tour...
            </div>
          ) : packages.length === 0 ? (
            <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm px-6 py-10 text-sm font-bold text-slate-400">
              Tour này chưa có gói tour nào.
            </div>
          ) : packages.map((item) => (
            <div key={item.id} className={`bg-white rounded-[2rem] border shadow-sm ${selectedPackageId === item.id ? 'border-blue-200' : 'border-slate-100'}`}>
              <button type="button" onClick={() => updateSearchParams(setSearchParams, { packageId: item.id })} className="w-full text-left p-6">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                        {getPackageStatusLabel(item.status)}
                      </span>
                      {item.isDefault && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-emerald-100 text-emerald-700">
                          Mặc định
                        </span>
                      )}
                      {item.isDeleted && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">
                          Đã xóa mềm
                        </span>
                      )}
                    </div>
                    <p className="text-xl font-black text-slate-900 mt-3">{item.name}</p>
                    <p className="text-xs font-black uppercase tracking-widest text-slate-400 mt-1">{item.code}</p>
                    <div className="flex flex-wrap items-center gap-4 mt-4 text-sm font-medium text-slate-500">
                      <span>{getPackageModeLabel(item.mode)}</span>
                      <span>{getHoldStrategyLabel(item.holdStrategy)}</span>
                      <span>{item.componentCount} thành phần</span>
                    </div>
                  </div>
                  <div className="w-12 h-12 rounded-2xl bg-slate-100 text-slate-700 flex items-center justify-center">
                    <Boxes size={18} />
                  </div>
                </div>
              </button>
              <div className="px-6 pb-6 flex flex-wrap items-center gap-2">
                {item.isDeleted ? (
                  <button type="button" onClick={() => handleAction(item, 'restore')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest">
                    Khôi phục
                  </button>
                ) : (
                  <button type="button" onClick={() => handleAction(item, 'delete')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest">
                    Xóa mềm
                  </button>
                )}
                <button type="button" onClick={() => handleAction(item, item.isActive ? 'deactivate' : 'activate')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest">
                  {item.isActive ? 'Tạm khóa' : 'Kích hoạt'}
                </button>
                <button type="button" onClick={() => handleAction(item, item.isDefault ? 'unmark-default' : 'mark-default')} className="px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest">
                  {item.isDefault ? 'Bỏ mặc định' : 'Đặt mặc định'}
                </button>
              </div>
            </div>
          ))}
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              {selectedPackage ? 'Cập nhật gói tour' : 'Tạo gói tour'}
            </h2>
            <p className="text-slate-500 font-medium mt-1">{selectedTour ? `Tour đang chọn: ${selectedTour.name}` : 'Chọn tour để quản lý gói.'}</p>
          </div>

          <form onSubmit={handleSubmit} className="p-8 space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mã gói</span>
                <input name="code" value={form.code} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Tên gói</span>
                <input name="name" value={form.name} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" required />
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Kiểu gói</span>
                <select name="mode" value={form.mode} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {PACKAGE_MODE_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Trạng thái</span>
                <select name="status" value={form.status} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {PACKAGE_STATUS_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Mã tiền tệ</span>
                <input name="currencyCode" value={form.currencyCode} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none" />
              </label>
              <label className="space-y-2">
                <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Chiến lược giữ chỗ</span>
                <select name="holdStrategy" value={form.holdStrategy} onChange={handleFieldChange} className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none">
                  {HOLD_STRATEGY_OPTIONS.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </label>
            </div>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Quy tắc định giá (JSON)</span>
              <textarea name="pricingRuleJson" value={form.pricingRuleJson} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <label className="space-y-2 block">
              <span className="text-[11px] font-black uppercase tracking-widest text-slate-400">Metadata (JSON)</span>
              <textarea name="metadataJson" value={form.metadataJson} onChange={handleFieldChange} rows={4} className="w-full rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium outline-none resize-none" />
            </label>

            <div className="grid grid-cols-2 gap-3">
              {[
                { name: 'isDefault', label: 'Đặt mặc định' },
                { name: 'autoRepriceBeforeConfirm', label: 'Tự tính giá lại trước confirm' },
                { name: 'isActive', label: 'Đang hoạt động' },
              ].map((item) => (
                <label key={item.name} className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700">
                  <input type="checkbox" name={item.name} checked={Boolean(form[item.name])} onChange={handleFieldChange} className="h-4 w-4 rounded border-slate-300 text-blue-600" />
                  {item.label}
                </label>
              ))}
            </div>

            {selectedPackage && (
              <div className="rounded-2xl bg-slate-50 border border-slate-100 px-5 py-4 text-xs font-bold text-slate-500">
                Cập nhật lần cuối: {formatDateTime(selectedPackage.updatedAt || selectedPackage.createdAt)}
              </div>
            )}

            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={saving || !selectedTourId} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
                {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                {selectedPackage ? 'Lưu gói tour' : 'Tạo gói tour'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </TourManagementShell>
  );
}
