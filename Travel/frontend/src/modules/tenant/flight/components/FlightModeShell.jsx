import React from 'react';
import FlightManagementPageShell from './FlightManagementPageShell';
import AdminFlightPageShell from '../../../admin/flight/components/AdminFlightPageShell';

const FlightModeShell = ({
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
      <AdminFlightPageShell
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
      </AdminFlightPageShell>
    );
  }

  return (
    <FlightManagementPageShell
      pageKey={pageKey}
      title={title}
      subtitle={subtitle}
      notice={notice}
      error={error}
      actions={actions}
    >
      {children}
    </FlightManagementPageShell>
  );
};

export default FlightModeShell;
