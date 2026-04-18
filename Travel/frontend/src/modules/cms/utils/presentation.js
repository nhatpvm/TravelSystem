export function formatCmsDate(value, withTime = false) {
  if (!value) {
    return 'Chưa cập nhật';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Chưa cập nhật';
  }

  return new Intl.DateTimeFormat('vi-VN', withTime
    ? { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }
    : { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
}

export function slugifyCmsValue(value) {
  return String(value || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

export function normalizeCmsPostStatus(status) {
  if (typeof status === 'string' && status.trim()) {
    return status;
  }

  const numericStatus = Number(status);
  if (numericStatus === 2) return 'Scheduled';
  if (numericStatus === 3) return 'Published';
  if (numericStatus === 4) return 'Unpublished';
  if (numericStatus === 5) return 'Archived';
  return 'Draft';
}

export function getCmsStatusClass(status) {
  const normalized = normalizeCmsPostStatus(status);
  if (normalized === 'Published') return 'bg-green-100 text-green-600';
  if (normalized === 'Scheduled') return 'bg-blue-100 text-blue-600';
  if (normalized === 'Archived' || normalized === 'Unpublished') return 'bg-slate-100 text-slate-600';
  return 'bg-amber-100 text-amber-600';
}

export function getCmsSeoScore(audit) {
  if (typeof audit?.score === 'number') {
    return audit.score;
  }

  if (typeof audit?.summary?.score === 'number') {
    return audit.summary.score;
  }

  return null;
}

export function getCmsSeoIssueLevel(issue) {
  return issue?.level || issue?.severity || 'info';
}
