import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Armchair,
  Bus,
  CircleDollarSign,
  Clock3,
  LocateFixed,
  MapPinned,
  Route,
  ShieldCheck,
  Store,
  Waypoints,
} from 'lucide-react';
import { getBusManagementSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Chuyến xe', icon: Bus },
  { key: 'operations', label: 'Điều hành', icon: Waypoints },
  { key: 'providers', label: 'Đội xe', icon: Store },
  { key: 'stop-points', label: 'Danh mục bến', icon: MapPinned },
  { key: 'routes', label: 'Tuyến đường', icon: Route },
  { key: 'trip-stop-times', label: 'Lịch dừng', icon: Clock3 },
  { key: 'trip-stop-points', label: 'Đón/trả theo chuyến', icon: LocateFixed },
  { key: 'trip-segment-prices', label: 'Giá chặng', icon: CircleDollarSign },
  { key: 'vehicles', label: 'Xe', icon: Bus },
  { key: 'vehicle-details', label: 'Chi tiết xe', icon: Bus },
  { key: 'seat-maps', label: 'Sơ đồ xe', icon: Armchair },
  { key: 'trip-seats', label: 'Sơ đồ ghế', icon: Armchair },
  { key: 'seat-holds', label: 'Giữ chỗ', icon: ShieldCheck },
];

const BusManagementSecondaryNav = ({ currentKey = 'overview' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={getBusManagementSectionPath(item.key)}
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

export default BusManagementSecondaryNav;
