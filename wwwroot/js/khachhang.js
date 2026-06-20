// KHÁCH HÀNG - SQL/API ONLY
// Không lưu dữ liệu nghiệp vụ trên trình duyệt. Toàn bộ dữ liệu lấy qua API -> Backend -> SQL Server.

const SESSION_KEY = 'KKTH_ACTIVE_USER';
const TOKEN_KEY = 'JWT_TOKEN';
const user = JSON.parse(sessionStorage.getItem(SESSION_KEY) || 'null');
const token = sessionStorage.getItem(TOKEN_KEY) || '';

if (!token || !user || user.role !== 'customer') {
    window.location.href = 'login.html';
}

const state = {
    services: [],
    cars: [],
    bookings: [],
    orders: [],
    cart: [],
    qr: null
};

let selectedBookingType = 'Xem xe tại showroom';
let cachedBookingServices = [];
let selectedBookingServiceId = '';
const BOOKING_TYPE_SHOWROOM = 'Xem xe tại showroom';
const BOOKING_TYPE_REPAIR = 'Bảo dưỡng định kỳ / sửa chữa';

function authHeaders(extra = {}) {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + token,
        ...extra
    };
}

async function apiFetch(url, options = {}) {
    const res = await fetch(url, {
        cache: 'no-store',
        ...options,
        headers: authHeaders(options.headers || {})
    });

    const json = await res.json().catch(() => ({}));

    if (res.status === 401 || res.status === 403) {
        sessionStorage.clear();
        alert('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
        window.location.href = 'login.html';
        throw new Error('Unauthorized');
    }

    if (!res.ok || json.success === false) {
        throw new Error(json.message || json.error || ('API lỗi: ' + url));
    }

    return json.data || json.value || json;
}

async function apiGetList(url) {
    const data = await apiFetch(url);
    return Array.isArray(data) ? data : (Array.isArray(data.data) ? data.data : []);
}

function displayCustomerAccount(value) {
    const raw = String(value || '').trim();
    const fakeEmail = raw.match(/^(0\d{9,10})@khachhang\.com$/i);
    return fakeEmail ? fakeEmail[1] : raw;
}

function getCurrentUserAccount() {
    return displayCustomerAccount(user.phoneNumber || user.phone || user.user || user.email || '');
}

function getCurrentUserEmail() {
    return String(user.email || user.user || '').trim().toLowerCase();
}

function getCurrentUserPhone() {
    return String(user.phoneNumber || user.phone || '').trim();
}

function normalizeText(value) {
    return displayCustomerAccount(value || '').trim().toLowerCase().replace(/\s+/g, '');
}

function isMine(record) {
    if (!record) return false;

    const myId = String(user.id || user.Id || '').trim();
    const myEmail = normalizeText(getCurrentUserEmail());
    const myPhone = normalizeText(getCurrentUserPhone());
    const myAccount = normalizeText(getCurrentUserAccount());
    const myName = normalizeText(user.name || user.fullName || '');

    const ids = [record.customerId, record.CustomerId, record.idCustomer].map(x => String(x || '').trim());
    if (myId && ids.includes(myId)) return true;

    const text = normalizeText([
        record.customerAccount, record.CustomerAccount,
        record.customerEmail, record.CustomerEmail,
        record.email, record.Email,
        record.customerPhone, record.CustomerPhone,
        record.phoneNumber, record.PhoneNumber,
        record.customerName, record.CustomerName,
        record.fullName, record.FullName,
        record.name, record.Name,
        record.note, record.Note
    ].join(' '));

    return Boolean(
        (myEmail && text.includes(myEmail)) ||
        (myPhone && text.includes(myPhone)) ||
        (myAccount && text.includes(myAccount)) ||
        (myName && text.includes(myName))
    );
}

function formatMoney(num) {
    const n = Number(String(num || 0).replace(/[^0-9.-]/g, '')) || 0;
    return n.toLocaleString('vi-VN') + 'đ';
}

function parseMoney(num) {
    return Number(String(num || 0).replace(/[^0-9.-]/g, '')) || 0;
}

function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.innerText = value;
}

function setValue(id, value) {
    const el = document.getElementById(id);
    if (el) el.value = value || '';
}

