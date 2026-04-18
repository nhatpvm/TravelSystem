const CUSTOMER_PREFERENCES_KEY = 'customer_account_preferences';
const CUSTOMER_PREFERENCES_CHANGED_EVENT = 'customer:preferences:changed';

const DEFAULT_PREFERENCES = {
  languageCode: 'vi',
  currencyCode: 'VND',
  themeMode: 'light',
  emailNotificationsEnabled: true,
  smsNotificationsEnabled: false,
  pushNotificationsEnabled: true,
};

function hasWindow() {
  return typeof window !== 'undefined' && typeof document !== 'undefined';
}

export function normalizeCustomerPreferences(value = {}) {
  const next = {
    ...DEFAULT_PREFERENCES,
    ...(value || {}),
  };

  return {
    languageCode: normalizeLanguageCode(next.languageCode),
    currencyCode: normalizeCurrencyCode(next.currencyCode),
    themeMode: normalizeThemeMode(next.themeMode),
    emailNotificationsEnabled: next.emailNotificationsEnabled !== false,
    smsNotificationsEnabled: next.smsNotificationsEnabled === true,
    pushNotificationsEnabled: next.pushNotificationsEnabled !== false,
  };
}

export function getStoredCustomerPreferences() {
  if (!hasWindow()) {
    return { ...DEFAULT_PREFERENCES };
  }

  const raw = window.localStorage.getItem(CUSTOMER_PREFERENCES_KEY);
  if (!raw) {
    return { ...DEFAULT_PREFERENCES };
  }

  try {
    return normalizeCustomerPreferences(JSON.parse(raw));
  } catch {
    return { ...DEFAULT_PREFERENCES };
  }
}

export function saveCustomerPreferences(value) {
  const next = normalizeCustomerPreferences(value);

  if (hasWindow()) {
    window.localStorage.setItem(CUSTOMER_PREFERENCES_KEY, JSON.stringify(next));
  }

  applyCustomerPreferences(next);
  notifyCustomerPreferencesChanged();
  return next;
}

export function applyCustomerPreferences(value) {
  const next = normalizeCustomerPreferences(value);

  if (!hasWindow()) {
    return next;
  }

  document.documentElement.lang = next.languageCode;
  document.documentElement.dataset.themeMode = next.themeMode;
  document.documentElement.style.colorScheme = next.themeMode === 'dark' ? 'dark' : 'light';
  if (document.body) {
    document.body.dataset.themeMode = next.themeMode;
  }

  return next;
}

export function applyStoredCustomerPreferences() {
  return applyCustomerPreferences(getStoredCustomerPreferences());
}

export function getCustomerLocale() {
  const languageCode = getStoredCustomerPreferences().languageCode;

  switch (String(languageCode || '').trim().toLowerCase()) {
    case 'en':
      return 'en-US';
    case 'zh-cn':
      return 'zh-CN';
    case 'ko':
      return 'ko-KR';
    case 'th':
      return 'th-TH';
    case 'vi':
    default:
      return 'vi-VN';
  }
}

export function getCustomerCurrency(fallback = 'VND') {
  return getStoredCustomerPreferences().currencyCode || fallback || 'VND';
}

export function subscribeToCustomerPreferenceChanges(callback) {
  if (!hasWindow()) {
    return () => {};
  }

  const handler = () => {
    callback(getStoredCustomerPreferences());
  };

  window.addEventListener(CUSTOMER_PREFERENCES_CHANGED_EVENT, handler);
  return () => {
    window.removeEventListener(CUSTOMER_PREFERENCES_CHANGED_EVENT, handler);
  };
}

function notifyCustomerPreferencesChanged() {
  if (!hasWindow()) {
    return;
  }

  window.dispatchEvent(new CustomEvent(CUSTOMER_PREFERENCES_CHANGED_EVENT));
}

function normalizeLanguageCode(value) {
  switch (String(value || '').trim().toLowerCase()) {
    case 'en':
      return 'en';
    case 'zh-cn':
      return 'zh-CN';
    case 'ko':
      return 'ko';
    case 'th':
      return 'th';
    case 'vi':
    default:
      return 'vi';
  }
}

function normalizeCurrencyCode(value) {
  const code = String(value || '').trim().toUpperCase();
  return code || DEFAULT_PREFERENCES.currencyCode;
}

function normalizeThemeMode(value) {
  return String(value || '').trim().toLowerCase() === 'dark' ? 'dark' : 'light';
}
