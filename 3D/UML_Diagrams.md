# Biểu đồ UML Thiết kế Game 3D Anomaly

Tài liệu này bao gồm các sơ đồ UML chuyên biệt để mô tả kiến trúc và luồng hoạt động của game 3D Anomaly, được thiết kế bám sát vào cấu trúc mã nguồn thực tế của dự án (`AnomalySystem`, `Mobile Input`, `FirstPersonController`, `UI`). 

Bạn có thể copy mã code bên dưới để hiển thị trên các công cụ hỗ trợ Mermaid (như GitHub, Obsidian, Notion) hoặc paste trực tiếp vào các trình vẽ sơ đồ.

---

## 1. Sơ đồ Use-case (Use-case Diagram)
Mô tả các tính năng chính mà người chơi (Player) có thể tương tác với hệ thống game.

```mermaid
flowchart LR
    Player([Người chơi])
    
    subgraph Hệ thống Game Anomaly
        direction TB
        UC1((Khởi động Game))
        UC2((Di chuyển & Quay góc nhìn))
        UC3((Kiểm tra Anomaly))
        UC4((Đi qua cửa\nTiến / Lùi))
        UC5((Tạm dừng Game / Cài đặt))
        
        UC6((Tăng Cấp độ))
        UC7((Reset Cấp độ))
        UC8((Chạm trán Jumpscare\nNgẫu nhiên))
    end
    
    Player --> UC1
    Player --> UC2
    Player --> UC3
    Player --> UC4
    Player --> UC5
    Player --> UC8
    
    UC4 -.->|Lựa chọn đúng| UC6
    UC4 -.->|Lựa chọn sai| UC7
    
    classDef usecase fill:#f9f9f9,stroke:#333,stroke-width:2px;
    class UC1,UC2,UC3,UC4,UC5,UC6,UC7,UC8 usecase;
```

---

## 2. Sơ đồ Tuần tự (Sequence Diagram)
Mô tả luồng tương tác chi tiết khi người chơi đưa ra quyết định đi qua một cánh cửa (Đi tiếp hoặc Quay lại) tại hành lang.

```mermaid
sequenceDiagram
    actor Player as Người chơi
    participant Door as DoorChoice
    participant Level as LevelManager
    participant Anomaly as AnomalyManager
    participant Jump as JumpscareController
    participant UI as LevelDisplay
    
    Player->>Door: Nhân vật đi qua cửa (OnTriggerEnter)
    Door->>Level: CheckChoice(isForwardDoor)
    Level->>Anomaly: HasAnomaly()
    Anomaly-->>Level: trả về true/false
    
    alt Quyết định ĐÚNG
        Level->>Level: IncreaseLevel()
        Level->>UI: Cập nhật Text Level (Level += 1)
        Level->>Anomaly: Sinh Segment tiếp theo (Có/Không có Anomaly)
    else Quyết định SAI
        Level->>Level: ResetLevel() (Level = 0)
        Level->>UI: Cập nhật Text Level
        Level->>Anomaly: Reset về Segment ban đầu
    end
```

---

## 3. Sơ đồ Hoạt động (Activity Diagram)
Mô tả quy trình logic cốt lõi (Core Game Loop) của hệ thống level.

```mermaid
stateDiagram-v2
    [*] --> Start: Bắt đầu game
    Start --> Generate: Tạo hành lang (Random Anomaly)
    Generate --> Explore: Người chơi khám phá
    
    Explore --> Decision: Đưa ra quyết định
    
    state Decision {
        [*] --> Forward: Đi tiếp (Forward Door)
        [*] --> Backward: Quay lại (Backward Door)
    }
    
    Decision --> Validate: Xác thực lựa chọn
    
    state Validate {
        state "Lựa chọn có đúng không?" as check_choice
        check_choice --> Correct: Đi tiếp khi KHÔNG CÓ Anomaly\nHoặc Quay lại khi CÓ Anomaly
        check_choice --> Wrong: Đi tiếp khi CÓ Anomaly\nHoặc Quay lại khi KHÔNG CÓ Anomaly
    }
    
    Correct --> LevelUp: Tăng Level
    LevelUp --> CheckWin: Kiểm tra Max Level
    CheckWin --> Win: Đã đạt Max Level
    CheckWin --> Generate: Chưa đạt Max
    
    Wrong --> Reset: Đưa Level về 0
    Reset --> Generate
    
    Win --> [*]
```

