export function getMasterDataBasePath() {
  return '/admin/master-data';
}

export function getMasterDataSectionPath(sectionKey = 'overview') {
  const basePath = getMasterDataBasePath();

  switch (sectionKey) {
    case 'locations':
      return `${basePath}/locations`;
    case 'providers':
      return `${basePath}/providers`;
    case 'geo-sync':
      return `${basePath}/geo-sync`;
    case 'geo-sync-logs':
      return `${basePath}/geo-sync-logs`;
    case 'vehicle-models':
      return `${basePath}/vehicle-models`;
    case 'vehicles':
      return `${basePath}/vehicles`;
    case 'seat-maps':
      return `${basePath}/seat-maps`;
    case 'seats':
      return `${basePath}/seats`;
    case 'overview':
    default:
      return basePath;
  }
}
