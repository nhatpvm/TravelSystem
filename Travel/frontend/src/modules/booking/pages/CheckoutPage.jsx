import React, { useEffect, useMemo, useRef, useState } from 'react';
import {
  User,
  Mail,
  Phone,
  Ticket,
  FileText,
  CheckCircle2,
  ChevronRight,
  ShieldCheck,
  Clock,
  Plane,
  Hotel,
  Users,
} from 'lucide-react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import MainLayout from '../../../shared/components/layouts/MainLayout';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { getBusTripDetail } from '../../../services/busService';
import { getTrainTripDetail } from '../../../services/trainService';
import { getFlightOfferAncillaries, getFlightOfferDetails } from '../../../services/flightService';
import { getPublicTourById, quoteTour } from '../../../services/tourService';
import {
  createCustomerOrder,
  deleteCheckoutDraft,
  listCheckoutDrafts,
  listSavedPassengers,
  markCheckoutDraftResumed,
  upsertCheckoutDraft,
} from '../../../services/customerCommerceService';
import { formatCurrency, formatDateTime, formatTime } from '../../tenant/train/utils/presentation';

function buildPassengerTypes(product, seatCount, adults, children) {
  if (product === 'hotel' || product === 'tour') {
    return [
      ...Array.from({ length: Math.max(1, adults) }, () => 'adult'),
      ...Array.from({ length: Math.max(0, children) }, () => 'child'),
    ];
  }

  if (product === 'flight') {
    return ['adult'];
  }

  return Array.from({ length: Math.max(1, seatCount) }, () => 'adult');
}

function syncPassengers(existing, passengerTypes) {
  return passengerTypes.map((type, index) => ({
    fullName: existing[index]?.fullName || '',
    passengerType: type,
    gender: existing[index]?.gender || '',
    dateOfBirth: existing[index]?.dateOfBirth || '',
    nationalityCode: existing[index]?.nationalityCode || '',
    idNumber: existing[index]?.idNumber || '',
    passportNumber: existing[index]?.passportNumber || '',
    email: existing[index]?.email || '',
    phoneNumber: existing[index]?.phoneNumber || '',
    notes: existing[index]?.notes || '',
  }));
}

function mapSavedPassengerType(passenger) {
  const value = Number(passenger?.passengerType || 0);
  if (value === 2) {
    return 'child';
  }

  if (value === 3) {
    return 'infant';
  }

  return 'adult';
}

function getPassengerCompleteness(passenger) {
  return [
    passenger?.fullName,
    passenger?.dateOfBirth,
    passenger?.idNumber,
    passenger?.passportNumber,
    passenger?.email,
    passenger?.phoneNumber,
  ].filter((item) => String(item || '').trim().length > 0).length;
}

function sortSavedPassengers(items) {
  return [...items].sort((left, right) => {
    if (Boolean(left?.isDefault) !== Boolean(right?.isDefault)) {
      return left?.isDefault ? -1 : 1;
    }

    const completenessDiff = getPassengerCompleteness(right) - getPassengerCompleteness(left);
    if (completenessDiff !== 0) {
      return completenessDiff;
    }

    return String(left?.fullName || '').localeCompare(String(right?.fullName || ''), 'vi');
  });
}

function buildPassengerAssignments(savedPassengers, passengerTypes) {
  const orderedPassengers = sortSavedPassengers(savedPassengers);
  const remaining = [...orderedPassengers];

  return passengerTypes.map((targetType) => {
    const sameTypeIndex = remaining.findIndex((item) => mapSavedPassengerType(item) === targetType);
    const fallbackIndex = remaining.findIndex(() => true);
    const selectedIndex = sameTypeIndex >= 0 ? sameTypeIndex : fallbackIndex;

    if (selectedIndex < 0) {
      return null;
    }

    const [selected] = remaining.splice(selectedIndex, 1);
    return selected || null;
  });
}

function mergePassengerDraft(existingPassenger, savedPassenger, fallbackPassenger) {
  if (!savedPassenger) {
    return existingPassenger;
  }

  return {
    ...existingPassenger,
    fullName: existingPassenger.fullName || savedPassenger.fullName || '',
    passengerType: existingPassenger.passengerType,
    gender: existingPassenger.gender || savedPassenger.gender || '',
    dateOfBirth: existingPassenger.dateOfBirth || savedPassenger.dateOfBirth || '',
    nationalityCode: existingPassenger.nationalityCode || savedPassenger.nationalityCode || '',
    idNumber: existingPassenger.idNumber || savedPassenger.idNumber || '',
    passportNumber: existingPassenger.passportNumber || savedPassenger.passportNumber || '',
    email: existingPassenger.email || savedPassenger.email || fallbackPassenger?.email || '',
    phoneNumber: existingPassenger.phoneNumber || savedPassenger.phoneNumber || fallbackPassenger?.phoneNumber || '',
    notes: existingPassenger.notes || savedPassenger.notes || '',
  };
}

