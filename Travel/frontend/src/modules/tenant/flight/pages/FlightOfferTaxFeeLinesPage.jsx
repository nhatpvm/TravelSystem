import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import FlightModeShell from '../components/FlightModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminFlightOfferTaxFeeLine,
  createFlightOfferTaxFeeLine,
  deleteAdminFlightOfferTaxFeeLine,
  deleteFlightOfferTaxFeeLine,
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlightOfferTaxFeeLines,
  listFlightOfferTaxFeeLines,
  restoreAdminFlightOfferTaxFeeLine,
  restoreFlightOfferTaxFeeLine,
  updateAdminFlightOfferTaxFeeLine,
  updateFlightOfferTaxFeeLine,
} from '../../../../services/flightService';
import {
  formatCurrency,
  getTaxFeeLineTypeLabel,
  parseEnumOptionValue,
  TAX_FEE_LINE_TYPE_OPTIONS,
} from '../utils/presentation';

function createEmptyForm() {
  return {
    offerId: '',
    sortOrder: '1',
    lineType: 2,
    code: '',
    name: '',
    currencyCode: 'VND',
    amount: '0',
  };
}

function hydrateForm(item) {
  return {
    offerId: item.offerId || '',
    sortOrder: String(item.sortOrder ?? 1),
    lineType: parseEnumOptionValue(TAX_FEE_LINE_TYPE_OPTIONS, item.lineType, 2),
    code: item.code || '',
    name: item.name || '',
    currencyCode: item.currencyCode || 'VND',
    amount: String(item.amount ?? 0),
  };
}

function buildPayload(form) {
  return {
    offerId: form.offerId,
    sortOrder: Number(form.sortOrder || 0),
    lineType: Number(form.lineType),
    code: form.code.trim().toUpperCase(),
    name: form.name.trim(),
    currencyCode: String(form.currencyCode || 'VND').trim().toUpperCase(),
    amount: Number(form.amount || 0),
  };
}

function getOfferLabel(offer) {
  return offer?.flight?.flightNumber || offer?.flightNumber || offer?.id?.slice(0, 8) || 'Offer';
}

export default function FlightOfferTaxFeeLinesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [searchParams] = useSearchParams();
  const [offers, setOffers] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [selectedOfferId, setSelectedOfferId] = useState(searchParams.get('offerId') || '');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightOfferTaxFeeLines(params, tenantId) : listFlightOfferTaxFeeLines;
  const createFn = isAdmin ? (payload) => createAdminFlightOfferTaxFeeLine(payload, tenantId) : createFlightOfferTaxFeeLine;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightOfferTaxFeeLine(id, payload, tenantId) : updateFlightOfferTaxFeeLine;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightOfferTaxFeeLine(id, tenantId) : deleteFlightOfferTaxFeeLine;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightOfferTaxFeeLine(id, tenantId) : restoreFlightOfferTaxFeeLine;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setOffers([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const optionsResponse = isAdmin ? await getAdminFlightOptions(tenantId) : await getFlightManagerOptions();
      const nextOffers = Array.isArray(optionsResponse?.offers) ? optionsResponse.offers : [];
      setOffers(nextOffers);

      const effectiveOfferId = selectedOfferId || nextOffers[0]?.id || '';
      if (!selectedOfferId && effectiveOfferId) {
        setSelectedOfferId(effectiveOfferId);
      }

      if (!effectiveOfferId) {
        setItems([]);
        setSelectedId('');
        setForm(createEmptyForm());
        return;
      }

      const itemsResponse = await listFn({ includeDeleted: true, offerId: effectiveOfferId });
      const nextItems = Array.isArray(itemsResponse?.items) ? itemsResponse.items : [];
      setItems(nextItems);

      if (nextItems.length > 0) {
        const selected = nextItems.find((item) => item.id === selectedId) || nextItems[0];
        setSelectedId(selected.id);
        setForm(hydrateForm(selected));
      } else {
        setSelectedId('');
        setForm({
          ...createEmptyForm(),
          offerId: effectiveOfferId,
        });
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải thuế và phí của offer.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, tenantId, selectedOfferId, loadDataRef]);

  const offerLookup = useMemo(
    () => Object.fromEntries(offers.map((item) => [item.id, item])),
    [offers],
  );

  function handleCreateNew() {
    setSelectedId('');
    setForm({
      ...createEmptyForm(),
      offerId: selectedOfferId || offers[0]?.id || '',
    });
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = buildPayload(form);
      if (selectedId) {
        await updateFn(selectedId, payload);
        setNotice('Đã cập nhật dòng thuế/phí.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo dòng thuế/phí mới.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được dòng thuế/phí.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    setError('');
    setNotice('');

    try {
      if (item.isDeleted) {
        await restoreFn(item.id);
        setNotice('Đã khôi phục dòng thuế/phí.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn dòng thuế/phí.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái dòng thuế/phí.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="tax-fee-lines"
      title="Thuế và phí offer"
      subtitle="Chi tiết dòng giá giúp hiển thị breakdown chuẩn trên detail, checkout và minh bạch khi đối soát với đối tác."
      notice={notice}
      error={error}
      actions={(
        <>
          <div className="px-4 py-3 rounded-2xl border border-slate-200 bg-white">
            <select
              value={selectedOfferId}
              onChange={(event) => setSelectedOfferId(event.target.value)}
              className="bg-transparent text-sm font-bold text-slate-700 outline-none"
            >
              {offers.map((item) => (
                <option key={item.id} value={item.id}>{getOfferLabel(item)}</option>
              ))}
            </select>
          </div>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm dòng giá
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách dòng giá</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Một offer có thể gồm nhiều dòng giá như base fare, thuế, phí hoặc giảm trừ.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải thuế và phí...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có dòng giá nào.</div>
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
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.code}</p>
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-600">
                        {getTaxFeeLineTypeLabel(item.lineType)}
                      </span>
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.name}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      Offer: {getOfferLabel(offerLookup[item.offerId])} · {formatCurrency(item.amount, item.currencyCode)}
                    </p>
                  </div>
                  <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                    {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-6">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật dòng giá' : 'Tạo dòng giá mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Dùng để dựng breakdown giá ở detail page và checkout một cách minh bạch.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <select value={form.offerId} onChange={(event) => setForm((current) => ({ ...current, offerId: event.target.value }))} className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              <option value="">Chọn offer</option>
              {offers.map((item) => <option key={item.id} value={item.id}>{getOfferLabel(item)}</option>)}
            </select>
            <input type="number" min="0" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} placeholder="Thứ tự hiển thị" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <select value={form.lineType} onChange={(event) => setForm((current) => ({ ...current, lineType: Number(event.target.value) }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none">
              {TAX_FEE_LINE_TYPE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã dòng giá" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên hiển thị" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.currencyCode} onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))} placeholder="Mã tiền tệ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" min="0" step="1000" value={form.amount} onChange={(event) => setForm((current) => ({ ...current, amount: event.target.value }))} placeholder="Số tiền" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật dòng giá' : 'Tạo dòng giá')}
            </button>
            {selectedId ? (
              <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">
                Tạo bản mới
              </button>
            ) : null}
          </div>
        </form>
      </div>
    </FlightModeShell>
  );
}
