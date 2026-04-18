import { api } from './api';
import { listLocations } from './masterDataService';

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

function getAdminFlightRequest(path, params = {}, tenantId) {
  return api.get(`/admin/flight/${path}${toQuery(params)}`, withTenantHeaders(tenantId));
}

function postAdminFlight(path, payload, tenantId) {
  return api.post(`/admin/flight/${path}`, payload, withTenantHeaders(tenantId));
}

function putAdminFlight(path, payload, tenantId) {
  return api.put(`/admin/flight/${path}`, payload, withTenantHeaders(tenantId));
}

function patchAdminFlight(path, payload, tenantId) {
  return api.patch(`/admin/flight/${path}`, payload, withTenantHeaders(tenantId));
}

function deleteAdminFlightRequest(path, tenantId) {
  return api.delete(`/admin/flight/${path}`, withTenantHeaders(tenantId));
}

export function searchFlightAirports(params = {}) {
  return api.get(`/search/flights/airports${toQuery(params)}`, { auth: false });
}

export function searchFlights(params = {}) {
  return api.get(`/search/flights${toQuery(params)}`, { auth: false });
}

export function getFlightOfferDetails(offerId, params = {}) {
  return api.get(`/flight/offers/${offerId}${toQuery(params)}`, { auth: false });
}

export function getFlightOfferAncillaries(offerId, params = {}) {
  return api.get(`/flight/offers/${offerId}/ancillaries${toQuery(params)}`, { auth: false });
}

export function getFlightSeatMapByOffer(offerId) {
  return api.get(`/flight/offers/${offerId}/seat-map`, { auth: false });
}

export function getFlightCabinSeatMap(cabinSeatMapId) {
  return api.get(`/flight/cabin-seat-maps/${cabinSeatMapId}`, { auth: false });
}

export function getFlightManagerOptions() {
  return api.get('/qlvmm/flight/options');
}

export function listFlightManagerFlights(params = {}) {
  return api.get(`/qlvmm/flight/flights${toQuery(params)}`);
}

export function getFlightManagerFlight(id, params = {}) {
  return api.get(`/qlvmm/flight/flights/${id}${toQuery(params)}`);
}

export function listFlightManagerOffers(params = {}) {
  return api.get(`/qlvmm/flight/offers${toQuery(params)}`);
}

export function getFlightManagerOffer(id, params = {}) {
  return api.get(`/qlvmm/flight/offers/${id}${toQuery(params)}`);
}

export function listFlightManagerSeatMaps(params = {}) {
  return api.get(`/qlvmm/flight/seat-maps${toQuery(params)}`);
}

export function getFlightManagerSeatMap(id, params = {}) {
  return api.get(`/qlvmm/flight/seat-maps/${id}${toQuery(params)}`);
}

export function getFlightManagerSeatMapByOffer(offerId) {
  return api.get(`/qlvmm/flight/seat-maps/by-offer/${offerId}`);
}

export function listFlightManagerAncillaries(params = {}) {
  return api.get(`/qlvmm/flight/ancillaries${toQuery(params)}`);
}

export function getFlightManagerAncillary(id, params = {}) {
  return api.get(`/qlvmm/flight/ancillaries/${id}${toQuery(params)}`);
}

export function getFlightManagerAncillariesByOffer(offerId, params = {}) {
  return api.get(`/qlvmm/flight/ancillaries/by-offer/${offerId}${toQuery(params)}`);
}

