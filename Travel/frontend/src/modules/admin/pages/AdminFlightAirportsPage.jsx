import React from 'react';
import FlightAirportsPage from '../../tenant/flight/pages/FlightAirportsPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightAirportsPage() {
  const adminScope = useAdminFlightScope();
  return <FlightAirportsPage mode="admin" adminScope={adminScope} />;
}
