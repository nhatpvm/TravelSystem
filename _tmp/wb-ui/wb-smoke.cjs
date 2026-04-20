const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const FRONTEND_BASE = 'http://127.0.0.1:4174';
const API_BASE = 'http://127.0.0.1:5183/api/v1';
const ORDER_CODE = 'ORD-2026042012595167BB39';
const OUTPUT_DIR = 'D:/FPT/TicketBooking.V3/_tmp/wb/ui';

async function login() {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      usernameOrEmail: 'customer@ticketbooking.local',
      password: 'Customer@12345',
    }),
  });

  if (!response.ok) {
    throw new Error(`Login failed with ${response.status}`);
  }

  const data = await response.json();
  if (data.requiresTwoFactor) {
    throw new Error('Smoke account unexpectedly requires 2FA.');
  }

  return data;
}

async function seedAuth(page, session) {
  await page.addInitScript((payload) => {
    window.localStorage.setItem('auth_token', payload.accessToken);
    window.localStorage.setItem('auth_refresh_token', payload.refreshToken);
    window.localStorage.setItem('auth_session_id', payload.sessionId);
    window.localStorage.setItem('auth_expires_at', payload.expiresAt);
    window.localStorage.setItem('auth_refresh_expires_at', payload.refreshTokenExpiresAt);
    window.localStorage.setItem('auth_user', JSON.stringify(payload.user));
    window.localStorage.setItem('auth_memberships', JSON.stringify([]));
    window.localStorage.setItem('auth_permissions', JSON.stringify([]));
    window.localStorage.setItem('auth_remember', 'true');
  }, session);
}

async function ensureDir() {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

async function run() {
  await ensureDir();
  const session = await login();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1600 } });
  await seedAuth(page, session);

  const result = {};

  await page.goto(`${FRONTEND_BASE}/my-account/profile`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Gợi ý cho bạn');
  result.profile = {
    suggestionSection: await page.locator('text=Gợi ý cho bạn').count(),
    suggestionSignals:
      await page.locator('text=Bạn đã xem lại dịch vụ này nhiều lần gần đây.').count()
      + await page.locator('text=Mở lại bộ lọc bạn đã dùng gần đây để tiếp tục so sánh.').count(),
  };
  await page.screenshot({ path: path.join(OUTPUT_DIR, 'profile-wb.png'), fullPage: true });

  await page.goto(`${FRONTEND_BASE}/my-account/bookings/${ORDER_CODE}`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Tiến trình đơn hàng');
  result.bookingDetail = {
    timelineCards: await page.locator('text=Mốc hiện tại').count() + await page.locator('div.rounded-3xl.border.p-5').count(),
    hasRefundStatus: await page.locator('text=Refund hiện tại').count(),
  };
  await page.screenshot({ path: path.join(OUTPUT_DIR, 'booking-detail-wb.png'), fullPage: true });

  await page.goto(`${FRONTEND_BASE}/my-account/bookings/${ORDER_CODE}/cancel`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Lý do');
  await page.getByRole('button', { name: 'Thay đổi lịch trình cá nhân' }).click();
  await page.getByRole('button', { name: 'Tiếp theo' }).click();
  await page.waitForSelector('text=Ước tính hoàn tiền');
  result.cancelRefund = {
    hasEstimateBlock: await page.locator('text=Ước tính hoàn tiền').count(),
    hasSettlementImpact: await page.locator('text=Rule minh bạch').count(),
    requestedAmountValue: await page.locator('input[type=\"number\"]').inputValue(),
  };
  await page.screenshot({ path: path.join(OUTPUT_DIR, 'cancel-refund-wb.png'), fullPage: true });

  await page.goto(`${FRONTEND_BASE}/ticket/success?orderCode=${ORDER_CODE}`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Mã vé điện tử');
  result.ticket = {
    hasRefundChip: await page.locator('text=Refund').count(),
    hasPaymentChip: await page.locator('text=Payment').count(),
    copyButtons: await page.locator('button').filter({ has: page.locator('svg') }).count(),
  };
  await page.screenshot({ path: path.join(OUTPUT_DIR, 'ticket-wb.png'), fullPage: true });

  await page.goto(`${FRONTEND_BASE}/my-account/notifications`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Thông báo');
  result.notifications = {
    cards: await page.locator('button.group.relative.w-full').count(),
  };

  fs.writeFileSync(
    'D:/FPT/TicketBooking.V3/_tmp/wb/wb-ui-smoke.json',
    JSON.stringify(result, null, 2),
    'utf8',
  );

  await browser.close();
}

run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
