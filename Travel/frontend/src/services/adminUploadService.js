import { api } from './api';

export function uploadAdminImage(file, options = {}) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('scope', options.scope || '');

  if (options.tenantId) {
    formData.append('tenantId', options.tenantId);
  }

  return api.post('/admin/uploads/images', formData, {
    headers: options.tenantId
      ? { 'X-TenantId': options.tenantId }
      : undefined,
  });
}
