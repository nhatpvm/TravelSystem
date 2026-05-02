import React from 'react';
import { motion } from 'framer-motion';
import { MoveRight } from 'lucide-react';
import nav1 from '../../../assets/nav1.png';
import nav2 from '../../../assets/nav2.png';
import nav3 from '../../../assets/nav3.png';

const cards = [
  {
    id: 1,
    title: "NÚI\nBATUR",
    image: nav1,
    bgColor: "from-[#1EB4D4] to-[#0E8FA8]"
  },
  {
    id: 2,
    title: "Phiêu Lưu\nChờ Đợi",
    image: nav2,
    bgColor: "from-[#F5A623] to-[#E08E0A]"
  },
  {
    id: 3,
    title: "Kỳ Nghỉ\nTuyệt Vời",
    image: nav3,
    bgColor: "from-[#1EB4D4] to-[#0E8FA8]"
  }
];

const AdventureCardsThree = () => {
  return (
    <section className="py-24 bg-white">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {cards.map((card, idx) => (
            <motion.div
              key={card.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.15 }}
              viewport={{ once: true }}
              className={`relative rounded-[2rem] overflow-hidden h-[320px] bg-gradient-to-br ${card.bgColor} group cursor-pointer`}
            >
              {/* Background Image - expands to full on hover */}
              <div className="absolute right-0 bottom-0 w-[65%] h-full group-hover:w-full transition-all duration-700 ease-in-out">
                <img
                  src={card.image}
                  alt={card.title}
                  className="w-full h-full object-cover opacity-90 group-hover:opacity-100 group-hover:scale-110 transition-all duration-700"
                />
                {/* Gradient fade - gets darker on hover for readability */}
                <div 
                  className="absolute inset-0 transition-all duration-700"
                  style={{ 
                    background: `linear-gradient(to right, ${idx === 1 ? '#F5A623' : '#1EB4D4'} 0%, transparent 50%)` 
                  }}
                ></div>
                <div className="absolute inset-0 bg-black/0 group-hover:bg-black/30 transition-all duration-700"></div>
              </div>

              {/* Content */}
              <div className="relative z-10 p-8 h-full flex flex-col justify-between">
                <h3 className="text-3xl md:text-4xl font-black text-white leading-tight whitespace-pre-line">
                  {card.title}
                </h3>
                <button className="w-fit bg-white/20 backdrop-blur-sm hover:bg-white hover:text-gray-900 text-white px-6 py-3 rounded-full text-sm font-bold flex items-center gap-2 transition-all group/btn border border-white/30">
                  Đặt ngay <MoveRight size={16} className="group-hover/btn:translate-x-1 transition-transform" />
                </button>
              </div>

              {/* Decorative elements */}
              {idx === 1 && (
                <div className="absolute top-6 right-6 z-10">
                  <motion.div
                    animate={{ rotate: [0, 15, -15, 0] }}
                    transition={{ repeat: Infinity, duration: 4 }}
                  >
                    <img
                      src={nav3}
                      alt="Plane"
                      className="w-16 opacity-60"
                    />
                  </motion.div>
                </div>
              )}
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default AdventureCardsThree;
