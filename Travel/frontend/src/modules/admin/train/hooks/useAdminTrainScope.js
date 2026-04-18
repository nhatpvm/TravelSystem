import { useEffect, useMemo, useState } from 'react';
import { listAdminTenants } from '../../../../services/adminIdentity';
import { getCurrentTenantId } from '../../../../services/interceptor';

const STORAGE_KEY = 'admin_train_tenant_id';

function readStoredTenantId() {
  return window.localStorage.getItem(STORAGE_KEY) || '';
}

function resolveInitialTenantId(items) {
  const currentTenantId = getCurrentTenantId();
  if (currentTenantId && items.some((item) => item.id === currentTenantId)) {
    return currentTenantId;
  }

  const storedTenantId = readStoredTenantId();
  if (storedTenantId && items.some((item) => item.id === storedTenantId)) {
    return storedTenantId;
  }

  return items[0]?.id || '';
}

export default function useAdminTrainScope() {
  const [tenants, setTenants] = useState([]);
  const [selectedTenantId, setSelectedTenantId] = useState(() => readStoredTenantId());
  const [scopeLoading, setScopeLoading] = useState(true);
  const [scopeError, setScopeError] = useState('');

  useEffect(() => {
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

        setSelectedTenantId(resolveInitialTenantId(items));
      } catch (error) {
        if (!active) {
          return;
        }

        setScopeError(error.message || 'Không thể tải danh sách tenant cho module tàu.');
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
  }, []);

  useEffect(() => {
    if (selectedTenantId) {
      window.localStorage.setItem(STORAGE_KEY, selectedTenantId);
    }
  }, [selectedTenantId]);

  const selectedTenant = useMemo(
    () => tenants.find((item) => item.id === selectedTenantId) || null,
    [tenants, selectedTenantId],
  );

  return {
    tenantId: selectedTenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeLoading,
    scopeError,
  };
}
