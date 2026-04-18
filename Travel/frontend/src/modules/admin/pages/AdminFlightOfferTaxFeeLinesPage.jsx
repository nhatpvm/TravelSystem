import React from 'react';
import FlightOfferTaxFeeLinesPage from '../../tenant/flight/pages/FlightOfferTaxFeeLinesPage';
import useAdminFlightScope from '../flight/hooks/useAdminFlightScope';

export default function AdminFlightOfferTaxFeeLinesPage() {
  const adminScope = useAdminFlightScope();
  return <FlightOfferTaxFeeLinesPage mode="admin" adminScope={adminScope} />;
}
