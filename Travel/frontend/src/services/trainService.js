import { api } from './api';
import { listLocations, listProviders } from './masterDataService';

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

export function searchTrainLocations(params = {}) {
  return api.get(`/train/search/locations${toQuery(params)}`, { auth: false });
}

export function searchTrainTrips(params = {}) {
  return api.get(`/train/search/trips${toQuery(params)}`, { auth: false });
}

export function getTrainTripDetail(tripId, params = {}) {
  return api.get(`/train/trips/${tripId}${toQuery(params)}`, { auth: false });
}

export function getTrainTripSeats(tripId, params = {}, options = {}) {
  return api.get(`/train/trips/${tripId}/seats${toQuery(params)}`, { auth: false, ...options });
}

export function holdTrainSeats(payload) {
  return api.post('/train/seat-holds', payload);
}

export function releaseTrainSeatHold(holdToken) {
  return api.delete(`/train/seat-holds/${holdToken}`);
}

export function getTrainManagerOptions() {
  return api.get('/qlvt/train/options');
}

export function listTrainStopPoints(params = {}) {
  return api.get(`/qlvt/train/stop-points${toQuery(params)}`);
}

export function getTrainStopPoint(id, params = {}) {
  return api.get(`/qlvt/train/stop-points/${id}${toQuery(params)}`);
}

export function createTrainStopPoint(payload) {
  return api.post('/qlvt/train/stop-points', payload);
}

export function updateTrainStopPoint(id, payload) {
  return api.put(`/qlvt/train/stop-points/${id}`, payload);
}

export function deleteTrainStopPoint(id) {
  return api.delete(`/qlvt/train/stop-points/${id}`);
}

export function restoreTrainStopPoint(id) {
  return api.post(`/qlvt/train/stop-points/${id}/restore`, {});
}

export function listTrainRoutes(params = {}) {
  return api.get(`/qlvt/train/routes${toQuery(params)}`);
}

export function getTrainRoute(id, params = {}) {
  return api.get(`/qlvt/train/routes/${id}${toQuery(params)}`);
}

export function createTrainRoute(payload) {
  return api.post('/qlvt/train/routes', payload);
}

export function updateTrainRoute(id, payload) {
  return api.put(`/qlvt/train/routes/${id}`, payload);
}

export function replaceTrainRouteStops(id, payload) {
  return api.put(`/qlvt/train/routes/${id}/stops`, payload);
}

export function deleteTrainRoute(id) {
  return api.delete(`/qlvt/train/routes/${id}`);
}

export function restoreTrainRoute(id) {
  return api.post(`/qlvt/train/routes/${id}/restore`, {});
}

export function listTrainTrips(params = {}) {
  return api.get(`/qlvt/train/trips${toQuery(params)}`);
}

export function getTrainTrip(id, params = {}) {
  return api.get(`/qlvt/train/trips/${id}${toQuery(params)}`);
}

export function createTrainTrip(payload) {
  return api.post('/qlvt/train/trips', payload);
}

export function updateTrainTrip(id, payload) {
  return api.put(`/qlvt/train/trips/${id}`, payload);
}

export function deleteTrainTrip(id) {
  return api.delete(`/qlvt/train/trips/${id}`);
}

export function restoreTrainTrip(id) {
  return api.post(`/qlvt/train/trips/${id}/restore`, {});
}

export function listTrainCars(params = {}) {
  return api.get(`/qlvt/train/cars${toQuery(params)}`);
}

export function getTrainCar(id, params = {}) {
  return api.get(`/qlvt/train/cars/${id}${toQuery(params)}`);
}

export function createTrainCar(payload) {
  return api.post('/qlvt/train/cars', payload);
}

export function updateTrainCar(id, payload) {
  return api.put(`/qlvt/train/cars/${id}`, payload);
}

export function generateTrainCarSeats(carId, payload) {
  return api.post(`/qlvt/train/cars/${carId}/seats/generate`, payload);
}

export function deleteTrainCar(id) {
  return api.delete(`/qlvt/train/cars/${id}`);
}

export function restoreTrainCar(id) {
  return api.post(`/qlvt/train/cars/${id}/restore`, {});
}

export function listTrainCarSeats(params = {}) {
  return api.get(`/qlvt/train/car-seats${toQuery(params)}`);
}

export function getTrainCarSeat(id, params = {}) {
  return api.get(`/qlvt/train/car-seats/${id}${toQuery(params)}`);
}

export function createTrainCarSeat(payload) {
  return api.post('/qlvt/train/car-seats', payload);
}

export function updateTrainCarSeat(id, payload) {
  return api.put(`/qlvt/train/car-seats/${id}`, payload);
}

export function deleteTrainCarSeat(id) {
  return api.delete(`/qlvt/train/car-seats/${id}`);
}

export function restoreTrainCarSeat(id) {
  return api.post(`/qlvt/train/car-seats/${id}/restore`, {});
}

