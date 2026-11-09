// Quản lý Tài khoản (Đăng ký / Đăng nhập) & Dữ liệu cá nhân theo từng người dùng


const EXPENSE_CATEGORIES = [
  { id: 'food', name: 'Ăn uống', color: '#f59e0b' },
  { id: 'housing', name: 'Nhà ở & Điện nước', color: '#3b82f6' },
  { id: 'transport', name: 'Di chuyển', color: '#0ea5e9' },
  { id: 'shopping', name: 'Mua sắm', color: '#ec4899' },
  { id: 'debt_pay', name: 'Trả nợ', color: '#ef4444' },
  { id: 'entertainment', name: 'Giải trí', color: '#8b5cf6' },
  { id: 'education', name: 'Học tập', color: '#10b981' },
  { id: 'other_exp', name: 'Chi phí khác', color: '#64748b' }
];

const INCOME_CATEGORIES = [
  { id: 'salary', name: 'Lương chính', color: '#10b981' },
  { id: 'freelance', name: 'Dự án ngoài', color: '#3b82f6' },
  { id: 'bonus', name: 'Thưởng / Phụ cấp', color: '#f59e0b' },
  { id: 'debt_collect', name: 'Thu hồi nợ', color: '#0ea5e9' },
  { id: 'other_inc', name: 'Thu nhập khác', color: '#8b5cf6' }
];

