# Hotel Swagger Test Suite

Bo tai lieu nay dung de test nhanh nhom `hotel controllers` hien tai tren Swagger UI cua `TicketBooking.Api`.

## File lien quan

- Request mau: `TicketBooking.Api/Swagger/HotelSwaggerTestSuite.http`
- Swagger UI local: `http://localhost:5183/swagger`

## Chuan bi

1. Chay API o local.
2. Mo Swagger UI.
3. Bam `Authorize` va nap JWT phu hop:
   - `Admin token` cho nhom `/admin/*`
   - `QLKS token` cho nhom `/qlks/*`
4. Chuan bi `X-TenantId` dang `Guid` cho cac request can tenant context.
   - `Admin write` (`POST`, `PUT`, `PATCH`, `DELETE`): bat buoc.
   - `Admin read` (`GET`): tuy chon, nhung nen dien de scope dung tenant.
   - `QLKS`: tuy chon neu tai khoan chi thuoc 1 tenant; bat buoc neu tai khoan thuoc nhieu tenant.
5. Dung file `.http` de copy query/body vao Swagger UI.

## Quy uoc enum trong body JSON

Project hien tai chua bat `JsonStringEnumConverter`, vi vay cac enum nen gui bang so:

- `HotelStatus`: `1 = Draft`, `2 = Active`, `3 = Inactive`, `4 = Suspended`
- `CancellationPolicyType`: `1 = FreeCancellation`, `2 = NonRefundable`, `3 = Custom`
- `PenaltyChargeType`: `1 = PercentOfNight`, `2 = PercentOfTotal`, `3 = FixedAmount`, `4 = NightCount`

## Thu tu chay de xac minh end-to-end

1. `QLKS / Hotels`
   - Tao 1 hotel moi.
   - Update hotel bang bo `clear flags`.
   - Kiem tra list/get/deactivate/activate.

2. `Public / Hotels`
   - List hotel.
   - Get by `id`.
   - Get by `slug`.
   - Get gallery.
   - Get reviews.
   - Get availability hop le.
   - Test availability khong hop le de nhan `400`.

3. `Admin / Hotels`
   - Tao 1 hotel rieng cho admin flow.
   - List/get/update.

4. `Admin / Hotel Amenities`
   - Tao amenity.
   - Link amenity vao hotel.
   - Doc lai danh sach link.

5. `Admin / Hotel Contacts`
   - Tao contact.
   - Set primary.
   - Deactivate va kiem tra request `set-primary` tra `400` neu contact inactive.

6. `Admin / Hotel Images`
   - Tao image.
   - Set primary.
   - Deactivate va kiem tra request `set-primary` tra `400` neu image inactive.

7. `Admin / Hotel Reviews`
   - List review theo `hotelId`.
   - Update/approve/hide voi mot review da ton tai.

8. `QLKS / Hotel Policies`
   - Tao `cancellation policy`, `check-in-out rule`, `property policy`.
   - Test request invalid de dam bao validation moi da chay.

9. `Admin / Hotel Policies`
   - Lap lai bo policy tren admin endpoints.

## Ky vong chinh

- Public chi nhin thay hotel/room type/rate plan dang `Active`.
- Availability khong tra ve ngay inventory `Closed`.
- Availability khong tinh thieu gia khi `rooms > 1`.
- `set-primary` cho image/contact inactive phai bi chan.
- `clear flags` o update hotel phai clear duoc field nullable.
- `cancellation policy rules` va `check-in-out window` invalid phai tra `400`.

## Ghi chu khi chay bang Swagger UI

- Cac endpoint `create` thuong tra ve body chi co `{ id }`, nen sau moi request tao moi ban can copy `id` vao o route/query cua buoc tiep theo.
- Cac test review can `reviewId` da ton tai. Neu moi truong chua co review seed thi bo qua nhom nay hoac seed truoc.
- Trong file `.http`, cac bien dang de placeholder. Chi can thay token, tenant, va cac `id` sau khi create.
