import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import ScrollToTop from './ScrollToTop';
import HomePage from '../modules/home/pages/HomePage';
import HomeTwoPage from '../modules/home/pages/HomeTwoPage';
import HomeThreePage from '../modules/home/pages/HomeThreePage';
import PromotionsPage from '../modules/home/pages/PromotionsPage';
import BusResultsPage from '../modules/home/pages/BusResultsPage';
import BusSeatSelectionPage from '../modules/home/pages/BusSeatSelectionPage';
import FlightResultsPage from '../modules/home/pages/FlightResultsPage';
import TrainResultsPage from '../modules/home/pages/TrainResultsPage';
import HotelResultsPage from '../modules/home/pages/HotelResultsPage';
import CheckoutPage from '../modules/booking/pages/CheckoutPage';
import PaymentPage from '../modules/booking/pages/PaymentPage';
import TicketPage from '../modules/booking/pages/TicketPage';
import AboutPage from '../modules/about/pages/AboutPage';
import DestinationsPage from '../modules/destinations/pages/DestinationsPage';
import DestinationDetailsPage from '../modules/destinations/pages/DestinationDetailsPage';
import OurTourPage from '../modules/tours/pages/OurTourPage';
import TourDetailsPage from '../modules/tours/pages/TourDetailsPage';
import ActivitiesPage from '../modules/activities/pages/ActivitiesPage';
import ActivityDetailsPage from '../modules/activities/pages/ActivityDetailsPage';
import OurTeamPage from '../modules/team/pages/OurTeamPage';
import TeamDetailsPage from '../modules/team/pages/TeamDetailsPage';
import FAQPage from '../modules/home/pages/FAQPage';
import BlogGridPage from '../modules/blog/pages/BlogGridPage';
import BlogClassicPage from '../modules/blog/pages/BlogClassicPage';
import BlogDetailsPage from '../modules/blog/pages/BlogDetailsPage';
import ContactPage from '../modules/contact/pages/ContactPage';
import LoginPage from '../modules/auth/pages/LoginPage';
import RegisterPage from '../modules/auth/pages/RegisterPage';
import ForgotPasswordPage from '../modules/auth/pages/ForgotPasswordPage';
import ResetPasswordPage from '../modules/auth/pages/ResetPasswordPage';
import RequireAuth from '../modules/auth/components/RequireAuth';
import BusTripDetailPage from '../modules/home/pages/BusTripDetailPage';
import HotelDetailPage from '../modules/home/pages/HotelDetailPage';
import FlightDetailPage from '../modules/home/pages/FlightDetailPage';
import TourDetailPage from '../modules/home/pages/TourDetailPage';
import TourResultsPage from '../modules/home/pages/TourResultsPage';
import TrainDetailPage from '../modules/home/pages/TrainDetailPage';
import TrainSeatSelectionPage from '../modules/home/pages/TrainSeatSelectionPage';
import NotFoundPage from '../modules/home/pages/NotFoundPage';
import SupportPage from '../modules/home/pages/SupportPage';
import PrivacyPolicyPage from '../modules/home/pages/PrivacyPolicyPage';
import TermsPage from '../modules/home/pages/TermsPage';
import SavedPassengersPage from '../modules/user/pages/SavedPassengersPage';
import ProfilePage from '../modules/user/pages/ProfilePage';
import MyBookingsPage from '../modules/user/pages/MyBookingsPage';
import BookingDetailPage from '../modules/user/pages/BookingDetailPage';
import CancelBookingPage from '../modules/user/pages/CancelBookingPage';
import PaymentsPage from '../modules/user/pages/PaymentsPage';
import SecurityPage from '../modules/user/pages/SecurityPage';
import WishlistPage from '../modules/user/pages/WishlistPage';
import NotificationsPage from '../modules/user/pages/NotificationsPage';
import PaymentHistoryPage from '../modules/user/pages/PaymentHistoryPage';
import UserSettingsPage from '../modules/user/pages/UserSettingsPage';
import AdminLayout from '../shared/components/layouts/AdminLayout';
import AdminDashboard from '../modules/admin/dashboard/pages/AdminDashboard';
import AdminFinancePage from '../modules/admin/finance/pages/AdminFinancePage';
import AdminTenantsPage from '../modules/admin/pages/AdminTenantsPage';
import AdminCMSPage from '../modules/admin/pages/AdminCMSPage';
import AdminUsersPage from '../modules/admin/pages/AdminUsersPage';
import AdminRolesPage from '../modules/admin/pages/AdminRolesPage';
import AdminPermissionsPage from '../modules/admin/pages/AdminPermissionsPage';
import AdminRolePermissionsPage from '../modules/admin/pages/AdminRolePermissionsPage';
import AdminUserPermissionsPage from '../modules/admin/pages/AdminUserPermissionsPage';
import AdminBookingsPage from '../modules/admin/pages/AdminBookingsPage';
import UserLayout from '../shared/components/layouts/UserLayout';
import TenantLayout from '../shared/components/layouts/TenantLayout';
import TenantDashboard from '../modules/tenant/dashboard/pages/TenantDashboard';
import TenantOnboardingPage from '../modules/tenant/pages/TenantOnboardingPage';
import BusInventoryPage from '../modules/tenant/inventory/pages/BusInventoryPage';
import HotelARIPage from '../modules/tenant/inventory/pages/HotelARIPage';
import TourInventoryPage from '../modules/tenant/inventory/pages/TourInventoryPage';
import TenantBookingsPage from '../modules/tenant/pages/TenantBookingsPage';
import StaffManagementPage from '../modules/tenant/pages/StaffManagementPage';
import PartnerFinancePage from '../modules/tenant/pages/PartnerFinancePage';
import TenantSettingsPage from '../modules/tenant/pages/TenantSettingsPage';
import TenantReportsPage from '../modules/tenant/pages/TenantReportsPage';
import TenantReviewsPage from '../modules/tenant/pages/TenantReviewsPage';
import TrainInventoryPage from '../modules/tenant/inventory/pages/TrainInventoryPage';
import TrainOperationsPage from '../modules/tenant/pages/TrainOperationsPage';
import TrainProvidersPage from '../modules/tenant/pages/TrainProvidersPage';
import FlightInventoryPage from '../modules/tenant/inventory/pages/FlightInventoryPage';
import FlightOperationsPage from '../modules/tenant/pages/FlightOperationsPage';
import FlightProvidersPage from '../modules/tenant/pages/FlightProvidersPage';
import HotelInventoryPage from '../modules/tenant/inventory/pages/HotelInventoryPage';
import BusOperationsPage from '../modules/tenant/pages/BusOperationsPage';
import AdminRefundsPage from '../modules/admin/pages/AdminRefundsPage';
import AdminPromoPage from '../modules/admin/pages/AdminPromoPage';
import AdminSupportPage from '../modules/admin/pages/AdminSupportPage';
import AdminPaymentsPage from '../modules/admin/pages/AdminPaymentsPage';
import AdminSettlementPage from '../modules/admin/pages/AdminSettlementPage';
import AdminAuditPage from '../modules/admin/pages/AdminAuditPage';
import AdminOutboxPage from '../modules/admin/pages/AdminOutboxPage';
import AdminNotificationsPage from '../modules/admin/pages/AdminNotificationsPage';
import AdminMasterDataPage from '../modules/admin/pages/AdminMasterDataPage';
import AdminLocationsPage from '../modules/admin/pages/AdminLocationsPage';
import AdminProvidersPage from '../modules/admin/pages/AdminProvidersPage';
import AdminGeoSyncPage from '../modules/admin/pages/AdminGeoSyncPage';
import AdminGeoSyncLogsPage from '../modules/admin/pages/AdminGeoSyncLogsPage';
import AdminVehicleModelsPage from '../modules/admin/pages/AdminVehicleModelsPage';
import AdminVehiclesPage from '../modules/admin/pages/AdminVehiclesPage';
import AdminSeatMapsPage from '../modules/admin/pages/AdminSeatMapsPage';
import AdminSeatsPage from '../modules/admin/pages/AdminSeatsPage';
import VATInvoicePage from '../modules/user/pages/VATInvoicePage';
import BlogTagPage from '../modules/home/pages/BlogTagPage';
import ServerErrorPage from '../modules/home/pages/ServerErrorPage';
import FlightSeatSelectionPage from '../modules/home/pages/FlightSeatSelectionPage';
import BusProvidersPage from '../modules/tenant/pages/BusProvidersPage';
import TenantCMSPage from '../modules/tenant/pages/TenantCMSPage';
import BusStopPointsPage from '../modules/tenant/bus/pages/BusStopPointsPage';
import BusRoutesPage from '../modules/tenant/bus/pages/BusRoutesPage';
import BusTripStopTimesPage from '../modules/tenant/bus/pages/BusTripStopTimesPage';
import BusTripStopPointsPage from '../modules/tenant/bus/pages/BusTripStopPointsPage';
import BusTripSegmentPricesPage from '../modules/tenant/bus/pages/BusTripSegmentPricesPage';
import BusVehicleDetailsPage from '../modules/tenant/bus/pages/BusVehicleDetailsPage';
import BusTripSeatsPage from '../modules/tenant/bus/pages/BusTripSeatsPage';
import BusSeatHoldsPage from '../modules/tenant/bus/pages/BusSeatHoldsPage';
import TrainStopPointsPage from '../modules/tenant/train/pages/TrainStopPointsPage';
import TrainRoutesPage from '../modules/tenant/train/pages/TrainRoutesPage';
import TrainTripStopTimesPage from '../modules/tenant/train/pages/TrainTripStopTimesPage';
import TrainTripSegmentPricesPage from '../modules/tenant/train/pages/TrainTripSegmentPricesPage';
import TrainCarsPage from '../modules/tenant/train/pages/TrainCarsPage';
import TrainCarSeatsPage from '../modules/tenant/train/pages/TrainCarSeatsPage';
import TrainTripSeatsPage from '../modules/tenant/train/pages/TrainTripSeatsPage';
import TrainSeatHoldsPage from '../modules/tenant/train/pages/TrainSeatHoldsPage';
import FlightAirlinesPage from '../modules/tenant/flight/pages/FlightAirlinesPage';
import FlightAirportsPage from '../modules/tenant/flight/pages/FlightAirportsPage';
import FlightAircraftModelsPage from '../modules/tenant/flight/pages/FlightAircraftModelsPage';
import FlightAircraftsPage from '../modules/tenant/flight/pages/FlightAircraftsPage';
import FlightFareClassesPage from '../modules/tenant/flight/pages/FlightFareClassesPage';
import FlightFareRulesPage from '../modules/tenant/flight/pages/FlightFareRulesPage';
import FlightFlightsPage from '../modules/tenant/flight/pages/FlightFlightsPage';
import FlightOffersPage from '../modules/tenant/flight/pages/FlightOffersPage';
import FlightOfferTaxFeeLinesPage from '../modules/tenant/flight/pages/FlightOfferTaxFeeLinesPage';
import FlightCabinSeatMapsPage from '../modules/tenant/flight/pages/FlightCabinSeatMapsPage';
import FlightCabinSeatsPage from '../modules/tenant/flight/pages/FlightCabinSeatsPage';
import FlightAncillariesPage from '../modules/tenant/flight/pages/FlightAncillariesPage';
import CmsMediaPage from '../modules/cms/pages/CmsMediaPage';
import CmsCategoriesPage from '../modules/cms/pages/CmsCategoriesPage';
import CmsTagsPage from '../modules/cms/pages/CmsTagsPage';
import CmsRevisionsPage from '../modules/cms/pages/CmsRevisionsPage';
import CmsPreviewPage from '../modules/cms/pages/CmsPreviewPage';
import CmsSeoAuditPage from '../modules/cms/pages/CmsSeoAuditPage';
import CmsSiteSettingsPage from '../modules/cms/pages/CmsSiteSettingsPage';
import TourSchedulesPage from '../modules/tenant/tour/pages/TourSchedulesPage';
import TourPricingPage from '../modules/tenant/tour/pages/TourPricingPage';
import TourCapacityPage from '../modules/tenant/tour/pages/TourCapacityPage';
import TourPackagesPage from '../modules/tenant/tour/pages/TourPackagesPage';
import TourContentPage from '../modules/tenant/tour/pages/TourContentPage';
import TourExperiencePage from '../modules/tenant/tour/pages/TourExperiencePage';
import TourPackageBuilderPage from '../modules/tenant/tour/pages/TourPackageBuilderPage';
import TourPackageReportingPage from '../modules/tenant/tour/pages/TourPackageReportingPage';
import AdminToursPage from '../modules/admin/pages/AdminToursPage';
import AdminTourReviewsPage from '../modules/admin/pages/AdminTourReviewsPage';
import AdminTourSchedulesPage from '../modules/admin/pages/AdminTourSchedulesPage';
import AdminTourFaqsPage from '../modules/admin/pages/AdminTourFaqsPage';
import AdminTrainInventoryPage from '../modules/admin/pages/AdminTrainInventoryPage';
import AdminTrainStopPointsPage from '../modules/admin/pages/AdminTrainStopPointsPage';
import AdminTrainRoutesPage from '../modules/admin/pages/AdminTrainRoutesPage';
import AdminTrainTripStopTimesPage from '../modules/admin/pages/AdminTrainTripStopTimesPage';
import AdminTrainTripSegmentPricesPage from '../modules/admin/pages/AdminTrainTripSegmentPricesPage';
import AdminTrainCarsPage from '../modules/admin/pages/AdminTrainCarsPage';
import AdminTrainCarSeatsPage from '../modules/admin/pages/AdminTrainCarSeatsPage';
import AdminFlightInventoryPage from '../modules/admin/pages/AdminFlightInventoryPage';
import AdminFlightAirlinesPage from '../modules/admin/pages/AdminFlightAirlinesPage';
import AdminFlightAirportsPage from '../modules/admin/pages/AdminFlightAirportsPage';
import AdminFlightAircraftModelsPage from '../modules/admin/pages/AdminFlightAircraftModelsPage';
import AdminFlightAircraftsPage from '../modules/admin/pages/AdminFlightAircraftsPage';
import AdminFlightFareClassesPage from '../modules/admin/pages/AdminFlightFareClassesPage';
import AdminFlightFareRulesPage from '../modules/admin/pages/AdminFlightFareRulesPage';
import AdminFlightFlightsPage from '../modules/admin/pages/AdminFlightFlightsPage';
import AdminFlightOffersPage from '../modules/admin/pages/AdminFlightOffersPage';
import AdminFlightOfferTaxFeeLinesPage from '../modules/admin/pages/AdminFlightOfferTaxFeeLinesPage';
import AdminFlightCabinSeatMapsPage from '../modules/admin/pages/AdminFlightCabinSeatMapsPage';
import AdminFlightCabinSeatsPage from '../modules/admin/pages/AdminFlightCabinSeatsPage';
import AdminFlightAncillariesPage from '../modules/admin/pages/AdminFlightAncillariesPage';
import HotelRoomTypesPage from '../modules/tenant/hotel/pages/HotelRoomTypesPage';
import HotelRatePlansPage from '../modules/tenant/hotel/pages/HotelRatePlansPage';
import HotelPoliciesPage from '../modules/tenant/hotel/pages/HotelPoliciesPage';
import HotelExtraServicesPage from '../modules/tenant/hotel/pages/HotelExtraServicesPage';
import AdminHotelInventoryPage from '../modules/admin/pages/AdminHotelInventoryPage';
import AdminHotelAriPage from '../modules/admin/pages/AdminHotelAriPage';
import AdminHotelRoomTypesPage from '../modules/admin/pages/AdminHotelRoomTypesPage';
import AdminHotelRatePlansPage from '../modules/admin/pages/AdminHotelRatePlansPage';
import AdminHotelPoliciesPage from '../modules/admin/pages/AdminHotelPoliciesPage';
import AdminHotelExtraServicesPage from '../modules/admin/pages/AdminHotelExtraServicesPage';
import AdminHotelContactsPage from '../modules/admin/pages/AdminHotelContactsPage';
import AdminHotelImagesPage from '../modules/admin/pages/AdminHotelImagesPage';
import AdminHotelAmenitiesPage from '../modules/admin/pages/AdminHotelAmenitiesPage';
import AdminRoomAmenitiesPage from '../modules/admin/pages/AdminRoomAmenitiesPage';
import AdminMealPlansPage from '../modules/admin/pages/AdminMealPlansPage';
import AdminBedTypesPage from '../modules/admin/pages/AdminBedTypesPage';
import AdminRoomTypeImagesPage from '../modules/admin/pages/AdminRoomTypeImagesPage';
import AdminRoomTypePoliciesPage from '../modules/admin/pages/AdminRoomTypePoliciesPage';
import AdminHotelPromoOverridesPage from '../modules/admin/pages/AdminHotelPromoOverridesPage';
import AdminHotelReviewsPage from '../modules/admin/pages/AdminHotelReviewsPage';
import CustomerPreferenceBootstrap from '../modules/user/components/CustomerPreferenceBootstrap';

