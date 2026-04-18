const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright-core');

const API_BASE = 'http://127.0.0.1:5183/api/v1';
const APP_BASE = 'http://127.0.0.1:4173';
const CHROME_PATH = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const OUT_DIR = 'd:/FPT/TicketBooking.V3/_tmp/tours-live-test';
const ARTIFACTS_DIR = path.join(OUT_DIR, 'artifacts');
fs.mkdirSync(ARTIFACTS_DIR, { recursive: true });

const AUTH_KEYS = {
  accessToken: 'auth_token',
  refreshToken: 'auth_refresh_token',
  sessionId: 'auth_session_id',
  accessTokenExpiresAt: 'auth_expires_at',
  refreshTokenExpiresAt: 'auth_refresh_expires_at',
  user: 'auth_user',
  memberships: 'auth_memberships',
  currentTenantId: 'auth_current_tenant_id',
  permissions: 'auth_permissions',
  remember: 'auth_remember',
};

const PREFIX = `ToursLive_${new Date().toISOString().replace(/[-:.TZ]/g, '').slice(0, 14)}`;
const result = {
  startedAt: new Date().toISOString(),
  prefix: PREFIX,
  api: {},
  ui: [],
  consoleErrors: [],
  pageErrors: [],
  failedRequests: [],
};

function pushStep(area, name, extra = {}) {
  result.ui.push({ area, name, at: new Date().toISOString(), ...extra });
}

