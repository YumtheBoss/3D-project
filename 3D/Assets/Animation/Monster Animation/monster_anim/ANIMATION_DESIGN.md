# Monster Animation Controller - Phân Tích & Design Plan

## 1. Tổng quan Controller hiện tại

### States đã có
| State | Speed | Transitions ra | Ghi chú |
|-------|-------|----------------|---------|
| **IDLE** | 1.0 | → WALK, RUN, ATTACK, DAMAGE | Default state |
| **WALK** | 1.5 | → IDLE, RUN, ATTACK, DAMAGE | |
| **RUN** | 2.0 | → WALK, IDLE, ATTACK, DAMAGE | |
| **ATTACK** | 1.0 | → IDLE, WALK, RUN, DAMAGE | |
| **DAMAGE** | 1.0 | → IDLE, WALK, RUN, ATTACK | |
| **KRIT ATTACK** | 1.0 | ❌ Không có transition nào | **Dead-end state** |
| **JUMP** | 1.0 | ❌ Không có transition nào | **Dead-end state** |
| **DEATH** | 1.0 | ❌ Không có transition nào | Terminal — đúng |

### Parameters hiện tại (10 bool)
```
is_Idie_to_running    ← ⚠️ Sai chính tả ("Idie" viết hoa I)
is_idie_to_attack
is_idie_to_walk
is_idie_to_damage
is_damage_to_walk
is_damage_to_attack
is_damage_to_running
is_walk_to_attack
is_walk_to_running
is_attack_to_running
```

---

## 2. Các vấn đề cốt lõi

### Vấn đề 1 — Thiết kế parameter sai kiểu (NGHIÊM TRỌNG)

**Nguyên nhân:** Mỗi parameter được đặt tên theo cặp `source_to_destination`, dẫn đến công thức **N×(N-1) parameters** cho N states. Với 8 states, lý thuyết cần 56 parameters. Hiện tại controller chỉ có 10 — nghĩa là đa số transitions bị thiếu hoặc dùng sai parameter.

**Ví dụ vấn đề thực tế:**
- Để chuyển từ RUN → IDLE cần parameter `is_run_to_idle` nhưng parameter này **không tồn tại**.
- Hiện tại transition ATTACK → IDLE đang dùng `is_Idie_to_running` (false) — logic ngược và sai ngữ nghĩa hoàn toàn.

---

### Vấn đề 2 — Tất cả parameters default = `true` (NGHIÊM TRỌNG)

Toàn bộ 10 parameters đều có `m_DefaultBool: 1` (true). Kết hợp với `HasExitTime = true`, khi game khởi động:

- IDLE sẽ **tự động** transition sang WALK, RUN, ATTACK, DAMAGE theo thứ tự exit time mà không cần kích hoạt từ script.
- Monster sẽ chạy qua các states ngẫu nhiên ngay lúc spawn.

---

### Vấn đề 3 — HasExitTime + Bool = Transition không kiểm soát được

Hiện tại **tất cả** transitions đều dùng:
- `HasExitTime: true` — transition tự kích hoạt khi clip đến % nhất định
- `Bool condition` — chỉ cần parameter == true/false

Với Bool, giá trị **tồn tại vĩnh viễn**. Khi bạn set `is_walk_to_running = true`, mọi lần WALK đạt exit time đều nhảy sang RUN, dù script không muốn.

**Cần dùng Trigger thay vì Bool** cho các transition one-shot (ATTACK, DAMAGE).

---

### Vấn đề 4 — KRIT ATTACK và JUMP là dead-end states

Cả hai states này:
- **Không có transition ra** → khi vào, animator bị kẹt vĩnh viễn
- **KRIT ATTACK** không có transition vào từ bất kỳ state nào trong transitions hiện tại
- **JUMP** tương tự — tồn tại trong state machine nhưng không có đường vào/ra

---

### Vấn đề 5 — Tên parameter không nhất quán

```
is_Idie_to_running    ← "I" viết hoa (typo)
is_idie_to_attack     ← "i" viết thường
```

Khi script C# gọi `animator.SetBool("is_idie_to_running", true)` sẽ **không hoạt động** vì tên thực là `is_Idie_to_running`. Unity phân biệt chữ hoa/thường.

---

## 3. Thiết kế lại được đề xuất

### Nguyên tắc thiết kế đúng

Thay vì `is_stateA_to_stateB`, dùng **mô tả trạng thái hiện tại** hoặc **trigger hành động**:

| Loại | Dùng cho | Kiểu parameter |
|------|----------|----------------|
| Trạng thái liên tục | IDLE / WALK / RUN | `Float speed` hoặc `Bool isChasing` |
| Hành động one-shot | ATTACK, DAMAGE, JUMP | `Trigger` |
| Trạng thái terminal | DEATH | `Bool isDead` |
| Hành động đặc biệt | KRIT ATTACK | `Trigger kritAttack` |

### Parameters đề xuất (6 thay vì 10)

```
speed           [Float]   0=idle, 0.5=walk, 1.0=run
isChasing       [Bool]    false=patrol, true=đang đuổi player
attackTrigger   [Trigger] kích hoạt ATTACK
kritTrigger     [Trigger] kích hoạt KRIT ATTACK  
damageTrigger   [Trigger] kích hoạt DAMAGE (bị đèn pin chiếu)
isDead          [Bool]    kích hoạt DEATH (terminal)
```

### Flow State Machine đề xuất

