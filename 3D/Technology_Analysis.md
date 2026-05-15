# CHƯƠNG: PHÂN TÍCH CÔNG NGHỆ VÀ KIẾN TRÚC HỆ THỐNG
*Tài liệu này cung cấp cái nhìn chuyên sâu và lý luận chi tiết về việc lựa chọn công nghệ, kiến trúc phần mềm và các công cụ bổ trợ được áp dụng trong quá trình phát triển dự án Game 3D Anomaly.*

---

## 1. Nền tảng Phát triển (Development Platform)

### 1.1. Unity Game Engine (Phiên bản 6000.4.0f1)
Dự án được xây dựng hoàn toàn trên nền tảng Unity 6. Đây là sự lựa chọn tối ưu cho dự án đồ án nhờ các ưu điểm vượt trội:
* **Tính đa nền tảng (Cross-platform):** Unity hỗ trợ xuất bản (build) dự án mượt mà trên nhiều hệ điều hành khác nhau. Trong dự án này, khả năng cross-platform được tận dụng để phát triển song song phiên bản PC (Windows) và phiên bản thiết bị di động (Android/iOS) mà không cần viết lại mã nguồn cốt lõi.
* **Hệ sinh thái mạnh mẽ:** Khả năng truy cập vào Unity Package Manager và Unity Asset Store giúp tăng tốc độ phát triển, cho phép tích hợp nhanh các công cụ như ProBuilder hay New Input System.

### 1.2. Ngôn ngữ lập trình C# (C-Sharp)
Toàn bộ logic nghiệp vụ (Game Logic) của dự án được lập trình bằng C#.
* **Hướng đối tượng (Object-Oriented Programming - OOP):** Giúp cấu trúc mã nguồn rõ ràng, dễ bảo trì thông qua các đặc tính đóng gói, kế thừa và đa hình.
* **Quản lý bộ nhớ tự động (Garbage Collection):** Xử lý hiệu quả việc cấp phát và giải phóng vùng nhớ trong môi trường 3D liên tục sinh/hủy đối tượng (spawn/destroy các Anomaly và Segment hành lang).

---

## 2. Hệ thống Đồ họa và Xây dựng Môi trường (Graphics & Environment)

### 2.1. Universal Render Pipeline - URP (Phiên bản 17.4.0)
Thay vì sử dụng Built-in Render Pipeline cũ, dự án áp dụng URP (Universal Render Pipeline) làm cốt lõi xử lý đồ họa:
* **Tối ưu hóa hiệu năng (Performance Optimization):** URP cung cấp khả năng dựng hình theo kiểu Single-pass, giúp tiết kiệm tối đa số lần gọi vẽ (Draw Calls), đặc biệt quan trọng để duy trì tốc độ khung hình (FPS) ổn định trên nền tảng Mobile.
* **Xử lý hậu kỳ (Post-Processing):** Hỗ trợ mạnh mẽ các hiệu ứng thị giác như Bloom, Color Grading, Ambient Occlusion, giúp tăng tính rùng rợn và chân thực cho không gian hành lang kín của game.

### 2.2. Quy trình Vật liệu PBR (Physically Based Rendering)
Game sử dụng chuẩn PBR nhằm mô phỏng chính xác cách ánh sáng tương tác với vật thể trong thế giới thực. Để giải quyết bài toán thiếu hụt tài nguyên mô hình 3D cao cấp, dự án sử dụng phần mềm độc lập **Bounding Box Materialize**:
* **Materialize Workflow:** Phần mềm này cho phép nội suy (interpolate) từ một bức ảnh 2D cơ bản (Albedo/Diffuse map) để tạo ra một bộ Texture hoàn chỉnh bao gồm: 
  * *Normal Map:* Giả lập các chi tiết lồi lõm, gồ ghề của tường gạch.
  * *Height Map / Bump Map:* Thêm chiều sâu thị giác.
  * *Smoothness / Metallic Map:* Xác định mức độ nhám và khả năng phản chiếu ánh sáng của sàn nhà, cửa.
  * *Ambient Occlusion (AO):* Đổ bóng tự nhiên tại các khe nứt, góc khuất.

### 2.3. Dựng hình trong Engine bằng ProBuilder (Phiên bản 6.0.9)
Việc xây dựng bản đồ (Level Design) được thực hiện trực tiếp bên trong Unity thông qua package **ProBuilder**.
* Thay vì thiết kế môi trường bằng Blender hay Maya rồi import vào, ProBuilder cho phép dựng khối (Whiteboxing) trực tiếp, chỉnh sửa các mặt (Faces), cạnh (Edges) và UV map của hành lang theo thời gian thực (Real-time). Điều này giúp rút ngắn chu trình kiểm thử tỷ lệ (Scale Testing) của nhân vật so với không gian.