function App() {
  return (
    <BrowserRouter>
      <ScrollToTop />
      <CustomerPreferenceBootstrap />
      <div className="App">
        <Routes>
          {/* Customer Portal Routes */}
          <Route path="/" element={<HomePage />} />
          <Route path="/home-2" element={<HomeTwoPage />} />
          <Route path="/home-3" element={<HomeThreePage />} />
          <Route path="/promotions" element={<PromotionsPage />} />
          <Route path="/bus/results" element={<BusResultsPage />} />
          <Route path="/bus/seat-selection" element={<BusSeatSelectionPage />} />
          <Route path="/flight/results" element={<FlightResultsPage />} />
          <Route path="/train/results" element={<TrainResultsPage />} />
          <Route path="/hotel/results" element={<HotelResultsPage />} />
          <Route path="/checkout" element={<CheckoutPage />} />
          <Route path="/payment" element={<PaymentPage />} />
          <Route path="/ticket/success" element={<TicketPage />} />
          <Route path="/about" element={<AboutPage />} />
          <Route path="/destinations" element={<DestinationsPage />} />
          <Route path="/destinations/details" element={<DestinationDetailsPage />} />
          <Route path="/tours" element={<OurTourPage />} />
          <Route path="/tours/details" element={<TourDetailsPage />} />
          <Route path="/activities" element={<ActivitiesPage />} />
          <Route path="/activities/details" element={<ActivityDetailsPage />} />
          <Route path="/team" element={<OurTeamPage />} />
          <Route path="/team/details" element={<TeamDetailsPage />} />
          <Route path="/faq" element={<FAQPage />} />
          <Route path="/blog" element={<BlogGridPage />} />
          <Route path="/blog/classic" element={<BlogClassicPage />} />
          <Route path="/blog/details" element={<BlogDetailsPage />} />
          <Route path="/blog/:slug" element={<BlogDetailsPage />} />
          <Route path="/contact" element={<ContactPage />} />

          {/* Auth Routes */}
          <Route path="/auth/login" element={<LoginPage />} />
          <Route path="/auth/register" element={<RegisterPage />} />
          <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/auth/reset-password" element={<ResetPasswordPage />} />

          {/* Product Detail Routes */}
          <Route path="/bus/trip/:id" element={<BusTripDetailPage />} />
          <Route path="/hotel/:id" element={<HotelDetailPage />} />
          <Route path="/flight/detail" element={<FlightDetailPage />} />
          <Route path="/tour/:id" element={<TourDetailPage />} />
          <Route path="/tour/results" element={<TourResultsPage />} />
          <Route path="/train/details" element={<TrainDetailPage />} />
          <Route path="/train/seat-selection" element={<TrainSeatSelectionPage />} />
          <Route path="/support" element={<SupportPage />} />
          <Route path="/privacy" element={<PrivacyPolicyPage />} />
          <Route path="/terms" element={<TermsPage />} />
          <Route path="/500" element={<ServerErrorPage />} />
          <Route path="/blog/tag/:tag" element={<BlogTagPage />} />
          <Route path="/blog/category/:cat" element={<BlogTagPage />} />
          <Route path="/flight/seat-selection" element={<FlightSeatSelectionPage />} />

          {/* User Account Routes */}
          <Route path="/my-account" element={<RequireAuth><UserLayout /></RequireAuth>}>
            <Route path="profile" element={<ProfilePage />} />
            <Route path="bookings" element={<MyBookingsPage />} />
            <Route path="bookings/:id" element={<BookingDetailPage />} />
            <Route path="bookings/:id/cancel" element={<CancelBookingPage />} />
            <Route path="passengers" element={<SavedPassengersPage />} />
            <Route path="payments" element={<PaymentsPage />} />
            <Route path="payment-history" element={<PaymentHistoryPage />} />
            <Route path="security" element={<SecurityPage />} />
            <Route path="wishlist" element={<WishlistPage />} />
            <Route path="notifications" element={<NotificationsPage />} />
            <Route path="settings" element={<UserSettingsPage />} />
            <Route path="vat-invoice" element={<VATInvoicePage />} />
          </Route>

          {/* Tenant Portal Routes */}
          <Route path="/tenant" element={<RequireAuth access="tenant"><TenantLayout /></RequireAuth>}>
            <Route index element={<RequireAuth access="tenant" tenantPermission="tenant.dashboard.read"><TenantDashboard /></RequireAuth>} />
            <Route path="inventory/bus" element={<RequireAuth access="tenant" tenantModule="bus"><BusInventoryPage /></RequireAuth>} />
            <Route path="inventory/hotel" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelInventoryPage /></RequireAuth>} />
            <Route path="inventory/tour" element={<RequireAuth access="tenant" tenantModule="tour"><TourInventoryPage /></RequireAuth>} />
            <Route path="inventory/train" element={<RequireAuth access="tenant" tenantModule="train"><TrainInventoryPage /></RequireAuth>} />
            <Route path="inventory/flight" element={<RequireAuth access="tenant" tenantModule="flight"><FlightInventoryPage /></RequireAuth>} />
            <Route path="inventory/hotel-full" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelInventoryPage /></RequireAuth>} />
            <Route path="operations/hotel" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelRoomTypesPage /></RequireAuth>} />
            <Route path="operations/hotel/room-types" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelRoomTypesPage /></RequireAuth>} />
            <Route path="operations/hotel/rate-plans" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelRatePlansPage /></RequireAuth>} />
            <Route path="operations/hotel/policies" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelPoliciesPage /></RequireAuth>} />
            <Route path="providers/hotel" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelExtraServicesPage /></RequireAuth>} />
            <Route path="providers/hotel/extra-services" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelExtraServicesPage /></RequireAuth>} />
            <Route path="providers/hotel/ari" element={<RequireAuth access="tenant" tenantModule="hotel"><HotelARIPage /></RequireAuth>} />
            <Route path="operations/bus" element={<RequireAuth access="tenant" tenantModule="bus"><BusOperationsPage /></RequireAuth>} />
            <Route path="operations/bus/stop-points" element={<RequireAuth access="tenant" tenantModule="bus"><BusStopPointsPage /></RequireAuth>} />
            <Route path="operations/bus/routes" element={<RequireAuth access="tenant" tenantModule="bus"><BusRoutesPage /></RequireAuth>} />
            <Route path="operations/bus/trip-stop-times" element={<RequireAuth access="tenant" tenantModule="bus"><BusTripStopTimesPage /></RequireAuth>} />
            <Route path="operations/bus/trip-stop-points" element={<RequireAuth access="tenant" tenantModule="bus"><BusTripStopPointsPage /></RequireAuth>} />
            <Route path="operations/bus/trip-segment-prices" element={<RequireAuth access="tenant" tenantModule="bus"><BusTripSegmentPricesPage /></RequireAuth>} />
            <Route path="operations/train" element={<RequireAuth access="tenant" tenantModule="train"><TrainOperationsPage /></RequireAuth>} />
            <Route path="operations/train/stop-points" element={<RequireAuth access="tenant" tenantModule="train"><TrainStopPointsPage /></RequireAuth>} />
            <Route path="operations/train/routes" element={<RequireAuth access="tenant" tenantModule="train"><TrainRoutesPage /></RequireAuth>} />
            <Route path="operations/train/trip-stop-times" element={<RequireAuth access="tenant" tenantModule="train"><TrainTripStopTimesPage /></RequireAuth>} />
            <Route path="operations/train/trip-segment-prices" element={<RequireAuth access="tenant" tenantModule="train"><TrainTripSegmentPricesPage /></RequireAuth>} />
            <Route path="operations/flight" element={<RequireAuth access="tenant" tenantModule="flight"><FlightOperationsPage /></RequireAuth>} />
            <Route path="operations/flight/airlines" element={<RequireAuth access="tenant" tenantModule="flight"><FlightAirlinesPage /></RequireAuth>} />
            <Route path="operations/flight/airports" element={<RequireAuth access="tenant" tenantModule="flight"><FlightAirportsPage /></RequireAuth>} />
            <Route path="operations/flight/fare-classes" element={<RequireAuth access="tenant" tenantModule="flight"><FlightFareClassesPage /></RequireAuth>} />
            <Route path="operations/flight/fare-rules" element={<RequireAuth access="tenant" tenantModule="flight"><FlightFareRulesPage /></RequireAuth>} />
            <Route path="operations/flight/flights" element={<RequireAuth access="tenant" tenantModule="flight"><FlightFlightsPage /></RequireAuth>} />
            <Route path="operations/flight/offers" element={<RequireAuth access="tenant" tenantModule="flight"><FlightOffersPage /></RequireAuth>} />
            <Route path="operations/flight/tax-fee-lines" element={<RequireAuth access="tenant" tenantModule="flight"><FlightOfferTaxFeeLinesPage /></RequireAuth>} />
            <Route path="operations/tour/schedules" element={<RequireAuth access="tenant" tenantModule="tour"><TourSchedulesPage /></RequireAuth>} />
            <Route path="operations/tour/pricing" element={<RequireAuth access="tenant" tenantModule="tour"><TourPricingPage /></RequireAuth>} />
            <Route path="operations/tour/capacity" element={<RequireAuth access="tenant" tenantModule="tour"><TourCapacityPage /></RequireAuth>} />
            <Route path="operations/tour/packages" element={<RequireAuth access="tenant" tenantModule="tour"><TourPackagesPage /></RequireAuth>} />
            <Route path="operations/tour/content" element={<RequireAuth access="tenant" tenantModule="tour"><TourContentPage /></RequireAuth>} />
            <Route path="operations/tour/experience" element={<RequireAuth access="tenant" tenantModule="tour"><TourExperiencePage /></RequireAuth>} />
            <Route path="operations/tour/package-builder" element={<RequireAuth access="tenant" tenantModule="tour"><TourPackageBuilderPage /></RequireAuth>} />
            <Route path="operations/tour/reporting" element={<RequireAuth access="tenant" tenantModule="tour"><TourPackageReportingPage /></RequireAuth>} />
            <Route path="providers/bus" element={<RequireAuth access="tenant" tenantModule="bus"><BusProvidersPage /></RequireAuth>} />
            <Route path="providers/bus/vehicle-details" element={<RequireAuth access="tenant" tenantModule="bus"><BusVehicleDetailsPage /></RequireAuth>} />
            <Route path="providers/bus/seats" element={<RequireAuth access="tenant" tenantModule="bus"><BusTripSeatsPage /></RequireAuth>} />
            <Route path="providers/bus/seat-holds" element={<RequireAuth access="tenant" tenantModule="bus"><BusSeatHoldsPage /></RequireAuth>} />
            <Route path="providers/train" element={<RequireAuth access="tenant" tenantModule="train"><TrainProvidersPage /></RequireAuth>} />
            <Route path="providers/train/cars" element={<RequireAuth access="tenant" tenantModule="train"><TrainCarsPage /></RequireAuth>} />
            <Route path="providers/train/car-seats" element={<RequireAuth access="tenant" tenantModule="train"><TrainCarSeatsPage /></RequireAuth>} />
            <Route path="providers/train/seats" element={<RequireAuth access="tenant" tenantModule="train"><TrainTripSeatsPage /></RequireAuth>} />
            <Route path="providers/train/seat-holds" element={<RequireAuth access="tenant" tenantModule="train"><TrainSeatHoldsPage /></RequireAuth>} />
            <Route path="providers/flight" element={<RequireAuth access="tenant" tenantModule="flight"><FlightProvidersPage /></RequireAuth>} />
            <Route path="providers/flight/aircraft-models" element={<RequireAuth access="tenant" tenantModule="flight"><FlightAircraftModelsPage /></RequireAuth>} />
            <Route path="providers/flight/aircrafts" element={<RequireAuth access="tenant" tenantModule="flight"><FlightAircraftsPage /></RequireAuth>} />
            <Route path="providers/flight/seat-maps" element={<RequireAuth access="tenant" tenantModule="flight"><FlightCabinSeatMapsPage /></RequireAuth>} />
            <Route path="providers/flight/seats" element={<RequireAuth access="tenant" tenantModule="flight"><FlightCabinSeatsPage /></RequireAuth>} />
            <Route path="providers/flight/ancillaries" element={<RequireAuth access="tenant" tenantModule="flight"><FlightAncillariesPage /></RequireAuth>} />
            <Route path="cms" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><TenantCMSPage /></RequireAuth>} />
            <Route path="cms/media" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsMediaPage mode="tenant" /></RequireAuth>} />
            <Route path="cms/categories" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsCategoriesPage mode="tenant" /></RequireAuth>} />
            <Route path="cms/tags" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsTagsPage mode="tenant" /></RequireAuth>} />
            <Route path="cms/revisions" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsRevisionsPage mode="tenant" /></RequireAuth>} />
            <Route path="cms/preview" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsPreviewPage mode="tenant" /></RequireAuth>} />
            <Route path="cms/seo-audit" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsSeoAuditPage mode="tenant" /></RequireAuth>} />
            <Route path="cms/site-settings" element={<RequireAuth access="tenant" tenantPermission="cms.posts.read"><CmsSiteSettingsPage mode="tenant" /></RequireAuth>} />
            <Route path="bookings" element={<RequireAuth access="tenant" tenantPermission="tenant.bookings.read"><TenantBookingsPage /></RequireAuth>} />
            <Route path="staff" element={<RequireAuth access="tenant" tenantPermission="tenant.staff.manage"><StaffManagementPage /></RequireAuth>} />
            <Route path="finance" element={<RequireAuth access="tenant" tenantPermission="tenant.finance.read"><PartnerFinancePage /></RequireAuth>} />
            <Route path="settings" element={<RequireAuth access="tenant" tenantPermission="tenant.settings.read"><TenantSettingsPage /></RequireAuth>} />
            <Route path="reports" element={<RequireAuth access="tenant" tenantPermission="tenant.reports.read"><TenantReportsPage /></RequireAuth>} />
            <Route path="reviews" element={<RequireAuth access="tenant" tenantPermission="tenant.reviews.read"><TenantReviewsPage /></RequireAuth>} />
          </Route>

          {/* Admin Portal Routes */}
          <Route path="/admin" element={<RequireAuth access="admin"><AdminLayout /></RequireAuth>}>
            <Route index element={<AdminDashboard />} />
            <Route path="finance" element={<AdminFinancePage />} />
            <Route path="tenants" element={<AdminTenantsPage />} />
            <Route path="cms" element={<AdminCMSPage />} />
            <Route path="cms/media" element={<CmsMediaPage mode="admin" />} />
            <Route path="cms/categories" element={<CmsCategoriesPage mode="admin" />} />
            <Route path="cms/tags" element={<CmsTagsPage mode="admin" />} />
            <Route path="cms/revisions" element={<CmsRevisionsPage mode="admin" />} />
            <Route path="cms/preview" element={<CmsPreviewPage mode="admin" />} />
            <Route path="cms/seo-audit" element={<CmsSeoAuditPage mode="admin" />} />
            <Route path="cms/site-settings" element={<CmsSiteSettingsPage mode="admin" />} />
            <Route path="users" element={<AdminUsersPage />} />
            <Route path="roles" element={<AdminRolesPage />} />
            <Route path="permissions" element={<AdminPermissionsPage />} />
            <Route path="role-permissions" element={<AdminRolePermissionsPage />} />
            <Route path="user-permissions" element={<AdminUserPermissionsPage />} />
            <Route path="bookings" element={<AdminBookingsPage />} />
            <Route path="refunds" element={<AdminRefundsPage />} />
            <Route path="promos" element={<AdminPromoPage />} />
            <Route path="support" element={<AdminSupportPage />} />
            <Route path="payments" element={<AdminPaymentsPage />} />
            <Route path="settlement" element={<AdminSettlementPage />} />
            <Route path="audit" element={<AdminAuditPage />} />
            <Route path="outbox" element={<AdminOutboxPage />} />
            <Route path="notifications" element={<AdminNotificationsPage />} />
            <Route path="master-data" element={<AdminMasterDataPage />} />
            <Route path="master-data/locations" element={<AdminLocationsPage />} />
            <Route path="master-data/providers" element={<AdminProvidersPage />} />
            <Route path="master-data/geo-sync" element={<AdminGeoSyncPage />} />
            <Route path="master-data/geo-sync-logs" element={<AdminGeoSyncLogsPage />} />
            <Route path="master-data/vehicle-models" element={<AdminVehicleModelsPage />} />
            <Route path="master-data/vehicles" element={<AdminVehiclesPage />} />
            <Route path="master-data/seat-maps" element={<AdminSeatMapsPage />} />
            <Route path="master-data/seats" element={<AdminSeatsPage />} />
            <Route path="tours" element={<AdminToursPage />} />
            <Route path="tour-schedules" element={<AdminTourSchedulesPage />} />
            <Route path="tour-faqs" element={<AdminTourFaqsPage />} />
            <Route path="tour-reviews" element={<AdminTourReviewsPage />} />
            <Route path="train" element={<AdminTrainInventoryPage />} />
            <Route path="train/stop-points" element={<AdminTrainStopPointsPage />} />
            <Route path="train/routes" element={<AdminTrainRoutesPage />} />
            <Route path="train/trip-stop-times" element={<AdminTrainTripStopTimesPage />} />
            <Route path="train/trip-segment-prices" element={<AdminTrainTripSegmentPricesPage />} />
            <Route path="train/cars" element={<AdminTrainCarsPage />} />
            <Route path="train/car-seats" element={<AdminTrainCarSeatsPage />} />
            <Route path="flight" element={<AdminFlightInventoryPage />} />
            <Route path="flight/airlines" element={<AdminFlightAirlinesPage />} />
            <Route path="flight/airports" element={<AdminFlightAirportsPage />} />
            <Route path="flight/aircraft-models" element={<AdminFlightAircraftModelsPage />} />
            <Route path="flight/aircrafts" element={<AdminFlightAircraftsPage />} />
            <Route path="flight/fare-classes" element={<AdminFlightFareClassesPage />} />
            <Route path="flight/fare-rules" element={<AdminFlightFareRulesPage />} />
            <Route path="flight/flights" element={<AdminFlightFlightsPage />} />
            <Route path="flight/offers" element={<AdminFlightOffersPage />} />
            <Route path="flight/tax-fee-lines" element={<AdminFlightOfferTaxFeeLinesPage />} />
            <Route path="flight/seat-maps" element={<AdminFlightCabinSeatMapsPage />} />
            <Route path="flight/seats" element={<AdminFlightCabinSeatsPage />} />
            <Route path="flight/ancillaries" element={<AdminFlightAncillariesPage />} />
            <Route path="hotels" element={<AdminHotelInventoryPage />} />
            <Route path="hotels/room-types" element={<AdminHotelRoomTypesPage />} />
            <Route path="hotels/rate-plans" element={<AdminHotelRatePlansPage />} />
            <Route path="hotels/policies" element={<AdminHotelPoliciesPage />} />
            <Route path="hotels/extra-services" element={<AdminHotelExtraServicesPage />} />
            <Route path="hotels/contacts" element={<AdminHotelContactsPage />} />
            <Route path="hotels/images" element={<AdminHotelImagesPage />} />
            <Route path="hotels/amenities" element={<AdminHotelAmenitiesPage />} />
            <Route path="hotels/room-amenities" element={<AdminRoomAmenitiesPage />} />
            <Route path="hotels/meal-plans" element={<AdminMealPlansPage />} />
            <Route path="hotels/bed-types" element={<AdminBedTypesPage />} />
            <Route path="hotels/room-type-images" element={<AdminRoomTypeImagesPage />} />
            <Route path="hotels/room-type-policies" element={<AdminRoomTypePoliciesPage />} />
            <Route path="hotels/promo-overrides" element={<AdminHotelPromoOverridesPage />} />
            <Route path="hotels/reviews" element={<AdminHotelReviewsPage />} />
            <Route path="hotels/ari" element={<AdminHotelAriPage />} />
          </Route>

          <Route path="/tenant/onboarding" element={<TenantOnboardingPage />} />

          {/* 404 catch-all */}
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