function syncUserInfo() {
    const name = user.name || user.fullName || 'Khách hàng';
    const account = getCurrentUserEmail() || getCurrentUserPhone() || getCurrentUserAccount();

    setText('nav-name', name);
    setText('welcome-msg', `Xin chào, ${name}!`);
    setText('prof-fullname', name);
    setValue('prof-display-name', name);
    setValue('prof-user', account);
    setValue('prof-phone', getCurrentUserPhone());

    const emailInput = document.getElementById('prof-email');
    if (emailInput) emailInput.value = getCurrentUserEmail();
}

function logoutCustomer() {
    if (confirm('Bạn muốn đăng xuất khỏi tài khoản?')) {
        fetch('/api/auth/logout', { method: 'POST', headers: authHeaders() }).catch(() => {});
        sessionStorage.clear();
        window.location.href = 'login.html';
    }
}

function nav(id) {
    const section = document.getElementById(id);
    const button = document.getElementById('btn-' + id);
    if (!section) return alert('Không tìm thấy màn hình: ' + id);

    document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('nav button').forEach(b => b.classList.remove('active'));
    section.classList.add('active');
    if (button) button.classList.add('active');
    window.scrollTo(0, 0);

    if (id === 'mycars') loadMyCars();
    if (id === 'book') {
        refreshBookingOptions();
        loadMyBookings();
    }
    if (id === 'order') loadOrders();
    if (id === 'profile') syncUserInfo();
    updateStats();
}

function normalizeService(item, index) {
    const id = item.serviceId || item.ServiceId || item.id || item.Id || ('DV_' + index);
    const name = item.serviceName || item.ServiceName || item.name || item.Name || item.f1 || 'Dịch vụ sửa chữa';
    const code = item.serviceCode || item.ServiceCode || item.code || item.Code || item.f2 || id;
    const price = item.price ?? item.Price ?? item.f3 ?? 0;
    return {
        id,
        name: String(name),
        code: String(code),
        price: parseMoney(price),
        desc: item.description || item.Description || item.note || item.f4 || 'Dịch vụ sửa chữa, bảo dưỡng và chăm sóc xe tại garage.',
        image: item.image || item.imageUrl || item.ImageUrl || ''
    };
}

async function loadServices() {
    const data = await apiGetList('/api/Services');
    state.services = data.map(normalizeService);
    cachedBookingServices = state.services;
    renderShowroom();
    populateBookingServiceSelect(state.services);
}

function renderShowroom() {
    const box = document.getElementById('showroom-list');
    if (!box) return;

    if (!state.services.length) {
        box.innerHTML = '<p style="color:var(--text-muted);">Chưa có dịch vụ nào. Admin cần thêm dịch vụ trong SQL.</p>';
        return;
    }

    box.innerHTML = state.services.map(s => `
        <div class="item-card">
            <div class="item-img"><i class="fa-solid fa-screwdriver-wrench"></i></div>
            <div class="item-content">
                <h3>${s.name}</h3>
                <p style="color:var(--text-muted); font-size:14px; min-height:44px;">${s.desc}</p>
                <span class="item-price">${formatMoney(s.price)}</span>
                <div class="action-buttons">
                    <button class="btn-cart" onclick="addToCart('${String(s.id).replace(/'/g, '')}')"><i class="fa-solid fa-plus"></i> Chọn</button>
                    <button class="btn-buy" onclick="addToCart('${String(s.id).replace(/'/g, '')}'); nav('book');">Đặt lịch</button>
                </div>
            </div>
        </div>
    `).join('');
}

function populateBookingServiceSelect(services) {
    const select = document.getElementById('booking-service-select');
    if (!select) return;

    if (!services.length) {
        select.innerHTML = '<option value="NONE">Chưa có dịch vụ</option>';
        return;
    }

    select.innerHTML = services.map(s => `<option value="${s.id}" data-price="${s.price}">${s.name} - ${formatMoney(s.price)}</option>`).join('');
    if (selectedBookingServiceId) select.value = selectedBookingServiceId;
}

function getSelectedBookingService() {
    const select = document.getElementById('booking-service-select');
    if (!select || !select.value || select.value === 'NONE') return null;
    return state.services.find(s => String(s.id) === String(select.value)) || null;
}

