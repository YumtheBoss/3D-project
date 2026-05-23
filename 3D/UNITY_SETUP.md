# Unity Inspector Setup Guide — Behind the Door

Hướng dẫn này liệt kê toàn bộ việc cần làm trong Unity Editor sau khi pull code mới nhất.  
Thực hiện theo thứ tự từ trên xuống dưới.

---

## 1. Room 3 — Hành lang tối (Room3Starter)

**Script:** `Assets/Scripts/Room3/Room3Starter.cs`

### Các bước:
1. Trong scene **Hospital**, tạo một Empty GameObject đặt tên `Room3Starter`.
2. Add Component → **Room3Starter**.
3. Gán `Entry Monologue`: kéo InnerMonologue asset/prefab của Room 3 vào đây  
   *(nội dung gợi ý: cảm giác bị theo dõi trong bóng tối, linh cảm xấu)*.
4. `Start Delay` = **1.5** (để R3FlickerEvent chạy trước, tạo atmosphere).

> **Lưu ý:** Script tự đăng ký `RoomManager.OnRoomEntered` — không cần wiring thêm.

---

## 2. Room 3 — Đèn pin bắt đầu có tác dụng (FlashlightController)

**Script:** `Assets/Scripts/AnomalySystem/FlashlightController.cs`

FlashlightController đã được cập nhật để quét cả `DemonController` lẫn `FloorDemonAI`.  
Không cần thay đổi gì thêm trong Inspector nếu đã setup trước đây.

### Kiểm tra nhanh:
| Field | Giá trị gợi ý |
|---|---|
| Spot Angle | 25 |
| Light Range | 15 |
| Light Intensity | 3 |
| Light Vanish Time | 3 (giây chiếu để quỷ tan) |
| Cooldown Duration | 5 (giây khóa đèn sau khi quỷ tan) |
| Demon Layer | Layer chứa quỷ (hoặc để mặc định) |

---

## 3. Room 4 — Chase Sequence (Room4ChaseSequence)

**Script:** `Assets/Scripts/Gameplay/Room4ChaseSequence.cs`

### Các bước:
1. Tạo một Empty GameObject ở đầu hành lang Room 4, đặt tên `Room4ChaseTrigger`.
2. Add Component → **Box Collider** → bật `Is Trigger`.
3. Add Component → **Room4ChaseSequence**.
4. Gán các field:

| Field | Gán gì |
|---|---|
| Corridor Demon | Prefab quỷ hành lang (ban đầu **inactive** trong scene) |
| Demon Speed | 4.5 |
| Catch Distance | 1.5 |
| Chase Audio Source | AudioSource trên GameObject này |
| Chase Music | AudioClip nhạc chase căng thẳng |
| Exit Trigger | Collider trigger ở cửa cuối hành lang |
| Attack Anim Duration | 1.5 |
| Camera Zoom Time | 0.4 |
| Camera Zoom Distance | 2 |
| Player Controller | Script điều khiển nhân vật (tắt khi bị bắt) |

5. Tạo Empty GameObject ở cửa cuối hành lang `Room4ExitTrigger`:
   - Add **Box Collider** → `Is Trigger = true`
   - Add script nhỏ gọi `room4ChaseSequence.OnPlayerReachedExit()` khi player chạm vào.

### Prefab quỷ Room 4:
- Dùng chung prefab với Room 5 (có `DemonController`).
- Để **inactive** trong Hierarchy khi bắt đầu — `Room4ChaseSequence.Awake()` tự tắt.

---

## 4. Room 5 — 2 con quỷ 2 tầng (FloorDemonAI)

**Script:** `Assets/Scripts/Room5/FloorDemonAI.cs`

### Nguyên tắc hoạt động:
- Mỗi con quỷ bị giới hạn trong một khoảng Y (tầng) — không bao giờ gặp nhau.
- NavMesh vật lý ngăn đi qua tường/sàn; Y-bound logic ngăn con này theo đuổi player ở tầng kia.
- Khi player chạy sang tầng khác, quỷ chuyển sang trạng thái **Searching** (đến vị trí cuối cùng nhìn thấy), sau đó về **Patrolling**.

### Các bước chuẩn bị:

#### 4a. NavMesh
1. Mở **Window → AI → Navigation**.
2. Chọn tất cả geometry tầng 1, tầng 2 — đánh dấu **Navigation Static**.
3. Bake NavMesh riêng biệt cho từng tầng (đảm bảo không có liên kết giữa 2 tầng).

#### 4b. Tạo Demon_Floor1
1. Kéo prefab quỷ vào scene, đặt tên `Demon_Floor1`.
2. Add Component → **FloorDemonAI**.
3. Cấu hình:

