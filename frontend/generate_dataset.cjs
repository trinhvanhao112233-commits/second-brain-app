const fs = require('fs');
const path = require('path');

const categoriesExpense = [
  { cat: 'Ăn uống', items: ['ăn sáng phở bò', 'bún chả', 'cơm tấm sườn bì', 'bánh mì pate', 'hủ tiếu Nam Vang', 'cơm rang dưa bò', 'trà sữa trân châu đường đen', 'cà phê muối', 'mua bánh ngọt', 'ăn lẩu cùng bạn bè', 'ăn trưa văn phòng', 'ăn tối cùng gia đình', 'ăn ốc luộc', 'uống sinh tố bơ', 'mua trà tắc'] },
  { cat: 'Đi lại', items: ['đổ xăng xe máy', 'đi Grab bike', 'bắt Grab car', 'nạp thẻ xe buýt', 'vé tàu điện Cát Linh', 'gửi xe tháng', 'thay nhớt xe máy', 'vá săm xe', 'tiền phí cầu đường BOT', 'rửa xe bọt tuyết', 'đi taxi Mai Linh'] },
  { cat: 'Mua sắm', items: ['mua áo thun trên Shopee', 'mua tai nghe Bluetooth', 'mua sạc dự phòng', 'đi siêu thị WinMart', 'mua đôi giày sneaker mới', 'mua sách lập trình Clean Code', 'mua đồ decor bàn học', 'mua chuột không dây Logitech', 'mua bàn phím cơ'] },
  { cat: 'Hóa đơn', items: ['tiền điện tháng này', 'tiền nước sinh hoạt', 'cước internet VNPT', 'tiền phòng trọ', 'phí quản lý chung cư', 'tiền rác', 'nạp tiền điện thoại Viettel', 'gia hạn gói Netflix', 'thanh toán Spotify Premium'] },
  { cat: 'Giải trí', items: ['mua vé xem phim CGV', 'đi hát karaoke cùng lớp', 'mua game trên Steam', 'đi đánh bida với hội bạn', 'đi cà phê acoustic cuối tuần', 'mua vé xem ca nhạc live concert'] },
  { cat: 'Y tế', items: ['mua thuốc cảm sốt ở Long Châu', 'đi khám răng lấy cao răng', 'mua khẩu trang y tế', 'khám tổng quát ở bệnh viện', 'mua vitamin C và kẽm', 'mua chai thuốc nhỏ mắt', 'mua hộp gạc y tế'] }
];

const amounts = [
  { text: '20k', val: 20000 },
  { text: '30k', val: 30000 },
  { text: '45k', val: 45000 },
  { text: '65k', val: 65000 },
  { text: '80k', val: 80000 },
  { text: '120k', val: 120000 },
  { text: '250 ngàn', val: 250000 },
  { text: '350k', val: 350000 },
  { text: '500k', val: 500000 },
  { text: '1.2 củ', val: 1200000 },
  { text: '2.5 triệu', val: 2500000 },
  { text: '50.000đ', val: 50000 },
  { text: '100.000đ', val: 100000 }
];

const wallets = [null, 'ví chính', 'ví phụ', 'ví tiết kiệm', 'ví tiền mặt'];
const prefixesExpense = ['Đã ', 'Vừa ', 'Hôm nay ', 'Sáng nay ', 'Trưa nay ', 'Chiều nay ', 'Mới '];

const dataset = [];
let idCount = 1;

// 1. Sinh các câu Chi tiêu (Expense) ~ 150 mẫu
for (const c of categoriesExpense) {
  for (const item of c.items) {
    for (let i = 0; i < 2; i++) {
      const amtObj = amounts[Math.floor(Math.random() * amounts.length)];
      const prefix = prefixesExpense[Math.floor(Math.random() * prefixesExpense.length)];
      const wallet = wallets[Math.floor(Math.random() * wallets.length)];
      
      let input = prefix + item + ' hết ' + amtObj.text;
      if (wallet) input += ' bằng ' + wallet;

      dataset.push({
        id: idCount++,
        instruction: 'Phân tích yêu cầu ghi chép tài chính của người dùng',
        input: input,
        output: {
          actionType: 'add_expense',
          amount: amtObj.val,
          category: c.cat,
          note: item.charAt(0).toUpperCase() + item.slice(1),
          walletName: wallet,
          title: null,
          startTime: null,
          endTime: null,
          priority: null,
          replyMessage: 'Đã ghi nhận khoản chi ' + amtObj.val.toLocaleString('vi-VN') + 'đ cho ' + item + ' vào ví.'
        }
      });
    }
  }
}

