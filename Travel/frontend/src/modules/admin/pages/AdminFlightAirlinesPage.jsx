import React from 'react';
import FlightAirlinesPage from '../../tenant/flight/pages/FlightAirlinesPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightAirlinesPage() {
  const adminScope = useAdminFlightScope();
  return <FlightAirlinesPage mode="admin" adminScope={adminScope} />;
}
