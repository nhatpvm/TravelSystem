export function getHotelManagementSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'operations':
      return '/tenant/operations/hotel';
    case 'providers':
      return '/tenant/providers/hotel';
    case 'room-types':
      return '/tenant/operations/hotel/room-types';
    case 'rate-plans':
      return '/tenant/operations/hotel/rate-plans';
    case 'policies':
      return '/tenant/operations/hotel/policies';
    case 'extra-services':
      return '/tenant/providers/hotel/extra-services';
    case 'ari':
      return '/tenant/providers/hotel/ari';
    case 'overview':
    default:
      return '/tenant/inventory/hotel';
  }
}

export function getAdminHotelSectionPath(sectionKey = 'overview') {
  switch (sectionKey) {
    case 'room-types':
      return '/admin/hotels/room-types';
    case 'rate-plans':
      return '/admin/hotels/rate-plans';
    case 'policies':
      return '/admin/hotels/policies';
    case 'extra-services':
      return '/admin/hotels/extra-services';
    case 'contacts':
      return '/admin/hotels/contacts';
    case 'images':
      return '/admin/hotels/images';
    case 'amenities':
      return '/admin/hotels/amenities';
    case 'room-amenities':
      return '/admin/hotels/room-amenities';
    case 'meal-plans':
      return '/admin/hotels/meal-plans';
    case 'bed-types':
      return '/admin/hotels/bed-types';
    case 'room-type-images':
      return '/admin/hotels/room-type-images';
    case 'room-type-policies':
      return '/admin/hotels/room-type-policies';
    case 'promo-overrides':
      return '/admin/hotels/promo-overrides';
    case 'reviews':
      return '/admin/hotels/reviews';
    case 'ari':
      return '/admin/hotels/ari';
    case 'overview':
    default:
      return '/admin/hotels';
  }
}
