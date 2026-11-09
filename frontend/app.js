// ============================================================================
// HỆ THỐNG QUẢN LÝ CÁ NHÂN — LOGIC CHÍNH
// ============================================================================

let currentUser = getCurrentUser();
let state = currentUser ? loadAppStateForUser(currentUser.username) : createSampleState();

// Trạng thái giao diện
let currentTab = 'dashboard';
let scheduleViewMode = 'week'; // 'day' | 'week' | 'month'
let scheduleFocusDate = new Date(); // Ngày mốc đang xem trong Lịch làm việc
let deadlineFilterStatus = 'all';
let financeTypeFilter = 'all';
let debtDirectionFilter = 'borrowed';
let activeNoteId = null;

// ============================================================================
// HÀM TIỆN ÍCH CHUNG
// ============================================================================

function saveCurrentState() {
  if (currentUser) {
    saveAppStateForUser(currentUser.username, state);
  }
}

function formatVND(amount) {
  const num = Number(amount) || 0;
  return num.toLocaleString('vi-VN') + ' ₫';
}

function toDateStr(dateObj) {
  const yyyy = dateObj.getFullYear();
  const mm = String(dateObj.getMonth() + 1).padStart(2, '0');
  const dd = String(dateObj.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function formatDateVN(dateStr) {
  if (!dateStr) return '';
  const part = dateStr.split('T')[0];
  const [y, m, d] = part.split('-');
  if (!y || !m || !d) return dateStr;
  return `${d}/${m}/${y}`;
}

function formatDateTimeVN(dtStr) {
  if (!dtStr) return '';
  const [datePart, timePart] = dtStr.split('T');
  return `${timePart ? timePart + ' - ' : ''}${formatDateVN(datePart)}`;
}

function getDaysRemaining(dateOrDateTimeStr) {
  if (!dateOrDateTimeStr) return 0;
  const now = new Date();
  const target = dateOrDateTimeStr.includes('T')
    ? new Date(dateOrDateTimeStr)
    : new Date(dateOrDateTimeStr + 'T23:59:59');
  return Math.ceil((target - now) / (1000 * 60 * 60 * 24));
}

function getRemainingTimeBadge(dtStr, status) {
  if (status === 'done') {
    return `<span class="badge badge-success">Đã hoàn thành</span>`;
  }
  const now = new Date();
  const target = dtStr.includes('T') ? new Date(dtStr) : new Date(dtStr + 'T23:59:59');
  const diffHours = Math.round((target - now) / (1000 * 60 * 60));

  if (diffHours < 0) {
    const overdueDays = Math.ceil(Math.abs(diffHours) / 24);
    return `<span class="badge badge-danger">Quá hạn ${overdueDays} ngày</span>`;
  }
  if (diffHours <= 24) {
    return `<span class="badge badge-danger">Còn ${diffHours} giờ</span>`;
  }
  const days = Math.ceil(diffHours / 24);
  if (days <= 3) {
    return `<span class="badge badge-warning">Còn ${days} ngày</span>`;
  }
  return `<span class="badge badge-info">Còn ${days} ngày</span>`;
}

function getPriorityBadge(priority) {
  switch (priority) {
    case 'urgent':
      return `<span class="badge badge-danger">Khẩn cấp</span>`;
    case 'high':
      return `<span class="badge badge-warning">Ưu tiên cao</span>`;
    case 'medium':
      return `<span class="badge badge-info">Trung bình</span>`;
    default:
      return `<span class="badge badge-neutral">Thấp</span>`;
  }
}

function getPriorityWeight(priority) {
  const map = { urgent: 4, high: 3, medium: 2, low: 1 };
  return map[priority] || 1;
}

function getCategoryMeta(catId) {
  const all = [...EXPENSE_CATEGORIES, ...INCOME_CATEGORIES];
  return (
    all.find((c) => c.id === catId) || {
      id: catId,
      name: 'Khác',
      color: '#64748b'
    }
  );
}

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function showToast(message, type = 'info') {
  const container = document.getElementById('toast-container');
  const toast = document.createElement('div');
  toast.className = 'toast';
  const colors = {
    success: '#10b981',
    danger: '#ef4444',
    warning: '#f59e0b',
    info: '#3b82f6'
  };
  toast.style.borderLeftColor = colors[type] || colors.info;
  toast.textContent = message;
  container.appendChild(toast);
  setTimeout(() => {
    toast.style.opacity = '0';
    setTimeout(() => toast.remove(), 220);
  }, 2800);
}

// ============================================================================
// ĐĂNG KÝ / ĐĂNG NHẬP / ĐĂNG XUẤT
// ============================================================================

function switchAuthMode(mode) {
  const isLogin = mode === 'login';
  document.getElementById('tab-btn-login').classList.toggle('active', isLogin);
  document.getElementById('tab-btn-register').classList.toggle('active', !isLogin);
  document.getElementById('form-login').style.display = isLogin ? 'flex' : 'none';
  document.getElementById('form-register').style.display = isLogin ? 'none' : 'flex';
  hideAuthError();
}

function showAuthError(msg) {
  const box = document.getElementById('auth-error-box');
  box.textContent = msg;
  box.style.display = 'block';
}

function hideAuthError() {
  const box = document.getElementById('auth-error-box');
  box.style.display = 'none';
}

function handleLogin(e) {
  e.preventDefault();
  const username = document.getElementById('login-username').value.trim().toLowerCase();
  const password = document.getElementById('login-password').value;

  const users = loadUsersList();
  const matched = users.find((u) => u.username.toLowerCase() === username && u.password === password);

  if (!matched) {
    showAuthError('Tên đăng nhập hoặc mật khẩu không chính xác.');
    return;
  }

  loginSuccess(matched);
}

function handleRegister(e) {
  e.preventDefault();
  const fullName = document.getElementById('reg-fullname').value.trim();
  const username = document.getElementById('reg-username').value.trim().toLowerCase();
  const password = document.getElementById('reg-password').value;
  const confirm = document.getElementById('reg-confirm').value;

  if (username.length < 3) {
    showAuthError('Tên đăng nhập cần có ít nhất 3 ký tự.');
    return;
  }
  if (password.length < 3) {
    showAuthError('Mật khẩu cần có ít nhất 3 ký tự.');
    return;
  }
  if (password !== confirm) {
    showAuthError('Mật khẩu xác nhận không khớp.');
    return;
  }

  const users = loadUsersList();
  if (users.some((u) => u.username.toLowerCase() === username)) {
    showAuthError('Tên đăng nhập này đã tồn tại, vui lòng chọn tên khác.');
    return;
  }

  const newUser = {
    username,
    fullName,
    password,
    createdAt: getRelativeDateStr(0)
  };
  users.push(newUser);
  saveUsersList(users);
  showToast('Đăng ký tài khoản thành công!', 'success');
  loginSuccess(newUser);
}

function loginSuccess(userObj) {
  currentUser = { username: userObj.username, fullName: userObj.fullName };
  setCurrentUser(currentUser);
  state = loadAppStateForUser(currentUser.username);
  activeNoteId = state.notes[0]?.id || null;
  updateAuthUI();
  applyTheme();
  renderAll();
}

function handleLogout() {
  currentUser = null;
  setCurrentUser(null);
  updateAuthUI();
  showToast('Đã đăng xuất khỏi tài khoản');
}

function updateAuthUI() {
  const authScreen = document.getElementById('auth-screen');
  const appLayout = document.getElementById('app-main-layout');

  if (!currentUser) {
    authScreen.classList.remove('hidden');
    appLayout.classList.add('hidden');
  } else {
    authScreen.classList.add('hidden');
    appLayout.classList.remove('hidden');
    document.getElementById('user-display-name').textContent = currentUser.fullName || currentUser.username;
    document.getElementById('welcome-name').textContent = (currentUser.fullName || currentUser.username).trim().split(/\s+/).pop();
    document.getElementById('user-display-username').textContent = `@${currentUser.username}`;
    const initials = (currentUser.fullName || currentUser.username).trim().charAt(0).toUpperCase();
    document.getElementById('user-avatar-initials').textContent = initials;
    const accNameInput = document.getElementById('acc-fullname');
    if (accNameInput) accNameInput.value = currentUser.fullName || currentUser.username;
  }
}

function handleUpdateAccount(e) {
  e.preventDefault();
  if (!currentUser) return;
  const newFullName = document.getElementById('acc-fullname').value.trim();
  const newPassword = document.getElementById('acc-new-password').value;

  if (newPassword && newPassword.length < 3) {
    showToast('Mật khẩu mới cần có ít nhất 3 ký tự', 'danger');
    return;
  }

  const users = loadUsersList().map((u) => {
    if (u.username.toLowerCase() !== currentUser.username.toLowerCase()) return u;
    return {
      ...u,
      fullName: newFullName || u.fullName,
      password: newPassword ? newPassword : u.password
    };
  });

  saveUsersList(users);
  currentUser.fullName = newFullName || currentUser.fullName;
  setCurrentUser(currentUser);
  document.getElementById('acc-new-password').value = '';
  updateAuthUI();
  showToast('Đã cập nhật thông tin tài khoản', 'success');
}

// ============================================================================
// ĐIỀU HƯỚNG TAB & GIAO DIỆN
// ============================================================================

const TAB_TITLES = {
  dashboard: ['Bảng tổng hợp', 'Theo dõi nhanh lịch làm việc, deadline, chi tiêu và các khoản nợ ưu tiên'],
  schedule: ['Lịch làm việc', 'Chuyển đổi linh hoạt chế độ xem theo Ngày, theo Tuần hoặc theo Tháng'],
  deadlines: ['Lịch Deadline', 'Quản lý thời hạn hoàn thành công việc và mức độ ưu tiên'],
  finance: ['Quản lý Chi tiêu', 'Ghi chép thu chi, kiểm soát ngân sách và phân tích dòng tiền'],
  debts: ['Quản lý & Ưu tiên Nợ', 'Tổng hợp các khoản vay và sắp xếp thứ tự trả nợ tối ưu theo thời gian'],
  notes: ['Sổ ghi chú', 'Lưu trữ các ghi chú văn bản thuần túy, đơn giản và rõ ràng'],
  settings: ['Cài đặt & Giao diện', 'Cá nhân hóa không gian, quản lý tài khoản và sao lưu dữ liệu']
};

function switchTab(tabId) {
  currentTab = tabId;
  document.querySelectorAll('.nav-item').forEach((btn) => {
    btn.classList.toggle('active', btn.dataset.tab === tabId);
  });
  document.querySelectorAll('.tab-pane').forEach((pane) => {
    pane.classList.toggle('active', pane.id === `tab-${tabId}`);
  });
  const [title, sub] = TAB_TITLES[tabId] || ['Quản Lý Cá Nhân', ''];
  document.getElementById('topbar-title').textContent = title;
  document.getElementById('topbar-subtitle').textContent = sub;
  document.getElementById('sidebar').classList.remove('open');
  syncSidebar();
  renderAll();
}

function toggleSidebar() {
  document.getElementById('sidebar').classList.toggle('open');
  syncSidebar();
}

function syncSidebar() {
  const open = document.getElementById('sidebar').classList.contains('open');
  document.getElementById('sidebar-scrim').classList.toggle('open', open);
  document.getElementById('mobile-menu-btn').setAttribute('aria-expanded', String(open));
}

const APPEARANCE_PALETTES = [
  { name: 'Đại dương', color: '#2563eb', note: 'Trong trẻo & tập trung' },
  { name: 'Tím mộng mơ', color: '#7c3aed', note: 'Sáng tạo & tinh tế' },
  { name: 'Rừng xanh', color: '#087f5b', note: 'Bình yên & cân bằng' },
  { name: 'Hồng đất', color: '#be3455', note: 'Ấm áp & dịu dàng' },
  { name: 'Nắng mật ong', color: '#b56b09', note: 'Tươi vui & ấm áp' },
  { name: 'Chàm thanh lịch', color: '#6366f1', note: 'Tối giản & hiện đại' }
];
const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');

function colorLuminance(hex) {
  const values = hex.match(/[a-f\d]{2}/gi).map(part => {
    const channel = parseInt(part, 16) / 255;
    return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return values[0] * 0.2126 + values[1] * 0.7152 + values[2] * 0.0722;
}

function mixColor(hex, target, amount) {
  return '#' + hex.slice(1).match(/../g).map((part, index) => Math.round(parseInt(part, 16) * (1 - amount) + parseInt(target.slice(1 + index * 2, 3 + index * 2), 16) * amount).toString(16).padStart(2, '0')).join('');
}

function applyTheme() {
  const mode = ['light', 'dark', 'system'].includes(state.theme) ? state.theme : 'dark';
  const dark = mode === 'dark' || (mode === 'system' && systemTheme.matches);
  const root = document.documentElement;
  root.dataset.theme = dark ? 'dark' : 'light';
  const accent = /^#[a-f\d]{6}$/i.test(state.accentColor) ? state.accentColor.toLowerCase() : '#6366f1';
  const luminance = colorLuminance(accent);
  const ink = (luminance + 0.05) / 0.05 > 1.05 / (luminance + 0.05) ? '#000000' : '#ffffff';
  const surface = dark ? '#141b29' : '#ffffff';
  let readable = accent;
  // Keep accent labels readable even for very pale or very dark custom colors.
  for (let step = 0; step < 20; step++) {
    const a = colorLuminance(readable), b = colorLuminance(surface);
    if ((Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05) >= 4.5) break;
    readable = mixColor(readable, dark ? '#ffffff' : '#000000', 0.12);
  }
  root.style.setProperty('--accent-primary', accent);
  root.style.setProperty('--accent-hover', mixColor(accent, ink === '#ffffff' ? '#000000' : '#ffffff', 0.12));
  root.style.setProperty('--accent-ink', ink);
  root.style.setProperty('--accent-text', readable);
  root.style.setProperty('--accent-light', accent + (dark ? '24' : '10'));
  root.style.setProperty('--accent-border', accent + '55');
  document.getElementById('theme-toggle-btn').textContent = dark ? '☀  Chuyển sang chế độ sáng' : '☾  Chuyển sang chế độ tối';
  renderAppearanceSettings(mode, accent);
}

function renderAppearanceSettings(mode, accent) {
  document.getElementById('theme-mode-options').innerHTML = [
    ['light', '☀', 'Sáng'], ['dark', '☾', 'Tối'], ['system', '◐', 'Hệ thống']
  ].map(([id, icon, label]) => `<button type="button" class="mode-option ${mode === id ? 'selected' : ''}" aria-pressed="${mode === id}" onclick="setThemeMode('${id}')"><span aria-hidden="true">${icon}</span>${label}<span class="selection-tick" aria-hidden="true">✓</span></button>`).join('');
  document.getElementById('theme-palette-options').innerHTML = APPEARANCE_PALETTES.map(palette => `<button type="button" class="palette-option ${accent === palette.color ? 'selected' : ''}" aria-pressed="${accent === palette.color}" onclick="setAccentColor('${palette.color}')"><span class="palette-dot" style="background:${palette.color}" aria-hidden="true"></span><span><strong>${palette.name}</strong><small>${palette.note}</small></span><span class="selection-tick" aria-hidden="true">✓</span></button>`).join('');
  document.getElementById('custom-accent').value = accent;
  document.getElementById('custom-accent-value').textContent = accent.toUpperCase();
  document.getElementById('appearance-name').textContent = APPEARANCE_PALETTES.find(p => p.color === accent)?.name || 'Màu riêng của bạn';
}

function setThemeMode(mode) {
  if (!['light', 'dark', 'system'].includes(mode)) return;
  state.theme = mode;
  saveCurrentState();
  applyTheme();
}

function setAccentColor(color) {
  if (!/^#[a-f\d]{6}$/i.test(color)) return;
  state.accentColor = color.toLowerCase();
  saveCurrentState();
  applyTheme();
}

function resetAppearance() {
  state.theme = 'light';
  state.accentColor = '#6366f1';
  saveCurrentState();
  applyTheme();
  showToast('Đã khôi phục giao diện mặc định', 'success');
}

function toggleTheme() {
  setThemeMode(document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark');
}

systemTheme.addEventListener('change', () => { if (state.theme === 'system') applyTheme(); });

function startLiveClock() {
  const el = document.getElementById('live-clock');
  const update = () => {
    const now = new Date();
    const days = ['CN', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
    const timeStr = now.toLocaleTimeString('vi-VN', { hour12: false });
    const dateStr = now.toLocaleDateString('vi-VN');
    el.textContent = `${days[now.getDay()]}, ${dateStr} • ${timeStr}`;
  };
  update();
  setInterval(update, 1000);
}

// ============================================================================
// THUẬT TOÁN ƯU TIÊN TRẢ NỢ
// ============================================================================

function getRemainingDebt(debt) {
  return Math.max(0, (Number(debt.totalAmount) || 0) - (Number(debt.paidAmount) || 0));
}

function sortDebtsByStrategy(debtsList, strategy = 'time') {
  const copy = [...debtsList];
  copy.sort((a, b) => {
    const remA = getRemainingDebt(a);
    const remB = getRemainingDebt(b);
    if (remA === 0 && remB > 0) return 1;
    if (remB === 0 && remA > 0) return -1;

    const daysA = getDaysRemaining(a.dueDate);
    const daysB = getDaysRemaining(b.dueDate);
    const prioA = getPriorityWeight(a.priorityLevel);
    const prioB = getPriorityWeight(b.priorityLevel);
    const rateA = Number(a.interestRate) || 0;
    const rateB = Number(b.interestRate) || 0;

    if (strategy === 'time') {
      if (daysA !== daysB) return daysA - daysB;
      if (prioB !== prioA) return prioB - prioA;
      return rateB - rateA;
    }
    if (strategy === 'avalanche') {
      if (rateB !== rateA) return rateB - rateA;
      if (prioB !== prioA) return prioB - prioA;
      return daysA - daysB;
    }
    if (strategy === 'snowball') {
      if (remA !== remB) return remA - remB;
      return daysA - daysB;
    }
    return daysA - daysB;
  });
  return copy;
}

// ============================================================================
// BIỂU ĐỒ TRÒN CHI TIÊU (SVG DONUT)
// ============================================================================

function buildDonutChartHTML(items, totalAmount, emptyText = 'Chưa có dữ liệu chi tiêu') {
  if (!items.length || totalAmount <= 0) {
    return `<div class="empty-state">${emptyText}</div>`;
  }

  const size = 156;
  const strokeWidth = 22;
  const center = size / 2;
  const radius = center - strokeWidth;
  const circumference = 2 * Math.PI * radius;

  let accumulatedPercent = 0;
  const circles = items
    .map((item) => {
      const pct = item.amount / totalAmount;
      const strokeDasharray = `${pct * circumference} ${circumference}`;
      const strokeDashoffset = -accumulatedPercent * circumference;
      accumulatedPercent += pct;
      return `<circle
        cx="${center}" cy="${center}" r="${radius}"
        fill="transparent"
        stroke="${item.color}"
        stroke-width="${strokeWidth}"
        stroke-dasharray="${strokeDasharray}"
        stroke-dashoffset="${strokeDashoffset}"
        transform="rotate(-90 ${center} ${center})"
      />`;
    })
    .join('');

  const legendRows = items
    .map((item) => {
      const pct = Math.round((item.amount / totalAmount) * 100);
      return `
        <div class="legend-item">
          <div class="legend-label">
            <span class="legend-dot" style="background: ${item.color}"></span>
            <span>${escapeHtml(item.name)}</span>
          </div>
          <div style="font-weight: 600;">
            ${formatVND(item.amount)} <span style="color: var(--text-muted); font-size: 0.74rem;">(${pct}%)</span>
          </div>
        </div>
      `;
    })
    .join('');

  return `
    <div style="position: relative; width: ${size}px; height: ${size}px; flex-shrink: 0;">
      <svg width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">
        <circle cx="${center}" cy="${center}" r="${radius}" fill="transparent" stroke="var(--bg-tertiary)" stroke-width="${strokeWidth}" />
        ${circles}
      </svg>
      <div style="position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; text-align: center; pointer-events: none;">
        <span style="font-size: 0.68rem; color: var(--text-secondary);">Tổng chi</span>
        <strong style="font-size: 0.8rem;">${formatVND(totalAmount)}</strong>
      </div>
    </div>
    <div class="chart-legend">${legendRows}</div>
  `;
}

function getExpenseBreakdownData() {
  const map = {};
  state.transactions
    .filter((t) => t.type === 'expense')
    .forEach((t) => {
      map[t.category] = (map[t.category] || 0) + Number(t.amount);
    });

  return Object.entries(map)
    .map(([catId, amount]) => {
      const meta = getCategoryMeta(catId);
      return {
        id: catId,
        name: meta.name,
        color: meta.color,
        amount
      };
    })
    .sort((a, b) => b.amount - a.amount);
}

// ============================================================================
// CẬP NHẬT BADGE & BẢNG TỔNG HỢP (DASHBOARD)
// ============================================================================

function updateSidebarBadges() {
  const todayStr = getRelativeDateStr(0);
  document.getElementById('badge-schedule').textContent = state.schedules.filter((s) => s.date === todayStr).length;
  document.getElementById('badge-deadlines').textContent = state.deadlines.filter((d) => d.status !== 'done').length;
  document.getElementById('badge-debts').textContent = state.debts.filter(
    (d) => d.direction === 'borrowed' && getRemainingDebt(d) > 0
  ).length;
  document.getElementById('badge-notes').textContent = state.notes.length;
}

function renderDashboard() {
  const todayStr = getRelativeDateStr(0);
  document.getElementById('dash-today-date-label').textContent = `Ngày ${formatDateVN(todayStr)}`;

  const totalIncome = state.transactions
    .filter((t) => t.type === 'income')
    .reduce((sum, t) => sum + Number(t.amount), 0);
  const totalExpense = state.transactions
    .filter((t) => t.type === 'expense')
    .reduce((sum, t) => sum + Number(t.amount), 0);
  const netBalance = totalIncome - totalExpense;

  document.getElementById('dash-net-balance').textContent = formatVND(netBalance);
  document.getElementById('dash-income-expense-sub').textContent = `Thu: ${formatVND(totalIncome)} • Chi: ${formatVND(totalExpense)}`;

  const borrowedDebts = state.debts.filter((d) => d.direction === 'borrowed');
  const lentDebts = state.debts.filter((d) => d.direction === 'lent');
  const totalBorrowedRemaining = borrowedDebts.reduce((sum, d) => sum + getRemainingDebt(d), 0);
  const totalLentRemaining = lentDebts.reduce((sum, d) => sum + getRemainingDebt(d), 0);

  document.getElementById('dash-total-debt').textContent = formatVND(totalBorrowedRemaining);
  document.getElementById('dash-debt-sub').textContent = `Khoản cho vay cần thu: ${formatVND(totalLentRemaining)}`;

  const activeDeadlines = state.deadlines.filter((d) => d.status !== 'done');
  const urgentDeadlines = activeDeadlines.filter(
    (d) => d.priority === 'urgent' || getDaysRemaining(d.dueDate) <= 1
  );
  document.getElementById('dash-active-deadlines').textContent = `${activeDeadlines.length} việc`;
  document.getElementById('dash-deadline-sub').textContent = `${urgentDeadlines.length} việc khẩn cấp / sát hạn`;

  const todaySchedules = state.schedules
    .filter((s) => s.date === todayStr)
    .sort((a, b) => a.startTime.localeCompare(b.startTime));
  document.getElementById('dash-today-schedules').textContent = `${todaySchedules.length} lịch`;

  // 1. Ưu tiên trả nợ trên Dashboard
  const sortedBorrowed = sortDebtsByStrategy(
    borrowedDebts.filter((d) => getRemainingDebt(d) > 0),
    'time'
  ).slice(0, 3);

  const dashDebtsEl = document.getElementById('dash-priority-debts');
  if (!sortedBorrowed.length) {
    dashDebtsEl.innerHTML = `<div class="empty-state">Hiện không có khoản nợ nào cần trả.</div>`;
  } else {
    dashDebtsEl.innerHTML = sortedBorrowed
      .map((debt, idx) => {
        const rem = getRemainingDebt(debt);
        const days = getDaysRemaining(debt.dueDate);
        const statusClass = days < 0 ? 'overdue' : days <= 3 ? 'due-soon' : '';
        const dueBadge =
          days < 0
            ? `<span class="badge badge-danger">Quá hạn ${Math.abs(days)} ngày</span>`
            : days === 0
            ? `<span class="badge badge-danger">Đến hạn hôm nay</span>`
            : `<span class="badge badge-warning">Còn ${days} ngày (${formatDateVN(debt.dueDate)})</span>`;

        return `
          <div class="debt-priority-card ${statusClass}">
            <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 10px;">
              <div style="display: flex; align-items: center; gap: 10px;">
                <span class="debt-rank-number">${idx + 1}</span>
                <div>
                  <div class="item-title">${escapeHtml(debt.person)}</div>
                  <div style="font-size: 0.78rem; color: var(--text-secondary);">${escapeHtml(debt.title)}</div>
                </div>
              </div>
              <div style="text-align: right;">
                <div style="font-weight: 700; color: var(--danger); font-size: 0.95rem;">${formatVND(rem)}</div>
                <small style="font-size: 0.72rem; color: var(--text-muted);">Gốc: ${formatVND(debt.totalAmount)}</small>
              </div>
            </div>
            <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 8px;">
              <div style="display: flex; gap: 6px; flex-wrap: wrap;">
                ${dueBadge}
                ${debt.interestRate > 0 ? `<span class="badge badge-neutral">Lãi ${debt.interestRate}%/năm</span>` : ''}
              </div>
              <button class="btn btn-xs btn-primary" onclick="openPayDebtModal('${debt.id}')">Thanh toán</button>
            </div>
          </div>
        `;
      })
      .join('');
  }

  // 2. Deadline sắp tới
  const sortedDeadlines = [...activeDeadlines]
    .sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate))
    .slice(0, 4);

  const dashDlEl = document.getElementById('dash-upcoming-deadlines');
  if (!sortedDeadlines.length) {
    dashDlEl.innerHTML = `<div class="empty-state">Không có deadline nào đang chờ xử lý.</div>`;
  } else {
    dashDlEl.innerHTML = sortedDeadlines
      .map(
        (dl) => `
        <div class="list-item">
          <div class="list-item-left" style="flex-direction: column; gap: 5px;">
            <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
              ${getPriorityBadge(dl.priority)}
              <span class="item-title">${escapeHtml(dl.title)}</span>
            </div>
            <div class="item-meta">
              ${getRemainingTimeBadge(dl.dueDate, dl.status)}
              <span>Hạn: ${formatDateTimeVN(dl.dueDate)}</span>
              <span>Tiến độ: <strong>${dl.progress}%</strong></span>
            </div>
            <div class="progress-wrap">
              <div class="progress-bar ${dl.progress >= 80 ? 'green' : dl.progress >= 40 ? 'indigo' : 'amber'}" style="width: ${dl.progress}%"></div>
            </div>
          </div>
          <div class="list-item-right">
            <button class="btn btn-xs btn-secondary" onclick="markDeadlineDone('${dl.id}')">Hoàn thành</button>
          </div>
        </div>
      `
      )
      .join('');
  }

  // 3. Lịch làm việc hôm nay
  const dashSchEl = document.getElementById('dash-today-schedule-list');
  if (!todaySchedules.length) {
    dashSchEl.innerHTML = `<div class="empty-state">Hôm nay chưa có lịch làm việc nào.</div>`;
  } else {
    const typeLabels = {
      work: '<span class="badge badge-info">Ca làm việc</span>',
      meeting: '<span class="badge badge-warning">Cuộc họp</span>',
      personal: '<span class="badge badge-success">Cá nhân</span>'
    };
    dashSchEl.innerHTML = todaySchedules
      .map(
        (s) => `
        <div class="list-item">
          <div class="list-item-left">
            <div style="min-width: 92px; font-weight: 700; color: var(--accent-primary); font-size: 0.84rem;">
              ${s.startTime} - ${s.endTime}
            </div>
            <div>
              <div class="item-title">${escapeHtml(s.title)}</div>
              <div class="item-meta">
                ${typeLabels[s.type] || ''}
                ${s.location ? `<span>Địa điểm: ${escapeHtml(s.location)}</span>` : ''}
              </div>
            </div>
          </div>
        </div>
      `
      )
      .join('');
  }

  // 4. Ngân sách & Biểu đồ
  const budget = Number(state.monthlyBudget) || 10000000;
  const usedPct = Math.min(100, Math.round((totalExpense / budget) * 100));
  const barColor = usedPct >= 90 ? 'red' : usedPct >= 75 ? 'amber' : 'green';
  document.getElementById('dash-budget-bar-wrap').innerHTML = `
    <div style="display: flex; justify-content: space-between; font-size: 0.8rem; margin-bottom: 4px;">
      <span>Đã chi tiêu: <strong>${formatVND(totalExpense)} / ${formatVND(budget)}</strong></span>
      <strong>${usedPct}%</strong>
    </div>
    <div class="progress-wrap">
      <div class="progress-bar ${barColor}" style="width: ${usedPct}%"></div>
    </div>
  `;

  document.getElementById('dash-expense-chart').innerHTML = buildDonutChartHTML(
    getExpenseBreakdownData(),
    totalExpense
  );
}

// ============================================================================
// TAB 2: LỊCH LÀM VIỆC (XEM THEO NGÀY / TUẦN / THÁNG)
// ============================================================================

function setScheduleViewMode(mode) {
  scheduleViewMode = mode;
  ['day', 'week', 'month'].forEach((m) => {
    document.getElementById(`sch-view-${m}`).classList.toggle('active', m === mode);
  });
  renderScheduleTab();
}

function navigateSchedulePeriod(direction) {
  if (scheduleViewMode === 'day') {
    scheduleFocusDate.setDate(scheduleFocusDate.getDate() + direction);
  } else if (scheduleViewMode === 'week') {
    scheduleFocusDate.setDate(scheduleFocusDate.getDate() + direction * 7);
  } else {
    scheduleFocusDate.setMonth(scheduleFocusDate.getMonth() + direction);
  }
  renderScheduleTab();
}

function navigateScheduleToday() {
  scheduleFocusDate = new Date();
  renderScheduleTab();
}

function selectDayAndSwitchToDayView(dateStr) {
  scheduleFocusDate = new Date(dateStr + 'T09:00:00');
  setScheduleViewMode('day');
}

function getMondayOfWeek(d) {
  const copy = new Date(d);
  const day = copy.getDay();
  const diff = copy.getDate() - day + (day === 0 ? -6 : 1);
  copy.setDate(diff);
  return copy;
}

function renderScheduleTab() {
  const filterType = document.getElementById('schedule-filter-type').value;
  const viewport = document.getElementById('schedule-main-viewport');
  const labelEl = document.getElementById('schedule-period-label');
  const todayStr = getRelativeDateStr(0);

  const typeBadgeMap = {
    work: '<span class="badge badge-info">Ca làm việc</span>',
    meeting: '<span class="badge badge-warning">Cuộc họp</span>',
    personal: '<span class="badge badge-success">Cá nhân</span>'
  };

  // ---------------- 1. CHẾ ĐỘ XEM THEO NGÀY (DAY VIEW) ----------------
  if (scheduleViewMode === 'day') {
    const targetStr = toDateStr(scheduleFocusDate);
    const dayNames = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
    labelEl.textContent = `${dayNames[scheduleFocusDate.getDay()]}, ${formatDateVN(targetStr)}`;

    let daySchedules = state.schedules.filter((s) => s.date === targetStr);
    if (filterType !== 'all') {
      daySchedules = daySchedules.filter((s) => s.type === filterType);
    }
    daySchedules.sort((a, b) => a.startTime.localeCompare(b.startTime));

    // Tạo dòng thời gian từ 07:00 đến 21:00
    const hours = [];
    for (let h = 7; h <= 21; h++) {
      const hPrefix = String(h).padStart(2, '0');
      const matchedInHour = daySchedules.filter((s) => s.startTime.startsWith(hPrefix));
      const slotHTML = matchedInHour
        .map(
          (s) => `
          <div class="list-item" style="padding: 8px 12px;">
            <div class="list-item-left" style="align-items: center;">
              <strong style="color: var(--accent-primary); min-width: 90px; font-size: 0.82rem;">${s.startTime} - ${s.endTime}</strong>
              ${typeBadgeMap[s.type] || ''}
              <span class="item-title" style="font-size: 0.86rem;">${escapeHtml(s.title)}</span>
              ${s.location ? `<span style="font-size: 0.76rem; color: var(--text-secondary);">(${escapeHtml(s.location)})</span>` : ''}
            </div>
            <div class="list-item-right">
              <button class="btn btn-xs btn-secondary" onclick="openScheduleModal(null, '${s.id}')">Sửa</button>
              <button class="btn btn-xs btn-danger" onclick="deleteSchedule('${s.id}')">Xóa</button>
            </div>
          </div>
        `
        )
        .join('');

      hours.push(`
        <div class="day-hour-row">
          <div class="day-hour-label">${hPrefix}:00</div>
          <div class="day-hour-slot">${slotHTML}</div>
        </div>
      `);
    }

    viewport.innerHTML = `
      <div class="grid-sidebar-right">
        <div class="card">
          <div class="card-header">
            <div>
              <div class="card-title">Khung giờ chi tiết trong ngày (${formatDateVN(targetStr)})</div>
              <div class="card-subtitle">Các công việc và sự kiện phân bổ từ 07:00 đến 21:00</div>
            </div>
            <button class="btn btn-xs btn-primary" onclick="openScheduleModal('${targetStr}')">+ Thêm vào ngày này</button>
          </div>
          <div class="day-timeline">${hours.join('')}</div>
        </div>

        <div class="card">
          <div class="card-header">
            <div>
              <div class="card-title">Tổng hợp sự kiện ngày ${formatDateVN(targetStr)}</div>
              <div class="card-subtitle">${daySchedules.length} ca làm việc / sự kiện</div>
            </div>
          </div>
          <div class="item-list">
            ${
              daySchedules.length
                ? daySchedules
                    .map(
                      (s) => `
                    <div class="compact-schedule-card ${s.type}">
                      <div class="compact-schedule-top">
                        <strong style="color: var(--accent-primary); font-size: 0.82rem;">${s.startTime} - ${s.endTime}</strong>
                        ${typeBadgeMap[s.type] || ''}
                      </div>
                      <div class="item-title">${escapeHtml(s.title)}</div>
                      ${s.location ? `<div style="font-size: 0.76rem; color: var(--text-secondary);">Địa điểm: ${escapeHtml(s.location)}</div>` : ''}
                      ${s.note ? `<div style="font-size: 0.75rem; color: var(--text-muted);">Ghi chú: ${escapeHtml(s.note)}</div>` : ''}
                      <div class="compact-schedule-actions">
                        <button class="btn btn-xs btn-secondary" onclick="openScheduleModal(null, '${s.id}')">Sửa</button>
                        <button class="btn btn-xs btn-danger" onclick="deleteSchedule('${s.id}')">Xóa</button>
                      </div>
                    </div>
                  `
                    )
                    .join('')
                : `<div class="empty-state">Chưa có lịch làm việc nào trong ngày này.</div>`
            }
          </div>
        </div>
      </div>
    `;
    return;
  }

  // ---------------- 2. CHẾ ĐỘ XEM THEO TUẦN (WEEK VIEW) ----------------
  if (scheduleViewMode === 'week') {
    const monday = getMondayOfWeek(scheduleFocusDate);
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    labelEl.textContent = `Tuần: ${formatDateVN(toDateStr(monday))} — ${formatDateVN(toDateStr(sunday))}`;

    const dayShortNames = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ Nhật'];
    const colsHTML = [];

    for (let i = 0; i < 7; i++) {
      const curDay = new Date(monday);
      curDay.setDate(monday.getDate() + i);
      const dStr = toDateStr(curDay);
      const isToday = dStr === todayStr;

      let dayEvents = state.schedules.filter((s) => s.date === dStr);
      if (filterType !== 'all') {
        dayEvents = dayEvents.filter((s) => s.type === filterType);
      }
      dayEvents.sort((a, b) => a.startTime.localeCompare(b.startTime));

      const cards = dayEvents
        .map(
          (s) => `
          <div class="week-event-card ${s.type}" onclick="openScheduleModal(null, '${s.id}')" title="Bấm để chỉnh sửa">
            <div class="week-event-time">${s.startTime} - ${s.endTime}</div>
            <div class="week-event-title">${escapeHtml(s.title)}</div>
            ${s.location ? `<div style="font-size: 0.7rem; color: var(--text-secondary); margin-top: 2px;">${escapeHtml(s.location)}</div>` : ''}
          </div>
        `
        )
        .join('');

      colsHTML.push(`
        <div class="week-day-column ${isToday ? 'today' : ''}">
          <div class="week-day-header">
            <div class="w-name">${dayShortNames[i]} ${isToday ? '• Hôm nay' : ''}</div>
            <div class="w-date">${curDay.getDate()}/${curDay.getMonth() + 1}</div>
          </div>
          <div class="week-day-events">
            ${cards || '<div style="font-size: 0.74rem; color: var(--text-muted); text-align: center; padding: 18px 4px;">Trống</div>'}
          </div>
          <div style="padding: 8px; border-top: 1px solid var(--border-color); text-align: center;">
            <button class="btn btn-xs btn-secondary" style="width: 100%;" onclick="openScheduleModal('${dStr}')">+ Thêm lịch</button>
          </div>
        </div>
      `);
    }

    viewport.innerHTML = `
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Bảng lịch làm việc trong tuần (7 ngày)</div>
            <div class="card-subtitle">Bấm vào từng công việc để sửa, hoặc bấm "+ Thêm lịch" ở chân mỗi cột ngày</div>
          </div>
        </div>
        <div class="week-grid">${colsHTML.join('')}</div>
      </div>
    `;
    return;
  }

  // ---------------- 3. CHẾ ĐỘ XEM THEO THÁNG (MONTH VIEW) ----------------
  const year = scheduleFocusDate.getFullYear();
  const month = scheduleFocusDate.getMonth();
  labelEl.textContent = `Tháng ${month + 1} / ${year}`;

  const firstDayOfMonth = new Date(year, month, 1);
  const lastDayOfMonth = new Date(year, month + 1, 0);
  const startDayOffset = (firstDayOfMonth.getDay() + 6) % 7;
  const totalDays = lastDayOfMonth.getDate();
  const selectedStr = toDateStr(scheduleFocusDate);

  const cells = [];
  for (let i = 0; i < startDayOffset; i++) {
    cells.push(`<div class="calendar-cell other-month"></div>`);
  }

  for (let day = 1; day <= totalDays; day++) {
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    const isToday = dateStr === todayStr;
    const isSelected = dateStr === selectedStr;

    let daySchedules = state.schedules.filter((s) => s.date === dateStr);
    if (filterType !== 'all') {
      daySchedules = daySchedules.filter((s) => s.type === filterType);
    }
    const dayDeadlines = state.deadlines.filter(
      (d) => d.status !== 'done' && d.dueDate.startsWith(dateStr)
    );

    const dotsHTML = [
      ...daySchedules.slice(0, 3).map(
        (s) => `<div class="cal-event-dot ${s.type}" title="${escapeHtml(s.startTime + ' ' + s.title)}">${s.startTime} ${escapeHtml(s.title)}</div>`
      ),
      ...dayDeadlines.slice(0, 1).map(
        (dl) => `<div class="cal-event-dot deadline" title="${escapeHtml(dl.title)}">Hạn: ${escapeHtml(dl.title)}</div>`
      )
    ].join('');

    cells.push(`
      <div class="calendar-cell ${isToday ? 'today' : ''}"
           style="${isSelected ? 'outline: 2px solid var(--accent-primary);' : ''}"
           onclick="selectMonthCell('${dateStr}')"
           ondblclick="selectDayAndSwitchToDayView('${dateStr}')">
        <div class="calendar-cell-date">
          <span>${day}</span>
          ${isToday ? '<span style="font-size: 0.65rem; color: var(--accent-primary);">Hôm nay</span>' : ''}
        </div>
        ${dotsHTML}
      </div>
    `);
  }

  let rightList = state.schedules.filter((s) => s.date === selectedStr);
  if (filterType !== 'all') {
    rightList = rightList.filter((s) => s.type === filterType);
  }
  rightList.sort((a, b) => a.startTime.localeCompare(b.startTime));

  viewport.innerHTML = `
    <div class="grid-sidebar-right">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Lưới lịch Tháng ${month + 1}/${year}</div>
            <div class="card-subtitle">Bấm 1 lần vào ngày để xem danh sách bên phải, nháy đúp để mở Xem theo Ngày</div>
          </div>
        </div>
        <div class="calendar-grid">
          <div class="calendar-day-header">Thứ 2</div>
          <div class="calendar-day-header">Thứ 3</div>
          <div class="calendar-day-header">Thứ 4</div>
          <div class="calendar-day-header">Thứ 5</div>
          <div class="calendar-day-header">Thứ 6</div>
          <div class="calendar-day-header">Thứ 7</div>
          <div class="calendar-day-header">CN</div>
          ${cells.join('')}
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Chi tiết ngày ${formatDateVN(selectedStr)}</div>
            <div class="card-subtitle">${rightList.length} sự kiện</div>
          </div>
          <button class="btn btn-xs btn-primary" onclick="openScheduleModal('${selectedStr}')">+ Thêm lịch</button>
        </div>
        <div class="item-list">
          ${
            rightList.length
              ? rightList
                  .map(
                    (s) => `
                  <div class="compact-schedule-card ${s.type}">
                    <div class="compact-schedule-top">
                      <strong style="color: var(--accent-primary); font-size: 0.82rem;">${s.startTime} - ${s.endTime}</strong>
                      ${typeBadgeMap[s.type] || ''}
                    </div>
                    <div class="item-title">${escapeHtml(s.title)}</div>
                    ${s.location ? `<div style="font-size: 0.76rem; color: var(--text-secondary);">Địa điểm: ${escapeHtml(s.location)}</div>` : ''}
                    ${s.note ? `<div style="font-size: 0.75rem; color: var(--text-muted);">Ghi chú: ${escapeHtml(s.note)}</div>` : ''}
                    <div class="compact-schedule-actions">
                      <button class="btn btn-xs btn-secondary" onclick="openScheduleModal(null, '${s.id}')">Sửa</button>
                      <button class="btn btn-xs btn-danger" onclick="deleteSchedule('${s.id}')">Xóa</button>
                    </div>
                  </div>
                `
                  )
                  .join('')
              : `<div class="empty-state">Ngày ${formatDateVN(selectedStr)} chưa có lịch làm việc.</div>`
          }
        </div>
      </div>
    </div>
  `;
}

function selectMonthCell(dateStr) {
  scheduleFocusDate = new Date(dateStr + 'T09:00:00');
  renderScheduleTab();
}

function openScheduleModal(defaultDate = null, editId = null) {
  const existing = editId ? state.schedules.find((s) => s.id === editId) : null;
  const dateVal = existing ? existing.date : defaultDate || toDateStr(scheduleFocusDate);

  openModal(
    existing ? 'Chỉnh sửa lịch làm việc' : 'Thêm lịch làm việc mới',
    `
      <form onsubmit="saveScheduleForm(event, '${editId || ''}')">
        <div class="form-grid">
          <div class="form-group full">
            <label>Nội dung công việc / Ca làm *</label>
            <input type="text" class="input" id="sch-title" required value="${existing ? escapeHtml(existing.title) : ''}" placeholder="VD: Họp giao ban, Làm việc ca sáng..." />
          </div>
          <div class="form-group">
            <label>Ngày *</label>
            <input type="date" class="input" id="sch-date" required value="${dateVal}" />
          </div>
          <div class="form-group">
            <label>Phân loại</label>
            <select class="select" id="sch-type">
              <option value="work" ${existing?.type === 'work' ? 'selected' : ''}>Ca làm việc</option>
              <option value="meeting" ${existing?.type === 'meeting' ? 'selected' : ''}>Cuộc họp</option>
              <option value="personal" ${existing?.type === 'personal' ? 'selected' : ''}>Cá nhân</option>
            </select>
          </div>
          <div class="form-group">
            <label>Giờ bắt đầu *</label>
            <input type="time" class="input" id="sch-start" required value="${existing ? existing.startTime : '08:30'}" />
          </div>
          <div class="form-group">
            <label>Giờ kết thúc *</label>
            <input type="time" class="input" id="sch-end" required value="${existing ? existing.endTime : '11:30'}" />
          </div>
          <div class="form-group full">
            <label>Địa điểm / Phòng họp</label>
            <input type="text" class="input" id="sch-location" value="${existing ? escapeHtml(existing.location) : ''}" placeholder="VD: Văn phòng, Trực tuyến..." />
          </div>
          <div class="form-group full">
            <label>Ghi chú</label>
            <input type="text" class="input" id="sch-note" value="${existing ? escapeHtml(existing.note) : ''}" placeholder="Ghi chú thêm..." />
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
          <button type="submit" class="btn btn-primary">Lưu lịch</button>
        </div>
      </form>
    `
  );
}

function saveScheduleForm(e, editId) {
  e.preventDefault();
  const item = {
    id: editId || 'sch-' + Date.now(),
    title: document.getElementById('sch-title').value.trim(),
    date: document.getElementById('sch-date').value,
    type: document.getElementById('sch-type').value,
    startTime: document.getElementById('sch-start').value,
    endTime: document.getElementById('sch-end').value,
    location: document.getElementById('sch-location').value.trim(),
    note: document.getElementById('sch-note').value.trim()
  };

  if (editId) {
    state.schedules = state.schedules.map((s) => (s.id === editId ? item : s));
    showToast('Đã cập nhật lịch làm việc', 'success');
  } else {
    state.schedules.push(item);
    showToast('Đã thêm lịch làm việc mới', 'success');
  }
  saveCurrentState();
  closeModal();
  renderAll();
}

function deleteSchedule(id) {
  state.schedules = state.schedules.filter((s) => s.id !== id);
  saveCurrentState();
  showToast('Đã xóa lịch làm việc', 'danger');
  renderAll();
}

// ============================================================================
// TAB 3: LỊCH DEADLINE
// ============================================================================

function setDeadlineFilter(filter) {
  deadlineFilterStatus = filter;
  const buttons = document.querySelectorAll('#deadline-status-tabs .pill-tab');
  const map = ['all', 'active', 'overdue', 'done'];
  buttons.forEach((btn, idx) => btn.classList.toggle('active', map[idx] === filter));
  renderDeadlinesTab();
}

function renderDeadlinesTab() {
  const total = state.deadlines.length;
  const active = state.deadlines.filter((d) => d.status !== 'done').length;
  const overdue = state.deadlines.filter(
    (d) => d.status !== 'done' && (getDaysRemaining(d.dueDate) < 0 || d.priority === 'urgent')
  ).length;
  const done = state.deadlines.filter((d) => d.status === 'done').length;

  document.getElementById('deadline-summary-stats').innerHTML = `
    <div class="stat-card">
      <span>Tổng số Deadline</span><h3>${total}</h3><small>Toàn bộ đầu việc</small>
    </div>
    <div class="stat-card">
      <span>Đang thực hiện</span><h3>${active}</h3><small>Chưa hoàn thành</small>
    </div>
    <div class="stat-card">
      <span>Khẩn cấp / Quá hạn</span><h3 style="color: var(--danger);">${overdue}</h3><small>Cần xử lý ưu tiên</small>
    </div>
    <div class="stat-card">
      <span>Đã hoàn thành</span><h3 style="color: var(--success);">${done}</h3><small>Tỷ lệ hoàn thành: ${total ? Math.round((done / total) * 100) : 0}%</small>
    </div>
  `;

  let list = [...state.deadlines];
  if (deadlineFilterStatus === 'active') {
    list = list.filter((d) => d.status !== 'done');
  } else if (deadlineFilterStatus === 'overdue') {
    list = list.filter((d) => d.status !== 'done' && (getDaysRemaining(d.dueDate) <= 1 || d.priority === 'urgent'));
  } else if (deadlineFilterStatus === 'done') {
    list = list.filter((d) => d.status === 'done');
  }

  const sortBy = document.getElementById('deadline-sort-by').value;
  list.sort((a, b) => {
    if (a.status === 'done' && b.status !== 'done') return 1;
    if (b.status === 'done' && a.status !== 'done') return -1;
    if (sortBy === 'due_asc') return new Date(a.dueDate) - new Date(b.dueDate);
    if (sortBy === 'priority_desc') return getPriorityWeight(b.priority) - getPriorityWeight(a.priority);
    if (sortBy === 'progress_asc') return a.progress - b.progress;
    return 0;
  });

  const container = document.getElementById('deadline-list-container');
  if (!list.length) {
    container.innerHTML = `<div class="empty-state">Không có deadline nào phù hợp.</div>`;
    return;
  }

  container.innerHTML = list
    .map(
      (dl) => `
      <div class="list-item ${dl.status === 'done' ? 'completed' : ''}">
        <div class="list-item-left" style="flex-direction: column; gap: 6px;">
          <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
            ${getPriorityBadge(dl.priority)}
            <span class="badge badge-neutral">${escapeHtml(dl.category || 'Chung')}</span>
            <span class="item-title">${escapeHtml(dl.title)}</span>
          </div>
          <div class="item-meta">
            ${getRemainingTimeBadge(dl.dueDate, dl.status)}
            <span>Hạn chót: <strong>${formatDateTimeVN(dl.dueDate)}</strong></span>
            ${dl.note ? `<span>• Ghi chú: ${escapeHtml(dl.note)}</span>` : ''}
          </div>
          <div style="display: flex; align-items: center; gap: 12px; width: 100%; max-width: 420px; margin-top: 2px;">
            <div class="progress-wrap" style="margin-top: 0; flex: 1;">
              <div class="progress-bar ${dl.progress >= 80 ? 'green' : dl.progress >= 40 ? 'indigo' : 'amber'}" style="width: ${dl.progress}%"></div>
            </div>
            <span style="font-size: 0.76rem; font-weight: 600; min-width: 34px;">${dl.progress}%</span>
            <input type="range" min="0" max="100" step="10" value="${dl.progress}"
                   style="width: 80px; cursor: pointer;"
                   onchange="updateDeadlineProgress('${dl.id}', this.value)" />
          </div>
        </div>
        <div class="list-item-right">
          ${
            dl.status !== 'done'
              ? `<button class="btn btn-xs btn-success" onclick="markDeadlineDone('${dl.id}')">Hoàn thành</button>`
              : `<button class="btn btn-xs btn-secondary" onclick="reopenDeadline('${dl.id}')">Mở lại</button>`
          }
          <button class="btn btn-xs btn-secondary" onclick="openDeadlineModal('${dl.id}')">Sửa</button>
          <button class="btn btn-xs btn-danger" onclick="deleteDeadline('${dl.id}')">Xóa</button>
        </div>
      </div>
    `
    )
    .join('');
}

function updateDeadlineProgress(id, newProgress) {
  const pct = Number(newProgress);
  state.deadlines = state.deadlines.map((d) => {
    if (d.id !== id) return d;
    return {
      ...d,
      progress: pct,
      status: pct >= 100 ? 'done' : pct > 0 ? 'in_progress' : 'todo'
    };
  });
  saveCurrentState();
  renderAll();
}

function markDeadlineDone(id) {
  state.deadlines = state.deadlines.map((d) =>
    d.id === id ? { ...d, progress: 100, status: 'done' } : d
  );
  saveCurrentState();
  showToast('Đã đánh dấu hoàn thành deadline', 'success');
  renderAll();
}

function reopenDeadline(id) {
  state.deadlines = state.deadlines.map((d) =>
    d.id === id ? { ...d, progress: 50, status: 'in_progress' } : d
  );
  saveCurrentState();
  renderAll();
}

function deleteDeadline(id) {
  state.deadlines = state.deadlines.filter((d) => d.id !== id);
  saveCurrentState();
  showToast('Đã xóa deadline', 'danger');
  renderAll();
}

function openDeadlineModal(editId = null) {
  const existing = editId ? state.deadlines.find((d) => d.id === editId) : null;
  const defaultDue = existing ? existing.dueDate : getRelativeDateTimeStr(2, 17, 0);

  openModal(
    existing ? 'Chỉnh sửa Deadline' : 'Thêm Deadline mới',
    `
      <form onsubmit="saveDeadlineForm(event, '${editId || ''}')">
        <div class="form-grid">
          <div class="form-group full">
            <label>Tên công việc / Deadline *</label>
            <input type="text" class="input" id="dl-title" required value="${existing ? escapeHtml(existing.title) : ''}" placeholder="VD: Nộp báo cáo, Hoàn thành dự án..." />
          </div>
          <div class="form-group">
            <label>Hạn chót (Ngày & Giờ) *</label>
            <input type="datetime-local" class="input" id="dl-due" required value="${defaultDue}" />
          </div>
          <div class="form-group">
            <label>Mức độ ưu tiên</label>
            <select class="select" id="dl-priority">
              <option value="urgent" ${existing?.priority === 'urgent' ? 'selected' : ''}>Khẩn cấp</option>
              <option value="high" ${existing?.priority === 'high' ? 'selected' : ''}>Ưu tiên cao</option>
              <option value="medium" ${!existing || existing?.priority === 'medium' ? 'selected' : ''}>Trung bình</option>
              <option value="low" ${existing?.priority === 'low' ? 'selected' : ''}>Thấp</option>
            </select>
          </div>
          <div class="form-group">
            <label>Phân nhóm</label>
            <input type="text" class="input" id="dl-category" value="${existing ? escapeHtml(existing.category) : 'Công việc'}" />
          </div>
          <div class="form-group">
            <label>Tiến độ hiện tại (%)</label>
            <input type="number" class="input" id="dl-progress" min="0" max="100" value="${existing ? existing.progress : 0}" />
          </div>
          <div class="form-group full">
            <label>Ghi chú</label>
            <input type="text" class="input" id="dl-note" value="${existing ? escapeHtml(existing.note) : ''}" />
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
          <button type="submit" class="btn btn-primary">Lưu Deadline</button>
        </div>
      </form>
    `
  );
}

function saveDeadlineForm(e, editId) {
  e.preventDefault();
  const progress = Number(document.getElementById('dl-progress').value) || 0;
  const item = {
    id: editId || 'dl-' + Date.now(),
    title: document.getElementById('dl-title').value.trim(),
    dueDate: document.getElementById('dl-due').value,
    priority: document.getElementById('dl-priority').value,
    category: document.getElementById('dl-category').value.trim() || 'Chung',
    progress,
    status: progress >= 100 ? 'done' : progress > 0 ? 'in_progress' : 'todo',
    note: document.getElementById('dl-note').value.trim()
  };

  if (editId) {
    state.deadlines = state.deadlines.map((d) => (d.id === editId ? item : d));
    showToast('Đã cập nhật deadline', 'success');
  } else {
    state.deadlines.push(item);
    showToast('Đã thêm deadline mới', 'success');
  }
  saveCurrentState();
  closeModal();
  renderAll();
}

// ============================================================================
// TAB 4: QUẢN LÝ CHI TIÊU
// ============================================================================

function populateFinanceCategoryFilter() {
  const select = document.getElementById('fin-category-filter');
  if (!select || select.options.length > 1) return;
  [...EXPENSE_CATEGORIES, ...INCOME_CATEGORIES].forEach((c) => {
    const opt = document.createElement('option');
    opt.value = c.id;
    opt.textContent = c.name;
    select.appendChild(opt);
  });
}

function setFinanceTypeFilter(type) {
  financeTypeFilter = type;
  ['all', 'expense', 'income'].forEach((t) => {
    document.getElementById(`fin-filter-${t}`).classList.toggle('active', t === type);
  });
  renderFinanceTab();
}

function renderFinanceTab() {
  populateFinanceCategoryFilter();

  const totalIncome = state.transactions
    .filter((t) => t.type === 'income')
    .reduce((sum, t) => sum + Number(t.amount), 0);
  const totalExpense = state.transactions
    .filter((t) => t.type === 'expense')
    .reduce((sum, t) => sum + Number(t.amount), 0);
  const balance = totalIncome - totalExpense;
  const budget = Number(state.monthlyBudget) || 10000000;
  const budgetPct = Math.round((totalExpense / budget) * 100);

  document.getElementById('finance-kpi-grid').innerHTML = `
    <div class="stat-card">
      <span>Tổng Thu Nhập</span>
      <h3 style="color: var(--success)">+${formatVND(totalIncome)}</h3>
      <small>Các khoản thu vào</small>
    </div>
    <div class="stat-card">
      <span>Tổng Chi Tiêu</span>
      <h3 style="color: var(--danger)">-${formatVND(totalExpense)}</h3>
      <small>Đã dùng ${budgetPct}% ngân sách tháng</small>
    </div>
    <div class="stat-card">
      <span>Số Dư Hiện Tại</span>
      <h3>${formatVND(balance)}</h3>
      <small>Chênh lệch Thu - Chi</small>
    </div>
    <div class="stat-card">
      <span>Ngân Sách Tháng</span>
      <h3>${formatVND(budget)}</h3>
      <small>Còn trong hạn mức: ${formatVND(Math.max(0, budget - totalExpense))}</small>
    </div>
  `;

  document.getElementById('finance-donut-chart').innerHTML = buildDonutChartHTML(
    getExpenseBreakdownData(),
    totalExpense
  );

  const catFilter = document.getElementById('fin-category-filter').value;
  const query = (document.getElementById('fin-search-input').value || '').toLowerCase().trim();

  let txList = [...state.transactions];
  if (financeTypeFilter !== 'all') {
    txList = txList.filter((t) => t.type === financeTypeFilter);
  }
  if (catFilter !== 'all') {
    txList = txList.filter((t) => t.category === catFilter);
  }
  if (query) {
    txList = txList.filter(
      (t) =>
        t.title.toLowerCase().includes(query) ||
        (t.note && t.note.toLowerCase().includes(query))
    );
  }
  txList.sort((a, b) => b.date.localeCompare(a.date));

  const txContainer = document.getElementById('transaction-list-container');
  if (!txList.length) {
    txContainer.innerHTML = `<div class="empty-state">Chưa có giao dịch nào phù hợp.</div>`;
  } else {
    txContainer.innerHTML = txList
      .map((tx) => {
        const meta = getCategoryMeta(tx.category);
        const isInc = tx.type === 'income';
        return `
          <div class="list-item">
            <div class="list-item-left" style="align-items: center;">
              <span class="legend-dot" style="background: ${meta.color}; width: 10px; height: 10px; flex-shrink: 0;"></span>
              <div>
                <div class="item-title">${escapeHtml(tx.title)}</div>
                <div class="item-meta">
                  <span class="badge badge-neutral">${meta.name}</span>
                  <span>Ngày: ${formatDateVN(tx.date)}</span>
                  ${tx.note ? `<span>• ${escapeHtml(tx.note)}</span>` : ''}
                </div>
              </div>
            </div>
            <div class="list-item-right">
              <strong style="color: var(--${isInc ? 'success' : 'danger'}); font-size: 0.94rem; margin-right: 6px;">
                ${isInc ? '+' : '-'}${formatVND(tx.amount)}
              </strong>
              <button class="btn btn-xs btn-secondary" onclick="openTransactionModal('${tx.type}', '${tx.id}')">Sửa</button>
              <button class="btn btn-xs btn-danger" onclick="deleteTransaction('${tx.id}')">Xóa</button>
            </div>
          </div>
        `;
      })
      .join('');
  }
}

function openTransactionModal(defaultType = 'expense', editId = null) {
  const existing = editId ? state.transactions.find((t) => t.id === editId) : null;
  const currentType = existing ? existing.type : defaultType;
  const catOptions = [...EXPENSE_CATEGORIES, ...INCOME_CATEGORIES]
    .map((c) => `<option value="${c.id}" ${existing?.category === c.id ? 'selected' : ''}>${c.name}</option>`)
    .join('');

  openModal(
    existing ? 'Chỉnh sửa khoản Thu / Chi' : 'Ghi nhận khoản Thu / Chi',
    `
      <form onsubmit="saveTransactionForm(event, '${editId || ''}')">
        <div class="form-grid">
          <div class="form-group">
            <label>Loại giao dịch *</label>
            <select class="select" id="tx-type">
              <option value="expense" ${currentType === 'expense' ? 'selected' : ''}>Khoản Chi tiêu</option>
              <option value="income" ${currentType === 'income' ? 'selected' : ''}>Khoản Thu nhập</option>
            </select>
          </div>
          <div class="form-group">
            <label>Số tiền (VNĐ) *</label>
            <input type="number" class="input" id="tx-amount" required min="1000" step="1000" value="${existing ? existing.amount : ''}" placeholder="VD: 150000" />
          </div>
          <div class="form-group full">
            <label>Nội dung *</label>
            <input type="text" class="input" id="tx-title" required value="${existing ? escapeHtml(existing.title) : ''}" placeholder="VD: Tiền ăn trưa, Đổ xăng, Nhận lương..." />
          </div>
          <div class="form-group">
            <label>Danh mục</label>
            <select class="select" id="tx-category">${catOptions}</select>
          </div>
          <div class="form-group">
            <label>Ngày *</label>
            <input type="date" class="input" id="tx-date" required value="${existing ? existing.date : getRelativeDateStr(0)}" />
          </div>
          <div class="form-group full">
            <label>Ghi chú</label>
            <input type="text" class="input" id="tx-note" value="${existing ? escapeHtml(existing.note) : ''}" />
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
          <button type="submit" class="btn btn-primary">Lưu giao dịch</button>
        </div>
      </form>
    `
  );
}

function saveTransactionForm(e, editId = '') {
  e.preventDefault();
  const tx = {
    ...(editId ? state.transactions.find(item => item.id === editId) : {}),
    id: editId || 'tx-' + Date.now(),
    type: document.getElementById('tx-type').value,
    amount: Number(document.getElementById('tx-amount').value),
    title: document.getElementById('tx-title').value.trim(),
    category: document.getElementById('tx-category').value,
    date: document.getElementById('tx-date').value,
    note: document.getElementById('tx-note').value.trim()
  };

  if (editId) {
    state.transactions = state.transactions.map((t) => (t.id === editId ? tx : t));
    showToast('Đã cập nhật khoản thu/chi', 'success');
  } else {
    state.transactions.unshift(tx);
    showToast('Đã lưu khoản thu/chi mới', 'success');
  }
  saveCurrentState();
  closeModal();
  renderAll();
}

function deleteTransaction(id) {
  state.transactions = state.transactions.filter((t) => t.id !== id);
  saveCurrentState();
  showToast('Đã xóa giao dịch', 'danger');
  renderAll();
}

function openBudgetModal() {
  openModal(
    'Đặt hạn mức ngân sách chi tiêu tháng',
    `
      <form onsubmit="saveBudgetForm(event)">
        <div class="form-group">
          <label>Hạn mức chi tiêu tối đa trong tháng (VNĐ)</label>
          <input type="number" class="input" id="budget-input" required min="500000" step="500000" value="${state.monthlyBudget || 12000000}" />
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
          <button type="submit" class="btn btn-primary">Lưu hạn mức</button>
        </div>
      </form>
    `
  );
}

function saveBudgetForm(e) {
  e.preventDefault();
  state.monthlyBudget = Number(document.getElementById('budget-input').value) || 12000000;
  saveCurrentState();
  closeModal();
  showToast('Đã cập nhật ngân sách tháng', 'success');
  renderAll();
}

// ============================================================================
// TAB 5: QUẢN LÝ NỢ & ƯU TIÊN TRẢ NỢ THEO THỜI GIAN
// ============================================================================

function setDebtDirectionFilter(dir) {
  debtDirectionFilter = dir;
  ['borrowed', 'lent', 'all'].forEach((d) => {
    document.getElementById(`debt-dir-${d}`).classList.toggle('active', d === dir);
  });
  renderDebtsTab();
}

function renderDebtsTab() {
  const borrowed = state.debts.filter((d) => d.direction === 'borrowed');
  const lent = state.debts.filter((d) => d.direction === 'lent');

  const totalBorrowedOrigin = borrowed.reduce((s, d) => s + Number(d.totalAmount), 0);
  const totalBorrowedPaid = borrowed.reduce((s, d) => s + Number(d.paidAmount), 0);
  const totalBorrowedRem = borrowed.reduce((s, d) => s + getRemainingDebt(d), 0);
  const totalLentRem = lent.reduce((s, d) => s + getRemainingDebt(d), 0);
  const urgentBorrowedCount = borrowed.filter(
    (d) => getRemainingDebt(d) > 0 && getDaysRemaining(d.dueDate) <= 3
  ).length;

  document.getElementById('debt-summary-stats').innerHTML = `
    <div class="stat-card">
      <span>Tổng Nợ Phải Trả (Còn lại)</span>
      <h3 style="color: var(--danger)">${formatVND(totalBorrowedRem)}</h3>
      <small>Đã trả: ${formatVND(totalBorrowedPaid)} / ${formatVND(totalBorrowedOrigin)}</small>
    </div>
    <div class="stat-card">
      <span>Khoản Nợ Quá Hạn / Sát Hạn</span>
      <h3 style="color: var(--warning)">${urgentBorrowedCount} khoản</h3>
      <small>Cần ưu tiên trả trong 3 ngày tới</small>
    </div>
    <div class="stat-card">
      <span>Người Khác Nợ Mình (Cần thu)</span>
      <h3 style="color: var(--success)">${formatVND(totalLentRem)}</h3>
      <small>${lent.filter((d) => getRemainingDebt(d) > 0).length} khoản chưa thu hết</small>
    </div>
    <div class="stat-card">
      <span>Cân Đối Công Nợ Ròng</span>
      <h3>${formatVND(totalLentRem - totalBorrowedRem)}</h3>
      <small>Cho vay trừ Đi vay</small>
    </div>
  `;

  renderDebtPayoffPlan();

  const strategy = document.getElementById('debt-strategy-select').value;
  let filtered = [...state.debts];
  if (debtDirectionFilter !== 'all') {
    filtered = filtered.filter((d) => d.direction === debtDirectionFilter);
  }

  const sorted = sortDebtsByStrategy(filtered, strategy);
  const container = document.getElementById('debt-list-container');

  if (!sorted.length) {
    container.innerHTML = `<div class="empty-state">Không có khoản nợ nào trong mục này.</div>`;
    return;
  }

  container.innerHTML = sorted
    .map((debt, index) => {
      const rem = getRemainingDebt(debt);
      const pctPaid = debt.totalAmount > 0 ? Math.min(100, Math.round((debt.paidAmount / debt.totalAmount) * 100)) : 100;
      const days = getDaysRemaining(debt.dueDate);
      const isBorrowed = debt.direction === 'borrowed';
      const statusClass = rem === 0 ? '' : days < 0 ? 'overdue' : days <= 3 ? 'due-soon' : '';

      let timeBadge = '';
      if (rem === 0) {
        timeBadge = `<span class="badge badge-success">Đã tất toán</span>`;
      } else if (days < 0) {
        timeBadge = `<span class="badge badge-danger">Quá hạn ${Math.abs(days)} ngày (${formatDateVN(debt.dueDate)})</span>`;
      } else if (days === 0) {
        timeBadge = `<span class="badge badge-danger">Đến hạn hôm nay (${formatDateVN(debt.dueDate)})</span>`;
      } else if (days <= 3) {
        timeBadge = `<span class="badge badge-warning">Còn ${days} ngày (${formatDateVN(debt.dueDate)})</span>`;
      } else {
        timeBadge = `<span class="badge badge-info">Còn ${days} ngày (${formatDateVN(debt.dueDate)})</span>`;
      }

      const historyHTML =
        debt.history && debt.history.length
          ? `<div style="margin-top: 6px; font-size: 0.75rem; color: var(--text-muted);">
               Lịch sử thanh toán: ${debt.history
                 .map((h) => `${formatDateVN(h.date)}: +${formatVND(h.amount)}`)
                 .join(' • ')}
             </div>`
          : '';

      return `
        <div class="debt-priority-card ${statusClass}" style="${rem === 0 ? 'opacity: 0.6;' : ''}">
          <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap;">
            <div style="display: flex; align-items: flex-start; gap: 12px;">
              <span class="debt-rank-number">${index + 1}</span>
              <div>
                <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                  <span class="badge ${isBorrowed ? 'badge-danger' : 'badge-success'}">
                    ${isBorrowed ? 'Mình nợ' : 'Cho vay'}
                  </span>
                  <span class="item-title" style="font-size: 0.96rem;">${escapeHtml(debt.person)}</span>
                  ${getPriorityBadge(debt.priorityLevel)}
                  ${debt.interestRate > 0 ? `<span class="badge badge-neutral">Lãi: ${debt.interestRate}%/năm</span>` : ''}
                </div>
                <div style="font-size: 0.84rem; color: var(--text-secondary); margin-top: 3px;">
                  ${escapeHtml(debt.title)}
                </div>
              </div>
            </div>

            <div style="text-align: right;">
              <div style="font-size: 0.74rem; color: var(--text-secondary);">Còn lại cần ${isBorrowed ? 'trả' : 'thu'}:</div>
              <div style="font-size: 1.12rem; font-weight: 700; color: var(--${isBorrowed ? 'danger' : 'success'});">
                ${formatVND(rem)}
              </div>
              <div style="font-size: 0.74rem; color: var(--text-muted);">
                Đã ${isBorrowed ? 'trả' : 'thu'}: ${formatVND(debt.paidAmount)} / ${formatVND(debt.totalAmount)} (${pctPaid}%)
              </div>
            </div>
          </div>

          <div class="progress-wrap" style="margin: 2px 0;">
            <div class="progress-bar ${pctPaid >= 100 ? 'green' : isBorrowed ? 'red' : 'indigo'}" style="width: ${pctPaid}%"></div>
          </div>

          <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px;">
            <div>
              <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                ${timeBadge}
                ${debt.note ? `<span style="font-size: 0.78rem; color: var(--text-secondary);">• ${escapeHtml(debt.note)}</span>` : ''}
              </div>
              ${historyHTML}
            </div>

            <div style="display: flex; gap: 6px; align-items: center;">
              ${
                rem > 0
                  ? `<button class="btn btn-xs btn-primary" onclick="openPayDebtModal('${debt.id}')">
                       ${isBorrowed ? 'Thanh toán nợ' : 'Thu hồi nợ'}
                     </button>`
                  : ''
              }
              <button class="btn btn-xs btn-secondary" onclick="openDebtModal('${debt.id}')">Sửa</button>
              <button class="btn btn-xs btn-danger" onclick="deleteDebt('${debt.id}')">Xóa</button>
            </div>
          </div>
        </div>
      `;
    })
    .join('');
}

function renderDebtPayoffPlan() {
  const budgetInput = document.getElementById('debt-payoff-budget-input');
  const resultEl = document.getElementById('debt-payoff-plan-result');
  if (!budgetInput || !resultEl) return;

  let availableCash = Number(budgetInput.value) || 0;
  const strategy = document.getElementById('debt-strategy-select').value;
  const activeBorrowed = sortDebtsByStrategy(
    state.debts.filter((d) => d.direction === 'borrowed' && getRemainingDebt(d) > 0),
    strategy
  );

  if (!activeBorrowed.length) {
    resultEl.innerHTML = `<div style="color: var(--success); font-size: 0.84rem;">Bạn không còn khoản nợ nào cần phân bổ.</div>`;
    return;
  }

  if (availableCash <= 0) {
    resultEl.innerHTML = `<div style="color: var(--text-secondary); font-size: 0.84rem;">Nhập số tiền dự kiến trả nợ để xem thứ tự phân bổ.</div>`;
    return;
  }

  const allocations = [];
  for (const debt of activeBorrowed) {
    if (availableCash <= 0) break;
    const rem = getRemainingDebt(debt);
    const payNow = Math.min(rem, availableCash);
    availableCash -= payNow;
    allocations.push({
      debt,
      payNow,
      leftAfter: rem - payNow,
      isFullPayoff: payNow >= rem
    });
  }

  resultEl.innerHTML = `
    <div style="background: var(--bg-primary); border: 1px solid var(--border-color); border-radius: var(--radius-sm); padding: 12px 16px;">
      <div style="display: flex; flex-direction: column; gap: 8px;">
        ${allocations
          .map(
            (item, idx) => `
            <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.84rem; padding: 6px 0; border-bottom: 1px solid var(--border-color); flex-wrap: wrap; gap: 8px;">
              <div>
                <strong>Ưu tiên ${idx + 1}:</strong> Trả cho <strong>${escapeHtml(item.debt.person)}</strong> (${escapeHtml(item.debt.title)})
                ${
                  item.isFullPayoff
                    ? '<span class="badge badge-success" style="margin-left: 6px;">Tất toán dứt điểm</span>'
                    : `<span class="badge badge-warning" style="margin-left: 6px;">Trả 1 phần (Còn lại ${formatVND(item.leftAfter)})</span>`
                }
              </div>
              <div style="display: flex; align-items: center; gap: 10px;">
                <strong style="color: var(--success);">${formatVND(item.payNow)}</strong>
                <button class="btn btn-xs btn-secondary" onclick="openPayDebtModal('${item.debt.id}', ${item.payNow})">Áp dụng trả</button>
              </div>
            </div>
          `
          )
          .join('')}
      </div>
    </div>
  `;
}

function openPayDebtModal(debtId, suggestedAmount = null) {
  const debt = state.debts.find((d) => d.id === debtId);
  if (!debt) return;
  const rem = getRemainingDebt(debt);
  const defaultPay = suggestedAmount ? Math.min(rem, suggestedAmount) : rem;
  const isBorrowed = debt.direction === 'borrowed';

  openModal(
    `${isBorrowed ? 'Thanh toán khoản nợ' : 'Thu hồi khoản cho vay'}: ${escapeHtml(debt.person)}`,
    `
      <form onsubmit="confirmPayDebtForm(event, '${debtId}')">
        <div style="background: var(--bg-primary); padding: 12px; border-radius: var(--radius-sm); margin-bottom: 14px; font-size: 0.84rem; border: 1px solid var(--border-color);">
          <div>Nội dung: <strong>${escapeHtml(debt.title)}</strong></div>
          <div>Tổng gốc: <strong>${formatVND(debt.totalAmount)}</strong> • Đã ${isBorrowed ? 'trả' : 'thu'}: <strong>${formatVND(debt.paidAmount)}</strong></div>
          <div style="margin-top: 4px;">Số tiền còn lại: <strong style="color: var(--danger);">${formatVND(rem)}</strong></div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Số tiền ${isBorrowed ? 'trả' : 'thu'} đợt này (VNĐ) *</label>
            <input type="number" class="input" id="pay-debt-amount" required min="1000" max="${rem}" step="1000" value="${defaultPay}" />
          </div>
          <div class="form-group">
            <label>Ngày thực hiện *</label>
            <input type="date" class="input" id="pay-debt-date" required value="${getRelativeDateStr(0)}" />
          </div>
          <div class="form-group full">
            <label>Ghi chú</label>
            <input type="text" class="input" id="pay-debt-note" placeholder="VD: Chuyển khoản đợt 2..." />
          </div>
          <div class="form-group full">
            <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; color: var(--text-primary);">
              <input type="checkbox" id="pay-debt-sync-tx" checked />
              <span>Tự động ghi vào Sổ Thu Chi (${isBorrowed ? 'Khoản Chi trả nợ' : 'Khoản Thu hồi nợ'})</span>
            </label>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
          <button type="submit" class="btn btn-primary">Xác nhận</button>
        </div>
      </form>
    `
  );
}

function confirmPayDebtForm(e, debtId) {
  e.preventDefault();
  const amount = Number(document.getElementById('pay-debt-amount').value) || 0;
  const date = document.getElementById('pay-debt-date').value;
  const note = document.getElementById('pay-debt-note').value.trim();
  const syncTx = document.getElementById('pay-debt-sync-tx').checked;

  const targetDebt = state.debts.find((d) => d.id === debtId);
  if (!targetDebt) return;

  targetDebt.paidAmount = Math.min(targetDebt.totalAmount, Number(targetDebt.paidAmount) + amount);
  if (!Array.isArray(targetDebt.history)) targetDebt.history = [];
  targetDebt.history.push({ date, amount, note });

  if (syncTx) {
    const isBorrowed = targetDebt.direction === 'borrowed';
    state.transactions.unshift({
      id: 'tx-' + Date.now(),
      type: isBorrowed ? 'expense' : 'income',
      amount,
      category: isBorrowed ? 'debt_pay' : 'debt_collect',
      title: `${isBorrowed ? 'Trả nợ' : 'Thu nợ'}: ${targetDebt.person} (${targetDebt.title})`,
      date,
      note: note || 'Đồng bộ từ mục Quản lý Nợ'
    });
  }

  saveCurrentState();
  closeModal();
  showToast(`Đã ghi nhận thanh toán ${formatVND(amount)}`, 'success');
  renderAll();
}

function openDebtModal(editId = null) {
  const existing = editId ? state.debts.find((d) => d.id === editId) : null;
  openModal(
    existing ? 'Chỉnh sửa khoản Nợ / Cho vay' : 'Thêm khoản Nợ / Cho vay mới',
    `
      <form onsubmit="saveDebtForm(event, '${editId || ''}')">
        <div class="form-grid">
          <div class="form-group">
            <label>Loại công nợ *</label>
            <select class="select" id="debt-direction">
              <option value="borrowed" ${!existing || existing?.direction === 'borrowed' ? 'selected' : ''}>Khoản mình nợ (Cần trả)</option>
              <option value="lent" ${existing?.direction === 'lent' ? 'selected' : ''}>Người khác nợ mình (Cần thu)</option>
            </select>
          </div>
          <div class="form-group">
            <label>Chủ nợ / Người vay *</label>
            <input type="text" class="input" id="debt-person" required value="${existing ? escapeHtml(existing.person) : ''}" placeholder="VD: Anh Minh, Ngân hàng..." />
          </div>
          <div class="form-group full">
            <label>Nội dung khoản nợ *</label>
            <input type="text" class="input" id="debt-title" required value="${existing ? escapeHtml(existing.title) : ''}" placeholder="VD: Vay mua thiết bị, Mượn tiền cọc..." />
          </div>
          <div class="form-group">
            <label>Tổng số tiền gốc (VNĐ) *</label>
            <input type="number" class="input" id="debt-total" required min="1000" step="10000" value="${existing ? existing.totalAmount : ''}" />
          </div>
          <div class="form-group">
            <label>Số tiền đã trả/thu trước đó (VNĐ)</label>
            <input type="number" class="input" id="debt-paid" min="0" step="10000" value="${existing ? existing.paidAmount : 0}" />
          </div>
          <div class="form-group">
            <label>Ngày hẹn trả / Đáo hạn *</label>
            <input type="date" class="input" id="debt-due" required value="${existing ? existing.dueDate : getRelativeDateStr(7)}" />
          </div>
          <div class="form-group">
            <label>Mức độ ưu tiên</label>
            <select class="select" id="debt-priority">
              <option value="urgent" ${existing?.priorityLevel === 'urgent' ? 'selected' : ''}>Khẩn cấp</option>
              <option value="high" ${!existing || existing?.priorityLevel === 'high' ? 'selected' : ''}>Ưu tiên cao</option>
              <option value="medium" ${existing?.priorityLevel === 'medium' ? 'selected' : ''}>Trung bình</option>
              <option value="low" ${existing?.priorityLevel === 'low' ? 'selected' : ''}>Thấp</option>
            </select>
          </div>
          <div class="form-group">
            <label>Lãi suất (%/năm - Nhập 0 nếu không lãi)</label>
            <input type="number" class="input" id="debt-rate" min="0" max="100" step="0.5" value="${existing ? existing.interestRate : 0}" />
          </div>
          <div class="form-group">
            <label>Ghi chú</label>
            <input type="text" class="input" id="debt-note" value="${existing ? escapeHtml(existing.note) : ''}" />
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
          <button type="submit" class="btn btn-primary">Lưu khoản nợ</button>
        </div>
      </form>
    `
  );
}

function saveDebtForm(e, editId) {
  e.preventDefault();
  const existing = editId ? state.debts.find((d) => d.id === editId) : null;
  const item = {
    id: editId || 'debt-' + Date.now(),
    direction: document.getElementById('debt-direction').value,
    person: document.getElementById('debt-person').value.trim(),
    title: document.getElementById('debt-title').value.trim(),
    totalAmount: Number(document.getElementById('debt-total').value),
    paidAmount: Number(document.getElementById('debt-paid').value) || 0,
    dueDate: document.getElementById('debt-due').value,
    priorityLevel: document.getElementById('debt-priority').value,
    interestRate: Number(document.getElementById('debt-rate').value) || 0,
    note: document.getElementById('debt-note').value.trim(),
    history: existing?.history || []
  };

  if (editId) {
    state.debts = state.debts.map((d) => (d.id === editId ? item : d));
    showToast('Đã cập nhật khoản nợ', 'success');
  } else {
    state.debts.push(item);
    showToast('Đã thêm khoản nợ mới', 'success');
  }
  saveCurrentState();
  closeModal();
  renderAll();
}

function deleteDebt(id) {
  state.debts = state.debts.filter((d) => d.id !== id);
  saveCurrentState();
  showToast('Đã xóa khoản nợ', 'danger');
  renderAll();
}

// ============================================================================
// TAB 6: SỔ GHI CHÚ THUẦN TÚY (PURE TEXT NOTES)
// ============================================================================

function renderNotesTab() {
  const query = (document.getElementById('note-search-input')?.value || '').toLowerCase().trim();
  let notes = [...state.notes];

  if (query) {
    notes = notes.filter(
      (n) =>
        (n.title || '').toLowerCase().includes(query) ||
        (n.content || '').toLowerCase().includes(query)
    );
  }

  const listEl = document.getElementById('notes-sidebar-list');
  const titleInput = document.getElementById('active-note-title');
  const contentInput = document.getElementById('active-note-content');
  const statusEl = document.getElementById('active-note-status');

  if (!notes.length) {
    listEl.innerHTML = `<div class="empty-state">Chưa có ghi chú nào.</div>`;
    titleInput.value = '';
    contentInput.value = '';
    statusEl.textContent = 'Bấm "+ Tạo ghi chú mới" để bắt đầu viết';
    return;
  }

  if (!activeNoteId || !notes.some((n) => n.id === activeNoteId)) {
    activeNoteId = notes[0].id;
  }

  listEl.innerHTML = notes
    .map(
      (n) => `
      <div class="pure-note-item ${n.id === activeNoteId ? 'active' : ''}" onclick="selectPureNote('${n.id}')">
        <h4>${escapeHtml(n.title || 'Ghi chú không tiêu đề')}</h4>
        <p>${escapeHtml(n.content || 'Chưa có nội dung...')}</p>
        <small>Cập nhật: ${formatDateVN(n.updatedAt)}</small>
      </div>
    `
    )
    .join('');

  const currentNote = state.notes.find((n) => n.id === activeNoteId);
  if (currentNote) {
    if (document.activeElement !== titleInput) {
      titleInput.value = currentNote.title || '';
    }
    if (document.activeElement !== contentInput) {
      contentInput.value = currentNote.content || '';
    }
    statusEl.textContent = `Cập nhật lần cuối: ${formatDateVN(currentNote.updatedAt)}`;
  }
}

function selectPureNote(id) {
  activeNoteId = id;
  const currentNote = state.notes.find((n) => n.id === activeNoteId);
  if (currentNote) {
    document.getElementById('active-note-title').value = currentNote.title || '';
    document.getElementById('active-note-content').value = currentNote.content || '';
  }
  renderNotesTab();
}

function createNewPureNote() {
  const newNote = {
    id: 'note-' + Date.now(),
    title: 'Ghi chú mới',
    content: '',
    updatedAt: getRelativeDateStr(0)
  };
  state.notes.unshift(newNote);
  activeNoteId = newNote.id;
  saveCurrentState();
  renderAll();
  const titleEl = document.getElementById('active-note-title');
  titleEl.value = newNote.title;
  document.getElementById('active-note-content').value = '';
  titleEl.focus();
  titleEl.select();
}

function autoSaveActiveNote() {
  if (!activeNoteId) {
    createNewPureNote();
    return;
  }
  const titleVal = document.getElementById('active-note-title').value;
  const contentVal = document.getElementById('active-note-content').value;
  const today = getRelativeDateStr(0);

  state.notes = state.notes.map((n) =>
    n.id === activeNoteId ? { ...n, title: titleVal, content: contentVal, updatedAt: today } : n
  );
  saveCurrentState();
  updateSidebarBadges();

  // Cập nhật danh sách bên trái mà không làm mất con trỏ đang gõ
  const query = (document.getElementById('note-search-input')?.value || '').toLowerCase().trim();
  const notes = state.notes.filter(
    (n) =>
      !query ||
      (n.title || '').toLowerCase().includes(query) ||
      (n.content || '').toLowerCase().includes(query)
  );
  document.getElementById('notes-sidebar-list').innerHTML = notes
    .map(
      (n) => `
      <div class="pure-note-item ${n.id === activeNoteId ? 'active' : ''}" onclick="selectPureNote('${n.id}')">
        <h4>${escapeHtml(n.title || 'Ghi chú không tiêu đề')}</h4>
        <p>${escapeHtml(n.content || 'Chưa có nội dung...')}</p>
        <small>Cập nhật: ${formatDateVN(n.updatedAt)}</small>
      </div>
    `
    )
    .join('');
  document.getElementById('active-note-status').textContent = 'Đã tự động lưu';
}

function manualSaveActiveNote() {
  autoSaveActiveNote();
  showToast('Đã lưu ghi chú', 'success');
}

function deleteActiveNote() {
  if (!activeNoteId) return;
  state.notes = state.notes.filter((n) => n.id !== activeNoteId);
  activeNoteId = state.notes[0]?.id || null;
  saveCurrentState();
  showToast('Đã xóa ghi chú', 'danger');
  renderAll();
}

// ============================================================================
// TAB 7: SAO LƯU & CÀI ĐẶT
// ============================================================================

function exportBackupJSON() {
  const blob = new Blob([JSON.stringify(state, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `SaoLuu_QuanLyCaNhan_${currentUser?.username || 'user'}_${getRelativeDateStr(0)}.json`;
  a.click();
  URL.revokeObjectURL(url);
  showToast('Đã tải xuống file sao lưu JSON', 'success');
}

function importBackupJSON(event) {
  const file = event.target.files[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const imported = JSON.parse(e.target.result);
      state = { ...createSampleState(), ...imported };
      saveCurrentState();
      applyTheme();
      renderAll();
      showToast('Đã khôi phục dữ liệu thành công', 'success');
    } catch (err) {
      showToast('File JSON không hợp lệ', 'danger');
    }
  };
  reader.readAsText(file);
}

function exportTransactionsCSV() {
  const rows = [['Loai', 'Ngay', 'So_Tien_VND', 'Danh_Muc', 'Noi_Dung', 'Ghi_Chu']];
  state.transactions.forEach((t) => {
    const meta = getCategoryMeta(t.category);
    rows.push([
      t.type === 'income' ? 'Thu nhap' : 'Chi tieu',
      t.date,
      t.amount,
      `"${meta.name}"`,
      `"${(t.title || '').replace(/"/g, '""')}"`,
      `"${(t.note || '').replace(/"/g, '""')}"`
    ]);
  });

  const csvContent = '\uFEFF' + rows.map((r) => r.join(',')).join('\n');
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `So_Thu_Chi_${getRelativeDateStr(0)}.csv`;
  a.click();
  URL.revokeObjectURL(url);
  showToast('Đã xuất file CSV', 'success');
}

function handleResetSampleData() {
  const appearance = { theme: state.theme, accentColor: state.accentColor, bankSeenIds: state.bankSeenIds || [] };
  state = createSampleState();
  Object.assign(state, appearance);
  activeNoteId = state.notes[0]?.id || null;
  saveCurrentState();
  applyTheme();
  renderAll();
  showToast('Đã nạp lại dữ liệu mẫu', 'info');
}

function handleClearAllData() {
  state = {
    theme: state.theme,
    accentColor: state.accentColor,
    bankSeenIds: state.bankSeenIds || [],
    monthlyBudget: 10000000,
    schedules: [],
    deadlines: [],
    transactions: [],
    debts: [],
    notes: []
  };
  activeNoteId = null;
  saveCurrentState();
  renderAll();
  showToast('Đã xóa trắng toàn bộ dữ liệu mẫu', 'warning');
}

// ============================================================================
// MODAL HELPERS & KHỞI TẠO
// ============================================================================

function openModal(title, bodyHTML) {
  document.getElementById('modal-title').textContent = title;
  document.getElementById('modal-body').innerHTML = bodyHTML;
  document.getElementById('modal-backdrop').classList.add('open');
}

function closeModal() {
  document.getElementById('modal-backdrop').classList.remove('open');
}

function handleBackdropClick(e) {
  if (e.target.id === 'modal-backdrop') {
    closeModal();
  }
}

function renderAll() {
  if (!currentUser) return;
  updateSidebarBadges();
  renderDashboard();
  renderScheduleTab();
  renderDeadlinesTab();
  renderFinanceTab();
  renderDebtsTab();
  renderNotesTab();
}

function decorateWorkspace() {
  const paths = {
    dashboard: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
    schedule: '<rect x="3" y="5" width="18" height="16" rx="3"/><path d="M16 3v4M8 3v4M3 11h18m-13 5h2m4 0h2"/>',
    deadlines: '<circle cx="12" cy="13" r="8"/><path d="M12 9v4l3 2M9 2h6m3 3 2-2"/>',
    finance: '<rect x="3" y="5" width="18" height="15" rx="3"/><path d="M3 9h18m-6 5h6M7 5V3h11"/>',
    debts: '<path d="M4 7h15l-4-4m5 14H5l4 4M4 7v5m16 5v-5"/>',
    notes: '<path d="M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9zM14 3v6h6M8 13h8m-8 4h5"/>',
    settings: '<path d="M4 7h16M4 17h16"/><circle cx="9" cy="7" r="3"/><circle cx="15" cy="17" r="3"/>'
  };
  const icon = (path, className) => `<svg class="${className}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${path}</svg>`;
  document.querySelectorAll('.nav-item').forEach(button => button.insertAdjacentHTML('afterbegin', icon(paths[button.dataset.tab], 'nav-icon')));
  document.querySelectorAll('#tab-dashboard .stat-card').forEach((card, index) => card.insertAdjacentHTML('afterbegin', icon(paths[['finance', 'debts', 'deadlines', 'schedule'][index]], 'stat-icon')));
}

document.addEventListener('DOMContentLoaded', () => {
  loadUsersList(); // Khởi tạo tài khoản mặc định nếu chưa có
  decorateWorkspace();
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
      closeModal();
      document.getElementById('sidebar').classList.remove('open');
      syncSidebar();
    }
  });

  document.querySelectorAll('.nav-item').forEach((btn) => {
    btn.addEventListener('click', () => switchTab(btn.dataset.tab));
  });

  updateAuthUI();
  applyTheme();
  startLiveClock();
  if (currentUser) {
    activeNoteId = state.notes[0]?.id || null;
    renderAll();
  }
});
