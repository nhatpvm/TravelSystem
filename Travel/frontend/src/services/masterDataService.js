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

function catalogPrefix(scope = 'admin') {
  return scope === 'tenant' ? '/tenant/catalog' : '/admin/catalog';
}

function fleetPrefix(scope = 'admin') {
  return scope === 'tenant' ? '/tenant/fleet' : '/admin/fleet';
}

export function listLocations(params = {}, tenantId, scope = 'admin') {
  return api.get(`${catalogPrefix(scope)}/locations${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getLocation(id, params = {}, tenantId, scope = 'admin') {
  return api.get(`${catalogPrefix(scope)}/locations/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createLocation(payload, tenantId, scope = 'admin') {
  return api.post(`${catalogPrefix(scope)}/locations`, payload, withTenantHeaders(tenantId));
}

export function importLocations(file, payload = {}, tenantId, scope = 'admin') {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('type', String(payload.type ?? 2));
  formData.append('updateExisting', String(payload.updateExisting ?? true));
  formData.append('dryRun', String(payload.dryRun ?? false));

  return api.post(`${catalogPrefix(scope)}/locations/import`, formData, withTenantHeaders(tenantId));
}

export function updateLocation(id, payload, tenantId, scope = 'admin') {
  return api.put(`${catalogPrefix(scope)}/locations/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteLocation(id, tenantId, scope = 'admin') {
  return api.delete(`${catalogPrefix(scope)}/locations/${id}`, withTenantHeaders(tenantId));
}

export function restoreLocation(id, tenantId, scope = 'admin') {
  return api.post(`${catalogPrefix(scope)}/locations/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listProviders(params = {}, tenantId, scope = 'admin') {
  return api.get(`${catalogPrefix(scope)}/providers${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getProvider(id, params = {}, tenantId, scope = 'admin') {
  return api.get(`${catalogPrefix(scope)}/providers/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createProvider(payload, tenantId, scope = 'admin') {
  return api.post(`${catalogPrefix(scope)}/providers`, payload, withTenantHeaders(tenantId));
}

export function updateProvider(id, payload, tenantId, scope = 'admin') {
  return api.put(`${catalogPrefix(scope)}/providers/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteProvider(id, tenantId, scope = 'admin') {
  return api.delete(`${catalogPrefix(scope)}/providers/${id}`, withTenantHeaders(tenantId));
}

export function restoreProvider(id, tenantId, scope = 'admin') {
  return api.post(`${catalogPrefix(scope)}/providers/${id}/restore`, {}, withTenantHeaders(tenantId));
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

export function listVehicleModels(params = {}, tenantId, scope = 'admin') {
  return api.get(`${fleetPrefix(scope)}/vehicle-models${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createVehicleModel(payload, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/vehicle-models`, payload, withTenantHeaders(tenantId));
}

export function updateVehicleModel(id, payload, tenantId, scope = 'admin') {
  return api.put(`${fleetPrefix(scope)}/vehicle-models/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteVehicleModel(id, tenantId, scope = 'admin') {
  return api.delete(`${fleetPrefix(scope)}/vehicle-models/${id}`, withTenantHeaders(tenantId));
}

export function restoreVehicleModel(id, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/vehicle-models/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listVehicles(params = {}, tenantId, scope = 'admin') {
  return api.get(`${fleetPrefix(scope)}/vehicles${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getVehicle(id, params = {}, tenantId, scope = 'admin') {
  return api.get(`${fleetPrefix(scope)}/vehicles/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createVehicle(payload, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/vehicles`, payload, withTenantHeaders(tenantId));
}

export function updateVehicle(id, payload, tenantId, scope = 'admin') {
  return api.put(`${fleetPrefix(scope)}/vehicles/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteVehicle(id, tenantId, scope = 'admin') {
  return api.delete(`${fleetPrefix(scope)}/vehicles/${id}`, withTenantHeaders(tenantId));
}

export function restoreVehicle(id, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/vehicles/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function listSeatMaps(params = {}, tenantId, scope = 'admin') {
  return api.get(`${fleetPrefix(scope)}/seat-maps${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function getSeatMap(id, params = {}, tenantId, scope = 'admin') {
  return api.get(`${fleetPrefix(scope)}/seat-maps/${id}${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function createSeatMap(payload, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/seat-maps`, payload, withTenantHeaders(tenantId));
}

export function updateSeatMap(id, payload, tenantId, scope = 'admin') {
  return api.put(`${fleetPrefix(scope)}/seat-maps/${id}`, payload, withTenantHeaders(tenantId));
}

export function deleteSeatMap(id, tenantId, scope = 'admin') {
  return api.delete(`${fleetPrefix(scope)}/seat-maps/${id}`, withTenantHeaders(tenantId));
}

export function restoreSeatMap(id, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/seat-maps/${id}/restore`, {}, withTenantHeaders(tenantId));
}

export function generateSeatMapSeats(id, payload, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/seat-maps/${id}/generate-seats`, payload, withTenantHeaders(tenantId));
}

export function listSeats(params = {}, tenantId, scope = 'admin') {
  return api.get(`${fleetPrefix(scope)}/seats${toQuery(params)}`, withTenantHeaders(tenantId));
}

export function updateSeat(id, payload, tenantId, scope = 'admin') {
  return api.put(`${fleetPrefix(scope)}/seats/${id}`, payload, withTenantHeaders(tenantId));
}

export function bulkUpdateSeats(payload, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/seats/bulk-update`, payload, withTenantHeaders(tenantId));
}

export function deleteSeat(id, tenantId, scope = 'admin') {
  return api.delete(`${fleetPrefix(scope)}/seats/${id}`, withTenantHeaders(tenantId));
}

export function restoreSeat(id, tenantId, scope = 'admin') {
  return api.post(`${fleetPrefix(scope)}/seats/${id}/restore`, {}, withTenantHeaders(tenantId));
}
