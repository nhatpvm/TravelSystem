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

function createCrud(basePath) {
  return {
    list(params = {}, options) {
      return api.get(`${basePath}${toQuery(params)}`, options);
    },
    get(id, params = {}, options) {
      return api.get(`${basePath}/${id}${toQuery(params)}`, options);
    },
    create(payload, options) {
      return api.post(basePath, payload, options);
    },
    update(id, payload, options) {
      return api.put(`${basePath}/${id}`, payload, options);
    },
    remove(id, options) {
      return api.post(`${basePath}/${id}/delete`, {}, options);
    },
    restore(id, options) {
      return api.post(`${basePath}/${id}/restore`, {}, options);
    },
    activate(id, options) {
      return api.post(`${basePath}/${id}/activate`, {}, options);
    },
    deactivate(id, options) {
      return api.post(`${basePath}/${id}/deactivate`, {}, options);
    },
  };
}

function createScopedCrud(basePath) {
  const crud = createCrud(basePath);

  return {
    list(params = {}, tenantId) {
      return crud.list(params, withTenantHeaders(tenantId));
    },
    get(id, params = {}, tenantId) {
      return crud.get(id, params, withTenantHeaders(tenantId));
    },
    create(payload, tenantId) {
      return crud.create(payload, withTenantHeaders(tenantId));
    },
    update(id, payload, tenantId) {
      return crud.update(id, payload, withTenantHeaders(tenantId));
    },
    remove(id, tenantId) {
      return crud.remove(id, withTenantHeaders(tenantId));
    },
    restore(id, tenantId) {
      return crud.restore(id, withTenantHeaders(tenantId));
    },
    activate(id, tenantId) {
      return crud.activate(id, withTenantHeaders(tenantId));
    },
    deactivate(id, tenantId) {
      return crud.deactivate(id, withTenantHeaders(tenantId));
    },
  };
}

function createPolicyApi(basePath, scopeTenant = false) {
  const getOptions = (tenantId) => (scopeTenant ? withTenantHeaders(tenantId) : undefined);
  const buildPath = (section) => `${basePath}/${section}`;

  return {
    list(section, params = {}, tenantId) {
      return api.get(`${buildPath(section)}${toQuery(params)}`, getOptions(tenantId));
    },
    get(section, id, params = {}, tenantId) {
      return api.get(`${buildPath(section)}/${id}${toQuery(params)}`, getOptions(tenantId));
    },
    create(section, payload, tenantId) {
      return api.post(buildPath(section), payload, getOptions(tenantId));
    },
    update(section, id, payload, tenantId) {
      return api.put(`${buildPath(section)}/${id}`, payload, getOptions(tenantId));
    },
    remove(section, id, tenantId) {
      return api.post(`${buildPath(section)}/${id}/delete`, {}, getOptions(tenantId));
    },
    restore(section, id, tenantId) {
      return api.post(`${buildPath(section)}/${id}/restore`, {}, getOptions(tenantId));
    },
    activate(section, id, tenantId) {
      return api.post(`${buildPath(section)}/${id}/activate`, {}, getOptions(tenantId));
    },
    deactivate(section, id, tenantId) {
      return api.post(`${buildPath(section)}/${id}/deactivate`, {}, getOptions(tenantId));
    },
  };
}

function createAriApi(basePath, scopeTenant = false) {
  const getOptions = (tenantId) => (scopeTenant ? withTenantHeaders(tenantId) : undefined);

  return {
    getInventory(roomTypeId, params = {}, tenantId) {
      return api.get(`${basePath}/room-types/${roomTypeId}/inventory${toQuery(params)}`, getOptions(tenantId));
    },
    upsertInventory(roomTypeId, payload, tenantId) {
      return api.put(`${basePath}/room-types/${roomTypeId}/inventory/bulk`, payload, getOptions(tenantId));
    },
    deleteInventoryRange(roomTypeId, payload, tenantId) {
      return api.post(`${basePath}/room-types/${roomTypeId}/inventory/delete-range`, payload, getOptions(tenantId));
    },
    getDailyRates(mappingId, params = {}, tenantId) {
      return api.get(`${basePath}/rate-plan-room-types/${mappingId}/daily-rates${toQuery(params)}`, getOptions(tenantId));
    },
    upsertDailyRates(mappingId, payload, tenantId) {
      return api.put(`${basePath}/rate-plan-room-types/${mappingId}/daily-rates/bulk`, payload, getOptions(tenantId));
    },
    deleteDailyRatesRange(mappingId, payload, tenantId) {
      return api.post(`${basePath}/rate-plan-room-types/${mappingId}/daily-rates/delete-range`, payload, getOptions(tenantId));
    },
  };
}

function getScoped(path, params = {}, tenantId) {
  return api.get(`${path}${toQuery(params)}`, withTenantHeaders(tenantId));
}

function postScoped(path, payload, tenantId) {
  return api.post(path, payload, withTenantHeaders(tenantId));
}

function putScoped(path, payload, tenantId) {
  return api.put(path, payload, withTenantHeaders(tenantId));
}

const managerHotelsCrud = createCrud('/qlks/hotels');
const managerRoomTypesCrud = createCrud('/qlks/room-types');
const managerRatePlansCrud = createCrud('/qlks/rate-plans');
const managerExtraServicesCrud = createCrud('/qlks/extra-services');

