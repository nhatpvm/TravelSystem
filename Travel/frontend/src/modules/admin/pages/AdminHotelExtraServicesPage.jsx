import React from 'react';
import HotelExtraServicesPage from '../../tenant/hotel/pages/HotelExtraServicesPage';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';

export default function AdminHotelExtraServicesPage() {
  const adminScope = useAdminHotelScope();
  return <HotelExtraServicesPage mode="admin" adminScope={adminScope} />;
}
