import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';

const baseUrl = 'http://127.0.0.1:4173';
const apiBaseUrl = 'http://127.0.0.1:5183/api/v1';
const artifactDir = 'D:/FPT/TicketBooking.V3/_tmp/admin-refunds/ui';
const targetRefundCode = 'RFD-202604161751235FD104';

async function login() {
  const response = await fetch(`${apiBaseUrl}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userNameOrEmail: 'admin@ticketbooking.local',
      password: 'Admin@12345',
    }),
  });

  if (!response.ok) {
    throw new Error(`Login failed with ${response.status}`);
  }

  return response.json();
}

async function main() {
  await fs.mkdir(artifactDir, { recursive: true });
  const session = await login();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1200 } });
  const steps = [];

  page.on('dialog', async (dialog) => {
    steps.push({ step: 'dialog', message: dialog.message() });
    await dialog.accept();
  });

  await page.goto(baseUrl, { waitUntil: 'networkidle' });
  await page.evaluate((data) => {
    localStorage.setItem('auth_token', data.accessToken);
    localStorage.setItem('auth_refresh_token', data.refreshToken);
    localStorage.setItem('auth_session_id', data.sessionId);
    localStorage.setItem('auth_expires_at', data.expiresAt);
    localStorage.setItem('auth_refresh_expires_at', data.refreshTokenExpiresAt);
    localStorage.setItem('auth_user', JSON.stringify(data.user));
    localStorage.setItem('auth_memberships', JSON.stringify([]));
    localStorage.setItem('auth_permissions', JSON.stringify([]));
    localStorage.setItem('auth_remember', 'true');
  }, session);

  await page.goto(`${baseUrl}/admin/refunds?q=${encodeURIComponent(targetRefundCode)}`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(artifactDir, 'admin-refunds-before.png'), fullPage: true });

  const approveButton = page.getByRole('button', { name: 'Duyet voi so tien nhap tay' });
  if (await approveButton.count()) {
    await page.getByPlaceholder('Nhap so tien duyet').fill('6000000');
    await page.getByPlaceholder('Nhap ghi chu noi bo...').fill('UI smoke approve partial refund.');
    await approveButton.click();
    await page.waitForTimeout(1500);
    await page.reload({ waitUntil: 'networkidle' });
    steps.push({ step: 'approve', status: 'ok' });
  } else {
    steps.push({ step: 'approve', status: 'skip' });
  }

  const completeButton = page.getByRole('button', { name: 'Xac nhan hoan thu cong' });
  if (await completeButton.count()) {
    await page.getByPlaceholder('Nhap so tien hoan thuc te').fill('5900000');
    await page.getByPlaceholder('VD: MB-REF-20260420-001').fill('MB-REF-20260420-UI01');
    await page.getByPlaceholder('Nhap ghi chu noi bo...').fill('UI smoke complete manual refund.');
    await completeButton.click();
    await page.waitForTimeout(1500);
    await page.reload({ waitUntil: 'networkidle' });
    steps.push({ step: 'complete', status: 'ok' });
  } else {
    steps.push({ step: 'complete', status: 'skip' });
  }

  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(artifactDir, 'admin-refunds-after.png'), fullPage: true });

  const detailValues = await page.locator('.sticky.top-6').innerText();
  if (!detailValues.includes(targetRefundCode)) {
    throw new Error(`Refund detail panel missing ${targetRefundCode}`);
  }

  const summary = {
    url: page.url(),
    steps,
    detailValues,
  };

  await fs.writeFile(
    'D:/FPT/TicketBooking.V3/_tmp/admin-refunds/admin-refunds-ui.json',
    JSON.stringify(summary, null, 2),
    'utf8',
  );

  await browser.close();
}

main().catch(async (error) => {
  await fs.writeFile(
    'D:/FPT/TicketBooking.V3/_tmp/admin-refunds/admin-refunds-ui.json',
    JSON.stringify({ error: error.message, stack: error.stack }, null, 2),
    'utf8',
  );
  process.exitCode = 1;
});
