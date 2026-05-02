import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  ArrowRight,
  Calendar,
  Edit3,
  Eye,
  FileText,
  Globe,
  Image as ImageIcon,
  Link as LinkIcon,
  Plus,
  RefreshCw,
  Save,
  Search,
  ShieldCheck,
  Tag,
  Trash2,
  Upload,
} from 'lucide-react';
import { listAdminTenants } from '../../../services/adminIdentity';
import {
  archiveCmsPost,
  auditCmsDraft,
  buildCmsHtmlFromMarkdown,
  createCmsCategory,
  createCmsMedia,
  createCmsPost,
  createCmsRedirect,
  createCmsTag,
  deleteCmsMedia,
  deleteCmsPost,
  deleteCmsRedirect,
  getCmsOptions,
  getCmsPost,
  getCmsPostAudit,
  getCmsPostPreview,
  getCmsSiteSettings,
  listCmsMedia,
  listCmsPosts,
  listCmsRedirects,
  previewCmsDraft,
  publishCmsPost,
  restoreCmsMedia,
  restoreCmsPost,
  restoreCmsRedirect,
  scheduleCmsPost,
  unpublishCmsPost,
  updateCmsPost,
  upsertCmsSiteSettings,
} from '../../../services/cmsService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import CmsSecondaryNav from './CmsSecondaryNav';
import useLatestRef from '../../../shared/hooks/useLatestRef';

