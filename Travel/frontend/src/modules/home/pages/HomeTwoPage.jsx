import React from 'react';
import Navbar from '../components/Navbar';
import HeroTwo from '../components/HeroTwo';
import AboutIntro from '../components/AboutIntro';
import TopDestinations from '../components/TopDestinations';
import WhyChooseUs from '../components/WhyChooseUs';
import TravelStoryTwo from '../components/TravelStoryTwo';
import PopularDestinations from '../components/PopularDestinations';
import NewsArticlesTwo from '../components/NewsArticlesTwo';
import Footer from '../components/Footer';
import InstagramFeed from '../../about/components/InstagramFeed';

const HomeTwoPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />
      <HeroTwo />
      <div className="mt-40"> {/* Space for overlapping search box */}
        <AboutIntro />
        <TopDestinations />
        <WhyChooseUs />
        <TravelStoryTwo />
        <PopularDestinations />
        <NewsArticlesTwo />
        <InstagramFeed />
      </div>
      <Footer />
    </div>
  );
};

export default HomeTwoPage;
