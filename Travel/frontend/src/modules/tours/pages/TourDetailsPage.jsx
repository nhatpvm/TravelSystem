import React from 'react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import TourPublicDetailContent from '../components/TourPublicDetailContent';

export default function TourDetailsPage() {
  return (
    <MainLayout>
      <TourPublicDetailContent useFeaturedFallback />
    </MainLayout>
  );
}
