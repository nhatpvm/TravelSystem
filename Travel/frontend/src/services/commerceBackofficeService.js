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

function withTenantHeader(tenantId) {
  return tenantId ? { headers: { 'X-TenantId': tenantId } } : undefined;
}

export function listAdminCommercePayments(params = {}) {
  return api.get(`/admin/commerce/payments${toQuery(params)}`);
}

export function listAdminCommerceBookings(params = {}) {
  return api.get(`/admin/commerce/bookings${toQuery(params)}`);
}

export function getAdminCommerceBooking(orderId) {
  return api.get(`/admin/commerce/bookings/${orderId}`);
}

export function listAdminCommerceRefunds(params = {}) {
  return api.get(`/admin/commerce/refunds${toQuery(params)}`);
}

export function approveAdminCommerceRefund(refundId, payload = {}, tenantId = null) {
  return api.post(`/admin/commerce/refunds/${refundId}/approve`, payload, withTenantHeader(tenantId));
}

export function rejectAdminCommerceRefund(refundId, payload = {}, tenantId = null) {
  return api.post(`/admin/commerce/refunds/${refundId}/reject`, payload, withTenantHeader(tenantId));
}

export function completeAdminCommerceRefund(refundId, payload = {}, tenantId = null) {
  return api.post(`/admin/commerce/refunds/${refundId}/complete`, payload, withTenantHeader(tenantId));
}

export function listAdminCommerceSupportTickets(params = {}) {
  return api.get(`/admin/commerce/support-tickets${toQuery(params)}`);
}

export function replyAdminCommerceSupportTicket(ticketId, payload = {}, tenantId = null) {
  return api.post(`/admin/commerce/support-tickets/${ticketId}/reply`, payload, withTenantHeader(tenantId));
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

export function getTenantCommerceReports(params = {}) {
  return api.get(`/tenant/commerce/reports${toQuery(params)}`);
}

export function listTenantCommerceBookings(params = {}) {
  return api.get(`/tenant/commerce/bookings${toQuery(params)}`);
}

export function getTenantCommerceBooking(orderId) {
  return api.get(`/tenant/commerce/bookings/${orderId}`);
}

export function listTenantCommerceReviews(params = {}) {
  return api.get(`/tenant/commerce/reviews${toQuery(params)}`);
}

export function upsertTenantCommercePayoutAccount(payload = {}) {
  return api.put('/tenant/commerce/payout-account', payload);
}
