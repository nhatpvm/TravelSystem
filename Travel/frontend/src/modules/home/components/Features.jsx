import React from 'react';
import { Tag, Users, Headphones, Globe } from 'lucide-react';

const features = [
  {
    img:"https://ex-coders.com/html/turmet/assets/img/icon/01.svg",
    title: "Rất nhiều ưu đãi",
    color: "text-teal-500",
    bgColor: "bg-teal-50"
  },
  {
    img:"https://ex-coders.com/html/turmet/assets/img/icon/02.svg",
    title: "Hướng dẫn tốt nhất",
    color: "text-red-400",
    bgColor: "bg-red-50"
  },
  {
    img:"https://ex-coders.com/html/turmet/assets/img/icon/03.svg",
    title: "Hỗ trợ 24/7",
    color: "text-blue-400",
    bgColor: "bg-blue-50"
  },
  {
    img:"https://ex-coders.com/html/turmet/assets/img/icon/04.svg",
    title: "Quản lý du lịch",
    color: "text-purple-500",
    bgColor: "bg-purple-50"
  }
];

const Features = () => {
  return (
    <section className="py-20 bg-white">
      <div className="container mx-auto px-8 md:px-24 lg:px-48">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
          {features.map((feature, idx) => (
            <div key={idx} className="flex flex-col items-center text-center group">
              <div className={`w-20 h-20 rounded-full ${feature.bgColor} ${feature.color} flex items-center justify-center mb-6 transition-transform duration-300 group-hover:scale-110 shadow-sm`}>
                <img src={feature.img} alt={feature.title} />
              </div>
              <h3 className="text-xl font-bold text-gray-900 tracking-tight">
                {feature.title}
              </h3>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default Features;
