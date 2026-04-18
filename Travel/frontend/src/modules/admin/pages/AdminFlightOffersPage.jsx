import React from 'react';
import FlightOffersPage from '../../tenant/flight/pages/FlightOffersPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightOffersPage() {
  const adminScope = useAdminFlightScope();
  return <FlightOffersPage mode="admin" adminScope={adminScope} />;
}
