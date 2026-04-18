const fs = require('fs');
const path = require('path');
const { chromium } = require('../phase6-ui-test/node_modules/playwright');

const FRONTEND_URL = process.env.FRONTEND_URL || 'http://127.0.0.1:4177';
const API_URL = process.env.API_URL || 'http://127.0.0.1:5193/api/v1';
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const OUT_DIR = __dirname;
const ARTIFACT_DIR = path.join(OUT_DIR, 'artifacts');

fs.mkdirSync(ARTIFACT_DIR, { recursive: true });

const result = {
  generatedAt: new Date().toISOString(),
  frontendUrl: FRONTEND_URL,
  apiUrl: API_URL,
  demo: {},
  api: {},
  public: {},
  tenant: {},
  admin: {},
  ui: [],
  consoleErrors: [],
  pageErrors: [],
  apiErrors: [],
  ok: false,
};

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function detectMojibake(text) {
  return /Ã[\u0080-\u00BF]|Â[\u0080-\u00BF]|Ä[\u0080-\u00BF]|�|ï¿½/.test(String(text || ''));
}

async function apiRequest(method, pathName, { token, tenantId, auth = true, body } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (auth && token) {
    headers.Authorization = `Bearer ${token}`;
  }
  if (tenantId) {
    headers['X-TenantId'] = tenantId;
  }

  const response = await fetch(`${API_URL}${pathName}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();
  let data = null;

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

async function screenshot(page, name) {
  const filePath = path.join(ARTIFACT_DIR, `${name}.png`);
  await page.screenshot({ path: filePath, fullPage: true });
  return filePath;
}

async function waitForPath(page, predicate, timeout = 15000) {
  await page.waitForFunction(
    ({ source }) => {
      const value = `${window.location.pathname}${window.location.search}`;
      return Function(`return (${source})(arguments[0]);`)(value);
    },
    { source: predicate.toString() },
    { timeout },
  );
}

async function waitForBodyText(page, expected, timeout = 15000) {
  await page.waitForFunction(
    ({ needle }) => document.body.innerText.includes(needle),
    { needle: expected },
    { timeout },
  );
}

async function ensureNoMojibake(page, label) {
  const bodyText = await page.locator('body').innerText();
  assert(!detectMojibake(bodyText), `${label} vẫn còn lỗi vỡ tiếng Việt.`);
}

function wireContextDebug(context) {
  context.on('page', (page) => {
    page.on('pageerror', (error) => {
      result.pageErrors.push({ page: page.url(), message: error.message });
    });

    page.on('console', (message) => {
      if (message.type() === 'error') {
        const text = message.text();
        if (text.includes('ERR_FAILED') || text.includes('ERR_INSUFFICIENT_RESOURCES')) {
          return;
        }

        result.consoleErrors.push({ page: page.url(), message: text });
      }
    });

    page.on('response', (response) => {
      if (!response.url().includes('/api/')) return;
      if (response.status() < 400) return;
      result.apiErrors.push({
        page: page.url(),
        url: response.url(),
        status: response.status(),
      });
    });
  });
}

async function optimizeContext(context) {
  await context.route('**/*', (route) => {
    const resourceType = route.request().resourceType();
    if (resourceType === 'image' || resourceType === 'media' || resourceType === 'font') {
      route.abort();
      return;
    }

    route.continue();
  });
}

async function loginFromUi(page, username, password, expectedPathPart) {
  await page.goto(`${FRONTEND_URL}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type="text"]').fill(username);
  await page.locator('input[type="password"]').fill(password);
  await Promise.all([
    waitForPath(page, (pathValue) => !pathValue.includes('/auth/login')),
    page.locator('form button').last().click(),
  ]);

  if (expectedPathPart) {
    await page.waitForFunction(
      ({ expected }) => `${window.location.pathname}${window.location.search}`.includes(expected),
      { expected: expectedPathPart },
      { timeout: 15000 },
    );
  }
}

async function clickFirstFlightSeat(page) {
  const seatText = await page.evaluate(() => {
    const isVisible = (element) => {
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    };

    const buttons = Array.from(document.querySelectorAll('button'));
    const seatButton = buttons.find((button) => {
      const text = (button.innerText || '').trim();
      return /^\d+[A-Z]$/i.test(text) && !button.disabled && isVisible(button);
    });

    if (!seatButton) {
      return null;
    }

    seatButton.click();
    return seatButton.innerText.trim();
  });

  assert(seatText, 'Không tìm thấy ghế máy bay còn trống để chọn.');
  return seatText;
}

