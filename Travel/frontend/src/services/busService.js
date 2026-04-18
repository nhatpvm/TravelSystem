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

export function searchBusLocations(params = {}) {
  return api.get(`/bus/search/locations${toQuery(params)}`, { auth: false });
}

export function searchBusTrips(params = {}) {
  return api.get(`/bus/search/trips${toQuery(params)}`, { auth: false });
}

export function getBusTripDetail(tripId, params = {}) {
  return api.get(`/bus/trips/${tripId}${toQuery(params)}`, { auth: false });
}

export function getBusTripSeats(tripId, params = {}, options = {}) {
  return api.get(`/bus/trips/${tripId}/seats${toQuery(params)}`, options);
}

export function holdBusSeats(payload) {
  return api.post('/bus/seat-holds', payload);
}

export function releaseBusSeatHold(holdToken) {
  return api.delete(`/bus/seat-holds/${holdToken}`);
}

export function getBusManagerOptions() {
  return api.get('/qlnx/bus/options');
}

export function listBusStopPoints(params = {}) {
  return api.get(`/qlnx/bus/stop-points${toQuery(params)}`);
}

export function getBusStopPoint(id, params = {}) {
  return api.get(`/qlnx/bus/stop-points/${id}${toQuery(params)}`);
}

export function createBusStopPoint(payload) {
  return api.post('/qlnx/bus/stop-points', payload);
}

export function updateBusStopPoint(id, payload) {
  return api.put(`/qlnx/bus/stop-points/${id}`, payload);
}

export function deleteBusStopPoint(id) {
  return api.delete(`/qlnx/bus/stop-points/${id}`);
}

export function restoreBusStopPoint(id) {
  return api.post(`/qlnx/bus/stop-points/${id}/restore`, {});
}

export function listBusRoutes(params = {}) {
  return api.get(`/qlnx/bus/routes${toQuery(params)}`);
}

export function getBusRoute(id, params = {}) {
  return api.get(`/qlnx/bus/routes/${id}${toQuery(params)}`);
}

export function createBusRoute(payload) {
  return api.post('/qlnx/bus/routes', payload);
}

export function updateBusRoute(id, payload) {
  return api.put(`/qlnx/bus/routes/${id}`, payload);
}

export function replaceBusRouteStops(id, payload) {
  return api.put(`/qlnx/bus/routes/${id}/stops`, payload);
}

export function deleteBusRoute(id) {
  return api.delete(`/qlnx/bus/routes/${id}`);
}

export function restoreBusRoute(id) {
  return api.post(`/qlnx/bus/routes/${id}/restore`, {});
}

export function listBusTrips(params = {}) {
  return api.get(`/qlnx/bus/trips${toQuery(params)}`);
}

export function getBusTrip(id, params = {}) {
  return api.get(`/qlnx/bus/trips/${id}${toQuery(params)}`);
}

export function createBusTrip(payload) {
  return api.post('/qlnx/bus/trips', payload);
}

export function updateBusTrip(id, payload) {
  return api.put(`/qlnx/bus/trips/${id}`, payload);
}

export function deleteBusTrip(id) {
  return api.delete(`/qlnx/bus/trips/${id}`);
}

export function restoreBusTrip(id) {
  return api.post(`/qlnx/bus/trips/${id}/restore`, {});
}

export function listBusTripStopTimes(tripId, params = {}) {
  return api.get(`/qlnx/bus/trip-stop-times/trips/${tripId}${toQuery(params)}`);
}

export function getBusTripStopTime(id, params = {}) {
  return api.get(`/qlnx/bus/trip-stop-times/${id}${toQuery(params)}`);
}

export function createBusTripStopTime(tripId, payload) {
  return api.post(`/qlnx/bus/trip-stop-times/trips/${tripId}`, payload);
}

export function updateBusTripStopTime(id, payload) {
  return api.put(`/qlnx/bus/trip-stop-times/${id}`, payload);
}

export function replaceBusTripStopTimes(tripId, payload) {
  return api.put(`/qlnx/bus/trip-stop-times/trips/${tripId}/replace`, payload);
}

export function generateBusTripStopTimesFromRoute(tripId, payload) {
  return api.post(`/qlnx/bus/trip-stop-times/trips/${tripId}/generate-from-route`, payload);
}

export function deleteBusTripStopTime(id) {
  return api.delete(`/qlnx/bus/trip-stop-times/${id}`);
}

