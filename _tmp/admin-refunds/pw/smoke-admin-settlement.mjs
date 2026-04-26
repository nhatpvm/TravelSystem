import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';

const baseUrl = 'http://127.0.0.1:4173';
const apiBaseUrl = 'http://127.0.0.1:5183/api/v1';
const artifactDir = 'D:/FPT/TicketBooking.V3/_tmp/admin-settlement/ui';
const settlementDate = '2026-04-24';

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
  const page = await browser.newPage({ viewport: { width: 1440, height: 1400 } });

  page.on('dialog', async (dialog) => {
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

  await page.goto(`${baseUrl}/admin/settlement`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(artifactDir, 'admin-settlement-before.png'), fullPage: true });

  await page.getByRole('button', { name: 'Ngay' }).click();
  await page.locator('input[type="date"]').fill(settlementDate);
  await page.getByPlaceholder('Ghi chu doi soat/payout').fill('UI smoke daily settlement batch');
  await page.getByRole('button', { name: 'Tao batch' }).click();
  await page.getByText('Da tao hoac cap nhat batch settlement theo ky da chon.').waitFor({ timeout: 20000 });

  await page.getByText('24/04/2026').first().click();
  await page.getByText('Ngay thanh toan').waitFor({ timeout: 10000 });
  await page.screenshot({ path: path.join(artifactDir, 'admin-settlement-after.png'), fullPage: true });

  const summary = {
    url: page.url(),
    periodButtons: await page.locator('button').evaluateAll((buttons) =>
      buttons.map((button) => button.textContent?.trim()).filter(Boolean),
    ),
    detailText: await page.locator('.border-t.border-slate-100.p-5.bg-slate-50').first().innerText(),
  };

  await fs.writeFile(
    'D:/FPT/TicketBooking.V3/_tmp/admin-settlement/admin-settlement-ui.json',
    JSON.stringify(summary, null, 2),
    'utf8',
  );

  await browser.close();
}

main().catch(async (error) => {
  await fs.mkdir('D:/FPT/TicketBooking.V3/_tmp/admin-settlement', { recursive: true });
  await fs.writeFile(
    'D:/FPT/TicketBooking.V3/_tmp/admin-settlement/admin-settlement-ui.json',
    JSON.stringify({ error: error.message, stack: error.stack }, null, 2),
    'utf8',
  );
  process.exitCode = 1;
});
