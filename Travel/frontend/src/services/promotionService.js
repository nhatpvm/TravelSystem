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

export function listAdminPromotions(params = {}) {
  return api.get(`/admin/promotions${toQuery(params)}`);
}

export function getAdminPromotion(id, params = {}) {
  return api.get(`/admin/promotions/${id}${toQuery(params)}`);
}

export function createAdminPromotion(payload) {
  return api.post('/admin/promotions', payload);
}

export function updateAdminPromotion(id, payload) {
  return api.put(`/admin/promotions/${id}`, payload);
}

export function deleteAdminPromotion(id) {
  return api.post(`/admin/promotions/${id}/delete`, {});
}

export function restoreAdminPromotion(id) {
  return api.post(`/admin/promotions/${id}/restore`, {});
}

export function activateAdminPromotion(id) {
  return api.post(`/admin/promotions/${id}/activate`, {});
}

export function pauseAdminPromotion(id) {
  return api.post(`/admin/promotions/${id}/pause`, {});
}

export function listTenantPromotions(params = {}) {
  return api.get(`/tenant/promotions${toQuery(params)}`);
}

export function getTenantPromotion(id, params = {}) {
  return api.get(`/tenant/promotions/${id}${toQuery(params)}`);
}

export function createTenantPromotion(payload) {
  return api.post('/tenant/promotions', payload);
}

export function updateTenantPromotion(id, payload) {
  return api.put(`/tenant/promotions/${id}`, payload);
}

export function deleteTenantPromotion(id) {
  return api.post(`/tenant/promotions/${id}/delete`, {});
}

export function restoreTenantPromotion(id) {
  return api.post(`/tenant/promotions/${id}/restore`, {});
}

export function activateTenantPromotion(id) {
  return api.post(`/tenant/promotions/${id}/activate`, {});
}

export function pauseTenantPromotion(id) {
  return api.post(`/tenant/promotions/${id}/pause`, {});
}

export function quotePromotion(payload) {
  return api.post('/promotions/quote', payload, { auth: false });
}
