const { chromium } = require('playwright');
const fs = require('node:fs/promises');
const path = require('node:path');

const FRONTEND_URL = 'http://127.0.0.1:4173';
const API_URL = 'http://localhost:5183/api/v1';
const ARTIFACT_DIR = path.join(__dirname, 'artifacts');
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function detectMojibake(text) {
  return /Ã[\u0080-\u00BF]|Ä[\u0080-\u00BF]|Â[\u0080-\u00BF]|�/.test(text || '');
}

function summarizeMojibake(text) {
  const source = String(text || '');
  const match = source.match(/.{0,30}(?:Ã[\u0080-\u00BF]|Ä[\u0080-\u00BF]|Â[\u0080-\u00BF]|�).{0,50}/);
  return match ? match[0] : '';
}

async function ensureDir(dir) {
  await fs.mkdir(dir, { recursive: true });
}

async function readJson(url, options) {
  const response = await fetch(url, options);
  if (!response.ok) {
    throw new Error(`API ${response.status}: ${url}`);
  }

  return response.json();
}

async function waitForPath(page, predicate, timeout = 15000) {
  await page.waitForFunction(
    ({ source }) => {
      const path = `${window.location.pathname}${window.location.search}`;
      // eslint-disable-next-line no-new-func
      return Function(`return (${source})(arguments[0]);`)(path);
    },
    { source: predicate.toString() },
    { timeout },
  );
}

async function getDemoTrip() {
  const locations = await readJson(`${API_URL}/bus/search/locations?limit=50`);
  const hcm = locations.items.find((item) => item.name === 'TP. Hồ Chí Minh');
  const daLat = locations.items.find((item) => item.name === 'Đà Lạt');

  assert(hcm && daLat, 'Không tìm thấy location demo HCM/Đà Lạt.');

  const trips = await readJson(
    `${API_URL}/bus/search/trips?fromLocationId=${hcm.id}&toLocationId=${daLat.id}&departDate=2026-04-10&passengers=1`,
  );

  assert(Array.isArray(trips.items) && trips.items.length > 0, 'Không tìm thấy chuyến bus demo để test public search.');

  return {
    fromLocationId: hcm.id,
    toLocationId: daLat.id,
    departDate: '2026-04-10',
    trip: trips.items[0],
  };
}

function wireContextDebug(context, bucket) {
  context.on('page', (page) => {
    page.on('pageerror', (error) => {
      bucket.pageErrors.push({
        page: page.url(),
        message: error.message,
      });
    });

    page.on('console', (message) => {
      if (message.type() === 'error') {
        bucket.consoleErrors.push({
          page: page.url(),
          message: message.text(),
        });
      }
    });

    page.on('response', async (response) => {
      const url = response.url();
      if (!url.includes('/api/')) {
        return;
      }

      if (response.status() < 400) {
        return;
      }

      bucket.apiErrors.push({
        page: page.url(),
        status: response.status(),
        url,
      });
    });
  });
}

