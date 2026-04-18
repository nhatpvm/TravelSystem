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

function withTenantHeaders(tenantId, options = {}) {
  if (!tenantId) {
    return options;
  }

  return {
    ...options,
    headers: {
      ...(options.headers || {}),
      'X-TenantId': tenantId,
    },
  };
}

export function listPublicTours(params = {}) {
  return api.get(`/tours${toQuery(params)}`, { auth: false });
}

export function getPublicTourById(id) {
  return api.get(`/tours/${id}`, { auth: false });
}

export function getPublicTourBySlug(slug, params = {}) {
  return api.get(`/tours/slug/${slug}${toQuery(params)}`, { auth: false });
}

export function listPublicTourAvailability(tourId, params = {}) {
  return api.get(`/tours/${tourId}/availability${toQuery(params)}`, { auth: false });
}

export function getPublicTourGallery(tourId, params = {}) {
  return api.get(`/tours/${tourId}/gallery${toQuery(params)}`, { auth: false });
}

export function listPublicTourSchedules(tourId, params = {}) {
  return api.get(`/tours/${tourId}/schedules${toQuery(params)}`, { auth: false });
}

export function getPublicTourSchedule(tourId, scheduleId, params = {}) {
  return api.get(`/tours/${tourId}/schedules/${scheduleId}${toQuery(params)}`, { auth: false });
}

export function listPublicTourReviews(tourId, params = {}) {
  return api.get(`/tours/${tourId}/reviews${toQuery(params)}`, { auth: false });
}

export function listPublicTourFaqs(tourId, params = {}) {
  return api.get(`/tours/${tourId}/faqs${toQuery(params)}`, { auth: false });
}

export function listPublicTourItinerary(tourId, params = {}) {
  return api.get(`/tours/${tourId}/itinerary${toQuery(params)}`, { auth: false });
}

export function listPublicTourPolicies(tourId, params = {}) {
  return api.get(`/tours/${tourId}/policies${toQuery(params)}`, { auth: false });
}

export function listPublicTourAddons(tourId, params = {}) {
  return api.get(`/tours/${tourId}/addons${toQuery(params)}`, { auth: false });
}

export function quoteTour(tourId, payload) {
  return api.post(`/tours/${tourId}/quote`, payload, { auth: false });
}

export function createTourReservation(tourId, payload) {
  return api.post(`/tours/${tourId}/package-reservations`, payload);
}

export function getTourReservation(tourId, reservationId) {
  return api.get(`/tours/${tourId}/package-reservations/${reservationId}`);
}

export function releaseTourReservation(tourId, reservationId) {
  return api.post(`/tours/${tourId}/package-reservations/${reservationId}/release`, {});
}

export function confirmTourBooking(tourId, payload) {
  return api.post(`/tours/${tourId}/package-bookings/confirm`, payload);
}

export function getTourBooking(tourId, bookingId) {
  return api.get(`/tours/${tourId}/package-bookings/${bookingId}`);
}

export function cancelTourBookingItems(tourId, bookingId, payload) {
  return api.post(`/tours/${tourId}/package-bookings/${bookingId}/cancellations/items`, payload);
}

export function getTourBookingRefunds(tourId, bookingId) {
  return api.get(`/tours/${tourId}/package-bookings/${bookingId}/refunds`);
}

export function listMyTourBookings(params = {}) {
  return api.get(`/my/tour-package-bookings${toQuery(params)}`);
}

export function getMyTourBooking(bookingId) {
  return api.get(`/my/tour-package-bookings/${bookingId}`);
}

export function listManagerTours(params = {}) {
  return api.get(`/ql-tour/tours${toQuery(params)}`);
}

export function getManagerTour(id, params = {}) {
  return api.get(`/ql-tour/tours/${id}${toQuery(params)}`);
}

export function createManagerTour(payload) {
  return api.post('/ql-tour/tours', payload);
}

export function updateManagerTour(id, payload) {
  return api.put(`/ql-tour/tours/${id}`, payload);
}

export function toggleManagerTourAction(id, action) {
  return api.post(`/ql-tour/tours/${id}/${action}`, {});
}

export function listManagerTourSchedules(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/schedules${toQuery(params)}`);
}

export function createManagerTourSchedule(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/schedules`, payload);
}

export function updateManagerTourSchedule(tourId, scheduleId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/schedules/${scheduleId}`, payload);
}

export function toggleManagerTourScheduleAction(tourId, scheduleId, action) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/${action}`, {});
}

export function listManagerTourPrices(tourId, scheduleId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/prices${toQuery(params)}`);
}

export function createManagerTourPrice(tourId, scheduleId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/prices`, payload);
}