function addToCart(serviceId) {
    const service = state.services.find(s => String(s.id) === String(serviceId));
    if (!service) return alert('Không tìm thấy dịch vụ!');
    if (!state.cart.find(x => String(x.id) === String(service.id))) {
        state.cart.push(service);
    }
    renderCart();
    updateStats();
    alert('Đã thêm dịch vụ vào danh sách đã chọn.');
}

function removeFromCart(index) {
    state.cart.splice(index, 1);
    renderCart();
    renderBookingSelectedServices();
    updateStats();
}

function openCartModal() {
    renderCart();
    document.getElementById('cartModal')?.classList.add('active');
}

function closeCartModal() {
    document.getElementById('cartModal')?.classList.remove('active');
}

function renderCart() {
    const count = state.cart.length;
    setText('header-cart-count', count);
    setText('stat-cart', count);

    const box = document.getElementById('cart-items-container');
    const totalBox = document.getElementById('cart-total-price');
    const total = state.cart.reduce((sum, item) => sum + parseMoney(item.price), 0);
    if (totalBox) totalBox.innerText = formatMoney(total);

    if (!box) return;
    if (!state.cart.length) {
        box.innerHTML = '<p style="text-align:center; color:var(--text-muted); padding:30px 0;">Chưa chọn dịch vụ nào</p>';
        return;
    }

    box.innerHTML = state.cart.map((item, idx) => `
        <div class="cart-item">
            <div class="cart-item-info">
                <h4>${item.name}</h4>
                <p>${formatMoney(item.price)}</p>
            </div>
            <button class="btn-remove-item" onclick="removeFromCart(${idx})"><i class="fa-solid fa-trash"></i></button>
        </div>
    `).join('');
}

function checkoutCart() {
    if (!state.cart.length) return alert('Bạn chưa chọn dịch vụ nào!');
    closeCartModal();
    selectedBookingType = BOOKING_TYPE_REPAIR;
    nav('book');
    const repairCard = [...document.querySelectorAll('.option-card')].find(x => x.innerText.includes('Bảo dưỡng') || x.innerText.includes('Sửa chữa'));
    selectBookingType(repairCard, BOOKING_TYPE_REPAIR);
    renderBookingSelectedServices();
}

function mapCar(item) {
    const id = item.carId || item.CarId || item.id || item.Id;
    return {
        id,
        carId: id,
        customerId: item.customerId || item.CustomerId,
        licensePlate: item.licensePlate || item.LicensePlate || item.f1 || '',
        brand: item.brand || item.Brand || '',
        model: item.model || item.Model || '',
        year: item.year || item.Year || '',
        status: item.status || item.Status || 'Đang hoạt động',
        customerName: item.customerName || item.CustomerName || '',
        customerEmail: item.customerEmail || item.CustomerEmail || '',
        customerPhone: item.customerPhone || item.CustomerPhone || ''
    };
}

async function loadMyCars() {
    const data = await apiGetList('/api/Cars');
    state.cars = data.map(mapCar).filter(isMine);
    renderMyCars();
    refreshCarSelect();
    updateStats();
}

function renderMyCars() {
    const box = document.getElementById('mycars-list');
    if (!box) return;

    if (!state.cars.length) {
        box.innerHTML = '<p style="color:var(--text-muted);">Bạn chưa có xe nào trong hệ thống.</p>';
        return;
    }

    box.innerHTML = state.cars.map(car => `
        <div class="mycar-card">
            <div class="mycar-img" style="background-image:url('https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=80')">
                <div class="mycar-status">${car.status}</div>
            </div>
            <div class="mycar-body">
                <h3>${car.licensePlate}</h3>
                <div class="mycar-info">
                    <div><i class="fa-solid fa-car"></i> ${car.brand} ${car.model}</div>
                    <div><i class="fa-solid fa-calendar"></i> ${car.year || 'Chưa cập nhật'}</div>
                </div>
            </div>
        </div>
    `).join('');
}