---

## 4. Sơ đồ Lớp (Class Diagram)
Mô tả các thành phần class chính yếu trong dự án và mối quan hệ giữa chúng.

```mermaid
classDiagram
    class FirstPersonController {
        +float walkSpeed
        +float sprintSpeed
        -Rigidbody rb
        +Move()
        +Jump()
        +Look()
    }
    
    class LevelManager {
        +int currentLevel
        +int maxLevel
        +CheckChoice(bool goForward)
        -IncreaseLevel()
        -ResetLevel()
    }
    
    class AnomalyManager {
        -List~AnomalyObject~ anomalies
        +bool hasAnomaly
        +SpawnAnomaly()
        +ClearAnomaly()
    }
    
    class DoorChoice {
        +bool isForwardDoor
        -OnTriggerEnter(Collider other)
    }
    
    class JumpscareController {
        +GameObject jumpscareUI
        +AudioClip jumpscareSound
        +TriggerJumpscare()
    }
    
    class MobileInputs {
        <<System>>
        +MobileJoystick joystick
        +MobileTouchCamera touchCamera
        +MobileButtons buttons
    }

    LevelManager --> AnomalyManager : Điều khiển Anomaly
    AnomalyManager --> JumpscareController : Kích hoạt hù dọa ngẫu nhiên
    DoorChoice --> LevelManager : Gửi tín hiệu lựa chọn
    FirstPersonController ..> MobileInputs : Nhận dữ liệu (Cross-platform)
```

---

## 5. Sơ đồ Hệ thống / Kiến trúc Component (System Diagram)
Mô tả kiến trúc chia tách các Subsystem trong game (Điều khiển, Logic chính, Giao diện, Âm thanh).

```mermaid
flowchart TD
    subgraph InputSystem [Hệ thống Điều khiển & Nhập liệu]
        PC[PC Input: Bàn phím / Chuột]
        Mobile[Mobile Input: Touch / Joystick]
    end
    
    subgraph PlayerSystem [Hệ thống Player]
        FPC[FirstPersonController]
        Audio[Audio Manager / Footstep]
    end
    
    subgraph GameLogic [Hệ thống Core Logic]
        LM[Level Manager]
        AM[Anomaly Manager]
        DC[Door Choice Triggers]
    end
    
    subgraph Presentation [Hệ thống Hiển thị & UI]
        UI[Main Menu / Pause Menu]
        JC[Jumpscare Controller]
        LD[Level Display]
    end

    InputSystem -->|Gửi tín hiệu| FPC
    FPC -->|Va chạm Collider| DC
    DC -->|Kiểm tra kết quả| LM
    LM -->|Phối hợp sinh Anomaly| AM
    AM -->|Kích hoạt hù dọa| JC
    LM -->|Cập nhật UI| LD
    FPC -->|Gọi phát âm thanh| Audio
    UI -.->|Quản lý trạng thái| GameLogic
```

---

## 6. Sơ đồ Triển khai (Deployment Diagram)
Mô tả sơ đồ vật lý của ứng dụng khi được build và chạy trên thiết bị đầu cuối của người chơi (hỗ trợ cả PC & Mobile).

```mermaid
flowchart TD
    subgraph Hardware [Phần cứng thiết bị]
        CPU[CPU & GPU]
        Screen[Màn hình / Cảm ứng]
        Speaker[Tai nghe / Loa]
        Storage[Ổ cứng / Bộ nhớ Flash]
    end
    
    subgraph OS [Hệ điều hành & Unity Engine]
        GameApp[Anomaly 3D Game Client]
        SaveData[PlayerPrefs / Settings Data]
    end
    
    GameApp -.->|Đọc/Ghi dữ liệu| SaveData
    GameApp ===>|Render Hình ảnh & Input| Screen
    GameApp -.->|Xuất Âm thanh 3D| Speaker
    GameApp ===>|Tính toán & Xử lý| CPU
    SaveData -.->|Lưu trữ vật lý| Storage
```

---

## 7. Sơ đồ Kiến trúc Component (Component-Based Architecture Diagram)
Mô hình Component-based là đặc trưng cốt lõi của các game engine như Unity. Sơ đồ này mô tả cách các thực thể (GameObjects) được lắp ráp từ các thành phần (Components) độc lập và cách các Component này giao tiếp với nhau để tạo nên logic game hoàn chỉnh.