export function updateManagerTourPrice(tourId, scheduleId, priceId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/prices/${priceId}`, payload);
}

export function toggleManagerTourPriceAction(tourId, scheduleId, priceId, action) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/prices/${priceId}/${action}`, {});
}

export function getManagerTourCapacity(tourId, scheduleId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/capacity${toQuery(params)}`);
}

export function createManagerTourCapacity(tourId, scheduleId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/capacity`, payload);
}

export function updateManagerTourCapacity(tourId, scheduleId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/capacity`, payload);
}

export function toggleManagerTourCapacityAction(tourId, scheduleId, action) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/capacity/${action}`, {});
}

export function listManagerTourPackages(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/packages${toQuery(params)}`);
}

export function createManagerTourPackage(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/packages`, payload);
}

export function updateManagerTourPackage(tourId, packageId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/packages/${packageId}`, payload);
}

export function toggleManagerTourPackageAction(tourId, packageId, action) {
  return api.post(`/ql-tour/tours/${tourId}/packages/${packageId}/${action}`, {});
}

export function listManagerTourBookings(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings${toQuery(params)}`);
}

export function getManagerTourBooking(tourId, bookingId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}`);
}

export function getManagerTourBookingTimeline(tourId, bookingId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/timeline`);
}

export function cancelManagerTourBookingItems(tourId, bookingId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/cancellations/items`, payload);
}

export function cancelManagerTourBookingRemaining(tourId, bookingId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/cancellations/bulk`, payload);
}

export function getManagerTourBookingCancellation(tourId, bookingId, cancellationId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/cancellations/${cancellationId}`);
}

export function getManagerTourBookingRefunds(tourId, bookingId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/refunds`);
}

export function markManagerTourRefundReady(tourId, bookingId, refundId, payload = {}) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/refunds/${refundId}/ready`, payload);
}

export function rejectManagerTourRefund(tourId, bookingId, refundId, payload = {}) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/refunds/${refundId}/reject`, payload);
}

export function getManagerTourBookingItinerary(tourId, bookingId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/itinerary`);
}

export function getManagerTourBookingVoucher(tourId, bookingId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/voucher`);
}

export function listManagerTourBookingReschedules(tourId, bookingId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/reschedules`);
}

export function holdManagerTourBookingReschedule(tourId, bookingId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/reschedules`, payload);
}

export function getManagerTourBookingReschedule(tourId, bookingId, rescheduleId) {
  return api.get(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/reschedules/${rescheduleId}`);
}

export function confirmManagerTourBookingReschedule(tourId, bookingId, rescheduleId, payload = {}) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/reschedules/${rescheduleId}/confirm`, payload);
}

export function releaseManagerTourBookingReschedule(tourId, bookingId, rescheduleId) {
  return api.post(`/ql-tour/tours/${tourId}/package-bookings/${bookingId}/reschedules/${rescheduleId}/release`, {});
}

export function listManagerTourReviews(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/reviews${toQuery(params)}`);
}

export function getManagerTourReview(tourId, reviewId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/reviews/${reviewId}${toQuery(params)}`);
}

export function replyManagerTourReview(tourId, reviewId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/reviews/${reviewId}/reply`, payload);
}

export function approveManagerTourReview(tourId, reviewId, payload = {}) {
  return api.post(`/ql-tour/tours/${tourId}/reviews/${reviewId}/approve`, payload);
}

export function rejectManagerTourReview(tourId, reviewId, payload = {}) {
  return api.post(`/ql-tour/tours/${tourId}/reviews/${reviewId}/reject`, payload);
}

export function hideManagerTourReview(tourId, reviewId) {
  return api.post(`/ql-tour/tours/${tourId}/reviews/${reviewId}/hide`, {});
}

export function makeManagerTourReviewPublic(tourId, reviewId) {
  return api.post(`/ql-tour/tours/${tourId}/reviews/${reviewId}/public`, {});
}

export function listManagerTourContacts(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/contacts${toQuery(params)}`);
}

export function getManagerTourContact(tourId, contactId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/contacts/${contactId}${toQuery(params)}`);
}

export function createManagerTourContact(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/contacts`, payload);
}

export function updateManagerTourContact(tourId, contactId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/contacts/${contactId}`, payload);
}

export function toggleManagerTourContactAction(tourId, contactId, action) {
  return api.post(`/ql-tour/tours/${tourId}/contacts/${contactId}/${action}`, {});
}

export function listManagerTourImages(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/images${toQuery(params)}`);
}

export function getManagerTourImage(tourId, imageId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/images/${imageId}${toQuery(params)}`);
}

export function createManagerTourImage(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/images`, payload);
}

