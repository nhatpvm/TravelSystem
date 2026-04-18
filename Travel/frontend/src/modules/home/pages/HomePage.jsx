import React from 'react';
import { Plane } from 'lucide-react';
import Navbar from '../components/Navbar';
import Hero from '../components/Hero';
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
import Footer from '../components/Footer';

const HomePage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />
      <Hero />
      <Features />
      <DestinationCategories />
      <AboutAdventure />
      <Promotions />
      <PopularDestinations />
      <LastMinuteOffers />
      <Testimonials />
      <BookingPlatform />
      <TourGuides />
      <TourFacilities />
      <NewsArticles />
      <Footer />
    </div>
  );
};

export default HomePage;
