import React from 'react';
import FlightAircraftModelsPage from '../../tenant/flight/pages/FlightAircraftModelsPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightAircraftModelsPage() {
  const adminScope = useAdminFlightScope();
  return <FlightAircraftModelsPage mode="admin" adminScope={adminScope} />;
}
