import { useEffect, useMemo, useState } from 'react';
import { listAdminTenants } from '../../../../services/adminIdentity';
import { getCurrentTenantId } from '../../../../services/interceptor';

const STORAGE_KEY = 'admin_flight_tenant_id';
const FLIGHT_TENANT_TYPE = 'Flight';

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

export default function useAdminFlightScope() {
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
        const response = await listAdminTenants({ page: 1, pageSize: 100, type: 3 });
        if (!active) {
          return;
        }

        const items = (response.items || []).filter((item) => item?.type === FLIGHT_TENANT_TYPE);
        setTenants(items);

        if (!items.length) {
          setSelectedTenantId('');
          setScopeError('Chưa có tenant hàng không nào để quản lý.');
          return;
        }

        setSelectedTenantId(resolveInitialTenantId(items));
      } catch (error) {
        if (!active) {
          return;
        }

        setScopeError(error.message || 'Không thể tải danh sách tenant cho module hàng không.');
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
