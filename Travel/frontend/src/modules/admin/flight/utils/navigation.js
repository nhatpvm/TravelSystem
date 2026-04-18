export function getAdminFlightSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'airlines':
      return '/admin/flight/airlines';
    case 'airports':
      return '/admin/flight/airports';
    case 'flights':
      return '/admin/flight/flights';
    case 'offers':
      return '/admin/flight/offers';
    case 'fare-classes':
      return '/admin/flight/fare-classes';
    case 'fare-rules':
      return '/admin/flight/fare-rules';
    case 'tax-fee-lines':
      return '/admin/flight/tax-fee-lines';
    case 'aircraft-models':
      return '/admin/flight/aircraft-models';
    case 'aircrafts':
      return '/admin/flight/aircrafts';
    case 'seat-maps':
      return '/admin/flight/seat-maps';
    case 'seats':
      return '/admin/flight/seats';
    case 'ancillaries':
      return '/admin/flight/ancillaries';
    case 'overview':
    default:
      return '/admin/flight';
  }
}