export function listTrainTripStopTimes(tripId, params = {}) {
  return api.get(`/qlvt/train/trip-stop-times/trips/${tripId}${toQuery(params)}`);
}

export function getTrainTripStopTime(id, params = {}) {
  return api.get(`/qlvt/train/trip-stop-times/${id}${toQuery(params)}`);
}

export function createTrainTripStopTime(tripId, payload) {
  return api.post(`/qlvt/train/trip-stop-times/trips/${tripId}`, payload);
}

export function updateTrainTripStopTime(id, payload) {
  return api.put(`/qlvt/train/trip-stop-times/${id}`, payload);
}

export function replaceTrainTripStopTimes(tripId, payload) {
  return api.put(`/qlvt/train/trip-stop-times/trips/${tripId}/replace`, payload);
}

export function generateTrainTripStopTimesFromRoute(tripId, payload) {
  return api.post(`/qlvt/train/trip-stop-times/trips/${tripId}/generate-from-route`, payload);
}

export function deleteTrainTripStopTime(id) {
  return api.delete(`/qlvt/train/trip-stop-times/${id}`);
}

export function restoreTrainTripStopTime(id) {
  return api.post(`/qlvt/train/trip-stop-times/${id}/restore`, {});
}

export function listTrainTripSegmentPrices(tripId, params = {}) {
  return api.get(`/qlvt/train/trip-segment-prices/trips/${tripId}${toQuery(params)}`);
}

export function getTrainTripSegmentPrice(id, params = {}) {
  return api.get(`/qlvt/train/trip-segment-prices/${id}${toQuery(params)}`);
}

export function createTrainTripSegmentPrice(payload) {
  return api.post('/qlvt/train/trip-segment-prices', payload);
}

export function updateTrainTripSegmentPrice(id, payload) {
  return api.put(`/qlvt/train/trip-segment-prices/${id}`, payload);
}

export function replaceTrainTripSegmentPrices(payload) {
  return api.put('/qlvt/train/trip-segment-prices/replace', payload);
}

export function generateTrainTripSegmentPrices(payload) {
  return api.post('/qlvt/train/trip-segment-prices/generate-all-pairs', payload);
}

export function deleteTrainTripSegmentPrice(id) {
  return api.delete(`/qlvt/train/trip-segment-prices/${id}`);
}

export function restoreTrainTripSegmentPrice(id) {
  return api.post(`/qlvt/train/trip-segment-prices/${id}/restore`, {});
}

export function getTrainManagerTripSeats(tripId, params = {}) {
  return api.get(`/qlvt/train/trips/${tripId}/seats${toQuery(params)}`);
}

export function listTrainManagerSeatHolds(tripId, params = {}) {
  return api.get(`/qlvt/train/seat-holds/trip/${tripId}${toQuery(params)}`);
}

export function releaseTrainManagerSeatHold(holdToken) {
  return api.delete(`/qlvt/train/seat-holds/${holdToken}`);
}

export async function getAdminTrainOptions(tenantId) {
  const [providersResponse, locationsResponse, stopPointsResponse, routesResponse, tripsResponse, carsResponse] = await Promise.all([
    listProviders({ includeDeleted: true, type: 2 }, tenantId),
    listLocations({ includeDeleted: true, type: 3 }, tenantId),
    listAdminTrainStopPoints({ includeDeleted: true }, tenantId),
    listAdminTrainRoutes({ includeDeleted: true }, tenantId),
    listAdminTrainTrips({ includeDeleted: true }, tenantId),
    listAdminTrainCars({ includeDeleted: true }, tenantId),
  ]);

  return {
    providers: Array.isArray(providersResponse?.items) ? providersResponse.items : [],
    locations: Array.isArray(locationsResponse?.items) ? locationsResponse.items : [],
    stopPoints: Array.isArray(stopPointsResponse?.items) ? stopPointsResponse.items : [],
    routes: Array.isArray(routesResponse?.items) ? routesResponse.items : [],
    trips: Array.isArray(tripsResponse?.items) ? tripsResponse.items : [],
    cars: Array.isArray(carsResponse?.items) ? carsResponse.items : [],
  };
}

