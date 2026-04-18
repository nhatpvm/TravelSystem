import React from 'react';
import FlightCabinSeatMapsPage from '../../tenant/flight/pages/FlightCabinSeatMapsPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightCabinSeatMapsPage() {
  const adminScope = useAdminFlightScope();
  return <FlightCabinSeatMapsPage mode="admin" adminScope={adminScope} />;
}
