import React, { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw, PhoneCall } from 'lucide-react';
import AdminHotelPageShell from '../hotel/components/AdminHotelPageShell';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  createAdminHotelContact,
  deleteAdminHotelContact,
  getAdminHotelContact,
  getAdminHotelOptions,
  listAdminHotelContacts,
  restoreAdminHotelContact,
  setAdminHotelContactPrimary,
  updateAdminHotelContact,
} from '../../../services/hotelService';

function createEmptyForm(hotelId = '') {
  return {
    hotelId,
    name: '',
    title: '',
    department: '',
    phone: '',
    email: '',
    contactType: '',
    isPrimary: false,
    sortOrder: 0,
    notes: '',
    metadataJson: '',
    isActive: true,
  };
}

function hydrateForm(detail) {
  return {
    hotelId: detail.hotelId || '',
    name: detail.name || '',
    title: detail.title || '',
    department: detail.department || '',
    phone: detail.phone || '',
    email: detail.email || '',
    contactType: detail.contactType || '',
    isPrimary: !!detail.isPrimary,
    sortOrder: detail.sortOrder ?? 0,
    notes: detail.notes || '',
    metadataJson: detail.metadataJson || '',
    isActive: detail.isActive ?? true,
    rowVersionBase64: detail.rowVersionBase64 || '',
  };
}

function buildPayload(form) {
  return {
    hotelId: form.hotelId,
    name: form.name.trim() || null,
    title: form.title.trim() || null,
    department: form.department.trim() || null,
    phone: form.phone.trim() || null,
    email: form.email.trim() || null,
    contactType: form.contactType.trim() || null,
    isPrimary: !!form.isPrimary,
    sortOrder: Number(form.sortOrder || 0),
    notes: form.notes.trim() || null,
    metadataJson: form.metadataJson.trim() || null,
    isActive: !!form.isActive,
    rowVersionBase64: form.rowVersionBase64 || null,
  };
}

