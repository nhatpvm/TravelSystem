import { useEffect, useMemo, useState } from 'react';
import { listAdminTenants } from '../../../services/adminIdentity';
import {
  getCmsOptions,
  getCmsSiteSettings,
  listCmsMedia,
  listCmsPosts,
  listCmsRedirects,
} from '../../../services/cmsService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import useLatestRef from '../../../shared/hooks/useLatestRef';

export default function useCmsWorkspaceData(mode = 'admin') {
  const session = useAuthSession();
  const isAdmin = mode === 'admin';
  const [tenants, setTenants] = useState([]);
  const [selectedTenantId, setSelectedTenantId] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [options, setOptions] = useState({
    categories: [],
    tags: [],
    mediaAssets: [],
    siteSettings: null,
    mediaTypes: [],
    redirectStatusCodes: [],
  });
  const [posts, setPosts] = useState([]);
  const [mediaAssets, setMediaAssets] = useState([]);
  const [redirects, setRedirects] = useState([]);

  const tenantId = isAdmin ? selectedTenantId : session.currentTenantId;
  const selectedTenant = useMemo(
    () => tenants.find((item) => item.id === selectedTenantId) || null,
    [selectedTenantId, tenants],
  );

  const loadWorkspaceRef = useLatestRef(loadWorkspace);

  useEffect(() => {
    if (!isAdmin) {
      return undefined;
    }

    let mounted = true;

    async function loadTenants() {
      try {
        const response = await listAdminTenants({ page: 1, pageSize: 100 });
        if (!mounted) {
          return;
        }

        setTenants(response.items || []);
        if (!selectedTenantId && response.items?.length) {
          setSelectedTenantId(response.items[0].id);
        }
      } catch (err) {
        if (mounted) {
          setError(err.message || 'Không thể tải danh sách tenant.');
        }
      }
    }

    loadTenants();

    return () => {
      mounted = false;
    };
  }, [isAdmin, selectedTenantId]);

  useEffect(() => {
    if (!tenantId) {
      if (!isAdmin) {
        setError('Chưa xác định tenant hiện tại để quản lý CMS.');
      }
      return;
    }

    loadWorkspaceRef.current();
  }, [isAdmin, loadWorkspaceRef, tenantId]);

  async function loadWorkspace() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, postsResponse, mediaResponse, redirectsResponse, siteSettingsResponse] = await Promise.all([
        getCmsOptions(tenantId),
        listCmsPosts({ page: 1, pageSize: 100 }, tenantId),
        listCmsMedia({ page: 1, pageSize: 100 }, tenantId),
        listCmsRedirects({ page: 1, pageSize: 100 }, tenantId),
        getCmsSiteSettings(tenantId).catch(() => null),
      ]);

      setOptions({
        categories: optionsResponse.categories || [],
        tags: optionsResponse.tags || [],
        mediaAssets: optionsResponse.mediaAssets || [],
        siteSettings: siteSettingsResponse || optionsResponse.siteSettings || null,
        mediaTypes: optionsResponse.mediaTypes || [],
        redirectStatusCodes: optionsResponse.redirectStatusCodes || [],
      });
      setPosts(postsResponse.items || []);
      setMediaAssets(mediaResponse.items || []);
      setRedirects(redirectsResponse.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải dữ liệu CMS.');
      setOptions({
        categories: [],
        tags: [],
        mediaAssets: [],
        siteSettings: null,
        mediaTypes: [],
        redirectStatusCodes: [],
      });
      setPosts([]);
      setMediaAssets([]);
      setRedirects([]);
    } finally {
      setLoading(false);
    }
  }

  return {
    session,
    isAdmin,
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    loading,
    error,
    setError,
    options,
    posts,
    mediaAssets,
    redirects,
    reload: loadWorkspace,
  };
}