const adminHotelsCrud = createScopedCrud('/admin/hotels');
const adminRoomTypesCrud = createScopedCrud('/admin/room-types');
const adminRatePlansCrud = createScopedCrud('/admin/rate-plans');
const adminExtraServicesCrud = createScopedCrud('/admin/extra-services');
const adminContactsCrud = createScopedCrud('/admin/hotel-contacts');
const adminImagesCrud = createScopedCrud('/admin/hotel-images');
const adminHotelAmenitiesCrud = createScopedCrud('/admin/hotel-amenities');
const adminRoomAmenitiesCrud = createScopedCrud('/admin/room-amenities');
const adminMealPlansCrud = createScopedCrud('/admin/meal-plans');
const adminBedTypesCrud = createScopedCrud('/admin/bed-types');
const adminRoomTypeImagesCrud = createScopedCrud('/admin/room-type-images');
const adminRoomTypePoliciesCrud = createScopedCrud('/admin/room-type-policies');
const adminPromoOverridesCrud = createScopedCrud('/admin/promo-rate-overrides');
const adminReviewsCrud = createScopedCrud('/admin/hotel-reviews');

const managerPoliciesApi = createPolicyApi('/qlks/hotel-policies');
const adminPoliciesApi = createPolicyApi('/admin/hotel-policies', true);
const managerAriApi = createAriApi('/qlks/ari');
const adminAriApi = createAriApi('/admin/ari', true);

const POLICY_SECTIONS = {
  cancellation: 'cancellation-policies',
  checkInOut: 'check-in-out-rules',
  property: 'property-policies',
};

export function listPublicHotels(params = {}) {
  return api.get(`/hotels${toQuery(params)}`, { auth: false });
}

export function getPublicHotel(id, params = {}) {
  return api.get(`/hotels/${id}${toQuery(params)}`, { auth: false });
}

export function getPublicHotelBySlug(slug, params = {}) {
  return api.get(`/hotels/slug/${slug}${toQuery(params)}`, { auth: false });
}

export function getHotelAvailability(hotelId, params = {}) {
  return api.get(`/hotels/${hotelId}/availability${toQuery(params)}`, { auth: false });
}

export function getHotelAvailabilityBySlug(slug, params = {}) {
  return api.get(`/hotels/slug/${slug}/availability${toQuery(params)}`, { auth: false });
}

export function getHotelGallery(hotelId) {
  return api.get(`/hotels/${hotelId}/gallery`, { auth: false });
}

export function getHotelGalleryBySlug(slug) {
  return api.get(`/hotels/slug/${slug}/gallery`, { auth: false });
}

export function getHotelReviews(hotelId, params = {}) {
  return api.get(`/hotels/${hotelId}/reviews${toQuery(params)}`, { auth: false });
}

export function getHotelReviewsBySlug(slug, params = {}) {
  return api.get(`/hotels/slug/${slug}/reviews${toQuery(params)}`, { auth: false });
}

export function listManagedHotels(params = {}) { return managerHotelsCrud.list(params); }
export function getManagedHotel(id, params = {}) { return managerHotelsCrud.get(id, params); }
export function createManagedHotel(payload) { return managerHotelsCrud.create(payload); }
export function updateManagedHotel(id, payload) { return managerHotelsCrud.update(id, payload); }
export function deleteManagedHotel(id) { return managerHotelsCrud.remove(id); }
export function restoreManagedHotel(id) { return managerHotelsCrud.restore(id); }
export function activateManagedHotel(id) { return managerHotelsCrud.activate(id); }
export function deactivateManagedHotel(id) { return managerHotelsCrud.deactivate(id); }

export function listManagedRoomTypes(params = {}) { return managerRoomTypesCrud.list(params); }
export function getManagedRoomType(id, params = {}) { return managerRoomTypesCrud.get(id, params); }
export function createManagedRoomType(payload) { return managerRoomTypesCrud.create(payload); }
export function updateManagedRoomType(id, payload) { return managerRoomTypesCrud.update(id, payload); }
export function deleteManagedRoomType(id) { return managerRoomTypesCrud.remove(id); }
export function restoreManagedRoomType(id) { return managerRoomTypesCrud.restore(id); }
export function activateManagedRoomType(id) { return managerRoomTypesCrud.activate(id); }
export function deactivateManagedRoomType(id) { return managerRoomTypesCrud.deactivate(id); }

export function listManagedRatePlans(params = {}) { return managerRatePlansCrud.list(params); }
export function getManagedRatePlan(id, params = {}) { return managerRatePlansCrud.get(id, params); }
export function createManagedRatePlan(payload) { return managerRatePlansCrud.create(payload); }
export function updateManagedRatePlan(id, payload) { return managerRatePlansCrud.update(id, payload); }
export function deleteManagedRatePlan(id) { return managerRatePlansCrud.remove(id); }
export function restoreManagedRatePlan(id) { return managerRatePlansCrud.restore(id); }
export function activateManagedRatePlan(id) { return managerRatePlansCrud.activate(id); }
export function deactivateManagedRatePlan(id) { return managerRatePlansCrud.deactivate(id); }