```mermaid
flowchart TB
    %% GameObjects (Entities)
    subgraph Player_GameObject ["🎮 GameObject: Player"]
        direction TB
        Transform_P["«Component»<br/>Transform"]
        RB["«Component»<br/>Rigidbody"]
        Col["«Component»<br/>Capsule Collider"]
        FPC["«Script Component»<br/>FirstPersonController"]
        Audio["«Component»<br/>AudioSource"]
    end

    subgraph Input_GameObject ["📱 GameObject: Input Manager (Mobile)"]
        direction TB
        TouchCam["«Script Component»<br/>MobileTouchCamera"]
        Joy["«Script Component»<br/>MobileJoystick"]
    end

    subgraph Door_GameObject ["🚪 GameObject: Corridor Door"]
        direction TB
        Transform_D["«Component»<br/>Transform"]
        BoxCol["«Component»<br/>BoxCollider (Trigger)"]
        DC["«Script Component»<br/>DoorChoice"]
    end

    subgraph Manager_GameObject ["⚙️ GameObject: Game System"]
        direction TB
        LM["«Script Component»<br/>LevelManager"]
        AM["«Script Component»<br/>AnomalyManager"]
        JC["«Script Component»<br/>JumpscareController"]
    end
    
    subgraph UI_GameObject ["🖼️ GameObject: Canvas UI"]
        direction TB
        LevelText["«Component»<br/>TextMeshProUGUI"]
        HUD["«Script Component»<br/>LevelDisplay"]
    end

    %% Mối quan hệ tương tác giữa các Components (Message / Events)
    TouchCam -.->|"Gửi input góc nhìn"| FPC
    Joy -.->|"Gửi input di chuyển"| FPC
    
    Col ==>|"Va chạm vật lý"| BoxCol
    BoxCol -->|"OnTriggerEnter"| DC
    
    DC -->|"Gọi Hàm CheckChoice"| LM
    LM -->|"Lấy dữ liệu trạng thái"| AM
    AM -->|"Trigger"| JC
    
    LM -->|"Update UI Event"| HUD
    HUD -->|"Thay đổi nội dung"| LevelText

    %% Styling
    classDef go fill:#f8f9fa,stroke:#ced4da,stroke-width:2px,color:#212529;
    classDef sys fill:#e9ecef,stroke:#adb5bd,stroke-width:2px,stroke-dasharray: 5 5;
    classDef comp fill:#ffffff,stroke:#495057,stroke-width:1px,color:#212529;
    classDef script fill:#e3f2fd,stroke:#0d6efd,stroke-width:1px,color:#084298;

    class Player_GameObject,Door_GameObject,Manager_GameObject,UI_GameObject,Input_GameObject go;
    class Transform_P,RB,Col,Audio,Transform_D,BoxCol,LevelText comp;
    class FPC,TouchCam,Joy,DC,LM,AM,JC,HUD script;
```

---

## 8. Sơ đồ Quy trình tạo PBR Texture (Materialize Workflow)
Quy trình (Workflow) sử dụng phần mềm **Bounding Box Materialize** để tạo ra một bộ Texture PBR (Physically Based Rendering) hoàn chỉnh từ một bức ảnh 2D cơ bản (Albedo/Diffuse). Quy trình này rất quan trọng để đưa vật liệu chân thực vào Unity.

