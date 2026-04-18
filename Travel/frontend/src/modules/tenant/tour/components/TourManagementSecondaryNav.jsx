import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import {
  Boxes,
  CalendarDays,
  CircleDollarSign,
  Compass,
  FileStack,
  Layers3,
  MapPinned,
  UsersRound,
} from 'lucide-react';
import { getTourManagementSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Tour', icon: Compass },
  { key: 'schedules', label: 'Lịch khởi hành', icon: CalendarDays },
  { key: 'pricing', label: 'Bảng giá', icon: CircleDollarSign },
  { key: 'capacity', label: 'Sức chứa', icon: UsersRound },
  { key: 'packages', label: 'Gói tour', icon: Boxes },
  { key: 'content', label: 'Nội dung', icon: FileStack },
  { key: 'experience', label: 'Trải nghiệm', icon: MapPinned },
  { key: 'builder', label: 'Package builder', icon: Layers3 },
  { key: 'reporting', label: 'Báo cáo package', icon: CircleDollarSign },
];

const TourManagementSecondaryNav = ({ currentKey = 'overview' }) => {
  const location = useLocation();
  const search = new URLSearchParams(location.search);
  const sharedParams = {
    tourId: search.get('tourId') || '',
    scheduleId: search.get('scheduleId') || '',
  };

  return (
    <div className="flex flex-wrap gap-3">
      {NAV_ITEMS.map((item) => {
        const Icon = item.icon;

        return (
          <NavLink
            key={item.key}
            to={getTourManagementSectionPath(item.key, sharedParams)}
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

export default TourManagementSecondaryNav;
