# Room 3 — Setup Guide (Cập nhật logic đèn + Monologue)

> Cập nhật lần cuối: 2026-05-25

---

## Tổng quan logic đèn Room 3

| Đèn | Màu | Hành vi |
|---|---|---|
| **Light #1** | Trắng | Nhấp nháy ngay khi player bước vào Room 3 |
| **Light #2** | Trắng/Neutral | Tắt và được thay bằng đèn mới (đỏ tối/cam) sau khi player đọc xong ghi chú Kinh Thánh |

---

## 1. Chuẩn bị đèn trong scene

### Light #1 — Đèn trắng nhấp nháy

1. Chọn đèn trong scene (hoặc tạo `Point Light` / `Spot Light` mới), đặt tên `Room3_FlickerLight`.
2. Đặt màu **trắng** (`#FFFFFF`), intensity tuỳ ý (gợi ý: **1.5**).
3. Add Component → **LightFlicker**.
4. Cấu hình `LightFlicker`:

| Field | Giá trị gợi ý |
|---|---|
| Min Off Time | 0.05 |
| Max Off Time | 0.4 |
| Min On Time | 0.08 |
| Max On Time | 0.6 |
| Flicker On Enable | **false** (Room3Starter sẽ gọi thủ công) |

> **Quan trọng:** Để `Flicker On Enable = false` — Room3Starter sẽ gọi `StartFlicker()` sau khi RoomManager chuyển sang Room 3.

---

### Light #2 — Đèn bị thay thế

1. Chọn đèn thứ 2 trong scene, đặt tên `Room3_NormalLight`.
2. Để đèn này bật bình thường lúc đầu.

### Replacement Light — Đèn thay thế

1. Tạo thêm một `Point Light` mới, đặt tên `Room3_ReplacementLight`.
2. Đặt màu **đỏ tối** (`#8B0000`) hoặc **cam tắt** (`#FF4400`), intensity thấp (gợi ý: **0.6**).
3. Trong Hierarchy, **tắt GameObject này** (uncheck ở Inspector) — script sẽ bật lên khi cần.

---

## 2. Setup Room3Starter

**Script:** `Assets/Scripts/Room3/Room3Starter.cs`

1. Tạo Empty GameObject trong scene Hospital, đặt tên `Room3Starter`.
2. Add Component → **Room3Starter**.
3. Gán các field:

| Field | Gán gì |
|---|---|
| `Entry Monologue` | InnerMonologue GO chứa lines entry (xem mục 5) |
| `Start Delay` | **1.5** |
| `Flicker Light` | `Room3_FlickerLight` (có LightFlicker component) |
| `Flicker Start Delay` | **0.3** |

> Script tự đăng ký `RoomManager.OnRoomEntered` — không cần wiring thêm.

---

## 3. Setup BibleNotePickup

**Script:** `Assets/Scripts/Room3/BibleNotePickup.cs`

1. Chọn GameObject mảnh giấy 3D trong scene, add Component → **BibleNotePickup**.
2. Gán các field:

| Field | Gán gì |
|---|---|
| `Note Panel UI` | Panel UI từ Canvas (ẩn lúc đầu) |
| `Note Content Text` | TextMeshProUGUI bên trong Panel |
| `Close Button` | Button đóng ghi chú |
| `Hint Text` | TextMeshProUGUI hiện hint "[E] để nhặt" |
| `Pickup Sound` | AudioClip tiếng nhặt giấy |
| `Light To Replace` | `Room3_NormalLight` |
| `Replacement Light` | `Room3_ReplacementLight` (đang inactive) |
| `Post Note Monologue` | InnerMonologue GO chứa lines post-note (xem mục 5) |

### Tạo UI Panel cho ghi chú

1. Trong Canvas, tạo Panel → đặt tên `BibleNotePanel`.
2. Bên trong Panel:
   - Thêm **TextMeshProUGUI** đặt tên `NoteText` — font gợi ý: `Darkella Demo SDF`.
   - Thêm **Button** đặt tên `CloseButton`, text: `"Đóng"` hoặc `"✕"`.
3. Tắt `BibleNotePanel.SetActive(false)` trong Inspector — script tự bật khi nhặt.

---

## 4. Luồng hoạt động

