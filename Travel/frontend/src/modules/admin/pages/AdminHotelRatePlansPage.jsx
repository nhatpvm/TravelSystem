import React from 'react';
import HotelRatePlansPage from '../../tenant/hotel/pages/HotelRatePlansPage';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';

export default function AdminHotelRatePlansPage() {
  const adminScope = useAdminHotelScope();
  return <HotelRatePlansPage mode="admin" adminScope={adminScope} />;
}
