import React, { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import ScrollToTop from './ScrollToTop';
import RequireAuth from '../modules/auth/components/RequireAuth';
import AdminLayout from '../shared/components/layouts/AdminLayout';
import UserLayout from '../shared/components/layouts/UserLayout';
import TenantLayout from '../shared/components/layouts/TenantLayout';
import CustomerPreferenceBootstrap from '../modules/user/components/CustomerPreferenceBootstrap';
const HomePage = lazy(() => import('../modules/home/pages/HomePage'));
const HomeTwoPage = lazy(() => import('../modules/home/pages/HomeTwoPage'));
const HomeThreePage = lazy(() => import('../modules/home/pages/HomeThreePage'));
const PromotionsPage = lazy(() => import('../modules/home/pages/PromotionsPage'));
const BusResultsPage = lazy(() => import('../modules/home/pages/BusResultsPage'));
const BusSeatSelectionPage = lazy(() => import('../modules/home/pages/BusSeatSelectionPage'));
const FlightResultsPage = lazy(() => import('../modules/home/pages/FlightResultsPage'));
const TrainResultsPage = lazy(() => import('../modules/home/pages/TrainResultsPage'));
const HotelResultsPage = lazy(() => import('../modules/home/pages/HotelResultsPage'));
const CheckoutPage = lazy(() => import('../modules/booking/pages/CheckoutPage'));
const PaymentPage = lazy(() => import('../modules/booking/pages/PaymentPage'));
const TicketPage = lazy(() => import('../modules/booking/pages/TicketPage'));
const AboutPage = lazy(() => import('../modules/about/pages/AboutPage'));
const DestinationsPage = lazy(() => import('../modules/destinations/pages/DestinationsPage'));
const DestinationDetailsPage = lazy(() => import('../modules/destinations/pages/DestinationDetailsPage'));
const OurTourPage = lazy(() => import('../modules/tours/pages/OurTourPage'));
const TourDetailsPage = lazy(() => import('../modules/tours/pages/TourDetailsPage'));
const ActivitiesPage = lazy(() => import('../modules/activities/pages/ActivitiesPage'));
const ActivityDetailsPage = lazy(() => import('../modules/activities/pages/ActivityDetailsPage'));
const OurTeamPage = lazy(() => import('../modules/team/pages/OurTeamPage'));
const TeamDetailsPage = lazy(() => import('../modules/team/pages/TeamDetailsPage'));
const FAQPage = lazy(() => import('../modules/home/pages/FAQPage'));
const BlogGridPage = lazy(() => import('../modules/blog/pages/BlogGridPage'));
const BlogClassicPage = lazy(() => import('../modules/blog/pages/BlogClassicPage'));
const BlogDetailsPage = lazy(() => import('../modules/blog/pages/BlogDetailsPage'));
const ContactPage = lazy(() => import('../modules/contact/pages/ContactPage'));
const LoginPage = lazy(() => import('../modules/auth/pages/LoginPage'));
const RegisterPage = lazy(() => import('../modules/auth/pages/RegisterPage'));
const ForgotPasswordPage = lazy(() => import('../modules/auth/pages/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('../modules/auth/pages/ResetPasswordPage'));
const BusTripDetailPage = lazy(() => import('../modules/home/pages/BusTripDetailPage'));
const HotelDetailPage = lazy(() => import('../modules/home/pages/HotelDetailPage'));
const FlightDetailPage = lazy(() => import('../modules/home/pages/FlightDetailPage'));
const TourDetailPage = lazy(() => import('../modules/home/pages/TourDetailPage'));
const TourResultsPage = lazy(() => import('../modules/home/pages/TourResultsPage'));
const TrainDetailPage = lazy(() => import('../modules/home/pages/TrainDetailPage'));
const TrainSeatSelectionPage = lazy(() => import('../modules/home/pages/TrainSeatSelectionPage'));
const NotFoundPage = lazy(() => import('../modules/home/pages/NotFoundPage'));
const SupportPage = lazy(() => import('../modules/home/pages/SupportPage'));
const PrivacyPolicyPage = lazy(() => import('../modules/home/pages/PrivacyPolicyPage'));
const TermsPage = lazy(() => import('../modules/home/pages/TermsPage'));
const SavedPassengersPage = lazy(() => import('../modules/user/pages/SavedPassengersPage'));
const ProfilePage = lazy(() => import('../modules/user/pages/ProfilePage'));
const MyBookingsPage = lazy(() => import('../modules/user/pages/MyBookingsPage'));
const BookingDetailPage = lazy(() => import('../modules/user/pages/BookingDetailPage'));
const CancelBookingPage = lazy(() => import('../modules/user/pages/CancelBookingPage'));
const PaymentsPage = lazy(() => import('../modules/user/pages/PaymentsPage'));
const SecurityPage = lazy(() => import('../modules/user/pages/SecurityPage'));
const WishlistPage = lazy(() => import('../modules/user/pages/WishlistPage'));
const NotificationsPage = lazy(() => import('../modules/user/pages/NotificationsPage'));
const PaymentHistoryPage = lazy(() => import('../modules/user/pages/PaymentHistoryPage'));
const UserSettingsPage = lazy(() => import('../modules/user/pages/UserSettingsPage'));
const AdminDashboard = lazy(() => import('../modules/admin/dashboard/pages/AdminDashboard'));
const AdminFinancePage = lazy(() => import('../modules/admin/finance/pages/AdminFinancePage'));
const AdminTenantsPage = lazy(() => import('../modules/admin/pages/AdminTenantsPage'));
const AdminCMSPage = lazy(() => import('../modules/admin/pages/AdminCMSPage'));
const AdminUsersPage = lazy(() => import('../modules/admin/pages/AdminUsersPage'));
const AdminRolesPage = lazy(() => import('../modules/admin/pages/AdminRolesPage'));
const AdminPermissionsPage = lazy(() => import('../modules/admin/pages/AdminPermissionsPage'));
const AdminRolePermissionsPage = lazy(() => import('../modules/admin/pages/AdminRolePermissionsPage'));
const AdminUserPermissionsPage = lazy(() => import('../modules/admin/pages/AdminUserPermissionsPage'));
const AdminBookingsPage = lazy(() => import('../modules/admin/pages/AdminBookingsPage'));
const TenantDashboard = lazy(() => import('../modules/tenant/dashboard/pages/TenantDashboard'));
const TenantOnboardingPage = lazy(() => import('../modules/tenant/pages/TenantOnboardingPage'));
const BusInventoryPage = lazy(() => import('../modules/tenant/inventory/pages/BusInventoryPage'));
const HotelARIPage = lazy(() => import('../modules/tenant/inventory/pages/HotelARIPage'));
const TourInventoryPage = lazy(() => import('../modules/tenant/inventory/pages/TourInventoryPage'));
const TenantBookingsPage = lazy(() => import('../modules/tenant/pages/TenantBookingsPage'));
const StaffManagementPage = lazy(() => import('../modules/tenant/pages/StaffManagementPage'));
const PartnerFinancePage = lazy(() => import('../modules/tenant/pages/PartnerFinancePage'));
const TenantSettingsPage = lazy(() => import('../modules/tenant/pages/TenantSettingsPage'));
const TenantReportsPage = lazy(() => import('../modules/tenant/pages/TenantReportsPage'));
const TenantReviewsPage = lazy(() => import('../modules/tenant/pages/TenantReviewsPage'));
const TenantPromosPage = lazy(() => import('../modules/tenant/pages/TenantPromosPage'));
const TrainInventoryPage = lazy(() => import('../modules/tenant/inventory/pages/TrainInventoryPage'));
const TrainOperationsPage = lazy(() => import('../modules/tenant/pages/TrainOperationsPage'));
const TrainProvidersPage = lazy(() => import('../modules/tenant/pages/TrainProvidersPage'));
const FlightInventoryPage = lazy(() => import('../modules/tenant/inventory/pages/FlightInventoryPage'));
const FlightOperationsPage = lazy(() => import('../modules/tenant/pages/FlightOperationsPage'));
const FlightProvidersPage = lazy(() => import('../modules/tenant/pages/FlightProvidersPage'));
const HotelInventoryPage = lazy(() => import('../modules/tenant/inventory/pages/HotelInventoryPage'));
const BusOperationsPage = lazy(() => import('../modules/tenant/pages/BusOperationsPage'));
const AdminRefundsPage = lazy(() => import('../modules/admin/pages/AdminRefundsPage'));
const AdminPromoPage = lazy(() => import('../modules/admin/pages/AdminPromoPage'));
const AdminSupportPage = lazy(() => import('../modules/admin/pages/AdminSupportPage'));
const AdminPaymentsPage = lazy(() => import('../modules/admin/pages/AdminPaymentsPage'));
const AdminSettlementPage = lazy(() => import('../modules/admin/pages/AdminSettlementPage'));
const AdminAuditPage = lazy(() => import('../modules/admin/pages/AdminAuditPage'));
const AdminOutboxPage = lazy(() => import('../modules/admin/pages/AdminOutboxPage'));
const AdminNotificationsPage = lazy(() => import('../modules/admin/pages/AdminNotificationsPage'));
const AdminMasterDataPage = lazy(() => import('../modules/admin/pages/AdminMasterDataPage'));
const AdminLocationsPage = lazy(() => import('../modules/admin/pages/AdminLocationsPage'));
const AdminProvidersPage = lazy(() => import('../modules/admin/pages/AdminProvidersPage'));
const AdminGeoSyncPage = lazy(() => import('../modules/admin/pages/AdminGeoSyncPage'));
const AdminGeoSyncLogsPage = lazy(() => import('../modules/admin/pages/AdminGeoSyncLogsPage'));
const AdminVehicleModelsPage = lazy(() => import('../modules/admin/pages/AdminVehicleModelsPage'));
const AdminVehiclesPage = lazy(() => import('../modules/admin/pages/AdminVehiclesPage'));
const AdminSeatMapsPage = lazy(() => import('../modules/admin/pages/AdminSeatMapsPage'));
const AdminSeatsPage = lazy(() => import('../modules/admin/pages/AdminSeatsPage'));
const VATInvoicePage = lazy(() => import('../modules/user/pages/VATInvoicePage'));
const BlogTagPage = lazy(() => import('../modules/home/pages/BlogTagPage'));
const ServerErrorPage = lazy(() => import('../modules/home/pages/ServerErrorPage'));
const FlightSeatSelectionPage = lazy(() => import('../modules/home/pages/FlightSeatSelectionPage'));
const BusProvidersPage = lazy(() => import('../modules/tenant/pages/BusProvidersPage'));
const TenantCMSPage = lazy(() => import('../modules/tenant/pages/TenantCMSPage'));
const BusStopPointsPage = lazy(() => import('../modules/tenant/bus/pages/BusStopPointsPage'));
const BusRoutesPage = lazy(() => import('../modules/tenant/bus/pages/BusRoutesPage'));
const BusTripStopTimesPage = lazy(() => import('../modules/tenant/bus/pages/BusTripStopTimesPage'));
const BusTripStopPointsPage = lazy(() => import('../modules/tenant/bus/pages/BusTripStopPointsPage'));
const BusTripSegmentPricesPage = lazy(() => import('../modules/tenant/bus/pages/BusTripSegmentPricesPage'));
const BusFleetVehiclesPage = lazy(() => import('../modules/tenant/bus/pages/BusFleetVehiclesPage'));
const BusVehicleDetailsPage = lazy(() => import('../modules/tenant/bus/pages/BusVehicleDetailsPage'));
const BusSeatMapsPage = lazy(() => import('../modules/tenant/bus/pages/BusSeatMapsPage'));
const BusTripSeatsPage = lazy(() => import('../modules/tenant/bus/pages/BusTripSeatsPage'));
const BusSeatHoldsPage = lazy(() => import('../modules/tenant/bus/pages/BusSeatHoldsPage'));
const TrainStopPointsPage = lazy(() => import('../modules/tenant/train/pages/TrainStopPointsPage'));
const TrainRoutesPage = lazy(() => import('../modules/tenant/train/pages/TrainRoutesPage'));
const TrainTripStopTimesPage = lazy(() => import('../modules/tenant/train/pages/TrainTripStopTimesPage'));
const TrainTripSegmentPricesPage = lazy(() => import('../modules/tenant/train/pages/TrainTripSegmentPricesPage'));
const TrainCarsPage = lazy(() => import('../modules/tenant/train/pages/TrainCarsPage'));
const TrainCarSeatsPage = lazy(() => import('../modules/tenant/train/pages/TrainCarSeatsPage'));
const TrainTripSeatsPage = lazy(() => import('../modules/tenant/train/pages/TrainTripSeatsPage'));
const TrainSeatHoldsPage = lazy(() => import('../modules/tenant/train/pages/TrainSeatHoldsPage'));
const FlightAirlinesPage = lazy(() => import('../modules/tenant/flight/pages/FlightAirlinesPage'));
const FlightAirportsPage = lazy(() => import('../modules/tenant/flight/pages/FlightAirportsPage'));
const FlightAircraftModelsPage = lazy(() => import('../modules/tenant/flight/pages/FlightAircraftModelsPage'));
const FlightAircraftsPage = lazy(() => import('../modules/tenant/flight/pages/FlightAircraftsPage'));
const FlightFareClassesPage = lazy(() => import('../modules/tenant/flight/pages/FlightFareClassesPage'));
const FlightFareRulesPage = lazy(() => import('../modules/tenant/flight/pages/FlightFareRulesPage'));
const FlightFlightsPage = lazy(() => import('../modules/tenant/flight/pages/FlightFlightsPage'));
const FlightOffersPage = lazy(() => import('../modules/tenant/flight/pages/FlightOffersPage'));
const FlightOfferTaxFeeLinesPage = lazy(() => import('../modules/tenant/flight/pages/FlightOfferTaxFeeLinesPage'));
const FlightCabinSeatMapsPage = lazy(() => import('../modules/tenant/flight/pages/FlightCabinSeatMapsPage'));
const FlightCabinSeatsPage = lazy(() => import('../modules/tenant/flight/pages/FlightCabinSeatsPage'));
const FlightAncillariesPage = lazy(() => import('../modules/tenant/flight/pages/FlightAncillariesPage'));
const CmsMediaPage = lazy(() => import('../modules/cms/pages/CmsMediaPage'));
const CmsCategoriesPage = lazy(() => import('../modules/cms/pages/CmsCategoriesPage'));
const CmsTagsPage = lazy(() => import('../modules/cms/pages/CmsTagsPage'));
const CmsRevisionsPage = lazy(() => import('../modules/cms/pages/CmsRevisionsPage'));
const CmsPreviewPage = lazy(() => import('../modules/cms/pages/CmsPreviewPage'));
const CmsSeoAuditPage = lazy(() => import('../modules/cms/pages/CmsSeoAuditPage'));
const CmsSiteSettingsPage = lazy(() => import('../modules/cms/pages/CmsSiteSettingsPage'));
const TourSchedulesPage = lazy(() => import('../modules/tenant/tour/pages/TourSchedulesPage'));
const TourPricingPage = lazy(() => import('../modules/tenant/tour/pages/TourPricingPage'));
const TourCapacityPage = lazy(() => import('../modules/tenant/tour/pages/TourCapacityPage'));
const TourPackagesPage = lazy(() => import('../modules/tenant/tour/pages/TourPackagesPage'));
const TourContentPage = lazy(() => import('../modules/tenant/tour/pages/TourContentPage'));
const TourExperiencePage = lazy(() => import('../modules/tenant/tour/pages/TourExperiencePage'));
const TourPackageBuilderPage = lazy(() => import('../modules/tenant/tour/pages/TourPackageBuilderPage'));
const TourPackageReportingPage = lazy(() => import('../modules/tenant/tour/pages/TourPackageReportingPage'));
const AdminToursPage = lazy(() => import('../modules/admin/pages/AdminToursPage'));
const AdminTourReviewsPage = lazy(() => import('../modules/admin/pages/AdminTourReviewsPage'));
const AdminTourSchedulesPage = lazy(() => import('../modules/admin/pages/AdminTourSchedulesPage'));
const AdminTourFaqsPage = lazy(() => import('../modules/admin/pages/AdminTourFaqsPage'));
const AdminTrainInventoryPage = lazy(() => import('../modules/admin/pages/AdminTrainInventoryPage'));
const AdminTrainStopPointsPage = lazy(() => import('../modules/admin/pages/AdminTrainStopPointsPage'));
const AdminTrainRoutesPage = lazy(() => import('../modules/admin/pages/AdminTrainRoutesPage'));
const AdminTrainTripStopTimesPage = lazy(() => import('../modules/admin/pages/AdminTrainTripStopTimesPage'));
const AdminTrainTripSegmentPricesPage = lazy(() => import('../modules/admin/pages/AdminTrainTripSegmentPricesPage'));
const AdminTrainCarsPage = lazy(() => import('../modules/admin/pages/AdminTrainCarsPage'));
const AdminTrainCarSeatsPage = lazy(() => import('../modules/admin/pages/AdminTrainCarSeatsPage'));
const AdminFlightInventoryPage = lazy(() => import('../modules/admin/pages/AdminFlightInventoryPage'));
const AdminFlightAirlinesPage = lazy(() => import('../modules/admin/pages/AdminFlightAirlinesPage'));
const AdminFlightAirportsPage = lazy(() => import('../modules/admin/pages/AdminFlightAirportsPage'));
const AdminFlightAircraftModelsPage = lazy(() => import('../modules/admin/pages/AdminFlightAircraftModelsPage'));
const AdminFlightAircraftsPage = lazy(() => import('../modules/admin/pages/AdminFlightAircraftsPage'));
const AdminFlightFareClassesPage = lazy(() => import('../modules/admin/pages/AdminFlightFareClassesPage'));
const AdminFlightFareRulesPage = lazy(() => import('../modules/admin/pages/AdminFlightFareRulesPage'));
const AdminFlightFlightsPage = lazy(() => import('../modules/admin/pages/AdminFlightFlightsPage'));
const AdminFlightOffersPage = lazy(() => import('../modules/admin/pages/AdminFlightOffersPage'));
const AdminFlightOfferTaxFeeLinesPage = lazy(() => import('../modules/admin/pages/AdminFlightOfferTaxFeeLinesPage'));
const AdminFlightCabinSeatMapsPage = lazy(() => import('../modules/admin/pages/AdminFlightCabinSeatMapsPage'));
const AdminFlightCabinSeatsPage = lazy(() => import('../modules/admin/pages/AdminFlightCabinSeatsPage'));
const AdminFlightAncillariesPage = lazy(() => import('../modules/admin/pages/AdminFlightAncillariesPage'));
const HotelRoomTypesPage = lazy(() => import('../modules/tenant/hotel/pages/HotelRoomTypesPage'));
const HotelRatePlansPage = lazy(() => import('../modules/tenant/hotel/pages/HotelRatePlansPage'));
const HotelPoliciesPage = lazy(() => import('../modules/tenant/hotel/pages/HotelPoliciesPage'));
const HotelExtraServicesPage = lazy(() => import('../modules/tenant/hotel/pages/HotelExtraServicesPage'));
const AdminHotelInventoryPage = lazy(() => import('../modules/admin/pages/AdminHotelInventoryPage'));
const AdminHotelAriPage = lazy(() => import('../modules/admin/pages/AdminHotelAriPage'));
const AdminHotelRoomTypesPage = lazy(() => import('../modules/admin/pages/AdminHotelRoomTypesPage'));
const AdminHotelRatePlansPage = lazy(() => import('../modules/admin/pages/AdminHotelRatePlansPage'));
const AdminHotelPoliciesPage = lazy(() => import('../modules/admin/pages/AdminHotelPoliciesPage'));
const AdminHotelExtraServicesPage = lazy(() => import('../modules/admin/pages/AdminHotelExtraServicesPage'));
const AdminHotelContactsPage = lazy(() => import('../modules/admin/pages/AdminHotelContactsPage'));
const AdminHotelImagesPage = lazy(() => import('../modules/admin/pages/AdminHotelImagesPage'));
const AdminHotelAmenitiesPage = lazy(() => import('../modules/admin/pages/AdminHotelAmenitiesPage'));
const AdminRoomAmenitiesPage = lazy(() => import('../modules/admin/pages/AdminRoomAmenitiesPage'));
const AdminMealPlansPage = lazy(() => import('../modules/admin/pages/AdminMealPlansPage'));
const AdminBedTypesPage = lazy(() => import('../modules/admin/pages/AdminBedTypesPage'));
const AdminRoomTypeImagesPage = lazy(() => import('../modules/admin/pages/AdminRoomTypeImagesPage'));
const AdminRoomTypePoliciesPage = lazy(() => import('../modules/admin/pages/AdminRoomTypePoliciesPage'));
const AdminHotelPromoOverridesPage = lazy(() => import('../modules/admin/pages/AdminHotelPromoOverridesPage'));
const AdminHotelReviewsPage = lazy(() => import('../modules/admin/pages/AdminHotelReviewsPage'));

function App() {
  return (
    <BrowserRouter>
      <ScrollToTop />
      <CustomerPreferenceBootstrap />
      <div className="App">
        <Suspense fallback={null}>
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
            <Route path="providers/bus/vehicles" element={<RequireAuth access="tenant" tenantModule="bus"><BusFleetVehiclesPage /></RequireAuth>} />
            <Route path="providers/bus/vehicle-details" element={<RequireAuth access="tenant" tenantModule="bus"><BusVehicleDetailsPage /></RequireAuth>} />
            <Route path="providers/bus/seat-maps" element={<RequireAuth access="tenant" tenantModule="bus"><BusSeatMapsPage /></RequireAuth>} />
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
            <Route path="promos" element={<RequireAuth access="tenant"><TenantPromosPage /></RequireAuth>} />
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
            <Route path="settings" element={<CmsSiteSettingsPage mode="admin" />} />
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
        </Suspense>
      </div>
    </BrowserRouter>
  );
}

export default App;
