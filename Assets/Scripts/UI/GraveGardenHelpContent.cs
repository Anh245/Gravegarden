using System;

public static class GraveGardenHelpContent
{
    public sealed class Page
    {
        public readonly string Title, Body;
        public Page(string title,string body){Title=title;Body=body;}
    }
    public static int Count => 8;
    public static Page Get(int index)
    {
        switch(index)
        {
            case 0:return new Page("Mục tiêu của GraveGarden", @"Xây dựng khu vườn phòng thủ để ngăn quái vật phá hủy nhà chính.

Trong năm đợt đầu, bạn bảo vệ HeartCrypt trên hai tuyến đường. Khi hoàn thành đợt 5, hai khu mới mở ra: VioletCrypt và GoldenCrypt.

Sau mở rộng, hãy bảo vệ cả ba nhà cùng lúc. Chỉ cần một nhà đang hoạt động hết máu, trận đấu kết thúc.

Ba khu dùng chung một ví tiền. Hãy dành tiền cho cả phòng thủ và công trình sản xuất.");
            case 1:return new Page("Di chuyển góc nhìn và thao tác", @"Giữ chuột phải trên nền rồi kéo để di chuyển góc nhìn. Lăn bánh xe chuột để phóng to hoặc thu nhỏ.

Bấm nút Xây dựng hoặc phím B để bật các ô xây. Bấm Xong hoặc B để thoát chế độ xây.

Phím Esc ưu tiên hủy di chuyển công trình hoặc thoát chế độ xây; khi phù hợp, Esc mở hoặc đóng bảng tạm dừng.

Trong hướng dẫn mở từ Pause, Esc đóng hướng dẫn trước. Bấm Tiếp tục trận để chơi tiếp.");
            case 2:return new Page("Mua và đặt công trình", @"Bật chế độ xây, chọn thẻ công trình trong cửa hàng, chọn một ô trống hợp lệ rồi bấm Xây.

Mỗi công trình chiếm nhiều ô nhỏ. Toàn bộ diện tích công trình phải nằm trên vùng được phép xây và không chồng công trình khác.

Thẻ chưa mở khóa hoặc không đủ tiền sẽ chưa mua được. Một số công trình chỉ mở từ các đợt sau.

Đường đi, khu chưa mở và ô bị chặn không dùng để xây. Hãy đặt tháp gần tuyến địch để tận dụng tầm bắn.");
            case 3:return new Page("Sản xuất và thu hoạch", @"Vườn Xương và các công trình sản xuất tích lũy tài nguyên vào kho riêng.

Bấm chuột trái lên công trình sản xuất để thu phần tiền có thể nhận vào ví. Khi kho đầy, hãy thu hoạch để tiếp tục tích lũy.

Bấm chuột phải lên công trình sản xuất để mở bảng nâng cấp. Nâng cấp có thể cải thiện tốc độ hoặc sức chứa theo dữ liệu công trình.

Sau khi quay lại từ bản lưu, thu nhập ngoại tuyến được đưa vào kho, tối đa theo giới hạn thời gian và sức chứa. Bạn vẫn cần thu hoạch để nhận vào ví.");
            case 4:return new Page("Nâng cấp, di chuyển và bán", @"Bấm chuột trái lên tháp hoặc công trình hỗ trợ để mở bảng thao tác. Với công trình sản xuất, dùng chuột phải.

Nâng cấp tiêu tốn tiền và cải thiện chỉ số theo từng cấp. Xem thông tin trước khi xác nhận.

Chọn Di chuyển, chọn vị trí mới hợp lệ rồi đặt. Esc hoặc Hủy giữ vị trí cũ. Di chuyển không mất tiền và giữ cấp công trình.

Bán công trình hoàn lại 65% tiền đã đầu tư, cộng phần tiền trong kho có thể thu. Kiểm tra số tiền hiển thị trước khi bán.");
            case 5:return new Page("Đợt tấn công và mở rộng ba khu", @"Đồng hồ chuẩn bị tự bắt đầu đợt khi về 0. Khu đầu có 20 giây; trước đợt 6 có 45 giây; từ đợt 7 có 30 giây.

Bấm Bắt đầu đợt sớm để nhận thưởng theo số giây còn lại × 4, lấy phần nguyên. Đồng hồ dừng trong hướng dẫn Mara và Pause. Quái vật xuất hiện theo nhịp, không phải tất cả cùng lúc.

Còn lại bao gồm địch đang trên sân và địch chưa xuất hiện. Đợt chỉ hoàn thành khi đã sinh hết và các địch bị hạ đã kết thúc hiệu ứng chết.

Hoàn thành đợt 5 mở hai khu mới, thưởng thêm 2.000 xu và hồi đầy máu ba nhà một lần. Hãy xây phòng thủ cho hai khu mới trước khi bắt đầu đợt 6.

Mỗi nhà có hai tuyến. Địch đi tuyến nào sẽ tấn công nhà tương ứng ở cuối tuyến đó.");
            case 6:return new Page("Lưu và tiếp tục trận", @"Bạn có thể lưu hoặc tải khi đang chuẩn bị giữa hai đợt, không còn địch sống. Không thể lưu giữa đợt tấn công.

Dùng Lưu hoặc Pause → Lưu và về menu. Từ menu chính, chọn Tiếp tục để khôi phục bản lưu gần nhất.

Bản lưu giữ ví tiền, tiến trình wave, ba khu, máu nhà và tiến trình công trình. Tải không tự hồi đầy máu hoặc thưởng mở khu lần nữa.

Về menu không lưu sẽ bỏ các thay đổi chưa được lưu. Tải lại sẽ thay thế trạng thái hiện tại bằng bản lưu, không cộng dồn hai bộ công trình.");
            case 7:return new Page("Chơi tiếp sau chương 1", @"Từ đợt 6, trò chơi tiếp tục theo chế độ vô tận. Số địch và máu tăng dần; khoảng sinh địch giảm tới mức giới hạn.

Các đợt 10, 15, 20… có sáu Ossuary Giant, chia trên sáu tuyến. Chuẩn bị phòng thủ ở cả ba khu trước các đợt boss.

Dùng các loại tháp và công trình hỗ trợ phù hợp với tình huống. Nâng cấp sản xuất để duy trì nguồn tiền, đồng thời kiểm tra thanh máu ba nhà.

Bạn có thể đổi tốc độ 1× hoặc 2× bằng các nút trên màn hình. Mở Pause để tạm dừng, điều chỉnh âm thanh hoặc xem lại hướng dẫn.");
            default:throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
