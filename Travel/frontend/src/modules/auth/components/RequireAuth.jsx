import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import {
  canAccessAdmin,
  canAccessTenant,
  canAccessTenantModule,
  getPostLoginPath,
  getTenantAccessiblePath,
  hasTenantPermission,
} from '../types';
import { useAuthSession } from '../hooks/useAuthSession';

export default function RequireAuth({
  access = 'authenticated',
  tenantModule = null,
  tenantPermission = null,
  children,
}) {
  const location = useLocation();
  const session = useAuthSession();
  const { user, isAuthenticated, isReady } = session;

  if (isAuthenticated && !isReady) {
    return null;
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/auth/login" replace state={{ from: location.pathname }} />;
  }

  if (access === 'admin' && !canAccessAdmin(user)) {
    return <Navigate to={getPostLoginPath(user)} replace />;
  }

  if (access === 'tenant' && !canAccessTenant(user)) {
    return <Navigate to={getPostLoginPath(user)} replace />;
  }

  if (access === 'tenant' && tenantModule && !canAccessTenantModule(session, tenantModule)) {
    return <Navigate to={getTenantAccessiblePath(session)} replace />;
  }

  if (access === 'tenant' && tenantPermission && !hasTenantPermission(session, tenantPermission)) {
    return <Navigate to={getTenantAccessiblePath(session)} replace />;
  }

  return children ?? <Outlet />;
}