export async function getAdminFlightOptions(tenantId) {
  const [
    locationsResponse,
    airlinesResponse,
    airportsResponse,
    aircraftModelsResponse,
    aircraftsResponse,
    fareClassesResponse,
    fareRulesResponse,
    flightsResponse,
    offersResponse,
    seatMapsResponse,
    ancillariesResponse,
  ] = await Promise.all([
    listLocations({ includeDeleted: true, type: 4 }, tenantId),
    listAdminFlightAirlines({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightAirports({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightAircraftModels({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightAircrafts({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightFareClasses({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightFareRules({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlights({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightOffers({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightCabinSeatMaps({ includeDeleted: true, pageSize: 100 }, tenantId),
    listAdminFlightAncillaries({ includeDeleted: true, pageSize: 100 }, tenantId),
  ]);

  return {
    locations: Array.isArray(locationsResponse?.items) ? locationsResponse.items : [],
    airlines: Array.isArray(airlinesResponse?.items) ? airlinesResponse.items : [],
    airports: Array.isArray(airportsResponse?.items) ? airportsResponse.items : [],
    aircraftModels: Array.isArray(aircraftModelsResponse?.items) ? aircraftModelsResponse.items : [],
    aircrafts: Array.isArray(aircraftsResponse?.items) ? aircraftsResponse.items : [],
    fareClasses: Array.isArray(fareClassesResponse?.items) ? fareClassesResponse.items : [],
    fareRules: Array.isArray(fareRulesResponse?.items) ? fareRulesResponse.items : [],
    flights: Array.isArray(flightsResponse?.items) ? flightsResponse.items : [],
    offers: Array.isArray(offersResponse?.items) ? offersResponse.items : [],
    seatMaps: Array.isArray(seatMapsResponse?.items) ? seatMapsResponse.items : [],
    ancillaries: Array.isArray(ancillariesResponse?.items) ? ancillariesResponse.items : [],
  };
}

function listFlightAirlinesBase(params = {}, tenantId) {
  return getAdminFlightRequest('airlines', params, tenantId);
}

function getFlightAirlineBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`airlines/${id}`, params, tenantId);
}

function createFlightAirlineBase(payload, tenantId) {
  return postAdminFlight('airlines', payload, tenantId);
}

function updateFlightAirlineBase(id, payload, tenantId) {
  return putAdminFlight(`airlines/${id}`, payload, tenantId);
}

function deleteFlightAirlineBase(id, tenantId) {
  return deleteAdminFlightRequest(`airlines/${id}`, tenantId);
}

function restoreFlightAirlineBase(id, tenantId) {
  return postAdminFlight(`airlines/${id}/restore`, {}, tenantId);
}

export function listFlightAirlines(params = {}) { return listFlightAirlinesBase(params); }
export function getFlightAirline(id, params = {}) { return getFlightAirlineBase(id, params); }
export function createFlightAirline(payload) { return createFlightAirlineBase(payload); }
export function updateFlightAirline(id, payload) { return updateFlightAirlineBase(id, payload); }
export function deleteFlightAirline(id) { return deleteFlightAirlineBase(id); }
export function restoreFlightAirline(id) { return restoreFlightAirlineBase(id); }
export function listAdminFlightAirlines(params = {}, tenantId) { return listFlightAirlinesBase(params, tenantId); }
export function getAdminFlightAirline(id, params = {}, tenantId) { return getFlightAirlineBase(id, params, tenantId); }
export function createAdminFlightAirline(payload, tenantId) { return createFlightAirlineBase(payload, tenantId); }
export function updateAdminFlightAirline(id, payload, tenantId) { return updateFlightAirlineBase(id, payload, tenantId); }
export function deleteAdminFlightAirline(id, tenantId) { return deleteFlightAirlineBase(id, tenantId); }
export function restoreAdminFlightAirline(id, tenantId) { return restoreFlightAirlineBase(id, tenantId); }

function listFlightAirportsBase(params = {}, tenantId) {
  return getAdminFlightRequest('airports', params, tenantId);
}

function getFlightAirportBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`airports/${id}`, params, tenantId);
}

function createFlightAirportBase(payload, tenantId) {
  return postAdminFlight('airports', payload, tenantId);
}

function updateFlightAirportBase(id, payload, tenantId) {
  return putAdminFlight(`airports/${id}`, payload, tenantId);
}

function deleteFlightAirportBase(id, tenantId) {
  return deleteAdminFlightRequest(`airports/${id}`, tenantId);
}

function restoreFlightAirportBase(id, tenantId) {
  return postAdminFlight(`airports/${id}/restore`, {}, tenantId);
}

export function listFlightAirports(params = {}) { return listFlightAirportsBase(params); }
export function getFlightAirport(id, params = {}) { return getFlightAirportBase(id, params); }
export function createFlightAirport(payload) { return createFlightAirportBase(payload); }
export function updateFlightAirport(id, payload) { return updateFlightAirportBase(id, payload); }
export function deleteFlightAirport(id) { return deleteFlightAirportBase(id); }
export function restoreFlightAirport(id) { return restoreFlightAirportBase(id); }
export function listAdminFlightAirports(params = {}, tenantId) { return listFlightAirportsBase(params, tenantId); }
export function getAdminFlightAirport(id, params = {}, tenantId) { return getFlightAirportBase(id, params, tenantId); }
export function createAdminFlightAirport(payload, tenantId) { return createFlightAirportBase(payload, tenantId); }
export function updateAdminFlightAirport(id, payload, tenantId) { return updateFlightAirportBase(id, payload, tenantId); }
export function deleteAdminFlightAirport(id, tenantId) { return deleteFlightAirportBase(id, tenantId); }
export function restoreAdminFlightAirport(id, tenantId) { return restoreFlightAirportBase(id, tenantId); }

function listFlightAircraftModelsBase(params = {}, tenantId) {
  return getAdminFlightRequest('aircraft-models', params, tenantId);
}

function getFlightAircraftModelBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`aircraft-models/${id}`, params, tenantId);
}

function createFlightAircraftModelBase(payload, tenantId) {
  return postAdminFlight('aircraft-models', payload, tenantId);
}

function updateFlightAircraftModelBase(id, payload, tenantId) {
  return putAdminFlight(`aircraft-models/${id}`, payload, tenantId);
}

function deleteFlightAircraftModelBase(id, tenantId) {
  return deleteAdminFlightRequest(`aircraft-models/${id}`, tenantId);
}

function restoreFlightAircraftModelBase(id, tenantId) {
  return postAdminFlight(`aircraft-models/${id}/restore`, {}, tenantId);
}

export function listFlightAircraftModels(params = {}) { return listFlightAircraftModelsBase(params); }
export function getFlightAircraftModel(id, params = {}) { return getFlightAircraftModelBase(id, params); }
export function createFlightAircraftModel(payload) { return createFlightAircraftModelBase(payload); }
export function updateFlightAircraftModel(id, payload) { return updateFlightAircraftModelBase(id, payload); }
export function deleteFlightAircraftModel(id) { return deleteFlightAircraftModelBase(id); }
export function restoreFlightAircraftModel(id) { return restoreFlightAircraftModelBase(id); }
export function listAdminFlightAircraftModels(params = {}, tenantId) { return listFlightAircraftModelsBase(params, tenantId); }
export function getAdminFlightAircraftModel(id, params = {}, tenantId) { return getFlightAircraftModelBase(id, params, tenantId); }
export function createAdminFlightAircraftModel(payload, tenantId) { return createFlightAircraftModelBase(payload, tenantId); }
export function updateAdminFlightAircraftModel(id, payload, tenantId) { return updateFlightAircraftModelBase(id, payload, tenantId); }
export function deleteAdminFlightAircraftModel(id, tenantId) { return deleteFlightAircraftModelBase(id, tenantId); }
export function restoreAdminFlightAircraftModel(id, tenantId) { return restoreFlightAircraftModelBase(id, tenantId); }

function listFlightAircraftsBase(params = {}, tenantId) {
  return getAdminFlightRequest('aircrafts', params, tenantId);
}

function getFlightAircraftBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`aircrafts/${id}`, params, tenantId);
}

function createFlightAircraftBase(payload, tenantId) {
  return postAdminFlight('aircrafts', payload, tenantId);
}

function updateFlightAircraftBase(id, payload, tenantId) {
  return putAdminFlight(`aircrafts/${id}`, payload, tenantId);
}

function deleteFlightAircraftBase(id, tenantId) {
  return deleteAdminFlightRequest(`aircrafts/${id}`, tenantId);
}

function restoreFlightAircraftBase(id, tenantId) {
  return postAdminFlight(`aircrafts/${id}/restore`, {}, tenantId);
}

export function listFlightAircrafts(params = {}) { return listFlightAircraftsBase(params); }
export function getFlightAircraft(id, params = {}) { return getFlightAircraftBase(id, params); }
export function createFlightAircraft(payload) { return createFlightAircraftBase(payload); }
export function updateFlightAircraft(id, payload) { return updateFlightAircraftBase(id, payload); }
export function deleteFlightAircraft(id) { return deleteFlightAircraftBase(id); }
export function restoreFlightAircraft(id) { return restoreFlightAircraftBase(id); }
export function listAdminFlightAircrafts(params = {}, tenantId) { return listFlightAircraftsBase(params, tenantId); }
export function getAdminFlightAircraft(id, params = {}, tenantId) { return getFlightAircraftBase(id, params, tenantId); }
export function createAdminFlightAircraft(payload, tenantId) { return createFlightAircraftBase(payload, tenantId); }
export function updateAdminFlightAircraft(id, payload, tenantId) { return updateFlightAircraftBase(id, payload, tenantId); }
export function deleteAdminFlightAircraft(id, tenantId) { return deleteFlightAircraftBase(id, tenantId); }
export function restoreAdminFlightAircraft(id, tenantId) { return restoreFlightAircraftBase(id, tenantId); }

function listFlightFareClassesBase(params = {}, tenantId) {
  return getAdminFlightRequest('fare-classes', params, tenantId);
}

function getFlightFareClassBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`fare-classes/${id}`, params, tenantId);
}

function createFlightFareClassBase(payload, tenantId) {
  return postAdminFlight('fare-classes', payload, tenantId);
}

function updateFlightFareClassBase(id, payload, tenantId) {
  return putAdminFlight(`fare-classes/${id}`, payload, tenantId);
}

function deleteFlightFareClassBase(id, tenantId) {
  return deleteAdminFlightRequest(`fare-classes/${id}`, tenantId);
}

function restoreFlightFareClassBase(id, tenantId) {
  return postAdminFlight(`fare-classes/${id}/restore`, {}, tenantId);
}

export function listFlightFareClasses(params = {}) { return listFlightFareClassesBase(params); }
export function getFlightFareClass(id, params = {}) { return getFlightFareClassBase(id, params); }
export function createFlightFareClass(payload) { return createFlightFareClassBase(payload); }
export function updateFlightFareClass(id, payload) { return updateFlightFareClassBase(id, payload); }
export function deleteFlightFareClass(id) { return deleteFlightFareClassBase(id); }
export function restoreFlightFareClass(id) { return restoreFlightFareClassBase(id); }
export function listAdminFlightFareClasses(params = {}, tenantId) { return listFlightFareClassesBase(params, tenantId); }
export function getAdminFlightFareClass(id, params = {}, tenantId) { return getFlightFareClassBase(id, params, tenantId); }
export function createAdminFlightFareClass(payload, tenantId) { return createFlightFareClassBase(payload, tenantId); }
export function updateAdminFlightFareClass(id, payload, tenantId) { return updateFlightFareClassBase(id, payload, tenantId); }
export function deleteAdminFlightFareClass(id, tenantId) { return deleteFlightFareClassBase(id, tenantId); }
export function restoreAdminFlightFareClass(id, tenantId) { return restoreFlightFareClassBase(id, tenantId); }

function listFlightFareRulesBase(params = {}, tenantId) {
  return getAdminFlightRequest('fare-rules', params, tenantId);
}

function getFlightFareRuleBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`fare-rules/${id}`, params, tenantId);
}

function createFlightFareRuleBase(payload, tenantId) {
  return postAdminFlight('fare-rules', payload, tenantId);
}

function updateFlightFareRuleBase(id, payload, tenantId) {
  return putAdminFlight(`fare-rules/${id}`, payload, tenantId);
}

function deleteFlightFareRuleBase(id, tenantId) {
  return deleteAdminFlightRequest(`fare-rules/${id}`, tenantId);
}

function restoreFlightFareRuleBase(id, tenantId) {
  return postAdminFlight(`fare-rules/${id}/restore`, {}, tenantId);
}

export function listFlightFareRules(params = {}) { return listFlightFareRulesBase(params); }
export function getFlightFareRule(id, params = {}) { return getFlightFareRuleBase(id, params); }
export function createFlightFareRule(payload) { return createFlightFareRuleBase(payload); }
export function updateFlightFareRule(id, payload) { return updateFlightFareRuleBase(id, payload); }
export function deleteFlightFareRule(id) { return deleteFlightFareRuleBase(id); }
export function restoreFlightFareRule(id) { return restoreFlightFareRuleBase(id); }
export function listAdminFlightFareRules(params = {}, tenantId) { return listFlightFareRulesBase(params, tenantId); }
export function getAdminFlightFareRule(id, params = {}, tenantId) { return getFlightFareRuleBase(id, params, tenantId); }
export function createAdminFlightFareRule(payload, tenantId) { return createFlightFareRuleBase(payload, tenantId); }
export function updateAdminFlightFareRule(id, payload, tenantId) { return updateFlightFareRuleBase(id, payload, tenantId); }
export function deleteAdminFlightFareRule(id, tenantId) { return deleteFlightFareRuleBase(id, tenantId); }
export function restoreAdminFlightFareRule(id, tenantId) { return restoreFlightFareRuleBase(id, tenantId); }

function listFlightsBase(params = {}, tenantId) {
  return getAdminFlightRequest('flights', params, tenantId);
}

function getFlightBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`flights/${id}`, params, tenantId);
}

function createFlightBase(payload, tenantId) {
  return postAdminFlight('flights', payload, tenantId);
}

function updateFlightBase(id, payload, tenantId) {
  return putAdminFlight(`flights/${id}`, payload, tenantId);
}

function deleteFlightBase(id, tenantId) {
  return deleteAdminFlightRequest(`flights/${id}`, tenantId);
}

function restoreFlightBase(id, tenantId) {
  return postAdminFlight(`flights/${id}/restore`, {}, tenantId);
}

export function listFlights(params = {}) { return listFlightsBase(params); }
export function getFlight(id, params = {}) { return getFlightBase(id, params); }
export function createFlight(payload) { return createFlightBase(payload); }
export function updateFlight(id, payload) { return updateFlightBase(id, payload); }
export function deleteFlight(id) { return deleteFlightBase(id); }
export function restoreFlight(id) { return restoreFlightBase(id); }
export function listAdminFlights(params = {}, tenantId) { return listFlightsBase(params, tenantId); }
export function getAdminFlight(id, params = {}, tenantId) { return getFlightBase(id, params, tenantId); }
export function createAdminFlight(payload, tenantId) { return createFlightBase(payload, tenantId); }
export function updateAdminFlight(id, payload, tenantId) { return updateFlightBase(id, payload, tenantId); }
export function deleteAdminFlight(id, tenantId) { return deleteFlightBase(id, tenantId); }
export function restoreAdminFlight(id, tenantId) { return restoreFlightBase(id, tenantId); }

function listFlightOffersBase(params = {}, tenantId) {
  return getAdminFlightRequest('offers', params, tenantId);
}

function getFlightOfferBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`offers/${id}`, params, tenantId);
}

