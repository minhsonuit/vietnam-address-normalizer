# Prompt cho Gemini

> Copy toàn bộ nội dung bên dưới và paste vào Gemini.

---

## Nhiệm vụ: Mở rộng VnAddressSanitizer — Thêm patterns và test cases mới

Đọc `docs/context/handoff.md` trước để nắm context hiện tại.

### Bối cảnh

Vừa fix xong 4 vấn đề từ batch-test (1019 địa chỉ thực tế):
- `call truoc khi den` → separator artifact `-,`
- `next to Vinmart` → English landmark không bị remove
- `giao cổng bảo vệ` → delivery instruction không match
- `???` → junk token không bị clean

Hiện tại: 52 tests pass, batch output: 322 changed / 697 unchanged trên 1019 dòng.

### Yêu cầu

**Bước 1**: Chạy batch test hiện tại và phân tích output:

```bash
dotnet run --project tools/VnAddressSanitizer.Runner -- "$(pwd)/sample_input.txt"
```

Grep trong `sample_input_output.txt` tìm các dòng OUT còn chứa noise chưa được xử lý. Tập trung vào:
- Các instruction tiếng Việt còn sót: `để trước cổng`, `bỏ trước cửa`, `gửi bảo vệ`, `để ở`, `bỏ ở`
- Các direction note tiếng Anh còn sót: `across from`, `in front of`, `beside`
- Bất kỳ pattern noise nào khác mà bạn phát hiện trong output

**Bước 2**: Với mỗi pattern phát hiện, tự đánh giá:
1. Pattern này có **an toàn** để remove không? (không ăn nhầm tên đường/phường/xã)
2. Cần match cả **có dấu** và **không dấu**?
3. Pattern thuộc stage nào? (Instructions, DirectionNotes, StandaloneJunk, etc.)

**Bước 3**: Implement fixes trong `src/VnAddressSanitizer/SanitizePatterns.cs`:
- Thêm regex mới hoặc mở rộng regex hiện có
- Nếu cần thêm stage mới trong pipeline, update `AddressSanitizer.cs`
- Đảm bảo regex là `static readonly`, không dùng `[GeneratedRegex]`

**Bước 4**: Thêm test cases trong `tests/VnAddressSanitizer.Tests/AddressSanitizerTests.cs`:
- **Unit test** cho mỗi pattern mới
- **False-positive regression test** để đảm bảo không ăn nhầm:
  - Tên đường chứa từ khóa (VD: `Đường Thuận Giao 25` — "giao" phải được giữ)
  - Tên phường/xã chứa từ trùng (VD: `Xã Giao Khẩu`)
  - Tên riêng tiếng Anh (VD: `Callisto Tower`, `Near East Plaza`)
- **Integration test** kết hợp nhiều noise pattern cùng lúc

**Bước 5**: Verify:

```bash
dotnet build
dotnet test
dotnet test --filter "FalsePositive"
dotnet run --project tools/VnAddressSanitizer.Runner -- "$(pwd)/sample_input.txt"
```

So sánh batch output trước/sau: Changed phải tăng, không có regression.

**Bước 6**: Update `docs/context/handoff.md` với:
- Những gì đã thêm/sửa
- Số test mới
- Batch stats mới (Total/Changed/Unchanged)

### Quy tắc bắt buộc

1. **KHÔNG BAO GIỜ remove core address** (số nhà, tên đường, phường, quận, tỉnh/TP)
2. **"giao" KHÔNG được match standalone** — chỉ với companion words
3. **Mọi pattern tiếng Việt phải có cả variant có dấu và không dấu**
4. **Nếu kết quả rỗng sau sanitize → trả về input gốc** (safety fallback)
5. **Chạy test sau mỗi thay đổi** — không commit code broken
6. **Khi không chắc chắn → HỎI thay vì đoán**

### Files cần đọc

| File | Mục đích |
|------|----------|
| `docs/context/handoff.md` | Context hiện tại |
| `src/VnAddressSanitizer/SanitizePatterns.cs` | Tất cả regex patterns |
| `src/VnAddressSanitizer/AddressSanitizer.cs` | Pipeline orchestration |
| `tests/VnAddressSanitizer.Tests/AddressSanitizerTests.cs` | Test suite hiện có |
| `sample_input_output.txt` | Batch output để phân tích |
| `AGENTS.md` | Critical rules |
