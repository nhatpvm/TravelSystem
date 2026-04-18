import React from 'react';
import FlightInventoryPage from '../../tenant/inventory/pages/FlightInventoryPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightInventoryPage() {
  const adminScope = useAdminFlightScope();
  return <FlightInventoryPage mode="admin" adminScope={adminScope} />;
}