function createFlightOfferBase(payload, tenantId) {
  return postAdminFlight('offers', payload, tenantId);
}

function updateFlightOfferBase(id, payload, tenantId) {
  return putAdminFlight(`offers/${id}`, payload, tenantId);
}

function deleteFlightOfferBase(id, tenantId) {
  return deleteAdminFlightRequest(`offers/${id}`, tenantId);
}

function restoreFlightOfferBase(id, tenantId) {
  return postAdminFlight(`offers/${id}/restore`, {}, tenantId);
}

export function listFlightOffers(params = {}) { return listFlightOffersBase(params); }
export function getManagedFlightOffer(id, params = {}) { return getFlightOfferBase(id, params); }
export function createFlightOffer(payload) { return createFlightOfferBase(payload); }
export function updateFlightOffer(id, payload) { return updateFlightOfferBase(id, payload); }
export function deleteFlightOffer(id) { return deleteFlightOfferBase(id); }
export function restoreFlightOffer(id) { return restoreFlightOfferBase(id); }
export function listAdminFlightOffers(params = {}, tenantId) { return listFlightOffersBase(params, tenantId); }
export function getAdminManagedFlightOffer(id, params = {}, tenantId) { return getFlightOfferBase(id, params, tenantId); }
export function createAdminFlightOffer(payload, tenantId) { return createFlightOfferBase(payload, tenantId); }
export function updateAdminFlightOffer(id, payload, tenantId) { return updateFlightOfferBase(id, payload, tenantId); }
export function deleteAdminFlightOffer(id, tenantId) { return deleteFlightOfferBase(id, tenantId); }
export function restoreAdminFlightOffer(id, tenantId) { return restoreFlightOfferBase(id, tenantId); }

