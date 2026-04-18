import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Armchair,
  CircleDollarSign,
  Clock3,
  MapPinned,
  Package,
  Route,
  ShieldCheck,
  Store,
  Train,
  Waypoints,
} from 'lucide-react';
import { getTrainManagementSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Kho vé tàu', icon: Train },
  { key: 'operations', label: 'Vận hành', icon: Waypoints },
  { key: 'providers', label: 'Toa tàu', icon: Store },
  { key: 'stop-points', label: 'Ga tàu', icon: MapPinned },
  { key: 'routes', label: 'Tuyến đường', icon: Route },
  { key: 'cars', label: 'Toa tàu', icon: Package },
  { key: 'car-seats', label: 'Ghế & giường', icon: Armchair },
  { key: 'trip-stop-times', label: 'Lịch dừng', icon: Clock3 },
  { key: 'trip-segment-prices', label: 'Giá chặng', icon: CircleDollarSign },
  { key: 'trip-seats', label: 'Sơ đồ chỗ', icon: Train },
  { key: 'seat-holds', label: 'Giữ chỗ', icon: ShieldCheck },
];

const TrainManagementSecondaryNav = ({ currentKey = 'overview' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={getTrainManagementSectionPath(item.key)}
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

export default TrainManagementSecondaryNav;
