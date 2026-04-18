import React from 'react';
import FlightFlightsPage from '../../tenant/flight/pages/FlightFlightsPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightFlightsPage() {
  const adminScope = useAdminFlightScope();
  return <FlightFlightsPage mode="admin" adminScope={adminScope} />;
}
