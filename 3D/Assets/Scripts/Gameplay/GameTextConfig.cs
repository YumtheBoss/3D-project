using System.Collections.Generic;
using UnityEngine;

public static class GameTextConfig
{
    // ==========================================
    // ── NỘI DUNG SỔ & GHI CHÚ (Notes / Pages) ──
    // ==========================================

    // Quyển sổ nhật ký (Room 0) - Gồm 3 trang có sẵn từ đầu
    public static string[] GetBookPages()
    {
        return new string[]
        {
            // Trang 1
            "<size=115%><b>— Nhật ký của Kẻ vô danh —</b></size>\n\n" +
            "Tôi không biết mình đã bị giam ở đây bao lâu rồi. Một ngày? Một tuần? Hay cả đời?\n\n" +
            "Không có ánh mặt trời, không có tiếng tích tắc của đồng hồ...\n" +
            "Chỉ có tiếng thở dốc của chính tôi và cánh cửa chết tiệt đó. Nó dẫn tôi đi vòng quanh, đưa tôi về lại chính căn phòng này. Tôi sắp phát điên rồi...\n\n" +
            "<color=#AAAAAA><i>— Những trang tiếp theo bị giật rách bạo lực —</i></color>",

            // Trang 2
            "<size=115%><b>— Lần thứ 17 —</b></size>\n\n" +
            "Nó đang đùa giỡn với tôi. Tôi chắc chắn đây là lần thứ mười bảy tôi bước qua cánh cửa gỗ đó.\n\n" +
            "Nhưng hãy nhìn xem... chiếc ghế gỗ ở góc phòng. Nó đã xoay hướng.\n" +
            "Nó đang hướng thẳng về phía tôi. Có ai đó... hoặc thứ gì đó đã dịch chuyển nó.\n\n" +
            "<mark=#FFFF00AA><b>→ Chú ý: Không gian xung quanh sẽ biến dạng khi ta đi qua cửa. Hãy quan sát thật kỹ từng chi tiết nhỏ nhất.</b></mark>\n\n" +
            "<color=#AAAAAA><i>— Vết mực loang lổ như máu khô —</i></color>",

            // Trang 3
            "<size=115%><b>— Quy luật Vòng lặp —</b></size>\n\n" +
            "Tôi đã hiểu ra quy luật của cái lồng giam này rồi.\n\n" +
            "Khi phát hiện <b>SỰ BẤT THƯỜNG</b> (một đồ vật di chuyển, biến mất hay một hiện tượng kỳ lạ) → lập tức <b>QUAY LẠI</b> cánh cửa phía sau.\n" +
            "Khi <b>MỌI THỨ BÌNH THƯỜNG</b> (không có gì thay đổi) → <b>TIẾP TỤC</b> đi về phía trước.\n\n" +
            "<color=#CC0000><i>Cảnh giác! Nó luôn theo dõi bạn. Nó rất giỏi ngụy trang dưới vẻ ngoài bình thường để lừa bạn đi sâu hơn vào cạm bẫy.</i></color>\n\n" +
            "<color=#AAAAAA><size=75%>— Những trang còn lại chỉ là đống giấy nát vụn —</size></color>"
        };
    }

    // Mảnh giấy xé rách (Room 1) - Đã lược bỏ hướng dẫn chơi game
    public const string TORN_PAGE_ROOM1 =
        "<size=115%><b>— Mảnh giấy hoen ố —</b></size>\n\n" +
        "Nếu ngươi đang đọc những dòng này, thì ngươi cũng đang mắc kẹt như ta. Cánh cửa trước mặt ngươi không dẫn đến tự do đâu, đó là cổ họng của ác mộng.\n\n" +
        "Thứ đứng sau nơi này đang dệt nên một không gian giả lập để giam giữ linh hồn ngươi. Nhưng quyền năng của nó có hạn, nó không thể tái tạo hoàn hảo mọi thứ.\n\n" +
        "<color=#CC0000><i>Chạy đi! Đừng đứng yên... Nó ngửi thấy nỗi sợ của ngươi rồi.</i></color>";

