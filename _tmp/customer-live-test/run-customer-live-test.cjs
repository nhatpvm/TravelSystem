const fs = require('fs');
const path = require('path');
const { chromium } = require('../tours-live-test/node_modules/playwright-core');

const API_BASE = process.env.API_BASE || 'http://127.0.0.1:5183/api/v1';
const APP_BASE = process.env.APP_BASE || 'http://127.0.0.1:4173';
const CHROME_PATH = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const OUT_DIR = __dirname;
const ARTIFACTS_DIR = path.join(OUT_DIR, 'artifacts');
const WEBHOOK_SECRET = process.env.SEPAY_WEBHOOK_SECRET || 'tbv3-customer-webhook-secret';

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

const result = {
  startedAt: new Date().toISOString(),
  apiBase: API_BASE,
  appBase: APP_BASE,
  account: {},
  orders: {},
  lists: {},
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
  return /Ã[\u0080-\u00BF]|Â[\u0080-\u00BF]|Ä[\u0080-\u00BF]|�/.test(String(text || ''));
}

async function apiRequest(method, pathName, { token, headers = {}, body, auth = true } = {}) {
  const requestHeaders = {
    'Content-Type': 'application/json',
    ...headers,
  };

  if (auth && token) {
    requestHeaders.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${pathName}`, {
    method,
    headers: requestHeaders,
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

function buildPassenger(fullName, passengerType, email, phone) {
  return {
    fullName,
    passengerType,
    gender: passengerType === 'child' ? 'female' : 'male',
    email,
    phoneNumber: phone,
  };
}

async function registerCustomer(email, password, fullName) {
  await apiRequest('POST', '/auth/register', {
    auth: false,
    body: {
      userName: email.split('@')[0],
      email,
      password,
      fullName,
      phoneNumber: '0901000000',
    },
  });
}

async function loginCustomer(usernameOrEmail, password) {
  return apiRequest('POST', '/auth/login', {
    auth: false,
    body: {
      usernameOrEmail,
      password,
      rememberMe: true,
    },
  });
}

async function getFirstTourSeed() {
  const list = await apiRequest('GET', '/tours?page=1&pageSize=10&upcomingOnly=true', { auth: false });
  const tour = (list?.items || [])[0];
  assert(tour?.id, 'Không tìm thấy tour public để test customer commerce.');

  const detail = await apiRequest('GET', `/tours/${tour.id}`, { auth: false });
  const schedule = (detail?.upcomingSchedules || [])[0];
  assert(schedule?.id, 'Tour public hiện chưa có lịch khởi hành phù hợp để test.');

  const quote = await apiRequest('POST', `/tours/${tour.id}/quote`, {
    auth: false,
    body: {
      scheduleId: schedule.id,
      includeDefaultAddons: true,
      includeDefaultPackageOptions: true,
      paxGroups: [
        { priceType: 1, quantity: 2 },
      ],
    },
  });

  const packageId = quote?.package?.packageId || null;
  assert(packageId, 'Tour public chưa resolve được packageId cho checkout customer.');

  return { tour, detail, schedule, packageId, quote };
}

async function createTourOrder(token, seed, email, note) {
  return apiRequest('POST', '/customer/orders', {
    token,
    body: {
      productType: 'tour',
      tourId: seed.tour.id,
      scheduleId: seed.schedule.id,
      packageId: seed.packageId || undefined,
      adultCount: 2,
      childCount: 0,
      contact: {
        fullName: 'Khách hàng nghiệm thu',
        phone: '0902000000',
        email,
      },
      passengers: [
        buildPassenger('Nguyễn Minh Khách', 'adult', email, '0902000000'),
        buildPassenger('Trần Thu Hà', 'adult', email, '0902000001'),
        buildPassenger('Bé An', 'child', email, '0902000002'),
      ],
      customerNote: note,
    },
  });
}

async function startPayment(token, orderCode) {
  return apiRequest('POST', `/customer/orders/${encodeURIComponent(orderCode)}/payment-init`, {
    token,
    body: {
      appBaseUrl: APP_BASE,
    },
  });
}

async function markPaymentPaid(order, token) {
  const payment = order?.payment;
  assert(payment?.providerInvoiceNumber, `Đơn ${order?.orderCode} chưa có provider invoice number.`);

  await apiRequest('POST', '/payments/sepay/webhook', {
    auth: false,
    headers: {
      'X-Secret-Key': WEBHOOK_SECRET,
    },
    body: {
      order_invoice_number: payment.providerInvoiceNumber,
      order_id: `sandbox-${order.orderCode}`,
      order_status: 'CAPTURED',
      amount: payment.amount,
    },
  });

  return apiRequest('GET', `/customer/orders/${encodeURIComponent(order.orderCode)}`, { token });
}

async function screenshot(page, name) {
  const filePath = path.join(ARTIFACTS_DIR, `${name}.png`);
  await page.screenshot({ path: filePath, fullPage: true });
  return filePath;
}

async function ensureNoMojibake(page, label) {
  const bodyText = await page.locator('body').innerText();
  assert(!detectMojibake(bodyText), `${label} vẫn còn lỗi vỡ tiếng Việt.`);
}

function attachDebug(page) {
  page.on('pageerror', (error) => {
    result.pageErrors.push({ url: page.url(), message: error.message });
  });

  page.on('console', (message) => {
    if (message.type() === 'error') {
      result.consoleErrors.push({ url: page.url(), message: message.text() });
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
}

async function visitAndCapture(page, route, marker, name) {
  await page.goto(`${APP_BASE}${route}`, { waitUntil: 'domcontentloaded' });
  if (marker) {
    await page.waitForFunction(
      ({ expected }) => document.body.innerText.includes(expected),
      { expected: marker },
      { timeout: 20000 },
    );
  }
  await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
  await ensureNoMojibake(page, route);
  const filePath = await screenshot(page, name);
  result.ui.push({ route, screenshot: filePath });
}

async function main() {
  const suffix = Date.now();
  const email = `customer-${suffix}@example.com`;
  const password = 'Customer@12345';
  const fullName = 'Khách hàng nghiệm thu';

  await registerCustomer(email, password, fullName);
  const auth = await loginCustomer(email, password);
  const token = auth.accessToken;

  result.account.user = {
    email,
    fullName,
    userId: auth?.user?.id || null,
  };

  const defaultPreferences = await apiRequest('GET', '/customer/account/preferences', { token });
  assert(defaultPreferences?.languageCode === 'vi', 'Default language preference phải là vi.');

  const updatedPreferences = await apiRequest('PUT', '/customer/account/preferences', {
    token,
    body: {
      languageCode: 'en',
      currencyCode: 'USD',
      themeMode: 'dark',
      emailNotificationsEnabled: true,
      smsNotificationsEnabled: true,
      pushNotificationsEnabled: false,
    },
  });
  assert(updatedPreferences?.languageCode === 'en', 'Không cập nhật được preferences cho customer.');
  result.account.preferences = updatedPreferences;

  const passenger = await apiRequest('POST', '/customer/account/passengers', {
    token,
    body: {
      fullName: 'Nguyễn Hành Khách',
      passengerType: 'adult',
      gender: 'male',
      email,
      phoneNumber: '0903000000',
      isDefault: true,
    },
  });
  assert(passenger?.id, 'Không tạo được saved passenger.');

  const updatedPassenger = await apiRequest('PUT', `/customer/account/passengers/${passenger.id}`, {
    token,
    body: {
      fullName: 'Nguyễn Hành Khách Chính',
      passengerType: 'adult',
      gender: 'male',
      email,
      phoneNumber: '0903000001',
      isDefault: true,
      notes: 'Khách ưu tiên cho checkout',
    },
  });
  result.account.passenger = updatedPassenger;

  const tourSeed = await getFirstTourSeed();
  result.orders.tourSeed = {
    tourId: tourSeed.tour.id,
    scheduleId: tourSeed.schedule.id,
    packageId: tourSeed.packageId,
    title: tourSeed.detail?.name || tourSeed.tour?.name || null,
  };

  const wishlistItem = await apiRequest('POST', '/customer/account/wishlist', {
    token,
    body: {
      productType: 'tour',
      targetId: tourSeed.tour.id,
      targetSlug: tourSeed.detail?.slug || null,
      title: tourSeed.detail?.name || tourSeed.tour?.name || 'Tour test customer',
      subtitle: tourSeed.schedule?.name || 'Lịch khởi hành sắp tới',
      locationText: tourSeed.detail?.destinationSummary || null,
      priceValue: tourSeed.tour?.fromPrice || null,
      currencyCode: tourSeed.tour?.currencyCode || 'VND',
      targetUrl: `/tour/${tourSeed.tour.id}`,
    },
  });
  assert(wishlistItem?.id, 'Không thêm được wishlist item.');
  result.account.wishlistItem = wishlistItem;

  const paidOrder = await createTourOrder(token, tourSeed, email, 'Đơn thanh toán thành công để nghiệm thu ticket');
  const paidOrderInitialized = await startPayment(token, paidOrder.orderCode);
  assert(
    paidOrderInitialized?.payment?.checkoutForm?.actionUrl?.includes('sepay'),
    'Payment init chưa trả checkout form của SePay.',
  );
  const paidOrderAfterWebhook = await markPaymentPaid(paidOrderInitialized, token);
  assert(Number(paidOrderAfterWebhook?.paymentStatus || 0) === 2, 'Đơn đã trả tiền nhưng paymentStatus chưa chuyển Paid.');
  assert(Number(paidOrderAfterWebhook?.ticketStatus || 0) === 2, 'Đơn đã trả tiền nhưng ticket chưa được phát hành.');
  const ticket = await apiRequest('GET', `/customer/orders/${encodeURIComponent(paidOrder.orderCode)}/ticket`, { token });
  assert(ticket?.ticketCode, 'Không lấy được ticket sau khi thanh toán.');
  result.orders.paidOrder = {
    orderCode: paidOrder.orderCode,
    paymentCode: paidOrderAfterWebhook?.payment?.paymentCode,
    providerInvoiceNumber: paidOrderAfterWebhook?.payment?.providerInvoiceNumber,
    ticketCode: ticket?.ticketCode,
  };

  const vatRequest = await apiRequest('POST', '/customer/account/vat-invoices', {
    token,
    body: {
      orderCode: paidOrder.orderCode,
      companyName: 'Công ty TNHH Nghiệm Thu 2TMNY',
      taxCode: '0312345678',
      companyAddress: '123 Nguyễn Huệ, Quận 1, TP.HCM',
      invoiceEmail: email,
      notes: 'Yêu cầu nghiệm thu VAT customer',
    },
  });
  assert(vatRequest?.requestCode, 'Không tạo được yêu cầu VAT cho đơn đã thanh toán.');
  result.orders.vatRequest = {
    orderCode: paidOrder.orderCode,
    requestCode: vatRequest.requestCode,
  };

  const refundOrder = await createTourOrder(token, tourSeed, email, 'Đơn dùng để kiểm tra luồng refund');
  const refundOrderInitialized = await startPayment(token, refundOrder.orderCode);
  const refundOrderPaid = await markPaymentPaid(refundOrderInitialized, token);
  const refundRequest = await apiRequest('POST', `/customer/orders/${encodeURIComponent(refundOrder.orderCode)}/refunds`, {
    token,
    body: {
      requestedAmount: refundOrderPaid?.payableAmount,
      reasonCode: 'CUSTOMER_REQUEST',
      reasonText: 'Khách đổi lịch cá nhân trong bài test customer commerce',
    },
  });
  assert(refundRequest?.refundCode, 'Không tạo được refund request.');
  result.orders.refundOrder = {
    orderCode: refundOrder.orderCode,
    refundCode: refundRequest.refundCode,
  };

  const cancelOrder = await createTourOrder(token, tourSeed, email, 'Đơn dùng để kiểm tra luồng hủy pending');
  const cancelled = await apiRequest('POST', `/customer/orders/${encodeURIComponent(cancelOrder.orderCode)}/cancel`, { token, body: {} });
  assert(Number(cancelled?.status || 0) === 5, 'Đơn pending chưa chuyển trạng thái hủy.');
  result.orders.cancelledOrder = {
    orderCode: cancelOrder.orderCode,
  };

  const pendingOrder = await createTourOrder(token, tourSeed, email, 'Đơn pending mở để kiểm tra giao diện cancel');
  result.orders.pendingOrder = {
    orderCode: pendingOrder.orderCode,
  };

  const orders = await apiRequest('GET', '/customer/orders?page=1&pageSize=20', { token });
  const payments = await apiRequest('GET', '/customer/account/payments', { token });
  const notifications = await apiRequest('GET', '/customer/account/notifications', { token });
  const vatInvoices = await apiRequest('GET', '/customer/account/vat-invoices', { token });
  const wishlist = await apiRequest('GET', '/customer/account/wishlist', { token });
  const passengers = await apiRequest('GET', '/customer/account/passengers', { token });
  const paymentMethods = await apiRequest('GET', '/customer/orders/payment-methods', { token });

  assert((orders?.items || []).length >= 4, 'Danh sách customer orders chưa đủ dữ liệu test.');
  assert(Array.isArray(payments) && payments.length >= 2, 'Payment history chưa ghi nhận đủ giao dịch.');
  assert((notifications?.items || []).length >= 4, 'Notification center chưa ghi nhận đủ sự kiện customer.');
  assert(Array.isArray(vatInvoices) && vatInvoices.length >= 1, 'Danh sách VAT invoice chưa có dữ liệu.');
  assert(Array.isArray(wishlist) && wishlist.length >= 1, 'Wishlist list chưa có dữ liệu.');
  assert(Array.isArray(passengers) && passengers.length >= 1, 'Saved passengers chưa có dữ liệu.');
  assert(Array.isArray(paymentMethods?.methods) && paymentMethods.methods.length >= 1, 'Chưa có payment method hỗ trợ cho checkout.');

  result.lists = {
    orders: orders.items.length,
    payments: payments.length,
    notifications: notifications.items.length,
    vatInvoices: vatInvoices.length,
    wishlist: wishlist.length,
    passengers: passengers.length,
    paymentMethods: paymentMethods.methods.length,
  };

  const browser = await chromium.launch({
    headless: true,
    executablePath: fs.existsSync(CHROME_PATH) ? CHROME_PATH : undefined,
  });

  const context = await browser.newContext({
    viewport: { width: 1440, height: 1024 },
  });
  const sessionPayload = {
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    sessionId: auth.sessionId,
    accessTokenExpiresAt: auth.expiresAt,
    refreshTokenExpiresAt: auth.refreshTokenExpiresAt,
    user: auth.user,
  };

  await context.addInitScript(({ keys, session }) => {
    localStorage.setItem(keys.accessToken, session.accessToken);
    localStorage.setItem(keys.refreshToken, session.refreshToken);
    localStorage.setItem(keys.sessionId, session.sessionId);
    localStorage.setItem(keys.accessTokenExpiresAt, session.accessTokenExpiresAt);
    localStorage.setItem(keys.refreshTokenExpiresAt, session.refreshTokenExpiresAt);
    localStorage.setItem(keys.user, JSON.stringify(session.user));
    localStorage.setItem(keys.memberships, JSON.stringify([]));
    localStorage.setItem(keys.permissions, JSON.stringify([]));
    localStorage.setItem(keys.remember, 'true');
  }, { keys: AUTH_KEYS, session: sessionPayload });

  const page = await context.newPage();
  attachDebug(page);

  await visitAndCapture(
    page,
    `/checkout?product=tour&tourId=${encodeURIComponent(tourSeed.tour.id)}&scheduleId=${encodeURIComponent(tourSeed.schedule.id)}${tourSeed.packageId ? `&packageId=${encodeURIComponent(tourSeed.packageId)}` : ''}&adult=2&child=0`,
    'Thông tin liên hệ',
    '01_checkout_tour',
  );
  await visitAndCapture(page, `/payment?orderCode=${encodeURIComponent(paidOrder.orderCode)}`, 'Thông tin đối soát', '02_payment_paid_order');
  await visitAndCapture(page, `/ticket/success?orderCode=${encodeURIComponent(paidOrder.orderCode)}`, 'Đặt dịch vụ thành công', '03_ticket_success');
  await visitAndCapture(page, '/my-account/profile', 'Thông tin cá nhân', '04_profile');
  await visitAndCapture(page, '/my-account/bookings', 'Đơn hàng của tôi', '05_bookings');
  await visitAndCapture(page, `/my-account/bookings/${encodeURIComponent(refundOrder.orderCode)}`, refundOrder.orderCode, '06_booking_detail_refund');
  await visitAndCapture(page, `/my-account/bookings/${encodeURIComponent(pendingOrder.orderCode)}/cancel`, 'Chọn lý do', '07_cancel_pending_order');
  await visitAndCapture(page, '/my-account/passengers', 'Hành khách đã lưu', '08_saved_passengers');
  await visitAndCapture(page, '/my-account/wishlist', 'Danh sách yêu thích', '09_wishlist');
  await visitAndCapture(page, '/my-account/notifications', 'Thông báo', '10_notifications');
  await visitAndCapture(page, '/my-account/payments', 'Thanh toán', '11_payments');
  await visitAndCapture(page, '/my-account/payment-history', 'Lịch sử thanh toán', '12_payment_history');
  await visitAndCapture(page, '/my-account/vat-invoice', 'Hóa đơn VAT', '13_vat_invoices');
  await visitAndCapture(page, '/my-account/settings', 'Cài đặt tài khoản', '14_settings');
  await visitAndCapture(page, '/my-account/security', 'Bảo mật tài khoản', '15_security');

  await browser.close();

  result.ok = true;
  result.finishedAt = new Date().toISOString();
}

main()
  .catch((error) => {
    result.ok = false;
    result.error = error.message;
    result.finishedAt = new Date().toISOString();
    console.error(error);
    process.exitCode = 1;
  })
  .finally(() => {
    fs.writeFileSync(
      path.join(OUT_DIR, 'customer-live-test-result.json'),
      JSON.stringify(result, null, 2),
      'utf8',
    );
  });
