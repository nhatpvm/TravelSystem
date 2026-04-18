import React from 'react';
import BusManagementSecondaryNav from './BusManagementSecondaryNav';

const BusManagementPageShell = ({
  pageKey = 'overview',
  title,
  subtitle,
  notice = '',
  error = '',
  actions = null,
  children,
}) => (
  <div className="p-8 space-y-8">
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
      <div>
        <h1 className="text-3xl font-black text-slate-900 tracking-tight">{title}</h1>
        <p className="text-slate-500 font-medium mt-1">{subtitle}</p>
      </div>
      <div className="flex flex-wrap items-center gap-3">
        {actions}
      </div>
    </div>

    <BusManagementSecondaryNav currentKey={pageKey} />

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

export default BusManagementPageShell;