    // Tờ giấy Kinh Thánh (Room 3)
    public const string BIBLE_NOTE_ROOM3 =
        "<size=85%>" +
        "<color=#FFCC88><size=110%><b>✝  Kinh Thánh  ✝</b></size></color>\n\n" +
        "<i>\"Sự sáng chiếu trong tối tăm, và tối tăm <b>không tiếp nhận</b> sự sáng.\"</i> <color=#777777><size=80%>— Giăng 1:5 —</size></color>\n\n" +
        "<i>\"Đức Giê-hô-va là <b>sự sáng</b> và sự cứu rỗi của tôi; tôi sẽ sợ ai?\"</i> <color=#777777><size=80%>— Thi Thiên 27:1 —</size></color>\n\n" +
        "<i>\"Hãy mặc lấy mọi khí giới của Đức Chúa Trời, để được đứng vững mà địch cùng mưu kế của ma quỷ.\"</i> <color=#777777><size=80%>— Ê-phê-sô 6:11 —</size></color>\n\n" +
        "<mark=#1A1A0080>" +
        "<color=#DDCC88><size=95%><b>[ Nét chữ cào rách giấy - nguệch ngoạc ]</b></size></color>\n" +
        "<color=#CCBB77><i>" +
        "Bọn chúng... bọn chúng sợ ánh sáng! Ánh sáng là lá chắn, là vũ khí duy nhất thiêu rụi tà ác.\n" +
        "Nếu nghe thấy tiếng xột xoạt trong bóng tối, hãy chiếu nguồn sáng vào chúng cho đến khi chúng rút lui.\n" +
        "<b>Giữ vững nguồn sáng của ngươi. Đừng để nỗi sợ lấn át.</b>" +
        "</i></color>" +
        "</mark>" +
        "</size>";

    // Tờ giấy Ma nơ canh (Room 4)
    public const string MANNEQUIN_NOTE_ROOM4 =
        "<size=85%>" +
        "<color=#FF4444><size=115%><b>— Huyết thư của Kẻ gục ngã —</b></size></color>\n\n" +
        "<i>Mười hai người... Mười hai linh hồn đã tan biến vào hư vô. Không một ai bước qua được cánh cửa cuối cùng.\n\n" +
        "Con gấu bông này mang hào quang thánh khiết. Đó là lá chắn linh hồn duy nhất cứu mạng ngươi trước lưỡi hái của quỷ dữ.\n\n" +
        "Ở phòng tiếp theo, hãy dùng ánh sáng của Gấu bông [Phím G] sạc đầy cho 3 đàn tế phong ấn tà ác để mở lối thoát hiểm.\n\n" +
        "<mark=#3A000080><color=#FF9999>" +
        "<b>CẢNH BÁO CHẾT CHÓC:</b> " +
        "Khi phong ấn bị thanh tẩy, tà khí sẽ gào thét báo động cho tất cả ác quỷ lao tới xé xác ngươi! " +
        "Hãy ôm chặt gấu bông, giữ vững đức tin và thanh tẩy toàn bộ 3 đàn tế. Nếu không, ngươi sẽ là linh hồn thứ mười ba!</color></mark></i>" +
        "</size>";


    // ==========================================
    // ── THOẠI KHÓA CỬA AN TOÀN (Safety Locks) ──
    // ==========================================

    public static string GetSafetyHint(RoomManager.RoomState room)
    {
        switch (room)
        {
            case RoomManager.RoomState.Room0:
                return "Cánh cửa này bị kẹt cứng... Quyển sổ cũ nát trên bàn kia dường như có thứ gì đó thu hút mình. Mình phải mở nó ra đọc trước.";
            case RoomManager.RoomState.Room1:
                return "Một lực cản vô hình chặn cánh cửa lại! Có thứ gì đó đang phát sáng trên kệ tủ... Mảnh giấy rách kia, mình phải nhặt nó.";
            case RoomManager.RoomState.Room3:
                return "Hành lang đằng sau cánh cửa tối tăm như hũ nút... đi tiếp lúc này là tự sát. Mình phải tìm thấy nguồn sáng hoặc vật chỉ đường nào đó ở quanh đây.";
            case RoomManager.RoomState.Room4:
                return "Không thể mở cửa! Một nỗi sợ hãi tột cùng bóp nghẹt tim tôi khi nhìn ra hành lang... Tôi cần thứ gì đó để bấu víu tinh thần. Con gấu bông phát sáng trong cũi sắt kia... tôi phải mang nó theo.";
            default:
                return "";
        }
    }

    // Khi người chơi cố quay lại cửa cũ tại Room 3 sau khi nhặt giấy
    public const string SAFETY_LOCK_ROOM3_BACK =
        "Cánh cửa đã bị khóa sầm lại! Nó bị chặn cứng từ phía bên kia rồi... Mình không thể quay lại. Lối thoát duy nhất là đi tiếp về phía trước.";


    // ==========================================
    // ── ĐỘC THOẠI NỘI TÂM (Inner Monologues) ──
    // ==========================================