async function capturePage(page, area, step, name) {
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  await ensureNoMojibake(page, `${area}/${step}`);
  const filePath = await screenshot(page, name);
  result.ui.push({ area, step, screenshot: filePath, url: page.url() });
  return filePath;
}

async function buildDemoData() {
  const adminAuth = await apiRequest('POST', '/auth/login', {
    auth: false,
    body: { usernameOrEmail: 'admin', password: 'Admin@12345' },
  });
  const managerAuth = await apiRequest('POST', '/auth/login', {
    auth: false,
    body: { usernameOrEmail: 'qlvmm', password: 'QlVmm@12345' },
  });

  const tenants = await apiRequest('GET', '/admin/tenants?page=1&pageSize=100', {
    token: adminAuth.accessToken,
  });
  const flightTenant = (tenants.items || []).find((item) => item.code === 'VMM001');
  assert(flightTenant, 'Không tìm thấy tenant VMM001 cho Flight.');

  const managerOptions = await apiRequest('GET', '/qlvmm/flight/options', {
    token: managerAuth.accessToken,
    tenantId: flightTenant.id,
  });
  assert((managerOptions.locations || []).length > 0, 'Flight manager options chưa có location airport.');
  assert((managerOptions.airlines || []).length > 0, 'Flight manager options chưa có airline.');
  assert((managerOptions.airports || []).length > 0, 'Flight manager options chưa có airport.');
  assert((managerOptions.flights || []).length > 0, 'Flight manager options chưa có flight.');
  assert((managerOptions.offers || []).length > 0, 'Flight manager options chưa có offer.');
  assert((managerOptions.seatMaps || []).length > 0, 'Flight manager options chưa có cabin seat map.');
  assert((managerOptions.ancillaries || []).length > 0, 'Flight manager options chưa có ancillary.');

  const departDate = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
  const airportLookup = await apiRequest('GET', '/search/flights/airports?limit=20', { auth: false });
  const fromAirport = (airportLookup.items || []).find((item) => item.iataCode === 'SGN');
  const toAirport = (airportLookup.items || []).find((item) => item.iataCode === 'HAN');
  assert(fromAirport && toAirport, 'Không tìm thấy SGN/HAN trong public airport lookup.');

  const publicSearch = await apiRequest('GET', `/search/flights?from=SGN&to=HAN&date=${departDate}`, { auth: false });
  const offer = (publicSearch.items || [])[0];
  assert(offer, 'Không tìm thấy offer demo SGN -> HAN để nghiệm thu Flight.');

  const offerDetail = await apiRequest('GET', `/flight/offers/${offer.offerId}`, { auth: false });
  const offerAncillaries = await apiRequest('GET', `/flight/offers/${offer.offerId}/ancillaries`, { auth: false });
  const seatMap = await apiRequest('GET', `/flight/offers/${offer.offerId}/seat-map`, { auth: false });
  assert(Array.isArray(seatMap?.seats) && seatMap.seats.length > 0, 'Seat map public của Flight chưa có ghế.');

  result.api.publicSearchCount = publicSearch.count || 0;
  result.api.publicAncillaryCount = Array.isArray(offerAncillaries.items) ? offerAncillaries.items.length : 0;
  result.api.publicSeatCount = Array.isArray(seatMap.seats) ? seatMap.seats.length : 0;

  result.api.managerFlights = Array.isArray(managerOptions.flights) ? managerOptions.flights.length : 0;
  result.api.managerOffers = Array.isArray(managerOptions.offers) ? managerOptions.offers.length : 0;
  result.api.managerSeatMaps = Array.isArray(managerOptions.seatMaps) ? managerOptions.seatMaps.length : 0;

  const adminAirlines = await apiRequest('GET', '/admin/flight/airlines?page=1&pageSize=20', {
    token: adminAuth.accessToken,
    tenantId: flightTenant.id,
  });
  const adminFlights = await apiRequest('GET', '/admin/flight/flights?page=1&pageSize=20', {
    token: adminAuth.accessToken,
    tenantId: flightTenant.id,
  });
  const adminOffers = await apiRequest('GET', '/admin/flight/offers?page=1&pageSize=20', {
    token: adminAuth.accessToken,
    tenantId: flightTenant.id,
  });
  const adminTaxFeeLines = await apiRequest('GET', `/admin/flight/offers/tax-fee-lines?offerId=${offer.offerId}&page=1&pageSize=20`, {
    token: adminAuth.accessToken,
    tenantId: flightTenant.id,
  });
  const adminCabinSeats = await apiRequest('GET', `/admin/flight/cabin-seats?cabinSeatMapId=${seatMap.cabinSeatMap.id}&page=1&pageSize=20`, {
    token: adminAuth.accessToken,
    tenantId: flightTenant.id,
  });

  result.api.adminAirlines = adminAirlines.total || 0;
  result.api.adminFlights = adminFlights.total || 0;
  result.api.adminOffers = adminOffers.total || 0;
  result.api.adminTaxFeeLines = adminTaxFeeLines.total || 0;
  result.api.adminCabinSeats = adminCabinSeats.total || 0;

  return {
    departDate,
    adminAuth,
    managerAuth,
    flightTenant,
    managerOptions,
    fromAirport,
    toAirport,
    offer,
    offerDetail,
    offerAncillaries,
    seatMap,
  };
}

