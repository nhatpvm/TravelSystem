import React from 'react';
import { NavLink } from 'react-router-dom';
import { BedDouble, Building2, ClipboardList, Layers3, WalletCards, CalendarDays } from 'lucide-react';
import { getHotelManagementSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Kho khách sạn', icon: Building2 },
  { key: 'room-types', label: 'Hạng phòng', icon: BedDouble },
  { key: 'rate-plans', label: 'Gói giá', icon: Layers3 },
  { key: 'policies', label: 'Chính sách', icon: ClipboardList },
  { key: 'extra-services', label: 'Dịch vụ thêm', icon: WalletCards },
  { key: 'ari', label: 'ARI', icon: CalendarDays },
];

const HotelManagementSecondaryNav = ({ currentKey = 'overview' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={getHotelManagementSectionPath(item.key)}
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

export default HotelManagementSecondaryNav;