function refreshCarSelect() {
    const select = document.getElementById('car-select-options');
    if (!select) return;
    if (!state.cars.length) {
        select.innerHTML = '<option value="">Chưa có xe - hãy thêm xe trước</option>';
        return;
    }
    select.innerHTML = state.cars.map(c => `<option value="${c.carId}">${c.licensePlate} - ${c.brand} ${c.model}</option>`).join('');
}

async function addMyCar() {
    const licensePlate = document.getElementById('new-car-plate')?.value.trim().toUpperCase();
    const brand = document.getElementById('new-car-brand')?.value.trim();
    const model = document.getElementById('new-car-model')?.value.trim();
    const year = Number(document.getElementById('new-car-year')?.value || new Date().getFullYear());

    if (!licensePlate || !brand || !model) return alert('Vui lòng nhập biển số, hãng xe và dòng xe!');

    const body = {
        licensePlate,
        brand,
        model,
        year,
        customerId: Number(user.id || user.Id)
    };

    try {
        await apiFetch('/api/Cars', { method: 'POST', body: JSON.stringify(body) });
        alert('Đã thêm xe vào SQL.');
        setValue('new-car-plate', '');
        setValue('new-car-brand', '');
        setValue('new-car-model', '');
        setValue('new-car-year', '');
        await loadMyCars();
    } catch (e) {
        alert(e.message || 'Không thêm được xe.');
    }
}

function selectBookingType(el, type) {
    selectedBookingType = type;
    document.querySelectorAll('.option-card').forEach(x => x.classList.remove('active'));
    if (el) el.classList.add('active');
    updateBookingTypeUI();
}

function updateBookingTypeUI() {
    const isRepair = selectedBookingType === BOOKING_TYPE_REPAIR;
    const serviceGroup = document.getElementById('booking-service-group');
    const noteLabel = document.getElementById('booking-note-label');
    if (serviceGroup) serviceGroup.style.display = isRepair ? 'block' : 'none';
    if (noteLabel) noteLabel.innerText = isRepair ? 'Ghi chú thêm' : 'Ghi chú tình trạng xe';
    renderBookingSelectedServices();
}

function addSelectedServiceToCart() {
    const service = getSelectedBookingService();
    if (!service) return alert('Vui lòng chọn dịch vụ!');
    addToCart(service.id);
    renderBookingSelectedServices();
}

function renderBookingSelectedServices() {
    const countEl = document.getElementById('booking-selected-count');
    const listEl = document.getElementById('booking-selected-services');
    const totalEl = document.getElementById('booking-estimated-total');
    if (countEl) countEl.innerText = state.cart.length;
    const total = state.cart.reduce((sum, item) => sum + parseMoney(item.price), 0);
    if (totalEl) totalEl.innerText = formatMoney(total);
    if (listEl) {
        listEl.innerHTML = state.cart.length
            ? state.cart.map((s, idx) => `<div style="display:flex; justify-content:space-between; gap:10px; padding:6px 0;"><span>${idx + 1}. ${s.name}</span><b>${formatMoney(s.price)}</b></div>`).join('')
            : 'Chưa chọn dịch vụ nào.';
    }
}

async function refreshBookingOptions() {
    if (!state.services.length) await loadServices();
    if (!state.cars.length) await loadMyCars();
    populateBookingServiceSelect(state.services);
    refreshCarSelect();
    updateBookingTypeUI();
}