```
                    ┌─────────────────────────────────────┐
                    │                                     │
              speed=0                                     │
         ┌──► IDLE ◄──────────────────────────────┐      │
         │     │                                   │      │
         │   speed>0                               │      │
         │   isChasing=false                       │      │
         │     ▼                                   │      │
         │   WALK ──── speed>0.7 / isChasing ───► RUN    │
         │     │  ◄─── speed<0.7 ─────────────────  │    │
         │     │                                    │    │
         │   [attackTrigger]              [attackTrigger] │
         │     ▼                                    ▼    │
         │   ATTACK ──────────────────────────────────   │
         │   (exit → IDLE)                               │
         │                                               │
         │  [damageTrigger từ bất kỳ state]              │
         │     ▼                                         │
         │   DAMAGE                                      │
         │   (exit → IDLE)                               │
         │                                               │
         │  [kritTrigger]                                │
         │     ▼                                         │
         │  KRIT ATTACK                                  │
         │   (exit → IDLE)                               │
         │                                               │
         │  [jumpTrigger] (nếu cần)                      │
         │     ▼                                         │
         │   JUMP                                        │
         │   (exit → RUN hoặc IDLE)                      │
         │                                               │
         │  [isDead = true từ bất kỳ state]              │
         └──── DEATH (terminal, không có exit) ──────────┘
```

### Transition rules chi tiết

| Từ | Đến | Điều kiện | HasExitTime |
|----|-----|-----------|-------------|
| Entry | IDLE | — | — |
| IDLE | WALK | speed > 0.1 | false |
| IDLE | RUN | isChasing = true | false |
| IDLE | ATTACK | attackTrigger | false |
| IDLE | DAMAGE | damageTrigger | false |
| IDLE | DEATH | isDead = true | false |
| WALK | IDLE | speed < 0.1 | false |
| WALK | RUN | isChasing = true | false |
| WALK | ATTACK | attackTrigger | false |
| WALK | DAMAGE | damageTrigger | false |
| RUN | WALK | isChasing = false | false |
| RUN | ATTACK | attackTrigger | false |
| RUN | DAMAGE | damageTrigger | false |
| ATTACK | IDLE | exit time 95% | **true** |
| KRIT ATTACK | IDLE | exit time 95% | **true** |
| DAMAGE | IDLE | exit time 90% | **true** |
| JUMP | RUN | exit time 90% | **true** |
| Any | DEATH | isDead = true | false |

> **Quy tắc:** Chỉ dùng `HasExitTime = true` cho animations phải chạy hết (ATTACK, DAMAGE, KRIT ATTACK, JUMP). Mọi transition dựa trên điều kiện game logic dùng `HasExitTime = false`.

---

## 4. Vai trò của từng animation trong game (Room 4 Chase)

| State | Khi nào dùng | Ghi chú |
|-------|-------------|---------|
| **IDLE** | Monster đứng yên, chưa phát hiện player | Đầu Room 4 hoặc sau khi mất dấu |
| **WALK** | Tuần tra, tìm kiếm player | Tốc độ chậm, AI NavMesh |
| **RUN** | Đang đuổi player (chase sequence) | `isChasing = true` khi thấy player |
| **ATTACK** | Bắt được player (khoảng cách gần) | Trigger game over / jumpscare |
| **KRIT ATTACK** | Tấn công đặc biệt (pounce?) | Dùng khi player ở khoảng cách xa |
| **JUMP** | Nhảy qua chướng ngại vật | Nếu có mechanic nhảy |
| **DAMAGE** | Bị đèn pin chiếu thẳng | Hoạt animation co rút, chậm lại |
| **DEATH** | HP về 0 / đèn pin sustained | Kết thúc Room 4 (good ending) |

---

## 5. Kế hoạch sửa chữa

### Bước 1 — Sửa ngay (không cần thiết kế lại hoàn toàn)
- [ ] Đổi `is_Idie_to_running` → `is_idie_to_running` (viết thường toàn bộ)
- [ ] Đổi tất cả parameters về `DefaultBool = false`
- [ ] Thêm transition ra cho KRIT ATTACK → IDLE (exit time 95%)
- [ ] Thêm transition ra cho JUMP → RUN (exit time 90%)
- [ ] Kiểm tra DEATH chỉ có transition vào, không có ra

### Bước 2 — Refactor parameters (khuyến nghị)
- [ ] Xóa 10 bool parameters cũ
- [ ] Thêm `speed` (Float), `isChasing` (Bool), `isDead` (Bool)
- [ ] Thêm `attackTrigger`, `damageTrigger`, `kritTrigger` (Trigger)
- [ ] Cập nhật tất cả transitions theo bảng Transition rules ở trên

### Bước 3 — Tích hợp với script
- [ ] `DemonController.cs` / `Room4ChaseSequence.cs` set parameters theo logic AI
- [ ] Khi NavMesh agent speed > threshold → `isChasing = true`
- [ ] Khi flashlight hit monster → gọi `damageTrigger`
- [ ] Khi player bị bắt → gọi `attackTrigger`

---

## 6. Lưu ý về context game

Game là horror walking sim — monster xuất hiện ở **Room 4** (chase sequence). Thiết kế animation cần phản ánh:

- Monster **không chết dễ** → DAMAGE chỉ làm chậm, không giết ngay
- Monster chỉ **chết bằng đèn pin sustained** ở điều kiện đặc biệt → DEATH là rare event (good ending)
- ATTACK animation = game over cho player → cần snappy, không có thời gian dodge
- DAMAGE animation = player có cơ hội thoát → cần đủ dài để player chạy