```
Player vào Room 3
    │
    ▼
RoomManager.OnRoomEntered(Room3) phát
    │
    ├─ [0.3s] Room3_FlickerLight.StartFlicker()   ← đèn trắng bắt đầu nhấp nháy
    └─ [1.5s] EntryMonologue.PlayManually()        ← inner monologue entry phát

Player đến gần mảnh giấy → nhấn [E]
    │
    ▼
BibleNotePickup mở ghi chú Kinh Thánh (Time.timeScale = 0)

Player nhấn "Đóng" hoặc [ESC]
    │
    ├─ Room3_NormalLight → SetActive(false)        ← đèn #2 tắt
    ├─ Room3_ReplacementLight → SetActive(true)    ← đèn mới (đỏ tối) bật
    └─ PostNoteMonologue.PlayManually()            ← inner monologue post-note phát
```

---

## 5. Inner Monologue — Nội dung gợi ý

### Entry Monologue (khi bước vào Room 3)

Chọn một trong các phương án sau, điền vào component `InnerMonologue`:

**Phương án A — Cảm giác bị quan sát (khuyến nghị):**
```
"Hành lang này... tối hơn tôi nhớ."
"Đèn cứ nhấp nháy mãi. Như thể đang thở."
"Tôi cảm thấy có gì đó... đứng sau mình."
"Đừng nhìn lại. Cứ đi."
```

**Phương án B — Ngờ vực thực tại:**
```
"Tại sao không có ai ở đây?"
"Bệnh viện này đáng lẽ phải có người..."
"Tiếng... có gì đó vừa di chuyển."
"Tôi chỉ cần tìm lối ra. Chỉ vậy thôi."
```

**Phương án C — Ngắn, vỡ câu, căng thẳng:**
```
"Tối quá."
"Đèn... đừng tắt."
"Cái gì thế? Phía cuối hành lang?"
"Không. Không có gì cả. Tôi ổn."
```

---

### Post-Note Monologue (sau khi đóng ghi chú Kinh Thánh)

**Phương án A — Nhận ra cơ chế, pha trộn hy vọng và sợ (khuyến nghị):**
```
"Bóng tối không thể tồn tại khi có ánh sáng."
"Ai đó đã biết điều này... trước khi tôi đến đây."
"Đèn pin. Đó là thứ duy nhất tôi có."
"Nhưng họ nói đèn sẽ tắt. Và tôi không được hoảng loạn."
"...Dễ nói hơn làm."
```

**Phương án B — Nói chuyện với bản thân, giọng run rẩy:**
```
"Ổn. Được rồi. Tôi hiểu rồi."
"Chiếu thẳng vào chúng. Đủ lâu."
"Nhưng sau đó đèn sẽ tắt."
"Người viết ghi chú này... họ có thoát ra không?"
```

**Phương án C — Mang tính tâm linh hơn:**
```
"'Đức Giê-hô-va là sự sáng và sự cứu rỗi của tôi...'"
"Tôi không tin những điều này. Nhưng bây giờ..."
"Nếu ánh sáng là thứ duy nhất có thể ngăn chúng..."
"Thì tôi sẽ không để đèn này tắt."
```

---

## 6. Gợi ý cấu hình InnerMonologue component

| Field | Giá trị |
|---|---|
| Delay Between Lines | **3.0** giây |
| Display Duration | **4.0** giây |
| Fade Time | **0.5** giây |
| Play On Trigger | false (gọi thủ công qua `PlayManually()`) |

---

## 7. Checklist

- [ ] Tạo `Room3_FlickerLight` — Point/Spot Light trắng, add **LightFlicker**, `flickerOnEnable = false`
- [ ] Tạo `Room3_NormalLight` — đèn thường, bật lúc đầu
- [ ] Tạo `Room3_ReplacementLight` — đèn đỏ tối, **tắt trong Inspector** từ đầu
- [ ] Tạo Empty GO `Room3Starter`, gán **Room3Starter** component, điền đủ fields
- [ ] Gán **BibleNotePickup** lên mảnh giấy 3D, điền đủ fields (kể cả 2 đèn và post-note monologue)
- [ ] Tạo `BibleNotePanel` trong Canvas (TextMeshProUGUI + CloseButton), tắt mặc định
- [ ] Tạo 2 InnerMonologue GO (entry + post-note), điền lines theo phương án đã chọn
- [ ] Test: vào Room 3 → đèn trắng nhấp nháy → nhặt ghi chú → đóng → đèn đổi + monologue phát
