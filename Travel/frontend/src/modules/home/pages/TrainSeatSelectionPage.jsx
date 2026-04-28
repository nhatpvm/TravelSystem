import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { Train, ArrowRight, ShieldCheck, Info } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getTrainTripDetail, getTrainTripSeats, holdTrainSeats } from '../../../services/trainService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, formatTime, getSeatStatusClass, getSeatStatusLabel } from '../../tenant/train/utils/presentation';

function buildSeatGroups(seats) {
  const compartments = new Map();
  const normalSeats = [];

  seats.forEach((seat) => {
    if (seat.compartmentCode || seat.compartmentIndex !== null) {
      const key = `${seat.compartmentCode || 'Khoang'}-${seat.compartmentIndex ?? 'x'}`;
      if (!compartments.has(key)) {
        compartments.set(key, []);
      }

      compartments.get(key).push(seat);
      return;
    }

    normalSeats.push(seat);
  });

  return {
    compartments: [...compartments.entries()].map(([key, items]) => ({
      key,
      label: items[0]?.compartmentCode || `Khoang ${items[0]?.compartmentIndex ?? ''}`.trim(),
      items: [...items].sort((left, right) => {
        if (left.rowIndex !== right.rowIndex) {
          return left.rowIndex - right.rowIndex;
        }

        return left.columnIndex - right.columnIndex;
      }),
    })),
    seats: normalSeats.sort((left, right) => {
      if (left.rowIndex !== right.rowIndex) {
        return left.rowIndex - right.rowIndex;
      }

      return left.columnIndex - right.columnIndex;
    }),
  };
}

function getDisplayRoute(detail) {
  const stops = detail?.stops || [];
  const origin = stops.find((item) => item.isSelectedOrigin) || stops[0];
  const destination = stops.find((item) => item.isSelectedDestination) || stops[stops.length - 1];

  return {
    from: origin?.location?.name || origin?.stopPoint?.name || 'Ga đi',
    to: destination?.location?.name || destination?.stopPoint?.name || 'Ga đến',
  };
}

