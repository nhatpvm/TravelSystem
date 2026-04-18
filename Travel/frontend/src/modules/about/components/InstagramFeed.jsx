import React from 'react';
import { Instagram } from 'lucide-react';

const photos = [
  { id: 1, src: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=600&auto=format&fit=crop", alt: "Coastal road" },
  { id: 2, src: "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=600&auto=format&fit=crop", alt: "Traveler on beach" },
  { id: 3, src: "https://images.unsplash.com/photo-1516426122078-c23e76319801?q=80&w=600&auto=format&fit=crop", alt: "Giraffes in Africa" },
  { id: 4, src: "https://images.unsplash.com/photo-1445019980597-93fa8acb246c?q=80&w=600&auto=format&fit=crop", alt: "Tropical beach" },
  { id: 5, src: "https://images.unsplash.com/photo-1544551763-46a013bb70d5?q=80&w=600&auto=format&fit=crop", alt: "Scuba diver" },
  { id: 7, src: "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?q=80&w=600&auto=format&fit=crop", alt: "Mountain landscape" },
  { id: 8, src: "https://images.unsplash.com/photo-1525625293386-3f8f99389edd?q=80&w=600&auto=format&fit=crop", alt: "Snowy peaks" },
  { id: 9, src: "https://images.unsplash.com/photo-1500835556837-99ac94a94552?q=80&w=600&auto=format&fit=crop", alt: "Airport" },
  { id: 10, src: "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?q=80&w=600&auto=format&fit=crop", alt: "Resort pool" },
  { id: 11, src: "https://images.unsplash.com/photo-1533105079780-92b9be482077?q=80&w=600&auto=format&fit=crop", alt: "Santorini" },
  { id: 12, src: "https://images.unsplash.com/photo-1501854140801-50d01698950b?q=80&w=600&auto=format&fit=crop", alt: "Aerial mountains" },
];

// duplicate for seamless loop
const allPhotos = [...photos, ...photos];

const InstagramFeed = () => {
  return (
    <section className="py-20 bg-white overflow-hidden">
      <div className="w-[90%] mx-auto">

        {/* Section Title */}
        <div className="flex items-center justify-center gap-6 mb-12">
          <div className="flex-1 h-[1px] bg-gray-200"></div>
          <h2 className="text-2xl md:text-3xl font-black text-gray-900 flex items-center gap-3 whitespace-nowrap">
            <Instagram size={28} className="text-[#E1306C]" />
            Theo dõi Instagram
          </h2>
          <div className="flex-1 h-[1px] bg-gray-200"></div>
        </div>

      </div>

      {/* Marquee Strip — full width for seamless scroll */}
      <div className="relative w-full overflow-hidden">
        {/* Fade edges */}
        <div className="absolute left-0 top-0 bottom-0 w-24 z-10 bg-gradient-to-r from-white to-transparent pointer-events-none"></div>
        <div className="absolute right-0 top-0 bottom-0 w-24 z-10 bg-gradient-to-l from-white to-transparent pointer-events-none"></div>

        {/* Scrolling track */}
        <div
          className="flex gap-4"
          style={{
            display: 'flex',
            width: 'max-content',
            animation: 'instagram-scroll 35s linear infinite',
          }}
        >
          {allPhotos.map((photo, index) => (
            <a
              key={index}
              href="#"
              className="relative flex-shrink-0 w-80 h-80 rounded-2xl overflow-hidden group cursor-pointer shadow-sm hover:shadow-xl transition-all duration-300"
            >
              <img
                src={photo.src}
                alt={photo.alt}
                className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
              />
              {/* Hover Overlay */}
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent opacity-0 group-hover:opacity-100 transition-all duration-300 flex items-center justify-center">
                <div className="bg-white/20 backdrop-blur-sm rounded-full p-3 border border-white/40">
                  <Instagram size={22} className="text-white" />
                </div>
              </div>
            </a>
          ))}
        </div>
      </div>

      {/* CSS keyframes */}
      <style>{`
        @keyframes instagram-scroll {
          0% { transform: translateX(0); }
          100% { transform: translateX(-50%); }
        }
      `}</style>
    </section>
  );
};

export default InstagramFeed;