function listFlightOfferTaxFeeLinesBase(params = {}, tenantId) {
  return getAdminFlightRequest('offers/tax-fee-lines', params, tenantId);
}

function getFlightOfferTaxFeeLineBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`offers/tax-fee-lines/${id}`, params, tenantId);
}

function createFlightOfferTaxFeeLineBase(payload, tenantId) {
  return postAdminFlight('offers/tax-fee-lines', payload, tenantId);
}

function updateFlightOfferTaxFeeLineBase(id, payload, tenantId) {
  return putAdminFlight(`offers/tax-fee-lines/${id}`, payload, tenantId);
}

function deleteFlightOfferTaxFeeLineBase(id, tenantId) {
  return deleteAdminFlightRequest(`offers/tax-fee-lines/${id}`, tenantId);
}

function restoreFlightOfferTaxFeeLineBase(id, tenantId) {
  return postAdminFlight(`offers/tax-fee-lines/${id}/restore`, {}, tenantId);
}

export function listFlightOfferTaxFeeLines(params = {}) { return listFlightOfferTaxFeeLinesBase(params); }
export function getFlightOfferTaxFeeLine(id, params = {}) { return getFlightOfferTaxFeeLineBase(id, params); }
export function createFlightOfferTaxFeeLine(payload) { return createFlightOfferTaxFeeLineBase(payload); }
export function updateFlightOfferTaxFeeLine(id, payload) { return updateFlightOfferTaxFeeLineBase(id, payload); }
export function deleteFlightOfferTaxFeeLine(id) { return deleteFlightOfferTaxFeeLineBase(id); }
export function restoreFlightOfferTaxFeeLine(id) { return restoreFlightOfferTaxFeeLineBase(id); }
export function listAdminFlightOfferTaxFeeLines(params = {}, tenantId) { return listFlightOfferTaxFeeLinesBase(params, tenantId); }
export function getAdminFlightOfferTaxFeeLine(id, params = {}, tenantId) { return getFlightOfferTaxFeeLineBase(id, params, tenantId); }
export function createAdminFlightOfferTaxFeeLine(payload, tenantId) { return createFlightOfferTaxFeeLineBase(payload, tenantId); }
export function updateAdminFlightOfferTaxFeeLine(id, payload, tenantId) { return updateFlightOfferTaxFeeLineBase(id, payload, tenantId); }
export function deleteAdminFlightOfferTaxFeeLine(id, tenantId) { return deleteFlightOfferTaxFeeLineBase(id, tenantId); }
export function restoreAdminFlightOfferTaxFeeLine(id, tenantId) { return restoreFlightOfferTaxFeeLineBase(id, tenantId); }

