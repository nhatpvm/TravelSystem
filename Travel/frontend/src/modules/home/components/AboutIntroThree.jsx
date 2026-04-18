import React from 'react';
import { motion } from 'framer-motion';
import { Plane, Compass, Calendar } from 'lucide-react';

const AboutIntroThree = () => {
    return (
        <section className="py-24 relative overflow-hidden bg-gradient-to-br from-[#F0F9FB] via-white to-[#FFF5F7]">
            {/* Background Decorative Illustration (Bottom Left) */}
            <div className="absolute bottom-0 left-0 w-48 md:w-64 opacity-80 pointer-events-none z-10">
                <img 
                    src="https://ex-coders.com/html/turmet/assets/img/about/bag-shape.png" 
                    alt="Travel Illustration" 
                    className="w-full h-auto"
                />
            </div>

            <div className="container mx-auto px-4 md:px-12 lg:px-24">
                <div className="flex flex-col lg:flex-row items-center gap-16 lg:gap-24">
                    
                    {/* Left: Overlapping Images */}
                    <div className="w-full lg:w-1/2 relative flex justify-center lg:justify-start">
                        <div className="relative w-full max-w-[500px] aspect-[4/5]">
                            {/* Plane Path Decoration */}
                            <div className="absolute -top-10 right-0 w-40 h-40 opacity-20 pointer-events-none">
                                <svg viewBox="0 0 100 100" fill="none" className="w-full h-full rotate-[-15deg]">
                                    <path d="M10 80C10 80 30 30 90 10" stroke="#1EB4D4" strokeWidth="1" strokeDasharray="4 4" />
                                </svg>
                                <div className="absolute top-0 right-0 text-[#1EB4D4]">
                                    <Plane size={24} className="rotate-45" />
                                </div>
                            </div>

                            {/* Image 1 (Back) */}
                            <motion.div 
                                initial={{ opacity: 0, rotate: -5, x: -20 }}
                                whileInView={{ opacity: 1, rotate: -12, x: 0 }}
                                transition={{ duration: 0.8 }}
                                className="absolute top-0 left-0 w-[75%] aspect-[4/5] rounded-[2.5rem] overflow-hidden shadow-2xl z-10 border-8 border-white"
                            >
                                <img 
                                    src="https://images.unsplash.com/photo-1513326738677-b964603b136d?q=80&w=800" 
                                    alt="Architecture View" 
                                    className="w-full h-full object-cover"
                                />
                            </motion.div>

                            {/* Image 2 (Front) */}
                            <motion.div 
                                initial={{ opacity: 0, rotate: 5, x: 20 }}
                                whileInView={{ opacity: 1, rotate: 10, x: 0 }}
                                transition={{ duration: 0.8, delay: 0.2 }}
                                className="absolute bottom-0 right-0 w-[75%] aspect-[4/5] rounded-[2.5rem] overflow-hidden shadow-2xl z-20 border-8 border-white"
                            >
                                <img 
                                    src="https://ex-coders.com/html/turmet/assets/img/about/07.png" 
                                    alt="Travelers Jumping" 
                                    className="w-full h-full object-cover"
                                />
                            </motion.div>
                        </div>
                    </div>

                    {/* Right: Content */}
                    <div className="w-full lg:w-1/2">
                        <motion.p 
                            initial={{ opacity: 0, x: 20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
                            style={{ fontFamily: "'Kalam', cursive" }}
                        >
                            Về chúng tôi
                        </motion.p>
                        <motion.h2 
                            initial={{ opacity: 0, x: 20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            transition={{ delay: 0.1 }}
                            className="text-4xl md:text-5xl lg:text-6xl font-black text-gray-900 leading-[1.1] mb-8"
                        >
                            Tận Hưởng Kỳ Nghỉ <br /> Tuyệt Vời Tại Đây
                        </motion.h2>
                        <motion.p 
                            initial={{ opacity: 0, x: 20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            transition={{ delay: 0.2 }}
                            className="text-gray-500 font-medium leading-relaxed mb-12 max-w-xl"
                        >
                            Dành thời gian tận hưởng những kỳ nghỉ tuyệt vời cùng người thân và bạn bè. Chúng tôi cung cấp những lộ trình linh hoạt giúp bạn khám phá những địa điểm mới mẻ và đầy cảm hứng.
                        </motion.p>

                        <div className="space-y-10">
                            {/* Feature 1 */}
                            <motion.div 
                                initial={{ opacity: 0, y: 20 }}
                                whileInView={{ opacity: 1, y: 0 }}
                                transition={{ delay: 0.3 }}
                                className="flex gap-6"
                            >
                                <div className="w-16 h-16 rounded-full bg-[#EBF8FB] flex items-center justify-center shrink-0 shadow-sm border border-white">
                                    <Calendar className="text-[#1EB4D4]" size={28} />
                                </div>
                                <div>
                                    <h4 className="text-xl font-black text-gray-900 mb-2">Lập Kế Hoạch Du Lịch</h4>
                                    <p className="text-gray-400 font-medium leading-relaxed max-w-sm">
                                        Lập kế hoạch du lịch chi tiết và phù hợp với sở thích cá nhân của bạn.
                                    </p>
                                </div>
                            </motion.div>

                            {/* Feature 2 */}
                            <motion.div 
                                initial={{ opacity: 0, y: 20 }}
                                whileInView={{ opacity: 1, y: 0 }}
                                transition={{ delay: 0.4 }}
                                className="flex gap-6"
                            >
                                <div className="w-16 h-16 rounded-full bg-[#FFF5F7] flex items-center justify-center shrink-0 shadow-sm border border-white">
                                    <Compass className="text-[#F15A24]" size={28} />
                                </div>
                                <div>
                                    <h4 className="text-xl font-black text-gray-900 mb-2">Khám Phá Xung Quanh</h4>
                                    <p className="text-gray-400 font-medium leading-relaxed max-w-sm">
                                        Khám phá những nét văn hóa đặc sắc và phong cảnh hữu tình xung quanh bạn.
                                    </p>
                                </div>
                            </motion.div>
                        </div>
                    </div>

                </div>
            </div>
        </section>
    );
};

export default AboutIntroThree;
