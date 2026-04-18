import { api } from './api';

function toQuery(params = {}) {
  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return;
    }

    searchParams.set(key, String(value));
  });

  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

export function listUsers(params) {
  return api.get(`/admin/users${toQuery(params)}`);
}

export function getUser(id) {
  return api.get(`/admin/users/${id}`);
}

export function createUser(payload) {
  return api.post('/admin/users', payload);
}

export function updateUser(id, payload) {
  return api.put(`/admin/users/${id}`, payload);
}

export function setUserRoles(id, roles) {
  return api.put(`/admin/users/${id}/roles`, { roles });
}

export function setUserActive(id, isActive) {
  return api.put(`/admin/users/${id}/active${toQuery({ isActive })}`);
}

export function lockUser(id, minutes = 15) {
  return api.post(`/admin/users/${id}/lock`, { minutes });
}

export function unlockUser(id) {
  return api.post(`/admin/users/${id}/unlock`, {});
}

export function resetUserPassword(id, newPassword) {
  return api.post(`/admin/users/${id}/reset-password`, { newPassword });
}

export function listRoles(params) {
  return api.get(`/admin/roles${toQuery(params)}`);
}

export function getRole(id) {
  return api.get(`/admin/roles/${id}`);
}

export function createRole(payload) {
  return api.post('/admin/roles', payload);
}

export function deleteRole(id) {
  return api.delete(`/admin/roles/${id}`);
}

export function listPermissions(params) {
  return api.get(`/admin/auth/permissions${toQuery(params)}`);
}

export function getPermission(id, params) {
  return api.get(`/admin/auth/permissions/${id}${toQuery(params)}`);
}

export function createPermission(payload) {
  return api.post('/admin/auth/permissions', payload);
}

export function updatePermission(id, payload) {
  return api.put(`/admin/auth/permissions/${id}`, payload);
}

export function deletePermission(id) {
  return api.delete(`/admin/auth/permissions/${id}`);
}

export function restorePermission(id) {
  return api.post(`/admin/auth/permissions/${id}/restore`, {});
}

export function listRolePermissions(params) {
  return api.get(`/admin/auth/role-permissions${toQuery(params)}`);
}

export function getRolePermission(id, params) {
  return api.get(`/admin/auth/role-permissions/${id}${toQuery(params)}`);
}

export function createRolePermission(payload) {
  return api.post('/admin/auth/role-permissions', payload);
}

export function updateRolePermission(id, payload) {
  return api.put(`/admin/auth/role-permissions/${id}`, payload);
}

export function deleteRolePermission(id) {
  return api.delete(`/admin/auth/role-permissions/${id}`);
}

export function restoreRolePermission(id) {
  return api.post(`/admin/auth/role-permissions/${id}/restore`, {});
}

export function listUserPermissions(params) {
  return api.get(`/admin/auth/user-permissions${toQuery(params)}`);
}

export function getUserPermission(id, params) {
  return api.get(`/admin/auth/user-permissions/${id}${toQuery(params)}`);
}

export function createUserPermission(payload) {
  return api.post('/admin/auth/user-permissions', normalizeUserPermissionPayload(payload));
}

export function updateUserPermission(id, payload) {
  return api.put(`/admin/auth/user-permissions/${id}`, normalizeUserPermissionPayload(payload));
}

export function deleteUserPermission(id) {
  return api.delete(`/admin/auth/user-permissions/${id}`);
}

export function restoreUserPermission(id) {
  return api.post(`/admin/auth/user-permissions/${id}/restore`, {});
}

export function listAdminTenants(params) {
  return api.get(`/admin/tenants${toQuery(params)}`);
}

export function getAdminTenant(id) {
  return api.get(`/admin/tenants/${id}`);
}

export function updateAdminTenant(id, payload) {
  return api.put(`/admin/tenants/${id}`, payload);
}

export function deleteAdminTenant(id) {
  return api.delete(`/admin/tenants/${id}`);
}

export function restoreAdminTenant(id) {
  return api.post(`/admin/tenants/${id}/restore`, {});
}

export function listAdminTenantOnboarding(params) {
  return api.get(`/admin/tenant-onboarding${toQuery(params)}`);
}

export function getAdminTenantOnboarding(trackingCode) {
  return api.get(`/admin/tenant-onboarding/${trackingCode}`);
}

export function reviewAdminTenantOnboarding(trackingCode, payload) {
  return api.post(`/admin/tenant-onboarding/${trackingCode}/review`, payload);
}

function normalizeUserPermissionPayload(payload = {}) {
  return {
    ...payload,
    effect: payload.effect === 'Deny' || payload.effect === 2 ? 2 : 1,
  };
}
