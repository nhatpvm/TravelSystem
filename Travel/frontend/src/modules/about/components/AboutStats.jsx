import React from 'react';
import { motion } from 'framer-motion';
import { Users, Globe, Map, Building2 } from 'lucide-react';

const AboutStats = () => {
  const stats = [
    {
      id: 1,
      icon: <Users size={32} />,
      number: "100,000+",
      label: "Khách thám hiểm"
    },
    {
      id: 2,
      icon: <Globe size={32} />,
      number: "5,000+",
      label: "Điểm đến"
    },
    {
      id: 3,
      icon: <Map size={32} />,
      number: "10,000+",
      label: "Nhiều chuyến đi hơn"
    },
    {
      id: 4,
      icon: <Building2 size={32} />,
      number: "2,000+",
      label: "Khách sạn sang trọng"
    }
  ];

  return (
    <section className="bg-[#1EB4D4] py-20 relative overflow-hidden">
      {/* Subtle Pattern Background (Optional) */}
      <div className="absolute inset-0 opacity-10 pointer-events-none">
        <svg width="100%" height="100%" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse">
              <path d="M 40 0 L 0 0 0 40" fill="none" stroke="white" strokeWidth="1"/>
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#grid)" />
        </svg>
      </div>

      <div className="container mx-auto px-4 relative z-10">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 items-center">
          {stats.map((stat, index) => (
            <div key={stat.id} className="relative flex flex-col items-center text-center text-white p-8 group">
              {/* Icon Container with Glow */}
              <motion.div 
                initial={{ scale: 0.8, opacity: 0 }}
                whileInView={{ scale: 1, opacity: 1 }}
                transition={{ delay: index * 0.1, duration: 0.5 }}
                className="w-20 h-20 rounded-full bg-white/10 flex items-center justify-center mb-6 relative"
              >
                <div className="absolute inset-0 rounded-full bg-white/20 animate-ping group-hover:scale-110 opacity-30 transition-all"></div>
                <div className="relative z-10 bg-white/20 p-4 rounded-full border border-white/30 backdrop-blur-sm">
                  {stat.icon}
                </div>
              </motion.div>

              {/* Text Info */}
              <motion.h3 
                initial={{ y: 20, opacity: 0 }}
                whileInView={{ y: 0, opacity: 1 }}
                transition={{ delay: index * 0.1 + 0.2 }}
                className="text-4xl md:text-5xl font-black mb-2 tracking-tighter"
              >
                {stat.number}
              </motion.h3>
              
              <motion.p 
                initial={{ opacity: 0 }}
                whileInView={{ opacity: 1 }}
                transition={{ delay: index * 0.1 + 0.3 }}
                className="text-white/80 font-bold uppercase tracking-widest text-xs"
              >
                {stat.label}
              </motion.p>

              {/* Dotted Vertical Divider (only hidden on small and for the last item) */}
              {index !== stats.length - 1 && (
                <div className="hidden lg:block absolute right-0 top-1/2 -translate-y-1/2 h-24 w-[1px] border-r border-dashed border-white/30"></div>
              )}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default AboutStats;
