import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { Armchair, Bus, ChevronRight, Info, ShieldCheck } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getBusTripDetail, getBusTripSeats, holdBusSeats } from '../../../services/busService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, formatTime, getSeatStatusClass, getSeatStatusLabel } from '../../tenant/bus/utils/presentation';

function buildSeatGrid(seatMap, seats) {
  const totalRows = Number(seatMap?.totalRows || 0);
  const totalColumns = Number(seatMap?.totalColumns || 0);
  const deckCount = Number(seatMap?.deckCount || 1);
  const grouped = [];

  for (let deck = 1; deck <= deckCount; deck += 1) {
    const rows = [];

    for (let rowIndex = 0; rowIndex < totalRows; rowIndex += 1) {
      const columns = [];

      for (let columnIndex = 0; columnIndex < totalColumns; columnIndex += 1) {
        const seat = seats.find((item) => item.deckIndex === deck && item.rowIndex === rowIndex && item.columnIndex === columnIndex) || null;
        columns.push(seat);
      }

      rows.push(columns);
    }

    grouped.push({ deck, rows });
  }

  return grouped;
}

const BusSeatSelectionPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const session = useAuthSession();
  const [detail, setDetail] = useState(null);
  const [seatData, setSeatData] = useState(null);
  const [selectedSeatIds, setSelectedSeatIds] = useState([]);
  const [submitting, setSubmitting] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const tripId = searchParams.get('tripId') || '';
  const fromTripStopTimeId = searchParams.get('fromTripStopTimeId') || '';
  const toTripStopTimeId = searchParams.get('toTripStopTimeId') || '';

  useEffect(() => {
    if (!tripId || !fromTripStopTimeId || !toTripStopTimeId) {
      setError('Thiếu thông tin chuyến hoặc chặng cần chọn ghế.');
      setLoading(false);
      return undefined;
    }

    let active = true;
    setLoading(true);
    setError('');

    Promise.all([
      getBusTripDetail(tripId, { fromTripStopTimeId, toTripStopTimeId }),
      getBusTripSeats(tripId, { fromTripStopTimeId, toTripStopTimeId }),
    ])
      .then(([detailResponse, seatResponse]) => {
        if (active) {
          setDetail(detailResponse);
          setSeatData(seatResponse);
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || 'Không tải được sơ đồ ghế.');
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

  const seatItems = seatData?.seats || [];
  const selectedSeats = seatItems.filter((seat) => selectedSeatIds.includes(seat.id));
  const basePrice = Number(detail?.segment?.price || 0);
  const totalPrice = selectedSeats.reduce((sum, seat) => sum + basePrice + Number(seat.priceModifier || 0), 0);
  const groupedDecks = buildSeatGrid(seatData?.seatMap, seatItems);

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
      const response = await holdBusSeats({
        tripId,
        fromTripStopTimeId,
        toTripStopTimeId,
        seatIds: selectedSeatIds,
      });

      const checkoutParams = new URLSearchParams({
        product: 'bus',
        tripId,
        fromTripStopTimeId,
        toTripStopTimeId,
        holdToken: response.holdToken,
        seatCount: String(selectedSeatIds.length),
      });

      navigate(`/checkout?${checkoutParams.toString()}`);
    } catch (err) {
      setError(err.message || 'Không giữ được ghế đã chọn.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-24">
        <div className="bg-slate-900 pt-32 pb-20">
          <div className="container mx-auto px-4">
            <div className="flex flex-col md:flex-row items-center justify-between gap-8">
              <div>
                <div className="flex items-center gap-3 text-[#1EB4D4] text-[10px] font-black uppercase tracking-[0.3em] mb-4">
                  <Bus size={14} />
                  <span>{detail?.provider?.name || detail?.tenant?.name || 'Sàn xe khách'}</span>
                </div>
                <h1 className="text-4xl font-black text-white tracking-tighter">Chọn chỗ ngồi ưa thích</h1>
              </div>
              <div className="flex items-center gap-8">
                <div className="text-center">
                  <p className="text-2xl font-black text-white leading-none">{formatTime(detail?.segment?.departureAt)}</p>
                  <p className="text-[10px] font-black text-white/40 uppercase tracking-widest mt-1">Khởi hành</p>
                </div>
                <div className="w-12 h-px bg-white/10" />
                <div className="text-center">
                  <p className="text-2xl font-black text-white leading-none">{formatTime(detail?.segment?.arrivalAt)}</p>
                  <p className="text-[10px] font-black text-white/40 uppercase tracking-widest mt-1">Đến nơi</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-10 relative z-10">
          {loading ? (
            <div className="bg-white p-12 rounded-[3.5rem] shadow-xl shadow-slate-200/50 border border-slate-100 text-center text-sm font-bold text-slate-500">
              Đang tải sơ đồ ghế...
            </div>
          ) : (
            <div className="flex flex-col lg:flex-row gap-12">
              <div className="flex-1 space-y-8">
                {error && (
                  <div className="rounded-[2rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
                    {error}
                  </div>
                )}

                <div className="bg-white p-12 rounded-[3.5rem] shadow-xl shadow-slate-200/50 border border-slate-100">
                  <div className="flex flex-wrap items-center justify-between gap-6 mb-12">
                    <h2 className="text-2xl font-black text-slate-900 tracking-tighter">Sơ đồ xe khách</h2>
                    <div className="flex gap-6 flex-wrap">
                      {['available', 'held_by_me', 'held', 'booked', 'inactive'].map((status) => (
                        <div key={status} className="flex items-center gap-3">
                          <div className={`w-5 h-5 rounded-lg ${getSeatStatusClass(status)}`} />
                          <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">
                            {getSeatStatusLabel(status)}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="grid grid-cols-1 xl:grid-cols-2 gap-10">
                    {groupedDecks.map((deck) => (
                      <div key={deck.deck} className="space-y-6">
                        <p className="text-center font-black text-slate-900 uppercase tracking-[0.2em] text-[10px] bg-slate-50 py-3 rounded-2xl">
                          {groupedDecks.length > 1 ? `Tầng ${deck.deck}` : 'Sơ đồ ghế'}
                        </p>
                        <div className="space-y-4 bg-slate-50 p-8 rounded-[2.5rem] border border-slate-100">
                          {deck.rows.map((columns, rowIndex) => (
                            <div
                              key={`${deck.deck}-${rowIndex}`}
                              className="grid gap-4"
                              style={{ gridTemplateColumns: `repeat(${columns.length}, minmax(0, 1fr))` }}
                            >
                              {columns.map((seat, columnIndex) => (
                                <button
                                  key={seat?.id || `${deck.deck}-${rowIndex}-${columnIndex}`}
                                  type="button"
                                  disabled={!seat || seat.status !== 'available'}
                                  onClick={() => toggleSeat(seat)}
                                  className={`
                                    h-20 rounded-2xl flex flex-col items-center justify-center transition-all relative group border-2
                                    ${!seat
                                      ? 'border-dashed border-slate-100 bg-transparent'
                                      : selectedSeatIds.includes(seat.id)
                                        ? 'bg-slate-900 text-white shadow-xl shadow-slate-900/20 border-slate-900'
                                        : seat.status === 'available'
                                          ? 'bg-white text-slate-600 hover:border-[#1EB4D4] border-transparent shadow-sm'
                                          : 'cursor-not-allowed border-transparent'}
                                    ${seat && seat.status !== 'available' ? getSeatStatusClass(seat.status) : ''}
                                  `}
                                >
                                  {seat ? (
                                    <>
                                      <div className={`p-2 rounded-lg mb-1 transition-colors ${
                                        selectedSeatIds.includes(seat.id)
                                          ? 'bg-white/10 text-[#1EB4D4]'
                                          : 'bg-slate-50 text-slate-400'
                                      }`}
                                      >
                                        <Armchair size={20} />
                                      </div>
                                      <span className="text-[10px] font-black uppercase tracking-tighter">{seat.seatNumber}</span>
                                    </>
                                  ) : null}
                                </button>
                              ))}
                            </div>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="bg-[#1EB4D4]/5 p-8 rounded-[2.5rem] border border-[#1EB4D4]/10 flex items-start gap-5">
                    <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center text-[#1EB4D4] shadow-sm shrink-0">
                      <Info size={20} />
                    </div>
                    <div>
                      <p className="font-black text-slate-900 uppercase tracking-widest text-[10px] mb-2">Thông báo giữ chỗ</p>
                      <p className="text-sm text-slate-600 font-medium leading-relaxed">
                        Ghế sẽ được giữ khi bạn bấm tiếp tục. Sau đó hệ thống sẽ chuyển sang bước thanh toán/đặt vé.
                      </p>
                    </div>
                  </div>
                  <div className="bg-slate-900 p-8 rounded-[2.5rem] border border-slate-800 flex items-start gap-5">
                    <div className="w-12 h-12 bg-white/10 rounded-2xl flex items-center justify-center text-[#1EB4D4] shadow-sm shrink-0">
                      <ShieldCheck size={20} />
                    </div>
                    <div>
                      <p className="font-black text-white uppercase tracking-widest text-[10px] mb-2">Giữ chỗ theo thời gian thực</p>
                      <p className="text-sm text-white/50 font-medium leading-relaxed">
                        Dữ liệu ghế được đồng bộ theo chuyến xe và chặng đang chọn để tránh trùng chỗ giữa nhiều khách hàng.
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <aside className="w-full lg:w-[400px]">
                <div className="sticky top-28 bg-white rounded-[3.5rem] shadow-2xl shadow-slate-200/60 border border-slate-100 p-10 overflow-hidden text-center">
                  <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16 -z-0" />

                  <div className="relative z-10 text-center">
                    <div className="w-20 h-20 bg-slate-50 rounded-[2rem] flex items-center justify-center text-[#1EB4D4] mx-auto mb-6">
                      <Bus size={32} />
                    </div>
                    <h3 className="font-black text-slate-900 text-2xl tracking-tighter mb-2">
                      {detail?.provider?.name || detail?.tenant?.name || 'Chuyến xe đang mở bán'}
                    </h3>
                    <p className="text-xs font-bold text-slate-400 mb-10">
                      {detail?.vehicleDetail?.busType || detail?.vehicle?.name || 'Xe khách'} • {detail?.trip?.code || 'Chuyến xe'}
                    </p>

                    <div className="space-y-6 mb-10">
                      <div className="flex justify-between items-center bg-slate-50 p-6 rounded-[2rem] gap-4">
                        <div className="text-left">
                          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Ghế đã chọn</p>
                          <p className="text-sm font-black text-slate-900 mt-1">
                            {selectedSeats.length > 0 ? selectedSeats.map((seat) => seat.seatNumber).join(', ') : 'Chưa chọn'}
                          </p>
                        </div>
                        <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm">
                          <Armchair size={18} />
                        </div>
                      </div>

                      <div className="px-6">
                        <div className="flex justify-between items-center mb-2">
                          <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Giá vé tạm tính</span>
                          <span className="text-lg font-black text-slate-900">{formatCurrency(totalPrice || 0, detail?.segment?.currency)}</span>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Phí nền tảng</span>
                          <span className="text-sm font-black text-green-500">Chưa tính ở bước này</span>
                        </div>
                      </div>

                      <div className="h-px bg-slate-50 mx-6" />

                      <div className="px-6 flex justify-between items-center text-center mx-auto">
                        <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Tổng cộng</span>
                        <p className="text-3xl font-black text-[#1EB4D4] tracking-tighter">
                          {formatCurrency(totalPrice || 0, detail?.segment?.currency)}
                        </p>
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
                      {submitting ? 'Đang giữ ghế...' : 'Tiếp tục đặt vé'}
                      <ChevronRight size={18} />
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
};

export default BusSeatSelectionPage;