export default function AdminHotelContactsPage() {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminHotelScope();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [hotels, setHotels] = useState([]);
  const [selectedHotelId, setSelectedHotelId] = useState('');
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm());

  async function loadData() {
    if (!tenantId) {
      setHotels([]);
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, listResponse] = await Promise.all([
        getAdminHotelOptions(tenantId),
        listAdminHotelContacts({ includeDeleted: true, pageSize: 100 }, tenantId),
      ]);

      const nextHotels = Array.isArray(optionsResponse?.hotels) ? optionsResponse.hotels : [];
      const nextItems = Array.isArray(listResponse?.items) ? listResponse.items : [];
      const nextHotelId = selectedHotelId || nextHotels[0]?.id || '';

      setHotels(nextHotels);
      setItems(nextItems);
      setSelectedHotelId(nextHotelId);

      if (!selectedId) {
        setForm(createEmptyForm(nextHotelId));
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh bạ khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [loadDataRef, tenantId]);

  const filteredItems = useMemo(
    () => items.filter((item) => !selectedHotelId || item.hotelId === selectedHotelId),
    [items, selectedHotelId],
  );

  async function loadDetail(id) {
    try {
      const detail = await getAdminHotelContact(id, { includeDeleted: true }, tenantId);
      setSelectedId(id);
      setSelectedHotelId(detail.hotelId || selectedHotelId);
      setForm(hydrateForm(detail));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết liên hệ.');
    }
  }

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm(selectedHotelId || hotels[0]?.id || ''));
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
        await updateAdminHotelContact(selectedId, payload, tenantId);
        setNotice('Đã cập nhật liên hệ khách sạn.');
      } else {
        await createAdminHotelContact(payload, tenantId);
        setNotice('Đã tạo liên hệ khách sạn mới.');
      }

      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu liên hệ khách sạn.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleDelete(item) {
    try {
      if (item.isDeleted) {
        await restoreAdminHotelContact(item.id, tenantId);
        setNotice('Đã khôi phục liên hệ khách sạn.');
      } else {
        await deleteAdminHotelContact(item.id, tenantId);
        setNotice('Đã ẩn liên hệ khách sạn.');
      }
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái liên hệ.');
    }
  }

  async function handleSetPrimary(item) {
    try {
      await setAdminHotelContactPrimary(item.id, tenantId);
      setNotice('Đã cập nhật liên hệ chính.');
      await loadDataRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể đặt liên hệ chính.');
    }
  }

  return (
    <AdminHotelPageShell
      pageKey="contacts"
      title="Liên hệ khách sạn"
      subtitle="Admin quản lý đầu mối liên hệ, bộ phận và kênh chăm sóc của từng khách sạn trong tenant đang chọn."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <>
          <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button type="button" onClick={handleCreateNew} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-black flex items-center gap-2">
            <Plus size={16} />
            Thêm liên hệ
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.9fr_1.1fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 space-y-4">
            <div>
              <p className="text-lg font-black text-slate-900">Danh bạ liên hệ</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Lọc theo khách sạn để kiểm tra liên hệ chính, hotline và bộ phận.</p>
            </div>
            <select value={selectedHotelId} onChange={(event) => setSelectedHotelId(event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
              <option value="">Tất cả khách sạn</option>
              {hotels.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </div>

          <div className="divide-y divide-slate-50 max-h-[720px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải danh bạ...</div>
            ) : filteredItems.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Tenant này chưa có liên hệ nào.</div>
            ) : filteredItems.map((item) => (
              <div
                key={item.id}
                role="button"
                tabIndex={0}
                onClick={() => loadDetail(item.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    loadDetail(item.id);
                  }
                }}
                className={`px-8 py-6 transition-all hover:bg-slate-50 ${selectedId === item.id ? 'bg-slate-50' : ''}`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center">
                      <PhoneCall size={20} />
                    </div>
                    <div>
                      <div className="flex items-center gap-3 flex-wrap">
                        <p className="font-black text-slate-900">{item.name || 'Liên hệ chưa đặt tên'}</p>
                        {item.isPrimary ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-blue-100 text-blue-700">Chính</span> : null}
                        {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                      </div>
                      <p className="text-xs font-bold text-slate-400 mt-2">
                        {item.contactType || 'General'} • {item.phone || 'Chưa có số điện thoại'}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button type="button" onClick={(event) => { event.stopPropagation(); handleSetPrimary(item); }} className="px-3 py-2 rounded-xl bg-blue-50 text-[10px] font-black uppercase tracking-widest text-blue-700">
                      Đặt chính
                    </button>
                    <button type="button" onClick={(event) => { event.stopPropagation(); handleToggleDelete(item); }} className="px-3 py-2 rounded-xl bg-slate-900 text-[10px] font-black uppercase tracking-widest text-white">
                      {item.isDeleted ? 'Khôi phục' : 'Ẩn'}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-8 space-y-5">
          <div>
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật liên hệ khách sạn' : 'Tạo liên hệ khách sạn mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Giữ lại đúng hotline, email, bộ phận để frontend public có thể hiển thị chuẩn.</p>
          </div>

          <select value={form.hotelId} onChange={(event) => setForm((current) => ({ ...current, hotelId: event.target.value }))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none">
            <option value="">Chọn khách sạn</option>
            {hotels.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên liên hệ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} placeholder="Chức danh" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.department} onChange={(event) => setForm((current) => ({ ...current, department: event.target.value }))} placeholder="Bộ phận" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.contactType} onChange={(event) => setForm((current) => ({ ...current, contactType: event.target.value }))} placeholder="Loại liên hệ" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.phone} onChange={(event) => setForm((current) => ({ ...current, phone: event.target.value }))} placeholder="Số điện thoại" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} placeholder="Email" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input type="number" value={form.sortOrder} onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))} placeholder="Thứ tự" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.metadataJson} onChange={(event) => setForm((current) => ({ ...current, metadataJson: event.target.value }))} placeholder="Metadata JSON" className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none" />
          </div>

          <textarea value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} rows={5} placeholder="Ghi chú nội bộ" className="w-full rounded-[1.75rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />

          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={form.isPrimary} onChange={(event) => setForm((current) => ({ ...current, isPrimary: event.target.checked }))} />
              Liên hệ chính
            </label>
            <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
              Kích hoạt
            </label>
          </div>

          <div className="flex flex-wrap gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : selectedId ? 'Cập nhật liên hệ' : 'Tạo liên hệ'}
            </button>
            {selectedId ? <button type="button" onClick={handleCreateNew} className="px-6 py-4 rounded-2xl bg-slate-100 text-sm font-black text-slate-600">Tạo bản mới</button> : null}
          </div>
        </form>
      </div>
    </AdminHotelPageShell>
  );
}