export function restoreBusTripStopTime(id) {
  return api.post(`/qlnx/bus/trip-stop-times/${id}/restore`, {});
}

export function listPickupPoints(tripStopTimeId, params = {}) {
  return api.get(`/qlnx/bus/trip-stop-times/${tripStopTimeId}/pickup-points${toQuery(params)}`);
}

export function createPickupPoint(tripStopTimeId, payload) {
  return api.post(`/qlnx/bus/trip-stop-times/${tripStopTimeId}/pickup-points`, payload);
}

export function updatePickupPoint(id, payload) {
  return api.put(`/qlnx/bus/trip-stop-pickup-points/${id}`, payload);
}

export function deletePickupPoint(id) {
  return api.delete(`/qlnx/bus/trip-stop-pickup-points/${id}`);
}

export function restorePickupPoint(id) {
  return api.post(`/qlnx/bus/trip-stop-pickup-points/${id}/restore`, {});
}

export function listDropoffPoints(tripStopTimeId, params = {}) {
  return api.get(`/qlnx/bus/trip-stop-times/${tripStopTimeId}/dropoff-points${toQuery(params)}`);
}

export function createDropoffPoint(tripStopTimeId, payload) {
  return api.post(`/qlnx/bus/trip-stop-times/${tripStopTimeId}/dropoff-points`, payload);
}

export function updateDropoffPoint(id, payload) {
  return api.put(`/qlnx/bus/trip-stop-dropoff-points/${id}`, payload);
}

export function deleteDropoffPoint(id) {
  return api.delete(`/qlnx/bus/trip-stop-dropoff-points/${id}`);
}

export function restoreDropoffPoint(id) {
  return api.post(`/qlnx/bus/trip-stop-dropoff-points/${id}/restore`, {});
}

export function listBusTripSegmentPrices(tripId, params = {}) {
  return api.get(`/qlnx/bus/trip-segment-prices/trips/${tripId}${toQuery(params)}`);
}

export function getBusTripSegmentPrice(id, params = {}) {
  return api.get(`/qlnx/bus/trip-segment-prices/${id}${toQuery(params)}`);
}

export function createBusTripSegmentPrice(payload) {
  return api.post('/qlnx/bus/trip-segment-prices', payload);
}

export function updateBusTripSegmentPrice(id, payload) {
  return api.put(`/qlnx/bus/trip-segment-prices/${id}`, payload);
}

export function replaceBusTripSegmentPrices(payload) {
  return api.put('/qlnx/bus/trip-segment-prices/replace', payload);
}

export function generateBusTripSegmentPrices(payload) {
  return api.post('/qlnx/bus/trip-segment-prices/generate-all-pairs', payload);
}

export function deleteBusTripSegmentPrice(id) {
  return api.delete(`/qlnx/bus/trip-segment-prices/${id}`);
}

export function restoreBusTripSegmentPrice(id) {
  return api.post(`/qlnx/bus/trip-segment-prices/${id}/restore`, {});
}

export function getBusManagerTripSeats(tripId, params = {}) {
  return api.get(`/qlnx/bus/trips/${tripId}/seats${toQuery(params)}`);
}

export function listBusManagerSeatHolds(tripId, params = {}) {
  return api.get(`/qlnx/bus/seat-holds/trip/${tripId}${toQuery(params)}`);
}

export function releaseBusManagerSeatHold(holdToken) {
  return api.delete(`/qlnx/bus/seat-holds/${holdToken}`);
}

export function listBusVehicleDetails(params = {}) {
  return api.get(`/qlnx/fleet/bus-vehicle-details${toQuery(params)}`);
}

export function getBusVehicleDetail(id, params = {}) {
  return api.get(`/qlnx/fleet/bus-vehicle-details/${id}${toQuery(params)}`);
}

export function createBusVehicleDetail(payload) {
  return api.post('/qlnx/fleet/bus-vehicle-details', payload);
}

export function updateBusVehicleDetail(id, payload) {
  return api.put(`/qlnx/fleet/bus-vehicle-details/${id}`, payload);
}

export function deleteBusVehicleDetail(id) {
  return api.delete(`/qlnx/fleet/bus-vehicle-details/${id}`);
}

export function restoreBusVehicleDetail(id) {
  return api.post(`/qlnx/fleet/bus-vehicle-details/${id}/restore`, {});
}
