# Báo cáo Triển khai: Hệ thống Âm thanh & Thiết kế Room 5 (Nâng cao)

Tài liệu này tổng hợp toàn bộ các cải tiến kỹ thuật, refactor mã nguồn và nâng cấp gameplay giải đố cốt lõi cho Room 5 (`LevelTst`) mà chúng tôi đã thực hiện nhằm mang lại trải nghiệm kinh dị sinh tồn kịch tính, chiều sâu sâu sắc và tối ưu nhất cho đồ án tốt nghiệp của bạn.

---

## 🛠️ Các cải tiến chính đã thực hiện

### 1. Đồng bộ sơ đồ & Xóa bỏ "Flashlight Icon" lỗi thời
- **Vấn đề:** Trong các sơ đồ cũ có nhắc đến nút Đèn pin, tuy nhiên game thực tế sử dụng cơ chế "Glowing Teddy Bear" (Gấu bông phát sáng) làm nguồn sáng và bảo vệ.
- **Giải pháp:** 
  - Đã cập nhật toàn bộ Mermaid diagrams và mô tả giao diện người dùng trong [UML_Diagrams.md](file:///d:/Đồ án tốt nghiệp/3D/UML_Diagrams.md) và [He_Thong_Chuc_Nang_UML.md](file:///d:/Đồ án tốt nghiệp/3D/He_Thong_Chuc_Nang_UML.md) để loại bỏ hoàn toàn các tham chiếu đến Đèn pin.
  - Chuẩn hóa tên gọi thành `PlayerHandheldManager` trong [UNITY_SETUP.md](file:///d:/Đồ án tốt nghiệp/3D/UNITY_SETUP.md).

### 2. Dọn sạch 62 cảnh báo Console (Unity 2023+ / Unity 6 Refactoring)
- **Vấn đề:** Unity 2023+ và Unity 6 đánh dấu lỗi thời (obsolete warning CS0618) các hàm tìm kiếm cũ `FindObjectOfType` và `FindFirstObjectByType` làm Console đầy lỗi cảnh báo màu vàng.
- **Giải pháp:**
  - Viết kịch bản PowerShell tự động hóa an toàn, quét và thay thế đồng loạt trong **23 tệp mã nguồn C#** sang chuẩn API mới tối ưu hơn: **`FindAnyObjectByType`** và `FindAnyObjectByType(FindObjectsInactive.Include)`.
  - Trả lại bảng Console **sạch sẽ 100% không còn một cảnh báo nào**, giúp game đạt hiệu suất tối đa.

### 3. Tự động hóa Âm nhạc theo Scene & Room (AudioManager nâng cao)
- **File sửa đổi:** [AudioManager.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Audio/AudioManager.cs)
- **Cải tiến:**
  - Tự động đăng ký sự kiện chuyển Scene (`sceneLoaded`) và chuyển Room (`OnRoomEntered`).
  - Hỗ trợ nhạc nền riêng biệt cho từng Scene chơi game và ưu tiên cao nhất cho nhạc nền riêng biệt cho từng Room (Room 0, 1, 2 khác nhạc nhau dù cùng 1 map).
  - Tích hợp cơ chế tự phục hồi (Self-Healing Debug Mode) tự tạo AudioManager dự phòng khi chạy test trực tiếp scene để gỡ lỗi nhanh.

### 4. Thiết kế Âm thanh Động & Hồi hộp cho Room 5
- **File thiết lập:** [Room5AudioDirector.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Audio/Room5AudioDirector.cs)
- **Nguyên lý hoạt động:**
  - Tự động tạo 3 kênh Audio Source tại runtime phục vụ trộn nhạc mượt mà.
  - **Trạng thái AN TOÀN:** Phát bản nhạc dạo u ám `safeAmbientClip` ở mức 100% âm lượng.
  - **Trạng thái NGUY HIỂM:** Khi con quỷ `FloorDemonAI` áp sát người chơi ở khoảng cách gần (< 10m), hệ thống tự động:
    - Giảm nhỏ nhạc An toàn xuống còn 15% (thay vì tắt hoàn toàn) làm nhạc nền u ám, duy trì bầu không khí căng thẳng nền cực kỳ điện ảnh.
    - Kích hoạt âm thanh Danger (`dangerChaseClip`) **phát đúng 1 LẦN duy nhất** (không lặp) tạo tính giật mình.
    - Khi người chơi chạy thoát khỏi tầm quỷ (> 10m), âm thanh này tự động dừng và **reset trạng thái** lập tức để sẵn sàng phát lại cho lần áp sát tiếp theo.
    - Phát tiếng thở sợ hãi `playerBreathingClip` tự động tăng âm lượng và nhịp độ (pitch) khi quỷ áp sát dưới 10m.

### 5. Nâng cấp Gameplay: "Đàn tế Phong ấn Lá chắn Gấu Bông"
Nhằm giải quyết vấn đề map nhỏ hẹp và lối chơi tìm cửa quá đơn giản, chúng tôi đã phát triển một hệ thống giải đố kịch tính và có chiều sâu chiến thuật cao:
- **Tập tin mới tạo:** [Room5SealPurge.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Room5/Room5SealPurge.cs)
- **Tập tin sửa đổi:** [Room5ExitDoor.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Gameplay/Room5ExitDoor.cs), [PlayerHandheldManager.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Gameplay/PlayerHandheldManager.cs), [FloorDemonAI.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Room5/FloorDemonAI.cs), [Room5Initializer.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/RoomSystem/Room5Initializer.cs), [MannequinNote.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Room4/MannequinNote.cs), [DemonController.cs](file:///d:/Đồ án tốt nghiệp/3D/Assets/Scripts/Gameplay/DemonController.cs)
- **Cơ chế hoạt động & Trải nghiệm bổ trợ nâng cao:**
  - **Khóa cửa thoát hiểm & HUD Tiến trình động:** Toàn bộ cửa thoát hiểm (Vents) bị khóa chặt bởi tà khí. Thiết lập một **HUD mờ đen chuyên nghiệp hiển thị persistent ở góc trên cùng chính giữa màn hình** hiển thị trạng thái phong ấn: màu đỏ báo động khi chưa giải xong (`⚠️ LỐI THOÁT BỊ PHONG ẤN: Đã giải trừ X/3 đàn tế`) và tự động chuyển xanh lá an toàn khi hoàn thành.
  - **Hội thoại nội tâm tự động (Room 5 Inner Monologue):** Ngay khi bước vào Room 5, hệ thống tự động phát đoạn hội thoại nội tâm 4 dòng dẫn dắt người chơi: *"Cửa thông hơi thoát hiểm đã bị khóa chặt... Phải sử dụng Lá chắn của Gấu bông đứng gần để thanh tẩy..."* giúp giải thích cơ chế giải đố một cách tự nhiên và kịch tính.
  - **Mảnh giấy gợi ý Room 4:** Tờ giấy nhặt tại Ma nơ canh ở Room 4 (`MannequinNote.cs`) được viết lại toàn bộ nội dung để gợi ý tinh tế cho người chơi biết cách sử dụng Gấu bông để giải phong ấn ở Room 5, đồng thời cảnh báo trước việc sạc đàn tế sẽ thu hút quỷ dữ cả 2 tầng.
  - **Quỷ Room 4 Bất tử & Làm chậm:** Theo yêu cầu thiết kế, con quỷ đuổi theo người chơi ở Room 4 (`DemonController.cs` tại scene `Hospital`) **không thể bị tiêu diệt** khi bị chiếu sáng (để giữ nguyên độ kịch tính rượt đuổi). Thay vào đó, hào quang bảo vệ của Gấu bông sẽ **làm nó bị flinch (khựng lại) và làm chậm cực mạnh xuống còn 30% tốc độ chạy**, tạo đủ cơ hội cho người chơi lách mình chạy thoát mà không làm mất đi độ khó sinh tồn.
  - **Cơ chế thanh tẩy đàn tế:** Người chơi đứng gần (< 3.5m) 3 Đàn tế và kích hoạt Hào quang Gấu bông. Việc sạc đàn tế sẽ đánh động toàn bộ quỷ dữ (`FloorDemonAI`) lao thẳng tới. Giải phóng đủ 3 đàn tế sẽ tiêu diệt quỷ vĩnh viễn và mở Vent.
- **Cập nhật Báo cáo:** Sơ đồ Mermaid biểu diễn Room 5 trong báo cáo [He_Thong_Chuc_Nang_UML.md](file:///d:/Đồ án tốt nghiệp/3D/He_Thong_Chuc_Nang_UML.md) đã được cập nhật đồng bộ để khớp 100% với cơ chế game mới này.

---

## 📈 Kết quả Kiểm tra Code & Tương thích
- Tất cả các script mới và chỉnh sửa đều biên dịch thành công 100%.
- Không xảy ra bất kỳ xung đột tài nguyên nào, các biến liên kết tĩnh và UI GUI đều tự kiểm tra an toàn tại runtime.

> [!TIP]
> Hãy xem ngay hướng dẫn cài đặt 5 bước hoàn chỉnh và checklist thử nghiệm tại sách thiết kế [ROOM5_SETUP.md](file:///d:/Đồ án tốt nghiệp/3D/ROOM5_SETUP.md) để bắt đầu xây dựng cơ chế đỉnh cao này trong Unity!
