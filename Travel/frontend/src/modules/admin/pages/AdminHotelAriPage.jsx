import React from 'react';
import HotelARIPage from '../../tenant/inventory/pages/HotelARIPage';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';

export default function AdminHotelAriPage() {
  const adminScope = useAdminHotelScope();
  return <HotelARIPage mode="admin" adminScope={adminScope} />;
}
