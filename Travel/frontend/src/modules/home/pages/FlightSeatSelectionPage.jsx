import React, { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { Plane, ChevronRight, Info, Check, ShieldCheck, Briefcase, Coffee } from 'lucide-react';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { getFlightOfferAncillaries, getFlightOfferDetails, getFlightSeatMapByOffer } from '../../../services/flightService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { formatCurrency, getAncillaryTypeLabel, getSeatStatusClass } from '../../tenant/flight/utils/presentation';

function buildSeatRows(seats) {
  const grouped = new Map();

  seats.forEach((seat) => {
    const rowIndex = Number(seat.rowIndex || 0);
    if (!grouped.has(rowIndex)) {
      grouped.set(rowIndex, []);
    }

    grouped.get(rowIndex).push(seat);
  });

  return [...grouped.entries()]
    .sort((left, right) => left[0] - right[0])
    .map(([rowIndex, items]) => ({
      rowIndex,
      seats: [...items].sort((left, right) => Number(left.columnIndex || 0) - Number(right.columnIndex || 0)),
    }));
}

function getSeatLabel(seat) {
  return seat?.seatNumber || `R${seat?.rowIndex || '-'}C${seat?.columnIndex || '-'}`;
}

export default function FlightSeatSelectionPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const session = useAuthSession();
  const [offerDetail, setOfferDetail] = useState(null);
  const [seatMap, setSeatMap] = useState(null);
  const [ancillaries, setAncillaries] = useState([]);
  const [selectedSegmentIndex, setSelectedSegmentIndex] = useState(0);
  const [selectedSeatId, setSelectedSeatId] = useState('');
  const [selectedAncillaryIds, setSelectedAncillaryIds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const offerId = searchParams.get('offerId') || '';

  useEffect(() => {
    if (!offerId) {
      setError('Thiếu thông tin offer chuyến bay để chọn ghế.');
      setLoading(false);
      return undefined;
    }

    let active = true;
    setLoading(true);
    setError('');

    Promise.all([
      getFlightOfferDetails(offerId),
      getFlightSeatMapByOffer(offerId),
      getFlightOfferAncillaries(offerId),
    ])
      .then(([detailResponse, seatMapResponse, ancillaryResponse]) => {
        if (!active) {
          return;
        }

        setOfferDetail(detailResponse);
        setSeatMap(seatMapResponse);
        setAncillaries(Array.isArray(ancillaryResponse?.items) ? ancillaryResponse.items : []);
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải sơ đồ ghế chuyến bay.');
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
  }, [offerId]);

  const segmentMaps = useMemo(() => {
    if (Array.isArray(seatMap?.segmentSeatMaps) && seatMap.segmentSeatMaps.length > 0) {
      return seatMap.segmentSeatMaps;
    }

    if (seatMap?.cabinSeatMap) {
      return [{
        segmentIndex: 0,
        cabinSeatMap: seatMap.cabinSeatMap,
        seats: Array.isArray(seatMap.seats) ? seatMap.seats : [],
        from: seatMap?.segments?.[0]?.from || null,
        to: seatMap?.segments?.[0]?.to || null,
      }];
    }

    return [];
  }, [seatMap]);

  const activeSegment = segmentMaps[selectedSegmentIndex] || segmentMaps[0] || null;
  const seatRows = useMemo(() => buildSeatRows(activeSegment?.seats || []), [activeSegment]);
  const selectedSeat = (activeSegment?.seats || []).find((item) => item.id === selectedSeatId) || null;
  const ancillaryItems = useMemo(() => ancillaries.filter((item) => item.isActive !== false), [ancillaries]);
  const selectedAncillaries = ancillaryItems.filter((item) => selectedAncillaryIds.includes(item.id));
  const ancillaryTotal = selectedAncillaries.reduce((sum, item) => sum + Number(item.price || 0), 0);
  const seatTotal = Number(selectedSeat?.priceModifier || 0);
  const totalPrice = Number(offerDetail?.offer?.totalPrice || 0) + ancillaryTotal + seatTotal;

  const toggleAncillary = (id) => {
    setSelectedAncillaryIds((current) => (
      current.includes(id)
        ? current.filter((item) => item !== id)
        : [...current, id]
    ));
  };

  const handleContinue = () => {
    if (!offerDetail) {
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

    const checkoutParams = new URLSearchParams({
      product: 'flight',
      offerId,
    });

    if (selectedSeat) {
      checkoutParams.set('seatId', selectedSeat.id);
      checkoutParams.set('seatNumber', getSeatLabel(selectedSeat));
      checkoutParams.set('seatPriceModifier', String(Number(selectedSeat.priceModifier || 0)));
    }

    if (selectedAncillaryIds.length > 0) {
      checkoutParams.set('ancillaryIds', selectedAncillaryIds.join(','));
    }

    navigate(`/checkout?${checkoutParams.toString()}`);
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pb-24">
        <div className="bg-slate-900 pt-32 pb-28">
          <div className="container mx-auto px-4">
            <div className="flex flex-col md:flex-row items-center justify-between gap-8">
              <div>
                <div className="flex items-center justify-center md:justify-start gap-3 text-[#1EB4D4] text-[10px] font-black uppercase tracking-[0.3em] mb-4 text-center">
                  <Plane size={14} className="-rotate-45" />
                  <span>
                    {(offerDetail?.segments?.[0]?.from?.iataCode || '---')} → {(offerDetail?.segments?.[offerDetail?.segments?.length - 1]?.to?.iataCode || '---')}
                  </span>
                </div>
                <h1 className="text-4xl font-black text-white tracking-tighter text-center md:text-left">Chọn ghế & dịch vụ</h1>
              </div>
              <div className="text-center md:text-right">
                <p className="text-2xl font-black text-white">{formatCurrency(offerDetail?.offer?.totalPrice || 0, offerDetail?.offer?.currencyCode)}</p>
                <p className="text-[10px] font-black text-white/40 uppercase tracking-widest mt-2">{offerDetail?.fareClass?.name || 'Fare class'}</p>
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 -mt-16 relative z-10">
          {loading ? (
            <div className="bg-white p-12 rounded-[3.5rem] shadow-xl shadow-slate-200/50 border border-slate-100 text-center text-sm font-bold text-slate-500">
              Đang tải sơ đồ ghế...
            </div>
          ) : error ? (
            <div className="bg-white p-12 rounded-[3.5rem] shadow-sm border border-rose-100 text-center text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : (
            <div className="flex flex-col lg:flex-row gap-12">
              <div className="flex-1 space-y-8">
                <div className="bg-white p-12 rounded-[4rem] shadow-xl shadow-slate-200/50 border border-slate-100 flex flex-col items-center">
                  <div className="w-64 h-24 bg-slate-50 rounded-t-[5rem] border-x border-t border-slate-100 flex flex-col items-center justify-center mb-12">
                    <Plane size={24} className="text-slate-200 -rotate-90" />
                    <p className="text-[10px] text-slate-300 font-black uppercase tracking-widest mt-2">Mũi máy bay</p>
                  </div>

                  {segmentMaps.length > 1 ? (
                    <div className="w-full flex flex-wrap justify-center gap-3 mb-8">
                      {segmentMaps.map((item, index) => (
                        <button key={`${item.segmentIndex}-${item.flightNumber || index}`} type="button" onClick={() => { setSelectedSegmentIndex(index); setSelectedSeatId(''); }} className={`px-5 py-3 rounded-2xl text-xs font-black uppercase tracking-widest transition-all ${selectedSegmentIndex === index ? 'bg-slate-900 text-white' : 'bg-slate-50 text-slate-500'}`}>
                          {(item.from?.iataCode || item.from?.code || '---')} → {(item.to?.iataCode || item.to?.code || '---')}
                        </button>
                      ))}
                    </div>
                  ) : null}

                  {seatRows.length === 0 ? (
                    <div className="w-full py-20 bg-slate-50 rounded-[3rem] text-center border-2 border-dashed border-slate-200">
                      <p className="text-lg font-black text-slate-900">Tenant chưa gắn sơ đồ cabin cho offer này.</p>
                      <p className="text-sm font-medium text-slate-400 mt-2">Bạn vẫn có thể tiếp tục checkout mà không chọn ghế cụ thể.</p>
                    </div>
                  ) : (
                    <div className="space-y-4 w-full max-w-4xl">
                      {seatRows.map((row) => (
                        <div key={row.rowIndex} className="flex gap-3 items-center justify-center">
                          <span className="w-10 text-[10px] font-black text-slate-100 bg-slate-400/20 rounded-lg h-12 flex items-center justify-center shrink-0">{row.rowIndex}</span>
                          {row.seats.map((seat) => {
                            const status = seat.status || 'available';
                            const isSelected = selectedSeatId === seat.id;
                            const isDisabled = status !== 'available' && !isSelected;

                            return (
                              <button key={seat.id} type="button" disabled={isDisabled} onClick={() => setSelectedSeatId((current) => (current === seat.id ? '' : seat.id))} className={`w-12 h-12 rounded-xl text-[10px] font-black border-2 transition-all flex items-center justify-center ${isSelected ? 'bg-slate-900 border-slate-900 text-white shadow-xl scale-110' : status === 'available' ? 'bg-slate-50 border-slate-100 text-slate-900 hover:border-[#1EB4D4]' : getSeatStatusClass(status)}`}>
                                {getSeatLabel(seat)}
                              </button>
                            );
                          })}
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div className="bg-white rounded-[3rem] p-10 shadow-xl shadow-slate-200/50 border border-slate-100">
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-6">Dịch vụ bổ sung</p>
                  <div className="space-y-4">
                    {ancillaryItems.length === 0 ? (
                      <div className="p-6 rounded-[2rem] bg-slate-50 border border-dashed border-slate-200 text-sm font-bold text-slate-500">Offer này hiện chưa có dịch vụ bổ sung.</div>
                    ) : ancillaryItems.map((item) => {
                      const active = selectedAncillaryIds.includes(item.id);

                      return (
                        <button key={item.id} type="button" onClick={() => toggleAncillary(item.id)} className={`w-full flex items-center gap-4 p-5 rounded-[1.8rem] border-2 transition-all ${active ? 'bg-white border-[#1EB4D4] shadow-lg' : 'bg-slate-50 border-transparent hover:border-slate-200'}`}>
                          <div className={`w-10 h-10 rounded-2xl flex items-center justify-center transition-all ${active ? 'bg-[#1EB4D4] text-white' : 'bg-white text-slate-400 shadow-sm'}`}>
                            {item.type === 'Meal' ? <Coffee size={16} /> : <Briefcase size={16} />}
                          </div>
                          <div className="flex-1 text-left">
                            <p className="text-xs font-black text-slate-900 uppercase tracking-widest leading-none mb-1">{item.name}</p>
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{getAncillaryTypeLabel(item.type)}</p>
                            <p className="text-xs font-black text-[#1EB4D4] tracking-tighter">{formatCurrency(item.price, item.currencyCode)}</p>
                          </div>
                          <div className={`w-6 h-6 rounded-full border-2 flex items-center justify-center transition-all ${active ? 'bg-[#1EB4D4] border-[#1EB4D4] text-white' : 'border-slate-200 text-transparent'}`}>
                            <Check size={14} />
                          </div>
                        </button>
                      );
                    })}
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="p-8 bg-slate-900 rounded-[3rem] text-white flex gap-6">
                    <div className="w-12 h-12 bg-white/10 rounded-2xl flex items-center justify-center shrink-0 text-[#1EB4D4]"><ShieldCheck size={24} /></div>
                    <div>
                      <p className="text-[10px] font-black uppercase tracking-[0.2em] mb-2 text-[#1EB4D4]">Không giữ ghế giả lập</p>
                      <p className="text-xs font-bold text-white/60 leading-relaxed">Phase Flight đang dùng trạng thái ghế realtime từ tenant và chuyển thẳng sang checkout để xác nhận lựa chọn.</p>
                    </div>
                  </div>
                  <div className="p-8 bg-blue-50/50 rounded-[3rem] border border-blue-100 flex gap-6">
                    <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center shrink-0 text-blue-500 shadow-sm"><Info size={24} /></div>
                    <div>
                      <p className="text-[10px] font-black uppercase tracking-[0.2em] mb-2 text-blue-500">Lưu ý</p>
                      <p className="text-xs font-bold text-blue-900/40 leading-relaxed">Bạn có thể tiếp tục mà không chọn ghế. Ancillary được cộng thẳng vào tổng giá tạm tính ở checkout.</p>
                    </div>
                  </div>
                </div>
              </div>

              <aside className="w-full lg:w-[400px] space-y-8">
                <div className="bg-white rounded-[3rem] p-10 shadow-2xl shadow-slate-300/40 border border-slate-100 relative overflow-hidden">
                  <div className="absolute top-0 right-0 w-32 h-32 bg-blue-50/50 rounded-full translate-x-16 -translate-y-16 -z-0" />

                  <div className="relative z-10">
                    {selectedSeat ? (
                      <div className="bg-slate-900 rounded-[2rem] p-6 text-white flex items-center gap-5 mb-8">
                        <div className="w-14 h-14 bg-white/10 rounded-2xl flex items-center justify-center text-lg font-black">{getSeatLabel(selectedSeat)}</div>
                        <div>
                          <p className="text-[10px] font-black uppercase tracking-widest text-[#1EB4D4] mb-1">Ghế đã chọn</p>
                          <p className="text-xl font-black tracking-tighter">Ghế {getSeatLabel(selectedSeat)}</p>
                        </div>
                      </div>
                    ) : (
                      <div className="bg-slate-50 border-2 border-dashed border-slate-200 rounded-[2rem] p-8 text-center mb-8">
                        <p className="text-xs font-black text-slate-400 uppercase tracking-widest">Chưa chọn ghế cụ thể</p>
                      </div>
                    )}

                    <div className="space-y-4 mb-8">
                      <div className="flex justify-between items-center text-xs">
                        <span className="font-bold text-slate-400 uppercase tracking-widest">Giá vé</span>
                        <span className="font-black text-slate-900">{formatCurrency(offerDetail?.offer?.totalPrice || 0, offerDetail?.offer?.currencyCode)}</span>
                      </div>
                      <div className="flex justify-between items-center text-xs">
                        <span className="font-bold text-slate-400 uppercase tracking-widest">Ghế chọn</span>
                        <span className="font-black text-slate-900">{formatCurrency(selectedSeat?.priceModifier || 0, offerDetail?.offer?.currencyCode)}</span>
                      </div>
                      <div className="flex justify-between items-center text-xs">
                        <span className="font-bold text-slate-400 uppercase tracking-widest">Dịch vụ thêm</span>
                        <span className="font-black text-slate-900">{formatCurrency(ancillaryTotal, offerDetail?.offer?.currencyCode)}</span>
                      </div>
                    </div>

                    <div className="px-4 pb-6 flex justify-between items-center">
                      <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Tổng chi phí</span>
                      <p className="text-4xl font-black text-[#1EB4D4] tracking-tighter">{formatCurrency(totalPrice, offerDetail?.offer?.currencyCode)}</p>
                    </div>

                    <button type="button" onClick={handleContinue} disabled={!offerDetail} className={`w-full h-16 rounded-[1.5rem] font-black text-xs uppercase tracking-[0.2em] flex items-center justify-center gap-3 transition-all ${offerDetail ? 'bg-slate-900 text-white shadow-xl shadow-slate-900/10 hover:bg-[#1EB4D4] hover:shadow-[#1EB4D4]/30' : 'bg-slate-100 text-slate-300 cursor-not-allowed'}`}>
                      Tiếp tục checkout <ChevronRight size={18} />
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
