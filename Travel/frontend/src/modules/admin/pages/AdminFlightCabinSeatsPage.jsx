import React from 'react';
import FlightCabinSeatsPage from '../../tenant/flight/pages/FlightCabinSeatsPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightCabinSeatsPage() {
  const adminScope = useAdminFlightScope();
  return <FlightCabinSeatsPage mode="admin" adminScope={adminScope} />;
}