export function listAdminTrainStopPoints(params = {}, tenantId) {
  return api.get(`/admin/train/stop-points${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainStopPoint(id, params = {}, tenantId) {
  return api.get(`/admin/train/stop-points/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainStopPoint(payload, tenantId) {
  return api.post('/admin/train/stop-points', payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainStopPoint(id, payload, tenantId) {
  return api.put(`/admin/train/stop-points/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainStopPoint(id, tenantId) {
  return api.delete(`/admin/train/stop-points/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainStopPoint(id, tenantId) {
  return api.post(`/admin/train/stop-points/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listAdminTrainRoutes(params = {}, tenantId) {
  return api.get(`/admin/train/routes${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainRoute(id, params = {}, tenantId) {
  return api.get(`/admin/train/routes/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainRoute(payload, tenantId) {
  return api.post('/admin/train/routes', payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainRoute(id, payload, tenantId) {
  return api.put(`/admin/train/routes/${id}`, payload, withTenantHeaders(tenantId));
}

export function replaceAdminTrainRouteStops(id, payload, tenantId) {
  return api.put(`/admin/train/routes/${id}/stops`, payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainRoute(id, tenantId) {
  return api.delete(`/admin/train/routes/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainRoute(id, tenantId) {
  return api.post(`/admin/train/routes/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listAdminTrainTrips(params = {}, tenantId) {
  return api.get(`/admin/train/trips${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainTrip(id, params = {}, tenantId) {
  return api.get(`/admin/train/trips/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainTrip(payload, tenantId) {
  return api.post('/admin/train/trips', payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainTrip(id, payload, tenantId) {
  return api.put(`/admin/train/trips/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainTrip(id, tenantId) {
  return api.delete(`/admin/train/trips/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainTrip(id, tenantId) {
  return api.post(`/admin/train/trips/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listAdminTrainCars(params = {}, tenantId) {
  return api.get(`/admin/train/cars${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainCar(id, params = {}, tenantId) {
  return api.get(`/admin/train/cars/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainCar(payload, tenantId) {
  return api.post('/admin/train/cars', payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainCar(id, payload, tenantId) {
  return api.put(`/admin/train/cars/${id}`, payload, withTenantHeaders(tenantId));
}

export function generateAdminTrainCarSeats(carId, payload, tenantId) {
  return api.post(`/admin/train/cars/${carId}/seats/generate`, payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainCar(id, tenantId) {
  return api.delete(`/admin/train/cars/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainCar(id, tenantId) {
  return api.post(`/admin/train/cars/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listAdminTrainCarSeats(params = {}, tenantId) {
  return api.get(`/admin/train/car-seats${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainCarSeat(id, params = {}, tenantId) {
  return api.get(`/admin/train/car-seats/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainCarSeat(payload, tenantId) {
  return api.post('/admin/train/car-seats', payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainCarSeat(id, payload, tenantId) {
  return api.put(`/admin/train/car-seats/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainCarSeat(id, tenantId) {
  return api.delete(`/admin/train/car-seats/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainCarSeat(id, tenantId) {
  return api.post(`/admin/train/car-seats/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listAdminTrainTripStopTimes(tripId, params = {}, tenantId) {
  return api.get(`/admin/train/trip-stop-times/trips/${tripId}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainTripStopTime(id, params = {}, tenantId) {
  return api.get(`/admin/train/trip-stop-times/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainTripStopTime(tripId, payload, tenantId) {
  return api.post(`/admin/train/trip-stop-times/trips/${tripId}`, payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainTripStopTime(id, payload, tenantId) {
  return api.put(`/admin/train/trip-stop-times/${id}`, payload, withTenantHeaders(tenantId));
}

export function replaceAdminTrainTripStopTimes(tripId, payload, tenantId) {
  return api.put(`/admin/train/trip-stop-times/trips/${tripId}/replace`, payload, withTenantHeaders(tenantId));
}

export function generateAdminTrainTripStopTimesFromRoute(tripId, payload, tenantId) {
  return api.post(`/admin/train/trip-stop-times/trips/${tripId}/generate-from-route`, payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainTripStopTime(id, tenantId) {
  return api.delete(`/admin/train/trip-stop-times/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainTripStopTime(id, tenantId) {
  return api.post(`/admin/train/trip-stop-times/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listAdminTrainTripSegmentPrices(tripId, params = {}, tenantId) {
  return api.get(`/admin/train/trip-segment-prices/trips/${tripId}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getAdminTrainTripSegmentPrice(id, params = {}, tenantId) {
  return api.get(`/admin/train/trip-segment-prices/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createAdminTrainTripSegmentPrice(payload, tenantId) {
  return api.post('/admin/train/trip-segment-prices', payload, withTenantHeaders(tenantId));
}

export function updateAdminTrainTripSegmentPrice(id, payload, tenantId) {
  return api.put(`/admin/train/trip-segment-prices/${id}`, payload, withTenantHeaders(tenantId));
}

export function replaceAdminTrainTripSegmentPrices(payload, tenantId) {
  return api.put('/admin/train/trip-segment-prices/replace', payload, withTenantHeaders(tenantId));
}

export function generateAdminTrainTripSegmentPrices(payload, tenantId) {
  return api.post('/admin/train/trip-segment-prices/generate-all-pairs', payload, withTenantHeaders(tenantId));
}

export function deleteAdminTrainTripSegmentPrice(id, tenantId) {
  return api.delete(`/admin/train/trip-segment-prices/${id}`, withTenantHeaders(tenantId));
}

export function restoreAdminTrainTripSegmentPrice(id, tenantId) {
  return api.post(`/admin/train/trip-segment-prices/${id}/restore`, {}, withTenantHeaders(tenantId));
}
