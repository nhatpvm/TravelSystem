import React from 'react';
import HotelRoomTypesPage from '../../tenant/hotel/pages/HotelRoomTypesPage';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';

export default function AdminHotelRoomTypesPage() {
  const adminScope = useAdminHotelScope();
  return <HotelRoomTypesPage mode="admin" adminScope={adminScope} />;
}