async function testPublicFlow(browser, demo) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
  await optimizeContext(context);
  wireContextDebug(context);
  const page = await context.newPage();

  const resultsUrl = `${FRONTEND_URL}/flight/results?from=SGN&to=HAN&date=${demo.departDate}&passengers=1`;
  await page.goto(resultsUrl, { waitUntil: 'domcontentloaded' });
  await page.locator(`a[href="/flight/detail?offerId=${demo.offer.offerId}"]`).first().waitFor({ state: 'visible', timeout: 15000 });
  result.public.resultsScreenshot = await capturePage(page, 'public', 'results', '01_public_flight_results');

  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/flight/detail')),
    page.locator(`a[href="/flight/detail?offerId=${demo.offer.offerId}"]`).first().click(),
  ]);
  await waitForBodyText(page, demo.offerDetail.flight.flightNumber);
  result.public.detailScreenshot = await capturePage(page, 'public', 'detail', '02_public_flight_detail');

  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/flight/seat-selection')),
    page.locator(`a[href="/flight/seat-selection?offerId=${demo.offer.offerId}"]`).first().click(),
  ]);
  await page.waitForFunction(
    () => Array.from(document.querySelectorAll('button')).some((button) => /^\d+[A-Z]$/i.test((button.innerText || '').trim())),
    undefined,
    { timeout: 20000 },
  );
  result.public.seatSelectionScreenshot = await capturePage(page, 'public', 'seat-selection', '03_public_flight_seat_selection');

  result.public.selectedSeat = await clickFirstFlightSeat(page);
  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/auth/login')),
    page.locator('aside button').click(),
  ]);
  result.public.loginRedirectScreenshot = await capturePage(page, 'public', 'login-redirect', '04_public_flight_login_redirect');

  await page.locator('input[type="text"]').fill('customer');
  await page.locator('input[type="password"]').fill('Customer@12345');
  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/flight/seat-selection')),
    page.locator('form button').last().click(),
  ]);

  await page.waitForFunction(
    () => Array.from(document.querySelectorAll('button')).some((button) => /^\d+[A-Z]$/i.test((button.innerText || '').trim())),
    undefined,
    { timeout: 20000 },
  );
  result.public.seatSelectionAfterLoginScreenshot = await capturePage(page, 'public', 'seat-selection-after-login', '05_public_flight_seat_selection_after_login');

  result.public.selectedSeatAfterLogin = await clickFirstFlightSeat(page);
  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/checkout?product=flight')),
    page.locator('aside button').click(),
  ]);
  await waitForBodyText(page, demo.offerDetail.flight.flightNumber);
  result.public.checkoutScreenshot = await capturePage(page, 'public', 'checkout', '06_public_flight_checkout');

  await context.close();
}

