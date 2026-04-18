import React from 'react';
import HotelInventoryPage from '../../tenant/inventory/pages/HotelInventoryPage';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';

export default function AdminHotelInventoryPage() {
  const adminScope = useAdminHotelScope();
  return <HotelInventoryPage mode="admin" adminScope={adminScope} />;
}
