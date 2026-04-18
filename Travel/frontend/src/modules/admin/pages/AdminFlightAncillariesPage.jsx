import React from 'react';
import FlightAncillariesPage from '../../tenant/flight/pages/FlightAncillariesPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightAncillariesPage() {
  const adminScope = useAdminFlightScope();
  return <FlightAncillariesPage mode="admin" adminScope={adminScope} />;
}
