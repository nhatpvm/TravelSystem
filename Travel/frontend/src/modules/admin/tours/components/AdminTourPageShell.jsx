import React from 'react';
import AdminTourSecondaryNav from './AdminTourSecondaryNav';

const AdminTourPageShell = ({
  pageKey = 'tours',
  title,
  subtitle,
  tenants = [],
  selectedTenantId = '',
  setSelectedTenantId = () => {},
  selectedTenant = null,
  error = '',
  notice = '',
  actions = null,
  children,
}) => (
  <div className="space-y-8 p-8">
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
      <div>
        <h1 className="text-3xl font-black text-slate-900 tracking-tight">{title}</h1>
        <p className="text-slate-500 font-medium mt-1">{subtitle}</p>
      </div>
      <div className="flex flex-wrap items-center gap-3">
        <div className="px-4 py-3 bg-white rounded-2xl border border-slate-100 shadow-sm">
          <select
            value={selectedTenantId}
            onChange={(event) => setSelectedTenantId(event.target.value)}
            className="bg-transparent outline-none text-sm font-bold text-slate-700"
          >
            {tenants.map((item) => (
              <option key={item.id} value={item.id}>{item.name}</option>
            ))}
          </select>
        </div>
        {actions}
      </div>
    </div>

    <AdminTourSecondaryNav currentKey={pageKey} />

    {selectedTenant && (
      <div className="rounded-2xl border border-sky-100 bg-sky-50 px-5 py-4 text-sm font-bold text-sky-700">
        Đang quản lý tour cho tenant: {selectedTenant.name}
      </div>
    )}

    {notice && (
      <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
        {notice}
      </div>
    )}

    {error && (
      <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
        {error}
      </div>
    )}

    {children}
  </div>
);

export default AdminTourPageShell;