function getRelativeDateStr(offsetDays = 0) {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function getRelativeDateTimeStr(offsetDays = 0, hour = 17, minute = 0) {
  const datePart = getRelativeDateStr(offsetDays);
  const hh = String(hour).padStart(2, '0');
  const min = String(minute).padStart(2, '0');
  return `${datePart}T${hh}:${min}`;
}

function createSampleState() {
  const today = getRelativeDateStr(0);

  return {
    theme: 'light',
    accentColor: '#6366f1',
    monthlyBudget: 12000000,
    schedules: [
      {
        id: 'sch-1',
        title: 'Họp triển khai công việc đầu ngày',
        date: today,
        startTime: '08:30',
        endTime: '10:00',
        type: 'meeting',
        location: 'Phòng họp A',
        note: 'Báo cáo tiến độ tuần'
      },
      {
        id: 'sch-2',
        title: 'Làm việc chuyên sâu (Xử lý dự án chính)',
        date: today,
        startTime: '13:30',
        endTime: '17:00',
        type: 'work',
        location: 'Văn phòng',
        note: 'Hoàn thiện phân hệ báo cáo'
      },
      {
        id: 'sch-3',
        title: 'Tập thể thao buổi chiều',
        date: today,
        startTime: '18:00',
        endTime: '19:15',
        type: 'personal',
        location: 'Phòng tập',
        note: ''
      },
      {
        id: 'sch-4',
        title: 'Gặp gỡ & trao đổi với khách hàng',
        date: getRelativeDateStr(1),
        startTime: '09:30',
        endTime: '11:00',
        type: 'meeting',
        location: 'Trực tuyến',
        note: 'Gửi tài liệu trước buổi họp'
      },
      {
        id: 'sch-5',
        title: 'Ca làm việc sáng',
        date: getRelativeDateStr(2),
        startTime: '08:00',
        endTime: '12:00',
        type: 'work',
        location: 'Văn phòng',
        note: ''
      },
      {
        id: 'sch-6',
        title: 'Tổng kết công việc tuần',
        date: getRelativeDateStr(3),
        startTime: '15:00',
        endTime: '17:00',
        type: 'work',
        location: 'Phòng họp B',
        note: ''
      },
      {
        id: 'sch-7',
        title: 'Kiểm tra tiến độ dự án đầu tuần',
        date: getRelativeDateStr(-1),
        startTime: '09:00',
        endTime: '11:30',
        type: 'work',
        location: 'Văn phòng',
        note: ''
      }
    ],
    deadlines: [
      {
        id: 'dl-1',
        title: 'Nộp báo cáo tổng kết tài chính & kế hoạch quý',
        category: 'Công việc',
        dueDate: getRelativeDateTimeStr(0, 20, 0),
        priority: 'urgent',
        progress: 75,
        status: 'in_progress',
        note: 'Kiểm tra lại số liệu bảng tổng hợp lần cuối'
      },
      {
        id: 'dl-2',
        title: 'Bàn giao bản cập nhật giao diện cho khách hàng',
        category: 'Dự án',
        dueDate: getRelativeDateTimeStr(2, 17, 0),
        priority: 'high',
        progress: 40,
        status: 'in_progress',
        note: 'Hoàn thiện phần hiển thị trên màn hình nhỏ'
      },
      {
        id: 'dl-3',
        title: 'Thanh toán hóa đơn định kỳ & sao kê tháng',
        category: 'Cá nhân',
        dueDate: getRelativeDateTimeStr(-1, 18, 0),
        priority: 'urgent',
        progress: 20,
        status: 'todo',
        note: 'Đã quá hạn 1 ngày, cần xử lý sớm'
      },
      {
        id: 'dl-4',
        title: 'Hoàn thành tài liệu chuyên môn phần 4',
        category: 'Học tập',
        dueDate: getRelativeDateTimeStr(5, 22, 0),
        priority: 'medium',
        progress: 60,
        status: 'in_progress',
        note: ''
      }
    ],
    transactions: [
      {
        id: 'tx-1',
        type: 'income',
        amount: 18500000,
        category: 'salary',
        title: 'Lương tháng cố định',
        date: getRelativeDateStr(-5),
        note: 'Chuyển khoản ngân hàng'
      },
      {
        id: 'tx-2',
        type: 'income',
        amount: 4500000,
        category: 'freelance',
        title: 'Thu nhập dự án thiết kế ngoài',
        date: getRelativeDateStr(-2),
        note: 'Thanh toán đợt cuối'
      },
      {
        id: 'tx-3',
        type: 'expense',
        amount: 3500000,
        category: 'housing',
        title: 'Tiền thuê nhà & Điện nước tháng này',
        date: getRelativeDateStr(-4),
        note: ''
      },
      {
        id: 'tx-4',
        type: 'expense',
        amount: 1250000,
        category: 'food',
        title: 'Mua thực phẩm & đồ ăn trong tuần',
        date: getRelativeDateStr(-3),
        note: ''
      },
      {
        id: 'tx-5',
        type: 'expense',
        amount: 450000,
        category: 'transport',
        title: 'Đổ xăng & Bảo dưỡng xe',
        date: getRelativeDateStr(-1),
        note: ''
      },
      {
        id: 'tx-6',
        type: 'expense',
        amount: 850000,
        category: 'shopping',
        title: 'Mua thiết bị phục vụ công việc',
        date: today,
        note: ''
      }
    ],
    debts: [
      {
        id: 'debt-1',
        direction: 'borrowed',
        person: 'Thẻ tín dụng VPBank',
        title: 'Dư nợ sao kê thẻ tháng này',
        totalAmount: 4500000,
        paidAmount: 1500000,
        interestRate: 28,
        dueDate: getRelativeDateStr(-1),
        priorityLevel: 'urgent',
        note: 'Trả ngay để tránh phí phạt quá hạn',
        history: [
          { date: getRelativeDateStr(-6), amount: 1500000, note: 'Thanh toán đợt 1' }
        ]
      },
      {
        id: 'debt-2',
        direction: 'borrowed',
        person: 'Anh Minh (Đồng nghiệp)',
        title: 'Mượn tiền đặt cọc nhà',
        totalAmount: 5000000,
        paidAmount: 2000000,
        interestRate: 0,
        dueDate: getRelativeDateStr(2),
        priorityLevel: 'high',
        note: 'Hẹn trả nốt 3 triệu trong tuần này',
        history: [
          { date: getRelativeDateStr(-15), amount: 2000000, note: 'Trả trước một phần' }
        ]
      },
      {
        id: 'debt-3',
        direction: 'borrowed',
        person: 'Trả góp thiết bị làm việc',
        title: 'Kỳ thanh toán 4/6',
        totalAmount: 6000000,
        paidAmount: 4800000,
        interestRate: 0,
        dueDate: getRelativeDateStr(6),
        priorityLevel: 'medium',
        note: 'Còn 1.200.000đ là tất toán dứt điểm',
        history: []
      },
      {
        id: 'debt-4',
        direction: 'lent',
        person: 'Hoàng Nam',
        title: 'Cho mượn xoay sở cá nhân',
        totalAmount: 2500000,
        paidAmount: 500000,
        interestRate: 0,
        dueDate: getRelativeDateStr(1),
        priorityLevel: 'high',
        note: 'Hẹn cuối tuần chuyển khoản hoàn trả',
        history: [
          { date: getRelativeDateStr(-5), amount: 500000, note: 'Đã trả trước 500k' }
        ]
      }
    ],
    notes: [
      {
        id: 'note-1',
        title: 'Nguyên tắc phân bổ thu nhập hàng tháng',
        content: '1. Dành 50% thu nhập cho các nhu cầu thiết yếu: Tiền nhà, điện nước, ăn uống, đi lại.\n2. Dành 30% thu nhập để trả dứt điểm các khoản nợ ngắn hạn theo thứ tự ưu tiên thời gian.\n3. Dành 20% còn lại đưa vào quỹ dự phòng cá nhân.',
        updatedAt: today
      },
      {
        id: 'note-2',
        title: 'Ghi chú nội dung cuộc họp dự án',
        content: '- Thống nhất lại bố cục giao diện tối giản, dễ nhìn.\n- Tập trung vào trải nghiệm xem lịch theo Ngày, Tuần, Tháng.\n- Kiểm tra kỹ tính năng tính toán ưu tiên trả nợ theo hạn chót.',
        updatedAt: today
      },
      {
        id: 'note-3',
        title: 'Danh sách việc cá nhân cần làm cuối tuần',
        content: '- Dọn dẹp lại bàn làm việc và sắp xếp tài liệu.\n- Sao lưu dữ liệu quan trọng.\n- Lên lịch làm việc chi tiết cho tuần tiếp theo.',
        updatedAt: getRelativeDateStr(-1)
      }
    ]
  };
}

// ============================================================================
// QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG (AUTH)
// ============================================================================
