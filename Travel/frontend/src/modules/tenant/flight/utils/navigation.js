export function getFlightManagementSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'operations':
      return '/tenant/operations/flight';
    case 'providers':
      return '/tenant/providers/flight';
    case 'airlines':
      return '/tenant/operations/flight/airlines';
    case 'airports':
      return '/tenant/operations/flight/airports';
    case 'flights':
      return '/tenant/operations/flight/flights';
    case 'offers':
      return '/tenant/operations/flight/offers';
    case 'fare-classes':
      return '/tenant/operations/flight/fare-classes';
    case 'fare-rules':
      return '/tenant/operations/flight/fare-rules';
    case 'tax-fee-lines':
      return '/tenant/operations/flight/tax-fee-lines';
    case 'aircraft-models':
      return '/tenant/providers/flight/aircraft-models';
    case 'aircrafts':
      return '/tenant/providers/flight/aircrafts';
    case 'seat-maps':
      return '/tenant/providers/flight/seat-maps';
    case 'seats':
      return '/tenant/providers/flight/seats';
    case 'ancillaries':
      return '/tenant/providers/flight/ancillaries';
    case 'overview':
    default:
      return '/tenant/inventory/flight';
  }
}
