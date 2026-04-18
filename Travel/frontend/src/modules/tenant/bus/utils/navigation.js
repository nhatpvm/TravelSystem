export function getBusManagementBasePath() {
  return '/tenant/inventory/bus';
}

export function getBusManagementSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'stop-points':
      return '/tenant/operations/bus/stop-points';
    case 'routes':
      return '/tenant/operations/bus/routes';
    case 'trip-stop-times':
      return '/tenant/operations/bus/trip-stop-times';
    case 'trip-stop-points':
      return '/tenant/operations/bus/trip-stop-points';
    case 'trip-segment-prices':
      return '/tenant/operations/bus/trip-segment-prices';
    case 'vehicle-details':
      return '/tenant/providers/bus/vehicle-details';
    case 'trip-seats':
      return '/tenant/providers/bus/seats';
    case 'seat-holds':
      return '/tenant/providers/bus/seat-holds';
    case 'operations':
      return '/tenant/operations/bus';
    case 'providers':
      return '/tenant/providers/bus';
    case 'overview':
    default:
      return '/tenant/inventory/bus';
  }
}
