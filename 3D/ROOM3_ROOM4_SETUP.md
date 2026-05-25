# Setup Guide — Room 3 & Room 4

> Cập nhật: 2026-05-26

---

## ROOM 3 — Hành lang bệnh viện nhỏ

### Luồng hoạt động

```
Player vào Room 3
    ↓ [0.3s] Đèn trắng #1 bắt đầu nhấp nháy  (Room3Starter)
    ↓ [1.5s] Inner monologue Entry phát        (Room3Starter)

Player đến gần tờ giấy → [E]
    ↓ Đọc ghi chú Kinh Thánh                  (BibleNotePickup)

Player nhấn Đóng
    ↓ Đèn #2 (gần cửa) tắt → đèn đỏ tối bật  (BibleNotePickup)
    ↓ Inner monologue Post-Note phát           (BibleNotePickup)
```

---

### Bước 1 — Chuẩn bị đèn

| Tên GO | Loại | Cài đặt | Ghi chú |
|---|---|---|---|
| `R3_FlickerLight` | Point/Spot Light | Màu **trắng**, Intensity 1.5 | Thêm component **LightFlicker**, `flickerOnEnable = false` |
| `R3_NormalLight` | Point Light | Màu trắng/vàng nhạt, Intensity 1.2 | Đặt **gần cửa dẫn vào tờ giấy** |
| `R3_ReplacementLight` | Point Light | Màu **đỏ tối** `#8B1A1A`, Intensity 0.5 | **Tắt trong Inspector** từ đầu (inactive) |

> Đặt `R3_NormalLight` và `R3_ReplacementLight` cùng vị trí (chồng lên nhau) để ánh sáng đổi chỗ tự nhiên.

---

### Bước 2 — Room3Starter

1. Tạo Empty GO tên `Room3Starter` trong scene.
2. Add Component → **Room3Starter**

| Field | Gán |
|---|---|
| `Entry Monologue` | InnerMonologue GO với lines bên dưới |
| `Start Delay` | **1.5** |
| `Flicker Light` | `R3_FlickerLight` (có LightFlicker) |
| `Flicker Start Delay` | **0.3** |

**Inner Monologue Entry — gợi ý lines:**
```
"Hành lang này... tối hơn tôi nhớ."
"Đèn cứ nhấp nháy mãi. Như thể đang thở."
"Tôi cảm thấy có gì đó... đứng sau mình."
"Đừng nhìn lại. Cứ đi."
```

---

### Bước 3 — BibleNotePickup (tờ giấy 3D)

1. Đặt prop giấy 3D trong scene, gần cuối hành lang.
2. Add Component → **BibleNotePickup**

| Field | Gán |
|---|---|
| `Note Panel UI` | Panel UI trong Canvas (ẩn mặc định) |
| `Note Content Text` | TextMeshProUGUI bên trong Panel |
| `Close Button` | Button "Đóng" |
| `Hint Text` | TMP hint "[E] để nhặt" |
| `Light To Replace` | `R3_NormalLight` |
| `Replacement Light` | `R3_ReplacementLight` (đang inactive) |
| `Post Note Monologue` | InnerMonologue GO với lines bên dưới |
| `Pickup Sound` | AudioClip tiếng nhặt giấy |

**Inner Monologue Post-Note — gợi ý lines:**
```
"Ánh sáng... là vũ khí."
"Ai đó đã biết điều này trước khi tôi đến đây."
"Đèn pin. Đó là thứ duy nhất tôi có."
"Nhưng họ nói đèn sẽ tắt. Và tôi không được hoảng loạn."
"...Dễ nói hơn làm."
```

---

### Bước 4 — UI Panel (dùng chung cho cả 2 note)

1. Trong **Canvas** → tạo **Panel** → đặt tên `NotePanel_R3`
2. Bên trong:
   - **TextMeshProUGUI** tên `NoteText` — font `Darkella Demo SDF` (hoặc font có sẵn)
   - **Button** tên `CloseBtn` — text "Đóng" hoặc "✕"
3. Tắt Panel mặc định (`SetActive false`)

---

### Checklist Room 3