async function submitBooking() {
    const dateValue = document.getElementById('booking-date')?.value;
    const note = document.getElementById('booking-note')?.value.trim() || '';
    const carIdRaw = document.getElementById('car-select-options')?.value;
    const isRepair = selectedBookingType === BOOKING_TYPE_REPAIR;

    if (!dateValue) return alert('Vui lòng chọn ngày giờ hẹn!');
    const appointmentDate = new Date(dateValue);
    if (appointmentDate <= new Date()) return alert('Không được đặt lịch trong quá khứ!');
    if (!carIdRaw) return alert('Vui lòng chọn xe của bạn!');
    if (isRepair && state.cart.length === 0) return alert('Vui lòng chọn ít nhất một dịch vụ sửa chữa!');
    if (!isRepair && !note) return alert('Vui lòng nhập ghi chú tình trạng xe để garage chuẩn bị tư vấn.');

    const services = isRepair ? state.cart.map(s => ({
        serviceId: Number(s.id) || null,
        serviceName: s.name,
        price: parseMoney(s.price)
    })) : [];
    const total = services.reduce((sum, s) => sum + parseMoney(s.price), 0);
    const serviceName = services.length ? services.map(s => s.serviceName).join(', ') : '';

    const body = {
        customerName: user.name || user.fullName || 'Khách hàng',
        customerAccount: getCurrentUserPhone() || getCurrentUserEmail(),
        customerEmail: getCurrentUserEmail(),
        type: selectedBookingType,
        serviceName: serviceName,
        services: services,
        estimatedAmount: total,
        selectedTarget: serviceName || selectedBookingType,
        carId: Number(carIdRaw),
        appointmentDate: appointmentDate.toISOString(),
        note
    };

    try {
        await apiFetch('/api/Appointments/customer-request', { method: 'POST', body: JSON.stringify(body) });
        alert('Đặt lịch thành công. Vui lòng chờ Admin xác nhận.');
        setValue('booking-note', '');
        if (isRepair) state.cart = [];
        renderCart();
        renderBookingSelectedServices();
        await loadMyBookings();
        nav('book');
    } catch (e) {
        alert(e.message || 'Không đặt được lịch hẹn.');
    }
}

function getNoteValue(note, label) {
    const text = String(note || '');
    const match = text.match(new RegExp(label + ':\\s*([^\\n]+)', 'i'));
    return match ? match[1].trim() : '';
}

function mapBooking(item) {
    const note = item.note || item.Note || '';
    return {
        id: item.appointmentId || item.AppointmentId || item.id || item.Id,
        customerId: item.customerId || item.CustomerId,
        customerName: item.customerName || item.CustomerName || '',
        customerEmail: item.customerEmail || item.CustomerEmail || '',
        customerPhone: item.customerPhone || item.CustomerPhone || '',
        type: item.type || item.Type || getNoteValue(note, 'Loại yêu cầu') || 'Lịch hẹn',
        serviceName: item.serviceName || item.ServiceName || (item.services || item.Services || []).map(x => x.serviceName || x.ServiceName).filter(Boolean).join(', ') || getNoteValue(note, 'Dịch vụ') || 'Dịch vụ sửa chữa',
        date: item.date || item.Date || item.appointmentDate || item.AppointmentDate || '',
        status: item.status || item.Status || 'Chờ xác nhận',
        rejectionReason: item.rejectionReason || item.RejectionReason || '',
        rejectedAt: item.rejectedAt || item.RejectedAt || '',
        note
    };
}

async function loadMyBookings() {
    const data = await apiGetList('/api/Appointments');
    state.bookings = data.map(mapBooking).filter(isMine);
    renderBookings();
    updateStats();
}

function renderBookings() {
    const box = document.getElementById('my-bookings-list');
    if (!box) return;
    if (!state.bookings.length) {
        box.innerHTML = '<p style="color:var(--text-muted);">Bạn chưa có lịch hẹn nào.</p>';
        return;
    }

    box.innerHTML = state.bookings.map(b => {
        const st = String(b.status || '');
        const cls = st.includes('từ chối') || st.includes('Từ chối') ? 'rejected' : st.includes('Chờ') ? 'pending' : 'done';
        return `
            <div class="booking-history-card">
                <strong>${b.serviceName}</strong><br>
                <span style="color:var(--text-muted);">${b.type} • ${b.date}</span><br>
                <span class="booking-status ${cls}">${b.status}</span>
                ${b.rejectionReason ? `<div class="reject-reason-box"><b>Lý do từ chối:</b> ${b.rejectionReason}${b.rejectedAt ? `<br><b>Thời gian từ chối:</b> ${b.rejectedAt}` : ''}</div>` : ''}
            </div>
        `;
    }).join('');
}