function listFlightCabinSeatMapsBase(params = {}, tenantId) {
  return getAdminFlightRequest('cabin-seat-maps', params, tenantId);
}

function getFlightCabinSeatMapBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`cabin-seat-maps/${id}`, params, tenantId);
}

function createFlightCabinSeatMapBase(payload, tenantId) {
  return postAdminFlight('cabin-seat-maps', payload, tenantId);
}

function updateFlightCabinSeatMapBase(id, payload, tenantId) {
  return putAdminFlight(`cabin-seat-maps/${id}`, payload, tenantId);
}

function deleteFlightCabinSeatMapBase(id, tenantId) {
  return deleteAdminFlightRequest(`cabin-seat-maps/${id}`, tenantId);
}

function restoreFlightCabinSeatMapBase(id, tenantId) {
  return postAdminFlight(`cabin-seat-maps/${id}/restore`, {}, tenantId);
}

function regenerateFlightCabinSeatMapSeatsBase(id, payload, tenantId) {
  return postAdminFlight(`cabin-seat-maps/${id}/regenerate-seats`, payload, tenantId);
}

export function listFlightCabinSeatMaps(params = {}) { return listFlightCabinSeatMapsBase(params); }
export function getManagedFlightCabinSeatMap(id, params = {}) { return getFlightCabinSeatMapBase(id, params); }
export function createManagedFlightCabinSeatMap(payload) { return createFlightCabinSeatMapBase(payload); }
export function updateManagedFlightCabinSeatMap(id, payload) { return updateFlightCabinSeatMapBase(id, payload); }
export function deleteManagedFlightCabinSeatMap(id) { return deleteFlightCabinSeatMapBase(id); }
export function restoreManagedFlightCabinSeatMap(id) { return restoreFlightCabinSeatMapBase(id); }
export function regenerateFlightCabinSeatMapSeats(id, payload) { return regenerateFlightCabinSeatMapSeatsBase(id, payload); }
export function listAdminFlightCabinSeatMaps(params = {}, tenantId) { return listFlightCabinSeatMapsBase(params, tenantId); }
export function getAdminManagedFlightCabinSeatMap(id, params = {}, tenantId) { return getFlightCabinSeatMapBase(id, params, tenantId); }
export function createAdminManagedFlightCabinSeatMap(payload, tenantId) { return createFlightCabinSeatMapBase(payload, tenantId); }
export function updateAdminManagedFlightCabinSeatMap(id, payload, tenantId) { return updateFlightCabinSeatMapBase(id, payload, tenantId); }
export function deleteAdminManagedFlightCabinSeatMap(id, tenantId) { return deleteFlightCabinSeatMapBase(id, tenantId); }
export function restoreAdminManagedFlightCabinSeatMap(id, tenantId) { return restoreFlightCabinSeatMapBase(id, tenantId); }
export function regenerateAdminFlightCabinSeatMapSeats(id, payload, tenantId) { return regenerateFlightCabinSeatMapSeatsBase(id, payload, tenantId); }