export function updateManagerTourImage(tourId, imageId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/images/${imageId}`, payload);
}

export function toggleManagerTourImageAction(tourId, imageId, action) {
  return api.post(`/ql-tour/tours/${tourId}/images/${imageId}/${action}`, {});
}

export function listManagerTourPolicies(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/policies${toQuery(params)}`);
}

export function getManagerTourPolicy(tourId, policyId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/policies/${policyId}${toQuery(params)}`);
}

export function createManagerTourPolicy(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/policies`, payload);
}

export function updateManagerTourPolicy(tourId, policyId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/policies/${policyId}`, payload);
}

export function toggleManagerTourPolicyAction(tourId, policyId, action) {
  return api.post(`/ql-tour/tours/${tourId}/policies/${policyId}/${action}`, {});
}

export function listManagerTourFaqEntries(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/faqs${toQuery(params)}`);
}

export function getManagerTourFaqEntry(tourId, faqId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/faqs/${faqId}${toQuery(params)}`);
}

export function createManagerTourFaqEntry(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/faqs`, payload);
}

export function updateManagerTourFaqEntry(tourId, faqId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/faqs/${faqId}`, payload);
}

export function toggleManagerTourFaqAction(tourId, faqId, action) {
  return api.post(`/ql-tour/tours/${tourId}/faqs/${faqId}/${action}`, {});
}

export function listManagerTourItineraryDays(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/itinerary${toQuery(params)}`);
}

export function getManagerTourItineraryDay(tourId, dayId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/itinerary/${dayId}${toQuery(params)}`);
}

export function createManagerTourItineraryDay(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/itinerary/days`, payload);
}

export function updateManagerTourItineraryDay(tourId, dayId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/itinerary/days/${dayId}`, payload);
}

export function toggleManagerTourItineraryDayAction(tourId, dayId, action) {
  return api.post(`/ql-tour/tours/${tourId}/itinerary/days/${dayId}/${action}`, {});
}

export function listManagerTourItineraryItems(tourId, dayId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/itinerary/days/${dayId}/items${toQuery(params)}`);
}

export function replaceManagerTourItineraryItems(tourId, dayId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/itinerary/days/${dayId}/items`, payload);
}

export function reorderManagerTourItineraryDays(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/itinerary/days/reorder`, payload);
}

export function listManagerTourPickupPoints(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points${toQuery(params)}`);
}

export function getManagerTourPickupPoint(tourId, pointId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points/${pointId}${toQuery(params)}`);
}

export function createManagerTourPickupPoint(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points`, payload);
}

export function updateManagerTourPickupPoint(tourId, pointId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points/${pointId}`, payload);
}

export function toggleManagerTourPickupPointAction(tourId, pointId, action) {
  return api.post(`/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points/${pointId}/${action}`, {});
}

export function listManagerTourDropoffPoints(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points${toQuery(params)}`);
}

export function getManagerTourDropoffPoint(tourId, pointId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points/${pointId}${toQuery(params)}`);
}

export function createManagerTourDropoffPoint(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points`, payload);
}

export function updateManagerTourDropoffPoint(tourId, pointId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points/${pointId}`, payload);
}

export function toggleManagerTourDropoffPointAction(tourId, pointId, action) {
  return api.post(`/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points/${pointId}/${action}`, {});
}

export function listManagerTourAddons(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/addons${toQuery(params)}`);
}

export function getManagerTourAddon(tourId, addonId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/addons/${addonId}${toQuery(params)}`);
}

export function createManagerTourAddon(tourId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/addons`, payload);
}

export function updateManagerTourAddon(tourId, addonId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/addons/${addonId}`, payload);
}

export function toggleManagerTourAddonAction(tourId, addonId, action) {
  return api.post(`/ql-tour/tours/${tourId}/addons/${addonId}/${action}`, {});
}

export function listManagerTourPackageComponents(tourId, packageId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/packages/${packageId}/components${toQuery(params)}`);
}

export function getManagerTourPackageComponent(tourId, packageId, componentId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}${toQuery(params)}`);
}

export function createManagerTourPackageComponent(tourId, packageId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/packages/${packageId}/components`, payload);
}

export function updateManagerTourPackageComponent(tourId, packageId, componentId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}`, payload);
}

export function toggleManagerTourPackageComponentAction(tourId, packageId, componentId, action) {
  return api.post(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}/${action}`, {});
}

export function listManagerTourPackageOptions(tourId, packageId, componentId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}/options${toQuery(params)}`);
}

export function getManagerTourPackageOption(tourId, packageId, componentId, optionId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}/options/${optionId}${toQuery(params)}`);
}

export function createManagerTourPackageOption(tourId, packageId, componentId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}/options`, payload);
}

