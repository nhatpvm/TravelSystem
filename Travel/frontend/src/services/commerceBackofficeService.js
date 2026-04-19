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

export function listAdminCommercePayments(params = {}) {
  return api.get(`/admin/commerce/payments${toQuery(params)}`);
}

export function listAdminCommerceRefunds(params = {}) {
  return api.get(`/admin/commerce/refunds${toQuery(params)}`);
}

export function approveAdminCommerceRefund(refundId, payload = {}) {
  return api.post(`/admin/commerce/refunds/${refundId}/approve`, payload);
}

export function rejectAdminCommerceRefund(refundId, payload = {}) {
  return api.post(`/admin/commerce/refunds/${refundId}/reject`, payload);
}

export function completeAdminCommerceRefund(refundId, payload = {}) {
  return api.post(`/admin/commerce/refunds/${refundId}/complete`, payload);
}

export function listAdminCommerceSupportTickets(params = {}) {
  return api.get(`/admin/commerce/support-tickets${toQuery(params)}`);
}

export function replyAdminCommerceSupportTicket(ticketId, payload = {}) {
  return api.post(`/admin/commerce/support-tickets/${ticketId}/reply`, payload);
}

export function getAdminCommerceSettlements() {
  return api.get('/admin/commerce/settlements');
}

export function generateAdminCommerceSettlementBatch(payload = {}) {
  return api.post('/admin/commerce/settlements/batches/generate', payload);
}

export function markAdminCommerceSettlementBatchPaid(batchId, payload = {}) {
  return api.post(`/admin/commerce/settlements/batches/${batchId}/mark-paid`, payload);
}

export function getTenantCommerceFinance() {
  return api.get('/tenant/commerce/finance');
}

export function upsertTenantCommercePayoutAccount(payload = {}) {
  return api.put('/tenant/commerce/payout-account', payload);
}
