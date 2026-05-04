import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Building2,
  Database,
  Layers3,
  LocateFixed,
  Logs,
  MapPinned,
  PlayCircle,
  Route,
  ScanLine,
} from 'lucide-react';
import { getMasterDataSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Tổng quan', icon: Database },
  { key: 'locations', label: 'Địa điểm', icon: MapPinned },
  { key: 'providers', label: 'Đối tác', icon: Building2 },
  { key: 'geo-sync', label: 'Đồng bộ địa giới', icon: PlayCircle },
  { key: 'geo-sync-logs', label: 'Nhật ký địa giới', icon: Logs },
  { key: 'vehicle-models', label: 'Mẫu phương tiện', icon: Layers3 },
  { key: 'vehicles', label: 'Phương tiện', icon: Route },
  { key: 'seat-maps', label: 'Sơ đồ ghế', icon: LocateFixed },
  { key: 'seats', label: 'Ghế', icon: ScanLine },
];

const MasterDataSecondaryNav = ({ currentKey = 'overview', scope = 'admin' }) => {
  const items = scope === 'tenant'
    ? NAV_ITEMS.filter((item) => ['providers', 'vehicle-models', 'vehicles', 'seat-maps', 'seats'].includes(item.key))
    : NAV_ITEMS;

  return (
  <div className="flex flex-wrap gap-3">
    {items.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={getMasterDataSectionPath(item.key, scope)}
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
};

export default MasterDataSecondaryNav;
