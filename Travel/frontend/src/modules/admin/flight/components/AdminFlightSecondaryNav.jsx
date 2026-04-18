import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  BadgeCent,
  Plane,
  Ticket,
  Armchair,
  MapPinned,
  BriefcaseBusiness,
  Layers3,
  Building2,
  ClipboardList,
  Grid2X2,
  ListChecks,
  ShoppingBag,
} from 'lucide-react';
import { getAdminFlightSectionPath } from '../utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Kho vé máy bay', icon: Plane },
  { key: 'airlines', label: 'Hãng bay', icon: Building2 },
  { key: 'airports', label: 'Sân bay', icon: MapPinned },
  { key: 'flights', label: 'Lịch bay', icon: Plane },
  { key: 'offers', label: 'Offer', icon: Ticket },
  { key: 'fare-classes', label: 'Fare class', icon: Layers3 },
  { key: 'fare-rules', label: 'Điều kiện vé', icon: ClipboardList },
  { key: 'tax-fee-lines', label: 'Thuế & phí', icon: BadgeCent },
  { key: 'aircraft-models', label: 'Mẫu tàu bay', icon: Grid2X2 },
  { key: 'aircrafts', label: 'Tàu bay', icon: BriefcaseBusiness },
  { key: 'seat-maps', label: 'Sơ đồ ghế', icon: Armchair },
  { key: 'seats', label: 'Ghế cabin', icon: ListChecks },
  { key: 'ancillaries', label: 'Dịch vụ thêm', icon: ShoppingBag },
];

const AdminFlightSecondaryNav = ({ currentKey = 'overview' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={getAdminFlightSectionPath(item.key)}
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

export default AdminFlightSecondaryNav;
