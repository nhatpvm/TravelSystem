import React from 'react';
import { motion } from 'framer-motion';

const WhyChooseUs = () => {
    const steps = [
        {
            number: "01",
            title: "Tìm Và Tận Hưởng Chuyến Đi Phù Hợp Với Lối Sống Của Bạn",
            desc: "Chúng tôi cung cấp những lộ trình linh hoạt và phong phú, giúp bạn và bạn bè có những giây phút trải nghiệm tuyệt vời nhất."
        },
        {
            number: "02",
            title: "Du Lịch Với Sự Tự Tin Tuyệt Đối",
            desc: "Mọi khâu chuẩn bị từ an ninh đến dịch vụ đều được chúng tôi kiểm soát chặt chẽ để bạn hoàn toàn yên tâm tận hưởng."
        },
        {
            number: "03",
            title: "Khám Phá Những Giá Trị Thực Sự Từ Chúng Tôi",
            desc: "Không chỉ là một chuyến đi, đó là hành trình khám phá bản thân và những nét văn hóa đặc sắc trên khắp thế giới."
        }
    ];

    return (
        <section className="py-24 bg-white overflow-hidden">
            <div className="container mx-auto px-4 md:px-12 lg:px-24">
                <div className="flex flex-col lg:flex-row items-center gap-16">
                    
                    {/* Left: Content */}
                    <div className="w-full lg:w-1/2">
                        <motion.p 
                            initial={{ opacity: 0, x: -20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
                            style={{ fontFamily: "'Kalam', cursive" }}
                        >
                            Tại sao chọn chúng tôi
                        </motion.p>
                        <motion.h2 
                            initial={{ opacity: 0, x: -20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            transition={{ delay: 0.1 }}
                            className="text-4xl md:text-5xl font-black text-gray-900 leading-tight mb-8"
                        >
                            Nhận Trải Nghiệm Du Lịch <br /> Tốt Nhất Cùng Chúng Tôi
                        </motion.h2>
                        <motion.p 
                            initial={{ opacity: 0, x: -20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            transition={{ delay: 0.2 }}
                            className="text-gray-500 font-medium leading-[1.8] mb-12 max-w-xl"
                        >
                            Chúng tôi cam kết mang lại sự hài lòng tối đa cho mọi du khách thông qua những dịch vụ chuyên nghiệp và tận tâm nhất.
                        </motion.p>

                        {/* Steps List */}
                        <div className="relative space-y-12">
                            {/* Vertical Dashed Line */}
                            <div className="absolute left-[26px] top-6 bottom-6 w-[2px] border-l-2 border-dashed border-gray-100 hidden md:block"></div>

                            {steps.map((step, index) => (
                                <motion.div 
                                    key={index}
                                    initial={{ opacity: 0, x: -30 }}
                                    whileInView={{ opacity: 1, x: 0 }}
                                    transition={{ delay: 0.3 + index * 0.1 }}
                                    className="flex gap-8 relative z-10"
                                >
                                    <div className="w-14 h-14 rounded-full bg-[#F0F9FB] flex items-center justify-center shrink-0 border border-gray-50">
                                        <span className="text-[#1EB4D4] text-xl font-black">{step.number}</span>
                                    </div>
                                    <div>
                                        <h4 className="text-xl font-black text-gray-900 mb-3 leading-tight">{step.title}</h4>
                                        <p className="text-gray-400 font-medium leading-relaxed max-w-md">{step.desc}</p>
                                    </div>
                                </motion.div>
                            ))}
                        </div>
                    </div>

                    {/* Right: Abstract Image Section */}
                    <div className="w-full lg:w-1/2 relative flex justify-center">
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.8 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ duration: 0.8 }}
                            className="relative w-full max-w-[600px] aspect-square flex items-center justify-center"
                        >
                            {/* Giant Cyan Blob Shape Background */}
                            <div className="absolute inset-0 bg-[#1EB4D4] rounded-full opacity-10 animate-pulse scale-110"></div>
                            <div className="absolute inset-4 bg-[#1EB4D4] rounded-[4rem] rotate-12 opacity-80 overflow-hidden shadow-2xl shadow-[#1EB4D4]/30">
                                <img 
                                    src="https://images.unsplash.com/photo-1530789253388-582c481c54b0?q=80&w=1200" 
                                    alt="Travel Couple" 
                                    className="w-full h-full object-cover -rotate-12 scale-125"
                                />
                            </div>

                            <motion.div 
                                animate={{ rotate: [0, 10, 0] }}
                                transition={{ duration: 6, repeat: Infinity, ease: "easeInOut" }}
                                className="absolute bottom-20 -left-10 w-24 h-24 bg-[#1EB4D4] rounded-full shadow-xl shadow-[#1EB4D4]/50 hidden md:block"
                            ></motion.div>
                        </motion.div>
                    </div>

                </div>
            </div>
        </section>
    );
};

export default WhyChooseUs;