export function listManagedExtraServices(params = {}) { return managerExtraServicesCrud.list(params); }
export function getManagedExtraService(id, params = {}) { return managerExtraServicesCrud.get(id, params); }
export function createManagedExtraService(payload) { return managerExtraServicesCrud.create(payload); }
export function updateManagedExtraService(id, payload) { return managerExtraServicesCrud.update(id, payload); }
export function deleteManagedExtraService(id) { return managerExtraServicesCrud.remove(id); }
export function restoreManagedExtraService(id) { return managerExtraServicesCrud.restore(id); }
export function activateManagedExtraService(id) { return managerExtraServicesCrud.activate(id); }
export function deactivateManagedExtraService(id) { return managerExtraServicesCrud.deactivate(id); }
export function getManagedExtraServicePrices(id) { return api.get(`/qlks/extra-services/${id}/prices`); }
export function replaceManagedExtraServicePrices(id, payload) { return api.put(`/qlks/extra-services/${id}/prices`, payload); }

export function listManagedCancellationPolicies(params = {}) { return managerPoliciesApi.list(POLICY_SECTIONS.cancellation, params); }
export function getManagedCancellationPolicy(id, params = {}) { return managerPoliciesApi.get(POLICY_SECTIONS.cancellation, id, params); }
export function createManagedCancellationPolicy(payload) { return managerPoliciesApi.create(POLICY_SECTIONS.cancellation, payload); }
export function updateManagedCancellationPolicy(id, payload) { return managerPoliciesApi.update(POLICY_SECTIONS.cancellation, id, payload); }
export function deleteManagedCancellationPolicy(id) { return managerPoliciesApi.remove(POLICY_SECTIONS.cancellation, id); }
export function restoreManagedCancellationPolicy(id) { return managerPoliciesApi.restore(POLICY_SECTIONS.cancellation, id); }
export function activateManagedCancellationPolicy(id) { return managerPoliciesApi.activate(POLICY_SECTIONS.cancellation, id); }
export function deactivateManagedCancellationPolicy(id) { return managerPoliciesApi.deactivate(POLICY_SECTIONS.cancellation, id); }

export function listManagedCheckInOutRules(params = {}) { return managerPoliciesApi.list(POLICY_SECTIONS.checkInOut, params); }
export function getManagedCheckInOutRule(id, params = {}) { return managerPoliciesApi.get(POLICY_SECTIONS.checkInOut, id, params); }
export function createManagedCheckInOutRule(payload) { return managerPoliciesApi.create(POLICY_SECTIONS.checkInOut, payload); }
export function updateManagedCheckInOutRule(id, payload) { return managerPoliciesApi.update(POLICY_SECTIONS.checkInOut, id, payload); }
export function deleteManagedCheckInOutRule(id) { return managerPoliciesApi.remove(POLICY_SECTIONS.checkInOut, id); }
export function restoreManagedCheckInOutRule(id) { return managerPoliciesApi.restore(POLICY_SECTIONS.checkInOut, id); }
export function activateManagedCheckInOutRule(id) { return managerPoliciesApi.activate(POLICY_SECTIONS.checkInOut, id); }
export function deactivateManagedCheckInOutRule(id) { return managerPoliciesApi.deactivate(POLICY_SECTIONS.checkInOut, id); }

export function listManagedPropertyPolicies(params = {}) { return managerPoliciesApi.list(POLICY_SECTIONS.property, params); }
export function getManagedPropertyPolicy(id, params = {}) { return managerPoliciesApi.get(POLICY_SECTIONS.property, id, params); }
export function createManagedPropertyPolicy(payload) { return managerPoliciesApi.create(POLICY_SECTIONS.property, payload); }
export function updateManagedPropertyPolicy(id, payload) { return managerPoliciesApi.update(POLICY_SECTIONS.property, id, payload); }
export function deleteManagedPropertyPolicy(id) { return managerPoliciesApi.remove(POLICY_SECTIONS.property, id); }
export function restoreManagedPropertyPolicy(id) { return managerPoliciesApi.restore(POLICY_SECTIONS.property, id); }
export function activateManagedPropertyPolicy(id) { return managerPoliciesApi.activate(POLICY_SECTIONS.property, id); }
export function deactivateManagedPropertyPolicy(id) { return managerPoliciesApi.deactivate(POLICY_SECTIONS.property, id); }

export function getManagedRoomInventory(roomTypeId, params = {}) { return managerAriApi.getInventory(roomTypeId, params); }
export function upsertManagedRoomInventory(roomTypeId, payload) { return managerAriApi.upsertInventory(roomTypeId, payload); }
export function deleteManagedRoomInventoryRange(roomTypeId, payload) { return managerAriApi.deleteInventoryRange(roomTypeId, payload); }
export function getManagedDailyRates(mappingId, params = {}) { return managerAriApi.getDailyRates(mappingId, params); }
export function upsertManagedDailyRates(mappingId, payload) { return managerAriApi.upsertDailyRates(mappingId, payload); }
export function deleteManagedDailyRatesRange(mappingId, payload) { return managerAriApi.deleteDailyRatesRange(mappingId, payload); }

