const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

(async () => {
  const uiBaseUrl = 'http://127.0.0.1:4174';
  const apiBaseUrl = 'http://127.0.0.1:5183/api/v1';
  const artifactsDir = 'D:/FPT/TicketBooking.V3/_tmp/wa/ui';
  fs.mkdirSync(artifactsDir, { recursive: true });

  const loginResponse = await fetch(`${apiBaseUrl}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userNameOrEmail: 'customer@ticketbooking.local', password: 'Customer@12345', rememberMe: true }),
  });
  const login = await loginResponse.json();
  if (!loginResponse.ok || !login.accessToken) {
    throw new Error(`API login failed: ${JSON.stringify(login)}`);
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
  await context.addInitScript((session) => {
    window.localStorage.setItem('auth_token', session.accessToken);
    window.localStorage.setItem('auth_refresh_token', session.refreshToken);
    window.localStorage.setItem('auth_session_id', session.sessionId);
    window.localStorage.setItem('auth_expires_at', session.expiresAt);
    window.localStorage.setItem('auth_refresh_expires_at', session.refreshTokenExpiresAt);
    window.localStorage.setItem('auth_user', JSON.stringify(session.user));
    window.localStorage.setItem('auth_memberships', JSON.stringify([]));
    window.localStorage.setItem('auth_permissions', JSON.stringify([]));
    window.localStorage.setItem('auth_remember', 'true');
  }, login);

  const page = await context.newPage();

  try {
    await page.goto(`${uiBaseUrl}/my-account/profile`, { waitUntil: 'networkidle' });
    await page.locator('text=Khach san demo WA').first().waitFor({ timeout: 15000 });

    const profileChecks = {
      hasDraftCard: await page.locator('text=Khach san demo WA').count(),
      hasRecentSearchCard: await page.locator('text=SGN -> HAN').count(),
      hasRecentViewCard: await page.locator('text=Admin Hotel').count(),
    };

    await page.screenshot({ path: path.join(artifactsDir, 'profile-wa.png'), fullPage: true });

    await page.goto(`${uiBaseUrl}/checkout?product=hotel&hotelId=demo`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);

    const checkoutChecks = {
      restoredBanner: await page.locator('text=Da khoi phuc checkout dang do gan nhat.').count(),
      contactName: await page.locator('input').nth(0).inputValue(),
      contactPhone: await page.locator('input').nth(1).inputValue(),
      contactEmail: await page.locator('input').nth(2).inputValue(),
      passengerName: await page.locator('input').nth(3).inputValue(),
      passengerIdNumber: await page.locator('input').nth(4).inputValue(),
    };

    await page.screenshot({ path: path.join(artifactsDir, 'checkout-resume.png'), fullPage: true });

    const result = {
      profileChecks,
      checkoutChecks,
      finalUrl: page.url(),
    };

    fs.writeFileSync('D:/FPT/TicketBooking.V3/_tmp/wa/wa-ui-smoke.json', JSON.stringify(result, null, 2));
    console.log(JSON.stringify(result, null, 2));
  } finally {
    await context.close();
    await browser.close();
  }
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