function mapOrder(item) {
    const id = item.invoiceId || item.InvoiceId || item.id || item.Id;
    const paidAmount = item.paidAmount ?? item.PaidAmount ?? 0;
    const pendingAmount = item.pendingAmount ?? item.PendingAmount ?? 0;
    const remainingAmount = item.remainingAmount ?? item.RemainingAmount ?? item.totalAmount ?? item.TotalAmount ?? item.amount ?? item.Amount ?? 0;
    const latestPaymentStatus = item.latestPaymentStatus || item.LatestPaymentStatus || '';
    return {
        id,
        invoiceId: id,
        customerId: item.customerId || item.CustomerId,
        customerName: item.customerName || item.CustomerName || '',
        customerEmail: item.customerEmail || item.CustomerEmail || '',
        customerPhone: item.customerPhone || item.CustomerPhone || '',
        serviceName: item.serviceName || item.ServiceName || item.description || item.Description || 'Hóa đơn sửa chữa',
        amount: item.totalAmount ?? item.TotalAmount ?? item.amount ?? item.Amount ?? 0,
        paidAmount,
        pendingAmount,
        remainingAmount,
        status: item.status || item.Status || 'Chưa thanh toán',
        latestPaymentId: item.latestPaymentId || item.LatestPaymentId || item.paymentId || item.PaymentId,
        latestPaymentStatus
    };
}

function canPayInvoice(o) {
    const status = String(o.status || '').toLowerCase();
    const latest = String(o.latestPaymentStatus || '').toLowerCase();
    const remaining = Number(o.remainingAmount ?? o.amount ?? 0);
    if (status.includes('chờ xác nhận') || latest.includes('chờ xác nhận')) return false;
    if (status.includes('đã thanh toán') || status.includes('hoàn tất')) return false;
    return remaining > 0 || status.includes('chưa thanh toán') || status.includes('thanh toán một phần');
}

async function loadOrders() {
    let data = [];
    try {
        data = await apiGetList('/api/Invoices/my');
    } catch (e) {
        data = await apiGetList('/api/Invoices');
        data = data.map(mapOrder).filter(isMine);
        state.orders = data;
        renderOrders();
        updateStats();
        return;
    }
    state.orders = data.map(mapOrder);
    renderOrders();
    updateStats();
}

function renderOrders() {
    const box = document.getElementById('user-orders');
    if (!box) return;
    if (!state.orders.length) {
        box.innerHTML = '<p style="color:var(--text-muted);">Bạn chưa có hóa đơn nào.</p>';
        return;
    }

    box.innerHTML = state.orders.map(o => `
        <div class="order-card">
            <div>
                <h3 style="font-size:18px; font-weight:900; margin-bottom:8px;">${o.serviceName}</h3>
                <p style="color:var(--text-muted);">Mã hóa đơn: HD${o.invoiceId} • Trạng thái: <b>${o.status}</b></p>
            </div>
            <div style="text-align:right;">
                <b style="font-size:22px; color:var(--primary);">${formatMoney(o.amount)}</b><br>
                ${canPayInvoice(o) ? `<button class="btn-buy" style="margin-top:10px;" onclick="openQrForInvoice(${o.invoiceId}, ${parseMoney(o.remainingAmount || o.amount)})">Thanh toán QR</button>` : (String(o.latestPaymentStatus || o.status).includes('Chờ') ? '<div style="margin-top:10px;color:#b45309;font-weight:800;">Đang chờ Admin xác nhận QR</div>' : '')}
            </div>
        </div>
    `).join('');
}

function buildQrUrl(amount, content) {
    return `https://img.vietqr.io/image/VCB-9387999288-compact2.png?amount=${encodeURIComponent(amount)}&addInfo=${encodeURIComponent(content)}&accountName=${encodeURIComponent('DO TRUNG KIEN')}`;
}

function openQrForInvoice(invoiceId, amount) {
    const content = 'THANH TOAN HD' + invoiceId;
    state.qr = { invoiceId, amount, content };
    document.getElementById('qrPaymentImage').src = buildQrUrl(amount, content);
    setText('qrOrderId', 'HD' + invoiceId);
    setText('qrAmount', formatMoney(amount));
    setText('qrContent', content);
    document.getElementById('qrPaymentModal')?.classList.add('active');
}

function cancelQrPayment() {
    state.qr = null;
    document.getElementById('qrPaymentModal')?.classList.remove('active');
}