export async function getHotelManagerOptions() {
  const [
    hotelsResponse,
    roomTypesResponse,
    ratePlansResponse,
    cancellationPoliciesResponse,
    checkInOutRulesResponse,
    propertyPoliciesResponse,
    extraServicesResponse,
  ] = await Promise.all([
    listManagedHotels({ includeDeleted: true, pageSize: 100 }),
    listManagedRoomTypes({ includeDeleted: true, pageSize: 100 }),
    listManagedRatePlans({ includeDeleted: true, pageSize: 100 }),
    listManagedCancellationPolicies({ includeDeleted: true, pageSize: 100 }),
    listManagedCheckInOutRules({ includeDeleted: true, pageSize: 100 }),
    listManagedPropertyPolicies({ includeDeleted: true, pageSize: 100 }),
    listManagedExtraServices({ includeDeleted: true, pageSize: 100 }),
  ]);

  return {
    hotels: Array.isArray(hotelsResponse?.items) ? hotelsResponse.items : [],
    roomTypes: Array.isArray(roomTypesResponse?.items) ? roomTypesResponse.items : [],
    ratePlans: Array.isArray(ratePlansResponse?.items) ? ratePlansResponse.items : [],
    cancellationPolicies: Array.isArray(cancellationPoliciesResponse?.items) ? cancellationPoliciesResponse.items : [],
    checkInOutRules: Array.isArray(checkInOutRulesResponse?.items) ? checkInOutRulesResponse.items : [],
    propertyPolicies: Array.isArray(propertyPoliciesResponse?.items) ? propertyPoliciesResponse.items : [],
    extraServices: Array.isArray(extraServicesResponse?.items) ? extraServicesResponse.items : [],
  };
}

export function listAdminHotels(params = {}, tenantId) { return adminHotelsCrud.list(params, tenantId); }
export function getAdminHotel(id, params = {}, tenantId) { return adminHotelsCrud.get(id, params, tenantId); }
export function createAdminHotel(payload, tenantId) { return adminHotelsCrud.create(payload, tenantId); }
export function updateAdminHotel(id, payload, tenantId) { return adminHotelsCrud.update(id, payload, tenantId); }
export function deleteAdminHotel(id, tenantId) { return adminHotelsCrud.remove(id, tenantId); }
export function restoreAdminHotel(id, tenantId) { return adminHotelsCrud.restore(id, tenantId); }
export function activateAdminHotel(id, tenantId) { return adminHotelsCrud.activate(id, tenantId); }
export function deactivateAdminHotel(id, tenantId) { return adminHotelsCrud.deactivate(id, tenantId); }

export function listAdminRoomTypes(params = {}, tenantId) { return adminRoomTypesCrud.list(params, tenantId); }
export function getAdminRoomType(id, params = {}, tenantId) { return adminRoomTypesCrud.get(id, params, tenantId); }
export function createAdminRoomType(payload, tenantId) { return adminRoomTypesCrud.create(payload, tenantId); }
export function updateAdminRoomType(id, payload, tenantId) { return adminRoomTypesCrud.update(id, payload, tenantId); }
export function deleteAdminRoomType(id, tenantId) { return adminRoomTypesCrud.remove(id, tenantId); }
export function restoreAdminRoomType(id, tenantId) { return adminRoomTypesCrud.restore(id, tenantId); }
export function activateAdminRoomType(id, tenantId) { return adminRoomTypesCrud.activate(id, tenantId); }
export function deactivateAdminRoomType(id, tenantId) { return adminRoomTypesCrud.deactivate(id, tenantId); }

export function listAdminRatePlans(params = {}, tenantId) { return adminRatePlansCrud.list(params, tenantId); }
export function getAdminRatePlan(id, params = {}, tenantId) { return adminRatePlansCrud.get(id, params, tenantId); }
export function createAdminRatePlan(payload, tenantId) { return adminRatePlansCrud.create(payload, tenantId); }
export function updateAdminRatePlan(id, payload, tenantId) { return adminRatePlansCrud.update(id, payload, tenantId); }
export function deleteAdminRatePlan(id, tenantId) { return adminRatePlansCrud.remove(id, tenantId); }
export function restoreAdminRatePlan(id, tenantId) { return adminRatePlansCrud.restore(id, tenantId); }
export function activateAdminRatePlan(id, tenantId) { return adminRatePlansCrud.activate(id, tenantId); }
export function deactivateAdminRatePlan(id, tenantId) { return adminRatePlansCrud.deactivate(id, tenantId); }

export function listAdminExtraServices(params = {}, tenantId) { return adminExtraServicesCrud.list(params, tenantId); }
export function getAdminExtraService(id, params = {}, tenantId) { return adminExtraServicesCrud.get(id, params, tenantId); }
export function createAdminExtraService(payload, tenantId) { return adminExtraServicesCrud.create(payload, tenantId); }
export function updateAdminExtraService(id, payload, tenantId) { return adminExtraServicesCrud.update(id, payload, tenantId); }
export function deleteAdminExtraService(id, tenantId) { return adminExtraServicesCrud.remove(id, tenantId); }
export function restoreAdminExtraService(id, tenantId) { return adminExtraServicesCrud.restore(id, tenantId); }
export function activateAdminExtraService(id, tenantId) { return adminExtraServicesCrud.activate(id, tenantId); }
export function deactivateAdminExtraService(id, tenantId) { return adminExtraServicesCrud.deactivate(id, tenantId); }
export function getAdminExtraServicePrices(id, tenantId) { return getScoped(`/admin/extra-services/${id}/prices`, {}, tenantId); }
export function replaceAdminExtraServicePrices(id, payload, tenantId) { return putScoped(`/admin/extra-services/${id}/prices`, payload, tenantId); }

