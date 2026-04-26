# VnAddressSanitizer — Noise Handling Logic

Tài liệu này định nghĩa chi tiết 7 nhóm dữ liệu rác (Noise) phổ biến trong địa chỉ giao hàng tại Việt Nam (thường bị người dùng hiểu nhầm là trường "ghi chú cho shipper") và giải thích logic cốt lõi mà thư viện sử dụng để làm sạch chúng.

## 1. Nhóm thông tin liên lạc (Contact Information Noise)

**Nguyên nhân:** Người dùng cố tình nhập số điện thoại vào địa chỉ để đảm bảo shipper có thể liên lạc được.
**Ví dụ:** `"123 Lê Lợi, Q1, sđt: 0901234567"`, `"Điện thoại 0987654321 Nguyễn Văn A"`

**Logic xử lý (Stage 3a & 3b):**
- **Xóa số điện thoại:** Dùng regex `(?<!\d)(?:\+?84|0)(?:[.\-\s]?\d){8,10}(?!\d)` để bắt chính xác định dạng SĐT Việt Nam (+84, 09x, độ dài 8-10 số). Có sử dụng `lookbehind/lookahead` để **tránh xóa nhầm số nhà** (ví dụ: `031 Nguyễn Văn Cừ`).
- **Xóa nhãn dán (Labels):** Sau khi xóa số, regex thứ hai `(?:phone|tel|s[đd]t|...)\s*[:：]?\s*,?\s*` sẽ chạy để dọn dẹp các cụm từ mồ côi như `sđt:`, `phone:` còn sót lại.

## 2. Nhóm chỉ dẫn giao nhận (Delivery/Order Instructions)

**Nguyên nhân:** Lời nhắn trực tiếp đến shipper, mã đơn hàng, hoặc lời cảm ơn.
**Ví dụ:** `"nhận hàng dùm"`, `"giao trong giờ hành chính"`, `"đừng gọi số này"`, `"mã đơn hàng ABC"`, `"cảm ơn shop"`.

**Logic xử lý (Stage 3c):**
- Quét qua danh sách từ khóa hành động (bắt buộc hỗ trợ cả có dấu và không dấu): `gọi/goi`, `giao`, `ship`, `nhận/nhan`, `để/de`, `bỏ/bo`, `mã đơn/ma don`, `cảm ơn/cam on`.
- **Nguyên tắc "Ăn đến dấu phẩy" (Greedy Match):** Hầu hết các pattern (VD: `(?:mã\s*đơn...)[^,]*`) sẽ match từ khóa và "nuốt" toàn bộ các từ theo sau cho đến khi đụng dấu phẩy (hoặc dấu phân cách hành chính) tiếp theo. Điều này giúp dọn sạch cụm từ mà không ảnh hưởng đến cấp hành chính phía sau.
- **Bảo vệ False Positive:** Cụm từ "giao" không bao giờ được match đứng một mình (để tránh lỗi ở `Đường Thuận Giao`). Nó phải đi kèm từ phụ (VD: `giao cho`, `giao tới`).

## 3. Nhóm chỉ dẫn tìm đường / Cột mốc (Direction & Landmark Notes)

**Nguyên nhân:** Ghi chú mốc địa lý để dễ tìm nhà.
**Ví dụ:** `"gần chùa X"`, `"đối diện siêu thị"`, `"sau lưng bệnh viện"`, `"ngay ngã 3"`, `"next to"`.

**Logic xử lý (Stage 3d):**
- Các từ khóa chỉ phương hướng (VD: `gần/gan`, `đối diện/doi dien`, `sau lưng/sau lung`, `next to`, `opposite`...) chỉ được match nếu nó nằm ngay sau một dấu phân cách (phẩy, gạch ngang) hoặc ở đầu câu.
- **Giới hạn số từ:** Regex `(?:\s+(?![,;-]\s*)[^,;-]+){0,3}` cho phép xóa từ khóa kèm theo tối đa 1-5 từ tiếp theo (để tránh xóa lẹm sang tên Phường/Xã nếu người dùng quên gõ dấu phẩy).