---

## 3. Kiến trúc Phần mềm và Mẫu Thiết kế (Software Architecture & Design Patterns)

### 3.1. Mô hình Component-based Architecture (CBA)
Kiến trúc cốt lõi của Unity là Entity-Component, nơi các thực thể (GameObjects) được định nghĩa bởi các thành phần (Components) mà nó chứa đựng.
* **Tính tách biệt (Decoupling):** Một đối tượng `Player` không phải là một lớp khổng lồ chứa mọi code, mà nó là sự lắp ghép của `FirstPersonController` (Xử lý di chuyển), `Rigidbody` (Xử lý vật lý), `CapsuleCollider` (Va chạm), và `AudioSource` (Phát âm thanh). Logic được chia nhỏ giúp dễ rà soát lỗi và tái sử dụng.

### 3.2. Áp dụng Mẫu Thiết kế (Design Patterns)
* **Singleton Pattern:** Được sử dụng triệt để cho các lớp quản lý mang tính toàn cục (Global Managers) như `LevelManager` (Quản lý cấp độ cửa) và `AnomalyManager` (Sinh dị thường). Đảm bảo trong suốt vòng đời của game chỉ tồn tại duy nhất một phiên bản của hệ thống này, chống tràn bộ nhớ và dễ dàng cho các script khác truy xuất (`LevelManager.Instance...`).
* **State Machine (FSM):** Sử dụng hệ thống Unity Animator Controller để xây dựng sơ đồ trạng thái (State Diagram). Logic hoạt ảnh và logic tạm dừng (Pause) được quản lý theo trạng thái chặt chẽ: `Locomotion` (di chuyển), `Jumpscare` (hù dọa), `Paused` (tạm dừng), ngăn chặn sự chồng chéo hành động.

---

## 4. Hệ thống Điều khiển và Tương tác (Input & Physics System)

### 4.1. Hệ thống điều khiển mới (New Input System - 1.19.0)
Dự án không dùng hàm `Input.GetAxis()` truyền thống mà nâng cấp lên hệ thống **Input System mới** của Unity.
* **Trừu tượng hóa thiết bị (Device Abstraction):** Cho phép gán các hành động (Actions) cụ thể (như "Di chuyển", "Nhìn") độc lập với thiết bị phần cứng.
* Giải quyết triệt để bài toán Cross-platform: Chỉ với một bộ script điều khiển duy nhất, game có thể tự động nhận diện On-screen Joystick (trên màn hình cảm ứng điện thoại) và Chuột + Bàn phím (trên PC) mà không cần if-else phức tạp.

### 4.2. Hệ thống Vật lý (Nvidia PhysX)
Sử dụng công nghệ mô phỏng vật lý tích hợp sẵn của Unity (PhysX) để xử lý va chạm (Collision) thay vì dùng tia (Raycast) thông thường:
* **Trigger Collision:** Các cánh cửa được gắn `BoxCollider` dạng `IsTrigger`. Khi người chơi bước qua, hàm sự kiện `OnTriggerEnter` sẽ lập tức kích hoạt bộ đếm logic của `LevelManager` để quyết định cấp độ tiếp theo.

---

## 5. Hệ thống Giao diện Người dùng (UI / UX)

### 5.1. Unity Canvas và Event System
Hệ thống UI được xây dựng theo kiến trúc Canvas-based (Package `com.unity.ugui: 2.0.0`).
* Tối ưu hóa việc cập nhật UI bằng cách chia Canvas thành các cụm riêng biệt (Static Canvas cho Menu, Dynamic Canvas cho HUD báo Level) để tránh việc Engine phải vẽ lại (Re-batch) toàn bộ màn hình khi chỉ có một con số Level thay đổi.

### 5.2. TextMeshPro (TMP)
Tất cả các thành phần văn bản (Text) đều được render bằng **TextMeshPro**.
* Khác với Text cơ bản sử dụng Bitmap, TMP sử dụng kỹ thuật **SDF (Signed Distance Field)**, giúp các chữ cái luôn giữ được độ viền sắc nét tuyệt đối dù phóng to đến mức nào hoặc chơi trên thiết bị màn hình độ phân giải siêu cao (Retina/4K).