export function listAdminCancellationPolicies(params = {}, tenantId) { return adminPoliciesApi.list(POLICY_SECTIONS.cancellation, params, tenantId); }
export function getAdminCancellationPolicy(id, params = {}, tenantId) { return adminPoliciesApi.get(POLICY_SECTIONS.cancellation, id, params, tenantId); }
export function createAdminCancellationPolicy(payload, tenantId) { return adminPoliciesApi.create(POLICY_SECTIONS.cancellation, payload, tenantId); }
export function updateAdminCancellationPolicy(id, payload, tenantId) { return adminPoliciesApi.update(POLICY_SECTIONS.cancellation, id, payload, tenantId); }
export function deleteAdminCancellationPolicy(id, tenantId) { return adminPoliciesApi.remove(POLICY_SECTIONS.cancellation, id, tenantId); }
export function restoreAdminCancellationPolicy(id, tenantId) { return adminPoliciesApi.restore(POLICY_SECTIONS.cancellation, id, tenantId); }
export function activateAdminCancellationPolicy(id, tenantId) { return adminPoliciesApi.activate(POLICY_SECTIONS.cancellation, id, tenantId); }
export function deactivateAdminCancellationPolicy(id, tenantId) { return adminPoliciesApi.deactivate(POLICY_SECTIONS.cancellation, id, tenantId); }

export function listAdminCheckInOutRules(params = {}, tenantId) { return adminPoliciesApi.list(POLICY_SECTIONS.checkInOut, params, tenantId); }
export function getAdminCheckInOutRule(id, params = {}, tenantId) { return adminPoliciesApi.get(POLICY_SECTIONS.checkInOut, id, params, tenantId); }
export function createAdminCheckInOutRule(payload, tenantId) { return adminPoliciesApi.create(POLICY_SECTIONS.checkInOut, payload, tenantId); }
export function updateAdminCheckInOutRule(id, payload, tenantId) { return adminPoliciesApi.update(POLICY_SECTIONS.checkInOut, id, payload, tenantId); }
export function deleteAdminCheckInOutRule(id, tenantId) { return adminPoliciesApi.remove(POLICY_SECTIONS.checkInOut, id, tenantId); }
export function restoreAdminCheckInOutRule(id, tenantId) { return adminPoliciesApi.restore(POLICY_SECTIONS.checkInOut, id, tenantId); }
export function activateAdminCheckInOutRule(id, tenantId) { return adminPoliciesApi.activate(POLICY_SECTIONS.checkInOut, id, tenantId); }
export function deactivateAdminCheckInOutRule(id, tenantId) { return adminPoliciesApi.deactivate(POLICY_SECTIONS.checkInOut, id, tenantId); }

export function listAdminPropertyPolicies(params = {}, tenantId) { return adminPoliciesApi.list(POLICY_SECTIONS.property, params, tenantId); }
export function getAdminPropertyPolicy(id, params = {}, tenantId) { return adminPoliciesApi.get(POLICY_SECTIONS.property, id, params, tenantId); }
export function createAdminPropertyPolicy(payload, tenantId) { return adminPoliciesApi.create(POLICY_SECTIONS.property, payload, tenantId); }
export function updateAdminPropertyPolicy(id, payload, tenantId) { return adminPoliciesApi.update(POLICY_SECTIONS.property, id, payload, tenantId); }
export function deleteAdminPropertyPolicy(id, tenantId) { return adminPoliciesApi.remove(POLICY_SECTIONS.property, id, tenantId); }
export function restoreAdminPropertyPolicy(id, tenantId) { return adminPoliciesApi.restore(POLICY_SECTIONS.property, id, tenantId); }
export function activateAdminPropertyPolicy(id, tenantId) { return adminPoliciesApi.activate(POLICY_SECTIONS.property, id, tenantId); }
export function deactivateAdminPropertyPolicy(id, tenantId) { return adminPoliciesApi.deactivate(POLICY_SECTIONS.property, id, tenantId); }

export function getAdminRoomInventory(roomTypeId, params = {}, tenantId) { return adminAriApi.getInventory(roomTypeId, params, tenantId); }
export function upsertAdminRoomInventory(roomTypeId, payload, tenantId) { return adminAriApi.upsertInventory(roomTypeId, payload, tenantId); }
export function deleteAdminRoomInventoryRange(roomTypeId, payload, tenantId) { return adminAriApi.deleteInventoryRange(roomTypeId, payload, tenantId); }
export function getAdminDailyRates(mappingId, params = {}, tenantId) { return adminAriApi.getDailyRates(mappingId, params, tenantId); }
export function upsertAdminDailyRates(mappingId, payload, tenantId) { return adminAriApi.upsertDailyRates(mappingId, payload, tenantId); }
export function deleteAdminDailyRatesRange(mappingId, payload, tenantId) { return adminAriApi.deleteDailyRatesRange(mappingId, payload, tenantId); }