// 2. Sinh các câu Nạp tiền / Thu nhập ~ 50 mẫu
const incomeSources = ['lương tháng công ty', 'tiền thưởng nóng dự án', 'bố mẹ gửi tiền sinh hoạt', 'tiền làm thêm freelance', 'được bạn trả nợ cũ', 'nạp tiền vào tài khoản', 'hoa hồng bán hàng', 'lãi tiết kiệm ngân hàng'];
const incomeAmounts = [
  { text: '500k', val: 500000 },
  { text: '1 triệu', val: 1000000 },
  { text: '2.5 triệu', val: 2500000 },
  { text: '5 củ', val: 5000000 },
  { text: '15 triệu', val: 15000000 },
  { text: '25 củ', val: 25000000 }
];

for (const inc of incomeSources) {
  for (const amt of incomeAmounts) {
    const wallet = wallets[Math.floor(Math.random() * wallets.length)];
    let input = 'Vừa nhận ' + inc + ' ' + amt.text;
    if (wallet) input += ' vào ' + wallet;

    dataset.push({
      id: idCount++,
      instruction: 'Phân tích yêu cầu ghi chép thu nhập của người dùng',
      input: input,
      output: {
        actionType: 'add_income',
        amount: amt.val,
        category: 'Nạp tiền',
        note: inc.charAt(0).toUpperCase() + inc.slice(1),
        walletName: wallet,
        title: null,
        startTime: null,
        endTime: null,
        priority: null,
        replyMessage: 'Đã cộng ' + amt.val.toLocaleString('vi-VN') + 'đ (' + inc + ') vào ví.'
      }
    });
  }
}

// 3. Sinh các câu Lịch trình (Calendar Events) ~ 120 mẫu
const eventSubjects = [
  { title: 'Học môn thiết kế và phát triển', cat: 'Học tập' },
  { title: 'Học môn lập trình web nâng cao', cat: 'Học tập' },
  { title: 'Học môn cơ sở dữ liệu phân tán', cat: 'Học tập' },
  { title: 'Học môn trí tuệ nhân tạo', cat: 'Học tập' },
  { title: 'Họp sprint dự án với team tech', cat: 'Work' },
  { title: 'Họp báo cáo tiến độ tuần', cat: 'Work' },
  { title: 'Gặp khách hàng trao đổi hợp đồng', cat: 'Work' },
  { title: 'Phỏng vấn ứng viên thực tập', cat: 'Work' },
  { title: 'Đi đá bóng sân cỏ nhân tạo', cat: 'Personal' },
  { title: 'Đi xem phim rạp cùng bạn', cat: 'Personal' },
  { title: 'Đi khám tổng quát định kỳ', cat: 'Health' },
  { title: 'Hẹn đi khám mắt', cat: 'Health' },
  { title: 'Bảo vệ đồ án tốt nghiệp', cat: 'Important' },
  { title: 'Thi vấn đáp môn hệ điều hành', cat: 'Important' }
];

const timeSlots = [
  { text: 'bắt đầu từ 7 giờ sáng kết thúc 9h sáng', sH: '07:00:00', eH: '09:00:00' },
  { text: 'từ 8h đến 10h', sH: '08:00:00', eH: '10:00:00' },
  { text: 'lúc 9h đến 11h30 trưa', sH: '09:00:00', eH: '11:30:00' },
  { text: 'chiều từ 14h đến 16h', sH: '14:00:00', eH: '16:00:00' },
  { text: 'chiều từ 15h30 kết thúc lúc 17h', sH: '15:30:00', eH: '17:00:00' },
  { text: 'tối từ 19h đến 21h', sH: '19:00:00', eH: '21:00:00' },
  { text: 'tối từ 19h30 kết thúc lúc 21h30', sH: '19:30:00', eH: '21:30:00' }
];

