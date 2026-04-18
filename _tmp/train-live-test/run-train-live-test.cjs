const fs = require('fs');
const path = require('path');
const { chromium } = require('../phase6-ui-test/node_modules/playwright');

const FRONTEND_URL = process.env.FRONTEND_URL || 'http://127.0.0.1:4176';
const API_URL = process.env.API_URL || 'http://127.0.0.1:5192/api/v1';
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const OUT_DIR = __dirname;
const ARTIFACT_DIR = path.join(OUT_DIR, 'artifacts');

fs.mkdirSync(ARTIFACT_DIR, { recursive: true });

const result = {
  generatedAt: new Date().toISOString(),
  frontendUrl: FRONTEND_URL,
  apiUrl: API_URL,
  public: {},
  tenant: {},
  consoleErrors: [],
  pageErrors: [],
  apiErrors: [],
  ui: [],
  ok: false,
};

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function detectMojibake(text) {
  return /Ã[\u0080-\u00BF]|Â[\u0080-\u00BF]|Ä[\u0080-\u00BF]|�/.test(String(text || ''));
}

async function apiRequest(method, pathName, { token, body, auth = true, tenantId } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (auth && token) headers.Authorization = `Bearer ${token}`;
  if (tenantId) headers['X-TenantId'] = tenantId;

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

function wireContextDebug(context) {
  context.on('page', (page) => {
    page.on('pageerror', (error) => {
      result.pageErrors.push({ page: page.url(), message: error.message });
    });

    page.on('console', (message) => {
      if (message.type() === 'error') {
        if (
          message.text().includes('net::ERR_INSUFFICIENT_RESOURCES')
          || message.text().includes('Failed to load resource: net::ERR_FAILED')
        ) {
          return;
        }

        result.consoleErrors.push({ page: page.url(), message: message.text() });
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

async function ensureNoMojibake(page, label) {
  const text = await page.locator('body').innerText();
  assert(!detectMojibake(text), `${label} vẫn còn lỗi vỡ tiếng Việt.`);
}

async function loginFromUi(page, username, password) {
  await page.goto(`${FRONTEND_URL}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type="text"]').fill(username);
  await page.locator('input[type="password"]').fill(password);
  await Promise.all([
    waitForPath(page, (pathValue) => !pathValue.includes('/auth/login')),
    page.locator('form button').last().click(),
  ]);
}

async function clickFirstTrainSeat(page) {
  const clicked = await page.evaluate(() => {
    const isVisible = (element) => {
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    };

    const buttons = Array.from(document.querySelectorAll('button'));
    const seatButton = buttons.find((button) => {
      const text = (button.innerText || '').trim();
      return /^\d{2,3}$/.test(text) && !button.disabled && isVisible(button);
    });

    if (!seatButton) {
      return null;
    }

    seatButton.click();
    return seatButton.innerText.trim();
  });

  assert(clicked, 'Không tìm thấy ghế tàu còn trống để chọn.');
  return clicked;
}

async function waitForTrainSeatsVisible(page, timeout = 20000) {
  await page.waitForFunction(
    () => Array.from(document.querySelectorAll('button')).some((button) => /^\d{2,3}$/.test((button.innerText || '').trim())),
    undefined,
    { timeout },
  );
}

async function buildDemoData() {
  const publicLocations = await apiRequest('GET', '/train/search/locations?limit=50', { auth: false });
  const from = publicLocations.items.find((item) => item.name === 'TP. Hồ Chí Minh');
  const to = publicLocations.items.find((item) => item.name === 'Đà Nẵng');
  assert(from && to, 'Không tìm thấy location demo TP. Hồ Chí Minh/Đà Nẵng cho Train.');

  const departDate = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
  const publicTrips = await apiRequest('GET', `/train/search/trips?fromLocationId=${from.id}&toLocationId=${to.id}&departDate=${departDate}&passengers=1`, { auth: false });
  const trip = publicTrips.items?.[0];
  assert(trip, 'Không tìm thấy chuyến tàu demo để nghiệm thu Train.');

  const managerAuth = await apiRequest('POST', '/auth/login', {
    auth: false,
    body: { usernameOrEmail: 'qlvt', password: 'QlVt@12345' },
  });

  const options = await apiRequest('GET', '/qlvt/train/options', { token: managerAuth.accessToken });
  const managerTrip = (options.trips || []).find((item) => item.id === trip.tripId) || options.trips?.[0];
  assert(managerTrip, 'Không lấy được dữ liệu chuyến train của QLVT.');

  const managerCar = (options.cars || []).find((item) => item.tripId === managerTrip.id) || options.cars?.[0];
  assert(managerCar, 'Không lấy được toa tàu demo của QLVT.');

  return {
    departDate,
    from,
    to,
    trip,
    managerTrip,
    managerCar,
  };
}

async function testPublicFlow(browser, demo) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
  await optimizeContext(context);
  wireContextDebug(context);
  const page = await context.newPage();

  const resultBucket = {
    selectedSeat: '',
    holdToken: '',
  };

  const resultsUrl = `${FRONTEND_URL}/train/results?fromLocationId=${demo.from.id}&toLocationId=${demo.to.id}&departDate=${demo.departDate}&passengers=1`;
  await page.goto(resultsUrl, { waitUntil: 'networkidle' });
  await page.waitForFunction((expected) => document.body.innerText.includes(expected), demo.trip.name, { timeout: 15000 });
  await ensureNoMojibake(page, 'Trang kết quả tàu');
  result.public.resultsScreenshot = await screenshot(page, '01_public_train_results');
  result.ui.push({ area: 'public', step: 'results', screenshot: result.public.resultsScreenshot });

  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/train/details')),
    page.locator(`a[href^="/train/details?tripId=${demo.trip.tripId}"]`).first().click(),
  ]);
  await page.waitForFunction((expected) => document.body.innerText.includes(expected), demo.trip.name, { timeout: 15000 });
  await ensureNoMojibake(page, 'Trang chi tiết tàu');
  result.public.detailScreenshot = await screenshot(page, '02_public_train_detail');
  result.ui.push({ area: 'public', step: 'detail', screenshot: result.public.detailScreenshot });

  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/train/seat-selection')),
    page.locator('a[href^="/train/seat-selection"]').click(),
  ]);
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  await waitForTrainSeatsVisible(page);
  await ensureNoMojibake(page, 'Trang chọn chỗ tàu');
  result.public.seatSelectionScreenshot = await screenshot(page, '03_public_train_seat_selection');
  result.ui.push({ area: 'public', step: 'seat-selection', screenshot: result.public.seatSelectionScreenshot });

  resultBucket.selectedSeat = await clickFirstTrainSeat(page);
  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/auth/login')),
    page.locator('aside button').click(),
  ]);
  result.public.loginRedirectScreenshot = await screenshot(page, '04_public_train_login_redirect');
  result.ui.push({ area: 'public', step: 'login-redirect', screenshot: result.public.loginRedirectScreenshot });

  await page.locator('input[type="text"]').fill('customer');
  await page.locator('input[type="password"]').fill('Customer@12345');
  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/train/seat-selection')),
    page.locator('form button').last().click(),
  ]);
  await waitForTrainSeatsVisible(page);
  await ensureNoMojibake(page, 'Trang chọn chỗ tàu sau login');
  result.public.seatSelectionAfterLoginScreenshot = await screenshot(page, '05_public_train_seat_selection_after_login');
  result.ui.push({ area: 'public', step: 'seat-selection-after-login', screenshot: result.public.seatSelectionAfterLoginScreenshot });

  resultBucket.selectedSeat = await clickFirstTrainSeat(page);
  await Promise.all([
    waitForPath(page, (pathValue) => pathValue.includes('/checkout')),
    page.locator('aside button').click(),
  ]);
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  await ensureNoMojibake(page, 'Trang checkout tàu');

  const checkoutUrl = new URL(page.url());
  resultBucket.holdToken = checkoutUrl.searchParams.get('holdToken') || '';
  assert(resultBucket.holdToken, 'Không lấy được holdToken sau khi giữ chỗ tàu.');
  result.public.checkoutUrl = page.url();
  result.public.checkoutScreenshot = await screenshot(page, '06_public_train_checkout');
  result.ui.push({ area: 'public', step: 'checkout', screenshot: result.public.checkoutScreenshot });

  const checkoutText = await page.locator('body').innerText();
  assert(checkoutText.includes(demo.from.name) && checkoutText.includes(demo.to.name), 'Checkout tàu chưa hiển thị đúng hành trình đã chọn.');

  await context.close();
  return resultBucket;
}

async function testTenantFlow(browser, demo, holdToken) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
  await optimizeContext(context);
  wireContextDebug(context);
  const page = await context.newPage();

  await loginFromUi(page, 'qlvt', 'QlVt@12345');
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});

  const trainMenuCount = await page.locator('a[href="/tenant/inventory/train"]').count();
  const busMenuCount = await page.locator('a[href="/tenant/inventory/bus"]').count();
  assert(trainMenuCount > 0, 'QLVT không thấy menu Train.');
  assert(busMenuCount === 0, 'QLVT vẫn thấy menu Bus.');

  await page.goto(`${FRONTEND_URL}/tenant/inventory/bus`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  assert(!page.url().includes('/tenant/inventory/bus'), 'Route guard tenant chưa chặn module bus cho QLVT.');

  const managerPages = [
    [`${FRONTEND_URL}/tenant/inventory/train`, demo.managerTrip.name, '07_tenant_train_inventory'],
    [`${FRONTEND_URL}/tenant/operations/train`, 'Vận hành Tàu', '08_tenant_train_operations'],
    [`${FRONTEND_URL}/tenant/operations/train/stop-points`, 'Ga tàu', '09_tenant_train_stop_points'],
    [`${FRONTEND_URL}/tenant/operations/train/routes`, 'Tuyến đường', '10_tenant_train_routes'],
    [`${FRONTEND_URL}/tenant/operations/train/trip-stop-times?tripId=${demo.managerTrip.id}`, demo.managerTrip.name, '11_tenant_train_trip_stop_times'],
    [`${FRONTEND_URL}/tenant/operations/train/trip-segment-prices?tripId=${demo.managerTrip.id}`, 'Giá chặng', '12_tenant_train_segment_prices'],
    [`${FRONTEND_URL}/tenant/providers/train`, 'Toa tàu', '13_tenant_train_providers'],
    [`${FRONTEND_URL}/tenant/providers/train/cars?tripId=${demo.managerTrip.id}`, demo.managerTrip.name, '14_tenant_train_cars'],
    [`${FRONTEND_URL}/tenant/providers/train/car-seats?carId=${demo.managerCar.id}`, demo.managerCar.carNumber, '15_tenant_train_car_seats'],
    [`${FRONTEND_URL}/tenant/providers/train/seats?tripId=${demo.managerTrip.id}&fromTripStopTimeId=${demo.trip.segment.fromTripStopTimeId}&toTripStopTimeId=${demo.trip.segment.toTripStopTimeId}`, 'Sơ đồ chỗ theo chuyến', '16_tenant_train_trip_seats'],
    [`${FRONTEND_URL}/tenant/providers/train/seat-holds?tripId=${demo.managerTrip.id}`, holdToken, '17_tenant_train_seat_holds_before_release'],
  ];

  for (const [url, expectedText, shotName] of managerPages) {
    await page.goto(url, { waitUntil: 'networkidle' });
    await page.waitForFunction((expected) => document.body.innerText.includes(expected), expectedText, { timeout: 15000 });
    await ensureNoMojibake(page, shotName);
    const shot = await screenshot(page, shotName);
    result.ui.push({ area: 'tenant', step: shotName, screenshot: shot });
  }

  const bodyText = await page.locator('body').innerText();
  assert(bodyText.includes(holdToken), 'QLVT không thấy hold token của khách trong màn seat holds.');

  const releaseButton = page.locator('button', { hasText: /Giải phóng/ }).first();
  await releaseButton.click();
  await page.waitForTimeout(1500);
  await page.reload({ waitUntil: 'networkidle' });
  const afterReleaseText = await page.locator('body').innerText();
  assert(!afterReleaseText.includes(holdToken), 'QLVT chưa giải phóng được hold của khách.');
  result.tenant.afterReleaseScreenshot = await screenshot(page, '18_tenant_train_seat_holds_after_release');
  result.ui.push({ area: 'tenant', step: 'seat-holds-after-release', screenshot: result.tenant.afterReleaseScreenshot });

  await context.close();
}

async function main() {
  const demo = await buildDemoData();
  result.demo = demo;

  const browser = await chromium.launch({
    headless: true,
    executablePath: fs.existsSync(CHROME_PATH) ? CHROME_PATH : undefined,
  });

  try {
    const publicState = await testPublicFlow(browser, demo);
    result.public.selectedSeat = publicState.selectedSeat;
    result.public.holdToken = publicState.holdToken;

    await testTenantFlow(browser, demo, publicState.holdToken);

    assert(result.consoleErrors.length === 0, 'Có console error trong quá trình nghiệm thu Train.');
    assert(result.pageErrors.length === 0, 'Có page error trong quá trình nghiệm thu Train.');
    assert(result.apiErrors.length === 0, 'Có API error trong quá trình nghiệm thu Train.');

    result.ok = true;
  } finally {
    await browser.close();
    fs.writeFileSync(path.join(OUT_DIR, 'train-live-test-result.json'), JSON.stringify(result, null, 2));
  }
}

main().catch((error) => {
  result.ok = false;
  result.error = { message: error.message, stack: error.stack };
  fs.writeFileSync(path.join(OUT_DIR, 'train-live-test-result.json'), JSON.stringify(result, null, 2));
  console.error(error);
  process.exitCode = 1;
});
