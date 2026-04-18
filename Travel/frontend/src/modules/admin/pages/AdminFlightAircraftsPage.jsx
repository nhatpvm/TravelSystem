import React from 'react';
import FlightAircraftsPage from '../../tenant/flight/pages/FlightAircraftsPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightAircraftsPage() {
  const adminScope = useAdminFlightScope();
  return <FlightAircraftsPage mode="admin" adminScope={adminScope} />;
}