function slugify(value) {
  return String(value || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function formatDate(value, withTime = false) {
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

function normalizePostStatus(status) {
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

function getStatusClass(status) {
  const normalized = normalizePostStatus(status);
  if (normalized === 'Published') return 'bg-green-100 text-green-600';
  if (normalized === 'Scheduled') return 'bg-blue-100 text-blue-600';
  if (normalized === 'Archived' || normalized === 'Unpublished') return 'bg-slate-100 text-slate-600';
  return 'bg-amber-100 text-amber-600';
}

function getStatusLabel(status) {
  const normalized = normalizePostStatus(status);
  if (normalized === 'Published') return 'Đã xuất bản';
  if (normalized === 'Scheduled') return 'Đã lên lịch';
  if (normalized === 'Unpublished') return 'Ngừng hiển thị';
  if (normalized === 'Archived') return 'Lưu trữ';
  return 'Bản nháp';
}

function buildEmptyPostForm() {
  return {
    title: '',
    slug: '',
    summary: '',
    contentMarkdown: '',
    coverImageUrl: '',
    coverMediaAssetId: '',
    categoryIds: [],
    tagIds: [],
    seoTitle: '',
    seoDescription: '',
    seoKeywords: '',
    canonicalUrl: '',
    robots: 'index,follow',
    status: 'Draft',
    publishedAt: '',
    unpublishedAt: '',
    scheduledAt: '',
    changeNote: '',
  };
}

function buildEmptyMediaForm() {
  return {
    fileName: '',
    title: '',
    altText: '',
    publicUrl: '',
    mimeType: 'image/jpeg',
    storageProvider: 'local',
    storageKey: '',
    sizeBytes: 0,
    width: '',
    height: '',
    type: 1,
  };
}

function buildEmptyRedirectForm() {
  return {
    fromPath: '',
    toPath: '',
    statusCode: 301,
    reason: '',
  };
}

function buildEmptySiteSettings() {
  return {
    siteName: '',
    siteUrl: '',
    defaultRobots: 'index,follow',
    defaultOgImageUrl: '',
    defaultTwitterCard: 'summary_large_image',
    defaultTwitterSite: '',
    defaultSchemaJsonLd: '',
    brandLogoUrl: '',
    supportEmail: '',
    supportPhone: '',
    isActive: true,
  };
}

function normalizePostPayload(form) {
  const scheduledAt = form.status === 'Scheduled' ? (form.scheduledAt || null) : null;
  const publishedAt = form.status === 'Published' ? (form.publishedAt || new Date().toISOString()) : null;
  const unpublishedAt = form.status === 'Unpublished' ? (form.unpublishedAt || new Date().toISOString()) : null;

  return {
    title: form.title.trim(),
    slug: slugify(form.slug || form.title),
    summary: form.summary.trim() || null,
    contentMarkdown: form.contentMarkdown,
    contentHtml: buildCmsHtmlFromMarkdown(form.contentMarkdown),
    coverMediaAssetId: form.coverMediaAssetId || null,
    coverImageUrl: form.coverImageUrl.trim() || null,
    seoTitle: form.seoTitle.trim() || null,
    seoDescription: form.seoDescription.trim() || null,
    seoKeywords: form.seoKeywords.trim() || null,
    canonicalUrl: form.canonicalUrl.trim() || null,
    robots: form.robots.trim() || null,
    ogTitle: form.seoTitle.trim() || form.title.trim(),
    ogDescription: form.seoDescription.trim() || form.summary.trim() || null,
    ogImageUrl: form.coverImageUrl.trim() || null,
    ogType: 'article',
    twitterCard: 'summary_large_image',
    twitterTitle: form.seoTitle.trim() || form.title.trim(),
    twitterDescription: form.seoDescription.trim() || form.summary.trim() || null,
    twitterImageUrl: form.coverImageUrl.trim() || null,
    scheduledAt,
    publishedAt,
    unpublishedAt,
    categoryIds: form.categoryIds,
    tagIds: form.tagIds,
    changeNote: form.changeNote.trim() || null,
  };
}

function normalizeDraftAuditPayload(form, options) {
  const categories = (options.categories || [])
    .filter((item) => form.categoryIds.includes(item.id))
    .map((item) => ({ id: item.id, name: item.name, slug: item.slug }));
  const tags = (options.tags || [])
    .filter((item) => form.tagIds.includes(item.id))
    .map((item) => ({ id: item.id, name: item.name, slug: item.slug }));
  const siteSettings = options.siteSettings || {};
  const words = form.contentMarkdown.trim().split(/\s+/).filter(Boolean).length;

  return {
    ...normalizePostPayload(form),
    wordCount: words,
    readingTimeMinutes: words > 0 ? Math.max(1, Math.ceil(words / 200)) : 0,
    categories,
    tags,
    siteUrl: siteSettings.siteUrl,
    siteDefaultRobots: siteSettings.defaultRobots,
    siteDefaultOgImageUrl: siteSettings.defaultOgImageUrl,
    siteDefaultTwitterCard: siteSettings.defaultTwitterCard,
    status: form.status === 'Published' ? 3 : form.status === 'Scheduled' ? 2 : 1,
    scheduledAt: form.status === 'Scheduled' ? (form.scheduledAt || null) : null,
  };
}

function mapPostToForm(postResponse) {
  return {
    title: postResponse.title || '',
    slug: postResponse.slug || '',
    summary: postResponse.summary || '',
    contentMarkdown: postResponse.contentMarkdown || '',
    coverImageUrl: postResponse.coverImageUrl || '',
    coverMediaAssetId: postResponse.coverMediaAssetId || '',
    categoryIds: Array.isArray(postResponse.categories) ? postResponse.categories.map((item) => item.id) : [],
    tagIds: Array.isArray(postResponse.tags) ? postResponse.tags.map((item) => item.id) : [],
    seoTitle: postResponse.seoTitle || '',
    seoDescription: postResponse.seoDescription || '',
    seoKeywords: postResponse.seoKeywords || '',
    canonicalUrl: postResponse.canonicalUrl || '',
    robots: postResponse.robots || 'index,follow',
    status: normalizePostStatus(postResponse.status),
    publishedAt: postResponse.publishedAt || '',
    unpublishedAt: postResponse.unpublishedAt || '',
    scheduledAt: postResponse.scheduledAt ? String(postResponse.scheduledAt).slice(0, 16) : '',
    changeNote: '',
  };
}

function getSeoScore(audit) {
  if (typeof audit?.score === 'number') {
    return audit.score;
  }

  if (typeof audit?.summary?.score === 'number') {
    return audit.summary.score;
  }

  return null;
}

function getSeoIssueLevel(issue) {
  return issue?.level || issue?.severity || 'info';
}

const CmsWorkspacePage = ({ mode = 'admin' }) => {
  const session = useAuthSession();
  const isAdmin = mode === 'admin';
  const [activeTab, setActiveTab] = useState('news');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [tenants, setTenants] = useState([]);
  const [selectedTenantId, setSelectedTenantId] = useState('');
  const [options, setOptions] = useState({ categories: [], tags: [], mediaAssets: [], siteSettings: null, mediaTypes: [], redirectStatusCodes: [] });
  const [posts, setPosts] = useState([]);
  const [mediaAssets, setMediaAssets] = useState([]);
  const [redirects, setRedirects] = useState([]);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editingPostId, setEditingPostId] = useState(null);
  const [postForm, setPostForm] = useState(buildEmptyPostForm());
  const [slugTouched, setSlugTouched] = useState(false);
  const [seoAudit, setSeoAudit] = useState(null);
  const [preview, setPreview] = useState(null);
  const [mediaForm, setMediaForm] = useState(buildEmptyMediaForm());
  const [redirectForm, setRedirectForm] = useState(buildEmptyRedirectForm());
  const [siteSettingsForm, setSiteSettingsForm] = useState(buildEmptySiteSettings());
  const [categoryForm, setCategoryForm] = useState({ name: '', slug: '', description: '' });
  const [tagForm, setTagForm] = useState({ name: '', slug: '' });

  const tenantId = isAdmin ? selectedTenantId : session?.currentTenantId;
  const selectedTenant = useMemo(() => tenants.find((item) => item?.id === selectedTenantId) || null, [selectedTenantId, tenants]);

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

        const nextTenants = Array.isArray(response.items)
          ? response.items.filter((item) => item?.id)
          : [];

        setTenants(nextTenants);
        if (!selectedTenantId && nextTenants.length) {
          setSelectedTenantId(nextTenants[0].id);
        } else if (selectedTenantId && !nextTenants.some((item) => item.id === selectedTenantId)) {
          setSelectedTenantId(nextTenants[0]?.id || '');
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
  }, [tenantId, activeTab, search, loadWorkspaceRef, isAdmin]);

  useEffect(() => {
    if (!options.siteSettings) {
      return;
    }

    setSiteSettingsForm({
      siteName: options.siteSettings.siteName || '',
      siteUrl: options.siteSettings.siteUrl || '',
      defaultRobots: options.siteSettings.defaultRobots || 'index,follow',
      defaultOgImageUrl: options.siteSettings.defaultOgImageUrl || '',
      defaultTwitterCard: options.siteSettings.defaultTwitterCard || 'summary_large_image',
      defaultTwitterSite: options.siteSettings.defaultTwitterSite || '',
      defaultSchemaJsonLd: options.siteSettings.defaultSchemaJsonLd || '',
      brandLogoUrl: options.siteSettings.brandLogoUrl || '',
      supportEmail: options.siteSettings.supportEmail || '',
      supportPhone: options.siteSettings.supportPhone || '',
      isActive: options.siteSettings.isActive !== false,
    });
  }, [options.siteSettings]);

  async function loadWorkspace() {
    setLoading(true);
    setError('');

    try {
      const [optionsResponse, postsResponse, mediaResponse, redirectsResponse, siteSettingsResponse] = await Promise.all([
        getCmsOptions(tenantId),
        listCmsPosts({ q: search, page: 1, pageSize: 100 }, tenantId),
        listCmsMedia({ page: 1, pageSize: 60 }, tenantId),
        listCmsRedirects({ q: search, page: 1, pageSize: 100 }, tenantId),
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
      setPosts([]);
      setMediaAssets([]);
      setRedirects([]);
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateNewPost() {
    setEditorOpen(true);
    setEditingPostId(null);
    setSlugTouched(false);
    setSeoAudit(null);
    setPreview(null);
    setPostForm(buildEmptyPostForm());
  }

  async function handleEditPost(postId) {
    setSaving(true);
    setError('');

    try {
      const [postResponse, auditResponse, previewResponse] = await Promise.all([
        getCmsPost(postId, tenantId),
        getCmsPostAudit(postId, tenantId).catch(() => null),
        getCmsPostPreview(postId, tenantId).catch(() => null),
      ]);

      setEditingPostId(postResponse.id);
      setSlugTouched(true);
      setEditorOpen(true);
      setSeoAudit(auditResponse);
      setPreview(previewResponse);
      setPostForm(mapPostToForm(postResponse));
    } catch (err) {
      setError(err.message || 'Không thể tải bài viết.');
    } finally {
      setSaving(false);
    }
  }

  async function runSeoTools() {
    setSaving(true);
    setError('');

    try {
      const payload = normalizeDraftAuditPayload(postForm, options);
      const [auditResponse, previewResponse] = await Promise.all([
        auditCmsDraft(payload, tenantId),
        previewCmsDraft(payload, tenantId),
      ]);

      setSeoAudit(auditResponse);
      setPreview(previewResponse);
      setNotice('Đã cập nhật bản xem trước và đánh giá SEO cho bản nháp hiện tại.');
    } catch (err) {
      setError(err.message || 'Không thể phân tích SEO cho bài viết.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSavePost(action = 'draft') {
    setSaving(true);
    setError('');

    try {
      const nextForm = {
        ...postForm,
        status: action === 'publish'
          ? 'Published'
          : action === 'schedule'
            ? 'Scheduled'
            : postForm.status || 'Draft',
      };
      const payload = normalizePostPayload(nextForm);
      let postId = editingPostId;

      if (editingPostId) {
        await updateCmsPost(editingPostId, payload, tenantId);
      } else {
        const createResponse = await createCmsPost(payload, tenantId);
        postId = createResponse.id;
        setEditingPostId(postId);
      }

      if (action === 'publish' && postId) {
        await publishCmsPost(postId, { changeNote: postForm.changeNote || 'Publish from CMS workspace' }, tenantId);
      }

      if (action === 'schedule' && postId) {
        await scheduleCmsPost(postId, { scheduledAt: postForm.scheduledAt, changeNote: postForm.changeNote || 'Schedule from CMS workspace' }, tenantId);
      }

      if (postId) {
        const [postResponse, auditResponse, previewResponse] = await Promise.all([
          getCmsPost(postId, tenantId),
          getCmsPostAudit(postId, tenantId).catch(() => null),
          getCmsPostPreview(postId, tenantId).catch(() => null),
        ]);
        setPostForm(mapPostToForm(postResponse));
        setSeoAudit(auditResponse);
        setPreview(previewResponse);
        setSlugTouched(true);
      }

      setNotice(action === 'publish'
        ? 'Bài viết đã được đăng ngay.'
        : action === 'schedule'
          ? 'Bài viết đã được lên lịch.'
          : 'Bản nháp đã được lưu.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể lưu bài viết.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDeletePost(post) {
    setSaving(true);
    setError('');

    try {
      if (post.isDeleted) {
        await restoreCmsPost(post.id, tenantId);
        setNotice('Bài viết đã được khôi phục.');
      } else {
        await deleteCmsPost(post.id, tenantId);
        setNotice('Bài viết đã được đưa vào thùng rác.');
      }
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái bài viết.');
    } finally {
      setSaving(false);
    }
  }

  async function handleArchivePost(postId) {
    setSaving(true);
    setError('');

    try {
      await archiveCmsPost(postId, { changeNote: 'Archive from CMS workspace' }, tenantId);
      setNotice('Bài viết đã được lưu trữ.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể lưu trữ bài viết.');
    } finally {
      setSaving(false);
    }
  }

  async function handleTogglePublish(post) {
    setSaving(true);
    setError('');

    try {
      if (normalizePostStatus(post.status) === 'Published') {
        await unpublishCmsPost(post.id, { changeNote: 'Unpublish from CMS workspace' }, tenantId);
        setNotice('Bài viết đã được gỡ khỏi kênh public.');
      } else {
        await publishCmsPost(post.id, { changeNote: 'Publish from CMS workspace' }, tenantId);
        setNotice('Bài viết đã được đăng công khai.');
      }

      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái xuất bản.');
    } finally {
      setSaving(false);
    }
  }

  async function handleCreateMedia() {
    setSaving(true);
    setError('');

    try {
      await createCmsMedia({
        ...mediaForm,
        fileName: mediaForm.fileName.trim(),
        title: mediaForm.title.trim() || null,
        altText: mediaForm.altText.trim() || null,
        publicUrl: mediaForm.publicUrl.trim() || null,
        storageKey: mediaForm.storageKey.trim() || mediaForm.fileName.trim(),
        width: mediaForm.width ? Number(mediaForm.width) : null,
        height: mediaForm.height ? Number(mediaForm.height) : null,
        sizeBytes: Number(mediaForm.sizeBytes || 0),
      }, tenantId);

      setMediaForm(buildEmptyMediaForm());
      setNotice('Metadata của media đã được tạo.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo media.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleMedia(media) {
    setSaving(true);
    setError('');

    try {
      if (media.isDeleted) {
        await restoreCmsMedia(media.id, tenantId);
        setNotice('Media đã được khôi phục.');
      } else {
        await deleteCmsMedia(media.id, tenantId);
        setNotice('Media đã được đưa vào thùng rác.');
      }
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật media.');
    } finally {
      setSaving(false);
    }
  }

  async function handleCreateRedirect() {
    setSaving(true);
    setError('');

    try {
      await createCmsRedirect({
        fromPath: redirectForm.fromPath.trim(),
        toPath: redirectForm.toPath.trim(),
        statusCode: Number(redirectForm.statusCode),
        reason: redirectForm.reason.trim() || null,
        isRegex: false,
        isActive: true,
      }, tenantId);

      setRedirectForm(buildEmptyRedirectForm());
      setNotice('Redirect mới đã được tạo.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo redirect.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleRedirect(redirect) {
    setSaving(true);
    setError('');

    try {
      if (redirect.isDeleted) {
        await restoreCmsRedirect(redirect.id, tenantId);
        setNotice('Redirect đã được khôi phục.');
      } else {
        await deleteCmsRedirect(redirect.id, tenantId);
        setNotice('Redirect đã được đưa vào thùng rác.');
      }
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật redirect.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSaveSiteSettings() {
    setSaving(true);
    setError('');

    try {
      await upsertCmsSiteSettings(siteSettingsForm, tenantId);
      setNotice('Thông tin SEO của site đã được cập nhật.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể lưu cấu hình site.');
    } finally {
      setSaving(false);
    }
  }

  async function handleCreateCategory() {
    setSaving(true);
    setError('');

    try {
      await createCmsCategory({
        name: categoryForm.name.trim(),
        slug: slugify(categoryForm.slug || categoryForm.name),
        description: categoryForm.description.trim() || null,
        sortOrder: options.categories.length + 1,
        isActive: true,
      }, tenantId);

      setCategoryForm({ name: '', slug: '', description: '' });
      setNotice('Danh mục mới đã được tạo.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo danh mục.');
    } finally {
      setSaving(false);
    }
  }

  async function handleCreateTag() {
    setSaving(true);
    setError('');

    try {
      await createCmsTag({
        name: tagForm.name.trim(),
        slug: slugify(tagForm.slug || tagForm.name),
        isActive: true,
      }, tenantId);

      setTagForm({ name: '', slug: '' });
      setNotice('Thẻ mới đã được tạo.');
      await loadWorkspaceRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo thẻ.');
    } finally {
      setSaving(false);
    }
  }

  function updatePostField(field, value) {
    setPostForm((current) => {
      const next = { ...current, [field]: value };
      if (field === 'title' && !slugTouched) {
        next.slug = slugify(value);
      }
      return next;
    });
  }

  function toggleSelection(field, id) {
    setPostForm((current) => ({
      ...current,
      [field]: current[field].includes(id)
        ? current[field].filter((item) => item !== id)
        : [...current[field], id],
    }));
  }

  const workspaceTitle = isAdmin ? 'Quản trị nội dung và CMS' : 'Quản lý nội dung và CMS';
  const workspaceSubtitle = isAdmin
    ? 'Admin và nhà quản lý có thể đăng bài, tối ưu SEO và quản lý media theo từng tenant.'
    : 'Đăng bài tin tức theo phong cách social-posting nhưng vẫn đầy đủ SEO, media và redirect.';
  const previewPath = preview?.post?.publicPath || '/tin-tuc/preview';

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">{workspaceTitle}</h1>
          <p className="text-slate-500 font-medium mt-1">{workspaceSubtitle}</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {isAdmin && (
            <div className="px-4 py-3 bg-white rounded-2xl border border-slate-100 shadow-sm">
              <select
                value={selectedTenantId}
                onChange={(event) => setSelectedTenantId(event.target.value)}
                className="bg-transparent outline-none text-sm font-bold text-slate-700"
              >
                {tenants.map((item) => (
                  <option key={item.id} value={item.id}>{item.name}</option>
                ))}
              </select>
            </div>
          )}
          <div className="flex bg-slate-100 p-1 rounded-2xl border border-slate-200">
            {[
              { id: 'news', label: 'Bài viết', icon: <FileText size={14} /> },
              { id: 'media', label: 'Thư viện', icon: <ImageIcon size={14} /> },
              { id: 'marketing', label: 'Marketing', icon: <Tag size={14} /> },
              { id: 'redirects', label: 'Chuyển hướng', icon: <LinkIcon size={14} /> },
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-5 py-2.5 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all ${
                  activeTab === tab.id ? 'bg-white text-blue-600 shadow-md' : 'text-slate-400 hover:text-slate-600'
                }`}
              >
                {tab.icon} {tab.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <CmsSecondaryNav mode={mode} currentKey="posts" />

      {selectedTenant && isAdmin && (
        <div className="rounded-2xl border border-sky-100 bg-sky-50 px-5 py-4 text-sm font-bold text-sky-700">
          Đang quản lý nội dung cho tenant: {selectedTenant.name}
        </div>
      )}

      {notice && (
        <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      )}

      {activeTab === 'news' && (
        <div className="space-y-6">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-4 bg-white px-5 py-3 rounded-2xl border border-slate-100 shadow-sm w-full md:w-96">
              <Search size={18} className="text-slate-300" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                type="text"
                placeholder="Tìm kiếm bài viết..."
                className="bg-transparent border-none focus:outline-none text-sm w-full font-medium"
              />
            </div>
            <div className="flex items-center gap-3">
              <button onClick={loadWorkspace} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
                <RefreshCw size={14} /> Đồng bộ
              </button>
              <button onClick={handleCreateNewPost} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-xl hover:bg-blue-600 transition-all">
                <Plus size={16} /> Viết bài mới
              </button>
            </div>
          </div>

          {editorOpen && (
            <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 p-8 space-y-6">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <h3 className="text-xl font-black text-slate-900">{editingPostId ? 'Cập nhật bài viết' : 'Soạn bài viết mới'}</h3>
                  <p className="text-sm text-slate-500 font-medium mt-1">Giữ layout cũ, bổ sung workflow đăng bài và SEO thật.</p>
                </div>
                <button
                  onClick={() => {
                    setEditorOpen(false);
                    setEditingPostId(null);
                    setSeoAudit(null);
                    setPreview(null);
                    setPostForm(buildEmptyPostForm());
                    setSlugTouched(false);
                  }}
                  className="px-5 py-3 bg-slate-50 text-slate-600 rounded-2xl font-black text-xs uppercase tracking-widest"
                >
                  Đóng
                </button>
              </div>

              <div className="grid grid-cols-1 xl:grid-cols-[1.2fr,0.8fr] gap-6">
                <div className="space-y-5">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <input value={postForm.title} onChange={(event) => updatePostField('title', event.target.value)} placeholder="Tiêu đề bài viết" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-800 outline-none" />
                    <input value={postForm.slug} onChange={(event) => { setSlugTouched(true); updatePostField('slug', slugify(event.target.value)); }} placeholder="duong-dan-seo" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-800 outline-none" />
                  </div>
                  <textarea value={postForm.summary} onChange={(event) => updatePostField('summary', event.target.value)} rows={3} placeholder="Tóm tắt ngắn cho danh sách và mạng xã hội" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
                  <textarea value={postForm.contentMarkdown} onChange={(event) => updatePostField('contentMarkdown', event.target.value)} rows={12} placeholder="Nhập nội dung bài viết..." className="w-full rounded-[2rem] border border-slate-100 bg-slate-50 px-5 py-5 text-sm font-medium text-slate-700 outline-none resize-y" />
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <input value={postForm.coverImageUrl} onChange={(event) => updatePostField('coverImageUrl', event.target.value)} placeholder="URL ảnh bìa" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <select value={postForm.coverMediaAssetId} onChange={(event) => updatePostField('coverMediaAssetId', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                      <option value="">Không gắn media</option>
                      {(options.mediaAssets || []).map((item) => (
                        <option key={item.id} value={item.id}>{item.title || item.fileName}</option>
                      ))}
                    </select>
                  </div>
                  <div className="space-y-3">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Danh mục</p>
                    <div className="flex flex-wrap gap-2">
                      {(options.categories || []).map((item) => (
                        <button key={item.id} type="button" onClick={() => toggleSelection('categoryIds', item.id)} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${postForm.categoryIds.includes(item.id) ? 'bg-slate-900 text-white' : 'bg-slate-50 text-slate-500 hover:bg-slate-100'}`}>
                          {item.name}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="space-y-3">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Thẻ</p>
                    <div className="flex flex-wrap gap-2">
                      {(options.tags || []).map((item) => (
                        <button key={item.id} type="button" onClick={() => toggleSelection('tagIds', item.id)} className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${postForm.tagIds.includes(item.id) ? 'bg-blue-600 text-white' : 'bg-slate-50 text-slate-500 hover:bg-slate-100'}`}>
                          {item.name}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <input value={postForm.seoTitle} onChange={(event) => updatePostField('seoTitle', event.target.value)} placeholder="Tiêu đề SEO" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <input value={postForm.seoKeywords} onChange={(event) => updatePostField('seoKeywords', event.target.value)} placeholder="từ khóa 1, từ khóa 2" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                  </div>
                  <textarea value={postForm.seoDescription} onChange={(event) => updatePostField('seoDescription', event.target.value)} rows={3} placeholder="Mô tả SEO" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <input value={postForm.canonicalUrl} onChange={(event) => updatePostField('canonicalUrl', event.target.value)} placeholder="URL chuẩn (canonical)" className="md:col-span-2 w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <select value={postForm.robots} onChange={(event) => updatePostField('robots', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                      <option value="index,follow">index,follow</option>
                      <option value="noindex,follow">noindex,follow</option>
                      <option value="index,nofollow">index,nofollow</option>
                      <option value="noindex,nofollow">noindex,nofollow</option>
                    </select>
                  </div>
                </div>

                <div className="space-y-5">
                  <div className="bg-slate-50 rounded-[2rem] p-6 border border-slate-100 space-y-4">
                    <div className="flex items-center gap-2 text-slate-900"><ShieldCheck size={18} /><h4 className="font-black">Hành động xuất bản</h4></div>
                    <input type="datetime-local" value={postForm.scheduledAt} onChange={(event) => updatePostField('scheduledAt', event.target.value)} className="w-full rounded-2xl border border-slate-100 bg-white px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <input value={postForm.changeNote} onChange={(event) => updatePostField('changeNote', event.target.value)} placeholder="Ghi chú cho lần chỉnh sửa" className="w-full rounded-2xl border border-slate-100 bg-white px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
                    <div className="grid grid-cols-1 gap-3">
                      <button onClick={() => handleSavePost('draft')} disabled={saving} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl"><Save size={14} /> Lưu bản nháp</button>
                      <button onClick={() => handleSavePost('publish')} disabled={saving} className="px-5 py-3 bg-blue-600 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl"><Globe size={14} /> Đăng ngay</button>
                      <button onClick={() => handleSavePost('schedule')} disabled={saving || !postForm.scheduledAt} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 border border-slate-100 shadow-sm disabled:opacity-60"><Calendar size={14} /> Lên lịch</button>
                      <button onClick={runSeoTools} disabled={saving} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 border border-slate-100 shadow-sm"><Eye size={14} /> Xem trước & SEO</button>
                    </div>
                  </div>
                  <div className="bg-white rounded-[2rem] border border-slate-100 p-6 space-y-4 shadow-sm">
                    <div className="flex items-center gap-2 text-slate-900"><Tag size={18} /><h4 className="font-black">Điểm SEO</h4></div>
                    <div className="text-4xl font-black text-slate-900">{getSeoScore(seoAudit) ?? '--'}<span className="text-base text-slate-400 ml-1">/100</span></div>
                    <div className="space-y-2">
                      {(seoAudit?.issues || []).slice(0, 4).map((issue) => (
                        <div key={`${issue.code}-${getSeoIssueLevel(issue)}`} className="rounded-2xl bg-slate-50 px-4 py-3 border border-slate-100">
                          <p className={`text-[10px] font-black uppercase tracking-widest ${getSeoIssueLevel(issue) === 'error' ? 'text-rose-500' : getSeoIssueLevel(issue) === 'warning' ? 'text-amber-500' : 'text-sky-500'}`}>{getSeoIssueLevel(issue)}</p>
                          <p className="text-sm font-bold text-slate-700 mt-1">{issue.message}</p>
                        </div>
                      ))}
                      {!seoAudit?.issues?.length && <div className="rounded-2xl bg-emerald-50 px-4 py-3 border border-emerald-100 text-sm font-bold text-emerald-700">Chưa có cảnh báo SEO nào.</div>}
                    </div>
                  </div>
                  <div className="bg-white rounded-[2rem] border border-slate-100 p-6 space-y-4 shadow-sm">
                    <div className="flex items-center gap-2 text-slate-900"><Eye size={18} /><h4 className="font-black">Xem trước trên tìm kiếm</h4></div>
                    <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100">
                      <p className="text-xs font-bold text-emerald-600 break-all">{preview?.seo?.canonicalUrl || previewPath}</p>
                      <h5 className="font-black text-blue-700 text-lg mt-1 line-clamp-2">{preview?.seo?.title || postForm.seoTitle || postForm.title || 'Tiêu đề bài viết'}</h5>
                      <p className="text-sm text-slate-500 mt-2 line-clamp-3">{preview?.seo?.description || postForm.seoDescription || postForm.summary || 'Mô tả SEO sẽ hiển thị tại đây.'}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-slate-50/50 border-b border-slate-100">
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Tiêu đề & chuyên mục</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Tác giả</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Ngày</th>
                  <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Hành động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {loading ? (
                  <tr><td colSpan={5} className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải bài viết CMS...</td></tr>
                ) : posts.length === 0 ? (
                  <tr><td colSpan={5} className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có bài viết phù hợp.</td></tr>
                ) : posts.map((post) => (
                  <tr key={post.id} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-8 py-5"><p className="font-black text-slate-900 line-clamp-1">{post.title}</p><span className="text-[10px] font-bold text-blue-500 uppercase tracking-wider">{post.primaryCategoryName || 'Chưa phân loại'}</span></td>
                    <td className="px-8 py-5 text-sm font-bold text-slate-600">{post.authorName || 'Quản trị viên'}</td>
                    <td className="px-8 py-5"><span className={`px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest ${getStatusClass(post.status)}`}>{getStatusLabel(post.status)}</span></td>
                    <td className="px-8 py-5 text-sm font-bold text-slate-500">{formatDate(post.publishedAt || post.updatedAt || post.createdAt)}</td>
                    <td className="px-8 py-5">
                      <div className="flex items-center gap-2 flex-wrap">
                        <button onClick={() => handleEditPost(post.id)} className="p-2 text-slate-400 hover:text-blue-600 transition-colors"><Edit3 size={16} /></button>
                        <button onClick={() => handleTogglePublish(post)} className="p-2 text-slate-400 hover:text-emerald-600 transition-colors"><Globe size={16} /></button>
                        <button onClick={() => handleArchivePost(post.id)} className="p-2 text-slate-400 hover:text-amber-600 transition-colors"><Tag size={16} /></button>
                        <button onClick={() => handleDeletePost(post)} className="p-2 text-slate-400 hover:text-red-500 transition-colors"><Trash2 size={16} /></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === 'media' && (
        <div className="grid grid-cols-1 lg:grid-cols-[0.8fr,1.2fr] gap-6">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
            <h3 className="text-lg font-black text-slate-900">Thêm metadata cho media</h3>
            <input value={mediaForm.fileName} onChange={(event) => setMediaForm((current) => ({ ...current, fileName: event.target.value }))} placeholder="hero-banner.jpg" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            <input value={mediaForm.publicUrl} onChange={(event) => setMediaForm((current) => ({ ...current, publicUrl: event.target.value }))} placeholder="https://cdn.example.com/hero.jpg" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={mediaForm.title} onChange={(event) => setMediaForm((current) => ({ ...current, title: event.target.value }))} placeholder="Tiêu đề media" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={mediaForm.altText} onChange={(event) => setMediaForm((current) => ({ ...current, altText: event.target.value }))} placeholder="Văn bản thay thế cho SEO" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <button onClick={handleCreateMedia} className="w-full px-6 py-3 bg-blue-600 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl shadow-blue-600/20"><Upload size={16} /> Tạo media</button>
          </div>
          <div className="space-y-4">
            <div className="flex justify-between items-center"><h3 className="text-lg font-black text-slate-900">Thư viện media</h3><button onClick={loadWorkspace} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm"><RefreshCw size={14} /> Tải lại</button></div>
            <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-4">
              {mediaAssets.map((asset) => (
                <div key={asset.id} className="group bg-white rounded-3xl p-4 border border-slate-100 shadow-sm hover:shadow-xl transition-all">
                  <div className="aspect-square bg-slate-50 rounded-2xl mb-3 flex items-center justify-center text-slate-300 overflow-hidden">
                    {asset.publicUrl ? <img src={asset.publicUrl} alt={asset.altText || asset.fileName} className="w-full h-full object-cover" /> : <ImageIcon size={32} />}
                  </div>
                  <p className="text-[11px] font-black text-slate-900 truncate mb-1">{asset.title || asset.fileName}</p>
                  <div className="flex justify-between items-center text-[9px] font-bold text-slate-400">
                    <span>{asset.mimeType}</span>
                    <button onClick={() => handleToggleMedia(asset)} className="hover:text-red-500 transition-colors"><Trash2 size={12} /></button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'marketing' && (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-8">
          <div className="bg-white rounded-[2.5rem] p-6 border border-slate-100 shadow-sm space-y-4">
            <div className="flex items-center gap-2 text-slate-900"><Globe size={18} /><h4 className="font-black text-lg">Cấu hình SEO của site</h4></div>
            <input value={siteSettingsForm.siteName} onChange={(event) => setSiteSettingsForm((current) => ({ ...current, siteName: event.target.value }))} placeholder="Tên site" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            <input value={siteSettingsForm.siteUrl} onChange={(event) => setSiteSettingsForm((current) => ({ ...current, siteUrl: event.target.value }))} placeholder="https://example.com" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={siteSettingsForm.defaultOgImageUrl} onChange={(event) => setSiteSettingsForm((current) => ({ ...current, defaultOgImageUrl: event.target.value }))} placeholder="Ảnh OG mặc định" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <input value={siteSettingsForm.supportEmail} onChange={(event) => setSiteSettingsForm((current) => ({ ...current, supportEmail: event.target.value }))} placeholder="support@email.com" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <button onClick={handleSaveSiteSettings} className="w-full px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl"><Save size={16} /> Lưu cấu hình SEO</button>
          </div>
          <div className="bg-white rounded-[2.5rem] p-6 border border-slate-100 shadow-sm space-y-4">
            <div className="flex items-center gap-2 text-slate-900"><Tag size={18} /><h4 className="font-black text-lg">Danh mục bài viết</h4></div>
            <input value={categoryForm.name} onChange={(event) => setCategoryForm((current) => ({ ...current, name: event.target.value, slug: current.slug || slugify(event.target.value) }))} placeholder="Tên danh mục" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            <input value={categoryForm.slug} onChange={(event) => setCategoryForm((current) => ({ ...current, slug: slugify(event.target.value) }))} placeholder="slug-danh-muc" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <textarea value={categoryForm.description} onChange={(event) => setCategoryForm((current) => ({ ...current, description: event.target.value }))} rows={3} placeholder="Mô tả ngắn cho trang SEO của danh mục" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none resize-none" />
            <button onClick={handleCreateCategory} className="w-full px-6 py-3 bg-blue-600 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl shadow-blue-600/20"><Plus size={16} /> Thêm danh mục</button>
          </div>
          <div className="bg-white rounded-[2.5rem] p-6 border border-slate-100 shadow-sm space-y-4">
            <div className="flex items-center gap-2 text-slate-900"><AlertTriangle size={18} /><h4 className="font-black text-lg">Thẻ và điều khiển SEO</h4></div>
            <input value={tagForm.name} onChange={(event) => setTagForm((current) => ({ ...current, name: event.target.value, slug: current.slug || slugify(event.target.value) }))} placeholder="Tên thẻ" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
            <input value={tagForm.slug} onChange={(event) => setTagForm((current) => ({ ...current, slug: slugify(event.target.value) }))} placeholder="slug-tag" className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-medium text-slate-700 outline-none" />
            <button onClick={handleCreateTag} className="w-full px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center justify-center gap-2 shadow-xl"><Plus size={16} /> Thêm thẻ</button>
            <div className="rounded-2xl bg-slate-50 p-5 border border-slate-100"><p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Đường dẫn xem trước SEO</p><p className="text-sm font-bold text-slate-700 break-all">{previewPath}</p></div>
          </div>
        </div>
      )}

      {activeTab === 'redirects' && (
        <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 p-8 space-y-6">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <h3 className="text-lg font-black text-slate-900">Quản lý chuyển hướng 301/302</h3>
            <div className="flex flex-wrap gap-3">
              <input value={redirectForm.fromPath} onChange={(event) => setRedirectForm((current) => ({ ...current, fromPath: event.target.value }))} placeholder="/old-blog-path" className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
              <input value={redirectForm.toPath} onChange={(event) => setRedirectForm((current) => ({ ...current, toPath: event.target.value }))} placeholder="/blog/new-clean-slug" className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none" />
              <select value={redirectForm.statusCode} onChange={(event) => setRedirectForm((current) => ({ ...current, statusCode: event.target.value }))} className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
                {(options.redirectStatusCodes || []).map((item) => (
                  <option key={item.value} value={item.value}>{item.value}</option>
                ))}
              </select>
              <button onClick={handleCreateRedirect} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2"><Plus size={16} /> Thêm redirect</button>
            </div>
          </div>
          <div className="space-y-3">
            {loading ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Đang tải danh sách redirect...</div>
            ) : redirects.length === 0 ? (
              <div className="rounded-2xl bg-slate-50 px-5 py-4 text-sm font-bold text-slate-400">Chưa có redirect nào.</div>
            ) : redirects.map((item) => (
              <div key={item.id} className="flex flex-col lg:flex-row lg:items-center gap-4 p-4 bg-slate-50 rounded-2xl border border-slate-100">
                <div className="font-bold text-sm text-slate-600 flex-1 break-all">{item.fromPath}</div>
                <ArrowRight size={16} className="text-slate-300" />
                <div className="font-bold text-sm text-blue-600 flex-1 break-all">{item.toPath}</div>
                <span className="bg-white px-3 py-1 rounded-lg text-[9px] font-black text-slate-400 border border-slate-100">{item.statusCode}</span>
                <button onClick={() => handleToggleRedirect(item)} className="text-slate-400 hover:text-red-500"><Trash2 size={16} /></button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default CmsWorkspacePage;
