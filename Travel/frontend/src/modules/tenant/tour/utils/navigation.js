const PATHS = {
  overview: '/tenant/inventory/tour',
  schedules: '/tenant/operations/tour/schedules',
  pricing: '/tenant/operations/tour/pricing',
  capacity: '/tenant/operations/tour/capacity',
  packages: '/tenant/operations/tour/packages',
  content: '/tenant/operations/tour/content',
  experience: '/tenant/operations/tour/experience',
  builder: '/tenant/operations/tour/package-builder',
  reporting: '/tenant/operations/tour/reporting',
};

export function buildTourManagementSearch(params = {}) {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  });

  const query = search.toString();
  return query ? `?${query}` : '';
}

export function getTourManagementSectionPath(section, params = {}) {
  const path = PATHS[section] || PATHS.overview;
  return `${path}${buildTourManagementSearch(params)}`;
}
