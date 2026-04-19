const { chromium } = require('playwright');
(async () => {
  const uiBaseUrl = 'http://127.0.0.1:4174';
  const apiBaseUrl = 'http://127.0.0.1:5183/api/v1';
  const loginResponse = await fetch(`${apiBaseUrl}/auth/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userNameOrEmail: 'customer@ticketbooking.local', password: 'Customer@12345', rememberMe: true }),
  });
  const login = await loginResponse.json();
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  await context.addInitScript((session) => {
    localStorage.setItem('auth_token', session.accessToken);
    localStorage.setItem('auth_refresh_token', session.refreshToken);
    localStorage.setItem('auth_session_id', session.sessionId);
    localStorage.setItem('auth_expires_at', session.expiresAt);
    localStorage.setItem('auth_refresh_expires_at', session.refreshTokenExpiresAt);
    localStorage.setItem('auth_user', JSON.stringify(session.user));
    localStorage.setItem('auth_memberships', JSON.stringify([]));
    localStorage.setItem('auth_permissions', JSON.stringify([]));
    localStorage.setItem('auth_remember', 'true');
  }, login);
  const page = await context.newPage();
  await page.goto(`${uiBaseUrl}/checkout?product=hotel&hotelId=demo`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  const values = await page.locator('input').evaluateAll((els) => els.slice(0, 8).map((el) => ({ placeholder: el.getAttribute('placeholder'), value: el.value })));
  const pageText = await page.locator('body').innerText();
  console.log(JSON.stringify({ values, hasRestoreAscii: pageText.includes('Da khoi phuc'), hasRestoreVi: pageText.includes('khôi ph?c'), snippet: pageText.slice(0, 500) }, null, 2));
  await browser.close();
})();
