import { api } from './api';

const PUBLIC_TENANT_CODE = import.meta.env.VITE_PUBLIC_CMS_TENANT_CODE || 'NX001';

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

function withTenantHeaders(tenantId) {
  return tenantId ? { headers: { 'X-TenantId': tenantId } } : undefined;
}

function managerCmsOptions(tenantId) {
  return withTenantHeaders(tenantId);
}

function buildManagerPath(path, params) {
  return `/manager/cms/${path}${toQuery(params)}`;
}

export function getPublicCmsTenantCode() {
  return PUBLIC_TENANT_CODE;
}

export function getPublicCmsSiteInfo(tenantCode = PUBLIC_TENANT_CODE) {
  return api.get(`/cms/site-info${toQuery({ tenantCode })}`, { auth: false });
}

export function listPublicCmsPosts(params = {}) {
  const tenantCode = params.tenantCode || PUBLIC_TENANT_CODE;
  return api.get(`/cms/news-index${toQuery({ ...params, tenantCode })}`, { auth: false });
}

export function getPublicCmsPost(slug, params = {}) {
  const tenantCode = params.tenantCode || PUBLIC_TENANT_CODE;
  return api.get(`/cms/posts/${slug}${toQuery({ tenantCode, includeContent: params.includeContent, bumpView: params.bumpView })}`, { auth: false });
}

export function listPublicCmsCategories(tenantCode = PUBLIC_TENANT_CODE) {
  return api.get(`/cms/categories${toQuery({ tenantCode })}`, { auth: false });
}

export function listPublicCmsTags(tenantCode = PUBLIC_TENANT_CODE) {
  return api.get(`/cms/tags${toQuery({ tenantCode })}`, { auth: false });
}

export function getPublicCmsCategoryPage(slug, params = {}) {
  const tenantCode = params.tenantCode || PUBLIC_TENANT_CODE;
  return api.get(`/cms/categories/${slug}${toQuery({ tenantCode, page: params.page, pageSize: params.pageSize })}`, { auth: false });
}

export function getPublicCmsTagPage(slug, params = {}) {
  const tenantCode = params.tenantCode || PUBLIC_TENANT_CODE;
  return api.get(`/cms/tags/${slug}${toQuery({ tenantCode, page: params.page, pageSize: params.pageSize })}`, { auth: false });
}

export function getCmsOptions(tenantId) {
  return api.get(buildManagerPath('options'), managerCmsOptions(tenantId));
}

export function listCmsPosts(params = {}, tenantId) {
  return api.get(buildManagerPath('posts', params), managerCmsOptions(tenantId));
}

export function getCmsPost(id, tenantId) {
  return api.get(buildManagerPath(`posts/${id}`), managerCmsOptions(tenantId));
}

export function createCmsPost(payload, tenantId) {
  return api.post(buildManagerPath('posts'), payload, managerCmsOptions(tenantId));
}

export function updateCmsPost(id, payload, tenantId) {
  return api.put(buildManagerPath(`posts/${id}`), payload, managerCmsOptions(tenantId));
}

export function deleteCmsPost(id, tenantId) {
  return api.delete(buildManagerPath(`posts/${id}`), managerCmsOptions(tenantId));
}

export function restoreCmsPost(id, tenantId) {
  return api.post(buildManagerPath(`posts/${id}/restore`), {}, managerCmsOptions(tenantId));
}

export function publishCmsPost(id, payload, tenantId) {
  return api.post(buildManagerPath(`posts/${id}/publish`), payload || {}, managerCmsOptions(tenantId));
}

export function unpublishCmsPost(id, payload, tenantId) {
  return api.post(buildManagerPath(`posts/${id}/unpublish`), payload || {}, managerCmsOptions(tenantId));
}

export function scheduleCmsPost(id, payload, tenantId) {
  return api.post(buildManagerPath(`posts/${id}/schedule`), payload, managerCmsOptions(tenantId));
}

