import fs from 'node:fs/promises';
import path from 'node:path';
import playwright from '../admin-refunds/pw/node_modules/playwright/index.js';

const { chromium } = playwright;

const root = 'D:/FPT/TicketBooking.V3';
const frontendBaseUrl = 'http://127.0.0.1:4173';
const uploadFilePath = path.join(root, '_tmp', 'admin-upload-smoke.png');
const loginPath = path.join(root, '_tmp', 'admin-upload-login.json');
const outputDir = path.join(root, '_tmp', 'admin-upload', 'ui');

const HOTEL_TENANT_ID = '32110ffc-3d5b-409b-8bbb-8fb3286d9cc7';
const TOUR_TENANT_ID = 'dfb80807-d1db-43b0-a62f-9b744f1dde4f';

async function ensureDir(dir) {
  await fs.mkdir(dir, { recursive: true });
}

async function setAdminSession(page, login) {
  await page.addInitScript((session) => {
    window.localStorage.setItem('auth_token', session.accessToken);
    window.localStorage.setItem('auth_refresh_token', session.refreshToken);
    window.localStorage.setItem('auth_session_id', session.sessionId);
    window.localStorage.setItem('auth_expires_at', session.expiresAt);
    window.localStorage.setItem('auth_refresh_expires_at', session.refreshTokenExpiresAt);
    window.localStorage.setItem('auth_user', JSON.stringify(session.user));
    window.localStorage.setItem('auth_memberships', JSON.stringify([]));
    window.localStorage.setItem('auth_permissions', JSON.stringify([]));
    window.localStorage.setItem('auth_remember', 'true');
    window.localStorage.setItem('admin_hotel_tenant_id', session.hotelTenantId);
    window.localStorage.setItem('admin_tours_tenant_id', session.tourTenantId);
  }, {
    accessToken: login.accessToken,
    refreshToken: login.refreshToken,
    sessionId: login.sessionId,
    expiresAt: login.expiresAt,
    refreshTokenExpiresAt: login.refreshTokenExpiresAt,
    user: login.user,
    hotelTenantId: HOTEL_TENANT_ID,
    tourTenantId: TOUR_TENANT_ID,
  });
}

async function saveScreenshot(page, name) {
  const screenshotPath = path.join(outputDir, name);
  await page.screenshot({ path: screenshotPath, fullPage: true });
  return screenshotPath;
}

async function collectBannerTexts(page) {
  const texts = await page.locator('div.rounded-2xl').allInnerTexts();
  return texts
    .map((item) => item.trim())
    .filter(Boolean)
    .slice(0, 6);
}

async function run() {
  await ensureDir(outputDir);
  const login = JSON.parse(await fs.readFile(loginPath, 'utf8'));

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1100 } });
  await setAdminSession(page, login);

  const results = {};

  await page.goto(`${frontendBaseUrl}/admin/hotels/images`, { waitUntil: 'networkidle' });
  await page.locator('input[type="file"]').setInputFiles(uploadFilePath);
  await page.getByPlaceholder('Tiêu đề ảnh').fill('Smoke hotel image');
  await page.getByPlaceholder('Alt text').fill('Smoke hotel image alt');
  await page.locator('button[type="submit"]').click();
  await page.getByText('Đã tạo ảnh khách sạn mới.').waitFor({ timeout: 15000 });
  results.hotelImage = {
    imageUrl: await page.locator('input[placeholder="URL ảnh"]').inputValue(),
    screenshot: await saveScreenshot(page, 'admin-hotel-images.png'),
  };

  await page.goto(`${frontendBaseUrl}/admin/hotels/room-type-images`, { waitUntil: 'networkidle' });
  await page.locator('input[type="file"]').setInputFiles(uploadFilePath);
  await page.getByPlaceholder('Tiêu đề ảnh').fill('Smoke room type image');
  await page.getByPlaceholder('Alt text').fill('Smoke room type image alt');
  await page.locator('button[type="submit"]').click();
  await page.getByText('Đã tạo ảnh hạng phòng mới.').waitFor({ timeout: 15000 });
  results.roomTypeImage = {
    imageUrl: await page.locator('input[placeholder="URL ảnh"]').inputValue(),
    screenshot: await saveScreenshot(page, 'admin-room-type-images.png'),
  };

  await page.goto(`${frontendBaseUrl}/admin/tours`, { waitUntil: 'networkidle' });
  await page.locator('select').first().selectOption(TOUR_TENANT_ID);
  await page.locator('button[type="submit"]:not([disabled])').waitFor({ timeout: 15000 });
  await page.locator('input[type="file"]').setInputFiles(uploadFilePath);
  await page.waitForTimeout(1500);
  const tourImageUrl = await page.locator('input[placeholder="URL ảnh bìa"]').inputValue();
  results.tourCover = {
    tenantBanner: await page.locator('div.rounded-2xl').nth(0).innerText(),
    imageUrl: tourImageUrl,
    banners: await collectBannerTexts(page),
    screenshot: await saveScreenshot(page, 'admin-tours.png'),
  };

  await page.goto(`${frontendBaseUrl}/admin/users`, { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: /Thêm người dùng/i }).click();
  await page.locator('input[type="file"]').setInputFiles(uploadFilePath);
  await page.waitForTimeout(1000);
  results.userAvatar = {
    imageUrl: await page.locator('input[placeholder="URL ảnh đại diện"]').first().inputValue(),
    banners: await collectBannerTexts(page),
    screenshot: await saveScreenshot(page, 'admin-users.png'),
  };

  await browser.close();
  const resultPath = path.join(root, '_tmp', 'admin-upload', 'admin-upload-ui.json');
  await fs.writeFile(resultPath, JSON.stringify(results, null, 2));
}

run().catch(async (error) => {
  const errorPath = path.join(root, '_tmp', 'admin-upload', 'admin-upload-ui-error.txt');
  await fs.writeFile(errorPath, `${error?.stack || error}`);
  console.error(error);
  process.exit(1);
});
