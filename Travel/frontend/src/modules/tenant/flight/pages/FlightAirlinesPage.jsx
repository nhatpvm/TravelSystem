import React, { useEffect, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import FlightModeShell from '../components/FlightModeShell';
import {
  createAdminFlightAirline,
  createFlightAirline,
  deleteAdminFlightAirline,
  deleteFlightAirline,
  listAdminFlightAirlines,
  listFlightAirlines,
  restoreAdminFlightAirline,
  restoreFlightAirline,
  updateAdminFlightAirline,
  updateFlightAirline,
} from '../../../../services/flightService';
import { uploadManagerImage } from '../../../../services/portalUploadService';
import ImageUploadField from '../../../../shared/components/forms/ImageUploadField';

function createEmptyForm() {
  return {
    code: '',
    name: '',
    iataCode: '',
    icaoCode: '',
    logoUrl: '',
    websiteUrl: '',
    supportPhone: '',
    supportEmail: '',
    isActive: true,
  };
}

function hydrateForm(item) {
  return {
    code: item.code || '',
    name: item.name || '',
    iataCode: item.iataCode || '',
    icaoCode: item.icaoCode || '',
    logoUrl: item.logoUrl || '',
    websiteUrl: item.websiteUrl || '',
    supportPhone: item.supportPhone || '',
    supportEmail: item.supportEmail || '',
    isActive: item.isActive ?? true,
  };
}

function buildPayload(form) {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    iataCode: form.iataCode.trim() || null,
    icaoCode: form.icaoCode.trim() || null,
    logoUrl: form.logoUrl.trim() || null,
    websiteUrl: form.websiteUrl.trim() || null,
    supportPhone: form.supportPhone.trim() || null,
    supportEmail: form.supportEmail.trim() || null,
    isActive: !!form.isActive,
  };
}

export default function FlightAirlinesPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [form, setForm] = useState(createEmptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingLogo, setUploadingLogo] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const listFn = isAdmin ? (params) => listAdminFlightAirlines(params, tenantId) : listFlightAirlines;
  const createFn = isAdmin ? (payload) => createAdminFlightAirline(payload, tenantId) : createFlightAirline;
  const updateFn = isAdmin ? (id, payload) => updateAdminFlightAirline(id, payload, tenantId) : updateFlightAirline;
  const deleteFn = isAdmin ? (id) => deleteAdminFlightAirline(id, tenantId) : deleteFlightAirline;
  const restoreFn = isAdmin ? (id) => restoreAdminFlightAirline(id, tenantId) : restoreFlightAirline;

  async function loadData() {
    if (isAdmin && !tenantId) {
      setItems([]);
      setSelectedId('');
      setForm(createEmptyForm());
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await listFn({ includeDeleted: true, pageSize: 100 });
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
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách hãng bay.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  function handleCreateNew() {
    setSelectedId('');
    setForm(createEmptyForm());
    setNotice('');
  }

  async function handleUploadLogo(file) {
    if (isAdmin && !tenantId) {
      setError('Vui lòng chọn tenant trước khi tải logo.');
      return;
    }

    setUploadingLogo(true);
    setError('');
    setNotice('');

    try {
      const response = await uploadManagerImage(file, {
        scope: 'airline-logo',
        tenantId: isAdmin ? tenantId : undefined,
      });
      setForm((current) => ({ ...current, logoUrl: response?.url || '' }));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải logo hãng bay.');
    } finally {
      setUploadingLogo(false);
    }
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
        setNotice('Đã cập nhật hãng bay.');
      } else {
        await createFn(payload);
        setNotice('Đã tạo hãng bay mới.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không lưu được hãng bay.');
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
        setNotice('Đã khôi phục hãng bay.');
      } else {
        await deleteFn(item.id);
        setNotice('Đã ẩn hãng bay.');
      }
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không cập nhật được trạng thái hãng bay.');
    }
  }

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="airlines"
      title="Hãng bay"
      subtitle="Quản lý thương hiệu, mã IATA/ICAO và thông tin hỗ trợ của từng hãng bay trong tenant."
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
            Thêm hãng bay
          </button>
        </>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.95fr_1.05fr] gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100">
            <p className="text-lg font-black text-slate-900">Danh sách hãng bay</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Chọn một hãng bay để chỉnh sửa hoặc khôi phục.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải hãng bay...</div>
            ) : items.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có hãng bay nào.</div>
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
                      <p className="font-black text-slate-900">{item.name}</p>
                      {!item.isActive ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-amber-100 text-amber-700">Tạm dừng</span> : null}
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{item.code} · {item.iataCode || '---'} · {item.icaoCode || '---'}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">{item.supportPhone || 'Chưa có hotline'}</p>
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
            <p className="text-lg font-black text-slate-900">{selectedId ? 'Cập nhật hãng bay' : 'Tạo hãng bay mới'}</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Giữ thông tin nhận diện thật gọn để frontend public hiển thị đúng thương hiệu.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.toUpperCase() }))} placeholder="Mã hãng" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Tên hãng bay" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.iataCode} onChange={(event) => setForm((current) => ({ ...current, iataCode: event.target.value.toUpperCase() }))} placeholder="IATA" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.icaoCode} onChange={(event) => setForm((current) => ({ ...current, icaoCode: event.target.value.toUpperCase() }))} placeholder="ICAO" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.supportPhone} onChange={(event) => setForm((current) => ({ ...current, supportPhone: event.target.value }))} placeholder="Hotline hỗ trợ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={form.supportEmail} onChange={(event) => setForm((current) => ({ ...current, supportEmail: event.target.value }))} placeholder="Email hỗ trợ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <div className="md:col-span-2">
              <ImageUploadField
                label=""
                value={form.logoUrl}
                onChange={(value) => setForm((current) => ({ ...current, logoUrl: value }))}
                onUpload={handleUploadLogo}
                uploading={uploadingLogo}
                placeholder="Logo URL"
                helperText="Hỗ trợ JPG, PNG, WEBP tối đa 10MB."
                previewAlt={form.name || 'Logo hãng bay'}
              />
            </div>
            <input value={form.websiteUrl} onChange={(event) => setForm((current) => ({ ...current, websiteUrl: event.target.value }))} placeholder="Website" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
          </div>
          <label className="flex items-center gap-3 text-sm font-bold text-slate-600">
            <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} />
            Hiển thị hãng bay trong các flow public
          </label>
          <div className="flex flex-wrap items-center gap-3">
            <button type="submit" disabled={saving} className="px-6 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black">
              {saving ? 'Đang lưu...' : (selectedId ? 'Cập nhật hãng' : 'Tạo hãng mới')}
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