```mermaid
flowchart TD
    Start([Bắt đầu]) --> Input["🖼️ 1. Nhập ảnh gốc<br/>(Diffuse / Base Color Map)"]
    
    subgraph PBR_Generation ["⚙️ Quá trình tạo Maps (Materialize)"]
        direction TB
        Input --> Height["⛰️ 2. Tạo Height Map<br/>(Dựa trên độ sáng tối để tạo độ sâu)"]
        
        Height --> Normal["🌊 3. Tạo Normal Map<br/>(Trích xuất chi tiết gồ ghề từ Height)"]
        Height --> AO["🌑 4. Tạo Ambient Occlusion (AO)<br/>(Đổ bóng các khe nứt, góc khuất)"]
        
        Input --> Metallic["🔗 5. Tạo Metallic Map<br/>(Xác định vùng nào là kim loại)"]
        Input --> Smoothness["✨ 6. Tạo Smoothness/Roughness<br/>(Xác định độ nhám, độ phản xạ)"]
        
        Normal --> Preview
        AO --> Preview
        Metallic --> Preview
        Smoothness --> Preview
    end
    
    Preview{"🔍 7. Xem trước 3D<br/>(Điều chỉnh ánh sáng)"}
    
    Preview -->|"Chưa ưng ý"| PBR_Generation
    Preview -->|"Đã đạt yêu cầu"| Export["💾 8. Export Project & Textures<br/>(Lưu toàn bộ ảnh Maps)"]
    
    Export --> Unity["🕹️ Import vào Unity<br/>(Kéo thả vào Standard/URP Lit Shader)"]
    Unity --> End([Hoàn thành])
    
    %% Styling
    classDef startend fill:#d1e7dd,stroke:#198754,stroke-width:2px;
    classDef input fill:#f8d7da,stroke:#dc3545,stroke-width:2px;
    classDef map fill:#e3f2fd,stroke:#0d6efd,stroke-width:2px,color:#000;
    classDef action fill:#fff3cd,stroke:#ffc107,stroke-width:2px;
    classDef unity fill:#d8b4e2,stroke:#6f42c1,stroke-width:2px;

    class Start,End startend;
    class Input input;
    class Height,Normal,AO,Metallic,Smoothness map;
    class Preview,Export action;
    class Unity unity;
```

---

## 9. Sơ đồ Trạng thái Hoạt ảnh (Animation State Machine / Animator Controller)
Sơ đồ này mô tả hệ thống Animator Controller trong Unity dùng để điều khiển hoạt ảnh (Animation) của nhân vật hoặc các thực thể sống trong game. Sơ đồ này thể hiện sự chuyển đổi giữa các trạng thái cơ bản trên mặt đất như đứng yên, đi bộ, chạy và các trạng thái đặc biệt như bị hù dọa, dựa trên các tham số (Parameters) truyền vào từ script `FirstPersonController`.

```mermaid
stateDiagram-v2
    [*] --> Locomotion : Bắt đầu Game

    %% Trạng thái kết hợp di chuyển trên mặt đất (Blend Tree hoặc States)
    state Locomotion {
        direction LR
        [*] --> Idle
        Idle --> Walk : Speed > 0.1
        Walk --> Idle : Speed < 0.1
        Walk --> Sprint : isSprinting == true
        Sprint --> Walk : isSprinting == false
    }
    
    %% Xử lý Jumpscare
    Locomotion --> Jumpscare : Trigger "Jumpscare"
    Jumpscare --> Locomotion : Kết thúc hoạt ảnh hù dọa\n(Has Exit Time)
```

---

## 10. Sơ đồ Luồng Giao diện Menu (UI Flow Diagram)
Sơ đồ này mô tả sự điều hướng của người chơi qua các giao diện người dùng (UI) khác nhau trong game, từ màn hình chính (Main Menu), màn hình cài đặt (Settings), đến giao diện khi đang chơi (In-Game HUD) và màn hình tạm dừng (Pause Menu).