export function listAdminHotelContacts(params = {}, tenantId) { return adminContactsCrud.list(params, tenantId); }
export function getAdminHotelContact(id, params = {}, tenantId) { return adminContactsCrud.get(id, params, tenantId); }
export function createAdminHotelContact(payload, tenantId) { return adminContactsCrud.create(payload, tenantId); }
export function updateAdminHotelContact(id, payload, tenantId) { return adminContactsCrud.update(id, payload, tenantId); }
export function deleteAdminHotelContact(id, tenantId) { return adminContactsCrud.remove(id, tenantId); }
export function restoreAdminHotelContact(id, tenantId) { return adminContactsCrud.restore(id, tenantId); }
export function activateAdminHotelContact(id, tenantId) { return adminContactsCrud.activate(id, tenantId); }
export function deactivateAdminHotelContact(id, tenantId) { return adminContactsCrud.deactivate(id, tenantId); }
export function setAdminHotelContactPrimary(id, tenantId) { return postScoped(`/admin/hotel-contacts/${id}/set-primary`, {}, tenantId); }

export function listAdminHotelImages(params = {}, tenantId) { return adminImagesCrud.list(params, tenantId); }
export function getAdminHotelImage(id, params = {}, tenantId) { return adminImagesCrud.get(id, params, tenantId); }
export function createAdminHotelImage(payload, tenantId) { return adminImagesCrud.create(payload, tenantId); }
export function updateAdminHotelImage(id, payload, tenantId) { return adminImagesCrud.update(id, payload, tenantId); }
export function deleteAdminHotelImage(id, tenantId) { return adminImagesCrud.remove(id, tenantId); }
export function restoreAdminHotelImage(id, tenantId) { return adminImagesCrud.restore(id, tenantId); }
export function activateAdminHotelImage(id, tenantId) { return adminImagesCrud.activate(id, tenantId); }
export function deactivateAdminHotelImage(id, tenantId) { return adminImagesCrud.deactivate(id, tenantId); }
export function setAdminHotelImagePrimary(id, tenantId) { return postScoped(`/admin/hotel-images/${id}/set-primary`, {}, tenantId); }

export function listAdminHotelAmenities(params = {}, tenantId) { return adminHotelAmenitiesCrud.list(params, tenantId); }
export function getAdminHotelAmenity(id, params = {}, tenantId) { return adminHotelAmenitiesCrud.get(id, params, tenantId); }
export function createAdminHotelAmenity(payload, tenantId) { return adminHotelAmenitiesCrud.create(payload, tenantId); }
export function updateAdminHotelAmenity(id, payload, tenantId) { return adminHotelAmenitiesCrud.update(id, payload, tenantId); }
export function deleteAdminHotelAmenity(id, tenantId) { return adminHotelAmenitiesCrud.remove(id, tenantId); }
export function restoreAdminHotelAmenity(id, tenantId) { return adminHotelAmenitiesCrud.restore(id, tenantId); }
export function activateAdminHotelAmenity(id, tenantId) { return adminHotelAmenitiesCrud.activate(id, tenantId); }
export function deactivateAdminHotelAmenity(id, tenantId) { return adminHotelAmenitiesCrud.deactivate(id, tenantId); }
export function getAdminHotelAmenityLinks(hotelId, tenantId) { return getScoped(`/admin/hotel-amenities/hotels/${hotelId}/links`, {}, tenantId); }
export function replaceAdminHotelAmenityLinks(hotelId, payload, tenantId) { return putScoped(`/admin/hotel-amenities/hotels/${hotelId}/links`, payload, tenantId); }

export function listAdminRoomAmenities(params = {}, tenantId) { return adminRoomAmenitiesCrud.list(params, tenantId); }
export function getAdminRoomAmenity(id, params = {}, tenantId) { return adminRoomAmenitiesCrud.get(id, params, tenantId); }
export function createAdminRoomAmenity(payload, tenantId) { return adminRoomAmenitiesCrud.create(payload, tenantId); }
export function updateAdminRoomAmenity(id, payload, tenantId) { return adminRoomAmenitiesCrud.update(id, payload, tenantId); }
export function deleteAdminRoomAmenity(id, tenantId) { return adminRoomAmenitiesCrud.remove(id, tenantId); }
export function restoreAdminRoomAmenity(id, tenantId) { return adminRoomAmenitiesCrud.restore(id, tenantId); }
export function activateAdminRoomAmenity(id, tenantId) { return adminRoomAmenitiesCrud.activate(id, tenantId); }
export function deactivateAdminRoomAmenity(id, tenantId) { return adminRoomAmenitiesCrud.deactivate(id, tenantId); }
export function getAdminRoomAmenityLinks(roomTypeId, tenantId) { return getScoped(`/admin/room-amenities/room-types/${roomTypeId}/links`, {}, tenantId); }
export function replaceAdminRoomAmenityLinks(roomTypeId, payload, tenantId) { return putScoped(`/admin/room-amenities/room-types/${roomTypeId}/links`, payload, tenantId); }

