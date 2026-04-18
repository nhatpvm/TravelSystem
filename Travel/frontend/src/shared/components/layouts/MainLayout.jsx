import React from 'react';
import Navbar from '../../../modules/home/components/Navbar';
import Footer from '../../../modules/home/components/Footer';

const MainLayout = ({ children }) => {
  return (
    <div className="flex flex-col min-h-screen">
      <Navbar />
      <main className="flex-grow">
        {children}
      </main>
      <Footer />
    </div>
  );
};

export default MainLayout;