function listFlightCabinSeatsBase(params = {}, tenantId) {
  return getAdminFlightRequest('cabin-seats', params, tenantId);
}

function getFlightCabinSeatBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`cabin-seats/${id}`, params, tenantId);
}

function createFlightCabinSeatBase(payload, tenantId) {
  return postAdminFlight('cabin-seats', payload, tenantId);
}

function updateFlightCabinSeatBase(id, payload, tenantId) {
  return putAdminFlight(`cabin-seats/${id}`, payload, tenantId);
}

function patchFlightCabinSeatBase(id, payload, tenantId) {
  return patchAdminFlight(`cabin-seats/${id}`, payload, tenantId);
}

function deleteFlightCabinSeatBase(id, tenantId) {
  return deleteAdminFlightRequest(`cabin-seats/${id}`, tenantId);
}

function restoreFlightCabinSeatBase(id, tenantId) {
  return postAdminFlight(`cabin-seats/${id}/restore`, {}, tenantId);
}

export function listFlightCabinSeats(params = {}) { return listFlightCabinSeatsBase(params); }
export function getManagedFlightCabinSeat(id, params = {}) { return getFlightCabinSeatBase(id, params); }
export function createManagedFlightCabinSeat(payload) { return createFlightCabinSeatBase(payload); }
export function updateManagedFlightCabinSeat(id, payload) { return updateFlightCabinSeatBase(id, payload); }
export function patchManagedFlightCabinSeat(id, payload) { return patchFlightCabinSeatBase(id, payload); }
export function deleteManagedFlightCabinSeat(id) { return deleteFlightCabinSeatBase(id); }
export function restoreManagedFlightCabinSeat(id) { return restoreFlightCabinSeatBase(id); }
export function listAdminFlightCabinSeats(params = {}, tenantId) { return listFlightCabinSeatsBase(params, tenantId); }
export function getAdminManagedFlightCabinSeat(id, params = {}, tenantId) { return getFlightCabinSeatBase(id, params, tenantId); }
export function createAdminManagedFlightCabinSeat(payload, tenantId) { return createFlightCabinSeatBase(payload, tenantId); }
export function updateAdminManagedFlightCabinSeat(id, payload, tenantId) { return updateFlightCabinSeatBase(id, payload, tenantId); }
export function patchAdminManagedFlightCabinSeat(id, payload, tenantId) { return patchFlightCabinSeatBase(id, payload, tenantId); }
export function deleteAdminManagedFlightCabinSeat(id, tenantId) { return deleteFlightCabinSeatBase(id, tenantId); }
export function restoreAdminManagedFlightCabinSeat(id, tenantId) { return restoreFlightCabinSeatBase(id, tenantId); }