async function loginFromUi(page, username, password) {
  await page.goto(`${FRONTEND_URL}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type="text"]').fill(username);
  await page.locator('input[type="password"]').fill(password);
  await Promise.all([
    waitForPath(page, (path) => !path.includes('/auth/login')),
    page.locator('form button').last().click(),
  ]);
}

async function capturePage(page, name, results) {
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(800);

  const bodyText = await page.locator('body').innerText();
  const target = path.join(ARTIFACT_DIR, `${name}.png`);
  await page.screenshot({ path: target, fullPage: true });

  results.pages.push({
    name,
    url: page.url(),
    screenshot: target,
    mojibakeDetected: detectMojibake(bodyText),
    mojibakeSample: summarizeMojibake(bodyText),
  });
}

async function openTenantPage(page, url, name, results, selector) {
  await page.goto(`${FRONTEND_URL}${url}`, { waitUntil: 'domcontentloaded' });
  if (selector) {
    await page.locator(selector).first().waitFor({ state: 'visible', timeout: 15000 });
  }
  await capturePage(page, name, results);
}

async function selectFirstAvailableSeat(page) {
  await page.waitForFunction(() => (
    [...document.querySelectorAll('button')]
      .some((node) => !node.disabled && /^\d{4}$/.test(node.innerText.trim()))
  ), undefined, { timeout: 15000 });

  const seatNumbers = await page.locator('button').evaluateAll((nodes) => (
    nodes
      .filter((node) => !node.disabled && /^\d{4}$/.test(node.innerText.trim()))
      .map((node) => node.innerText.trim())
  ));

  const seatNumber = seatNumbers[0];
  assert(seatNumber, 'Không tìm thấy ghế còn trống để test.');
  await page.locator(`button:has-text("${seatNumber}")`).click();
  return seatNumber;
}

async function testTenantFlow(browser, demoTrip) {
  const debug = { apiErrors: [], consoleErrors: [], pageErrors: [] };
  const results = {
    loginOk: false,
    operatorBadge: '',
    menu: {},
    routeGuardOk: false,
    pages: [],
    debug,
  };

  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  wireContextDebug(context, debug);
  const page = await context.newPage();

  await loginFromUi(page, 'qlnx', 'QlNx@12345');
  results.loginOk = page.url().includes('/tenant');

  const badgeText = await page.locator('header').textContent();
  results.operatorBadge = badgeText || '';
  assert(results.loginOk, 'QLNX đăng nhập xong nhưng không vào tenant portal.');
  assert((badgeText || '').includes('BUS OPERATOR'), 'Header tenant không hiển thị BUS OPERATOR.');

  results.menu = {
    busInventory: await page.locator('a[href="/tenant/inventory/bus"]').count(),
    busOperations: await page.locator('a[href="/tenant/operations/bus"]').count(),
    busProviders: await page.locator('a[href="/tenant/providers/bus"]').count(),
    trainInventory: await page.locator('a[href="/tenant/inventory/train"]').count(),
    flightInventory: await page.locator('a[href="/tenant/inventory/flight"]').count(),
    hotelInventory: await page.locator('a[href="/tenant/inventory/hotel"]').count(),
    tourInventory: await page.locator('a[href="/tenant/inventory/tour"]').count(),
  };

  assert(results.menu.busInventory > 0, 'Sidebar không có menu bus inventory cho QLNX.');
  assert(results.menu.busOperations > 0, 'Sidebar không có menu bus operations cho QLNX.');
  assert(results.menu.busProviders > 0, 'Sidebar không có menu bus providers cho QLNX.');
  assert(results.menu.trainInventory === 0, 'QLNX vẫn thấy menu train.');
  assert(results.menu.flightInventory === 0, 'QLNX vẫn thấy menu flight.');
  assert(results.menu.hotelInventory === 0, 'QLNX vẫn thấy menu hotel.');
  assert(results.menu.tourInventory === 0, 'QLNX vẫn thấy menu tour.');

  await page.goto(`${FRONTEND_URL}/tenant/inventory/train`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1200);
  results.routeGuardOk = !page.url().includes('/tenant/inventory/train');
  assert(results.routeGuardOk, 'Route guard tenant chưa chặn được module train cho QLNX.');

  await openTenantPage(page, '/tenant/inventory/bus', 'tenant-bus-inventory', results, 'form select');
  await openTenantPage(page, '/tenant/operations/bus', 'tenant-bus-operations', results, 'a[href="/tenant/operations/bus/stop-points"]');
  await openTenantPage(page, '/tenant/providers/bus', 'tenant-bus-providers', results, 'a[href="/tenant/providers/bus/vehicle-details"]');
  await openTenantPage(page, '/tenant/operations/bus/routes', 'tenant-bus-routes', results, 'form');
  await openTenantPage(page, `/tenant/operations/bus/trip-stop-times?tripId=${demoTrip.trip.tripId}`, 'tenant-bus-trip-stop-times', results, 'select');
  await openTenantPage(page, `/tenant/operations/bus/trip-stop-points?tripId=${demoTrip.trip.tripId}`, 'tenant-bus-trip-stop-points', results, 'select');
  await openTenantPage(page, `/tenant/operations/bus/trip-segment-prices?tripId=${demoTrip.trip.tripId}`, 'tenant-bus-trip-segment-prices', results, 'select');
  await openTenantPage(page, '/tenant/providers/bus/vehicle-details', 'tenant-bus-vehicle-details', results, 'select');
  await openTenantPage(page, `/tenant/providers/bus/seats?tripId=${demoTrip.trip.tripId}`, 'tenant-bus-seats', results, 'select');

  return { context, page, results };
}

async function testCustomerFlow(browser, demoTrip) {
  const debug = { apiErrors: [], consoleErrors: [], pageErrors: [] };
  const results = {
    pages: [],
    redirectedToLogin: false,
    returnedToSeatSelection: false,
    checkoutUrl: '',
    holdToken: '',
    seatNumber: '',
    debug,
  };

  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  wireContextDebug(context, debug);
  const page = await context.newPage();

  const query = new URLSearchParams({
    fromLocationId: demoTrip.fromLocationId,
    toLocationId: demoTrip.toLocationId,
    departDate: demoTrip.departDate,
    passengers: '1',
  });

  await page.goto(`${FRONTEND_URL}/bus/results?${query.toString()}`, { waitUntil: 'domcontentloaded' });
  await page.locator('a[href^="/bus/trip/"]').first().waitFor({ state: 'visible', timeout: 15000 });
  await capturePage(page, 'public-bus-results', results);

  await Promise.all([
    waitForPath(page, (path) => path.includes('/bus/trip/')),
    page.locator('a[href^="/bus/trip/"]').first().click(),
  ]);
  await capturePage(page, 'public-bus-trip-detail', results);

  await Promise.all([
    waitForPath(page, (path) => path.includes('/bus/seat-selection')),
    page.locator('a[href^="/bus/seat-selection"]').click(),
  ]);

  results.seatNumber = await selectFirstAvailableSeat(page);

  await Promise.all([
    waitForPath(page, (path) => path.includes('/auth/login')),
    page.locator('aside button').click(),
  ]);
  results.redirectedToLogin = page.url().includes('/auth/login');
  assert(results.redirectedToLogin, 'Khách chưa đăng nhập nhưng không bị đẩy sang login khi giữ ghế.');
  await capturePage(page, 'public-bus-login-after-seat-selection', results);

  await page.locator('input[type="text"]').fill('customer');
  await page.locator('input[type="password"]').fill('Customer@12345');
  await Promise.all([
    waitForPath(page, (path) => path.includes('/bus/seat-selection')),
    page.locator('form button').last().click(),
  ]);
  results.returnedToSeatSelection = page.url().includes('/bus/seat-selection');
  assert(results.returnedToSeatSelection, 'Login xong không quay lại được trang chọn ghế.');

  results.seatNumber = await selectFirstAvailableSeat(page);
  await Promise.all([
    waitForPath(page, (path) => path.includes('/checkout?')),
    page.locator('aside button').click(),
  ]);

  results.checkoutUrl = page.url();
  const checkoutUrl = new URL(page.url());
  results.holdToken = checkoutUrl.searchParams.get('holdToken') || '';
  assert(results.holdToken, 'Không lấy được holdToken sau khi giữ ghế.');
  await capturePage(page, 'public-bus-checkout-after-hold', results);

  return { context, page, results };
}

async function testSeatHoldFromTenant(page, demoTrip, holdToken, results) {
  await page.goto(`${FRONTEND_URL}/tenant/providers/bus/seat-holds?tripId=${demoTrip.trip.tripId}`, { waitUntil: 'domcontentloaded' });
  await page.locator('select').first().waitFor({ state: 'visible', timeout: 15000 });
  await page.waitForTimeout(1200);
  const bodyText = await page.locator('body').innerText();
  results.holdVisibleInTenant = bodyText.includes(holdToken);
  assert(results.holdVisibleInTenant, 'Tenant không nhìn thấy hold vừa được khách tạo.');
  await capturePage(page, 'tenant-bus-seat-holds-before-release', results);

  const releaseButton = page.locator('button', { hasText: /Giải phóng|Giáº£i phÃ³ng/ }).first();
  await releaseButton.click();
  await page.waitForTimeout(1500);

  const afterText = await page.locator('body').innerText();
  results.holdReleasedFromTenant = !afterText.includes(holdToken);
  await capturePage(page, 'tenant-bus-seat-holds-after-release', results);
}

async function main() {
  await ensureDir(ARTIFACT_DIR);
  const demoTrip = await getDemoTrip();
  const browser = await chromium.launch({
    headless: true,
    executablePath: CHROME_PATH,
  });

  const finalResult = {
    generatedAt: new Date().toISOString(),
    demoTrip,
    tenant: null,
    customer: null,
    overallStatus: 'passed',
  };

  try {
    const tenantState = await testTenantFlow(browser, demoTrip);
    finalResult.tenant = tenantState.results;

    const customerState = await testCustomerFlow(browser, demoTrip);
    finalResult.customer = customerState.results;

    await testSeatHoldFromTenant(tenantState.page, demoTrip, customerState.results.holdToken, tenantState.results);
  } catch (error) {
    finalResult.overallStatus = 'failed';
    finalResult.failure = {
      message: error.message,
      stack: error.stack,
    };
  } finally {
    await browser.close();
  }

  const outputPath = path.join(__dirname, 'phase6-ui-test-result.json');
  await fs.writeFile(outputPath, JSON.stringify(finalResult, null, 2), 'utf8');

  if (finalResult.overallStatus !== 'passed') {
    console.error(JSON.stringify(finalResult, null, 2));
    process.exitCode = 1;
    return;
  }

  console.log(JSON.stringify(finalResult, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
