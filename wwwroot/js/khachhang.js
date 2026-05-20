// 1. KIỂM TRA ĐĂNG NHẬP
        const user = JSON.parse(sessionStorage.getItem('KKTH_ACTIVE_USER') || localStorage.getItem('KKTH_ACTIVE_USER')) || JSON.parse(sessionStorage.getItem('activeUser')) || { name: 'Khách Demo', user: 'khach@demo.com' }; 
        
        let selectedBookingType = 'Xem xe tại showroom';

        function syncUserInfo() {
            if(!user) return;
            const userName = user.name || user.fullName || "Khách hàng";
            const userEmail = user.user || user.email || "Email trống";

            document.getElementById('nav-name').innerText = userName;
            document.getElementById('welcome-msg').innerText = `Xin chào, ${userName}!`;
            document.getElementById('prof-fullname').innerText = userName;
            document.getElementById('prof-display-name').value = userName;
            document.getElementById('prof-user').value = userEmail;
            
            updateStats();
        }

        function logoutCustomer(event) {
            if (event) {
                event.preventDefault();
                event.stopPropagation();
            }

            if(confirm("Bạn muốn đăng xuất khỏi tài khoản?")) {
                // Chỉ xóa thông tin đăng nhập, không xóa dữ liệu xe/dịch vụ/hóa đơn của hệ thống
                sessionStorage.removeItem('KKTH_ACTIVE_USER');
                sessionStorage.removeItem('activeUser');
                sessionStorage.removeItem('currentUser');
                localStorage.removeItem('KKTH_ACTIVE_USER');
                localStorage.removeItem('activeUser');
                localStorage.removeItem('currentUser');

                window.location.href = '/login.html';
            }
        }



        // Bảo đảm nút đăng xuất vẫn bấm được trên cả máy tính và điện thoại
        document.addEventListener('DOMContentLoaded', function () {
            const logoutBtn = document.getElementById('customerLogoutBtn');
            if (logoutBtn) {
                logoutBtn.addEventListener('click', logoutCustomer);
                logoutBtn.addEventListener('touchend', function (event) {
                    event.preventDefault();
                    logoutCustomer(event);
                }, { passive: false });
            }
        });
        function getCurrentUserKey() {
            const raw = (user && (user.user || user.email || user.phoneNumber || user.name)) || 'guest';
            return String(raw).trim().toLowerCase().replace(/[^a-z0-9@._-]/g, '_');
        }

        function getCartKey() {
            return 'db_cart_' + getCurrentUserKey();
        }

        function isMine(record) {
            const key = getCurrentUserKey();
            const account = String((user && (user.user || user.email || user.phoneNumber)) || '').trim().toLowerCase();
            const name = String((user && (user.name || user.fullName)) || '').trim().toLowerCase();
            if (!record) return false;
            if (String(record.ownerUser || '').trim().toLowerCase() === key) return true;
            if (account && String(record.customerAccount || record.customerEmail || record.user || '').trim().toLowerCase() === account) return true;
            // Chỉ fallback theo tên nếu record chưa có khóa tài khoản và tên khớp tuyệt đối
            if (!record.ownerUser && !record.customerAccount && name && String(record.f2 || record.customerName || '').trim().toLowerCase() === name) return true;
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

        async function readServicesFromApi() {
            const endpoints = [
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
            const localKeys = [
                'db_service',      // key Admin đang dùng nhiều nhất
                'db_services',
                'services',
                'db_repair_services',
                'db_repairServices',
                'db_product'
            ];

            let rawServices = [];
            localKeys.forEach(key => {
                rawServices = rawServices.concat(safeParseArray(key));
            });

            // Nếu backend/API có dịch vụ thì merge thêm vào.
            const apiServices = await readServicesFromApi();
            rawServices = rawServices.concat(apiServices);

            // Xóa trùng theo id/code/name.
            const seen = new Set();
            let services = rawServices
                .filter(isRepairService)
                .map(normalizeService)
                .filter(s => {
                    const k = String(s.id || s.code || s.name).toLowerCase();
                    if (seen.has(k)) return false;
                    seen.add(k);
                    return true;
                });

            // Dữ liệu mẫu để trang khách hàng không bị trống khi mới chạy project.
            if (services.length === 0) {
                services = [
                    { id: 'DV001', name: 'Thay dầu máy', code: 'DV001', price: 350000, desc: 'Thay dầu động cơ, kiểm tra lọc dầu và tình trạng vận hành.' },
                    { id: 'DV002', name: 'Rửa xe cao cấp', code: 'DV002', price: 80000, desc: 'Vệ sinh ngoại thất, làm sạch kính, mâm và thân xe.' },
                    { id: 'DV003', name: 'Kiểm tra phanh', code: 'DV003', price: 150000, desc: 'Kiểm tra má phanh, dầu phanh và độ an toàn hệ thống phanh.' },
                    { id: 'DV004', name: 'Bảo dưỡng định kỳ', code: 'DV004', price: 800000, desc: 'Kiểm tra tổng quát xe theo quy trình bảo dưỡng garage.' },
                    { id: 'DV005', name: 'Phủ Ceramic cao cấp', code: 'DV005', price: 5000000, desc: 'Bảo vệ sơn xe, tăng độ bóng và hạn chế bám bẩn.' }
                ];
            }

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
        // 4. CHỨC NĂNG GIỎ HÀNG
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
            const content = `THANH TOAN ${order.id}`;
            document.getElementById('qrOrderId').innerText = order.id;
            document.getElementById('qrAmount').innerText = Number(order.f3).toLocaleString('vi-VN') + 'đ';
            document.getElementById('qrContent').innerText = content;
            document.getElementById('qrPaymentImage').src = buildVietQrUrl(order.f3, content);
            document.getElementById('qrPaymentModal').classList.add('active');
        }

        function cancelQrPayment() {
            pendingQrOrder = null;
            document.getElementById('qrPaymentModal').classList.remove('active');
        }

        function confirmQrPayment() {
            if(!pendingQrOrder) return;
            let orders = JSON.parse(localStorage.getItem('db_order')) || [];
            const now = new Date().toLocaleString('vi-VN');
            const existingIndex = orders.findIndex(o => String(o.id) === String(pendingQrOrder.id));
            const updatedOrder = {
                ...pendingQrOrder,
                ownerUser: pendingQrOrder.ownerUser || getCurrentUserKey(),
                customerAccount: pendingQrOrder.customerAccount || user.user || user.email || user.phoneNumber || '',
                customerName: pendingQrOrder.customerName || user.name || user.fullName || 'Khách hàng',
                paymentMethod: 'Chuyển khoản VCB',
                paymentStatus: 'Chờ Admin xác nhận chuyển khoản QR VCB',
                status: 'Chờ xác nhận thanh toán QR',
                requestedAt: now,
                bankName: 'VCB',
                bankAccount: BANK_CONFIG.accountNumber,
                bankOwner: BANK_CONFIG.accountName
            };

            if(existingIndex >= 0) orders[existingIndex] = updatedOrder;
            else orders.push(updatedOrder);

            localStorage.setItem('db_order', JSON.stringify(orders));

            if(pendingQrOrder.clearCart) {
                localStorage.setItem(getCartKey(), JSON.stringify([]));
                closeCartModal();
            }

            document.getElementById('qrPaymentModal').classList.remove('active');
            alert('Đã gửi yêu cầu xác nhận thanh toán QR. Hóa đơn của tôi đang chờ Admin/Nhân viên kiểm tra giao dịch.');
            pendingQrOrder = null;
            loadOrders();
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
                customerAccount: user.user || user.email || user.phoneNumber || '',
                customerName: user.name || user.fullName || 'Khách hàng',
                customerEmail: user.user || user.email || '',
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
            addToCart(id, name, price);
            closeCartModal();
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

        function refreshBookingOptions(preferValue = '') {
            const select = document.getElementById('car-select-options');
            if(!select) return;
            const myCars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            const selectedServices = JSON.parse(localStorage.getItem(getCartKey())) || [];
            let options = [];

            myCars.forEach(car => {
                options.push({
                    value: 'CAR_' + car.id,
                    text: `${car.brand || ''} ${car.model || ''} - ${car.plate || ''}`,
                    type: 'car'
                });
            });

            selectedServices.forEach(item => {
                options.push({
                    value: 'SV_' + item.id,
                    text: `Dịch vụ đã chọn: ${item.name}`,
                    type: 'service'
                });
            });

            if(options.length === 0) {
                options.push({ value: 'NONE', text: 'Chưa có xe/dịch vụ - vui lòng thêm xe hoặc chọn dịch vụ trước', type: 'none' });
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

        function loadMyCars() {
            // Không tạo xe mẫu cho mọi tài khoản nữa.
            // Tài khoản mới sẽ trống xe, khách phải tự thêm xe của mình.
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

        function editMyCar(carId) {
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

            const cleanPlate = plate.trim();
            const cleanBrand = brand.trim();
            const cleanModel = model.trim();
            const cleanYear = year.trim();
            if(!cleanPlate || !cleanBrand || !cleanModel) {
                return alert('Biển số, hãng xe và dòng xe không được bỏ trống!');
            }
            if(cars.some(c => String(c.id) !== String(carId) && String(c.plate).trim().toLowerCase() === cleanPlate.toLowerCase())) {
                return alert('Biển số này đã có trong tài khoản của bạn!');
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
            alert('Đã cập nhật thông tin xe. Admin/Nhân viên cũng sẽ thấy thông tin mới.');
        }

        function deleteMyCar(carId) {
            let cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            const car = cars.find(c => String(c.id) === String(carId));
            if(!car) return alert('Không tìm thấy xe cần xóa!');
            if(String(car.ownerUser || '') !== getCurrentUserKey()) {
                return alert('Bạn không có quyền xóa xe này!');
            }

            const bookings = JSON.parse(localStorage.getItem('db_booking')) || [];
            const orders = JSON.parse(localStorage.getItem('db_order')) || [];
            const relatedBookings = bookings.filter(b => isMine(b) && String(b.selectedTarget || '').includes(car.id));
            const relatedOrders = orders.filter(o => isMine(o) && (String(o.plate || '').toLowerCase() === String(car.plate || '').toLowerCase() || String(o.carId || '') === String(car.id)));
            let warning = `Bạn có chắc muốn xóa xe ${car.brand || ''} ${car.model || ''} - ${car.plate || ''}?`;
            if(relatedBookings.length || relatedOrders.length) {
                warning += `\n\nXe này đang có ${relatedBookings.length} lịch hẹn và ${relatedOrders.length} hóa đơn liên quan. Lịch sử cũ vẫn giữ nguyên nhưng xe sẽ bị xóa khỏi mục Xe của tôi.`;
            }
            if(!confirm(warning)) return;

            cars = cars.filter(c => String(c.id) !== String(carId));
            localStorage.setItem(getMyCarKey(), JSON.stringify(cars));
            removeMyCarFromAdmin(car);
            loadMyCars();
            refreshBookingOptions();
            alert('Đã xóa xe khỏi tài khoản của bạn và cập nhật lại danh sách xe bên Admin.');
        }

        function addMyCar() {
            const plate = document.getElementById('new-car-plate').value.trim();
            const brand = document.getElementById('new-car-brand').value.trim();
            const model = document.getElementById('new-car-model').value.trim();
            const year = document.getElementById('new-car-year').value.trim();
            if(!plate || !brand || !model) return alert('Vui lòng nhập biển số, hãng xe và dòng xe!');

            const carId = 'CAR' + Date.now();
            const carRecord = {
                id: carId,
                ownerUser: getCurrentUserKey(),
                customerAccount: user.user || user.email || user.phoneNumber || '',
                customerName: user.name || user.fullName || 'Khách hàng',
                plate,
                brand,
                model,
                year,
                status: 'Đang hoạt động',
                lastService: 'Chưa có lịch sử',
                invoice: 'Không có hóa đơn chờ',
                img: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80'
            };

            let cars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
            if(cars.some(c => String(c.plate).trim().toLowerCase() === plate.toLowerCase())) {
                return alert('Biển số này đã có trong tài khoản của bạn!');
            }
            cars.unshift(carRecord);
            localStorage.setItem(getMyCarKey(), JSON.stringify(cars));

            // Đồng bộ sang Admin → Quản lý xe để nhân viên thấy xe khách vừa thêm.
            syncMyCarToAdmin(carRecord);

            document.getElementById('new-car-plate').value = '';
            document.getElementById('new-car-brand').value = '';
            document.getElementById('new-car-model').value = '';
            document.getElementById('new-car-year').value = '';
            loadMyCars();
            refreshBookingOptions();
            alert('Đã thêm xe vào mục Xe của tôi và đồng bộ sang Admin.');
        }

        function submitBooking() {
            const dateInput = document.getElementById('booking-date').value;
            const selectEl = document.getElementById('car-select-options');
            const selectedOption = selectEl.options[selectEl.selectedIndex];
            const carName = selectedOption ? selectedOption.text : 'Chưa chọn xe/dịch vụ';
            const selectedValue = selectedOption ? selectedOption.value : '';
            const note = document.getElementById('booking-note').value;

            if(!dateInput) {
                alert("Vui lòng chọn Ngày & Giờ hẹn!"); return;
            }

            let bookings = JSON.parse(localStorage.getItem('db_booking')) || [];
            bookings.push({
                id: 'BK' + Math.floor(Math.random() * 10000),
                ownerUser: getCurrentUserKey(),
                customerAccount: user.user || user.email || user.phoneNumber || '',
                customerName: user.name || user.fullName || 'Khách hàng',
                customerEmail: user.user || user.email || '',
                type: selectedBookingType,
                carService: carName,
                selectedTarget: selectedValue,
                date: dateInput.replace('T', ' '),
                note: note,
                status: 'Chờ Gara xác nhận',
                rejectionReason: '',
                rejectedAt: '',
                approvedAt: '',
                createdAt: new Date().toLocaleString('vi-VN')
            });

            localStorage.setItem('db_booking', JSON.stringify(bookings));
            loadMyBookings();
            
            alert("Gửi yêu cầu đặt lịch thành công! Nhân viên Gara sẽ sớm liên hệ xác nhận.");
            document.getElementById('booking-date').value = '';
            document.getElementById('booking-note').value = '';
            updateStats();
            nav('home');
        }

        function getBookingBadgeClass(status) {
            const st = String(status || '').toLowerCase();
            if(st.includes('từ chối')) return 'rejected';
            if(st.includes('chờ')) return 'pending';
            return 'done';
        }

        function loadMyBookings() {
            const bookings = JSON.parse(localStorage.getItem('db_booking')) || [];
            const container = document.getElementById('my-bookings-list');
            if(!container) return;
            const myBookings = bookings.filter(b => isMine(b));

            if(myBookings.length === 0) {
                container.innerHTML = `<div style="text-align:center; color:var(--text-muted); padding:30px 10px;">
                    <i class="fa-regular fa-calendar" style="font-size:42px; color:#cbd5e1; margin-bottom:12px;"></i>
                    <p>Bạn chưa có lịch hẹn nào.</p>
                </div>`;
                return;
            }

            container.innerHTML = myBookings.reverse().map(b => {
                const badgeClass = getBookingBadgeClass(b.status);
                const isRejected = String(b.status || '').includes('từ chối') || String(b.status || '').includes('Từ chối');
                const reasonHtml = isRejected
                    ? `<div class="reject-reason-box"><b>Lý do từ chối:</b><br>${b.rejectionReason || 'Admin/Nhân viên chưa nhập lý do.'}</div>`
                    : '';
                return `<div class="booking-history-card">
                    <div style="display:flex; justify-content:space-between; gap:14px; align-items:flex-start;">
                        <div>
                            <b style="font-size:16px; color:var(--text-main);">${b.type || 'Lịch hẹn dịch vụ'}</b>
                            <p style="color:var(--text-muted); margin-top:6px;"><i class="fa-regular fa-clock"></i> ${b.date || 'Chưa có ngày hẹn'}</p>
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
        function loadOrders() {
            const orders = JSON.parse(localStorage.getItem('db_order')) || [];
            const container = document.getElementById('user-orders');
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
                const isPaid = st.includes('đã') || st.includes('hoàn tất') || paySt.includes('đã được');
                const isPendingQr = st.includes('chờ xác nhận thanh toán qr') || paySt.includes('chờ admin');
                const isUnpaid = !isPaid && !isPendingQr;
                let statusColor = isPaid ? 'var(--success)' : (isPendingQr ? 'var(--warning)' : 'var(--danger)');
                let statusBg = isPaid ? '#dcfce7' : (isPendingQr ? 'var(--orange-light)' : '#fee2e2');
                let statusIcon = isPaid ? 'fa-circle-check' : (isPendingQr ? 'fa-clock-rotate-left' : 'fa-qrcode');
                let statusText = isPaid ? 'ĐÃ HOÀN TẤT' : (isPendingQr ? 'CHỜ XÁC NHẬN QR' : 'CHƯA THANH TOÁN');
                const payButton = isUnpaid ? `<button onclick="payInvoiceById('${ord.id}')" style="margin-top:10px; border:none; background:var(--primary); color:white; padding:10px 14px; border-radius:10px; font-weight:900; cursor:pointer;"><i class="fa-solid fa-qrcode"></i> Thanh toán QR</button>` : '';

                return `
                <div class="order-card">
                    <div style="display: flex; gap: 20px; align-items: center;">
                        <div style="width: 55px; height: 55px; background: var(--bg); color: var(--text-main); border-radius: 14px; display: flex; align-items: center; justify-content: center; font-size: 24px; border: 1px solid var(--border);">
                            <i class="fa-solid fa-file-invoice-dollar"></i>
                        </div>
                        <div>
                            <span style="font-weight: 800; color: var(--text-main); font-size: 17px; display: block; margin-bottom: 5px;">${ord.f1}</span>
                            <p style="font-size: 13px; color: var(--text-muted); font-weight: 500;"><i class="fa-regular fa-calendar" style="margin-right: 5px;"></i> Ngày tạo: ${ord.f4}</p>
                            ${ord.paymentMethod ? `<p style="font-size: 13px; color: var(--primary); font-weight: 800; margin-top: 4px;"><i class="fa-solid fa-building-columns" style="margin-right: 5px;"></i> ${ord.paymentMethod}</p>` : ''}
                            ${ord.paymentStatus ? `<p style="font-size: 13px; color: var(--success); font-weight: 800; margin-top: 4px;"><i class="fa-solid fa-circle-check" style="margin-right: 5px;"></i> ${ord.paymentStatus}</p>` : ''}
                            ${ord.paidAt ? `<p style="font-size: 12px; color: var(--text-muted); font-weight: 600; margin-top: 4px;">Thanh toán lúc: ${ord.paidAt}</p>` : ''}
                            ${ord.requestedAt ? `<p style="font-size: 12px; color: var(--text-muted); font-weight: 600; margin-top: 4px;">Gửi xác nhận lúc: ${ord.requestedAt}</p>` : ''}
                        </div>
                    </div>
                    <div style="text-align: right;">
                        <b style="display: block; font-size: 22px; font-weight: 900; color: var(--primary); margin-bottom: 8px;">${Number(ord.f3).toLocaleString('vi-VN')}đ</b>
                        <span style="font-size: 11px; color: ${statusColor}; font-weight: 800; display: inline-block; background: ${statusBg}; padding: 6px 12px; border-radius: 8px; letter-spacing: 0.5px;">
                            <i class="fa-solid ${statusIcon}" style="margin-right: 4px;"></i> ${statusText}
                        </span>
                        ${payButton}
                    </div>
                </div>
            `}).join('');
        }



        function payInvoiceById(orderId) {
            const orders = JSON.parse(localStorage.getItem('db_order')) || [];
            const invoice = orders.find(o => String(o.id) === String(orderId) && isMine(o));
            if(!invoice) return alert('Không tìm thấy hóa đơn của bạn.');
            if(Number(invoice.f3) <= 0) return alert('Hóa đơn này chưa có số tiền chính thức, vui lòng chờ gara cập nhật.');
            openQrPayment(invoice);
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


// =========================================================
// BẢN SỬA GỘP: KHÁCH HÀNG ĐỌC DỊCH VỤ TỪ SQL/API + MOBILE
// Admin thêm dịch vụ vào SQL qua /api/Services thì khách hàng sẽ thấy ở đây.
// =========================================================
function unwrapCustomerApiData(json) {
    if (Array.isArray(json)) return json;
    if (json && Array.isArray(json.data)) return json.data;
    if (json && Array.isArray(json.value)) return json.value;
    if (json && json.data) return json.data;
    return [];
}

function normalizeServiceForCustomer(item, index = 0) {
    return {
        id: item.serviceId || item.ServiceId || item.id || item.Id || item.f2 || ('DV' + index),
        name: item.serviceName || item.ServiceName || item.f1 || item.name || item.Name || 'Dịch vụ sửa chữa',
        code: item.f2 || ('DV' + String(item.serviceId || item.ServiceId || item.id || item.Id || index + 1).padStart(3, '0')),
        price: Number(item.price ?? item.Price ?? item.f3 ?? 0) || 0,
        desc: item.description || item.Description || item.f4 || item.desc || 'Dịch vụ sửa chữa tại garage TH2K.'
    };
}

async function getAllServicesForCustomer() {
    // Ưu tiên SQL Server/API để máy khác hoặc điện thoại mở qua Cloudflare vẫn thấy dữ liệu Admin đã thêm.
    try {
        const res = await fetch('/api/Services', { cache: 'no-store' });
        if (res.ok) {
            const json = await res.json();
            const apiServices = unwrapCustomerApiData(json).map(normalizeServiceForCustomer).filter(s => s.name);
            if (apiServices.length > 0) {
                localStorage.setItem('db_service', JSON.stringify(apiServices.map(s => ({ id: s.id, f1: s.name, f2: s.code, f3: s.price, f4: s.desc }))));
                return apiServices;
            }
        }
    } catch (e) {
        console.warn('Không tải được /api/Services, dùng dữ liệu localStorage:', e.message);
    }

    const keys = ['db_service', 'db_services', 'services'];
    for (const key of keys) {
        const raw = JSON.parse(localStorage.getItem(key) || '[]');
        if (Array.isArray(raw) && raw.length > 0) {
            return raw.map(normalizeServiceForCustomer).filter(s => s.name);
        }
    }
    return [];
}

async function loadProducts() {
    const services = await getAllServicesForCustomer();
    const list = document.getElementById('showroom-list') || document.getElementById('service-list') || document.getElementById('services-list');
    const carSelect = document.getElementById('car-select-options');

    if (!list) {
        console.error('Không tìm thấy vùng hiển thị dịch vụ: showroom-list/service-list/services-list');
        return;
    }

    if (services.length === 0) {
        list.innerHTML = `
            <div style="grid-column: 1/-1; background:white; padding:30px; border-radius:16px; text-align:center;">
                <h3>Chưa có dịch vụ sửa chữa</h3>
                <p>Admin cần thêm dịch vụ trong trang quản trị. Nếu đã thêm rồi, kiểm tra SQL Server/API /api/Services.</p>
            </div>`;
        if (carSelect) carSelect.innerHTML = '<option value="NONE">Chưa có dịch vụ</option>';
        return;
    }

    list.innerHTML = services.map((item, index) => {
        const safeName = String(item.name || '').replace(/'/g, "\\'");
        return `
            <div class="item-card">
                <div class="item-img"><i class="fa-solid fa-screwdriver-wrench"></i></div>
                <div class="item-content">
                    <div><span style="font-size: 11px; font-weight: 800; color: var(--success); background: #dcfce7; padding: 6px 10px; border-radius: 6px;">DỊCH VỤ SỬA CHỮA</span></div>
                    <h3 style="margin-top: 15px;">${item.name}</h3>
                    <p style="font-size: 13px; color: var(--text-muted);">Mã: ${item.code || ''}</p>
                    <p style="font-size: 13px; color: var(--text-muted); min-height:40px;">${item.desc || ''}</p>
                    <span class="item-price">${Number(item.price || 0).toLocaleString('vi-VN')}đ</span>
                    <div class="action-buttons">
                        <button class="btn-cart" onclick="addToCart('${item.id}', '${safeName}', ${Number(item.price || 0)})"><i class="fa-solid fa-check"></i> Chọn dịch vụ</button>
                        <button class="btn-buy" onclick="buyNow('${item.id}', '${safeName}', ${Number(item.price || 0)})">Đặt lịch ngay</button>
                    </div>
                </div>
            </div>`;
    }).join('');

    if (carSelect) {
        const carOptions = Array.from(carSelect.options || []).filter(o => String(o.value || '').startsWith('CAR_')).map(o => `<option value="${o.value}">${o.text}</option>`).join('');
        const serviceOptions = services.map(item => `<option value="SV_${item.id}">Dịch vụ: ${item.name}</option>`).join('');
        carSelect.innerHTML = carOptions + serviceOptions;
    }
}

// Ghi đè refreshBookingOptions để vừa có xe của tôi, vừa có dịch vụ từ SQL/API.
async function refreshBookingOptions(preferValue = '') {
    const select = document.getElementById('car-select-options');
    if(!select) return;
    const myCars = JSON.parse(localStorage.getItem(getMyCarKey())) || [];
    const cart = JSON.parse(localStorage.getItem(getCartKey())) || [];
    const services = await getAllServicesForCustomer();
    let options = [];

    myCars.forEach(car => options.push({ value: 'CAR_' + car.id, text: `${car.brand || ''} ${car.model || ''} - ${car.plate || ''}`, type: 'car' }));
    cart.forEach(item => options.push({ value: 'SV_' + item.id, text: `Dịch vụ đã chọn: ${item.name}`, type: 'service' }));
    if(cart.length === 0) services.forEach(s => options.push({ value: 'SV_' + s.id, text: `Dịch vụ: ${s.name}`, type: 'service' }));
    if(options.length === 0) options.push({ value: 'NONE', text: 'Chưa có xe/dịch vụ - vui lòng thêm xe hoặc chọn dịch vụ trước', type: 'none' });

    select.innerHTML = options.map(o => `<option value="${o.value}" data-type="${o.type}">${o.text}</option>`).join('');
    if(preferValue) select.value = preferValue;
}

// Bảo đảm khi quay lại tab dịch vụ sẽ tải lại từ SQL/API.
const oldNavForServiceReload = typeof nav === 'function' ? nav : null;
if (oldNavForServiceReload) {
    nav = function(id) {
        oldNavForServiceReload(id);
        if (id === 'shop') loadProducts();
        if (id === 'book') refreshBookingOptions();
    }
}

window.addEventListener('load', function() {
    loadProducts();
    refreshBookingOptions();
});