function listFlightAncillariesBase(params = {}, tenantId) {
  return getAdminFlightRequest('ancillaries', params, tenantId);
}

function getFlightAncillaryBase(id, params = {}, tenantId) {
  return getAdminFlightRequest(`ancillaries/${id}`, params, tenantId);
}

function createFlightAncillaryBase(payload, tenantId) {
  return postAdminFlight('ancillaries', payload, tenantId);
}

function updateFlightAncillaryBase(id, payload, tenantId) {
  return putAdminFlight(`ancillaries/${id}`, payload, tenantId);
}

function deleteFlightAncillaryBase(id, tenantId) {
  return deleteAdminFlightRequest(`ancillaries/${id}`, tenantId);
}

function restoreFlightAncillaryBase(id, tenantId) {
  return postAdminFlight(`ancillaries/${id}/restore`, {}, tenantId);
}

export function listFlightAncillaries(params = {}) { return listFlightAncillariesBase(params); }
export function getManagedFlightAncillary(id, params = {}) { return getFlightAncillaryBase(id, params); }
export function createManagedFlightAncillary(payload) { return createFlightAncillaryBase(payload); }
export function updateManagedFlightAncillary(id, payload) { return updateFlightAncillaryBase(id, payload); }
export function deleteManagedFlightAncillary(id) { return deleteFlightAncillaryBase(id); }
export function restoreManagedFlightAncillary(id) { return restoreFlightAncillaryBase(id); }
export function listAdminFlightAncillaries(params = {}, tenantId) { return listFlightAncillariesBase(params, tenantId); }
export function getAdminManagedFlightAncillary(id, params = {}, tenantId) { return getFlightAncillaryBase(id, params, tenantId); }
export function createAdminManagedFlightAncillary(payload, tenantId) { return createFlightAncillaryBase(payload, tenantId); }
export function updateAdminManagedFlightAncillary(id, payload, tenantId) { return updateFlightAncillaryBase(id, payload, tenantId); }
export function deleteAdminManagedFlightAncillary(id, tenantId) { return deleteFlightAncillaryBase(id, tenantId); }
export function restoreAdminManagedFlightAncillary(id, tenantId) { return restoreFlightAncillaryBase(id, tenantId); }
