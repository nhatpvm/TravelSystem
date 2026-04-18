import { useEffect, useMemo, useState } from 'react';
import { listAdminTenants } from '../../../../services/adminIdentity';

const STORAGE_KEY = 'admin_master_data_tenant_id';

function readStoredTenantId() {
  return window.localStorage.getItem(STORAGE_KEY) || '';
}

export default function useAdminMasterDataScope() {
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
  }, []);

  useEffect(() => {
    if (selectedTenantId) {
      window.localStorage.setItem(STORAGE_KEY, selectedTenantId);
    }
  }, [selectedTenantId]);

  const selectedTenant = useMemo(
    () => tenants.find((item) => item.id === selectedTenantId) || null,
    [selectedTenantId, tenants],
  );

  return {
    tenantId: selectedTenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeLoading,
    scopeError,
    setScopeError,
  };
}