function buildCheckoutKey(parts) {
  const search = new URLSearchParams();

  Object.entries(parts).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return;
    }

    search.set(key, String(value));
  });

  return search.toString();
}

function normalizeDraftPassenger(passenger) {
  return {
    fullName: passenger?.fullName || '',
    passengerType: passenger?.passengerType || 'adult',
    gender: passenger?.gender || '',
    dateOfBirth: passenger?.dateOfBirth || '',
    nationalityCode: passenger?.nationalityCode || '',
    idNumber: passenger?.idNumber || '',
    passportNumber: passenger?.passportNumber || '',
    email: passenger?.email || '',
    phoneNumber: passenger?.phoneNumber || '',
    notes: passenger?.notes || '',
  };
}

function parseCheckoutDraftSnapshot(snapshot) {
  if (!snapshot || typeof snapshot !== 'object') {
    return null;
  }

  return {
    useVAT: Boolean(snapshot.useVAT),
    contact: {
      fullName: snapshot.contact?.fullName || '',
      phone: snapshot.contact?.phone || '',
      email: snapshot.contact?.email || '',
      note: snapshot.contact?.note || '',
      companyName: snapshot.contact?.companyName || '',
      taxCode: snapshot.contact?.taxCode || '',
      companyAddress: snapshot.contact?.companyAddress || '',
      invoiceEmail: snapshot.contact?.invoiceEmail || '',
    },
    passengers: Array.isArray(snapshot.passengers)
      ? snapshot.passengers.map(normalizeDraftPassenger)
      : [],
  };
}

function getTrainRoute(detail) {
  const stops = detail?.stops || [];
  const origin = stops.find((item) => item.isSelectedOrigin) || stops[0];
  const destination = stops.find((item) => item.isSelectedDestination) || stops[stops.length - 1];

  return {
    from: origin?.location?.name || origin?.stopPoint?.name || 'Ga đi',
    to: destination?.location?.name || destination?.stopPoint?.name || 'Ga đến',
  };
}

function getBusRoute(detail) {
  const stops = detail?.stops || [];
  const origin = stops.find((item) => item.isSelectedOrigin) || stops[0];
  const destination = stops.find((item) => item.isSelectedDestination) || stops[stops.length - 1];

  return {
    from: origin?.location?.name || origin?.stopPoint?.name || 'Điểm đi',
    to: destination?.location?.name || destination?.stopPoint?.name || 'Điểm đến',
  };
}

function getFlightRoute(detail) {
  const segments = detail?.segments || [];
  const first = segments[0];
  const last = segments[segments.length - 1];

  return {
    from: first?.from?.name || 'Sân bay đi',
    to: last?.to?.name || 'Sân bay đến',
    fromCode: first?.from?.iataCode || first?.from?.code || '---',
    toCode: last?.to?.iataCode || last?.to?.code || '---',
    departureAt: first?.departureAt,
    arrivalAt: last?.arrivalAt,
  };
}

function formatPassengerTypeLabel(type) {
  return type === 'child' ? 'Trẻ em' : 'Người lớn';
}

function normalizePassengerPayload(passenger) {
  return {
    fullName: passenger.fullName.trim(),
    passengerType: passenger.passengerType,
    gender: passenger.gender || undefined,
    dateOfBirth: passenger.dateOfBirth || undefined,
    nationalityCode: passenger.nationalityCode || undefined,
    idNumber: passenger.idNumber || undefined,
    passportNumber: passenger.passportNumber || undefined,
    email: passenger.email || undefined,
    phoneNumber: passenger.phoneNumber || undefined,
    notes: passenger.notes || undefined,
  };
}

const CheckoutPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const session = useAuthSession();
  const { user, isAuthenticated } = session;

  const product = searchParams.get('product') || (searchParams.get('type') === 'tour' ? 'tour' : '');
  const isTour = product === 'tour';
  const isBus = product === 'bus';
  const isTrain = product === 'train';
  const isFlight = product === 'flight';
  const isHotel = product === 'hotel';

  const tripId = searchParams.get('tripId') || '';
  const fromTripStopTimeId = searchParams.get('fromTripStopTimeId') || '';
  const toTripStopTimeId = searchParams.get('toTripStopTimeId') || '';
  const holdToken = searchParams.get('holdToken') || '';
  const seatCount = Math.max(1, Number(searchParams.get('seatCount') || 1));

  const offerId = searchParams.get('offerId') || '';
  const seatNumber = searchParams.get('seatNumber') || '';
  const seatPriceModifier = Number(searchParams.get('seatPriceModifier') || 0);
  const ancillaryIds = (searchParams.get('ancillaryIds') || '').split(',').filter(Boolean);

  const hotelId = searchParams.get('hotelId') || '';
  const roomTypeId = searchParams.get('roomTypeId') || '';
  const ratePlanId = searchParams.get('ratePlanId') || '';
  const tourId = searchParams.get('tourId') || '';
  const scheduleId = searchParams.get('scheduleId') || '';
  const packageId = searchParams.get('packageId') || '';
  const hotelName = searchParams.get('hotelName') || 'Khách sạn';
  const roomTypeName = searchParams.get('roomTypeName') || 'Loại phòng';
  const ratePlanName = searchParams.get('ratePlanName') || 'Gói giá';
  const checkInDate = searchParams.get('checkInDate') || '';
  const checkOutDate = searchParams.get('checkOutDate') || '';
  const roomCount = Math.max(1, Number(searchParams.get('rooms') || 1));
  const adultCount = Math.max(1, Number(searchParams.get('adult') || 1));
  const childCount = Math.max(0, Number(searchParams.get('child') || 0));
  const hotelTotalPrice = Number(searchParams.get('totalPrice') || 0);
  const hotelCurrency = searchParams.get('currencyCode') || 'VND';

  const passengerTypes = useMemo(
    () => buildPassengerTypes(product, seatCount, adultCount, childCount),
    [adultCount, childCount, product, seatCount],
  );

  const [useVAT, setUseVAT] = useState(false);
  const [contact, setContact] = useState({
    fullName: '',
    phone: '',
    email: '',
    note: '',
    companyName: '',
    taxCode: '',
    companyAddress: '',
    invoiceEmail: '',
  });
  const [passengers, setPassengers] = useState(() => syncPassengers([], passengerTypes));
  const [savedPassengers, setSavedPassengers] = useState([]);
  const [savedPassengersLoading, setSavedPassengersLoading] = useState(false);
  const [autoFilledSavedPassengers, setAutoFilledSavedPassengers] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [sourceDetail, setSourceDetail] = useState(null);
  const [flightAncillaries, setFlightAncillaries] = useState([]);
  const [tourQuote, setTourQuote] = useState(null);
  const [draftReady, setDraftReady] = useState(!isAuthenticated);
  const [currentDraftId, setCurrentDraftId] = useState('');
  const [draftStatus, setDraftStatus] = useState('');
  const restoredDraftRef = useRef(false);

  useEffect(() => {
    setContact((current) => ({
      ...current,
      fullName: current.fullName || user?.fullName || '',
      phone: current.phone || user?.phoneNumber || '',
      email: current.email || user?.email || '',
      invoiceEmail: current.invoiceEmail || user?.email || '',
    }));
  }, [user]);

  useEffect(() => {
    setPassengers((current) => syncPassengers(current, passengerTypes));
  }, [passengerTypes]);

  useEffect(() => {
    if (!isAuthenticated) {
      setSavedPassengers([]);
      setAutoFilledSavedPassengers(false);
      return undefined;
    }

    let active = true;
    setSavedPassengersLoading(true);

    listSavedPassengers()
      .then((items) => {
        if (active) {
          setSavedPassengers(Array.isArray(items) ? items : []);
          setAutoFilledSavedPassengers(false);
        }
      })
      .catch(() => {
        if (active) {
          setSavedPassengers([]);
        }
      })
      .finally(() => {
        if (active) {
          setSavedPassengersLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated || autoFilledSavedPassengers || savedPassengers.length === 0) {
      return;
    }

    applySavedPassengersToState();
    setAutoFilledSavedPassengers(true);
  }, [autoFilledSavedPassengers, isAuthenticated, savedPassengers, user, passengerTypes]);

  useEffect(() => {
    if (isHotel || !product) {
      return undefined;
    }

    let active = true;
    setDetailLoading(true);
    setError('');

    const request = isBus
      ? Promise.all([
        getBusTripDetail(tripId, { fromTripStopTimeId, toTripStopTimeId }),
      ]).then(([detail]) => ({ detail, ancillaries: [] }))
      : isTrain
        ? Promise.all([
          getTrainTripDetail(tripId, { fromTripStopTimeId, toTripStopTimeId }),
        ]).then(([detail]) => ({ detail, ancillaries: [] }))
        : isFlight
          ? Promise.all([
            getFlightOfferDetails(offerId),
            getFlightOfferAncillaries(offerId),
          ]).then(([detail, ancillaryResponse]) => ({
            detail,
            ancillaries: Array.isArray(ancillaryResponse?.items) ? ancillaryResponse.items : [],
          }))
          : Promise.all([
            getPublicTourById(tourId),
            quoteTour(tourId, {
              scheduleId,
              packageId: packageId || undefined,
              includeDefaultAddons: true,
              includeDefaultPackageOptions: true,
              paxGroups: [
                ...(adultCount > 0 ? [{ priceType: 1, quantity: adultCount }] : []),
                ...(childCount > 0 ? [{ priceType: 2, quantity: childCount }] : []),
              ],
            }),
          ]).then(([detail, quote]) => ({
            detail,
            ancillaries: [],
            quote,
          }));

    request
      .then((response) => {
        if (!active) {
          return;
        }

        setSourceDetail(response.detail);
        setFlightAncillaries(response.ancillaries || []);
        setTourQuote(response.quote || null);
      })
      .catch((requestError) => {
        if (!active) {
          return;
        }

        setError(requestError.message || 'Không tải được thông tin dịch vụ cho bước checkout.');
      })
      .finally(() => {
        if (active) {
          setDetailLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [adultCount, childCount, fromTripStopTimeId, isBus, isFlight, isHotel, isTour, isTrain, offerId, packageId, product, scheduleId, toTripStopTimeId, tourId, tripId]);

  const busRoute = useMemo(() => getBusRoute(sourceDetail), [sourceDetail]);
  const trainRoute = useMemo(() => getTrainRoute(sourceDetail), [sourceDetail]);
  const flightRoute = useMemo(() => getFlightRoute(sourceDetail), [sourceDetail]);
  const selectedTourSchedule = useMemo(
    () => sourceDetail?.upcomingSchedules?.find((item) => item.id === scheduleId) || null,
    [scheduleId, sourceDetail],
  );
  const selectedAncillaries = useMemo(
    () => flightAncillaries.filter((item) => ancillaryIds.includes(item.id)),
    [ancillaryIds, flightAncillaries],
  );

  const subtotal = useMemo(() => {
    if (isBus) {
      return Number(sourceDetail?.segment?.price || 0) * seatCount;
    }

    if (isTrain) {
      return Number(sourceDetail?.segment?.price || 0) * seatCount;
    }

    if (isFlight) {
      return Number(sourceDetail?.offer?.totalPrice || 0)
        + seatPriceModifier
        + selectedAncillaries.reduce((sum, item) => sum + Number(item.price || 0), 0);
    }

    if (isHotel) {
      return hotelTotalPrice;
    }

    if (isTour) {
      return Number(tourQuote?.totalAmount || 0);
    }

    return 0;
  }, [hotelTotalPrice, isBus, isFlight, isHotel, isTour, isTrain, seatCount, seatPriceModifier, selectedAncillaries, sourceDetail, tourQuote]);

  const currency = isFlight
    ? sourceDetail?.offer?.currencyCode || 'VND'
    : isHotel
      ? hotelCurrency
      : sourceDetail?.segment?.currency || 'VND';

  const pageTitle = isBus
    ? 'Xác nhận vé xe khách'
    : isTrain
      ? 'Xác nhận vé tàu hỏa'
      : isFlight
        ? 'Xác nhận vé máy bay'
        : isHotel
          ? 'Xác nhận đặt phòng'
          : isTour
            ? 'Xác nhận đặt tour'
            : 'Xác nhận thanh toán';

  const checkoutKey = useMemo(() => buildCheckoutKey({
    product,
    tripId,
    fromTripStopTimeId,
    toTripStopTimeId,
    holdToken,
    seatCount,
    offerId,
    seatNumber,
    seatPriceModifier,
    ancillaryIds: ancillaryIds.join(','),
    hotelId,
    roomTypeId,
    ratePlanId,
    tourId,
    scheduleId,
    packageId,
    checkInDate,
    checkOutDate,
    roomCount,
    adultCount,
    childCount,
    totalPrice: hotelTotalPrice,
  }), [
    product,
    tripId,
    fromTripStopTimeId,
    toTripStopTimeId,
    holdToken,
    seatCount,
    offerId,
    seatNumber,
    seatPriceModifier,
    ancillaryIds,
    hotelId,
    roomTypeId,
    ratePlanId,
    tourId,
    scheduleId,
    packageId,
    checkInDate,
    checkOutDate,
    roomCount,
    adultCount,
    childCount,
    hotelTotalPrice,
  ]);

  const draftTitle = isBus
    ? `${busRoute.from} - ${busRoute.to}`
    : isTrain
      ? `${trainRoute.from} - ${trainRoute.to}`
      : isFlight
        ? `${flightRoute.fromCode} - ${flightRoute.toCode}`
        : isHotel
          ? hotelName
          : sourceDetail?.name || 'Tour du lich';

  const draftSubtitle = isBus
    ? sourceDetail?.provider?.name || 'Checkout xe khach'
    : isTrain
      ? sourceDetail?.provider?.name || 'Checkout tau hoa'
      : isFlight
        ? sourceDetail?.flight?.flightNumber || 'Checkout may bay'
        : isHotel
          ? `${roomTypeName} - ${ratePlanName}`
          : tourQuote?.package?.packageName || sourceDetail?.province || 'Checkout tour';

  const draftResumeUrl = `${location.pathname}${location.search}`;

  function applySavedPassengersToState() {
    if (!savedPassengers.length) {
      return;
    }

    const orderedPassengers = sortSavedPassengers(savedPassengers);
    const primaryPassenger = orderedPassengers[0];
    const assignments = buildPassengerAssignments(savedPassengers, passengerTypes);

    setContact((current) => ({
      ...current,
      fullName: current.fullName || primaryPassenger?.fullName || user?.fullName || '',
      phone: current.phone || primaryPassenger?.phoneNumber || user?.phoneNumber || '',
      email: current.email || primaryPassenger?.email || user?.email || '',
      invoiceEmail: current.invoiceEmail || primaryPassenger?.email || user?.email || '',
    }));

    setPassengers((current) => current.map((item, index) => mergePassengerDraft(
      item,
      assignments[index],
      primaryPassenger,
    )));
  }

  useEffect(() => {
    if (!isAuthenticated || !checkoutKey) {
      restoredDraftRef.current = false;
      setCurrentDraftId('');
      setDraftStatus('');
      setDraftReady(true);
      return;
    }

    let active = true;
    setDraftReady(false);

    listCheckoutDrafts({ checkoutKey, limit: 1 })
      .then(async (items) => {
        if (!active) {
          return;
        }

        const draft = Array.isArray(items) ? items[0] : null;
        setCurrentDraftId(draft?.id || '');

        if (!draft || restoredDraftRef.current) {
          return;
        }

        const parsedSnapshot = parseCheckoutDraftSnapshot(draft.snapshot);
        if (!parsedSnapshot) {
          return;
        }

        setUseVAT(Boolean(parsedSnapshot.useVAT));
        setContact((current) => ({
          ...current,
          ...parsedSnapshot.contact,
        }));
        setPassengers(syncPassengers(parsedSnapshot.passengers || [], passengerTypes));
        restoredDraftRef.current = true;
        setDraftStatus('Da khoi phuc checkout dang do gan nhat.');

        try {
          await markCheckoutDraftResumed(draft.id);
        } catch {
          // Ignore resume counter errors to keep restore flow smooth.
        }
      })
      .catch(() => {
        if (active) {
          setCurrentDraftId('');
        }
      })
      .finally(() => {
        if (active) {
          setDraftReady(true);
        }
      });

    return () => {
      active = false;
    };
  }, [checkoutKey, isAuthenticated, passengerTypes]);

  useEffect(() => {
    if (!isAuthenticated || !draftReady || !checkoutKey || !product) {
      return undefined;
    }

    const snapshotJson = JSON.stringify({
      useVAT,
      contact,
      passengers,
    });

    const timer = window.setTimeout(() => {
      upsertCheckoutDraft({
        productType: product,
        checkoutKey,
        title: draftTitle,
        subtitle: draftSubtitle,
        resumeUrl: draftResumeUrl,
        snapshotJson,
      })
        .then((draft) => {
          setCurrentDraftId(draft?.id || '');
        })
        .catch(() => {
          // Keep checkout flow usable if autosave fails.
        });
    }, 900);

    return () => {
      window.clearTimeout(timer);
    };
  }, [checkoutKey, contact, draftReady, draftResumeUrl, draftSubtitle, draftTitle, isAuthenticated, passengers, product, useVAT]);

  function handlePassengerChange(index, field, value) {
    setPassengers((current) => current.map((item, itemIndex) => (
      itemIndex === index
        ? { ...item, [field]: value }
        : item
    )));
  }

  function applySavedPassengers() {
    if (!savedPassengers.length) {
      return;
    }

    applySavedPassengersToState();
    setAutoFilledSavedPassengers(true);
    setDraftStatus('Da dien nhanh thong tin tu danh sach hanh khach da luu.');
  }

  async function handleSubmit() {
    if (!isAuthenticated) {
      navigate('/auth/login', {
        state: {
          returnTo: `${location.pathname}${location.search}`,
        },
      });
      return;
    }

    if (!contact.fullName.trim() || !contact.phone.trim() || !contact.email.trim()) {
      setError('Vui lòng nhập đầy đủ họ tên, số điện thoại và email liên hệ.');
      return;
    }

    if (passengers.some((item) => !item.fullName.trim())) {
      setError('Vui lòng nhập họ tên cho tất cả hành khách trước khi tiếp tục.');
      return;
    }

    const payload = {
      productType: product,
      tripId: tripId || undefined,
      offerId: offerId || undefined,
      hotelId: hotelId || undefined,
      roomTypeId: roomTypeId || undefined,
      ratePlanId: ratePlanId || undefined,
      tourId: tourId || undefined,
      scheduleId: scheduleId || undefined,
      packageId: packageId || tourQuote?.package?.packageId || undefined,
      seatId: searchParams.get('seatId') || undefined,
      ancillaryIds,
      holdToken: holdToken || undefined,
      adultCount,
      childCount,
      roomCount,
      checkInDate: checkInDate || undefined,
      checkOutDate: checkOutDate || undefined,
      contact: {
        fullName: contact.fullName.trim(),
        phone: contact.phone.trim(),
        email: contact.email.trim(),
      },
      passengers: passengers.map(normalizePassengerPayload),
      vat: useVAT ? {
        companyName: contact.companyName.trim(),
        taxCode: contact.taxCode.trim(),
        companyAddress: contact.companyAddress.trim(),
        invoiceEmail: (contact.invoiceEmail || contact.email).trim(),
      } : undefined,
      customerNote: contact.note.trim() || undefined,
    };

    setSubmitting(true);
    setError('');

    try {
      const order = await createCustomerOrder(payload);
      if (currentDraftId) {
        deleteCheckoutDraft(currentDraftId).catch(() => {});
      }
      navigate(`/payment?orderCode=${encodeURIComponent(order.orderCode)}`);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tạo đơn hàng để chuyển sang bước thanh toán.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-slate-50 pt-32 pb-20">
        <div className="container mx-auto px-4 max-w-6xl">
          <h1 className="text-3xl font-black text-slate-900 mb-8">{pageTitle}</h1>

          {draftStatus ? (
            <div className="mb-6 rounded-[2rem] border border-sky-100 bg-sky-50 px-6 py-4 text-sm font-bold text-sky-700">
              {draftStatus}
            </div>
          ) : null}

          {error ? (
            <div className="mb-6 rounded-[2rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : null}

          <div className="flex flex-col lg:flex-row gap-12">
            <div className="flex-1 space-y-8">
              <section className="bg-white p-8 rounded-[2.5rem] shadow-sm border border-slate-100">
                <div className="flex items-center gap-3 mb-8">
                  <div className="w-10 h-10 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center font-bold">1</div>
                  <h2 className="text-xl font-bold text-slate-900">Thông tin liên hệ</h2>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="space-y-2">
                    <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Họ và tên</label>
                    <div className="relative">
                      <User className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={contact.fullName} onChange={(event) => setContact((current) => ({ ...current, fullName: event.target.value }))} type="text" placeholder="Nhập họ và tên" className="w-full pl-12 pr-4 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all" />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Số điện thoại</label>
                    <div className="relative">
                      <Phone className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={contact.phone} onChange={(event) => setContact((current) => ({ ...current, phone: event.target.value }))} type="tel" placeholder="Nhập số điện thoại" className="w-full pl-12 pr-4 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all" />
                    </div>
                  </div>
                  <div className="md:col-span-2 space-y-2">
                    <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Email nhận xác nhận</label>
                    <div className="relative">
                      <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={contact.email} onChange={(event) => setContact((current) => ({ ...current, email: event.target.value }))} type="email" placeholder="Nhập địa chỉ email" className="w-full pl-12 pr-4 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all" />
                    </div>
                  </div>
                  <div className="md:col-span-2 space-y-2">
                    <label className="text-xs font-black text-slate-400 uppercase tracking-widest pl-1">Ghi chú cho đơn hàng</label>
                    <textarea value={contact.note} onChange={(event) => setContact((current) => ({ ...current, note: event.target.value }))} rows={3} placeholder="Ví dụ: hỗ trợ người lớn tuổi, liên hệ trước giờ khởi hành, yêu cầu thêm ghi chú..." className="w-full px-5 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400 transition-all resize-none" />
                  </div>
                </div>
              </section>

              <section className="bg-white p-8 rounded-[2.5rem] shadow-sm border border-slate-100">
                <div className="flex items-center justify-between mb-8 gap-4 flex-wrap">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center font-bold">2</div>
                    <h2 className="text-xl font-bold text-slate-900">Danh sách hành khách</h2>
                  </div>
                  <button type="button" onClick={applySavedPassengers} disabled={!savedPassengers.length} className="text-xs font-black text-blue-600 bg-blue-50 px-4 py-2 rounded-xl hover:bg-blue-100 transition-all disabled:opacity-40 disabled:cursor-not-allowed">
                    {savedPassengersLoading ? 'Đang tải danh sách đã lưu...' : 'Điền từ danh sách đã lưu'}
                  </button>
                </div>

                <div className="space-y-6">
                  {passengers.map((passenger, index) => (
                    <div key={`passenger-${index}`} className="rounded-[2rem] border border-slate-100 bg-slate-50 p-6">
                      <div className="flex items-center gap-3 mb-5">
                        <div className="w-10 h-10 rounded-xl bg-white text-blue-600 flex items-center justify-center shadow-sm">
                          <Users size={18} />
                        </div>
                        <div>
                          <p className="font-black text-slate-900">{formatPassengerTypeLabel(passenger.passengerType)} {index + 1}</p>
                          <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">
                            {passenger.passengerType === 'child' ? 'Thông tin trẻ em' : 'Thông tin hành khách'}
                          </p>
                        </div>
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <input value={passenger.fullName} onChange={(event) => handlePassengerChange(index, 'fullName', event.target.value)} type="text" placeholder="Họ và tên hành khách" className="w-full px-5 py-4 bg-white border border-slate-100 rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400" />
                        <input value={passenger.idNumber} onChange={(event) => handlePassengerChange(index, 'idNumber', event.target.value)} type="text" placeholder="CCCD / CMND" className="w-full px-5 py-4 bg-white border border-slate-100 rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400" />
                        <input value={passenger.dateOfBirth} onChange={(event) => handlePassengerChange(index, 'dateOfBirth', event.target.value)} type="date" className="w-full px-5 py-4 bg-white border border-slate-100 rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900" />
                        <input value={passenger.passportNumber} onChange={(event) => handlePassengerChange(index, 'passportNumber', event.target.value)} type="text" placeholder="Hộ chiếu (nếu có)" className="w-full px-5 py-4 bg-white border border-slate-100 rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400" />
                        <input value={passenger.email} onChange={(event) => handlePassengerChange(index, 'email', event.target.value)} type="email" placeholder="Email hành khách" className="w-full px-5 py-4 bg-white border border-slate-100 rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400" />
                        <input value={passenger.phoneNumber} onChange={(event) => handlePassengerChange(index, 'phoneNumber', event.target.value)} type="tel" placeholder="Số điện thoại hành khách" className="w-full px-5 py-4 bg-white border border-slate-100 rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium text-slate-900 placeholder:text-slate-400" />
                      </div>
                    </div>
                  ))}
                </div>
              </section>

              <section className="bg-white p-8 rounded-[3rem] shadow-sm border border-slate-100">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-12 h-12 bg-slate-50 text-slate-400 rounded-2xl flex items-center justify-center">
                      <FileText size={24} />
                    </div>
                    <div>
                      <h3 className="font-bold text-slate-900">Yêu cầu xuất hóa đơn VAT</h3>
                      <p className="text-xs text-slate-500 font-medium mt-1">Xuất hóa đơn điện tử cho doanh nghiệp</p>
                    </div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input type="checkbox" className="sr-only peer" checked={useVAT} onChange={() => setUseVAT((current) => !current)} />
                    <div className="w-14 h-8 bg-slate-100 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[4px] after:left-[4px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-6 after:w-6 after:transition-all peer-checked:bg-blue-600" />
                  </label>
                </div>

                {useVAT ? (
                  <div className="mt-8 grid grid-cols-1 md:grid-cols-2 gap-6 animate-slide-down">
                    <input value={contact.companyName} onChange={(event) => setContact((current) => ({ ...current, companyName: event.target.value }))} type="text" placeholder="Tên công ty" className="w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    <input value={contact.taxCode} onChange={(event) => setContact((current) => ({ ...current, taxCode: event.target.value }))} type="text" placeholder="Mã số thuế" className="w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    <input value={contact.companyAddress} onChange={(event) => setContact((current) => ({ ...current, companyAddress: event.target.value }))} type="text" placeholder="Địa chỉ công ty" className="md:col-span-2 w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    <input value={contact.invoiceEmail} onChange={(event) => setContact((current) => ({ ...current, invoiceEmail: event.target.value }))} type="email" placeholder="Email nhận hóa đơn" className="md:col-span-2 w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                  </div>
                ) : null}
              </section>
            </div>

            <aside className="w-full lg:w-96">
              <div className="space-y-6 sticky top-28">
                <div className="bg-white rounded-[3rem] shadow-xl border border-slate-100 p-8 overflow-hidden relative">
                  <div className="absolute top-0 left-0 w-2 h-full bg-blue-600" />
                  <h3 className="font-black text-slate-900 text-xl mb-6">Chi tiết thanh toán</h3>
                  <div className="space-y-4 mb-8">
                    <div className="flex justify-between items-start">
                      <span className="text-sm font-bold text-slate-500 italic">{isHotel ? `Tạm tính (${roomCount} phòng)` : `Tạm tính (${passengers.length} khách)`}</span>
                      <p className="font-black text-slate-900">{detailLoading ? 'Đang tải...' : formatCurrency(subtotal, currency)}</p>
                    </div>
                    <div className="flex justify-between items-start">
                      <span className="text-sm font-bold text-slate-500 italic">Phí dịch vụ</span>
                      <p className="font-black text-slate-900">0đ</p>
                    </div>
                    {isFlight ? (
                      <>
                        <div className="flex justify-between items-start">
                          <span className="text-sm font-bold text-slate-500 italic">Ghế đã chọn</span>
                          <p className="font-black text-slate-900">{formatCurrency(seatPriceModifier, currency)}</p>
                        </div>
                        <div className="flex justify-between items-start">
                          <span className="text-sm font-bold text-slate-500 italic">Dịch vụ thêm</span>
                          <p className="font-black text-slate-900">{formatCurrency(selectedAncillaries.reduce((sum, item) => sum + Number(item.price || 0), 0), currency)}</p>
                        </div>
                      </>
                    ) : null}
                    <div className="pt-6 flex justify-between items-center">
                      <span className="font-black text-slate-900">Tổng cộng</span>
                      <p className="text-3xl font-black text-blue-600">{detailLoading ? '--' : formatCurrency(subtotal, currency)}</p>
                    </div>
                  </div>

                  <button type="button" onClick={handleSubmit} disabled={submitting || detailLoading} className="w-full flex items-center justify-center gap-3 bg-blue-600 text-white py-5 rounded-[2rem] font-black text-lg shadow-2xl shadow-blue-500/40 hover:scale-105 transition-all disabled:opacity-60 disabled:cursor-not-allowed disabled:hover:scale-100">
                    {submitting ? 'Đang tạo đơn hàng...' : 'Tiếp tục thanh toán'} <ChevronRight size={24} />
                  </button>

                  <div className="mt-6 flex flex-col gap-3">
                    <div className="flex items-start gap-2 text-[10px] text-slate-400 font-bold leading-tight">
                      <CheckCircle2 size={14} className="text-green-500 shrink-0" />
                      <p>Bằng cách nhấn thanh toán, bạn đồng ý với Điều khoản & Chính sách của nền tảng.</p>
                    </div>
                    <div className="flex items-center gap-2 text-[10px] text-slate-400 font-bold leading-tight">
                      <ShieldCheck size={14} className="text-blue-500 shrink-0" />
                      <p>Tiền sẽ được thu về tài khoản platform và đối soát theo tenant sau thanh toán.</p>
                    </div>
                    {holdToken ? (
                      <div className="flex items-center gap-2 text-[10px] text-slate-400 font-bold leading-tight">
                        <Clock size={14} className="text-blue-500 shrink-0" />
                        <p>Mã giữ chỗ hiện tại: <span className="text-slate-600">{holdToken}</span></p>
                      </div>
                    ) : null}
                  </div>
                </div>

                <div className="bg-slate-900 text-white rounded-[2.5rem] p-8 shadow-2xl relative overflow-hidden group">
                  <div className="absolute top-[-20%] right-[-10%] w-32 h-32 bg-blue-500/20 rounded-full blur-2xl group-hover:bg-blue-500/30 transition-all" />
                  <h4 className="font-black text-blue-400 text-xs uppercase tracking-widest mb-4">Thông tin dịch vụ</h4>
                  <div className="space-y-4">
                    <div className="flex items-center gap-4">
                      <div className="w-10 h-10 bg-white/10 rounded-xl flex items-center justify-center">
                        {isFlight ? <Plane size={20} className="text-blue-400" /> : isHotel ? <Hotel size={20} className="text-blue-400" /> : <Ticket size={20} className="text-blue-400" />}
                      </div>
                      <div>
                        <p className="font-bold text-sm">{isBus ? `${busRoute.from} - ${busRoute.to}` : isTrain ? `${trainRoute.from} - ${trainRoute.to}` : isFlight ? `${flightRoute.fromCode} - ${flightRoute.toCode}` : isHotel ? hotelName : sourceDetail?.name || 'Tour du lịch'}</p>
                        <p className="text-[10px] opacity-60 font-bold uppercase tracking-widest">{isBus ? sourceDetail?.trip?.code || 'Chuyến xe' : isTrain ? sourceDetail?.trip?.trainNumber || sourceDetail?.trip?.code || 'Chuyến tàu' : isFlight ? sourceDetail?.flight?.flightNumber || 'Chuyến bay' : isHotel ? `${roomTypeName} • ${ratePlanName}` : tourQuote?.package?.packageName || 'Gói tour mặc định'}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="w-10 h-10 bg-white/10 rounded-xl flex items-center justify-center">
                        <Clock size={20} className="text-blue-400" />
                      </div>
                      <div>
                        <p className="font-bold text-sm">{isBus ? (detailLoading ? 'Đang tải hành trình...' : formatDateTime(sourceDetail?.segment?.departureAt)) : isTrain ? (detailLoading ? 'Đang tải hành trình...' : formatDateTime(sourceDetail?.segment?.departureAt)) : isFlight ? (detailLoading ? 'Đang tải hành trình...' : formatDateTime(flightRoute.departureAt)) : isHotel ? `${checkInDate || '--'} → ${checkOutDate || '--'}` : selectedTourSchedule ? `${selectedTourSchedule.departureDate || '--'}` : 'Đang cập nhật lịch khởi hành'}</p>
                        <p className="text-[10px] opacity-60 font-bold uppercase tracking-widest text-green-400">{isBus ? `${formatTime(sourceDetail?.segment?.departureAt)} - ${formatTime(sourceDetail?.segment?.arrivalAt)}` : isTrain ? `${formatTime(sourceDetail?.segment?.departureAt)} - ${formatTime(sourceDetail?.segment?.arrivalAt)}` : isFlight ? `${formatTime(flightRoute.departureAt)} - ${formatTime(flightRoute.arrivalAt)}` : isHotel ? `${roomCount} phòng • ${adultCount} người lớn${childCount > 0 ? ` • ${childCount} trẻ em` : ''}` : `${adultCount} người lớn${childCount > 0 ? ` • ${childCount} trẻ em` : ''}`}</p>
                      </div>
                    </div>
                    {isFlight ? (
                      <div className="flex items-center gap-4">
                        <div className="w-10 h-10 bg-white/10 rounded-xl flex items-center justify-center">
                          <ShieldCheck size={20} className="text-blue-400" />
                        </div>
                        <div>
                          <p className="font-bold text-sm">{seatNumber || 'Chưa chọn ghế cụ thể'}</p>
                          <p className="text-[10px] opacity-60 font-bold uppercase tracking-widest text-green-400">{selectedAncillaries.length > 0 ? `${selectedAncillaries.length} dịch vụ thêm` : 'Không có ancillary'}</p>
                        </div>
                      </div>
                    ) : null}
                  </div>
                </div>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </MainLayout>
  );
};

export default CheckoutPage;
