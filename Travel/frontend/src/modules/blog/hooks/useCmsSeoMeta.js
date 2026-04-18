import { useEffect } from 'react';

function ensureMeta(selector, attributes = {}) {
  let element = document.head.querySelector(selector);
  if (!element) {
    element = document.createElement('meta');
    Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
    document.head.appendChild(element);
  }
  return element;
}

function ensureLink(selector, rel) {
  let element = document.head.querySelector(selector);
  if (!element) {
    element = document.createElement('link');
    element.setAttribute('rel', rel);
    document.head.appendChild(element);
  }
  return element;
}

export default function useCmsSeoMeta(seo, fallbackTitle) {
  useEffect(() => {
    const title = seo?.title || fallbackTitle || 'Blog';
    const description = seo?.description || '';

    document.title = title;

    ensureMeta('meta[name="description"]', { name: 'description' }).setAttribute('content', description);
    ensureMeta('meta[name="robots"]', { name: 'robots' }).setAttribute('content', seo?.robots || 'index,follow');
    ensureMeta('meta[property="og:title"]', { property: 'og:title' }).setAttribute('content', seo?.ogTitle || title);
    ensureMeta('meta[property="og:description"]', { property: 'og:description' }).setAttribute('content', seo?.ogDescription || description);
    ensureMeta('meta[property="og:image"]', { property: 'og:image' }).setAttribute('content', seo?.ogImageUrl || '');
    ensureMeta('meta[name="twitter:card"]', { name: 'twitter:card' }).setAttribute('content', seo?.twitterCard || 'summary_large_image');
    ensureMeta('meta[name="twitter:title"]', { name: 'twitter:title' }).setAttribute('content', seo?.twitterTitle || title);
    ensureMeta('meta[name="twitter:description"]', { name: 'twitter:description' }).setAttribute('content', seo?.twitterDescription || description);
    ensureMeta('meta[name="twitter:image"]', { name: 'twitter:image' }).setAttribute('content', seo?.twitterImageUrl || '');

    const canonical = ensureLink('link[rel="canonical"]', 'canonical');
    canonical.setAttribute('href', seo?.canonicalUrl || window.location.href);
  }, [fallbackTitle, seo]);
}