async function testTenantFlow(browser, demo) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
  await optimizeContext(context);
  wireContextDebug(context);
  const page = await context.newPage();

  await loginFromUi(page, 'qlvmm', 'QlVmm@12345', '/tenant');

  const headerText = await page.locator('header').innerText();
  result.tenant.operatorHeader = headerText;
  assert(
    headerText.includes('FLIGHT OPERATOR') || headerText.includes('Quản lý vé máy bay'),
    'Header tenant của QLVMM chưa hiển thị đúng operator hàng không.',
  );

  result.tenant.menu = {
    flightInventory: await page.locator('a[href="/tenant/inventory/flight"]').count(),
    flightOperations: await page.locator('a[href="/tenant/operations/flight"]').count(),
    flightProviders: await page.locator('a[href="/tenant/providers/flight"]').count(),
    busInventory: await page.locator('a[href="/tenant/inventory/bus"]').count(),
    trainInventory: await page.locator('a[href="/tenant/inventory/train"]').count(),
    hotelInventory: await page.locator('a[href="/tenant/inventory/hotel"]').count(),
    tourInventory: await page.locator('a[href="/tenant/inventory/tour"]').count(),
  };

  assert(result.tenant.menu.flightInventory > 0, 'QLVMM chưa thấy menu kho vé máy bay.');
  assert(result.tenant.menu.flightOperations > 0, 'QLVMM chưa thấy menu vận hành hàng không.');
  assert(result.tenant.menu.flightProviders > 0, 'QLVMM chưa thấy menu đội bay & ghế cabin.');
  assert(result.tenant.menu.busInventory === 0, 'QLVMM vẫn thấy menu bus.');
  assert(result.tenant.menu.trainInventory === 0, 'QLVMM vẫn thấy menu train.');
  assert(result.tenant.menu.hotelInventory === 0, 'QLVMM vẫn thấy menu hotel.');
  assert(result.tenant.menu.tourInventory === 0, 'QLVMM vẫn thấy menu tour.');

  await page.goto(`${FRONTEND_URL}/tenant/inventory/train`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1200);
  result.tenant.routeGuardOk = !page.url().includes('/tenant/inventory/train');
  assert(result.tenant.routeGuardOk, 'Route guard tenant chưa chặn module train cho QLVMM.');

  const pages = [
    ['/tenant/inventory/flight', 'inventory', 'tenant_flight_inventory', 'a[href="/tenant/operations/flight/flights"]'],
    ['/tenant/operations/flight', 'operations-overview', 'tenant_flight_operations', 'a[href="/tenant/operations/flight/airlines"]'],
    ['/tenant/operations/flight/airlines', 'airlines', 'tenant_flight_airlines', 'form'],
    ['/tenant/operations/flight/airports', 'airports', 'tenant_flight_airports', 'form select'],
    ['/tenant/operations/flight/fare-classes', 'fare-classes', 'tenant_flight_fare_classes', 'form'],
    ['/tenant/operations/flight/fare-rules', 'fare-rules', 'tenant_flight_fare_rules', 'form'],
    ['/tenant/operations/flight/flights', 'flights', 'tenant_flight_flights', 'form'],
    ['/tenant/operations/flight/offers', 'offers', 'tenant_flight_offers', 'form'],
    [`/tenant/operations/flight/tax-fee-lines?offerId=${demo.offer.offerId}`, 'tax-fee-lines', 'tenant_flight_tax_fee_lines', 'form'],
    ['/tenant/providers/flight', 'providers-overview', 'tenant_flight_providers', 'a[href="/tenant/providers/flight/aircrafts"]'],
    ['/tenant/providers/flight/aircraft-models', 'aircraft-models', 'tenant_flight_aircraft_models', 'form'],
    ['/tenant/providers/flight/aircrafts', 'aircrafts', 'tenant_flight_aircrafts', 'form'],
    ['/tenant/providers/flight/seat-maps', 'seat-maps', 'tenant_flight_seat_maps', 'form'],
    [`/tenant/providers/flight/seats?cabinSeatMapId=${demo.seatMap.cabinSeatMap.id}`, 'seats', 'tenant_flight_seats', 'form'],
    ['/tenant/providers/flight/ancillaries', 'ancillaries', 'tenant_flight_ancillaries', 'form'],
  ];

  for (const [url, step, fileName, selector] of pages) {
    await page.goto(`${FRONTEND_URL}${url}`, { waitUntil: 'domcontentloaded' });
    await page.locator(selector).first().waitFor({ state: 'visible', timeout: 15000 });
    result.tenant[step] = await capturePage(page, 'tenant', step, fileName);
  }

  await context.close();
}