export default function TrainSeatSelectionPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const session = useAuthSession();
  const [detail, setDetail] = useState(null);
  const [seatData, setSeatData] = useState(null);
  const [selectedCarIndex, setSelectedCarIndex] = useState(0);
  const [selectedSeatIds, setSelectedSeatIds] = useState([]);
  const [submitting, setSubmitting] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const tripId = searchParams.get('tripId') || '';
  const fromTripStopTimeId = searchParams.get('fromTripStopTimeId') || '';
  const toTripStopTimeId = searchParams.get('toTripStopTimeId') || '';

  useEffect(() => {
    if (!tripId || !fromTripStopTimeId || !toTripStopTimeId) {
      setError('Thiếu thông tin chuyến tàu hoặc chặng cần chọn chỗ.');
      setLoading(false);
      return undefined;
    }

    let active = true;
    setLoading(true);
    setError('');

    Promise.all([
      getTrainTripDetail(tripId, { fromTripStopTimeId, toTripStopTimeId }),
      getTrainTripSeats(tripId, { fromTripStopTimeId, toTripStopTimeId }),
    ])
      .then(([detailResponse, seatResponse]) => {
        if (active) {
          setDetail(detailResponse);
          setSeatData(seatResponse);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được sơ đồ chỗ của chuyến tàu.');
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [tripId, fromTripStopTimeId, toTripStopTimeId]);

  const cars = seatData?.cars || [];
  const selectedCar = cars[selectedCarIndex] || cars[0] || null;
  const selectedCarSeats = useMemo(() => selectedCar?.seats || [], [selectedCar?.seats]);
  const groupedSeats = useMemo(() => buildSeatGroups(selectedCarSeats), [selectedCarSeats]);
  const selectedSeats = selectedCarSeats.filter((seat) => selectedSeatIds.includes(seat.id));
  const basePrice = Number(detail?.segment?.price || 0);
  const totalPrice = selectedSeats.reduce((sum, seat) => sum + basePrice + Number(seat.priceModifier || 0), 0);
  const route = getDisplayRoute(detail);

  const toggleSeat = (seat) => {
    if (!seat || seat.status !== 'available') {
      return;
    }

    setSelectedSeatIds((current) => (
      current.includes(seat.id)
        ? current.filter((item) => item !== seat.id)
        : [...current, seat.id]
    ));
  };

  const handleContinue = async () => {
    if (selectedSeatIds.length === 0 || submitting) {
      return;
    }

    if (!session.isAuthenticated) {
      navigate('/auth/login', {
        replace: true,
        state: {
          from: `${location.pathname}${location.search}`,
        },
      });
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      const response = await holdTrainSeats({
        tripId,
        fromTripStopTimeId,
        toTripStopTimeId,
        trainCarSeatIds: selectedSeatIds,
      });

      const checkoutParams = new URLSearchParams({
        tripId,
        fromTripStopTimeId,
        toTripStopTimeId,
        holdToken: response.holdToken,
        seatCount: String(selectedSeatIds.length),
        product: 'train',
      });

      navigate(`/checkout?${checkoutParams.toString()}`);
    } catch (err) {
      setError(err.message || 'Không giữ được chỗ đã chọn.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-24">
        <div className="bg-slate-900 pt-32 pb-20">
          <div className="container mx-auto px-4 text-center md:text-left">
            <div className="flex flex-col md:flex-row items-center justify-between gap-8">
              <div>
                <div className="flex items-center justify-center md:justify-start gap-3 text-[#1EB4D4] text-[10px] font-black uppercase tracking-[0.3em] mb-4">
                  <Train size={14} /> <span>{route.from} → {route.to}</span>
                </div>
                <h1 className="text-4xl font-black text-white tracking-tighter">Chọn toa & chỗ ngồi</h1>
              </div>
              <div className="flex items-center gap-8">
                <div className="text-center">
                  <p className="text-2xl font-black text-white leading-none">{formatTime(detail?.segment?.departureAt)}</p>
                  <p className="text-[10px] font-black text-white/40 uppercase tracking-widest mt-1">Ga đi</p>
                </div>
                <div className="w-12 h-px bg-white/10 hidden md:block" />
                <div className="text-center">
                  <p className="text-2xl font-black text-white leading-none">{formatTime(detail?.segment?.arrivalAt)}</p>
                  <p className="text-[10px] font-black text-white/40 uppercase tracking-widest mt-1">Ga đến</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-10 relative z-10">
          {loading ? (
            <div className="bg-white p-12 rounded-[3.5rem] shadow-xl shadow-slate-200/50 border border-slate-100 text-center text-sm font-bold text-slate-500">
              Đang tải sơ đồ chỗ...
            </div>
          ) : (
            <div className="flex flex-col lg:flex-row gap-12">
              <div className="flex-1 space-y-8">
                {error ? (
                  <div className="rounded-[2rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
                    {error}
                  </div>
                ) : null}

                <div className="bg-white p-12 rounded-[3.5rem] shadow-xl shadow-slate-200/50 border border-slate-100">
                  <div className="flex flex-wrap items-center justify-between gap-6 mb-12">
                    <div className="flex gap-3 flex-wrap">
                      {cars.map((car, index) => (
                        <button
                          key={car.id}
                          type="button"
                          onClick={() => {
                            setSelectedCarIndex(index);
                            setSelectedSeatIds([]);
                          }}
                          className={`h-14 px-8 rounded-2xl flex items-center gap-3 text-xs font-black uppercase tracking-widest transition-all ${selectedCar?.id === car.id ? 'bg-slate-900 text-white shadow-lg' : 'bg-slate-50 text-slate-400 hover:bg-slate-100'}`}
                        >
                          <Train size={16} /> Toa {car.carNumber}
                        </button>
                      ))}
                    </div>
                    <div className="flex gap-6 flex-wrap">
                      {['available', 'held_by_me', 'held', 'booked', 'inactive'].map((status) => (
                        <div key={status} className="flex items-center gap-3">
                          <div className={`w-5 h-5 rounded-lg ${getSeatStatusClass(status)}`} />
                          <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{getSeatStatusLabel(status)}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  {selectedCar ? (
                    groupedSeats.compartments.length > 0 ? (
                      <div className="space-y-6">
                        {groupedSeats.compartments.map((group) => (
                          <div key={group.key} className="group p-8 bg-slate-50 rounded-[2.5rem] border border-transparent hover:border-[#1EB4D4]/10 hover:bg-white transition-all">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-6">{group.label || 'Khoang'}</p>
                            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                              {group.items.map((seat) => {
                                const isSelected = selectedSeatIds.includes(seat.id);

                                return (
                                  <button
                                    key={seat.id}
                                    type="button"
                                    disabled={seat.status !== 'available' && !isSelected}
                                    onClick={() => toggleSeat(seat)}
                                    className={`
                                      h-24 rounded-2xl flex flex-col items-center justify-center transition-all p-2 border-2
                                      ${isSelected
                                        ? 'bg-slate-900 text-white shadow-xl scale-105 border-slate-900'
                                        : seat.status === 'available'
                                          ? 'bg-white text-slate-600 hover:border-[#1EB4D4] border-transparent shadow-sm'
                                          : getSeatStatusClass(seat.status)}
                                    `}
                                  >
                                    <div className="text-[11px] font-black uppercase tracking-tight text-center">
                                      {seat.seatNumber}
                                    </div>
                                  </button>
                                );
                              })}
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                        {groupedSeats.seats.map((seat) => {
                          const isSelected = selectedSeatIds.includes(seat.id);

                          return (
                            <button
                              key={seat.id}
                              type="button"
                              disabled={seat.status !== 'available' && !isSelected}
                              onClick={() => toggleSeat(seat)}
                              className={`
                                h-24 rounded-2xl flex flex-col items-center justify-center transition-all p-2 border-2
                                ${isSelected
                                  ? 'bg-slate-900 text-white shadow-xl scale-105 border-slate-900'
                                  : seat.status === 'available'
                                    ? 'bg-white text-slate-600 hover:border-[#1EB4D4] border-transparent shadow-sm'
                                    : getSeatStatusClass(seat.status)}
                              `}
                            >
                              <div className="text-[11px] font-black uppercase tracking-tight text-center">{seat.seatNumber}</div>
                            </button>
                          );
                        })}
                      </div>
                    )
                  ) : (
                    <div className="py-24 bg-slate-50 rounded-[3rem] text-center border-2 border-dashed border-slate-200">
                      <div className="w-20 h-20 bg-white rounded-full flex items-center justify-center mx-auto mb-6 text-slate-300">
                        <Train size={32} />
                      </div>
                      <p className="text-lg font-black text-slate-900 tracking-tight">Chưa có toa nào để hiển thị</p>
                      <p className="text-sm font-medium text-slate-400 mt-2">Đối tác chưa hoàn tất cấu hình toa và sơ đồ chỗ cho chuyến tàu này.</p>
                    </div>
                  )}
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="p-8 bg-slate-900 rounded-[3rem] text-white flex gap-6 italic">
                    <div className="w-12 h-12 bg-white/10 rounded-2xl flex items-center justify-center shrink-0 text-[#1EB4D4]"><ShieldCheck size={24} /></div>
                    <div>
                      <p className="text-[10px] font-black uppercase tracking-[0.2em] mb-2 text-[#1EB4D4]">Đã xác thực</p>
                      <p className="text-xs font-bold text-white/60 leading-relaxed">Lượt giữ chỗ của bạn sẽ được khóa tạm trong vài phút để tránh trùng chỗ với khách khác.</p>
                    </div>
                  </div>
                  <div className="p-8 bg-blue-50/50 rounded-[3rem] border border-blue-100 flex gap-6">
                    <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center shrink-0 text-blue-500 shadow-sm"><Info size={24} /></div>
                    <div>
                      <p className="text-[10px] font-black uppercase tracking-[0.2em] mb-2 text-blue-500">Lưu ý đi tàu</p>
                      <p className="text-xs font-bold text-blue-900/40 leading-relaxed">Họ tên khách hàng đi tàu cần trùng khớp với giấy tờ tùy thân khi check-in.</p>
                    </div>
                  </div>
                </div>
              </div>

              <aside className="w-full lg:w-[400px]">
                <div className="sticky top-28 bg-white rounded-[3.5rem] shadow-2xl shadow-slate-200/60 border border-slate-100 p-10 overflow-hidden">
                  <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16 -z-0" />

                  <div className="relative z-10">
                    <p className="text-[11px] font-black text-slate-400 uppercase tracking-widest mb-6">Chi tiết đặt chỗ</p>

                    <div className="space-y-6 mb-10">
                      {selectedSeats.length === 0 ? (
                        <div className="py-12 bg-slate-50 rounded-[2.5rem] text-center border border-dashed border-slate-200">
                          <p className="text-xs font-bold text-slate-400 italic">Chưa có chỗ nào được chọn</p>
                        </div>
                      ) : (
                        <div className="space-y-3">
                          {selectedSeats.map((seat) => (
                            <div key={seat.id} className="flex items-center justify-between p-4 bg-slate-900 text-white rounded-2xl">
                              <div className="flex items-center gap-3">
                                <div className="w-8 h-8 bg-white/10 rounded-xl flex items-center justify-center text-[#1EB4D4]"><Train size={14} /></div>
                                <span className="text-xs font-black uppercase tracking-widest">{selectedCar?.carNumber} - {seat.seatNumber}</span>
                              </div>
                              <span className="text-sm font-black">{formatCurrency(basePrice + Number(seat.priceModifier || 0), detail?.segment?.currency)}</span>
                            </div>
                          ))}
                        </div>
                      )}

                      <div className="px-6 flex justify-between items-center text-center mx-auto">
                        <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Toàn bộ chi phí</span>
                        <p className="text-3xl font-black text-[#1EB4D4] tracking-tighter">{formatCurrency(totalPrice || 0, detail?.segment?.currency)}</p>
                      </div>
                    </div>

                    <button
                      type="button"
                      onClick={handleContinue}
                      disabled={selectedSeatIds.length === 0 || submitting}
                      className={`
                        w-full h-16 rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 transition-all
                        ${selectedSeatIds.length === 0 || submitting
                          ? 'bg-slate-100 text-slate-300 cursor-not-allowed'
                          : 'bg-slate-900 text-white shadow-xl shadow-slate-900/10 hover:bg-[#1EB4D4] hover:shadow-[#1EB4D4]/30'}
                      `}
                    >
                      {submitting ? 'Đang giữ chỗ...' : 'Tiếp tục hành trình'} <ArrowRight size={18} />
                    </button>
                  </div>
                </div>
              </aside>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