| Field | Giá trị |
|---|---|
| Floor Y Min | **0** |
| Floor Y Max | **4.5** *(điều chỉnh theo map thực tế)* |
| Patrol Speed | 1.4 |
| Chase Speed | 3.8 |
| Detection Range | 12 |
| Catch Distance | 1.5 |
| Lose Floor Time | 2.5 (giây trước khi bỏ đuổi khi player sang tầng khác) |
| Search Duration | 4 (giây tìm kiếm trước khi quay lại patrol) |
| Light Vanish Time | 3 |
| Start Delay | **0** |
| Attack Anim Duration | 1.5 |
| Damage Flinch Duration | 0.6 |

4. Gán `Animator` component.
5. Gán `Patrol Waypoints`: tạo 4–5 Empty GameObjects trên **tầng 1**, assign vào array.

#### 4c. Tạo Demon_Floor2
1. Duplicate `Demon_Floor1`, đổi tên `Demon_Floor2`.
2. Kéo lên vị trí trên tầng 2.
3. Sửa:

| Field | Giá trị |
|---|---|
| Floor Y Min | **4.5** |
| Floor Y Max | **10** *(điều chỉnh theo map thực tế)* |
| Start Delay | **3** (để 2 con không kích hoạt cùng lúc) |

4. Thay `Patrol Waypoints` bằng 4–5 waypoints trên **tầng 2**.

#### 4d. Kiểm tra Gizmos
- Chọn từng demon trong Scene view → nhìn thấy:
  - **Hộp đỏ** = vùng tầng (floor bounds)
  - **Hình cầu vàng** = tầm phát hiện (detection range)
  - **Hình cầu đỏ nhỏ** = khoảng cách bắt (catch distance)
- Đảm bảo hộp đỏ của 2 con **không chồng lên nhau**.

---

## 5. Animator Controller — Monster

**File controller:** `Assets/MONS 6/PREFABS/` (hoặc nơi lưu controller mới)

> ⚠️ File `MonsterController.controller` đã bị xóa — cần tạo lại hoặc dùng controller khác.

### Các parameter bắt buộc (Bool):

| Parameter Name | Dùng cho |
|---|---|
| `is_idie_to_walk` | IDLE → WALK (patrol) |
| `is_idie_to_running` | IDLE → RUN (chase) |
| `is_idie_to_attack` | IDLE → ATTACK |
| `is_walk_to_running` | Guard: giữ RUN không tự exit về WALK |
| `is_attack_to_running` | Guard: giữ RUN không tự exit về ATTACK |
| `is_damage_to_running` | RUN ↔ DAMAGE (flinch) |
| `isDead` | AnyState → DEATH (khi bị đèn pin diệt) |

### Cách setup transitions:
- `IDLE → WALK`: `is_idie_to_walk = true`
- `IDLE → RUN`: `is_idie_to_running = true`
- `RUN → WALK`: `is_idie_to_running = false AND is_walk_to_running = false` *(guard ngăn auto-exit)*
- `RUN → ATTACK`: cần `is_attack_to_running = false` *(guard)*
- `RUN → DAMAGE`: `is_damage_to_running = true` → `DAMAGE → RUN`: `is_damage_to_running = false`
- `AnyState → DEATH`: `isDead = true` (Has Exit Time = false)
- Tắt **Has Exit Time** trên tất cả transitions để script kiểm soát hoàn toàn.

---

## 6. Checklist tổng hợp

- [ ] Tạo GameObject `Room3Starter` trong scene Hospital, gán InnerMonologue
- [ ] Kiểm tra FlashlightController đã có `Demon Layer` phù hợp
- [ ] Đặt trigger Box Collider cho `Room4ChaseTrigger` ở đầu hành lang
- [ ] Tạo/gán prefab quỷ Room 4 (inactive mặc định)
- [ ] Tạo `Room4ExitTrigger` ở cửa cuối hành lang Room 4
- [ ] Bake NavMesh riêng cho tầng 1 và tầng 2 của Room 5
- [ ] Đặt `Demon_Floor1` trên tầng 1, cấu hình Y Min=0, Y Max=4.5
- [ ] Đặt `Demon_Floor2` trên tầng 2, cấu hình Y Min=4.5, Y Max=10
- [ ] Tạo waypoints patrol cho từng tầng (4–5 điểm mỗi tầng)
- [ ] Kiểm tra Gizmos — 2 hộp đỏ không chồng nhau
- [ ] Animator Controller có đủ 7 parameter Bool ở trên
- [ ] Gán Animator vào FloorDemonAI và DemonController

---

*Cập nhật lần cuối: 2026-05-23*
