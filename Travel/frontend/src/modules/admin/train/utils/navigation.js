export function getAdminTrainSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'stop-points':
      return '/admin/train/stop-points';
    case 'routes':
      return '/admin/train/routes';
    case 'trip-stop-times':
      return '/admin/train/trip-stop-times';
    case 'trip-segment-prices':
      return '/admin/train/trip-segment-prices';
    case 'cars':
      return '/admin/train/cars';
    case 'car-seats':
      return '/admin/train/car-seats';
    case 'overview':
    default:
      return '/admin/train';
  }
}