export function listAdminMealPlans(params = {}, tenantId) { return adminMealPlansCrud.list(params, tenantId); }
export function getAdminMealPlan(id, params = {}, tenantId) { return adminMealPlansCrud.get(id, params, tenantId); }
export function createAdminMealPlan(payload, tenantId) { return adminMealPlansCrud.create(payload, tenantId); }
export function updateAdminMealPlan(id, payload, tenantId) { return adminMealPlansCrud.update(id, payload, tenantId); }
export function deleteAdminMealPlan(id, tenantId) { return adminMealPlansCrud.remove(id, tenantId); }
export function restoreAdminMealPlan(id, tenantId) { return adminMealPlansCrud.restore(id, tenantId); }
export function activateAdminMealPlan(id, tenantId) { return adminMealPlansCrud.activate(id, tenantId); }
export function deactivateAdminMealPlan(id, tenantId) { return adminMealPlansCrud.deactivate(id, tenantId); }
export function getAdminMealPlanLinks(roomTypeId, tenantId) { return getScoped(`/admin/meal-plans/room-types/${roomTypeId}/links`, {}, tenantId); }
export function replaceAdminMealPlanLinks(roomTypeId, payload, tenantId) { return putScoped(`/admin/meal-plans/room-types/${roomTypeId}/links`, payload, tenantId); }

export function listAdminBedTypes(params = {}, tenantId) { return adminBedTypesCrud.list(params, tenantId); }
export function getAdminBedType(id, params = {}, tenantId) { return adminBedTypesCrud.get(id, params, tenantId); }
export function createAdminBedType(payload, tenantId) { return adminBedTypesCrud.create(payload, tenantId); }
export function updateAdminBedType(id, payload, tenantId) { return adminBedTypesCrud.update(id, payload, tenantId); }
export function deleteAdminBedType(id, tenantId) { return adminBedTypesCrud.remove(id, tenantId); }
export function restoreAdminBedType(id, tenantId) { return adminBedTypesCrud.restore(id, tenantId); }
export function activateAdminBedType(id, tenantId) { return adminBedTypesCrud.activate(id, tenantId); }
export function deactivateAdminBedType(id, tenantId) { return adminBedTypesCrud.deactivate(id, tenantId); }
export function getAdminBedTypeLinks(roomTypeId, tenantId) { return getScoped(`/admin/bed-types/room-types/${roomTypeId}/links`, {}, tenantId); }
export function replaceAdminBedTypeLinks(roomTypeId, payload, tenantId) { return putScoped(`/admin/bed-types/room-types/${roomTypeId}/links`, payload, tenantId); }

export function listAdminRoomTypeImages(params = {}, tenantId) { return adminRoomTypeImagesCrud.list(params, tenantId); }
export function getAdminRoomTypeImage(id, params = {}, tenantId) { return adminRoomTypeImagesCrud.get(id, params, tenantId); }
export function createAdminRoomTypeImage(payload, tenantId) { return adminRoomTypeImagesCrud.create(payload, tenantId); }
export function updateAdminRoomTypeImage(id, payload, tenantId) { return adminRoomTypeImagesCrud.update(id, payload, tenantId); }
export function deleteAdminRoomTypeImage(id, tenantId) { return adminRoomTypeImagesCrud.remove(id, tenantId); }
export function restoreAdminRoomTypeImage(id, tenantId) { return adminRoomTypeImagesCrud.restore(id, tenantId); }
export function activateAdminRoomTypeImage(id, tenantId) { return adminRoomTypeImagesCrud.activate(id, tenantId); }
export function deactivateAdminRoomTypeImage(id, tenantId) { return adminRoomTypeImagesCrud.deactivate(id, tenantId); }
export function setAdminRoomTypeImagePrimary(id, tenantId) { return postScoped(`/admin/room-type-images/${id}/set-primary`, {}, tenantId); }

export function listAdminRoomTypePolicies(params = {}, tenantId) { return adminRoomTypePoliciesCrud.list(params, tenantId); }
export function getAdminRoomTypePolicy(id, params = {}, tenantId) { return adminRoomTypePoliciesCrud.get(id, params, tenantId); }
export function createAdminRoomTypePolicy(payload, tenantId) { return adminRoomTypePoliciesCrud.create(payload, tenantId); }
export function updateAdminRoomTypePolicy(id, payload, tenantId) { return adminRoomTypePoliciesCrud.update(id, payload, tenantId); }
export function deleteAdminRoomTypePolicy(id, tenantId) { return adminRoomTypePoliciesCrud.remove(id, tenantId); }
export function restoreAdminRoomTypePolicy(id, tenantId) { return adminRoomTypePoliciesCrud.restore(id, tenantId); }
export function activateAdminRoomTypePolicy(id, tenantId) { return adminRoomTypePoliciesCrud.activate(id, tenantId); }
export function deactivateAdminRoomTypePolicy(id, tenantId) { return adminRoomTypePoliciesCrud.deactivate(id, tenantId); }

