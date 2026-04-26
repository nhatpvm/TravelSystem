import { api } from './api';

export function uploadManagerImage(file, options = {}) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('scope', options.scope || '');

  return api.post('/manager/uploads/images', formData, {
    headers: options.tenantId
      ? { 'X-TenantId': options.tenantId }
      : undefined,
  });
}

export function uploadCustomerImage(file, options = {}) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('scope', options.scope || '');

  return api.post('/customer/uploads/images', formData);
}
