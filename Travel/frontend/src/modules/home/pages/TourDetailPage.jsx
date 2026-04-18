import React from 'react';
import { useParams } from 'react-router-dom';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import TourPublicDetailContent from '../../tours/components/TourPublicDetailContent';

export default function TourDetailPage() {
  const { id } = useParams();

  return (
    <MainLayout>
      <TourPublicDetailContent tourId={id} />
    </MainLayout>
  );
}
