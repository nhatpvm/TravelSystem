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

export function getAdminOpsOverview() {
  return api.get('/admin/ops/overview');
}

export function listAdminOpsAuditEvents(params = {}) {
  return api.get(`/admin/ops/audit-events${toQuery(params)}`);
}

export function listAdminOpsOutboxMessages(params = {}) {
  return api.get(`/admin/ops/outbox-messages${toQuery(params)}`);
}

export function getAdminOpsPromoReadiness() {
  return api.get('/admin/ops/promo-readiness');
}
