import React from 'react';
import { ChevronDown } from 'lucide-react';

function SidebarGroup({ title, items, selectedValue, onSelect }) {
  return (
    <div className="bg-white rounded-xl border border-gray-100 p-8 shadow-sm">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-xl font-black text-gray-900">{title}</h3>
        <ChevronDown size={20} className="text-gray-400" />
      </div>
      <div className="space-y-4">
        {items.map((item) => (
          <button
            key={item.value}
            type="button"
            onClick={() => onSelect(item.value)}
            className="w-full flex items-center justify-between group text-left"
          >
            <div className="flex items-center gap-3">
              <div
                className={`w-5 h-5 border-2 rounded flex items-center justify-center transition-all ${
                  selectedValue === item.value
                    ? 'bg-[#1EB4D4] border-[#1EB4D4]'
                    : 'border-gray-300 group-hover:border-[#1EB4D4]'
                }`}
              >
                {selectedValue === item.value && <div className="w-2 h-2 bg-white rounded-sm" />}
              </div>
              <span className="text-gray-500 font-bold group-hover:text-[#1EB4D4] transition-colors">
                {item.label}
              </span>
            </div>
            <span className="text-gray-400 text-sm font-medium">{item.count}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

const TourSidebar = ({
  destinations = [],
  selectedDestination = 'all',
  onDestinationChange = () => {},
  tourTypes = [],
  selectedType = 'all',
  onTypeChange = () => {},
  difficulties = [],
  selectedDifficulty = 'all',
  onDifficultyChange = () => {},
  priceLabel = 'Linh hoạt theo lịch khởi hành',
}) => (
  <aside className="w-full lg:w-1/4 space-y-8">
    <SidebarGroup
      title="Điểm đến"
      items={destinations}
      selectedValue={selectedDestination}
      onSelect={onDestinationChange}
    />

    <div className="bg-white rounded-xl border border-gray-100 p-8 shadow-sm">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-xl font-black text-gray-900">Khoảng giá</h3>
        <ChevronDown size={20} className="text-gray-400" />
      </div>
      <div className="relative h-2 bg-slate-100 rounded-full mb-6">
        <div className="absolute h-full bg-[#1EB4D4] left-[12%] right-[18%] rounded-full" />
        <div className="absolute top-1/2 -translate-y-1/2 left-[12%] w-4 h-4 bg-[#1EB4D4] border-2 border-white rounded shadow-md" />
        <div className="absolute top-1/2 -translate-y-1/2 right-[18%] w-4 h-4 bg-[#1EB4D4] border-2 border-white rounded shadow-md" />
      </div>
      <div className="text-gray-900 font-bold">
        Giá:
        <span className="text-gray-500 ml-2">{priceLabel}</span>
      </div>
    </div>

    <SidebarGroup
      title="Loại tour"
      items={tourTypes}
      selectedValue={selectedType}
      onSelect={onTypeChange}
    />

    <SidebarGroup
      title="Độ khó"
      items={difficulties}
      selectedValue={selectedDifficulty}
      onSelect={onDifficultyChange}
    />
  </aside>
);

export default TourSidebar;
