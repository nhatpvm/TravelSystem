import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import HotelModeShell from '../components/HotelModeShell';
import useLatestRef from '../../../../shared/hooks/useLatestRef';
import {
  createAdminCancellationPolicy,
  createAdminCheckInOutRule,
  createAdminPropertyPolicy,
  createManagedCancellationPolicy,
  createManagedCheckInOutRule,
  createManagedPropertyPolicy,
  deleteAdminCancellationPolicy,
  deleteAdminCheckInOutRule,
  deleteAdminPropertyPolicy,
  deleteManagedCancellationPolicy,
  deleteManagedCheckInOutRule,
  deleteManagedPropertyPolicy,
  getAdminCancellationPolicy,
  getAdminCheckInOutRule,
  getAdminHotelOptions,
  getAdminPropertyPolicy,
  getHotelManagerOptions,
  getManagedCancellationPolicy,
  getManagedCheckInOutRule,
  getManagedPropertyPolicy,
  listAdminCancellationPolicies,
  listAdminCheckInOutRules,
  listAdminPropertyPolicies,
  listManagedCancellationPolicies,
  listManagedCheckInOutRules,
  listManagedPropertyPolicies,
  restoreAdminCancellationPolicy,
  restoreAdminCheckInOutRule,
  restoreAdminPropertyPolicy,
  restoreManagedCancellationPolicy,
  restoreManagedCheckInOutRule,
  restoreManagedPropertyPolicy,
  updateAdminCancellationPolicy,
  updateAdminCheckInOutRule,
  updateAdminPropertyPolicy,
  updateManagedCancellationPolicy,
  updateManagedCheckInOutRule,
  updateManagedPropertyPolicy,
} from '../../../../services/hotelService';
import {
  CANCELLATION_POLICY_TYPE_OPTIONS,
  parseEnumOptionValue,
  readJsonInput,
  toPrettyJson,
} from '../utils/presentation';

function createCancellationForm(hotelId = '') {
  return {
    hotelId,
    code: '',
    name: '',
    type: 1,
    description: '',
    metadataJson: '',
    isActive: true,
    rulesJson: '[]',
  };
}

function createCheckInOutForm(hotelId = '') {
  return {
    hotelId,
    code: '',
    name: '',
    checkInFrom: '14:00',
    checkInTo: '18:00',
    checkOutFrom: '06:00',
    checkOutTo: '12:00',
    allowsEarlyCheckIn: false,
    allowsLateCheckOut: false,
    notes: '',
    isActive: true,
  };
}

function createPropertyForm(hotelId = '') {
  return {
    hotelId,
    code: '',
    name: '',
    policyJson: '{}',
    notes: '',
    isActive: true,
  };
}

