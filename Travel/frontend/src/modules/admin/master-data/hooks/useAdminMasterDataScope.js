import { useEffect, useMemo, useState } from 'react';
import { listAdminTenants } from '../../../../services/adminIdentity';
import { useAuthSession } from '../../../auth/hooks/useAuthSession';

const STORAGE_KEY = 'admin_master_data_tenant_id';

function readStoredTenantId() {
  return window.localStorage.getItem(STORAGE_KEY) || '';
}

export default function useAdminMasterDataScope({ scope = 'admin' } = {}) {
  const session = useAuthSession();
  const isTenantScope = scope === 'tenant';
  const [tenants, setTenants] = useState([]);
  const [selectedTenantId, setSelectedTenantId] = useState(() => readStoredTenantId());
  const [scopeLoading, setScopeLoading] = useState(true);
  const [scopeError, setScopeError] = useState('');

  useEffect(() => {
    if (isTenantScope) {
      setScopeLoading(false);
      setScopeError('');
      return undefined;
    }

    let active = true;

    async function loadTenants() {
      setScopeLoading(true);
      setScopeError('');

      try {
        const response = await listAdminTenants({ page: 1, pageSize: 100 });
        if (!active) {
          return;
        }

        const items = response.items || [];
        setTenants(items);

        if (!items.length) {
          setSelectedTenantId('');
          return;
        }

        const nextTenantId = items.some((item) => item.id === readStoredTenantId())
          ? readStoredTenantId()
          : items[0].id;

        setSelectedTenantId(nextTenantId);
      } catch (error) {
        if (!active) {
          return;
        }

        setScopeError(error.message || 'Không thể tải danh sách tenant.');
        setTenants([]);
        setSelectedTenantId('');
      } finally {
        if (active) {
          setScopeLoading(false);
        }
      }
    }

    loadTenants();

    return () => {
      active = false;
    };
  }, [isTenantScope]);

  useEffect(() => {
    if (!isTenantScope && selectedTenantId) {
      window.localStorage.setItem(STORAGE_KEY, selectedTenantId);
    }
  }, [isTenantScope, selectedTenantId]);

  const tenantScopedTenantId = session.currentTenantId || session.currentTenant?.tenantId || '';
  const tenantScopedTenant = useMemo(() => {
    if (!isTenantScope) {
      return null;
    }

    const current = session.currentTenant || session.memberships?.find((item) => item.tenantId === tenantScopedTenantId) || null;
    return current
      ? {
          ...current,
          id: current.id || current.tenantId,
          name: current.name || current.tenantName || 'Tenant hiện tại',
        }
      : null;
  }, [isTenantScope, session.currentTenant, session.memberships, tenantScopedTenantId]);

  const selectedTenant = useMemo(
    () => tenants.find((item) => item.id === selectedTenantId) || null,
    [selectedTenantId, tenants],
  );

  return {
    tenantId: isTenantScope ? tenantScopedTenantId : selectedTenantId,
    tenants: isTenantScope ? (tenantScopedTenant ? [tenantScopedTenant] : []) : tenants,
    selectedTenantId: isTenantScope ? tenantScopedTenantId : selectedTenantId,
    setSelectedTenantId: isTenantScope ? () => {} : setSelectedTenantId,
    selectedTenant: isTenantScope ? tenantScopedTenant : selectedTenant,
    scopeLoading,
    scopeError: isTenantScope && !tenantScopedTenantId ? 'Không xác định được tenant hiện tại.' : scopeError,
    setScopeError,
    isTenantScope,
    showTenantSelector: !isTenantScope,
    scopeHint: tenantScopedTenant ? `Đang quản lý dữ liệu nền cho tenant: ${tenantScopedTenant.name}` : '',
  };
}
