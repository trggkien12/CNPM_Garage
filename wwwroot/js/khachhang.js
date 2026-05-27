// 1. KIỂM TRA ĐĂNG NHẬP
        const user = JSON.parse(sessionStorage.getItem('KKTH_ACTIVE_USER') || localStorage.getItem('KKTH_ACTIVE_USER')) || JSON.parse(sessionStorage.getItem('activeUser')) || { name: 'Khách Demo', user: 'khach@demo.com' }; 
        
        let selectedBookingType = 'Xem xe tại showroom';
        let cachedBookingServices = [];
        let selectedBookingServiceId = '';


        function displayCustomerAccount(value) {
            const raw = String(value || '').trim();
            const fakeEmail = raw.match(/^(0\d{9,10})@khachhang\.com$/i);
            if(fakeEmail) return fakeEmail[1];
            return raw;
        }

        function getCurrentUserAccount() {
            return displayCustomerAccount((user && (user.phoneNumber || user.user || user.email)) || '');
        }

        function syncUserInfo() {
            if(!user) return;
            const userName = user.name || user.fullName || "Khách hàng";
            const userEmail = getCurrentUserAccount() || "Tài khoản trống";

            document.getElementById('nav-name').innerText = userName;
            document.getElementById('welcome-msg').innerText = `Xin chào, ${userName}!`;
            document.getElementById('prof-fullname').innerText = userName;
            document.getElementById('prof-display-name').value = userName;
            document.getElementById('prof-user').value = userEmail;
            
            updateStats();
        }

        function logoutCustomer() {
            if(confirm("Bạn muốn đăng xuất khỏi tài khoản?")) {
                sessionStorage.clear();
                localStorage.removeItem('KKTH_ACTIVE_USER');
                window.location.href = 'login.html';
            }
        }

        function getCurrentUserKey() {
            const raw = getCurrentUserAccount() || (user && user.name) || 'guest';
            return String(raw).trim().toLowerCase().replace(/[^a-z0-9@._-]/g, '_');
        }

        function normalizeUserKey(value) {
            return displayCustomerAccount(value || '')
                .trim()
                .toLowerCase()
                .replace(/\s+/g, '')
                .replace(/[^a-z0-9@._-]/g, '_');
        }

        function normalizeCompareText(value) {
            return displayCustomerAccount(value || '')
                .trim()
                .toLowerCase()
                .replace(/\s+/g, '');
        }

        function getCartKey() {
            return 'db_cart_' + getCurrentUserKey();
        }

        function isMine(record) {
            if (!record) return false;

            const account = normalizeCompareText(getCurrentUserAccount() || '');
            const name = normalizeCompareText((user && (user.name || user.fullName)) || '');
            const email = normalizeCompareText((user && (user.email || user.user)) || '');
            const phone = normalizeCompareText((user && (user.phoneNumber || user.phone || user.phone_number)) || '');
            const key = normalizeUserKey(getCurrentUserAccount() || (user && (user.name || user.fullName)) || 'guest');

            const fields = [
                record.ownerUser,
                record.customerAccount,
                record.customerEmail,
                record.customerPhone,
                record.phoneNumber,
                record.email,
                record.user,
                record.customerName,
                record.fullName,
                record.name,
                record.f2,
                record.note,
                record.description
            ];

            const text = normalizeCompareText(fields.join(' '));
            const owner = normalizeUserKey(record.ownerUser || '');

            if (owner && owner === key) return true;
            if (account && text.includes(account)) return true;
            if (phone && text.includes(phone)) return true;
            if (email && text.includes(email)) return true;

            // Fallback theo tên: dùng khi dữ liệu SQL chỉ trả customerName, không có tài khoản/sđt/email.
            if (name && text.includes(name)) return true;

            return false;
        }

        // 2. ĐIỀU HƯỚNG TAB
        function nav(id) {
            const section = document.getElementById(id);
            const button = document.getElementById('btn-' + id);

            if (!section) {
                alert('Không tìm thấy màn hình: ' + id);
                console.error('Thiếu section id =', id);
                return;
            }

            document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
            document.querySelectorAll('nav button').forEach(b => b.classList.remove('active'));

            section.classList.add('active');
            if (button) button.classList.add('active');

            window.scrollTo(0, 0);
            if (typeof updateStats === 'function') updateStats();
            if (id === 'mycars' && typeof loadMyCars === 'function') loadMyCars();
            if (id === 'book') {
                if (typeof refreshBookingOptions === 'function') refreshBookingOptions();
                if (typeof loadMyBookings === 'function') loadMyBookings();
            }
            if (id === 'order' && typeof loadOrders === 'function') loadOrders();
        }

        // 3. DỊCH VỤ SỬA CHỮA - ĐỌC CHUNG DỮ LIỆU ADMIN + KHÁCH HÀNG
        function safeParseArray(key) {
            try {
                const data = JSON.parse(localStorage.getItem(key) || '[]');
                return Array.isArray(data) ? data : [];
            } catch (e) {
                console.warn('Không đọc được localStorage key:', key, e);
                return [];
            }
        }

        function isRepairService(item) {
            const explicitType = String(item.type || item.Type || item.category || item.Category || item.kind || item.itemType || '').toLowerCase();

            // Chỉ loại phụ tùng khi dữ liệu có khai báo rõ là phụ tùng.
            // Không dùng f5 để phân loại vì f5 thường là đường dẫn ảnh URL.
            if (explicitType.includes('phụ tùng') || explicitType.includes('phu tung') || explicitType.includes('part') || explicitType.includes('spare')) {
                return false;
            }

            const name = String(item.f1 || item.name || item.Name || item.serviceName || item.ServiceName || '').toLowerCase();
            if (!name.trim()) return false;

            // Nếu không có type/category thì vẫn cho hiện vì Admin cũ thường chỉ lưu f1, f2, f3, f4, f5.
            return true;
        }

        function normalizeService(item, index) {
            const imageFromF5 = String(item.f5 || '').startsWith('http') ? item.f5 : '';
            const name = item.f1 || item.name || item.Name || item.serviceName || item.ServiceName || 'Dịch vụ sửa chữa';
            const code = item.f2 || item.code || item.Code || item.serviceCode || item.ServiceCode || ('DV' + String(index + 1).padStart(3, '0'));
            const rawPrice = item.f3 ?? item.price ?? item.Price ?? item.unitPrice ?? item.UnitPrice ?? 0;

            return {
                id: item.id || item.Id || item.serviceId || item.ServiceId || code || ('DV_' + index),
                name: String(name),
                code: String(code),
                price: Number(String(rawPrice).replace(/[^0-9.-]/g, '')) || 0,
                desc: item.description || item.Description || item.desc || item.note || item.f6 || 'Dịch vụ sửa chữa, bảo dưỡng và chăm sóc xe tại garage.',
                image: item.img || item.image || item.imageUrl || item.ImageUrl || imageFromF5 || ''
            };
        }


        function populateBookingServiceSelect(services) {
            cachedBookingServices = Array.isArray(services) ? services : [];
            const select = document.getElementById('booking-service-select');
            if (!select) return;

            if (cachedBookingServices.length === 0) {
                select.innerHTML = '<option value="NONE">Chưa có dịch vụ - Admin cần thêm dịch vụ</option>';
                return;
            }

            select.innerHTML = cachedBookingServices.map(s => `
                <option value="${s.id}" data-price="${Number(s.price) || 0}">
                    ${s.name} - ${Number(s.price || 0).toLocaleString('vi-VN')}đ
                </option>
            `).join('');

            if (selectedBookingServiceId) {
                select.value = selectedBookingServiceId;
            }
        }

        function getSelectedBookingService() {
            const select = document.getElementById('booking-service-select');
            if (!select || !select.value || select.value === 'NONE') return null;
            return cachedBookingServices.find(s => String(s.id) === String(select.value)) || null;
        }

        function createPendingInvoiceForBooking(booking, service) {
            const amount = Number(service && service.price) || Number(booking.estimatedAmount) || 0;
            if (amount <= 0) return null;

            let orders = JSON.parse(localStorage.getItem('db_order') || '[]');
            const existed = orders.find(o => String(o.bookingId || '') === String(booking.id));
            if (existed) return existed;

            const invoice = {
                id: 'HD' + Math.floor(Math.random() * 1000000),
                bookingId: booking.id,
                ownerUser: getCurrentUserKey(),
                customerAccount: displayCustomerAccount(booking.customerAccount || getCurrentUserAccount() || ''),
                customerName: booking.customerName || user.name || user.fullName || 'Khách hàng',
                f1: 'Hóa đơn tạm tính: ' + (service ? service.name : booking.carService || 'Dịch vụ sửa chữa'),
                f2: booking.customerName || user.name || user.fullName || 'Khách hàng',
                f3: amount,
                f4: new Date().toLocaleString('vi-VN'),
                carService: service ? service.name : booking.carService || '',
                selectedTarget: booking.selectedTarget || '',
                paymentMethod: '',
                paymentStatus: 'Chưa thanh toán',
                status: 'Chưa thanh toán',
                createdFrom: 'Tạo tự động sau khi khách đặt lịch'
            };

            orders.unshift(invoice);
            localStorage.setItem('db_order', JSON.stringify(orders));
            return invoice;
        }

        async function readServicesFromApi() {
            const endpoints = [
                '/api/Services',
                '/api/services',
                '/api/service',
                '/api/repairservices',
                '/api/RepairServices',
                '/api/products'
            ];

            for (const url of endpoints) {
                try {
                    const res = await fetch(url, { cache: 'no-store' });
                    if (!res.ok) continue;
                    const data = await res.json();
                    if (Array.isArray(data)) return data;
                    if (Array.isArray(data.data)) return data.data;
                    if (Array.isArray(data.items)) return data.items;
                } catch (e) {
                    // API nào không tồn tại thì bỏ qua, đọc localStorage tiếp.
                }
            }
            return [];
        }

        async function getAllServicesForCustomer() {
            // FIX: Phần Dịch vụ sửa chữa của khách hàng chỉ lấy từ SQL/API.
            // Không gộp db_service/localStorage/default nữa để tránh lỗi hiện lặp dịch vụ.
            const apiServices = await readServicesFromApi();

            const seen = new Set();
            const services = apiServices
                .filter(isRepairService)
                .map(normalizeService)
                .filter(s => {
                    // Ưu tiên lọc trùng theo Mã DV; nếu thiếu mã thì lọc theo Tên dịch vụ.
                    const codeKey = String(s.code || '').trim().toLowerCase();
                    const nameKey = String(s.name || '').trim().toLowerCase().replace(/\s+/g, ' ');
                    const key = codeKey || nameKey;

                    if (!key) return false;
                    if (seen.has(key)) return false;

                    seen.add(key);
                    return true;
                });

            return services;
        }

        function getServiceImage(service, index) {
            const defaults = [
                'https://images.unsplash.com/photo-1487754180451-c456f719a1fc?auto=format&fit=crop&w=1200&q=80',
                'https://images.unsplash.com/photo-1607860108855-64acf2078ed9?auto=format&fit=crop&w=1200&q=80',
                'https://images.unsplash.com/photo-1625047509168-a7026f36de04?auto=format&fit=crop&w=1200&q=80',
                'https://images.unsplash.com/photo-1632823469850-1b7b1e8b7e1e?auto=format&fit=crop&w=1200&q=80',
                'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?auto=format&fit=crop&w=1200&q=80'
            ];
            return service.image || defaults[index % defaults.length];
        }

        async function loadProducts() {
            const services = await getAllServicesForCustomer();
            populateBookingServiceSelect(services);

            const list = document.getElementById('showroom-list') 
                      || document.getElementById('service-list') 
                      || document.getElementById('services-list');

            const carSelect = document.getElementById('car-select-options');

            if (!list) {
                console.error('Không tìm thấy vùng hiển thị dịch vụ: showroom-list/service-list/services-list');
                return;
            }

            if (services.length === 0) {
                list.innerHTML = `
                    <div style="grid-column:1/-1; background:white; padding:30px; border-radius:16px; text-align:center;">
                        <h3>Chưa có dịch vụ sửa chữa</h3>
                        <p>Admin cần thêm dịch vụ trong trang quản trị.</p>
                    </div>
                `;
                return;
            }

            list.innerHTML = services.map((item, index) => {
                const image = getServiceImage(item, index);
                const safeName = String(item.name).replace(/'/g, "\'");
                const safeId = String(item.id).replace(/'/g, "\'");

                return `
                    <div class="item-card">
                        <div class="item-img" style="background-image:url('${image}')">
                            <i class="fa-solid fa-screwdriver-wrench"></i>
                        </div>
                        <div class="item-content">
                            <div>
                                <span style="font-size:11px; font-weight:800; color:var(--success); background:#dcfce7; padding:6px 10px; border-radius:6px;">
                                    DỊCH VỤ SỬA CHỮA
                                </span>
                            </div>
                            <h3 style="margin-top:15px;">${item.name}</h3>
                            <p style="font-size:13px; color:var(--text-muted); font-weight:700;">Mã: ${item.code}</p>
                            <p style="font-size:13px; color:var(--text-muted); line-height:1.6; margin-top:6px;">${item.desc}</p>
                            <span class="item-price">${Number(item.price).toLocaleString('vi-VN')}đ</span>

                            <div class="action-buttons">
                                <button class="btn-cart" onclick="addToCart('${safeId}', '${safeName}', ${Number(item.price)})">
                                    <i class="fa-solid fa-check"></i> Chọn dịch vụ
                                </button>
                                <button class="btn-buy" onclick="buyNow('${safeId}', '${safeName}', ${Number(item.price)})">
                                    Đặt lịch ngay
                                </button>
                            </div>
                        </div>
                    </div>
                `;
            }).join('');

            // Nếu khách chưa có xe/dịch vụ đã chọn thì select sẽ có danh sách dịch vụ để đặt lịch nhanh.
            if (carSelect) {
                const myCars = JSON.parse(localStorage.getItem(getMyCarKey ? getMyCarKey() : '')) || [];
                const selectedServices = JSON.parse(localStorage.getItem(getCartKey ? getCartKey() : '')) || [];
                if (myCars.length === 0 && selectedServices.length === 0) {
                    carSelect.innerHTML = services.map(item => `<option value="SV_${item.id}" data-type="service">Dịch vụ: ${item.name}</option>`).join('');
                }
            }
        }

        function selectServiceForBooking(id, name, price) {
            addToCart(id, name, price);
        }

        function bookServiceNow(id, name, price) {
            buyNow(id, name, price);
        }

        // ==========================================
        // 4. CHỨC NĂNG THANH TOÁN
        // ==========================================
        
        function addToCart(id, name, price) {
            let cart = JSON.parse(localStorage.getItem(getCartKey())) || [];
            cart.push({ id, name, price, date: new Date().toISOString() });
            localStorage.setItem(getCartKey(), JSON.stringify(cart));
            
            openCartModal();
            updateStats();
        }

        function openCartModal() {
            renderCartModal();
            document.getElementById('cartModal').classList.add('active');
        }

        function closeCartModal() {
            document.getElementById('cartModal').classList.remove('active');
        }

        function renderCartModal() {
            let cart = JSON.parse(localStorage.getItem(getCartKey())) || [];
            let container = document.getElementById('cart-items-container');
            let totalPriceEl = document.getElementById('cart-total-price');

            if(cart.length === 0) {
                container.innerHTML = `<div style="text-align: center; color: var(--text-muted); padding-top: 80px;">
                    <i class="fa-solid fa-basket-shopping" style="font-size: 60px; color: #cbd5e1; margin-bottom: 20px;"></i>
                    <h4 style="color: var(--text-main); font-size: 18px; margin-bottom: 10px;">Chưa chọn dịch vụ</h4>
                    <p>Hãy chọn một hoặc nhiều dịch vụ muốn làm cho xe của bạn.</p>
                </div>`;
                totalPriceEl.innerText = "0đ";
                return;
            }

            let total = 0;
            container.innerHTML = cart.map((item, index) => {
                total += Number(item.price);
                return `
                <div class="cart-item">
                    <div class="cart-item-info">
                        <h4>${item.name}</h4>
                        <p>${Number(item.price).toLocaleString('vi-VN')}đ</p>
                    </div>
                    <button class="btn-remove-item" onclick="removeFromCart(${index})" title="Xóa">
                        <i class="fa-solid fa-trash-can"></i>
                    </button>
                </div>`;
            }).join('');

            totalPriceEl.innerText = total.toLocaleString('vi-VN') + 'đ';
        }

        function removeFromCart(index) {
            let cart = JSON.parse(localStorage.getItem(getCartKey())) || [];
            cart.splice(index, 1);
            localStorage.setItem(getCartKey(), JSON.stringify(cart));
            renderCartModal();
            updateStats();
        }

        // Cấu hình QR chuyển khoản VCB
        const BANK_CONFIG = {
            bankCode: 'VCB',
            accountNumber: '9387999288',
            accountName: 'DO TRUNG KIEN'
        };
        let pendingQrOrder = null;

        function buildVietQrUrl(amount, content) {
            const cleanAmount = Math.max(0, Math.round(Number(amount) || 0));
            return `https://img.vietqr.io/image/${BANK_CONFIG.bankCode}-${BANK_CONFIG.accountNumber}-compact2.png?amount=${cleanAmount}&addInfo=${encodeURIComponent(content)}&accountName=${encodeURIComponent(BANK_CONFIG.accountName)}`;
        }

        function openQrPayment(order) {
            pendingQrOrder = order;
            const orderId = order.id || order.localOrderId || order.invoiceId || order.paymentId || ('HD' + Date.now());
            const amount = Number(order.f3 || order.totalAmount || order.amount || 0);
            const content = `THANH TOAN ${orderId}`;
            document.getElementById('qrOrderId').innerText = orderId;
            document.getElementById('qrAmount').innerText = amount.toLocaleString('vi-VN') + 'đ';
            document.getElementById('qrContent').innerText = content;
            document.getElementById('qrPaymentImage').src = buildVietQrUrl(amount, content);
            document.getElementById('qrPaymentModal').classList.add('active');
        }

        function cancelQrPayment() {
            pendingQrOrder = null;
            document.getElementById('qrPaymentModal').classList.remove('active');
        }

        async function confirmQrPayment() {
            if(!pendingQrOrder) return;

            const now = new Date().toLocaleString('vi-VN');
            const updatedOrder = {
                ...pendingQrOrder,
                ownerUser: pendingQrOrder.ownerUser || getCurrentUserKey(),
                customerAccount: displayCustomerAccount(pendingQrOrder.customerAccount || getCurrentUserAccount() || ''),
                customerName: pendingQrOrder.customerName || user.name || user.fullName || 'Khách hàng',
                paymentMethod: 'Chuyển khoản VCB',
                paymentStatus: 'Chờ Admin/Nhân viên xác nhận chuyển khoản QR VCB',
                status: 'Chờ xác nhận thanh toán QR',
                requestedAt: now,
                bankName: 'VCB',
                bankAccount: BANK_CONFIG.accountNumber,
                bankOwner: BANK_CONFIG.accountName
            };

            try {
                const payment = await apiJson('/api/Payments/qr-request', {
                    method: 'POST',
                    body: JSON.stringify({
                        invoiceId: pendingQrOrder.invoiceId || pendingQrOrder.InvoiceId || null,
                        localOrderId: pendingQrOrder.id || pendingQrOrder.localOrderId || '',
                        customerName: updatedOrder.customerName,
                        customerAccount: updatedOrder.customerAccount,
                        customerEmail: user.email || '',
                        serviceName: pendingQrOrder.f1 || pendingQrOrder.serviceName || 'Hóa đơn dịch vụ',
                        amount: Number(pendingQrOrder.f3 || pendingQrOrder.totalAmount || pendingQrOrder.amount || 0),
                        note: `Khách đã bấm Đã chuyển khoản lúc ${now}`
                    })
                });

                updatedOrder.paymentId = payment.paymentId || payment.PaymentId || payment.id || payment.Id;
                updatedOrder.invoiceId = payment.invoiceId || payment.InvoiceId || pendingQrOrder.invoiceId || null;
                updatedOrder.sqlSynced = true;
            } catch(e) {
                alert(e.message || 'Không gửi được yêu cầu thanh toán QR lên SQL. Vui lòng thử lại.');
                return;
            }

            let orders = JSON.parse(localStorage.getItem('db_order')) || [];
            const existingIndex = orders.findIndex(o => String(o.id) === String(updatedOrder.id));
            if(existingIndex >= 0) orders[existingIndex] = updatedOrder;
            else orders.push(updatedOrder);
            localStorage.setItem('db_order', JSON.stringify(orders));

            if(pendingQrOrder.clearCart) {
                localStorage.setItem(getCartKey(), JSON.stringify([]));
                closeCartModal();
            }

            document.getElementById('qrPaymentModal').classList.remove('active');
            alert('Đã gửi yêu cầu xác nhận thanh toán QR lên SQL. Hóa đơn đang chờ Admin/Nhân viên kiểm tra giao dịch.');
            pendingQrOrder = null;
            await loadOrders();
            updateStats();
            nav('order');
        }

        function checkoutCart() {
            let cart = JSON.parse(localStorage.getItem(getCartKey())) || [];
            if(cart.length === 0) {
                alert("Bạn chưa chọn dịch vụ nào! Vui lòng chọn dịch vụ cần đặt lịch.");
                return;
            }

            let total = cart.reduce((sum, item) => sum + Number(item.price), 0);
            let itemNames = cart.map(item => item.name).join(', ');
            let bookings = JSON.parse(localStorage.getItem('db_booking')) || [];
            bookings.push({
                id: 'BK' + Math.floor(Math.random() * 100000),
                ownerUser: getCurrentUserKey(),
                customerAccount: getCurrentUserAccount() || '',
                customerName: user.name || user.fullName || 'Khách hàng',
                customerEmail: user.email || '',
                type: 'Yêu cầu dịch vụ sửa chữa',
                carService: itemNames,
                date: 'Chưa chọn - Gara sẽ liên hệ',
                note: `Khách chọn dịch vụ: ${itemNames}. Tạm tính dự kiến: ${Number(total).toLocaleString('vi-VN')}đ. Tổng tiền chính thức sẽ được lập sau khi kiểm tra xe.`,
                status: 'Chờ Gara xác nhận',
                rejectionReason: '',
                rejectedAt: '',
                approvedAt: '',
                createdAt: new Date().toLocaleString('vi-VN')
            });
            localStorage.setItem('db_booking', JSON.stringify(bookings));
            localStorage.setItem(getCartKey(), JSON.stringify([]));
            closeCartModal();
            alert('Đã gửi yêu cầu đặt lịch dịch vụ. Gara sẽ kiểm tra và xác nhận với bạn.');
            updateStats();
            nav('book');
        }

        function buyNow(id, name, price) {
            const service = { id, name, price: Number(price) || 0 };
            if (!cachedBookingServices.find(s => String(s.id) === String(id))) {
                cachedBookingServices.push(service);
                populateBookingServiceSelect(cachedBookingServices);
            }
            selectedBookingServiceId = String(id);
            const serviceSelect = document.getElementById('booking-service-select');
            if (serviceSelect) serviceSelect.value = selectedBookingServiceId;
            nav('book');
            alert('Đã chọn dịch vụ: ' + name + '. Bạn hãy chọn ngày giờ hẹn rồi gửi yêu cầu đặt lịch.');
        }

        // 5. ĐẶT LỊCH
        function selectBookingType(element, type) {
            document.querySelectorAll('.option-card').forEach(c => c.classList.remove('active'));
            element.classList.add('active');
            selectedBookingType = type;
        }


        function getMyCarOwnerKey() {
            return getCurrentUserKey();
        }

        function getMyCarKey() {
            return 'db_my_cars_' + getMyCarOwnerKey();
        }


        function getAllAdminCars() {
            return JSON.parse(localStorage.getItem('db_car')) || [];
        }

        function saveAllAdminCars(cars) {
            localStorage.setItem('db_car', JSON.stringify(cars));
        }

        function normalizePlateText(value) {
            return String(value || '').trim().toUpperCase();
        }

        async function apiJson(url, options = {}) {
            const res = await fetch(url, {
                ...options,
                headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
                cache: 'no-store'
            });
            const data = await res.json().catch(() => ({}));
            if (!res.ok || data.success === false) {
                throw new Error(data.message || data.error || 'API lỗi');
            }
            return Array.isArray(data) ? data : (data.data || data);
        }

        async function getCurrentCustomerForApi() {
            const account = displayCustomerAccount(getCurrentUserAccount() || '');
            const phone = /^0\d{9,10}$/.test(account) ? account : ((user && user.phoneNumber) || '');
            const email = (user && user.email && user.email.includes('@')) ? user.email : (phone ? `${phone}@khachhang.com` : `${getCurrentUserKey()}@khachhang.com`);
            const name = (user && (user.name || user.fullName)) || 'Khách hàng';

            let customers = [];
            try { customers = await apiJson('/api/Customers'); } catch(e) { customers = []; }
            const found = customers.find(c =>
                String(c.phoneNumber || c.PhoneNumber || '').trim() === account ||
                String(c.email || c.Email || '').trim().toLowerCase() === String(email).toLowerCase() ||
                String(c.email || c.Email || '').trim().toLowerCase() === String(account).toLowerCase()
            );
            if (found) return found;

            if (!/^0\d{9,10}$/.test(phone || account)) {
                throw new Error('Tài khoản cần có số điện thoại hợp lệ để lưu xe lên SQL.');
            }

            return await apiJson('/api/Customers', {
                method: 'POST',
                body: JSON.stringify({
                    fullName: name,
                    phoneNumber: phone || account,
                    email,
                    address: '',
                    password: '123456'
                })
            });
        }

        async function readAllCarsFromApi() {
            try {
                const cars = await apiJson('/api/Cars');
                return Array.isArray(cars) ? cars : [];
            } catch(e) {
                return [];
            }
        }

        async function refreshBookingOptions(preferValue = '') {
            const select = document.getElementById('car-select-options');
            const serviceSelect = document.getElementById('booking-service-select');

            if (serviceSelect && cachedBookingServices.length === 0) {
                const services = await getAllServicesForCustomer();
                populateBookingServiceSelect(services);
            }

            if(!select) return;
            let myCars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            try {
                const apiCars = await readAllCarsFromApi();
                const account = displayCustomerAccount(getCurrentUserAccount() || '').toLowerCase();
                const myName = String((user && (user.name || user.fullName)) || '').toLowerCase();
                apiCars.forEach(c => {
                    const carAccount = displayCustomerAccount(c.customerPhone || c.CustomerPhone || c.customerEmail || c.CustomerEmail || '').toLowerCase();
                    const carName = String(c.customerName || c.CustomerName || '').toLowerCase();
                    const belongs = (account && carAccount === account) || (myName && carName === myName);
                    if(!belongs) return;
                    const plate = c.licensePlate || c.LicensePlate || c.f1 || c.plate;
                    if(!plate) return;
                    if(!myCars.find(x => normalizePlateText(x.plate) === normalizePlateText(plate))) {
                        myCars.unshift({
                            id: 'SQL_' + (c.carId || c.CarId || c.id || c.Id),
                            apiCarId: c.carId || c.CarId || c.id || c.Id,
                            customerId: c.customerId || c.CustomerId,
                            ownerUser: getCurrentUserKey(),
                            customerAccount: getCurrentUserAccount() || '',
                            customerName: c.customerName || c.CustomerName || user.name || user.fullName || 'Khách hàng',
                            plate,
                            brand: c.brand || c.Brand || '',
                            model: c.model || c.Model || '',
                            year: c.year || c.Year || '',
                            status: c.status || c.Status || 'Đang hoạt động'
                        });
                    }
                });
                localStorage.setItem(getMyCarKey(), JSON.stringify(myCars));
            } catch(e) {}
            let options = [];

            myCars.forEach(car => {
                options.push({
                    value: 'CAR_' + car.id,
                    text: `${car.brand || ''} ${car.model || ''} - ${car.plate || ''}`,
                    type: 'car'
                });
            });

            if(options.length === 0) {
                options.push({ value: 'NONE', text: 'Chưa có xe - vẫn có thể đặt lịch theo dịch vụ đã chọn', type: 'none' });
            }

            select.innerHTML = options.map(o => `<option value="${o.value}" data-type="${o.type}">${o.text}</option>`).join('');
            if(preferValue) select.value = preferValue;
        }

        function bookCar(carId) {
            const cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            const car = cars.find(c => String(c.id) === String(carId));
            nav('book');
            if(car) {
                refreshBookingOptions('CAR_' + car.id);
                const note = document.getElementById('booking-note');
                if(note && !note.value) note.value = `Đặt lịch cho xe ${car.brand || ''} ${car.model || ''}, biển số ${car.plate || ''}`;
            }
        }

        async function loadMyCars() {
            // Không tạo xe mẫu cho mọi tài khoản nữa.
            // Tài khoản mới sẽ trống xe, khách phải tự thêm xe của mình.
            await refreshBookingOptions();
            let cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            const box = document.getElementById('mycars-list');
            if(!box) return;
            if(cars.length === 0) {
                box.innerHTML = `<div class="card" style="grid-column:1/-1; text-align:center; padding:50px;">
                    <i class="fa-solid fa-car-side" style="font-size:54px; color:#cbd5e1; margin-bottom:16px;"></i>
                    <h3 style="margin-bottom:8px;">Bạn chưa thêm xe nào</h3>
                    <p style="color:var(--text-muted);">Hãy dùng form “Thêm xe mới” bên dưới để thêm xe vào tài khoản này. Xe của khách khác sẽ không hiển thị ở đây.</p>
                </div>`;
                return;
            }
            box.innerHTML = cars.map(car => `
                <div class="mycar-card">
                    <div class="mycar-img" style="background-image:url('${car.img || 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80'}')">
                        <span class="mycar-status">${car.status || 'Đang hoạt động'}</span>
                    </div>
                    <div class="mycar-body">
                        <h3>${car.brand || ''} ${car.model || ''}</h3>
                        <div class="mycar-info">
                            <div><i class="fa-solid fa-id-card"></i> Biển số: <b>${car.plate}</b></div>
                            <div><i class="fa-solid fa-calendar"></i> Năm sản xuất: ${car.year || 'Chưa cập nhật'}</div>
                            <div><i class="fa-solid fa-screwdriver-wrench"></i> Lần gần nhất: ${car.lastService || 'Chưa có'}</div>
                            <div><i class="fa-solid fa-file-invoice-dollar"></i> Hóa đơn: ${car.invoice || 'Không có'}</div>
                        </div>
                        <div style="display:grid; grid-template-columns:1fr 1fr; gap:10px; margin-top:18px;">
                            <button class="btn-buy" style="grid-column:1/-1; width:100%;" onclick="bookCar('${car.id}')"><i class="fa-solid fa-calendar-check"></i> Đặt lịch cho xe này</button>
                            <button class="btn-cart" style="padding:12px;" onclick="editMyCar('${car.id}')"><i class="fa-solid fa-pen-to-square"></i> Sửa</button>
                            <button class="btn-cart" style="padding:12px; color:var(--danger); background:#fee2e2;" onclick="deleteMyCar('${car.id}')"><i class="fa-solid fa-trash"></i> Xóa</button>
                        </div>
                    </div>
                </div>
            `).join('');
        }

        function syncMyCarToAdmin(carRecord) {
            let adminCars = getAllAdminCars();
            const idx = adminCars.findIndex(c => String(c.id) === String(carRecord.id) || (
                String(c.ownerUser || '').toLowerCase() === String(carRecord.ownerUser || '').toLowerCase()
                && String(c.f1 || c.plate || '').trim().toLowerCase() === String(carRecord.plate || '').trim().toLowerCase()
            ));
            const adminRecord = {
                id: carRecord.id,
                f1: carRecord.plate,
                f2: carRecord.customerName,
                f3: `${carRecord.brand || ''} ${carRecord.model || ''} ${carRecord.year || ''}`.trim(),
                status: carRecord.status || 'Đang hoạt động',
                ownerUser: carRecord.ownerUser,
                customerAccount: carRecord.customerAccount,
                customerName: carRecord.customerName,
                plate: carRecord.plate,
                brand: carRecord.brand,
                model: carRecord.model,
                year: carRecord.year
            };
            if(idx >= 0) adminCars[idx] = { ...adminCars[idx], ...adminRecord };
            else adminCars.unshift(adminRecord);
            saveAllAdminCars(adminCars);
        }

        function removeMyCarFromAdmin(carRecord) {
            let adminCars = getAllAdminCars();
            adminCars = adminCars.filter(c => !(String(c.id) === String(carRecord.id) || (
                String(c.ownerUser || '').toLowerCase() === String(carRecord.ownerUser || '').toLowerCase()
                && String(c.f1 || c.plate || '').trim().toLowerCase() === String(carRecord.plate || '').trim().toLowerCase()
            )));
            saveAllAdminCars(adminCars);
        }

        async function editMyCar(carId) {
            let cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            const index = cars.findIndex(c => String(c.id) === String(carId));
            if(index < 0) return alert('Không tìm thấy xe cần sửa!');

            const car = cars[index];
            if(String(car.ownerUser || '') !== getCurrentUserKey()) {
                return alert('Bạn không có quyền sửa xe này!');
            }

            const plate = prompt('Nhập biển số xe:', car.plate || '');
            if(plate === null) return;
            const brand = prompt('Nhập hãng xe:', car.brand || '');
            if(brand === null) return;
            const model = prompt('Nhập dòng xe:', car.model || '');
            if(model === null) return;
            const year = prompt('Nhập năm sản xuất:', car.year || '');
            if(year === null) return;

            const cleanPlate = normalizePlateText(plate);
            const cleanBrand = brand.trim();
            const cleanModel = model.trim();
            const cleanYear = year.trim();
            if(!cleanPlate || !cleanBrand || !cleanModel) {
                return alert('Biển số, hãng xe và dòng xe không được bỏ trống!');
            }

            try {
                const allApiCars = await readAllCarsFromApi();
                const duplicate = allApiCars.find(c => {
                    const apiId = c.carId || c.CarId || c.id || c.Id;
                    const plateApi = normalizePlateText(c.licensePlate || c.LicensePlate || c.plate || c.f1);
                    return plateApi === cleanPlate && String(apiId) !== String(car.apiCarId || '');
                });
                if(duplicate) return alert('Biển số xe này đã được khách hàng khác đăng ký. Không thể sửa trùng biển số.');

                if(car.apiCarId) {
                    const customer = await getCurrentCustomerForApi();
                    await apiJson(`/api/Cars/${car.apiCarId}`, {
                        method: 'PUT',
                        body: JSON.stringify({
                            licensePlate: cleanPlate,
                            brand: cleanBrand,
                            model: cleanModel,
                            year: Number(cleanYear) || new Date().getFullYear(),
                            customerId: customer.id || customer.Id
                        })
                    });
                }
            } catch(e) {
                return alert(e.message || 'Không cập nhật được xe lên SQL.');
            }

            const updatedCar = {
                ...car,
                plate: cleanPlate,
                brand: cleanBrand,
                model: cleanModel,
                year: cleanYear,
                updatedAt: new Date().toLocaleString('vi-VN')
            };
            cars[index] = updatedCar;
            localStorage.setItem(getMyCarKey(), JSON.stringify(cars));
            syncMyCarToAdmin(updatedCar);
            loadMyCars();
            refreshBookingOptions('CAR_' + updatedCar.id);
            alert('Đã cập nhật thông tin xe lên SQL. Admin/Nhân viên sẽ thấy thông tin mới.');
        }

        async function deleteMyCar(carId) {
            let cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            const car = cars.find(c => String(c.id) === String(carId));
            if(!car) return alert('Không tìm thấy xe cần xóa!');
            if(String(car.ownerUser || '') !== getCurrentUserKey()) {
                return alert('Bạn không có quyền xóa xe này!');
            }

            const warning = `Bạn có chắc muốn xóa xe ${car.brand || ''} ${car.model || ''} - ${car.plate || ''}?`;
            if(!confirm(warning)) return;

            try {
                if(car.apiCarId) {
                    await apiJson(`/api/Cars/${car.apiCarId}`, { method: 'DELETE' });
                }
            } catch(e) {
                return alert(e.message || 'Không thể xóa xe trên SQL. Có thể xe đang có lịch hẹn/phiếu sửa.');
            }

            cars = cars.filter(c => String(c.id) !== String(carId));
            localStorage.setItem(getMyCarKey(), JSON.stringify(cars));
            removeMyCarFromAdmin(car);
            loadMyCars();
            refreshBookingOptions();
            alert('Đã xóa xe khỏi SQL và cập nhật lại danh sách xe bên Admin.');
        }

        async function addMyCar() {
            const plate = document.getElementById('new-car-plate').value.trim();
            const brand = document.getElementById('new-car-brand').value.trim();
            const model = document.getElementById('new-car-model').value.trim();
            const year = document.getElementById('new-car-year').value.trim();
            if(!plate || !brand || !model) return alert('Vui lòng nhập biển số, hãng xe và dòng xe!');

            const cleanPlate = normalizePlateText(plate);
            let cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            if(cars.some(c => normalizePlateText(c.plate) === cleanPlate)) {
                return alert('Biển số này đã có trong tài khoản của bạn!');
            }

            try {
                const allApiCars = await readAllCarsFromApi();
                if(allApiCars.some(c => normalizePlateText(c.licensePlate || c.LicensePlate || c.plate || c.f1) === cleanPlate)) {
                    return alert('Biển số xe này đã được khách hàng khác đăng ký. Bạn không thể đăng ký trùng biển số.');
                }
            } catch(e) {}

            let apiCarId = null;
            let customerId = null;
            try {
                const customer = await getCurrentCustomerForApi();
                customerId = customer.id || customer.Id;
                const createdCar = await apiJson('/api/Cars', {
                    method: 'POST',
                    body: JSON.stringify({
                        licensePlate: cleanPlate,
                        brand,
                        model,
                        year: Number(year) || new Date().getFullYear(),
                        customerId
                    })
                });
                apiCarId = createdCar.carId || createdCar.CarId || createdCar.id || createdCar.Id;
            } catch(e) {
                return alert(e.message || 'Không lưu được xe lên SQL. Vui lòng thử lại.');
            }

            const carId = 'CAR' + Date.now();
            const carRecord = {
                id: carId,
                apiCarId,
                customerId,
                ownerUser: getCurrentUserKey(),
                customerAccount: getCurrentUserAccount() || '',
                customerName: user.name || user.fullName || 'Khách hàng',
                plate: cleanPlate,
                brand,
                model,
                year,
                status: 'Đang hoạt động',
                lastService: 'Chưa có lịch sử',
                invoice: 'Không có hóa đơn chờ',
                img: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80'
            };

            cars.unshift(carRecord);
            localStorage.setItem(getMyCarKey(), JSON.stringify(cars));
            syncMyCarToAdmin(carRecord);

            document.getElementById('new-car-plate').value = '';
            document.getElementById('new-car-brand').value = '';
            document.getElementById('new-car-model').value = '';
            document.getElementById('new-car-year').value = '';
            loadMyCars();
            refreshBookingOptions();
            alert('Đã thêm xe vào SQL. Admin máy tính sẽ nhìn thấy xe này trong Quản lý xe.');
        }

        async function submitBooking() {
            const dateInput = document.getElementById('booking-date').value;
            const selectEl = document.getElementById('car-select-options');
            const selectedOption = selectEl && selectEl.options.length ? selectEl.options[selectEl.selectedIndex] : null;
            const carName = selectedOption ? selectedOption.text : 'Chưa chọn xe';
            const selectedValue = selectedOption ? selectedOption.value : 'NONE';
            const selectedService = getSelectedBookingService();
            const note = document.getElementById('booking-note').value;

            if(!dateInput) {
                alert('Vui lòng chọn Ngày & Giờ hẹn!');
                return;
            }

            if(!selectedService) {
                alert('Vui lòng chọn dịch vụ sửa chữa!');
                return;
            }

            const appointmentDate = new Date(dateInput);
            if (appointmentDate <= new Date()) {
                alert('Ngày giờ hẹn phải lớn hơn thời điểm hiện tại!');
                return;
            }

            let selectedCarIdForApi = null;
            let selectedLocalCar = null;
            if (String(selectedValue).startsWith('CAR_')) {
                const localCarId = String(selectedValue).replace('CAR_', '');
                const myCarsForBooking = JSON.parse(localStorage.getItem(getMyCarKey()) || '[]');
                selectedLocalCar = myCarsForBooking.find(c => String(c.id) === localCarId);
                selectedCarIdForApi = selectedLocalCar && selectedLocalCar.apiCarId ? Number(selectedLocalCar.apiCarId) : null;
            }

            try {
                await apiJson('/api/Appointments/customer-request', {
                    method: 'POST',
                    body: JSON.stringify({
                        customerName: user.name || user.fullName || 'Khách hàng',
                        customerAccount: getCurrentUserAccount() || '',
                        customerEmail: user.email || '',
                        type: selectedBookingType,
                        serviceName: selectedService.name,
                        estimatedAmount: Number(selectedService.price) || 0,
                        selectedTarget: selectedValue,
                        carId: selectedCarIdForApi,
                        appointmentDate: dateInput,
                        note: note || `Khách đặt lịch dịch vụ: ${selectedService.name}. Tạm tính: ${Number(selectedService.price || 0).toLocaleString('vi-VN')}đ`
                    })
                });
            } catch(e) {
                alert(e.message || 'Không gửi được lịch hẹn lên SQL. Có thể khung giờ này đã quá nhiều người đặt, vui lòng chọn giờ khác.');
                return;
            }

            const now = new Date().toLocaleString('vi-VN');
            const booking = {
                id: 'BK' + Math.floor(Math.random() * 100000),
                ownerUser: getCurrentUserKey(),
                customerAccount: getCurrentUserAccount() || '',
                customerName: user.name || user.fullName || 'Khách hàng',
                customerEmail: user.email || '',
                type: selectedBookingType,
                carService: selectedService.name,
                serviceId: selectedService.id,
                serviceName: selectedService.name,
                estimatedAmount: Number(selectedService.price) || 0,
                selectedTarget: selectedValue,
                carInfo: carName,
                date: dateInput.replace('T', ' '),
                note: note || `Khách đặt lịch dịch vụ: ${selectedService.name}. Tạm tính: ${Number(selectedService.price || 0).toLocaleString('vi-VN')}đ`,
                status: 'Chờ Gara xác nhận',
                rejectionReason: '',
                rejectedAt: '',
                approvedAt: '',
                createdAt: now
            };

            let bookings = JSON.parse(localStorage.getItem('db_booking') || '[]');
            bookings.push(booking);
            localStorage.setItem('db_booking', JSON.stringify(bookings));
            loadMyBookings();

            // Tạo hóa đơn tạm tính để khách có thể quét QR.
            // Lưu ý: bấm “Đã chuyển khoản” chỉ chuyển sang trạng thái CHỜ ADMIN XÁC NHẬN,
            // không tự chuyển thành hoàn tất.
            const invoice = createPendingInvoiceForBooking(booking, selectedService);

            document.getElementById('booking-date').value = '';
            document.getElementById('booking-note').value = '';
            updateStats();

            if (invoice) {
                alert('Gửi lịch hẹn thành công! Hệ thống sẽ mở QR thanh toán tạm tính. Sau khi chuyển khoản, hóa đơn sẽ chờ Admin/Nhân viên xác nhận.');
                openQrPayment({ ...invoice, clearCart: false });
            } else {
                alert('Gửi yêu cầu đặt lịch thành công! Dịch vụ chưa có giá nên chưa thể tạo QR thanh toán.');
                nav('book');
            }
        }

        function getBookingBadgeClass(status) {
            const st = String(status || '').toLowerCase();
            if(st.includes('từ chối')) return 'rejected';
            if(st.includes('chờ')) return 'pending';
            return 'done';
        }

        async function loadMyBookings() {
            const container = document.getElementById('my-bookings-list');
            if(!container) return;

            let localBookings = JSON.parse(localStorage.getItem('db_booking')) || [];
            let apiAppointments = [];

            try { apiAppointments = await apiJson('/api/Appointments'); } catch(e) { apiAppointments = []; }

            const apiBookings = apiAppointments.map(a => {
                const customerAccount = displayCustomerAccount(a.customerAccount || a.CustomerAccount || a.customerPhone || a.CustomerPhone || a.customerEmail || a.CustomerEmail || '');
                const customerName = a.customerName || a.CustomerName || 'Khách hàng';
                return {
                    id: 'SQL_APP_' + (a.appointmentId || a.AppointmentId || a.id || a.Id),
                    appointmentId: a.appointmentId || a.AppointmentId || a.id || a.Id,
                    ownerUser: normalizeUserKey(customerAccount || customerName),
                    customerAccount,
                    customerName,
                    customerEmail: a.customerEmail || a.CustomerEmail || '',
                    type: a.type || a.Type || 'Lịch hẹn dịch vụ',
                    carService: a.serviceName || a.ServiceName || a.carInfo || a.CarInfo || 'Dịch vụ sửa chữa',
                    date: a.date || a.Date || a.appointmentDate || a.AppointmentDate || '',
                    note: a.note || a.Note || '',
                    status: a.status || a.Status || 'Chờ xác nhận',
                    rejectionReason: a.rejectionReason || a.RejectionReason || '',
                    rejectedAt: a.rejectedAt || a.RejectedAt || '',
                    createdAt: a.createdAt || a.CreatedAt || ''
                };
            });

            const byId = new Map();
            localBookings.forEach(b => byId.set(String(b.id), b));
            apiBookings.forEach(b => byId.set(String(b.id), { ...(byId.get(String(b.id)) || {}), ...b, sqlSynced: true }));
            const myBookings = Array.from(byId.values()).filter(b => isMine(b));

            if(myBookings.length === 0) {
                container.innerHTML = `<div style="text-align:center; color:var(--text-muted); padding:30px 10px;">
                    <i class="fa-regular fa-calendar" style="font-size:42px; color:#cbd5e1; margin-bottom:12px;"></i>
                    <p>Bạn chưa có lịch hẹn nào.</p>
                </div>`;
                return;
            }

            container.innerHTML = myBookings.reverse().map(b => {
                const badgeClass = getBookingBadgeClass(b.status);
                const isRejected = String(b.status || '').toLowerCase().includes('từ chối');
                const reasonHtml = isRejected
                    ? `<div class="reject-reason-box"><b>Lý do từ chối:</b><br>${b.rejectionReason || 'Admin/Nhân viên chưa nhập lý do.'}</div>`
                    : '';
                return `<div class="booking-history-card">
                    <div style="display:flex; justify-content:space-between; gap:14px; align-items:flex-start;">
                        <div>
                            <b style="font-size:16px; color:var(--text-main);">${b.type || 'Lịch hẹn dịch vụ'}</b>
                            <p style="color:var(--text-muted); margin-top:6px;"><i class="fa-regular fa-clock"></i> ${formatDateSafe(b.date) || 'Chưa có ngày hẹn'}</p>
                            <p style="color:var(--text-muted); margin-top:6px;"><i class="fa-solid fa-car"></i> ${b.carService || 'Chưa chọn xe/dịch vụ'}</p>
                            ${b.note ? `<p style="color:var(--text-muted); margin-top:6px;"><i class="fa-solid fa-note-sticky"></i> ${b.note}</p>` : ''}
                        </div>
                        <span class="booking-status ${badgeClass}">${b.status || 'Chờ xác nhận'}</span>
                    </div>
                    ${reasonHtml}
                </div>`;
            }).join('');
        }

        // 6. HIỂN THỊ LỊCH SỬ ĐƠN HÀNG
        async function loadOrders() {
            const container = document.getElementById('user-orders');
            if(!container) return;

            let localOrders = JSON.parse(localStorage.getItem('db_order')) || [];
            let apiInvoices = [];
            let apiPayments = [];

            try { apiInvoices = await apiJson('/api/Invoices'); } catch(e) { apiInvoices = []; }
            try { apiPayments = await apiJson('/api/Payments'); } catch(e) { apiPayments = []; }

            const fromSql = [];
            apiInvoices.forEach(inv => {
                const payments = inv.payments || inv.Payments || [];
                const latestPayment = payments[0] || {};
                const localOrderId = inv.localOrderId || inv.LocalOrderId || latestPayment.localOrderId || latestPayment.LocalOrderId || '';
                const customerAccount = displayCustomerAccount(inv.customerAccount || inv.CustomerAccount || latestPayment.customerAccount || latestPayment.CustomerAccount || '');
                const customerName = inv.customerName || inv.CustomerName || latestPayment.customerName || latestPayment.CustomerName || 'Khách hàng';
                const serviceName = inv.serviceName || inv.ServiceName || latestPayment.serviceName || latestPayment.ServiceName || 'Hóa đơn dịch vụ';
                const latestPaymentStatus = inv.latestPaymentStatus || inv.LatestPaymentStatus || latestPayment.status || latestPayment.Status || '';
                const rejectReason = inv.rejectReason || inv.RejectReason || latestPayment.rejectReason || latestPayment.RejectReason || '';

                fromSql.push({
                    id: localOrderId || ('SQL_HD_' + (inv.invoiceId || inv.InvoiceId || inv.id || inv.Id)),
                    invoiceId: inv.invoiceId || inv.InvoiceId || inv.id || inv.Id,
                    paymentId: inv.latestPaymentId || inv.LatestPaymentId || latestPayment.paymentId || latestPayment.PaymentId,
                    ownerUser: normalizeUserKey(customerAccount || customerName),
                    customerAccount,
                    customerName,
                    f1: serviceName,
                    f2: customerName,
                    f3: inv.totalAmount || inv.TotalAmount || inv.amount || inv.Amount || 0,
                    f4: inv.createdAt || inv.CreatedAt || '',
                    status: inv.status || inv.Status || inv.invoiceStatus || inv.InvoiceStatus || 'Chưa thanh toán',
                    paymentStatus: latestPaymentStatus,
                    paymentMethod: inv.latestPaymentMethod || inv.LatestPaymentMethod || latestPayment.paymentMethod || latestPayment.PaymentMethod || '',
                    requestedAt: latestPayment.paymentDate || latestPayment.PaymentDate || '',
                    paidAt: latestPayment.confirmedAt || latestPayment.ConfirmedAt || '',
                    rejectionReason: rejectReason
                });
            });

            // Nếu API Payments có giao dịch chưa nằm trong /api/Invoices thì merge thêm.
            apiPayments.forEach(p => {
                const localOrderId = p.localOrderId || p.LocalOrderId || '';
                const exists = fromSql.find(o => String(o.paymentId) === String(p.paymentId || p.PaymentId) || (localOrderId && String(o.id) === String(localOrderId)));
                if(exists) return;

                const customerAccount = displayCustomerAccount(p.customerAccount || p.CustomerAccount || '');
                const customerName = p.customerName || p.CustomerName || 'Khách hàng';
                fromSql.push({
                    id: localOrderId || ('SQL_PAY_' + (p.paymentId || p.PaymentId)),
                    invoiceId: p.invoiceId || p.InvoiceId,
                    paymentId: p.paymentId || p.PaymentId,
                    ownerUser: normalizeUserKey(customerAccount || customerName),
                    customerAccount,
                    customerName,
                    f1: p.serviceName || p.ServiceName || 'Hóa đơn dịch vụ',
                    f2: customerName,
                    f3: p.amount || p.Amount || 0,
                    f4: p.paymentDate || p.PaymentDate || '',
                    status: p.status || p.Status || 'Chờ xác nhận thanh toán QR',
                    paymentStatus: p.status || p.Status || '',
                    paymentMethod: p.paymentMethod || p.PaymentMethod || '',
                    requestedAt: p.paymentDate || p.PaymentDate || '',
                    paidAt: p.confirmedAt || p.ConfirmedAt || '',
                    rejectionReason: p.rejectReason || p.RejectReason || ''
                });
            });

            // Ưu tiên dữ liệu SQL, nhưng vẫn giữ localStorage làm dự phòng cho hóa đơn chưa sync.
            const byId = new Map();
            localOrders.forEach(o => byId.set(String(o.id), o));
            fromSql.forEach(o => byId.set(String(o.id), { ...(byId.get(String(o.id)) || {}), ...o, sqlSynced: true }));
            const orders = Array.from(byId.values());
            window.latestCustomerOrdersCache = orders;

            const myOrders = orders.filter(o => isMine(o));
            if(myOrders.length === 0) {
                container.innerHTML = `<div class="card" style="text-align: center; color: var(--text-muted); padding: 60px;">
                    <i class="fa-solid fa-receipt" style="font-size: 50px; color: #cbd5e1; margin-bottom: 20px;"></i>
                    <h3 style="color: var(--text-main); margin-bottom: 10px;">Chưa có lịch sử sửa chữa / hóa đơn</h3>
                    <p>Bạn chưa có hồ sơ sửa chữa hoặc hóa đơn nào. Hãy đặt lịch dịch vụ trước nhé.</p>
                </div>`;
                return;
            }

            container.innerHTML = myOrders.reverse().map(ord => {
                const st = String(ord.status || '').toLowerCase();
                const paySt = String(ord.paymentStatus || '').toLowerCase();
                const isRejected = st.includes('từ chối') || paySt.includes('từ chối') || st.includes('hủy') || paySt.includes('hủy');
                const isPaid = !isRejected && (st.includes('đã thanh toán') || st.includes('hoàn tất') || paySt.includes('đã xác nhận') || paySt.includes('đã thanh toán'));
                const isPendingQr = !isPaid && !isRejected && (st.includes('chờ xác nhận thanh toán qr') || st.includes('chờ xác nhận') || paySt.includes('chờ xác nhận') || paySt.includes('chờ admin'));
                const isUnpaid = !isPaid && !isPendingQr && !isRejected;

                let statusColor = isPaid ? 'var(--success)' : (isPendingQr ? 'var(--warning)' : (isRejected ? 'var(--danger)' : 'var(--danger)'));
                let statusBg = isPaid ? '#dcfce7' : (isPendingQr ? 'var(--orange-light)' : '#fee2e2');
                let statusIcon = isPaid ? 'fa-circle-check' : (isPendingQr ? 'fa-clock-rotate-left' : (isRejected ? 'fa-circle-xmark' : 'fa-qrcode'));
                let statusText = isPaid ? 'ĐÃ HOÀN TẤT' : (isPendingQr ? 'CHỜ ADMIN XÁC NHẬN QR' : (isRejected ? 'ĐÃ TỪ CHỐI' : 'CHƯA THANH TOÁN'));
                const payButton = isUnpaid ? `<button onclick="payInvoiceById('${ord.id}')" style="margin-top:10px; border:none; background:var(--primary); color:white; padding:10px 14px; border-radius:10px; font-weight:900; cursor:pointer;"><i class="fa-solid fa-qrcode"></i> Thanh toán QR</button>` : '';
                const reasonHtml = isRejected && ord.rejectionReason ? `<div class="reject-reason-box" style="margin-top:10px;"><b>Lý do từ chối:</b><br>${ord.rejectionReason}</div>` : '';

                return `
                <div class="order-card">
                    <div style="display: flex; gap: 20px; align-items: center;">
                        <div style="width: 55px; height: 55px; background: var(--bg); color: var(--text-main); border-radius: 14px; display: flex; align-items: center; justify-content: center; font-size: 24px; border: 1px solid var(--border);">
                            <i class="fa-solid fa-file-invoice-dollar"></i>
                        </div>
                        <div>
                            <span style="font-weight: 800; color: var(--text-main); font-size: 17px; display: block; margin-bottom: 5px;">${ord.f1 || ord.serviceName || 'Hóa đơn dịch vụ'}</span>
                            <p style="font-size: 13px; color: var(--text-muted); font-weight: 500;"><i class="fa-regular fa-calendar" style="margin-right: 5px;"></i> Ngày tạo: ${formatDateSafe(ord.f4)}</p>
                            ${ord.paymentMethod ? `<p style="font-size: 13px; color: var(--primary); font-weight: 800; margin-top: 4px;"><i class="fa-solid fa-building-columns" style="margin-right: 5px;"></i> ${ord.paymentMethod}</p>` : ''}
                            ${ord.paymentStatus ? `<p style="font-size: 13px; color: ${isRejected ? 'var(--danger)' : (isPaid ? 'var(--success)' : 'var(--warning)')}; font-weight: 800; margin-top: 4px;"><i class="fa-solid ${isPaid ? 'fa-circle-check' : (isRejected ? 'fa-circle-xmark' : 'fa-clock-rotate-left')}" style="margin-right: 5px;"></i> ${ord.paymentStatus}</p>` : ''}
                            ${ord.paidAt ? `<p style="font-size: 12px; color: var(--text-muted); font-weight: 600; margin-top: 4px;">Xác nhận lúc: ${formatDateSafe(ord.paidAt)}</p>` : ''}
                            ${ord.requestedAt ? `<p style="font-size: 12px; color: var(--text-muted); font-weight: 600; margin-top: 4px;">Gửi xác nhận lúc: ${formatDateSafe(ord.requestedAt)}</p>` : ''}
                            ${reasonHtml}
                        </div>
                    </div>
                    <div style="text-align: right;">
                        <b style="display: block; font-size: 22px; font-weight: 900; color: var(--primary); margin-bottom: 8px;">${Number(ord.f3 || 0).toLocaleString('vi-VN')}đ</b>
                        <span style="font-size: 11px; color: ${statusColor}; font-weight: 800; display: inline-block; background: ${statusBg}; padding: 6px 12px; border-radius: 8px; letter-spacing: 0.5px;">
                            <i class="fa-solid ${statusIcon}" style="margin-right: 4px;"></i> ${statusText}
                        </span>
                        ${payButton}
                    </div>
                </div>`;
            }).join('');
        }

        function formatDateSafe(value) {
            if(!value) return 'Chưa có';
            const d = new Date(value);
            if(!isNaN(d.getTime())) return d.toLocaleString('vi-VN');
            return value;
        }



        function payInvoiceById(orderId) {
            const localOrders = JSON.parse(localStorage.getItem('db_order')) || [];
            const cachedOrders = Array.isArray(window.latestCustomerOrdersCache) ? window.latestCustomerOrdersCache : [];
            const allOrders = [...cachedOrders, ...localOrders];

            const invoice = allOrders.find(o =>
                String(o.id) === String(orderId) ||
                String(o.invoiceId || '') === String(orderId) ||
                String(o.paymentId || '') === String(orderId)
            );

            if(!invoice || !isMine(invoice)) return alert('Không tìm thấy hóa đơn của bạn.');
            const amount = Number(invoice.f3 || invoice.totalAmount || invoice.amount || 0);
            if(amount <= 0) return alert('Hóa đơn này chưa có số tiền chính thức, vui lòng chờ gara cập nhật.');

            openQrPayment({
                ...invoice,
                id: invoice.id || invoice.localOrderId || ('SQL_HD_' + (invoice.invoiceId || invoice.paymentId || Date.now())),
                f3: amount
            });
        }

        // 7. CẬP NHẬT THỐNG KÊ HOẠT ĐỘNG
        function updateStats() {
            const orders = JSON.parse(localStorage.getItem('db_order')) || [];
            const bookings = JSON.parse(localStorage.getItem('db_booking')) || [];
            const cart = JSON.parse(localStorage.getItem(getCartKey())) || [];

            const myOrders = orders.filter(o => isMine(o));
            const myBookings = bookings.filter(b => isMine(b));

            document.getElementById('stat-orders').innerText = myOrders.length;
            document.getElementById('stat-bookings').innerText = myBookings.length;
            document.getElementById('stat-cart').innerText = cart.length;
            
            document.getElementById('header-cart-count').innerText = cart.length;

            const recentActContainer = document.getElementById('recent-activity');
            let activities = [];
            
            myOrders.slice(-2).forEach(o => activities.push({ title: `Yêu cầu dịch vụ: ${o.f1}`, time: o.f4, icon: 'fa-bag-shopping', color: 'var(--primary)' }));
            myBookings.slice(-2).forEach(b => {
                const rejected = String(b.status || '').includes('từ chối') || String(b.status || '').includes('Từ chối');
                activities.push({
                    title: rejected ? `Lịch hẹn bị từ chối: ${b.rejectionReason || b.type}` : `Đã đặt lịch: ${b.type}`,
                    time: rejected ? (b.rejectedAt || b.createdAt) : b.createdAt,
                    icon: rejected ? 'fa-circle-xmark' : 'fa-calendar-check',
                    color: rejected ? 'var(--danger)' : 'var(--warning)'
                });
            });
            
            if(activities.length === 0) {
                recentActContainer.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 30px 20px;">Bạn chưa có hoạt động giao dịch hoặc đặt lịch nào được ghi nhận.</p>';
            } else {
                recentActContainer.innerHTML = activities.reverse().slice(0, 3).map(act => `
                    <div style="display: flex; gap: 18px; margin-bottom: 18px; padding-bottom: 18px; border-bottom: 1px dashed var(--border);">
                        <div style="width: 45px; height: 45px; border-radius: 14px; background: var(--bg); color: ${act.color}; display: flex; align-items: center; justify-content: center; font-size: 18px; border: 1px solid var(--border);">
                            <i class="fa-solid ${act.icon}"></i>
                        </div>
                        <div style="display: flex; flex-direction: column; justify-content: center;">
                            <b style="font-size: 15px; color: var(--text-main); font-weight: 700;">${act.title}</b>
                            <p style="font-size: 13px; color: var(--text-muted); margin-top: 4px; font-weight: 500;"><i class="fa-regular fa-clock" style="margin-right: 5px;"></i> ${act.time}</p>
                        </div>
                    </div>
                `).join('');
            }
        }

        // Khởi động
        window.onload = () => {
            syncUserInfo();
            loadProducts();
            loadMyCars();
            refreshBookingOptions();
            loadMyBookings();
            loadOrders();
        };
