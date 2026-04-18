import React from 'react';
import FlightFareRulesPage from '../../tenant/flight/pages/FlightFareRulesPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightFareRulesPage() {
  const adminScope = useAdminFlightScope();
  return <FlightFareRulesPage mode="admin" adminScope={adminScope} />;
}
