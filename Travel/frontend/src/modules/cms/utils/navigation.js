export function getCmsBasePath(mode = 'admin') {
  return mode === 'tenant' ? '/tenant/cms' : '/admin/cms';
}

export function getCmsSectionPath(mode, sectionKey) {
  const basePath = getCmsBasePath(mode);

  switch (sectionKey) {
    case 'media':
      return `${basePath}/media`;
    case 'categories':
      return `${basePath}/categories`;
    case 'tags':
      return `${basePath}/tags`;
    case 'revisions':
      return `${basePath}/revisions`;
    case 'preview':
      return `${basePath}/preview`;
    case 'seo-audit':
      return `${basePath}/seo-audit`;
    case 'site-settings':
      return `${basePath}/site-settings`;
    case 'posts':
    default:
      return basePath;
  }
}
