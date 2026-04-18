using TicketBooking.Domain.Train;

namespace TicketBooking.Api.Services.Train;

public static class TrainCarSeatGenerationSupport
{
    public sealed class GeneratedSeatRow
    {
        public string SeatNumber { get; init; } = "";
        public TrainSeatType SeatType { get; init; }
        public string? CompartmentCode { get; init; }
        public int? CompartmentIndex { get; init; }
        public int RowIndex { get; init; }
        public int ColumnIndex { get; init; }
        public bool IsWindow { get; init; }
        public bool IsAisle { get; init; }
        public string? SeatClass { get; init; }
        public decimal? PriceModifier { get; init; }
    }

    public static List<GeneratedSeatRow> BuildLayout(
        int rows,
        int columns,
        bool useCompartments,
        int compartmentSize,
        bool sleeperUpperLower,
        string? seatClass,
        decimal? priceModifier)
    {
        var seats = new List<GeneratedSeatRow>();
        var seatCounter = 1;
        int? compartmentIndex = null;
        string? compartmentCode = null;
        var inCompartment = 0;

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                if (useCompartments)
                {
                    if (compartmentIndex is null)
                    {
                        compartmentIndex = 1;
                        compartmentCode = $"K{compartmentSize}-{compartmentIndex:00}";
                        inCompartment = 0;
                    }

                    inCompartment++;
                    if (inCompartment > compartmentSize)
                    {
                        compartmentIndex++;
                        compartmentCode = $"K{compartmentSize}-{compartmentIndex:00}";
                        inCompartment = 1;
                    }
                }
                else
                {
                    compartmentIndex = null;
                    compartmentCode = null;
                }

                var isWindow = c == 0 || c == columns - 1;
                var isAisle = columns >= 3 && (c == 1 || c == columns - 2);

                if (sleeperUpperLower)
                {
                    seats.Add(MakeSeat($"{seatCounter:000}-L", TrainSeatType.LowerBerth));
                    seats.Add(MakeSeat($"{seatCounter:000}-U", TrainSeatType.UpperBerth));
                    seatCounter++;
                }
                else
                {
                    seats.Add(MakeSeat($"{seatCounter:000}", TrainSeatType.Seat));
                    seatCounter++;
                }

                GeneratedSeatRow MakeSeat(string seatNo, TrainSeatType seatType)
                    => new()
                    {
                        SeatNumber = seatNo,
                        SeatType = seatType,
                        CompartmentCode = compartmentCode,
                        CompartmentIndex = compartmentIndex,
                        RowIndex = r,
                        ColumnIndex = c,
                        IsWindow = isWindow,
                        IsAisle = isAisle,
                        SeatClass = seatClass,
                        PriceModifier = priceModifier
                    };
            }
        }

        return seats;
    }
}