async function confirmQrPayment() {
    if (!state.qr) return;
    try {
        await apiFetch('/api/Payments/qr-request', {
            method: 'POST',
            body: JSON.stringify({
                invoiceId: state.qr.invoiceId,
                amount: state.qr.amount,
                customerName: user.name || user.fullName || 'Khách hàng',
                customerAccount: getCurrentUserPhone() || getCurrentUserEmail(),
                customerEmail: getCurrentUserEmail(),
                serviceName: 'Thanh toán hóa đơn',
                localOrderId: 'HD' + state.qr.invoiceId
            })
        });
        alert('Đã gửi yêu cầu xác nhận thanh toán QR. Vui lòng chờ Admin kiểm tra.');
        cancelQrPayment();
        await loadOrders();
    } catch (e) {
        alert(e.message || 'Không gửi được yêu cầu thanh toán.');
    }
}

async function updateCustomerProfile() {
    const fullName = document.getElementById('prof-display-name')?.value.trim();
    const phoneNumber = document.getElementById('prof-phone')?.value.trim();
    const email = getCurrentUserEmail();

    if (!fullName || !phoneNumber) return alert('Vui lòng nhập họ tên và số điện thoại!');

    try {
        const updated = await apiFetch('/api/Customers/' + user.id, {
            method: 'PUT',
            body: JSON.stringify({ fullName, phoneNumber, email, address: user.address || '' })
        });
        const data = updated || {};
        user.name = data.fullName || data.FullName || fullName;
        user.phoneNumber = data.phoneNumber || data.PhoneNumber || phoneNumber;
        user.phone = user.phoneNumber;
        sessionStorage.setItem(SESSION_KEY, JSON.stringify(user));
        syncUserInfo();
        alert('Đã cập nhật thông tin tài khoản.');
    } catch (e) {
        alert(e.message || 'Không cập nhật được tài khoản.');
    }
}

function updateStats() {
    setText('stat-orders', state.orders.length);
    setText('stat-bookings', state.bookings.filter(b => String(b.status || '').includes('Chờ')).length);
    setText('stat-cart', state.cart.length);
    setText('header-cart-count', state.cart.length);

    const recent = document.getElementById('recent-activity');
    if (recent) {
        const items = [
            ...state.bookings.slice(0, 3).map(b => `<div class="booking-history-card"><b>Lịch hẹn:</b> ${b.serviceName}<br><span style="color:var(--text-muted);">${b.date} • ${b.status}</span></div>`),
            ...state.orders.slice(0, 2).map(o => `<div class="booking-history-card"><b>Hóa đơn:</b> ${o.serviceName}<br><span style="color:var(--text-muted);">${formatMoney(o.amount)} • ${o.status}</span></div>`)
        ];
        recent.innerHTML = items.join('') || '<p style="color:var(--text-muted);">Chưa có hoạt động gần đây.</p>';
    }
}

async function loadAllCustomerData() {
    syncUserInfo();
    renderCart();
    try {
        await Promise.all([loadServices(), loadMyCars(), loadMyBookings(), loadOrders()]);
        updateStats();
    } catch (e) {
        console.error(e);
    }
}

function showSupportContent(type) {
    const contents = {
        guide: `Hướng dẫn đặt lịch sửa xe:
1. Chọn loại lịch phù hợp.
2. Chọn xe của bạn.
3. Nếu bảo dưỡng/sửa chữa, chọn một hoặc nhiều dịch vụ.
4. Chọn ngày giờ và gửi yêu cầu.
5. Theo dõi trạng thái trong mục Lịch hẹn của tôi.`,
        warranty: `Chính sách bảo hành:
- Bảo hành theo từng dịch vụ/phụ tùng trên hóa đơn.
- Khách cần giữ hóa đơn hoặc mã lịch sử sửa chữa.
- Không áp dụng khi xe bị can thiệp bởi đơn vị khác sau sửa chữa.`,
        price: `Bảng giá dịch vụ:
Vui lòng xem mục Dịch vụ sửa chữa. Giá hiển thị là tạm tính, garage sẽ kiểm tra thực tế và báo giá chính xác trước khi sửa.`
    };
    alert(contents[type] || 'Nội dung hỗ trợ đang được cập nhật.');
}

window.addEventListener('load', loadAllCustomerData);
