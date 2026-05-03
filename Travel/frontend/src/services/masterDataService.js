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

export function listLocations(params = {}, tenantId) {
  return api.get(`/admin/catalog/locations${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getLocation(id, params = {}, tenantId) {
  return api.get(`/admin/catalog/locations/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createLocation(payload, tenantId) {
  return api.post('/admin/catalog/locations', payload, withTenantHeaders(tenantId));
}

export function importLocations(file, payload = {}, tenantId) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('type', String(payload.type ?? 2));
  formData.append('updateExisting', String(payload.updateExisting ?? true));
  formData.append('dryRun', String(payload.dryRun ?? false));

  return api.post('/admin/catalog/locations/import', formData, withTenantHeaders(tenantId));
}

export function updateLocation(id, payload, tenantId) {
  return api.put(`/admin/catalog/locations/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteLocation(id, tenantId) {
  return api.delete(`/admin/catalog/locations/${id}`, withTenantHeaders(tenantId));
}

export function restoreLocation(id, tenantId) {
  return api.post(`/admin/catalog/locations/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listProviders(params = {}, tenantId) {
  return api.get(`/admin/catalog/providers${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getProvider(id, params = {}, tenantId) {
  return api.get(`/admin/catalog/providers/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createProvider(payload, tenantId) {
  return api.post('/admin/catalog/providers', payload, withTenantHeaders(tenantId));
}

export function updateProvider(id, payload, tenantId) {
  return api.put(`/admin/catalog/providers/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteProvider(id, tenantId) {
  return api.delete(`/admin/catalog/providers/${id}`, withTenantHeaders(tenantId));
}

export function restoreProvider(id, tenantId) {
  return api.post(`/admin/catalog/providers/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function runGeoSync(depth) {
  return api.post(`/admin/geo/sync${toQuery({ depth })}`, {});
}

export function listGeoSyncLogs(params = {}) {
  return api.get(`/admin/geo/sync-logs${toQuery(params)}`);
}

export function getGeoSyncLog(id) {
  return api.get(`/admin/geo/sync-logs/${id}`);
}

export function listGeoProvinces(params = {}) {
  return api.get(`/geo/provinces${toQuery(params)}`);
}

export function listGeoDistricts(params = {}) {
  return api.get(`/geo/districts${toQuery(params)}`);
}

export function listGeoWards(params = {}) {
  return api.get(`/geo/wards${toQuery(params)}`);
}

export function lookupGeoWard(wardCode) {
  return api.get(`/geo/ward-lookup${toQuery({ wardCode })}`);
}

export function listVehicleModels(params = {}, tenantId) {
  return api.get(`/admin/fleet/vehicle-models${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createVehicleModel(payload, tenantId) {
  return api.post('/admin/fleet/vehicle-models', payload, withTenantHeaders(tenantId));
}

export function updateVehicleModel(id, payload, tenantId) {
  return api.put(`/admin/fleet/vehicle-models/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteVehicleModel(id, tenantId) {
  return api.delete(`/admin/fleet/vehicle-models/${id}`, withTenantHeaders(tenantId));
}

export function restoreVehicleModel(id, tenantId) {
  return api.post(`/admin/fleet/vehicle-models/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listVehicles(params = {}, tenantId) {
  return api.get(`/admin/fleet/vehicles${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getVehicle(id, params = {}, tenantId) {
  return api.get(`/admin/fleet/vehicles/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createVehicle(payload, tenantId) {
  return api.post('/admin/fleet/vehicles', payload, withTenantHeaders(tenantId));
}

export function updateVehicle(id, payload, tenantId) {
  return api.put(`/admin/fleet/vehicles/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteVehicle(id, tenantId) {
  return api.delete(`/admin/fleet/vehicles/${id}`, withTenantHeaders(tenantId));
}

export function restoreVehicle(id, tenantId) {
  return api.post(`/admin/fleet/vehicles/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listSeatMaps(params = {}, tenantId) {
  return api.get(`/admin/fleet/seat-maps${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getSeatMap(id, params = {}, tenantId) {
  return api.get(`/admin/fleet/seat-maps/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createSeatMap(payload, tenantId) {
  return api.post('/admin/fleet/seat-maps', payload, withTenantHeaders(tenantId));
}

export function updateSeatMap(id, payload, tenantId) {
  return api.put(`/admin/fleet/seat-maps/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteSeatMap(id, tenantId) {
  return api.delete(`/admin/fleet/seat-maps/${id}`, withTenantHeaders(tenantId));
}

export function restoreSeatMap(id, tenantId) {
  return api.post(`/admin/fleet/seat-maps/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function generateSeatMapSeats(id, payload, tenantId) {
  return api.post(`/admin/fleet/seat-maps/${id}/generate-seats`, payload, withTenantHeaders(tenantId));
}

export function listSeats(params = {}, tenantId) {
  return api.get(`/admin/fleet/seats${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function updateSeat(id, payload, tenantId) {
  return api.put(`/admin/fleet/seats/${id}`, payload, withTenantHeaders(tenantId));
}

export function bulkUpdateSeats(payload, tenantId) {
  return api.post('/admin/fleet/seats/bulk-update', payload, withTenantHeaders(tenantId));
}

export function deleteSeat(id, tenantId) {
  return api.delete(`/admin/fleet/seats/${id}`, withTenantHeaders(tenantId));
}

export function restoreSeat(id, tenantId) {
  return api.post(`/admin/fleet/seats/${id}/restore`, {}, withTenantHeaders(tenantId));
}