const days = [
  { text: 'sáng mai', date: '2026-10-03' },
  { text: 'chiều mai', date: '2026-10-03' },
  { text: 'tối nay', date: '2026-10-02' },
  { text: 'ngày kia', date: '2026-10-04' },
  { text: 'thứ 2 tuần sau', date: '2026-10-05' },
  { text: 'thứ 4 tuần sau', date: '2026-10-07' }
];

for (const es of eventSubjects) {
  for (const slot of timeSlots) {
    const day = days[Math.floor(Math.random() * days.length)];
    const input = 'Hãy điền cho tôi lịch ' + es.title.toLowerCase() + ' ' + day.text + ' ' + slot.text;
    
    dataset.push({
      id: idCount++,
      instruction: 'Phân tích yêu cầu tạo lịch hẹn trong hệ thống Second Brain',
      input: input,
      output: {
        actionType: 'add_event',
        amount: null,
        category: es.cat,
        note: null,
        walletName: null,
        title: es.title,
        description: es.title,
        startTime: day.date + 'T' + slot.sH,
        endTime: day.date + 'T' + slot.eH,
        priority: null,
        replyMessage: 'Đã lên lịch ' + es.title + ' từ ' + slot.sH.slice(0,5) + ' đến ' + slot.eH.slice(0,5) + '!'
      }
    });
  }
}

// 4. Sinh các câu Nhiệm vụ (Tasks) ~ 40 mẫu
const taskActions = [
  'Nộp slide báo cáo đồ án tốt nghiệp',
  'Mua quà sinh nhật cho bạn gái',
  'Dọn dẹp phòng ngủ và giặt chăn màn',
  'Đọc xong 2 chương sách chuyên ngành',
  'Sửa lỗi giao diện responsive mobile',
  'Gửi email xác nhận danh sách thực tập sinh',
  'Làm bài tập lớn môn kiến trúc phần mềm',
  'Chuẩn bị tài liệu thuyết trình thứ 2'
];

const taskPriorities = [
  { text: 'việc này rất gấp', p: 'High' },
  { text: 'ưu tiên cao cần làm ngay', p: 'High' },
  { text: 'khi nào rảnh thì làm', p: 'Low' },
  { text: 'hạn chót ngày mai', p: 'High' },
  { text: '', p: 'Medium' }
];

for (const act of taskActions) {
  for (const p of taskPriorities) {
    const input = ('Nhớ ' + act.toLowerCase() + ' ' + p.text).trim();
    dataset.push({
      id: idCount++,
      instruction: 'Phân tích yêu cầu ghi nhớ công việc (Task)',
      input: input,
      output: {
        actionType: 'add_task',
        amount: null,
        category: null,
        note: null,
        walletName: null,
        title: act,
        description: null,
        startTime: null,
        endTime: null,
        priority: p.p,
        replyMessage: 'Đã lưu công việc ' + act + ' với mức ưu tiên ' + p.p + '.'
      }
    });
  }
}

// 5. Sinh các câu Chat / Trợ giúp ~ 20 mẫu
const chatQuestions = [
  'Website này có những tính năng gì vậy bot?',
  'Hướng dẫn tôi cách dùng ví tiền',
  'Làm sao để thêm lịch học trên weekboard?',
  'Bạn có thể làm được những gì?',
  'Xin chào bot',
  'Chào bạn trợ lý AI'
];

for (const q of chatQuestions) {
  dataset.push({
    id: idCount++,
    instruction: 'Phản hồi câu hỏi hoặc chào hỏi của người dùng',
    input: q,
    output: {
      actionType: 'chat',
      amount: null,
      category: null,
      note: null,
      walletName: null,
      title: null,
      startTime: null,
      endTime: null,
      priority: null,
      replyMessage: 'Tôi là Trợ lý AI Second Brain. Tôi có thể tự động ghi chép chi tiêu, cập nhật số dư ví và sắp xếp lịch trình hằng ngày cho bạn!'
    }
  });
}

const outputPath = path.join(__dirname, '..', 'backend', 'Data', 'second_brain_ai_dataset.json');
fs.writeFileSync(outputPath, JSON.stringify(dataset, null, 2), 'utf8');
console.log('Successfully generated ' + dataset.length + ' dataset items!');
