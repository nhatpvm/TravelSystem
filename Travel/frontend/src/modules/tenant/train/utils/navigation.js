export function getTrainManagementBasePath() {
  return '/tenant/inventory/train';
}

export function getTrainManagementSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'stop-points':
      return '/tenant/operations/train/stop-points';
    case 'routes':
      return '/tenant/operations/train/routes';
    case 'trip-stop-times':
      return '/tenant/operations/train/trip-stop-times';
    case 'trip-segment-prices':
      return '/tenant/operations/train/trip-segment-prices';
    case 'cars':
      return '/tenant/providers/train/cars';
    case 'car-seats':
      return '/tenant/providers/train/car-seats';
    case 'trip-seats':
      return '/tenant/providers/train/seats';
    case 'seat-holds':
      return '/tenant/providers/train/seat-holds';
    case 'operations':
      return '/tenant/operations/train';
    case 'providers':
      return '/tenant/providers/train';
    case 'overview':
    default:
      return '/tenant/inventory/train';
  }
}