## 4. Nhóm viết tắt, lỗi định dạng và ký tự rác (Formatting Typos & Junk)

**Nguyên nhân:** Gõ vội, copy-paste lỗi, hoặc thói quen viết tắt hành chính.
**Ví dụ:** `"Q1"`, `"P.12"`, `"TP HCM"`, `"TX"`, `"???"`, `"###"`, `"(gần chợ)"`.

**Logic xử lý (Stage 1, 1.5, 2, 3f, 5):**
- **NFC Normalization:** Chuẩn hóa Unicode tiếng Việt (tránh lỗi font chữ khác nhau).
- **Mở rộng viết tắt (Stage 1.5):** Dùng regex để an toàn chuyển `Q1` -> `Quận 1`, `P.12` -> `Phường 12`, `TP` -> `Thành phố`, `TX` -> `Thị xã`. (Cực kỳ quan trọng để API Geocoding hiểu đúng ngữ cảnh).
- **Loại bỏ ngoặc (Stage 2):** Xóa triệt để mọi nội dung nằm trong `(...)`.
- **Dọn rác (Stage 3f & 5):** Xóa `#` độc lập, dấu `???` liên tiếp. Bước dọn dẹp cuối (Stage 5) sẽ xóa các dấu phẩy kép `,,`, dấu gạch ngang mồ côi `-,` và khoảng trắng thừa do các bước trước để lại.

## 5. Nhóm trùng lặp cấp hành chính (Admin Unit Duplication)

**Nguyên nhân:** Hệ thống ERP/POS tự động nối (append) Phường/Xã, Quận/Huyện từ Combo box vào TextBox chứa địa chỉ người dùng đã tự gõ.
**Ví dụ:** `"Đường Nam, Phường Bến Nghé, Phường Bến Nghé, Quận 1, Quận 1"`

**Logic xử lý (Stage 4 - AdminUnitDeduplicator.cs):**
- Thuật toán luôn **ưu tiên giữ lại đơn vị ở cuối chuỗi** (do đây là chuẩn của hệ thống sinh ra) và xóa phần trùng lặp ở phía trước (do user gõ).
- Sử dụng thuật toán Fuzzy Match nội bộ `VietnameseTextHelper` để loại bỏ dấu tiếng Việt trước khi so sánh, giúp giải quyết các trường hợp user gõ không dấu (`Phuong Ben Nghe`) bị trùng với chuẩn hệ thống (`Phường Bến Nghé`).

## 6. Nhóm thông tin tòa nhà / Cụm dân cư dự án (Building & Project Info)

**Nguyên nhân:** Thông tin nội khu rất tốt cho shipper giao tận tay, nhưng lại làm giảm tỷ lệ (Match Rate) của các Engine Geocoding vì quá cụ thể.
**Ví dụ:** `"Tầng 3, Phòng 104, Block B, Chung cư Vinhome, KĐT Sala"`.

**Logic xử lý (Stage 3g - Tùy chọn):**
- Mặc định tính năng này tắt (`RemoveBuildingInfo = false`) để giữ thông tin cho giao hàng.
- Khi bật, regex sẽ xóa các cụm từ: `tầng/tang`, `lầu/lau`, `block`, `phòng`, `chung cư/cc`, `căn hộ`, `kđt`, `kdc`... và các số/ký tự đi liền theo sau.

## 7. Nhóm dư thừa mã bưu chính & Quốc gia (Postal Code & Country)

**Nguyên nhân:** Thói quen điền Form chuẩn quốc tế, nhưng gây nhiễu cho Local Geocoding.
**Ví dụ:** `"Quận 1, 700000, Việt Nam"`.

**Logic xử lý (Stage 3e):**
- Xóa cụm từ `Việt Nam` / `Viet Nam` ở cuối câu.
- Xóa chuỗi số `\d{6}` (mã bưu điện Việt Nam) đứng độc lập ở gần cuối câu.