- [ ] `R3_FlickerLight` — Point Light trắng + **LightFlicker** component, `flickerOnEnable = false`
- [ ] `R3_NormalLight` — Point Light gần cửa, **bật** lúc đầu
- [ ] `R3_ReplacementLight` — Point Light đỏ tối, **tắt** lúc đầu
- [ ] Empty GO `Room3Starter` + gán đủ fields
- [ ] Prop giấy 3D + **BibleNotePickup** + gán đủ fields
- [ ] Canvas Panel `NotePanel_R3` + TMP Text + Close Button
- [ ] 2 InnerMonologue GO (entry + post-note) với lines đã điền

---
---

## ROOM 4 — Căn phòng đỏ với ma nơ canh

### Luồng hoạt động

```
Player vào Room 4
    ↓ Ánh sáng đỏ thẫm + ambient đỏ           (Room4Environment)
    ↓ Con gấu bông trong cũi bắt đầu phát sáng (TeddyBearGlow)

Player đến gần ma nơ canh → [E]
    ↓ Đọc tờ giấy cảnh báo                     (MannequinNote)

Player nhấn Đóng
    ↓ Inner monologue "chạy đi" phát           (MannequinNote)

Player bước qua cửa cuối phòng
    ↓ Room4ChaseSequence kích hoạt             (Room4ChaseSequence trigger)
    ↓ Quái xuất hiện đằng sau, đuổi
    ↓ Player chạy tới cửa thoát → Room 5       (Room4ExitTrigger)
    ↓ Bị bắt → Bad Ending
```

---

### Bước 1 — Room4Environment

1. Tạo Empty GO `Room4Environment`, add Component → **Room4Environment**

| Field | Gán |
|---|---|
| `Main Light` | Directional Light chính của scene |
| `Room4 Light Color` | `#991A1A` (đỏ thẫm) |
| `Room4 Ambient Color` | `#260505` (đỏ cực tối) |
| `Aggressive Music` | AudioClip nhạc căng thẳng |
| `Audio Source` | AudioSource trên GO này |

> Không cần gán `Wall Pictures` / entities nếu không có tranh.

---

### Bước 2 — Cũi + Con gấu bông

1. Đặt prop cái **cũi** (cage) ở giữa phòng.
2. Bên trong đặt **prop con gấu bông**.
3. Chọn GO con gấu → Add Component → **TeddyBearGlow**

| Field | Gán |
|---|---|
| `Glow Color` | `#FFB85A` (vàng ấm) — hoặc thay đổi tùy ý |
| `Min Intensity` | 0.2 |
| `Max Intensity` | 1.4 |
| `Pulse Speed` | 1.2 |
| `Light Range` | 2.5 |
| `Light Offset` | (0, 0.3, 0) |

> Đèn tự tạo khi chạy — không cần thêm Light component thủ công.

---

### Bước 3 — Ma nơ canh + Tờ giấy

1. Đặt **3–4 ma nơ canh** xung quanh cũi, xoay mặt về phía cũi (chỉ vào gấu).
2. Chọn **1 ma nơ canh** để đặt tờ giấy (ma nơ canh gần player nhất).
3. Đặt prop giấy 3D trên tay/ngực ma nơ canh.
4. Add Component → **MannequinNote** lên prop giấy đó.

| Field | Gán |
|---|---|
| `Pickup Range` | 2.5 |
| `Note Panel UI` | Panel UI trong Canvas (ẩn mặc định) |
| `Note Content Text` | TextMeshProUGUI bên trong Panel |
| `Close Button` | Button "Đóng" |
| `Hint Text` | TMP hint "[E] để đọc" |
| `Post Read Monologue` | InnerMonologue GO với lines bên dưới |
| `Pickup Sound` | AudioClip tiếng nhặt giấy |

**Inner Monologue Post-Read Room 4 — gợi ý lines:**
```
"Mười hai người..."
"Họ đều thử và không ai quay lại."
"Cánh cửa cuối. Đó là lối thoát duy nhất."
"Chạy. Đừng nhìn lại."
```

---

### Bước 4 — Trigger kích hoạt chase (cửa cuối phòng)

