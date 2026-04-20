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

export function createCustomerOrder(payload) {
  return api.post('/customer/orders', payload);
}

export function listCustomerOrders(params = {}) {
  return api.get(`/customer/orders${toQuery(params)}`);
}

export function getCustomerOrder(orderCode) {
  return api.get(`/customer/orders/${encodeURIComponent(orderCode)}`);
}

export function getCustomerOrderTimeline(orderCode) {
  return api.get(`/customer/orders/${encodeURIComponent(orderCode)}/timeline`);
}

export function getCustomerRefundEstimate(orderCode, params = {}) {
  return api.get(`/customer/orders/${encodeURIComponent(orderCode)}/refund-estimate${toQuery(params)}`);
}

export function getSupportedPaymentMethods() {
  return api.get('/customer/orders/payment-methods');
}

export function startCustomerPayment(orderCode, payload = {}) {
  return api.post(`/customer/orders/${encodeURIComponent(orderCode)}/payment-init`, payload);
}

export function syncCustomerPayment(orderCode) {
  return api.post(`/customer/orders/${encodeURIComponent(orderCode)}/payment-sync`, {});
}

export function getCustomerTicket(orderCode) {
  return api.get(`/customer/orders/${encodeURIComponent(orderCode)}/ticket`);
}

export function requestCustomerRefund(orderCode, payload) {
  return api.post(`/customer/orders/${encodeURIComponent(orderCode)}/refunds`, payload);
}

export function cancelCustomerOrder(orderCode) {
  return api.post(`/customer/orders/${encodeURIComponent(orderCode)}/cancel`, {});
}

export function listSavedPassengers() {
  return api.get('/customer/account/passengers');
}

export function listCheckoutDrafts(params = {}) {
  return api.get(`/customer/account/checkout-drafts${toQuery(params)}`);
}

export function upsertCheckoutDraft(payload) {
  return api.put('/customer/account/checkout-drafts', payload);
}

export function markCheckoutDraftResumed(id) {
  return api.post(`/customer/account/checkout-drafts/${id}/resume`, {});
}

export function deleteCheckoutDraft(id) {
  return api.delete(`/customer/account/checkout-drafts/${id}`);
}

export function listRecentViews(params = {}) {
  return api.get(`/customer/account/recent-views${toQuery(params)}`);
}

export function trackRecentView(payload) {
  return api.post('/customer/account/recent-views', payload);
}

export function listRecentSearches(params = {}) {
  return api.get(`/customer/account/recent-searches${toQuery(params)}`);
}

export function trackRecentSearch(payload) {
  return api.post('/customer/account/recent-searches', payload);
}

export function listPersonalizedSuggestions(params = {}) {
  return api.get(`/customer/account/personalized-suggestions${toQuery(params)}`);
}

export function getCustomerAccountPreferences() {
  return api.get('/customer/account/preferences');
}

export function updateCustomerAccountPreferences(payload) {
  return api.put('/customer/account/preferences', payload);
}

export function createSavedPassenger(payload) {
  return api.post('/customer/account/passengers', payload);
}

export function updateSavedPassenger(id, payload) {
  return api.put(`/customer/account/passengers/${id}`, payload);
}

export function deleteSavedPassenger(id) {
  return api.delete(`/customer/account/passengers/${id}`);
}

export function listWishlistItems() {
  return api.get('/customer/account/wishlist');
}

export function addWishlistItem(payload) {
  return api.post('/customer/account/wishlist', payload);
}

export function deleteWishlistItem(id) {
  return api.delete(`/customer/account/wishlist/${id}`);
}

export function listCustomerNotifications() {
  return api.get('/customer/account/notifications');
}

export function markCustomerNotificationRead(id) {
  return api.post(`/customer/account/notifications/${id}/read`, {});
}

export function markAllCustomerNotificationsRead() {
  return api.post('/customer/account/notifications/read-all', {});
}

export function listCustomerPayments() {
  return api.get('/customer/account/payments');
}

export function listCustomerVatInvoices() {
  return api.get('/customer/account/vat-invoices');
}

export function createCustomerVatInvoice(payload) {
  return api.post('/customer/account/vat-invoices', payload);
}

export function listCustomerSupportTickets(params = {}) {
  return api.get(`/customer/account/support-tickets${toQuery(params)}`);
}

export function createCustomerSupportTicket(payload) {
  return api.post('/customer/account/support-tickets', payload);
}
