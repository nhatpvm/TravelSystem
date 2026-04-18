import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  Bed,
  BedDouble,
  Building2,
  CalendarDays,
  ClipboardList,
  Image,
  Images,
  Layers3,
  PhoneCall,
  ShieldCheck,
  Sparkles,
  Star,
  Utensils,
  WalletCards,
  BadgePercent,
  LampDesk,
} from 'lucide-react';
import { getAdminHotelSectionPath } from '../../../tenant/hotel/utils/navigation';

const NAV_ITEMS = [
  { key: 'overview', label: 'Khách sạn', icon: Building2 },
  { key: 'room-types', label: 'Hạng phòng', icon: BedDouble },
  { key: 'rate-plans', label: 'Gói giá', icon: Layers3 },
  { key: 'policies', label: 'Chính sách', icon: ClipboardList },
  { key: 'extra-services', label: 'Dịch vụ thêm', icon: WalletCards },
  { key: 'contacts', label: 'Liên hệ', icon: PhoneCall },
  { key: 'images', label: 'Ảnh khách sạn', icon: Image },
  { key: 'amenities', label: 'Tiện nghi KS', icon: Sparkles },
  { key: 'room-amenities', label: 'Tiện nghi phòng', icon: LampDesk },
  { key: 'meal-plans', label: 'Meal plan', icon: Utensils },
  { key: 'bed-types', label: 'Loại giường', icon: Bed },
  { key: 'room-type-images', label: 'Ảnh hạng phòng', icon: Images },
  { key: 'room-type-policies', label: 'Policy hạng phòng', icon: ShieldCheck },
  { key: 'promo-overrides', label: 'Promo override', icon: BadgePercent },
  { key: 'reviews', label: 'Đánh giá', icon: Star },
  { key: 'ari', label: 'ARI', icon: CalendarDays },
];

const AdminHotelSecondaryNav = ({ currentKey = 'overview' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={getAdminHotelSectionPath(item.key)}
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

export default AdminHotelSecondaryNav;
