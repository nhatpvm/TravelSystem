import React from 'react';
import Navbar from '../components/Navbar';
import HeroThree from '../components/HeroThree';
import AboutIntro from '../components/AboutIntro';
import Features from '../components/Features';
import DestinationCategories from '../components/DestinationCategories';
import AboutAdventure from '../components/AboutAdventure';
import Promotions from '../components/Promotions';
import PopularDestinations from '../components/PopularDestinations';
import LastMinuteOffers from '../components/LastMinuteOffers';
import Testimonials from '../components/Testimonials';
import BookingPlatform from '../components/BookingPlatform';
import TourGuides from '../components/TourGuides';
import TourFacilities from '../components/TourFacilities';
import NewsArticles from '../components/NewsArticles';
import PopularDestinationsThree from '../components/PopularDestinationsThree';
import AboutIntroThree from '../components/AboutIntroThree';
import FlightDealsThree from '../components/FlightDealsThree';
import DiscountTourThree from '../components/DiscountTourThree';
import FeaturedFlightDeals from '../components/FeaturedFlightDeals';
import TestimonialsThree from '../components/TestimonialsThree';
import AdventureCardsThree from '../components/AdventureCardsThree';
import FAQThree from '../components/FAQThree';
import StatsBarThree from '../components/StatsBarThree';
import RecentBlogThree from '../components/RecentBlogThree';
import InstagramFeed from '../../about/components/InstagramFeed';
import Footer from '../components/Footer';

const HomeThreePage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />
      <HeroThree />
      <div className="mt-40"> {/* Space for search box overlap */}
        <PopularDestinationsThree />
        <AboutIntroThree />
        <FlightDealsThree />
        <DiscountTourThree />
        <FeaturedFlightDeals />
        <TestimonialsThree />
        <AdventureCardsThree />
        <FAQThree />
        <StatsBarThree />
        <RecentBlogThree />
        <InstagramFeed />
      </div>
      <Footer />
    </div>
  );
};

export default HomeThreePage;
