import React from 'react';
import HotelPoliciesPage from '../../tenant/hotel/pages/HotelPoliciesPage';
import useAdminHotelScope from '../hotel/hooks/useAdminHotelScope';

export default function AdminHotelPoliciesPage() {
  const adminScope = useAdminHotelScope();
  return <HotelPoliciesPage mode="admin" adminScope={adminScope} />;
}