async function testAdminFlow(browser, demo) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
  await optimizeContext(context);
  wireContextDebug(context);
  const page = await context.newPage();

  await loginFromUi(page, 'admin', 'Admin@12345', '/admin');
  await page.goto(`${FRONTEND_URL}/admin/flight`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => {
    const select = document.querySelector('select');
    return !!select && select.options.length > 0;
  }, undefined, { timeout: 15000 });

  const tenantOptions = await page.evaluate(() => Array.from(document.querySelectorAll('select option')).map((item) => item.textContent?.trim() || ''));
  result.admin.tenantOptions = tenantOptions;
  assert(tenantOptions.length > 0, 'Admin Flight chưa có tenant selector.');
  assert(tenantOptions.every((item) => !/NX001|VT001|KS001|TOUR001/i.test(item)), 'Admin Flight vẫn lẫn tenant không phải hàng không.');
  await waitForBodyText(page, 'Đang quản lý dữ liệu hàng không cho tenant');
  result.admin.overview = await capturePage(page, 'admin', 'overview', 'admin_flight_overview');

  const pages = [
    ['/admin/flight/airlines', 'airlines', 'admin_flight_airlines', 'form'],
    ['/admin/flight/airports', 'airports', 'admin_flight_airports', 'form select'],
    ['/admin/flight/aircraft-models', 'aircraft-models', 'admin_flight_aircraft_models', 'form'],
    ['/admin/flight/aircrafts', 'aircrafts', 'admin_flight_aircrafts', 'form'],
    ['/admin/flight/fare-classes', 'fare-classes', 'admin_flight_fare_classes', 'form'],
    ['/admin/flight/fare-rules', 'fare-rules', 'admin_flight_fare_rules', 'form'],
    ['/admin/flight/flights', 'flights', 'admin_flight_flights', 'form'],
    ['/admin/flight/offers', 'offers', 'admin_flight_offers', 'form'],
    [`/admin/flight/tax-fee-lines?offerId=${demo.offer.offerId}`, 'tax-fee-lines', 'admin_flight_tax_fee_lines', 'form'],
    ['/admin/flight/seat-maps', 'seat-maps', 'admin_flight_seat_maps', 'form'],
    [`/admin/flight/seats?cabinSeatMapId=${demo.seatMap.cabinSeatMap.id}`, 'seats', 'admin_flight_seats', 'form'],
    ['/admin/flight/ancillaries', 'ancillaries', 'admin_flight_ancillaries', 'form'],
  ];

  for (const [url, step, fileName, selector] of pages) {
    await page.goto(`${FRONTEND_URL}${url}`, { waitUntil: 'domcontentloaded' });
    await page.locator(selector).first().waitFor({ state: 'visible', timeout: 15000 });
    await waitForBodyText(page, 'Đang quản lý dữ liệu hàng không cho tenant');
    result.admin[step] = await capturePage(page, 'admin', step, fileName);
  }

  await context.close();
}

async function main() {
  let browser;

  try {
    const demo = await buildDemoData();
    result.demo = {
      departDate: demo.departDate,
      tenantId: demo.flightTenant.id,
      tenantCode: demo.flightTenant.code,
      offerId: demo.offer.offerId,
      flightNumber: demo.offerDetail.flight.flightNumber,
      seatMapId: demo.seatMap.cabinSeatMap.id,
    };

    const launchOptions = {
      headless: true,
      channel: undefined,
    };

    if (fs.existsSync(CHROME_PATH)) {
      launchOptions.executablePath = CHROME_PATH;
    }

    browser = await chromium.launch(launchOptions);

    await testPublicFlow(browser, demo);
    await testTenantFlow(browser, demo);
    await testAdminFlow(browser, demo);

    result.ok = result.consoleErrors.length === 0 && result.pageErrors.length === 0 && result.apiErrors.length === 0;
  } catch (error) {
    result.ok = false;
    result.failure = {
      message: error.message,
      stack: error.stack,
    };
  } finally {
    if (browser) {
      await browser.close().catch(() => {});
    }

    fs.writeFileSync(path.join(OUT_DIR, 'flight-live-test-result.json'), JSON.stringify(result, null, 2));
    if (!result.ok) {
      process.exitCode = 1;
    }
  }
}

main();