**Room4ChaseSequence** — đặt ở **ngưỡng cửa dẫn ra hành lang**:

1. Tạo Empty GO `R4_ChaseTrigger` ở cửa cuối phòng.
2. Add Component → **Box Collider** → bật `Is Trigger`
3. Add Component → **Room4ChaseSequence**

| Field | Gán |
|---|---|
| `Corridor Demon` | Prefab quỷ (inactive mặc định trong Hierarchy) |
| `Demon Speed` | 4.5 |
| `Catch Distance` | 1.5 |
| `Chase Audio Source` | AudioSource trên GO này |
| `Chase Music` | AudioClip nhạc đuổi |
| `Exit Trigger` | Collider của `R4_ExitDoor` (xem bên dưới) |
| `Player Controller` | Script **FirstPersonController** trên Player |
| `Attack Anim Duration` | 1.5 |
| `Camera Zoom Time` | 0.4 |
| `Camera Zoom Distance` | 2 |

---

### Bước 5 — Cửa thoát (Room 4 → Room 5)

1. Tạo Empty GO `R4_ExitDoor` ở **cuối hành lang** (sau trigger chase).
2. Add Component → **Box Collider** → `Is Trigger = true`
3. Add Component → **Room4ExitTrigger**

| Field | Gán |
|---|---|
| `Chase Sequence` | `R4_ChaseTrigger` (có Room4ChaseSequence) |

> Trigger này bị **tắt** lúc đầu — `Room4ChaseSequence.BeginChase()` tự bật khi quái xuất hiện.

---

### Bước 6 — Prefab quỷ hành lang

1. Kéo prefab quỷ vào Hierarchy, đặt tên `R4_CorridorDemon`.
2. Đặt **phía sau** cửa chase trigger (ngoài tầm nhìn player).
3. **Tắt GameObject** trong Inspector (inactive) — script tự bật.
4. Đảm bảo có **NavMeshAgent** + **Animator** + **DemonController** (hoặc NavMeshAgent thuần).

> Bake NavMesh cho hành lang Room 4 trước (Window → AI → Navigation → Bake).

---

### Sơ đồ đặt object Room 4

```
[Spawn point R4]
      │
      ▼
[Phòng đỏ rực]
  ┌─────────────────────────┐
  │  [Mannequin]  [Cũi+Gấu] │  ← ma nơ canh xung quanh cũi
  │  [Mannequin]  [Mannequin]│
  │       [Tờ giấy trên tay] │  ← MannequinNote
  └────────────┬────────────┘
               │ [Cửa cuối phòng]
               ▼
       [R4_ChaseTrigger]  ← Room4ChaseSequence (Box Collider trigger)
               │
    [Hành lang ngắn]
    [R4_CorridorDemon đứng đây, inactive]
               │
               ▼
       [R4_ExitDoor]  ← Room4ExitTrigger → Room 5
```

---

### Checklist Room 4

- [ ] `Room4Environment` GO + gán Light, AudioSource, AudioClip
- [ ] Prop **cũi** + prop **gấu bông** bên trong + **TeddyBearGlow** component
- [ ] 3–4 **ma nơ canh** xung quanh cũi, xoay mặt vào gấu
- [ ] Prop giấy 3D trên tay ma nơ canh + **MannequinNote** component + gán UI
- [ ] Canvas Panel `NotePanel_R4` + TMP Text + Close Button (tắt mặc định)
- [ ] InnerMonologue GO post-read với lines đã điền
- [ ] Empty GO `R4_ChaseTrigger` + **Box Collider (Is Trigger)** + **Room4ChaseSequence** + gán fields
- [ ] Prefab quỷ `R4_CorridorDemon` đặt phía sau trigger, **inactive**
- [ ] **Bake NavMesh** cho hành lang Room 4
- [ ] Empty GO `R4_ExitDoor` + **Box Collider (Is Trigger)** + **Room4ExitTrigger** + gán chase
- [ ] Test: vào Room 4 → đèn đỏ → gấu sáng → đọc giấy → monologue → qua cửa → chase → thoát/bị bắt