```mermaid
%%{init: {'theme': 'neutral', 'themeVariables': { 'fontSize': '14px', 'fontFamily': 'Arial' }}}%%
flowchart TD
    Launch([Khởi chạy]) --> MainMenu
    
    subgraph SG_MainMenu ["🏠 Giao diện Main Menu"]
        direction TB
        MainMenu[Màn hình chính]
        BtnPlay(Nút: Bắt đầu)
        BtnContinue(Nút: Tiếp tục)
        BtnSettingsMain(Nút: Cài đặt)
        BtnQuit(Nút: Thoát)
        
        MainMenu --> BtnPlay
        MainMenu --> BtnContinue
        MainMenu --> BtnSettingsMain
        MainMenu --> BtnQuit
    end
    
    subgraph SG_Settings ["⚙️ Bảng Thiết lập (Màn hình cấu hình)"]
        direction TB
        SettingsPanel[Màn hình cấu hình]
        AudioSet[Âm lượng]
        GraphicSet[Cài đặt Đồ họa]
        BtnBack(Nút: Quay lại)
        
        SettingsPanel --- AudioSet & GraphicSet
        SettingsPanel --> BtnBack
    end
    
    subgraph SG_InGame ["🎮 Giao diện Trong Game (In-Game HUD)"]
        direction TB
        HUD[HUD: Cấp độ hiện tại]
        ItemShortcut[Phím tắt vật phẩm]
    end
    
    subgraph SG_PauseMenu ["⏸️ Giao diện Tạm dừng (Pause Menu)"]
        direction TB
        PauseMenu[Màn hình Pause]
        BtnResume(Nút: Tiếp tục)
        BtnSettingsPause(Nút: Cài đặt)
        BtnHome(Nút: Về Menu)
        
        PauseMenu --> BtnResume
        PauseMenu --> BtnSettingsPause
        PauseMenu --> BtnHome
    end

    subgraph SG_Ending ["🏆 Hệ thống Kết thúc & Sinh tồn"]
        direction LR
        GoodEnding[Màn hình Good Ending]
        GameOver[Màn hình Game Over]
    end

    Exit([Thoát])

    %% Điều hướng giữa các UI
    BtnPlay ==>|"Tải màn mới"| HUD
    BtnContinue ==>|"Đọc PlayerPrefs -> Tải"| HUD
    BtnSettingsMain -->|"Mở Panel"| SettingsPanel
    BtnQuit -->|"Thoát game"| Exit
    
    BtnBack -->|"Đóng Panel"| MainMenu
    BtnBack -.->|"Đóng Panel"| PauseMenu
    
    HUD -->|"Bấm ESC / Pause\n(Dừng Time)"| PauseMenu
    BtnResume ==>|"Đóng Pause\n(Chạy Time)"| HUD
    BtnSettingsPause -->|"Mở Panel"| SettingsPanel
    BtnHome -->|"Quay về"| MainMenu

    HUD -->|"Vượt Room 5 thành công"| GoodEnding
    HUD -->|"Chết / Jumpscare"| GameOver
    GameOver -->|"Bấm Chơi lại"| HUD
    GameOver -->|"Quay về"| MainMenu

    %% Styling chuyên nghiệp
    classDef menu fill:#e3f2fd,stroke:#0d6efd,stroke-width:2px,color:#000;
    classDef gameplay fill:#d1e7dd,stroke:#198754,stroke-width:2px,color:#000;
    classDef pause fill:#fff3cd,stroke:#ffc107,stroke-width:2px,color:#000;
    classDef danger fill:#f8d7da,stroke:#dc3545,stroke-width:2px,color:#000;
    classDef btn fill:#ffffff,stroke:#495057,stroke-width:1px,color:#212529;

    class MainMenu,SettingsPanel menu;
    class HUD,ItemShortcut gameplay;
    class PauseMenu pause;
    class GameOver,GoodEnding danger;
    class BtnPlay,BtnContinue,BtnSettingsMain,BtnQuit,BtnBack,BtnResume,BtnSettingsPause,BtnHome btn;
```

---

## 11. Sơ đồ Hoạt động Tạm dừng Game (Pause Menu Logic Diagram)
Sơ đồ này đi sâu vào logic lập trình đằng sau việc Tạm dừng (Pause) và Tiếp tục (Resume) game, đặc biệt quan trọng trong các game góc nhìn thứ nhất (FPS) khi cần xử lý thời gian (TimeScale), khóa/mở khóa con trỏ chuột (Cursor Lock) và vô hiệu hóa các Input của người chơi để tránh lỗi.

```mermaid
stateDiagram-v2
    direction TB
    [*] --> InGame : Đang chơi game
    
    InGame --> PausedState : Bấm ESC / Nút Pause
    
    %% Trạng thái khi bắt đầu Pause
    state PausedState {
        direction TB
        [*] --> FreezeTime : Time.timeScale = 0
        FreezeTime --> ShowUI : Bật PausePanel
        ShowUI --> DisablePlayer : Tắt FPC
        DisablePlayer --> UnlockCursor : Mở khóa chuột
    }
    
    state WaitInput <<choice>>
    PausedState --> WaitInput : Chờ thao tác UI
    
    WaitInput --> ResumingState : Bấm "Tiếp tục" hoặc ESC
    WaitInput --> SettingsMenu : Bấm "Cài đặt"
    WaitInput --> End : Bấm "Thoát" (Về Main)
    
    SettingsMenu --> WaitInput : Đóng Cài đặt
    
    %% Trạng thái khi Resume
    state ResumingState {
        direction TB
        [*] --> HideUI : Tắt PausePanel
        HideUI --> EnablePlayer : Bật lại FPC
        EnablePlayer --> LockCursor : Khóa chuột
        LockCursor --> NormalTime : Time.timeScale = 1
    }
    
    ResumingState --> InGame : Tiếp tục chơi
```