    public static List<InnerMonologue.MonologueLine> GetMonologueLines(string key)
    {
        var lines = new List<InnerMonologue.MonologueLine>();

        switch (key)
        {
            case "Room0_WakeUp":
                lines.Add(new InnerMonologue.MonologueLine { text = "...Mùi ẩm mốc xộc vào mũi... Lạnh quá. Đầu tôi... như sắp vỡ tung.", autoAdvanceDelay = 3.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "...Tôi đang ở đâu thế này? Đây không phải là nhà của tôi...", autoAdvanceDelay = 4.0f });
                lines.Add(new InnerMonologue.MonologueLine { text = "...Căn phòng này... Hy vọng khi đi qua cửa sẽ không kinh khủng như những gì ghi ở đây...", autoAdvanceDelay = 4.5f });
                break;

            case "Room1_Entry":
                lines.Add(new InnerMonologue.MonologueLine { text = "...Cái gì thế này? Tôi vừa đi qua cánh cửa đó mà? Tại sao... tại sao tôi vẫn ở trong căn phòng này? Tiếng bước chân của tôi... hình như có tiếng vọng khác đuổi theo...", autoAdvanceDelay = 5.0f });
                break;

            case "Room2_Entry":
                lines.Add(new InnerMonologue.MonologueLine { text = "Lại là căn phòng này... Nhưng cảm giác này lạnh lẽo hơn nhiều. Nếu những gì ghi chép lại là đúng... hẳn phải có thứ gì đó đã biến đổi. Mình phải căng mắt ra tìm kiếm...", autoAdvanceDelay = 5.5f });
                break;

            case "Room3_Entry":
                lines.Add(new InnerMonologue.MonologueLine { text = "Hành lang bệnh viện ư? Mùi thuốc sát trùng trộn lẫn với mùi xác thối... Kinh tởm quá. Tại sao ác mộng lại đưa tôi đến nơi này? Mình phải đi tìm mẩu giấy tiếp theo và biến khỏi đây ngay lập tức!", autoAdvanceDelay = 6.0f });
                break;

            case "Room3_PostNote":
                // Độc thoại sau khi nhặt giấy Kinh Thánh (Room 3) - Căn phòng tối sầm và cửa cũ bị khóa
                lines.Add(new InnerMonologue.MonologueLine { text = "Đèn... đèn tắt hết rồi! Tối đen như mực... Tôi không nhìn thấy gì cả!", autoAdvanceDelay = 3.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Cánh cửa cũ vừa đóng sầm lại... Nó bị khóa từ bên ngoài rồi! Có thứ gì đó đang ở ngay đây...", autoAdvanceDelay = 4.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Mình không thể quay lại được nữa... Chỉ có thể đi tiếp theo hướng cánh cửa có ánh sáng đỏ đằng kia thôi!", autoAdvanceDelay = 5.0f });
                break;

            case "Room4_Entry":
                lines.Add(new InnerMonologue.MonologueLine { text = "Hành lang này... tà khí dày đặc quá. Không khí đặc quánh lại làm tôi khó thở...", autoAdvanceDelay = 4.0f });
                break;

            case "Room4_BearInteract":
                lines.Add(new InnerMonologue.MonologueLine { text = "...Một con gấu bông cũ kỹ nằm trong cũi sắt? Tại sao nó lại phát ra luồng ấm áp kỳ lạ này... Ôi trời, nó vừa cười sao? Cảm giác... như có một linh hồn bảo vệ đang ẩn giấu bên trong.", autoAdvanceDelay = 5.0f });
                break;

            case "Room4_PostNote":
                lines.Add(new InnerMonologue.MonologueLine { text = "Mười hai người đã chết... Họ đều bị xé xác sao?", autoAdvanceDelay = 3.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Lối thoát duy nhất là cánh cửa cuối con đường này... Ôi Chúa ơi, tôi nghe thấy tiếng gầm rú của quỷ dữ!", autoAdvanceDelay = 4.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Phải chạy! Tuyệt đối ĐỪNG NHÌN LẠI PHÍA SAU!", autoAdvanceDelay = 4.0f });
                break;

            case "Room5_Entry":
                lines.Add(new InnerMonologue.MonologueLine { text = "Lối thoát hiểm... bị quấn chặt bởi tà khí đen ngòm!", autoAdvanceDelay = 3.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Ba cột năng lượng hắc ám kia chính là lõi phong ấn... Tôi có thể nghe thấy tiếng xích gông reo rắc.", autoAdvanceDelay = 4.2f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Mình phải dùng hào quang của Gấu bông [Phím G] đứng gần để thiêu rụi phong ấn!", autoAdvanceDelay = 4.5f });
                lines.Add(new InnerMonologue.MonologueLine { text = "Nhưng khi sạc đàn tế, tiếng tà khí bùng nổ sẽ đánh động toàn bộ quỷ dữ... Tôi phải sẵn sàng chạy trốn bất kỳ lúc nào!", autoAdvanceDelay = 4.5f });
                break;
        }

        return lines;
    }
}
