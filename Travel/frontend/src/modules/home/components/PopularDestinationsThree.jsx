import React from 'react';
import { motion } from 'framer-motion';

const PopularDestinationsThree = () => {
    const images = [
        {
            src: "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=800",
            alt: "Thailand Bay",
            className: "col-span-1 h-[250px]"
        },
        {
            src: "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?q=80&w=800",
            alt: "Couple Adventure",
            className: "col-span-1 row-span-2 h-[520px]"
        },
        {
            src: "https://images.unsplash.com/photo-1537996194471-e657df975ab4?q=80&w=800",
            alt: "Bali View",
            className: "col-span-1 h-[320px]"
        },
        {
            src: "https://ex-coders.com/html/turmet/assets/img/destination/new/11.jpg",
            alt: "Tropical Island",
            className: "col-span-1 h-[320px]"
        },
        {
            src: "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?q=80&w=800",
            alt: "Cloudy Peaks",
            className: "col-span-1 h-[250px]"
        },
        {
            src: "https://images.unsplash.com/photo-1544644181-1484b3fdfc62?q=80&w=800",
            alt: "Red Dress Mountain",
            className: "col-span-1 h-[450px]"
        },
        {
            src: "https://ex-coders.com/html/turmet/assets/img/destination/new/10.jpg",
            alt: "Island Photographer",
            className: "col-span-1 h-[250px]"
        }
    ];

    return (
        <section className="py-24 bg-white overflow-hidden">
            <div className="container mx-auto px-4 md:px-12 lg:px-24">
                {/* Section Header */}
                <div className="text-center mb-16 relative">
                    <motion.p 
                        initial={{ opacity: 0, y: 10 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
                        style={{ fontFamily: "'Kalam', cursive" }}
                    >
                        Điểm đến hàng đầu
                    </motion.p>
                    <motion.h2 
                        initial={{ opacity: 0, y: 20 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        transition={{ delay: 0.1 }}
                        className="text-4xl md:text-5xl font-black text-gray-900 leading-tight"
                    >
                        Những Điểm Đến Phổ Biến Nhất
                    </motion.h2>

                    {/* Decorative Circle Icon */}
                    <motion.div 
                        initial={{ scale: 0 }}
                        whileInView={{ scale: 1 }}
                        className="absolute left-1/2 -top-4 -translate-x-12 w-10 h-10 border-4 border-[#1EB4D4]/30 rounded-full hidden md:block"
                    ></motion.div>
                </div>

                {/* Staggered Masonry-style Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 items-start">
                    
                    {/* Column 1 */}
                    <div className="flex flex-col gap-6">
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[260px]"
                        >
                            <img src={images[0].src} alt={images[0].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ delay: 0.2 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[260px]"
                        >
                            <img src={images[4].src} alt={images[4].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                    </div>

                    {/* Column 2 (Tall Item) */}
                    <div className="flex flex-col">
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ delay: 0.1 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[544px]"
                        >
                            <img src={images[1].src} alt={images[1].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                    </div>

                    {/* Column 3 */}
                    <div className="flex flex-col gap-6">
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ delay: 0.3 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[330px]"
                        >
                            <img src={images[2].src} alt={images[2].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ delay: 0.4 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[430px]"
                        >
                            <img src={images[5].src} alt={images[5].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                    </div>

                    {/* Column 4 */}
                    <div className="flex flex-col gap-6">
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ delay: 0.5 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[330px]"
                        >
                            <img src={images[3].src} alt={images[3].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                        <motion.div 
                            initial={{ opacity: 0, scale: 0.9 }}
                            whileInView={{ opacity: 1, scale: 1 }}
                            transition={{ delay: 0.6 }}
                            className="rounded-3xl overflow-hidden shadow-lg h-[260px]"
                        >
                            <img src={images[6].src} alt={images[6].alt} className="w-full h-full object-cover hover:scale-110 transition-transform duration-700" />
                        </motion.div>
                    </div>

                </div>
            </div>
        </section>
    );
};

export default PopularDestinationsThree;