export function archiveCmsPost(id, payload, tenantId) {
  return api.post(buildManagerPath(`posts/${id}/archive`), payload || {}, managerCmsOptions(tenantId));
}

export function auditCmsDraft(payload, tenantId) {
  return api.post(buildManagerPath('seo-audit/draft'), payload, managerCmsOptions(tenantId));
}

export function getCmsPostAudit(id, tenantId) {
  return api.get(buildManagerPath(`seo-audit/posts/${id}`), managerCmsOptions(tenantId));
}

export function previewCmsDraft(payload, tenantId) {
  return api.post(buildManagerPath('preview'), payload, managerCmsOptions(tenantId));
}

export function getCmsPostPreview(id, tenantId) {
  return api.get(buildManagerPath(`preview/posts/${id}`), managerCmsOptions(tenantId));
}

export function listCmsRevisions(postId, tenantId) {
  return api.get(buildManagerPath(`posts/${postId}/revisions`), managerCmsOptions(tenantId));
}

export function getCmsRevision(postId, revisionId, tenantId) {
  return api.get(buildManagerPath(`posts/${postId}/revisions/${revisionId}`), managerCmsOptions(tenantId));
}

export function restoreCmsRevision(postId, revisionId, payload, tenantId) {
  return api.post(buildManagerPath(`posts/${postId}/revisions/${revisionId}/restore`), payload || {}, managerCmsOptions(tenantId));
}

export function listCmsMedia(params = {}, tenantId) {
  return api.get(buildManagerPath('media-assets', params), managerCmsOptions(tenantId));
}

export function createCmsMedia(payload, tenantId) {
  return api.post(buildManagerPath('media-assets'), payload, managerCmsOptions(tenantId));
}

export function deleteCmsMedia(id, tenantId) {
  return api.delete(buildManagerPath(`media-assets/${id}`), managerCmsOptions(tenantId));
}

export function restoreCmsMedia(id, tenantId) {
  return api.post(buildManagerPath(`media-assets/${id}/restore`), {}, managerCmsOptions(tenantId));
}

export function listCmsRedirects(params = {}, tenantId) {
  return api.get(buildManagerPath('redirects', params), managerCmsOptions(tenantId));
}

export function createCmsRedirect(payload, tenantId) {
  return api.post(buildManagerPath('redirects'), payload, managerCmsOptions(tenantId));
}

export function deleteCmsRedirect(id, tenantId) {
  return api.delete(buildManagerPath(`redirects/${id}`), managerCmsOptions(tenantId));
}

export function restoreCmsRedirect(id, tenantId) {
  return api.post(buildManagerPath(`redirects/${id}/restore`), {}, managerCmsOptions(tenantId));
}

export function getCmsSiteSettings(tenantId) {
  return api.get(buildManagerPath('site-settings'), managerCmsOptions(tenantId));
}

export function upsertCmsSiteSettings(payload, tenantId) {
  return api.put(buildManagerPath('site-settings'), payload, managerCmsOptions(tenantId));
}

export function listCmsCategories(params = {}, tenantId) {
  return api.get(buildManagerPath('categories', params), managerCmsOptions(tenantId));
}

export function createCmsCategory(payload, tenantId) {
  return api.post(buildManagerPath('categories'), payload, managerCmsOptions(tenantId));
}

export function listCmsTags(params = {}, tenantId) {
  return api.get(buildManagerPath('tags', params), managerCmsOptions(tenantId));
}

export function createCmsTag(payload, tenantId) {
  return api.post(buildManagerPath('tags'), payload, managerCmsOptions(tenantId));
}

export function buildCmsHtmlFromMarkdown(markdown) {
  const text = String(markdown || '').trim();
  if (!text) {
    return '';
  }

  return text
    .split(/\n{2,}/)
    .map((block) => `<p>${block
      .split('\n')
      .map((line) => escapeHtml(line))
      .join('<br />')}</p>`)
    .join('');
}

function escapeHtml(value) {
  return String(value || '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