export function listAdminPromoRateOverrides(params = {}, tenantId) { return adminPromoOverridesCrud.list(params, tenantId); }
export function getAdminPromoRateOverride(id, params = {}, tenantId) { return adminPromoOverridesCrud.get(id, params, tenantId); }
export function createAdminPromoRateOverride(payload, tenantId) { return adminPromoOverridesCrud.create(payload, tenantId); }
export function updateAdminPromoRateOverride(id, payload, tenantId) { return adminPromoOverridesCrud.update(id, payload, tenantId); }
export function deleteAdminPromoRateOverride(id, tenantId) { return adminPromoOverridesCrud.remove(id, tenantId); }
export function restoreAdminPromoRateOverride(id, tenantId) { return adminPromoOverridesCrud.restore(id, tenantId); }
export function activateAdminPromoRateOverride(id, tenantId) { return adminPromoOverridesCrud.activate(id, tenantId); }
export function deactivateAdminPromoRateOverride(id, tenantId) { return adminPromoOverridesCrud.deactivate(id, tenantId); }

export function listAdminHotelReviews(params = {}, tenantId) { return adminReviewsCrud.list(params, tenantId); }
export function getAdminHotelReview(id, params = {}, tenantId) { return adminReviewsCrud.get(id, params, tenantId); }
export function updateAdminHotelReview(id, payload, tenantId) { return adminReviewsCrud.update(id, payload, tenantId); }
export function approveAdminHotelReview(id, tenantId) { return postScoped(`/admin/hotel-reviews/${id}/approve`, {}, tenantId); }
export function rejectAdminHotelReview(id, payload, tenantId) { return postScoped(`/admin/hotel-reviews/${id}/reject`, payload, tenantId); }
export function hideAdminHotelReview(id, tenantId) { return postScoped(`/admin/hotel-reviews/${id}/hide`, {}, tenantId); }
export function showAdminHotelReview(id, tenantId) { return postScoped(`/admin/hotel-reviews/${id}/show`, {}, tenantId); }
export function replyAdminHotelReview(id, payload, tenantId) { return postScoped(`/admin/hotel-reviews/${id}/reply`, payload, tenantId); }
export function deleteAdminHotelReview(id, tenantId) { return adminReviewsCrud.remove(id, tenantId); }
export function restoreAdminHotelReview(id, tenantId) { return adminReviewsCrud.restore(id, tenantId); }

export async function getAdminHotelOptions(tenantId) {
  const [
    hotelsResponse,
    roomTypesResponse,
    ratePlansResponse,
    cancellationPoliciesResponse,
    checkInOutRulesResponse,
    propertyPoliciesResponse,
    extraServicesResponse,
    contactsResponse,
    imagesResponse,
    hotelAmenitiesResponse,
    roomAmenitiesResponse,
    mealPlansResponse,
    bedTypesResponse,
    roomTypeImagesResponse,
    roomTypePoliciesResponse,
    promoOverridesResponse,
    reviewsResponse,
  ] = await Promise.all([
    listAdminHotels({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminRoomTypes({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminRatePlans({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminCancellationPolicies({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminCheckInOutRules({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminPropertyPolicies({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminExtraServices({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminHotelContacts({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminHotelImages({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminHotelAmenities({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminRoomAmenities({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminMealPlans({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminBedTypes({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminRoomTypeImages({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminRoomTypePolicies({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminPromoRateOverrides({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminHotelReviews({ includeDeleted: true, pageSize: 100 }, tenantId),
  ]);

  return {
    hotels: Array.isArray(hotelsResponse?.items) ? hotelsResponse.items : [],
    roomTypes: Array.isArray(roomTypesResponse?.items) ? roomTypesResponse.items : [],
    ratePlans: Array.isArray(ratePlansResponse?.items) ? ratePlansResponse.items : [],
    cancellationPolicies: Array.isArray(cancellationPoliciesResponse?.items) ? cancellationPoliciesResponse.items : [],
    checkInOutRules: Array.isArray(checkInOutRulesResponse?.items) ? checkInOutRulesResponse.items : [],
    propertyPolicies: Array.isArray(propertyPoliciesResponse?.items) ? propertyPoliciesResponse.items : [],
    extraServices: Array.isArray(extraServicesResponse?.items) ? extraServicesResponse.items : [],
    contacts: Array.isArray(contactsResponse?.items) ? contactsResponse.items : [],
    images: Array.isArray(imagesResponse?.items) ? imagesResponse.items : [],
    hotelAmenities: Array.isArray(hotelAmenitiesResponse?.items) ? hotelAmenitiesResponse.items : [],
    roomAmenities: Array.isArray(roomAmenitiesResponse?.items) ? roomAmenitiesResponse.items : [],
    mealPlans: Array.isArray(mealPlansResponse?.items) ? mealPlansResponse.items : [],
    bedTypes: Array.isArray(bedTypesResponse?.items) ? bedTypesResponse.items : [],
    roomTypeImages: Array.isArray(roomTypeImagesResponse?.items) ? roomTypeImagesResponse.items : [],
    roomTypePolicies: Array.isArray(roomTypePoliciesResponse?.items) ? roomTypePoliciesResponse.items : [],
    promoOverrides: Array.isArray(promoOverridesResponse?.items) ? promoOverridesResponse.items : [],
    reviews: Array.isArray(reviewsResponse?.items) ? reviewsResponse.items : [],
  };
}
