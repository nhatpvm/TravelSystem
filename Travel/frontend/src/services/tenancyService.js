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

export function listTenantStaff(params) {
  return api.get(`/tenant/staff${toQuery(params)}`);
}

export function listTenantMemberships() {
  return api.get('/tenancy/memberships');
}

export function getCurrentTenantSettings() {
  return api.get('/tenancy/current-tenant/settings');
}

export function updateCurrentTenantSettings(payload) {
  return api.put('/tenancy/current-tenant/settings', payload);
}

export function getTenantStaff(id) {
  return api.get(`/tenant/staff/${id}`);
}

export function createTenantStaff(payload) {
  return api.post('/tenant/staff', payload);
}

export function updateTenantStaff(id, payload) {
  return api.put(`/tenant/staff/${id}`, payload);
}

export function deactivateTenantStaff(id) {
  return api.post(`/tenant/staff/${id}/deactivate`, {});
}

export function restoreTenantStaff(id) {
  return api.post(`/tenant/staff/${id}/restore`, {});
}

export function submitTenantOnboarding(formData) {
  return api.post('/tenancy/onboarding', formData, { auth: false });
}

export function getTenantOnboardingStatus(trackingCode) {
  return api.get(`/tenancy/onboarding/${trackingCode}`, { auth: false });
}