export function updateManagerTourPackageOption(tourId, packageId, componentId, optionId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}/options/${optionId}`, payload);
}

export function toggleManagerTourPackageOptionAction(tourId, packageId, componentId, optionId, action) {
  return api.post(`/ql-tour/tours/${tourId}/packages/${packageId}/components/${componentId}/options/${optionId}/${action}`, {});
}

export function listManagerTourPackageOverrides(tourId, scheduleId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides${toQuery(params)}`);
}

export function getManagerTourPackageOverride(tourId, scheduleId, overrideId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides/${overrideId}${toQuery(params)}`);
}

export function createManagerTourPackageOverride(tourId, scheduleId, payload) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides`, payload);
}

export function updateManagerTourPackageOverride(tourId, scheduleId, overrideId, payload) {
  return api.put(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides/${overrideId}`, payload);
}

export function toggleManagerTourPackageOverrideAction(tourId, scheduleId, overrideId, action) {
  return api.post(`/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides/${overrideId}/${action}`, {});
}

export function getManagerTourPackageReportingOverview(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/package-reporting/overview${toQuery(params)}`);
}

export function getManagerTourPackageReportingSourceBreakdown(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/package-reporting/source-breakdown${toQuery(params)}`);
}

export function listManagerTourPackageAuditEvents(tourId, params = {}) {
  return api.get(`/ql-tour/tours/${tourId}/package-reporting/audit-events${toQuery(params)}`);
}

export function listAdminTours(params = {}) {
  return api.get(`/admin/tours${toQuery(params)}`);
}

export function getAdminTour(id, params = {}) {
  return api.get(`/admin/tours/${id}${toQuery(params)}`);
}

export function listAdminTourReviews(params = {}) {
  return api.get(`/admin/tour-reviews${toQuery(params)}`);
}

export function getAdminTourReview(id, params = {}) {
  return api.get(`/admin/tour-reviews/${id}${toQuery(params)}`);
}

export function updateAdminTour(id, payload, tenantId) {
  return api.put(`/admin/tours/${id}`, payload, withTenantHeaders(tenantId));
}

export function toggleAdminTourAction(id, action, tenantId) {
  return api.post(`/admin/tours/${id}/${action}`, {}, withTenantHeaders(tenantId));
}

export function replyAdminTourReview(id, payload, tenantId) {
  return api.post(`/admin/tour-reviews/${id}/reply`, payload, withTenantHeaders(tenantId));
}

export function approveAdminTourReview(id, payload = {}, tenantId) {
  return api.post(`/admin/tour-reviews/${id}/approve`, payload, withTenantHeaders(tenantId));
}

export function rejectAdminTourReview(id, payload = {}, tenantId) {
  return api.post(`/admin/tour-reviews/${id}/reject`, payload, withTenantHeaders(tenantId));
}

export function hideAdminTourReview(id, tenantId) {
  return api.post(`/admin/tour-reviews/${id}/hide`, {}, withTenantHeaders(tenantId));
}

export function makeAdminTourReviewPublic(id, tenantId) {
  return api.post(`/admin/tour-reviews/${id}/public`, {}, withTenantHeaders(tenantId));
}

export function listAdminTourSchedules(params = {}) {
  return api.get(`/admin/tour-schedules${toQuery(params)}`);
}

export function getAdminTourSchedule(id, params = {}) {
  return api.get(`/admin/tour-schedules/${id}${toQuery(params)}`);
}

export function createAdminTourSchedule(payload, tenantId) {
  return api.post('/admin/tour-schedules', payload, withTenantHeaders(tenantId));
}

export function updateAdminTourSchedule(id, payload, tenantId) {
  return api.put(`/admin/tour-schedules/${id}`, payload, withTenantHeaders(tenantId));
}

export function toggleAdminTourScheduleAction(id, action, tenantId) {
  return api.post(`/admin/tour-schedules/${id}/${action}`, {}, withTenantHeaders(tenantId));
}

export function listAdminTourFaqs(params = {}) {
  return api.get(`/admin/tour-faqs${toQuery(params)}`);
}

export function getAdminTourFaq(id, params = {}) {
  return api.get(`/admin/tour-faqs/${id}${toQuery(params)}`);
}

export function createAdminTourFaq(payload, tenantId) {
  return api.post('/admin/tour-faqs', payload, withTenantHeaders(tenantId));
}

export function updateAdminTourFaq(id, payload, tenantId) {
  return api.put(`/admin/tour-faqs/${id}`, payload, withTenantHeaders(tenantId));
}

export function toggleAdminTourFaqAction(id, action, tenantId) {
  return api.post(`/admin/tour-faqs/${id}/${action}`, {}, withTenantHeaders(tenantId));
}
