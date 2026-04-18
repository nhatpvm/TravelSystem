import React from 'react';
import FlightFareClassesPage from '../../tenant/flight/pages/FlightFareClassesPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightFareClassesPage() {
  const adminScope = useAdminFlightScope();
  return <FlightFareClassesPage mode="admin" adminScope={adminScope} />;
}
