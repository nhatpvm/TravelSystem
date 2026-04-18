import React from 'react';
import { NavLink } from 'react-router-dom';
import { CalendarDays, Compass, FileQuestion, Star } from 'lucide-react';

const NAV_ITEMS = [
  { key: 'tours', label: 'Tour', icon: Compass, path: '/admin/tours' },
  { key: 'schedules', label: 'Lịch khởi hành', icon: CalendarDays, path: '/admin/tour-schedules' },
  { key: 'faqs', label: 'FAQ tour', icon: FileQuestion, path: '/admin/tour-faqs' },
  { key: 'reviews', label: 'Đánh giá tour', icon: Star, path: '/admin/tour-reviews' },
];

const AdminTourSecondaryNav = ({ currentKey = 'tours' }) => (
  <div className="flex flex-wrap gap-3">
    {NAV_ITEMS.map((item) => {
      const Icon = item.icon;

      return (
        <NavLink
          key={item.key}
          to={item.path}
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

export default AdminTourSecondaryNav;