async function apiRequest(method, pathName, { token, tenantId, body, auth = true } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (auth && token) headers.Authorization = `Bearer ${token}`;
  if (tenantId) headers['X-TenantId'] = tenantId;

  const response = await fetch(`${API_BASE}${pathName}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  let data = null;
  const text = await response.text();
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!response.ok) {
    throw new Error(`${method} ${pathName} -> ${response.status}: ${typeof data === 'string' ? data : JSON.stringify(data)}`);
  }

  return data;
}

async function login(usernameOrEmail, password) {
  return apiRequest('POST', '/auth/login', {
    auth: false,
    body: { usernameOrEmail, password, rememberMe: true },
  });
}

async function loadSession(usernameOrEmail, password, preferredTenantId = null) {
  const auth = await login(usernameOrEmail, password);
  let memberships = [];
  let currentTenantId = preferredTenantId;

  try {
    const membershipResponse = await apiRequest('GET', '/tenancy/memberships', { token: auth.accessToken });
    memberships = membershipResponse?.items || [];
    if (!currentTenantId) currentTenantId = memberships[0]?.tenantId || null;
  } catch {
    memberships = [];
  }

  let permissions = [];
  try {
    const query = currentTenantId
      ? `/auth/me/permissions?tenantId=${encodeURIComponent(currentTenantId)}&grantedOnly=true`
      : '/auth/me/permissions?grantedOnly=true';
    const permResponse = await apiRequest('GET', query, { token: auth.accessToken });
    permissions = Array.isArray(permResponse?.items)
      ? permResponse.items.filter((item) => item?.isGranted).map((item) => item.code)
      : [];
  } catch {
    permissions = [];
  }

  return {
    ...auth,
    memberships,
    currentTenantId,
    permissions,
  };
}

async function ensureManagerContent(session, tourId) {
  const token = session.accessToken;
  const tenantId = session.currentTenantId;
  const data = {};

  const contacts = await apiRequest('GET', `/ql-tour/tours/${tourId}/contacts?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let contact = (contacts.items || []).find((item) => item.name === `${PREFIX}_contact`);
  if (!contact) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/contacts`, {
      token, tenantId,
      body: { name: `${PREFIX}_contact`, title: 'Lead', department: 'Ops', phone: '0900000001', email: `${PREFIX.toLowerCase()}@mail.local`, contactType: 1, isPrimary: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    contact = await apiRequest('GET', `/ql-tour/tours/${tourId}/contacts/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.contact = contact;

  const images = await apiRequest('GET', `/ql-tour/tours/${tourId}/images?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let image = (images.items || []).find((item) => item.title === `${PREFIX}_image`);
  if (!image) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/images`, {
      token, tenantId,
      body: { imageUrl: 'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1200&q=80', caption: `${PREFIX}_caption`, altText: `${PREFIX}_alt`, title: `${PREFIX}_image`, isPrimary: true, isCover: true, isFeatured: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    image = await apiRequest('GET', `/ql-tour/tours/${tourId}/images/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.image = image;

  const policies = await apiRequest('GET', `/ql-tour/tours/${tourId}/policies?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let policy = (policies.items || []).find((item) => item.code === `${PREFIX}_policy`);
  if (!policy) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/policies`, {
      token, tenantId,
      body: { code: `${PREFIX}_policy`, name: `${PREFIX} Policy`, type: 1, shortDescription: 'Policy smoke test', descriptionMarkdown: 'Policy detail', policyJson: '{"scope":"ui-smoke"}', isHighlighted: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    policy = await apiRequest('GET', `/ql-tour/tours/${tourId}/policies/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.policy = policy;

  const faqs = await apiRequest('GET', `/ql-tour/tours/${tourId}/faqs?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let faq = (faqs.items || []).find((item) => item.question === `${PREFIX}_faq?`);
  if (!faq) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/faqs`, {
      token, tenantId,
      body: { question: `${PREFIX}_faq?`, answerMarkdown: 'FAQ smoke answer', type: 1, isHighlighted: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    faq = await apiRequest('GET', `/ql-tour/tours/${tourId}/faqs/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.faq = faq;

  const itinerary = await apiRequest('GET', `/ql-tour/tours/${tourId}/itinerary?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  const itineraryItems = itinerary.items || [];
  let day = itineraryItems.find((item) => item.title === `${PREFIX}_day1`);
  if (!day) {
    const usedDayNumbers = itineraryItems
      .map((item) => Number(item.dayNumber || 0))
      .filter((value) => Number.isFinite(value) && value > 0);
    const nextDayNumber = usedDayNumbers.length ? Math.max(...usedDayNumbers) + 1 : 1;
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/itinerary/days`, {
      token, tenantId,
      body: { dayNumber: nextDayNumber, title: `${PREFIX}_day1`, shortDescription: 'Day smoke', startLocation: 'Da Nang', endLocation: 'Hoi An', accommodationName: 'Smoke Hotel', transportationSummary: 'Bus', includesBreakfast: true, includesLunch: false, includesDinner: true, sortOrder: nextDayNumber, notes: 'UI smoke', isActive: true },
    });
    await apiRequest('PUT', `/ql-tour/tours/${tourId}/itinerary/days/${created.id}/items`, {
      token, tenantId,
      body: { items: [{ title: `${PREFIX}_item`, description: 'Activity', startTime: '08:00', endTime: '10:00' }] },
    });
    day = await apiRequest('GET', `/ql-tour/tours/${tourId}/itinerary/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.day = day;

  const pickups = await apiRequest('GET', `/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let pickup = (pickups.items || []).find((item) => item.code === `${PREFIX}_pickup`);
  if (!pickup) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points`, {
      token, tenantId,
      body: { code: `${PREFIX}_pickup`, name: `${PREFIX} Pickup`, addressLine: 'Pickup Street', district: 'Hai Chau', province: 'Da Nang', countryCode: 'VN', pickupTime: '07:30', isDefault: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    pickup = await apiRequest('GET', `/ql-tour/tours/${tourId}/pickup-dropoff/pickup-points/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.pickup = pickup;

  const dropoffs = await apiRequest('GET', `/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let dropoff = (dropoffs.items || []).find((item) => item.code === `${PREFIX}_dropoff`);
  if (!dropoff) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points`, {
      token, tenantId,
      body: { code: `${PREFIX}_dropoff`, name: `${PREFIX} Dropoff`, addressLine: 'Dropoff Street', district: 'Hoi An', province: 'Quang Nam', countryCode: 'VN', dropoffTime: '18:30', isDefault: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    dropoff = await apiRequest('GET', `/ql-tour/tours/${tourId}/pickup-dropoff/dropoff-points/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.dropoff = dropoff;

  const addons = await apiRequest('GET', `/ql-tour/tours/${tourId}/addons?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let addon = (addons.items || []).find((item) => item.code === `${PREFIX}_addon`);
  if (!addon) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/addons`, {
      token, tenantId,
      body: { code: `${PREFIX}_addon`, name: `${PREFIX} Addon`, type: 9, currencyCode: 'VND', basePrice: 150000, shortDescription: 'Addon smoke', descriptionMarkdown: 'Addon detail', isPerPerson: true, isRequired: false, allowQuantitySelection: true, minQuantity: 1, maxQuantity: 3, isDefaultSelected: false, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    addon = await apiRequest('GET', `/ql-tour/tours/${tourId}/addons/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.addon = addon;

  return data;
}

async function ensureManagerPackageData(session, tourId, scheduleId, packageId) {
  const token = session.accessToken;
  const tenantId = session.currentTenantId;
  const data = {};

  const comps = await apiRequest('GET', `/ql-tour/tours/${tourId}/packages/${packageId}/components?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let component = (comps.items || []).find((item) => item.code === `${PREFIX}_component`);
  if (!component) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/packages/${packageId}/components`, {
      token, tenantId,
      body: { code: `${PREFIX}_component`, name: `${PREFIX} Component`, componentType: 1, selectionMode: 1, minSelect: 1, maxSelect: 1, dayOffsetFromDeparture: 0, nightCount: 1, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    component = await apiRequest('GET', `/ql-tour/tours/${tourId}/packages/${packageId}/components/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.component = component;

  const options = await apiRequest('GET', `/ql-tour/tours/${tourId}/packages/${packageId}/components/${component.id}/options?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let option = (options.items || []).find((item) => item.code === `${PREFIX}_option`);
  if (!option) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/packages/${packageId}/components/${component.id}/options`, {
      token, tenantId,
      body: { code: `${PREFIX}_option`, name: `${PREFIX} Option`, sourceType: 6, bindingMode: 3, searchTemplateJson: '{"kind":"smoke"}', pricingMode: 1, currencyCode: 'VND', priceOverride: 3500000, quantityMode: 1, defaultQuantity: 1, minQuantity: 1, maxQuantity: 2, isDefaultSelected: true, isFallback: false, isDynamicCandidate: true, sortOrder: 1, notes: 'UI smoke', isActive: true },
    });
    option = await apiRequest('GET', `/ql-tour/tours/${tourId}/packages/${packageId}/components/${component.id}/options/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.option = option;

  const overrides = await apiRequest('GET', `/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let override = (overrides.items || []).find((item) => item.tourPackageOptionCode === `${PREFIX}_option` || item.tourPackageComponentOptionId === option.id);
  if (!override) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides`, {
      token, tenantId,
      body: { tourPackageComponentOptionId: option.id, status: 1, currencyCode: 'VND', priceOverride: 3600000, boundSnapshotJson: '{"source":"ui-smoke"}', notes: 'UI smoke', isActive: true },
    });
    override = await apiRequest('GET', `/ql-tour/tours/${tourId}/schedules/${scheduleId}/package-overrides/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  data.override = override;

  const reportingOverview = await apiRequest('GET', `/ql-tour/tours/${tourId}/package-reporting/overview`, { token, tenantId });
  const reportingSources = await apiRequest('GET', `/ql-tour/tours/${tourId}/package-reporting/source-breakdown`, { token, tenantId });
  const reportingAudit = await apiRequest('GET', `/ql-tour/tours/${tourId}/package-reporting/audit-events?page=1&pageSize=20`, { token, tenantId });
  data.reportingOverview = reportingOverview;
  data.reportingSources = reportingSources;
  data.reportingAudit = reportingAudit;

  return data;
}

async function ensureManagerReview(session, tourId) {
  const token = session.accessToken;
  const tenantId = session.currentTenantId;
  const reviews = await apiRequest('GET', `/ql-tour/tours/${tourId}/reviews?page=1&pageSize=100&includeDeleted=true`, { token, tenantId });
  let review = (reviews.items || []).find((item) => item.title === `${PREFIX}_review`);
  if (!review) {
    const created = await apiRequest('POST', `/ql-tour/tours/${tourId}/reviews`, {
      token, tenantId,
      body: { rating: 5, title: `${PREFIX}_review`, content: 'Review smoke content', reviewerName: `${PREFIX}_customer`, status: 1, isApproved: true, isPublic: true, moderationNote: 'UI smoke review' },
    });
    review = await apiRequest('GET', `/ql-tour/tours/${tourId}/reviews/${created.id}?includeDeleted=true`, { token, tenantId });
  }
  return review;
}

async function setupData() {
  const manager = await loadSession('qltour', 'QlTour@12345');
  const customer = await loadSession('customer', 'Customer@12345');
  const publicTours = await apiRequest('GET', '/tours?page=1&pageSize=10', { auth: false });
  if (!publicTours.items?.length) throw new Error('No public tours available for smoke test.');

  const publicTour = publicTours.items[0];
  const publicDetail = await apiRequest('GET', `/tours/${publicTour.id}`, { auth: false });
  const availability = await apiRequest('GET', `/tours/${publicTour.id}/availability?page=1&pageSize=20`, { auth: false });
  const schedule = availability.items?.find((item) => item.canBook) || availability.items?.[0];
  if (!schedule) throw new Error('No public tour schedule available for smoke test.');

  const quote = await apiRequest('POST', `/tours/${publicTour.id}/quote`, {
    auth: false,
    body: { scheduleId: schedule.scheduleId, includeDefaultAddons: true, includeDefaultPackageOptions: true, paxGroups: [{ priceType: 1, quantity: 2 }] },
  });
  const packageId = quote.package?.packageId;
  if (!packageId) throw new Error('Quote did not return a packageId.');

  const managerTours = await apiRequest('GET', '/ql-tour/tours?page=1&pageSize=100&includeDeleted=true', { token: manager.accessToken, tenantId: manager.currentTenantId });
  const tour = (managerTours.items || []).find((item) => item.id === publicTour.id) || managerTours.items?.[0];
  if (!tour) throw new Error('Manager tour scope is empty.');

  const schedules = await apiRequest('GET', `/ql-tour/tours/${tour.id}/schedules?page=1&pageSize=100&includeDeleted=true`, { token: manager.accessToken, tenantId: manager.currentTenantId });
  const managerSchedule = (schedules.items || []).find((item) => item.id === schedule.scheduleId) || schedules.items?.[0];
  if (!managerSchedule) throw new Error('Manager schedule scope is empty.');

  const prices = await apiRequest('GET', `/ql-tour/tours/${tour.id}/schedules/${managerSchedule.id}/prices?page=1&pageSize=100&includeDeleted=true`, { token: manager.accessToken, tenantId: manager.currentTenantId });
  const price = prices.items?.[0];
  if (!price) throw new Error('No manager price available.');

  const capacity = await apiRequest('GET', `/ql-tour/tours/${tour.id}/schedules/${managerSchedule.id}/capacity`, { token: manager.accessToken, tenantId: manager.currentTenantId });

  const packages = await apiRequest('GET', `/ql-tour/tours/${tour.id}/packages?page=1&pageSize=100&includeDeleted=true`, { token: manager.accessToken, tenantId: manager.currentTenantId });
  const tourPackage = (packages.items || []).find((item) => item.id === packageId) || packages.items?.[0];
  if (!tourPackage) throw new Error('No manager package available.');

  const content = await ensureManagerContent(manager, tour.id);
  const packageData = await ensureManagerPackageData(manager, tour.id, managerSchedule.id, tourPackage.id);
  const review = await ensureManagerReview(manager, tour.id);

  const admin = await loadSession('admin', 'Admin@12345', manager.currentTenantId);
  const adminReviews = await apiRequest('GET', `/admin/tour-reviews?tenantId=${encodeURIComponent(manager.currentTenantId)}&page=1&pageSize=100`, { token: admin.accessToken, tenantId: manager.currentTenantId });
  const adminFaqs = await apiRequest('GET', `/admin/tour-faqs?tenantId=${encodeURIComponent(manager.currentTenantId)}&tourId=${tour.id}&page=1&pageSize=100&includeDeleted=true`, { token: admin.accessToken, tenantId: manager.currentTenantId });
  const adminSchedules = await apiRequest('GET', `/admin/tour-schedules?tenantId=${encodeURIComponent(manager.currentTenantId)}&tourId=${tour.id}&page=1&pageSize=100&includeDeleted=true`, { token: admin.accessToken, tenantId: manager.currentTenantId });
  const publicPolicies = await apiRequest('GET', `/tours/${tour.id}/policies?page=1&pageSize=20`, { auth: false });
  const publicAddons = await apiRequest('GET', `/tours/${tour.id}/addons?page=1&pageSize=20`, { auth: false });
  const publicFaqs = await apiRequest('GET', `/tours/${tour.id}/faqs?page=1&pageSize=20`, { auth: false });
  const publicItinerary = await apiRequest('GET', `/tours/${tour.id}/itinerary?page=1&pageSize=20`, { auth: false });
  const publicGallery = await apiRequest('GET', `/tours/${tour.id}/gallery?page=1&pageSize=20`, { auth: false });
  const publicReviews = await apiRequest('GET', `/tours/${tour.id}/reviews?page=1&pageSize=20`, { auth: false });

  result.api.setup = {
    tourId: tour.id,
    tourCode: tour.code,
    tourName: tour.name,
    scheduleId: managerSchedule.id,
    scheduleCode: managerSchedule.code,
    priceId: price.id,
    packageId: tourPackage.id,
    packageCode: tourPackage.code,
    capacityStatus: capacity.status,
    contactId: content.contact.id,
    faqId: content.faq.id,
    reviewId: review.id,
    componentId: packageData.component.id,
    optionId: packageData.option.id,
    overrideId: packageData.override.id,
    publicCounts: {
      policies: publicPolicies.items?.length || 0,
      addons: publicAddons.items?.length || 0,
      faqs: publicFaqs.items?.length || 0,
      itineraryDays: publicItinerary.items?.length || 0,
      gallery: publicGallery.items?.length || 0,
      reviews: publicReviews.items?.length || 0,
    },
    adminCounts: {
      reviews: adminReviews.items?.length || 0,
      faqs: adminFaqs.items?.length || 0,
      schedules: adminSchedules.items?.length || 0,
    },
  };

  return { manager, customer, admin, tour, managerSchedule, price, tourPackage, content, packageData, review, quote };
}

async function hydrateSession(page, session) {
  await page.goto(APP_BASE, { waitUntil: 'domcontentloaded' });
  await page.evaluate(({ session, keys }) => {
    const storage = window.localStorage;
    storage.clear();
    storage.setItem(keys.accessToken, session.accessToken);
    storage.setItem(keys.refreshToken, session.refreshToken || '');
    storage.setItem(keys.sessionId, session.sessionId || '');
    storage.setItem(keys.accessTokenExpiresAt, session.expiresAt || session.accessTokenExpiresAt || '');
    storage.setItem(keys.refreshTokenExpiresAt, session.refreshTokenExpiresAt || '');
    storage.setItem(keys.user, JSON.stringify(session.user || null));
    storage.setItem(keys.memberships, JSON.stringify(session.memberships || []));
    if (session.currentTenantId) storage.setItem(keys.currentTenantId, session.currentTenantId);
    storage.setItem(keys.permissions, JSON.stringify(session.permissions || []));
    storage.setItem(keys.remember, 'true');
  }, { session, keys: AUTH_KEYS });
}

function bindPageDiagnostics(page, label) {
  page.on('pageerror', (error) => {
    result.pageErrors.push({ label, message: error.message, stack: error.stack || null });
  });
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      result.consoleErrors.push({ label, text: msg.text() });
    }
  });
  page.on('requestfailed', (request) => {
    const url = request.url();
    if (url.startsWith(APP_BASE) || url.startsWith(API_BASE)) {
      result.failedRequests.push({ label, url, method: request.method(), failure: request.failure()?.errorText || 'requestfailed' });
    }
  });
}

async function screenshot(page, name) {
  const file = path.join(ARTIFACTS_DIR, `${String(result.ui.length).padStart(2, '0')}_${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  return file;
}

async function runUi(setup) {
  const browser = await chromium.launch({ executablePath: CHROME_PATH, headless: true });
  try {
    const publicCtx = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
    const publicPage = await publicCtx.newPage();
    bindPageDiagnostics(publicPage, 'public');

    await publicPage.goto(`${APP_BASE}/tour/results`, { waitUntil: 'networkidle' });
    await publicPage.waitForURL(/\/tour\/results/);
    await publicPage.waitForLoadState('domcontentloaded');
    pushStep('public', 'tour-results-loaded', { screenshot: await screenshot(publicPage, 'public_results') });

    await publicPage.goto(`${APP_BASE}/tour/${setup.tour.id}`, { waitUntil: 'networkidle' });
    await publicPage.waitForLoadState('domcontentloaded');
    await publicPage.locator(`text=${setup.tour.name}`).first().waitFor({ timeout: 15000 });
    pushStep('public', 'tour-detail-loaded', { screenshot: await screenshot(publicPage, 'public_detail') });

    const checkoutUrl = `${APP_BASE}/checkout?type=tour&tourId=${setup.tour.id}&scheduleId=${setup.managerSchedule.id}&packageId=${setup.tourPackage.id}&adult=2&child=0`;
    await publicPage.goto(checkoutUrl, { waitUntil: 'networkidle' });
    await publicPage.locator('aside button').first().click();
    await publicPage.waitForURL(/\/auth\/login/);
    pushStep('customer', 'redirected-to-login-from-checkout', { screenshot: await screenshot(publicPage, 'customer_login_redirect') });

    await publicPage.locator('input[type="text"]').first().fill('customer');
    await publicPage.locator('input[type="password"]').first().fill('Customer@12345');
    await publicPage.locator('form button').last().click();
    await publicPage.waitForURL(/\/checkout\?type=tour/);
    pushStep('customer', 'returned-to-checkout-after-login', { screenshot: await screenshot(publicPage, 'customer_checkout_after_login') });

    const textInputs = publicPage.locator('input[type="text"], input[type="tel"], input[type="email"]');
    if (await textInputs.count() >= 3) {
      await textInputs.nth(0).fill('Customer UI Smoke');
      await textInputs.nth(1).fill('0901234567');
      await textInputs.nth(2).fill('customer@ticketbooking.local');
    }

    await publicPage.locator('aside button').first().click();
    await publicPage.waitForURL(/\/ticket\/success\?type=tour/);
    const ticketUrl = new URL(publicPage.url());
    const bookingId = ticketUrl.searchParams.get('bookingId');
    if (!bookingId) throw new Error('Ticket page did not return bookingId.');
    result.api.bookingId = bookingId;
    pushStep('customer', 'tour-booking-confirmed', { bookingId, screenshot: await screenshot(publicPage, 'customer_ticket_success') });

    await publicPage.goto(`${APP_BASE}/my-account/bookings`, { waitUntil: 'networkidle' });
    await publicPage.locator(`text=${setup.tour.name}`).first().waitFor({ timeout: 15000 });
    pushStep('customer', 'my-bookings-loaded', { screenshot: await screenshot(publicPage, 'customer_my_bookings') });

    await publicPage.goto(`${APP_BASE}/my-account/bookings/${bookingId}`, { waitUntil: 'networkidle' });
    await publicPage.locator(`text=${setup.tour.name}`).first().waitFor({ timeout: 15000 });
    pushStep('customer', 'booking-detail-loaded', { screenshot: await screenshot(publicPage, 'customer_booking_detail') });

    await publicPage.goto(`${APP_BASE}/my-account/bookings/${bookingId}/cancel`, { waitUntil: 'networkidle' });
    pushStep('customer', 'cancel-booking-page-loaded', { screenshot: await screenshot(publicPage, 'customer_cancel_booking') });
    await publicCtx.close();

    const tenantCtx = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
    const tenantPage = await tenantCtx.newPage();
    bindPageDiagnostics(tenantPage, 'tenant');
    await hydrateSession(tenantPage, setup.manager);

    const tenantPages = [
      [`${APP_BASE}/tenant/inventory/tour?tourId=${setup.tour.id}`, setup.tour.code, 'tenant_inventory'],
      [`${APP_BASE}/tenant/operations/tour/schedules?tourId=${setup.tour.id}&scheduleId=${setup.managerSchedule.id}`, setup.managerSchedule.code, 'tenant_schedules'],
      [`${APP_BASE}/tenant/operations/tour/pricing?tourId=${setup.tour.id}&scheduleId=${setup.managerSchedule.id}&priceId=${setup.price.id}`, setup.tour.name, 'tenant_pricing'],
      [`${APP_BASE}/tenant/operations/tour/capacity?tourId=${setup.tour.id}&scheduleId=${setup.managerSchedule.id}`, setup.tour.name, 'tenant_capacity'],
      [`${APP_BASE}/tenant/operations/tour/packages?tourId=${setup.tour.id}&packageId=${setup.tourPackage.id}`, setup.tourPackage.code, 'tenant_packages'],
      [`${APP_BASE}/tenant/operations/tour/content?tourId=${setup.tour.id}&tab=contacts&itemId=${setup.content.contact.id}`, `${PREFIX}_contact`, 'tenant_content'],
      [`${APP_BASE}/tenant/operations/tour/experience?tourId=${setup.tour.id}&tab=itinerary&itemId=${setup.content.day.id}`, `${PREFIX}_day1`, 'tenant_experience'],
      [`${APP_BASE}/tenant/operations/tour/package-builder?tourId=${setup.tour.id}&packageId=${setup.tourPackage.id}&componentId=${setup.packageData.component.id}&optionId=${setup.packageData.option.id}&scheduleId=${setup.managerSchedule.id}`, `${PREFIX} Component`, 'tenant_package_builder'],
      [`${APP_BASE}/tenant/operations/tour/reporting?tourId=${setup.tour.id}`, setup.tour.name, 'tenant_reporting'],
      [`${APP_BASE}/tenant/bookings`, setup.tour.name, 'tenant_bookings'],
      [`${APP_BASE}/tenant/reviews`, `${PREFIX}_review`, 'tenant_reviews'],
    ];

    for (const [url, text, shot] of tenantPages) {
      await tenantPage.goto(url, { waitUntil: 'networkidle' });
      await tenantPage.waitForFunction((expected) => document.body.innerText.includes(expected), text, { timeout: 15000 });
      pushStep('tenant', shot, { screenshot: await screenshot(tenantPage, shot) });
    }
    await tenantCtx.close();

    const adminCtx = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
    const adminPage = await adminCtx.newPage();
    bindPageDiagnostics(adminPage, 'admin');
    await hydrateSession(adminPage, setup.admin);

    const adminPages = [
      [`${APP_BASE}/admin/tours`, setup.tour.code, 'admin_tours'],
      [`${APP_BASE}/admin/tour-schedules`, setup.managerSchedule.code, 'admin_tour_schedules'],
      [`${APP_BASE}/admin/tour-faqs`, `${PREFIX}_faq?`, 'admin_tour_faqs'],
      [`${APP_BASE}/admin/tour-reviews`, `${PREFIX}_review`, 'admin_tour_reviews'],
    ];

    for (const [url, text, shot] of adminPages) {
      await adminPage.goto(url, { waitUntil: 'networkidle' });
      await adminPage.waitForFunction((expected) => document.body.innerText.includes(expected), text, { timeout: 15000 });
      pushStep('admin', shot, { screenshot: await screenshot(adminPage, shot) });
    }
    await adminCtx.close();
  } finally {
    await browser.close();
  }
}

(async () => {
  try {
    const setup = await setupData();
    await runUi(setup);
    result.completedAt = new Date().toISOString();
    result.ok = true;
  } catch (error) {
    result.completedAt = new Date().toISOString();
    result.ok = false;
    result.error = { message: error.message, stack: error.stack };
    console.error(error);
    process.exitCode = 1;
  } finally {
    fs.writeFileSync(path.join(OUT_DIR, 'tours-live-test-result.json'), JSON.stringify(result, null, 2));
  }
})();
