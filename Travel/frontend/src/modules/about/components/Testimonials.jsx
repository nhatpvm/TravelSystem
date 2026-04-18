import React from 'react';
import { motion } from 'framer-motion';
import { Quote, ArrowUp, ArrowDown } from 'lucide-react';

const Testimonials = () => {
  return (
    <section className="py-24 bg-[#F8FBFB] relative overflow-hidden">
      {/* Wave Background Pattern */}
      <div className="absolute inset-0 opacity-10 pointer-events-none">
        <svg width="100%" height="100%" viewBox="0 0 1440 800" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M0 100C200 50 400 150 600 100C800 50 1000 150 1200 100C1400 50 1600 150 1800 100" stroke="#1EB4D4" strokeWidth="2" />
          <path d="M0 200C200 150 400 250 600 200C800 150 1000 250 1200 200C1400 150 1600 250 1800 200" stroke="#1EB4D4" strokeWidth="2" />
          <path d="M0 300C200 250 400 350 600 300C800 250 1000 350 1200 300C1400 250 1600 350 1800 300" stroke="#1EB4D4" strokeWidth="2" />
        </svg>
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="flex flex-col lg:flex-row items-center gap-16">
          
          {/* Left Side: Image with Suitcase */}
          <div className="w-full lg:w-1/2 relative">
             <motion.div 
               initial={{ opacity: 0, x: -50 }}
               whileInView={{ opacity: 1, x: 0 }}
               transition={{ duration: 0.8 }}
               className="relative z-10"
             >
                {/* Background Shape */}
                <div className="absolute top-10 left-10 w-full h-[110%] bg-[#E6F4F6] -z-10 rounded-[3rem] rotate-[-2deg]"></div>
                
                {/* Main Image */}
                <img 
                  src="https://ex-coders.com/html/turmet/assets/img/testimonial/03.png" 
                  alt="Traveler with suitcase" 
                  className="w-full relative z-20"
                />

             </motion.div>
          </div>

          {/* Right Side: Testimonial Content */}
          <div className="w-full lg:w-1/2 relative">
             <div className="mb-12">
                <p 
                    className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
                    style={{ fontFamily: "'Kalam', cursive" }}
                >
                    Đánh giá
                </p>
                <h2 className="text-4xl md:text-5xl font-black text-gray-900 leading-tight">
                    Du khách yêu mến người bản địa của chúng tôi
                </h2>
             </div>

             {/* Testimonial Card */}
             <motion.div 
               initial={{ opacity: 0, y: 30 }}
               whileInView={{ opacity: 1, y: 0 }}
               className="bg-white p-10 md:p-12 rounded-[2.5rem] shadow-2xl shadow-gray-200/50 border border-gray-50 relative group"
             >
                {/* Author Info */}
                <div className="flex items-center justify-between mb-8">
                   <div className="flex items-center gap-4">
                      <div className="w-16 h-16 rounded-full overflow-hidden border-4 border-[#1EB4D4]/10">
                         <img 
                           src="https://images.unsplash.com/photo-1494790108377-be9c29b29330?q=80&w=150&h=150&auto=format&fit=crop" 
                           alt="Kathryn Murphy" 
                         />
                      </div>
                      <div>
                         <h4 className="text-xl font-bold text-gray-900">Kathryn Murphy</h4>
                         <p className="text-gray-400 text-sm font-medium">Nhà thiết kế Web</p>
                      </div>
                   </div>
                   <div className="text-[#1EB4D4] opacity-30 group-hover:opacity-100 transition-opacity">
                      <Quote size={48} fill="currentColor" />
                   </div>
                </div>

                <p className="text-gray-500 font-medium leading-[1.8] text-lg italic">
                   "Có rất nhiều biến thể của các đoạn văn bản Lorem Ipsum sẵn có, nhưng phần lớn đã bị thay đổi dưới một số hình thức, bởi những từ ngữ hài hước hoặc ngẫu nhiên không hề đáng tin."
                </p>
             </motion.div>

             {/* Navigation Controls */}
             <div className="absolute -right-4 lg:-right-12 top-1/2 -translate-y-1/2 flex flex-col gap-4">
                <button className="w-12 h-12 bg-white rounded-full flex items-center justify-center text-gray-400 hover:bg-[#1EB4D4] hover:text-white transition-all shadow-lg">
                   <ArrowUp size={20} />
                </button>
                <button className="w-12 h-12 bg-[#1EB4D4] rounded-full flex items-center justify-center text-white transition-all shadow-lg scale-110">
                   <ArrowDown size={20} />
                </button>
             </div>

             {/* Pulsing Dot */}
             <div className="absolute -top-10 right-20 w-3 h-3 bg-[#1EB4D4] rounded-full shadow-[0_0_20px_rgba(30,180,212,0.8)] animate-ping"></div>
          </div>

        </div>
      </div>
    </section>
  );
};

export default Testimonials;
