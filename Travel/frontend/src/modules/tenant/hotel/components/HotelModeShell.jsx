import React from 'react';
import HotelManagementPageShell from './HotelManagementPageShell';
import AdminHotelPageShell from '../../../admin/hotel/components/AdminHotelPageShell';

const HotelModeShell = ({
  mode = 'tenant',
  adminScope = null,
  pageKey = 'overview',
  title,
  subtitle,
  notice = '',
  error = '',
  actions = null,
  children,
}) => {
  if (mode === 'admin') {
    return (
      <AdminHotelPageShell
        pageKey={pageKey}
        title={title}
        subtitle={subtitle}
        tenants={adminScope?.tenants || []}
        selectedTenantId={adminScope?.selectedTenantId || ''}
        setSelectedTenantId={adminScope?.setSelectedTenantId || (() => {})}
        selectedTenant={adminScope?.selectedTenant || null}
        notice={notice}
        error={adminScope?.scopeError || error}
        actions={actions}
      >
        {children}
      </AdminHotelPageShell>
    );
  }

  return (
    <HotelManagementPageShell
      pageKey={pageKey}
      title={title}
      subtitle={subtitle}
      notice={notice}
      error={error}
      actions={actions}
    >
      {children}
    </HotelManagementPageShell>
  );
};

export default HotelModeShell;