export default function HotelPoliciesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [hotels, setHotels] = useState([]);
  const [selectedHotelId, setSelectedHotelId] = useState('');
  const [cancellations, setCancellations] = useState([]);
  const [checkRules, setCheckRules] = useState([]);
  const [properties, setProperties] = useState([]);
  const [selectedCancellationId, setSelectedCancellationId] = useState('');
  const [selectedCheckRuleId, setSelectedCheckRuleId] = useState('');
  const [selectedPropertyId, setSelectedPropertyId] = useState('');
  const [cancellationForm, setCancellationForm] = useState(createCancellationForm());
  const [checkForm, setCheckForm] = useState(createCheckInOutForm());
  const [propertyForm, setPropertyForm] = useState(createPropertyForm());

  async function loadData() {
    if (isAdmin && !tenantId) {
      setHotels([]);
      setCancellations([]);
      setCheckRules([]);
      setProperties([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, cancellationResponse, checkResponse, propertyResponse] = await Promise.all([
        isAdmin ? getAdminHotelOptions(tenantId) : getHotelManagerOptions(),
        isAdmin ? listAdminCancellationPolicies({ includeDeleted: true, pageSize: 100 }, tenantId) : listManagedCancellationPolicies({ includeDeleted: true, pageSize: 100 }),
        isAdmin ? listAdminCheckInOutRules({ includeDeleted: true, pageSize: 100 }, tenantId) : listManagedCheckInOutRules({ includeDeleted: true, pageSize: 100 }),
        isAdmin ? listAdminPropertyPolicies({ includeDeleted: true, pageSize: 100 }, tenantId) : listManagedPropertyPolicies({ includeDeleted: true, pageSize: 100 }),
      ]);

      const hotelItems = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
      const nextHotelId = selectedHotelId || hotelItems[0]?.id || '';

      setHotels(hotelItems);
      setSelectedHotelId(nextHotelId);
      setCancellations(Array.isArray(cancellationResponse?.items) ? cancellationResponse.items : []);
      setCheckRules(Array.isArray(checkResponse?.items) ? checkResponse.items : []);
      setProperties(Array.isArray(propertyResponse?.items) ? propertyResponse.items : []);

      if (!selectedCancellationId) {
        setCancellationForm(createCancellationForm(nextHotelId));
      }
      if (!selectedCheckRuleId) {
        setCheckForm(createCheckInOutForm(nextHotelId));
      }
      if (!selectedPropertyId) {
        setPropertyForm(createPropertyForm(nextHotelId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chính sách khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [isAdmin, loadDataRef, tenantId]);

  const filteredCancellations = useMemo(
    () => cancellations.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [cancellations, selectedHotelId],
  );

  const filteredCheckRules = useMemo(
    () => checkRules.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [checkRules, selectedHotelId],
  );

  const filteredProperties = useMemo(
    () => properties.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [properties, selectedHotelId],
  );

  async function selectCancellation(id) {
    const detail = isAdmin
      ? await getAdminCancellationPolicy(id, { includeDeleted: true }, tenantId)
      : await getManagedCancellationPolicy(id, { includeDeleted: true });

    setSelectedCancellationId(id);
    setCancellationForm({
      hotelId: detail.hotelId || selectedHotelId,
      code: detail.code || '',
      name: detail.name || '',
      type: parseEnumOptionValue(CANCELLATION_POLICY_TYPE_OPTIONS, detail.type, 1),
      description: detail.description || '',
      metadataJson: detail.metadataJson || '',
      isActive: detail.isActive ?? true,
      rulesJson: toPrettyJson(detail.rules || []),
      rowVersionBase64: detail.rowVersionBase64 || '',
    });
  }

  async function selectCheckRule(id) {
    const detail = isAdmin
      ? await getAdminCheckInOutRule(id, { includeDeleted: true }, tenantId)
      : await getManagedCheckInOutRule(id, { includeDeleted: true });

    setSelectedCheckRuleId(id);
    setCheckForm({
      hotelId: detail.hotelId || selectedHotelId,
      code: detail.code || '',
      name: detail.name || '',
      checkInFrom: detail.checkInFrom || '14:00',
      checkInTo: detail.checkInTo || '18:00',
      checkOutFrom: detail.checkOutFrom || '06:00',
      checkOutTo: detail.checkOutTo || '12:00',
      allowsEarlyCheckIn: detail.allowsEarlyCheckIn ?? false,
      allowsLateCheckOut: detail.allowsLateCheckOut ?? false,
      notes: detail.notes || '',
      isActive: detail.isActive ?? true,
      rowVersionBase64: detail.rowVersionBase64 || '',
    });
  }

  async function selectProperty(id) {
    const detail = isAdmin
      ? await getAdminPropertyPolicy(id, { includeDeleted: true }, tenantId)
      : await getManagedPropertyPolicy(id, { includeDeleted: true });

    setSelectedPropertyId(id);
    setPropertyForm({
      hotelId: detail.hotelId || selectedHotelId,
      code: detail.code || '',
      name: detail.name || '',
      policyJson: typeof detail.policyJson === 'string' ? detail.policyJson : toPrettyJson(detail.policyJson || {}),
      notes: detail.notes || '',
      isActive: detail.isActive ?? true,
      rowVersionBase64: detail.rowVersionBase64 || '',
    });
  }

  async function saveCancellation() {
    const payload = {
      hotelId: cancellationForm.hotelId,
      code: cancellationForm.code.trim(),
      name: cancellationForm.name.trim(),
      type: Number(cancellationForm.type || 1),
      description: cancellationForm.description.trim() || null,
      metadataJson: cancellationForm.metadataJson.trim() || null,
      isActive: !!cancellationForm.isActive,
      rules: readJsonInput(cancellationForm.rulesJson, []),
      rowVersionBase64: cancellationForm.rowVersionBase64 || null,
    };

    if (selectedCancellationId) {
      if (isAdmin) await updateAdminCancellationPolicy(selectedCancellationId, payload, tenantId);
      else await updateManagedCancellationPolicy(selectedCancellationId, payload);
      setNotice('Đã cập nhật chính sách hủy.');
    } else {
      if (isAdmin) await createAdminCancellationPolicy(payload, tenantId);
      else await createManagedCancellationPolicy(payload);
      setNotice('Đã tạo chính sách hủy mới.');
    }
  }

  async function saveCheckRule() {
    const payload = {
      hotelId: checkForm.hotelId,
      code: checkForm.code.trim(),
      name: checkForm.name.trim(),
      checkInFrom: checkForm.checkInFrom,
      checkInTo: checkForm.checkInTo,
      checkOutFrom: checkForm.checkOutFrom,
      checkOutTo: checkForm.checkOutTo,
      allowsEarlyCheckIn: !!checkForm.allowsEarlyCheckIn,
      allowsLateCheckOut: !!checkForm.allowsLateCheckOut,
      notes: checkForm.notes.trim() || null,
      isActive: !!checkForm.isActive,
      rowVersionBase64: checkForm.rowVersionBase64 || null,
    };

    if (selectedCheckRuleId) {
      if (isAdmin) await updateAdminCheckInOutRule(selectedCheckRuleId, payload, tenantId);
      else await updateManagedCheckInOutRule(selectedCheckRuleId, payload);
      setNotice('Đã cập nhật quy tắc check-in/out.');
    } else {
      if (isAdmin) await createAdminCheckInOutRule(payload, tenantId);
      else await createManagedCheckInOutRule(payload);
      setNotice('Đã tạo quy tắc check-in/out mới.');
    }
  }

  async function saveProperty() {
    const payload = {
      hotelId: propertyForm.hotelId,
      code: propertyForm.code.trim(),
      name: propertyForm.name.trim(),
      policyJson: propertyForm.policyJson.trim() || '{}',
      notes: propertyForm.notes.trim() || null,
      isActive: !!propertyForm.isActive,
      rowVersionBase64: propertyForm.rowVersionBase64 || null,
    };

    if (selectedPropertyId) {
      if (isAdmin) await updateAdminPropertyPolicy(selectedPropertyId, payload, tenantId);
      else await updateManagedPropertyPolicy(selectedPropertyId, payload);
      setNotice('Đã cập nhật nội quy lưu trú.');
    } else {
      if (isAdmin) await createAdminPropertyPolicy(payload, tenantId);
      else await createManagedPropertyPolicy(payload);
      setNotice('Đã tạo nội quy lưu trú mới.');
    }
  }

  async function handleSaveSection(event, section) {
    event.preventDefault();
    setSaving(section);
    setError('');
    setNotice('');

    try {
      if (section === 'cancellation') {
        await saveCancellation();
      }

      if (section === 'check') {
        await saveCheckRule();
      }

      if (section === 'property') {
        await saveProperty();
      }

      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu chính sách khách sạn.');
    } finally {
      setSaving('');
    }
  }

  async function handleToggleDelete(type, item) {
    try {
      if (type === 'cancellation') {
        if (item.isDeleted) {
          if (isAdmin) await restoreAdminCancellationPolicy(item.id, tenantId);
          else await restoreManagedCancellationPolicy(item.id);
        } else if (isAdmin) await deleteAdminCancellationPolicy(item.id, tenantId);
        else await deleteManagedCancellationPolicy(item.id);
      }

      if (type === 'check') {
        if (item.isDeleted) {
          if (isAdmin) await restoreAdminCheckInOutRule(item.id, tenantId);
          else await restoreManagedCheckInOutRule(item.id);
        } else if (isAdmin) await deleteAdminCheckInOutRule(item.id, tenantId);
        else await deleteManagedCheckInOutRule(item.id);
      }

      if (type === 'property') {
        if (item.isDeleted) {
          if (isAdmin) await restoreAdminPropertyPolicy(item.id, tenantId);
          else await restoreManagedPropertyPolicy(item.id);
        } else if (isAdmin) await deleteAdminPropertyPolicy(item.id, tenantId);
        else await deleteManagedPropertyPolicy(item.id);
      }

      setNotice('Đã cập nhật trạng thái chính sách.');
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái chính sách.');
    }
  }

  function handleCreateNew() {
    setSelectedCancellationId('');
    setSelectedCheckRuleId('');
    setSelectedPropertyId('');
    setCancellationForm(createCancellationForm(selectedHotelId));
    setCheckForm(createCheckInOutForm(selectedHotelId));
    setPropertyForm(createPropertyForm(selectedHotelId));
    setNotice('');
  }

  return (
    <HotelModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="policies"
      title="Chính sách khách sạn"
      subtitle="Quản lý policy hủy, giờ check-in/out và property policy theo từng khách sạn."
      notice={notice}
      error={error}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Tạo bộ mới
          </button>
        </>
      )}
    >
      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
        <select value={selectedHotelId} onChange={(event) => setSelectedHotelId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
          <option value="">Chọn khách sạn</option>
          {hotels.map((item) => (
            <option key={item.id} value={item.id}>{item.name}</option>
          ))}
        </select>

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          <div className="rounded-[2rem] border border-slate-100 p-5 space-y-4">
            <p className="text-sm font-black text-slate-900">Chính sách hủy</p>
            <div className="max-h-48 overflow-y-auto space-y-3">
              {filteredCancellations.map((item) => (
                <div key={item.id} className="rounded-2xl bg-slate-50 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <button type="button" onClick={() => selectCancellation(item.id)} className="text-left font-black text-slate-900">{item.name}</button>
                    <button type="button" onClick={() => handleToggleDelete('cancellation', item)} className="text-[10px] font-black uppercase tracking-widest text-slate-500">
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              ))}
              {!loading && filteredCancellations.length === 0 ? <p className="text-xs font-bold text-slate-400">Chưa có chính sách hủy.</p> : null}
            </div>
            <input value={cancellationForm.code} onChange={(event) => setCancellationForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã policy" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={cancellationForm.name} onChange={(event) => setCancellationForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên policy" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <select value={cancellationForm.type} onChange={(event) => setCancellationForm((current) => ({ ...current, type: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              {CANCELLATION_POLICY_TYPE_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <textarea value={cancellationForm.rulesJson} onChange={(event) => setCancellationForm((current) => ({ ...current, rulesJson: event.target.value }))} rows={8} placeholder='Rules JSON, ví dụ: [{"cancelBeforeHours":48,"chargeType":2,"chargeValue":0,"priority":1}]' className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <button type="button" onClick={(event) => handleSaveSection(event, 'cancellation')} disabled={saving !== ''} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving === 'cancellation' ? 'Đang lưu...' : 'Lưu chính sách hủy'}
            </button>
          </div>

          <div className="rounded-[2rem] border border-slate-100 p-5 space-y-4">
            <p className="text-sm font-black text-slate-900">Check-in / Check-out</p>
            <div className="max-h-48 overflow-y-auto space-y-3">
              {filteredCheckRules.map((item) => (
                <div key={item.id} className="rounded-2xl bg-slate-50 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <button type="button" onClick={() => selectCheckRule(item.id)} className="text-left font-black text-slate-900">{item.name}</button>
                    <button type="button" onClick={() => handleToggleDelete('check', item)} className="text-[10px] font-black uppercase tracking-widest text-slate-500">
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              ))}
              {!loading && filteredCheckRules.length === 0 ? <p className="text-xs font-bold text-slate-400">Chưa có rule check-in/out.</p> : null}
            </div>
            <input value={checkForm.code} onChange={(event) => setCheckForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã rule" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={checkForm.name} onChange={(event) => setCheckForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên rule" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <div className="grid grid-cols-2 gap-4">
              <input type="time" value={checkForm.checkInFrom} onChange={(event) => setCheckForm((current) => ({ ...current, checkInFrom: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
              <input type="time" value={checkForm.checkInTo} onChange={(event) => setCheckForm((current) => ({ ...current, checkInTo: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
              <input type="time" value={checkForm.checkOutFrom} onChange={(event) => setCheckForm((current) => ({ ...current, checkOutFrom: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
              <input type="time" value={checkForm.checkOutTo} onChange={(event) => setCheckForm((current) => ({ ...current, checkOutTo: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            </div>
            <textarea value={checkForm.notes} onChange={(event) => setCheckForm((current) => ({ ...current, notes: event.target.value }))} rows={4} placeholder="Ghi chú check-in/out" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <button type="button" onClick={(event) => handleSaveSection(event, 'check')} disabled={saving !== ''} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving === 'check' ? 'Đang lưu...' : 'Lưu check-in / check-out'}
            </button>
          </div>

          <div className="rounded-[2rem] border border-slate-100 p-5 space-y-4">
            <p className="text-sm font-black text-slate-900">Property policy</p>
            <div className="max-h-48 overflow-y-auto space-y-3">
              {filteredProperties.map((item) => (
                <div key={item.id} className="rounded-2xl bg-slate-50 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <button type="button" onClick={() => selectProperty(item.id)} className="text-left font-black text-slate-900">{item.name}</button>
                    <button type="button" onClick={() => handleToggleDelete('property', item)} className="text-[10px] font-black uppercase tracking-widest text-slate-500">
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              ))}
              {!loading && filteredProperties.length === 0 ? <p className="text-xs font-bold text-slate-400">Chưa có property policy.</p> : null}
            </div>
            <input value={propertyForm.code} onChange={(event) => setPropertyForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã policy" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={propertyForm.name} onChange={(event) => setPropertyForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên policy" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <textarea value={propertyForm.policyJson} onChange={(event) => setPropertyForm((current) => ({ ...current, policyJson: event.target.value }))} rows={8} placeholder="Policy JSON" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <textarea value={propertyForm.notes} onChange={(event) => setPropertyForm((current) => ({ ...current, notes: event.target.value }))} rows={3} placeholder="Ghi chú" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <button type="button" onClick={(event) => handleSaveSection(event, 'property')} disabled={saving !== ''} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving === 'property' ? 'Đang lưu...' : 'Lưu property policy'}
            </button>
          </div>
        </div>
      </div>
    </HotelModeShell>
  );
}
