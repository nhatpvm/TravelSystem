import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Eye,
  FileText,
  Globe,
  Image as ImageIcon,
  RefreshCw,
  ShieldCheck,
  Tag,
} from 'lucide-react';
import { getCmsSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'posts', label: 'Bài viết', icon: FileText },
  { key: 'media', label: 'Thư viện', icon: ImageIcon },
  { key: 'categories', label: 'Danh mục', icon: Tag },
  { key: 'tags', label: 'Thẻ', icon: Tag },
  { key: 'revisions', label: 'Lịch sử sửa', icon: RefreshCw },
  { key: 'preview', label: 'Xem trước', icon: Eye },
  { key: 'seo-audit', label: 'Kiểm tra SEO', icon: ShieldCheck },
  { key: 'site-settings', label: 'Cấu hình site', icon: Globe },
];

const CmsSecondaryNav = ({ mode = 'admin', currentKey = 'posts' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;
      const targetPath = getCmsSectionPath(mode, item.key);

      return (
        <NavLink
          key={item.key}
          to={targetPath}
          className={`flex items-center gap-2 px-5 py-3 rounded-2xl text-[11px] font-black uppercase tracking-widest transition-all ${
            currentKey === item.key
              ? 'bg-white text-blue-600 shadow-sm border border-slate-100'
              : 'bg-slate-100 text-slate-400 hover:text-slate-600 border border-transparent'
          }`}
        >
          <Icon size={14} />
          {item.label}
        </NavLink>
      );
    })}
  </div>
);

export default CmsSecondaryNav;
