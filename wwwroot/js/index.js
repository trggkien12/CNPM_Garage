const schema = {
            staff: { title: 'Nhân sự', cols: ['Họ Tên', 'Chức vụ', 'SĐT', 'Lương'], fields: ['f1', 'f2', 'f3', 'f4'] },
            customer: { title: 'Khách hàng', cols: ['Tên KH', 'SĐT', 'Email', 'Biển số', 'Loại xe'], fields: ['f1', 'f2', 'email', 'f3', 'f4'] },
            car: { title: 'Xe', cols: ['Biển số', 'Khách hàng', 'Hãng xe', 'Trạng thái'], fields: ['f1', 'f2', 'f3', 'status'] },
            service: { title: 'Dịch vụ sửa chữa', cols: ['Tên dịch vụ', 'Mã DV', 'Đơn giá', 'Ghi chú'], fields: ['f1', 'f2', 'f3', 'f4'] },
            inventory: { title: 'Kho', cols: ['Tên Phụ Tùng', 'Mã SP', 'Tồn kho', 'Vị trí'], fields: ['f1', 'f2', 'f3', 'f4'] },
            warranty: { title: 'Bảo hành', cols: ['Dịch vụ', 'Khách hàng', 'Ngày mua', 'Hạn BH'], fields: ['f1', 'f2', 'f3', 'f4'] },
            supplier: { title: 'Nhà cung cấp', cols: ['Tên NCC', 'SĐT', 'Địa chỉ', 'Trạng thái'], fields: ['f1', 'f2', 'f3', 'f4'] },
            order: { title: 'Hóa đơn', cols: ['Dịch vụ', 'Khách hàng', 'SĐT/Tài khoản', 'Tổng tiền', 'Trạng thái'], fields: ['f1', 'f2', 'customerAccount', 'f3', 'status'] },
            booking: { title: 'Lịch hẹn', cols: ['Khách đặt', 'Dịch vụ', 'Ngày giờ', 'Trạng thái', 'Lý do từ chối'], fields: ['customerName', 'type', 'date', 'status', 'rejectionReason'] }
        };

        let db = {};

        function saveDB() {
            // SQL only: không ghi dữ liệu xuống trình duyệt
        }

        // ĐỒNG BỘ SIÊU CẤP: TUYỆT ĐỐI KHÔNG LÀM MẤT DỮ LIỆU
        async function initData() {
            // SQL only: hàm này sẽ được index.js hotfix phía dưới ghi đè để tải dữ liệu từ API.
            if (typeof window.__loadSqlOnlyAdminData === 'function') {
                return window.__loadSqlOnlyAdminData();
            }
        }

        /* ======== TOAST NOTIFICATION SYSTEM ======== */
        function showToast(msg, type = 'success', duration = 3500) {
            let container = document.getElementById('toast-container');
            if (!container) {
                container = document.createElement('div');
                container.id = 'toast-container';
                container.style.cssText = 'position:fixed;top:24px;right:24px;z-index:99999;display:flex;flex-direction:column;gap:8px;pointer-events:none;';
                document.body.appendChild(container);
            }
            const colors = { success:'#10b981', error:'#ef4444', info:'#2563eb', warning:'#f59e0b' };
            const icons = { success:'✓', error:'✕', info:'ℹ', warning:'⚠' };
            const toast = document.createElement('div');
            toast.style.cssText = `background:${colors[type]||colors.info};color:#fff;padding:13px 18px;border-radius:12px;font-size:14px;font-weight:600;box-shadow:0 4px 24px rgba(0,0,0,0.18);display:flex;align-items:flex-start;gap:10px;min-width:240px;max-width:380px;pointer-events:all;animation:toastIn .28s ease;`;
            toast.innerHTML = `<span style="font-size:16px;margin-top:1px;">${icons[type]||icons.info}</span><span>${msg}</span>`;
            container.appendChild(toast);
            setTimeout(() => {
                toast.style.transition = 'all .28s ease';
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(24px)';
                setTimeout(() => toast.remove(), 300);
            }, duration);
        }

        /* ======== LOADING SPINNER ======== */
        function showLoading(msg = 'Đang tải...') {
            let el = document.getElementById('global-loader');
            if (!el) {
                el = document.createElement('div');
                el.id = 'global-loader';
                el.style.cssText = 'position:fixed;inset:0;background:rgba(15,23,42,.45);z-index:88888;display:flex;align-items:center;justify-content:center;flex-direction:column;gap:14px;';
                el.innerHTML = `<div style="width:48px;height:48px;border:4px solid #fff3;border-top:4px solid #fff;border-radius:50%;animation:spin .8s linear infinite;"></div><div style="color:#fff;font-size:15px;font-weight:600;">${msg}</div>`;
                document.body.appendChild(el);
            } else {
                el.querySelector('div:last-child').innerText = msg;
                el.style.display = 'flex';
            }
        }
        function hideLoading() {
            const el = document.getElementById('global-loader');
            if (el) el.style.display = 'none';
        }

        /* ======== HTML ESCAPE (chống XSS) ======== */
        function escHtml(str) {
            return String(str||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#039;');
        }

        function formatMoney(num) { return isNaN(num) ? num : Number(num).toLocaleString('vi-VN') + 'đ'; }
        function normalizeUserKey(value) {
            return String(value || '').trim().toLowerCase().replace(/[^a-z0-9@._-]/g, '_');
        }

        function displayCustomerAccount(value) {
            const raw = String(value || '').trim();
            const fakeEmail = raw.match(/^(0\d{9,10})@khachhang\.com$/i);
            if(fakeEmail) return fakeEmail[1];
            return raw;
        }

        function normalizePlateText(value) {
            return String(value || '').trim().toUpperCase();
        }

        function extractCarBrandType(car) {
            return String(car.f3 || car.carType || car.type || `${car.brand || ''} ${car.model || ''} ${car.year || ''}`).trim() || 'Chưa cập nhật';
        }

        function refreshCustomerCarColumns() {
            const cars = db.car || [];
            (db.customer || []).forEach(cus => {
                const account = String(cus.f2 || cus.customerAccount || '').trim().toLowerCase();
                const name = String(cus.f1 || cus.customerName || '').trim().toLowerCase();
                const ownedCars = cars.filter(car => {
                    const carAccount = String(car.customerAccount || '').trim().toLowerCase();
                    const carName = String(car.f2 || car.customerName || '').trim().toLowerCase();
                    return (account && carAccount === account) || (name && carName === name);
                });

                if (ownedCars.length > 0) {
                    cus.f3 = ownedCars.map(car => car.f1 || car.plate || car.licensePlate || '').filter(Boolean).join(', ');
                    cus.f4 = ownedCars.map(car => extractCarBrandType(car)).filter(Boolean).join(', ');
                } else {
                    cus.f3 = 'Chưa có xe';
                    cus.f4 = '';
                }
            });
        }


        function normalizeAdminText(value) {
            return displayCustomerAccount(value || '').trim().toLowerCase().replace(/\s+/g, '');
        }

        function normalizeAdminDate(value) {
            const raw = String(value || '').trim();
            if(!raw) return '';
            const d = new Date(raw);
            if(!isNaN(d.getTime())) {
                const yyyy = d.getFullYear();
                const mm = String(d.getMonth() + 1).padStart(2, '0');
                const dd = String(d.getDate()).padStart(2, '0');
                const hh = String(d.getHours()).padStart(2, '0');
                const mi = String(d.getMinutes()).padStart(2, '0');
                return `${yyyy}-${mm}-${dd} ${hh}:${mi}`;
            }
            return raw.replace('T', ' ').substring(0, 16);
        }

        function getServiceFromAppointmentNote(note) {
            const text = String(note || '');
            const match = text.match(/Dịch vụ:\s*([^\n]+)/i);
            return match && match[1] ? match[1].trim() : '';
        }

        function makeBookingFromApiAppointment(a) {
            const appId = a.appointmentId || a.AppointmentId || a.id || a.Id;
            const customerAccount = displayCustomerAccount(a.customerAccount || a.CustomerAccount || a.customerPhone || a.CustomerPhone || a.customerEmail || a.CustomerEmail || '');
            const customerName = a.customerName || a.CustomerName || a.fullName || a.FullName || 'Khách hàng';
            const note = a.note || a.Note || '';
            const service = a.serviceName || a.ServiceName || a.carService || a.CarService || getServiceFromAppointmentNote(note) || 'Dịch vụ sửa chữa';
            const date = a.date || a.Date || a.appointmentDate || a.AppointmentDate || '';
            return {
                id: 'SQL_APP_' + appId,
                appointmentId: appId,
                ownerUser: normalizeUserKey(customerAccount || customerName),
                customerAccount,
                customerName,
                customerEmail: a.customerEmail || a.CustomerEmail || '',
                type: a.type || a.Type || service || 'Lịch hẹn dịch vụ',
                carService: service,
                serviceName: service,
                date: date,
                note: note,
                status: a.status || a.Status || 'Chờ xác nhận',
                rejectionReason: a.rejectionReason || a.RejectionReason || '',
                rejectedAt: a.rejectedAt || a.RejectedAt || '',
                createdAt: a.createdAt || a.CreatedAt || '',
                sqlSynced: true
            };
        }

        function getAdminBookingMatchKey(b) {
            const account = normalizeAdminText(b.customerAccount || b.customerEmail || '');
            const name = normalizeAdminText(b.customerName || b.f2 || '');
            const service = normalizeAdminText(b.carService || b.serviceName || b.type || '');
            const date = normalizeAdminDate(b.date || b.appointmentDate || '');
            return `${account}|${name}|${service}|${date}`;
        }

        function findMatchingAdminBooking(localBooking, sqlBookings) {
            const localKey = getAdminBookingMatchKey(localBooking);
            let found = sqlBookings.find(sql => getAdminBookingMatchKey(sql) === localKey);
            if(found) return found;

            const localDate = normalizeAdminDate(localBooking.date || localBooking.appointmentDate || '');
            const localAccount = normalizeAdminText(localBooking.customerAccount || localBooking.customerEmail || '');
            const localName = normalizeAdminText(localBooking.customerName || localBooking.f2 || '');
            return sqlBookings.find(sql => {
                const sqlDate = normalizeAdminDate(sql.date || sql.appointmentDate || '');
                const sqlAccount = normalizeAdminText(sql.customerAccount || sql.customerEmail || '');
                const sqlName = normalizeAdminText(sql.customerName || sql.f2 || '');
                return localDate && sqlDate && localDate === sqlDate && ((localAccount && sqlAccount && localAccount === sqlAccount) || (localName && sqlName && localName === sqlName));
            });
        }

        async function syncAppointmentsFromApiToAdmin() {
            try {
                const res = await fetch('/api/Appointments', { cache: 'no-store' });
                if(!res.ok) return;
                const json = await res.json();
                const apiAppointments = Array.isArray(json) ? json : (json.data || json.value || []);
                const sqlBookings = apiAppointments.map(makeBookingFromApiAppointment).filter(b => b.appointmentId);
                const merged = [];
                const seen = new Set();

                sqlBookings.forEach(sql => {
                    const key = getAdminBookingMatchKey(sql) || String(sql.id || '');
                    if(seen.has(key)) return;
                    seen.add(key);
                    merged.push(sql);
                });

                (db.booking || []).forEach(local => {
                    const match = findMatchingAdminBooking(local, sqlBookings);
                    if(match) {
                        // Có bản SQL thì dùng bản SQL để trạng thái xác nhận/từ chối được đồng bộ cho khách.
                        return;
                    }
                    const key = getAdminBookingMatchKey(local) || String(local.id || '');
                    if(seen.has(key)) return;
                    seen.add(key);
                    merged.push(local);
                });

                db.booking = merged;
            } catch(e) {
                console.log('Không đồng bộ được lịch hẹn từ API', e);
            }
        }

        async function ensureSqlAppointmentForBooking(item) {
            let appointmentId = item.appointmentId || String(item.id || '').replace('SQL_APP_', '');
            if(appointmentId && String(appointmentId).match(/^\d+$/)) return appointmentId;

            const res = await fetch('/api/Appointments', { cache: 'no-store' });
            const json = await res.json().catch(() => ({}));
            if(!res.ok) throw new Error('Không đọc được lịch hẹn từ SQL');
            const apiAppointments = Array.isArray(json) ? json : (json.data || json.value || []);
            const sqlBookings = apiAppointments.map(makeBookingFromApiAppointment);
            const match = findMatchingAdminBooking(item, sqlBookings);
            if(!match || !match.appointmentId) {
                throw new Error('Không tìm thấy lịch hẹn tương ứng trong SQL. Hãy tải lại trang Admin rồi thử xác nhận lại.');
            }
            item.appointmentId = match.appointmentId;
            item.id = match.id;
            item.sqlSynced = true;
            return match.appointmentId;
        }
        function cleanCustomerAccount(value) {
            return displayCustomerAccount(value);
        }
        function findCustomerByNameOrAccount(value) {
            const v = String(value || '').trim().toLowerCase();
            if(!v) return null;
            return (db.customer || []).find(c =>
                String(c.f1 || '').trim().toLowerCase() === v ||
                String(c.f2 || '').trim().toLowerCase() === v ||
                String(c.customerAccount || '').trim().toLowerCase() === v
            ) || null;
        }

        function onlyNumber(value) {
            const n = Number(String(value || '').replace(/[^0-9.-]/g, ''));
            return isNaN(n) ? 0 : n;
        }
        function isPendingStatus(status) { return String(status || '').toLowerCase().includes('chờ'); }
        function isPaidStatus(status) {
            const st = String(status || '').toLowerCase();
            return (st.includes('đã') || st.includes('hoàn tất')) && !st.includes('chờ');
        }
        function isLowStock(value) {
            const raw = String(value || '').trim().toLowerCase();
            if(raw.includes('còn hàng') && !raw.includes('không')) return false;
            const n = onlyNumber(raw);
            return n <= 5; // Cảnh báo khi tồn kho <= 5, bao gồm cả khi = 0 (hết hàng)
        }
        function setText(id, value) { const el = document.getElementById(id); if(el) el.innerText = value; }
        function setHTML(id, value) { const el = document.getElementById(id); if(el) el.innerHTML = value; }


        async function syncDashboardFromApi() {
            try {
                const res = await fetch('/api/Statistics/dashboard', { cache: 'no-store' });
                if(!res.ok) return;
                const json = await res.json();
                const s = json.data || json;
                setText('dash-cus', s.totalCustomers ?? s.TotalCustomers ?? db.customer.length);
                setText('dash-order', s.totalOrders ?? s.TotalOrders ?? db.order.length);
                setText('dash-rev', formatMoney(s.totalRevenue ?? s.TotalRevenue ?? s.totalConfirmedPaidAmount ?? s.TotalConfirmedPaidAmount ?? db.revenue));
                setText('dash-pending-qr', s.pendingQrInvoices ?? s.PendingQrInvoices ?? 0);
                setText('dash-booking', s.totalPendingAppointments ?? s.TotalPendingAppointments ?? 0);
                setText('dash-service', s.totalServices ?? s.TotalServices ?? db.service.length);
                setText('dash-low-stock', s.lowStockParts ?? s.LowStockParts ?? 0);
                setText('nav-badge-booking', s.totalPendingAppointments ?? s.TotalPendingAppointments ?? 0);
            } catch(e) {
                console.log('Không đồng bộ được dashboard API', e);
            }
        }

        function renderDashboard() {
            const orders = db.order || [];
            const bookings = db.booking || [];
            const inventory = db.inventory || [];
            const services = db.service || [];
            const staff = db.staff || [];

            let calculatedRev = orders.reduce((sum, o) => isPaidStatus(o.status) ? sum + (Number(o.f3) || 0) : sum, 0);
            db.revenue = calculatedRev;

            const pendingQrOrders = orders.filter(o => String(o.status || '').includes('QR') || String(o.paymentStatus || '').includes('QR') || (isPendingStatus(o.status) && String(o.status || '').includes('thanh toán')));
            const pendingBookings = bookings.filter(b => isPendingStatus(b.status));
            const lowStocks = inventory.filter(i => isLowStock(i.f3));
            const completedOrders = orders.filter(o => isPaidStatus(o.status)).length;
            const completeRate = orders.length ? Math.round(completedOrders / orders.length * 100) : 0;

            setText('dash-cus', db.customer.length);
            setText('dash-order', orders.length);
            setText('dash-rev', formatMoney(calculatedRev));
            setText('dash-pending-qr', pendingQrOrders.length);
            setText('dash-booking', pendingBookings.length);
            setText('dash-service', services.length);
            setText('dash-low-stock', lowStocks.length);
            setText('dash-staff', staff.length);
            setText('dash-complete-rate', completeRate + '%');
            const bar = document.getElementById('dash-complete-bar'); if(bar) bar.style.width = completeRate + '%';
            setText('nav-badge-booking', pendingBookings.length);

            setHTML('dash-pending-list', pendingQrOrders.slice(0, 5).map(o => `
                <div class="mini-item">
                    <div><strong>${o.f2 || 'Khách hàng'}</strong><br><span>${o.f1 || 'Hóa đơn'} • ${o.id || ''}</span></div>
                    <div style="text-align:right"><strong style="color:var(--orange);">${formatMoney(o.f3)}</strong><br><button class="btn btn-success btn-sm" onclick="approveItem('order','${o.id}')"><i class="fa-solid fa-check"></i> Xác nhận</button></div>
                </div>`).join('') || '<p style="color:var(--muted); font-size:14px;">Không có thanh toán QR nào đang chờ.</p>');

            setHTML('dash-booking-list', pendingBookings.slice(0, 5).map(b => `
                <div class="mini-item">
                    <div><strong>${b.customerName || b.f1 || 'Khách hàng'}</strong><br><span>${b.type || b.f2 || 'Dịch vụ'} • ${b.date || b.f3 || ''}</span></div>
                    <div style="display:flex; gap:8px;">
                        <button class="btn btn-success btn-sm" title="Xác nhận lịch" onclick="approveItem('booking','${b.id}')"><i class="fa-solid fa-check"></i></button>
                        <button class="btn btn-danger btn-sm" title="Từ chối lịch" onclick="rejectBooking('${b.id}')"><i class="fa-solid fa-xmark"></i></button>
                    </div>
                </div>`).join('') || '<p style="color:var(--muted); font-size:14px;">Không có lịch hẹn đang chờ.</p>');

            setHTML('dash-stock-list', lowStocks.slice(0, 5).map(i => `
                <div class="mini-item">
                    <div><strong>${i.f1}</strong><br><span>Mã: ${i.f2 || '---'} • Vị trí: ${i.f4 || '---'}</span></div>
                    <strong style="color:var(--red);">Còn ${i.f3}</strong>
                </div>`).join('') || '<p style="color:var(--muted); font-size:14px;">Kho chưa có phụ tùng sắp hết.</p>');
        }

        function renderApp() {
            refreshCustomerCarColumns();
            renderDashboard();

            for(let type in schema) {
                let table = document.getElementById('tbl-' + type);
                if(!table) continue;
                
                let sc = schema[type];
                let thead = `<thead><tr>` + sc.cols.map(c => `<th>${c}</th>`).join('') + `<th style="text-align:right;">Thao tác</th></tr></thead>`;
                
                let dataToRender = [...db[type]];
                if (type === 'car') {
                    const kw = String(document.getElementById('car-plate-search')?.value || '').trim().toLowerCase();
                    if (kw) dataToRender = dataToRender.filter(row => String(row.f1 || row.plate || '').toLowerCase().includes(kw));
                }

                let tbody = `<tbody>` + dataToRender.map(row => {
                    let tdHtml = '';
                    sc.fields.forEach((fieldKey, idx) => {
                        let val = row[fieldKey] || '';
                        if(fieldKey === 'customerAccount') val = displayCustomerAccount(val);
                        if(idx === 0) {
                            tdHtml += `<td><strong>${val}</strong></td>`;
                        } else if (fieldKey === 'f3' && (type === 'service' || type === 'order')) {
                            tdHtml += `<td style="color:var(--blue); font-weight:700;">${formatMoney(val)}</td>`;
                        } else if (fieldKey === 'status') {
                            let badgeClass = (val.includes('Từ chối') || val.includes('từ chối')) ? 'status-rejected' : (val.includes('Chờ') || val.includes('Pending')) ? 'status-pending' : (val.includes('hủy') || val.includes('Hủy')) ? 'status-rejected' : 'status-done';
                            tdHtml += `<td><span class="status-badge ${badgeClass}">${val}</span></td>`;
                        } else {
                            tdHtml += `<td>${val}</td>`;
                        }
                    });

                    let approveBtn = '';
                    let rejectBtn = '';
                    if((type === 'booking' || type === 'order') && (row.status || '').includes('Chờ')) {
                        approveBtn = `<button class="btn btn-success btn-sm" title="Xác nhận" onclick="approveItem('${type}', '${row.id}')"><i class="fa-solid fa-check"></i></button>`;
                    }
                    if(type === 'booking' && (row.status || '').includes('Chờ')) {
                        rejectBtn = `<button class="btn btn-danger btn-sm" title="Từ chối lịch hẹn" onclick="rejectBooking('${row.id}')"><i class="fa-solid fa-xmark"></i></button>`;
                    }
                    if(type === 'order' && (row.status || '').includes('Chờ')) {
                        rejectBtn = `<button class="btn btn-danger btn-sm" title="Từ chối thanh toán QR" onclick="rejectOrder('${row.id}')"><i class="fa-solid fa-xmark"></i></button>`;
                    }

                    return `
                    <tr>
                        ${tdHtml}
                        <td style="text-align:right;">
                            <div class="action-group" style="justify-content:flex-end;">
                                ${approveBtn}
                                ${rejectBtn}
                                <button class="btn btn-warning btn-sm" title="Sửa" onclick="openModal('${type}', '${row.id}')"><i class="fa-solid fa-pen"></i></button>
                                <button class="btn btn-danger btn-sm" title="Xóa" onclick="deleteData('${type}', '${row.id}')"><i class="fa-solid fa-trash"></i></button>
                            </div>
                        </td>
                    </tr>`;
                }).join('') + `</tbody>`;
                
                table.innerHTML = thead + tbody;
            }

            document.getElementById('pos-items').innerHTML = db.service.map(item => `
                <div class="pos-item-card" onclick="addToCart('${item.f1}', '${item.f3}')">
                    <h4 style="margin-bottom:8px; font-size:15px;">${item.f1}</h4>
                    <span style="font-size:12px; color:var(--muted); background:var(--bg); padding:3px 8px; border-radius:4px;">${item.f2}</span>
                    <div style="color:var(--blue); font-weight:700; font-size: 16px; margin-top:12px;">${formatMoney(item.f3)}</div>
                </div>
            `).join('');
        }

        // MODAL
        function openModal(type, id = null) {
            let sc = schema[type];
            document.getElementById('entityType').value = type;
            document.getElementById('entityId').value = id || "";
            document.getElementById('modal-title').innerText = (id ? "Cập nhật " : "Thêm mới ") + sc.title;

            let item = id ? db[type].find(x => String(x.id) === String(id)) : {};
            let html = '';
            
            sc.cols.forEach((colName, idx) => {
                let fieldKey = sc.fields[idx];
                let val = item[fieldKey] || '';
                
                if (fieldKey === 'status') {
                    html += `
                    <div class="form-group">
                        <label>${colName}</label>
                        <select id="inp-${fieldKey}">
                            <option value="Chờ xác nhận" ${val.includes('Chờ') ? 'selected' : ''}>Chờ xác nhận</option>
                            <option value="Chờ xác nhận thanh toán QR" ${val.includes('thanh toán QR') ? 'selected' : ''}>Chờ xác nhận thanh toán QR</option>
                            <option value="Đã xác nhận / Hoàn tất" ${val.includes('Đã') || val.includes('Hoàn tất') ? 'selected' : ''}>Đã hoàn tất</option>
                            <option value="Đã từ chối" ${val === 'Đã từ chối' ? 'selected' : ''}>Đã từ chối</option>
                            <option value="Đã hủy" ${val === 'Đã hủy' ? 'selected' : ''}>Đã hủy</option>
                        </select>
                    </div>`;
                } else {
                    html += `
                    <div class="form-group">
                        <label>${colName}</label>
                        <input type="text" id="inp-${fieldKey}" value="${val}" placeholder="Nhập ${colName.toLowerCase()}...">
                    </div>`;
                }
            });

            document.getElementById('modal-fields').innerHTML = html;
            document.getElementById('mainModal').classList.add('active');
        }

        function closeModal() { document.getElementById('mainModal').classList.remove('active'); }

        async function saveData() {
            let type = document.getElementById('entityType').value;
            let id = document.getElementById('entityId').value;
            let sc = schema[type];

            let firstInput = document.getElementById('inp-' + sc.fields[0]).value;
            if(!firstInput) return alert("Vui lòng nhập thông tin cho trường đầu tiên!");

            let newItem = { id: id ? id : 'ID_' + Date.now() };

            if(id) {
                let existing = db[type].find(x => String(x.id) === String(id));
                if(existing) newItem = {...existing}; 
            } else {
                if(type === 'order') newItem.f4 = new Date().toLocaleString('vi-VN');
                if(type === 'booking') newItem.createdAt = new Date().toLocaleString('vi-VN');
            }

            sc.fields.forEach(fieldKey => {
                newItem[fieldKey] = document.getElementById('inp-' + fieldKey).value;
            });

            try {
                if(type === 'customer') {
                    const body = {
                        fullName: newItem.f1,
                        phoneNumber: displayCustomerAccount(newItem.f2 || ''),
                        email: String(newItem.email || '').trim(),
                        address: '',
                        password: '123456'
                    };
                    if(!body.email || !body.email.includes('@')) return alert('Vui lòng nhập email thật cho khách hàng!');
                    if(id && String(id).match(/^\d+$/)) {
                        await fetch(`/api/Customers/${id}`, { method:'PUT', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body) });
                    } else {
                        const res = await fetch('/api/Customers', { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body) });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không lưu được khách hàng lên SQL');
                        const created = json.data || json;
                        newItem.id = created.id || created.Id || newItem.id;
                    }
                }

                if(type === 'service') {
                    const body = {
                        serviceName: newItem.f1,
                        serviceCode: newItem.f2 || newItem.id,
                        price: Number(String(newItem.f3 || 0).replace(/[^0-9]/g,'')) || 0,
                        description: newItem.f4 || ''
                    };
                    const apiId = newItem.apiServiceId || (String(id).match(/^\d+$/) ? id : null);
                    const res = await fetch(apiId ? `/api/Services/${apiId}` : '/api/Services', {
                        method: apiId ? 'PUT' : 'POST',
                        headers:{'Content-Type':'application/json'},
                        body:JSON.stringify(body)
                    });
                    const json = await res.json().catch(()=>({}));
                    if(!res.ok || json.success === false) throw new Error(json.message || 'Không lưu được dịch vụ lên SQL');
                    const saved = json.data || json;
                    newItem.apiServiceId = saved.serviceId || saved.ServiceId || saved.id || saved.Id || apiId;
                    newItem.id = newItem.apiServiceId || newItem.id;
                }

                if(type === 'car') {
                    newItem.f1 = normalizePlateText(newItem.f1);
                    const duplicatePlate = (db.car || []).find(c => String(c.id) !== String(id) && normalizePlateText(c.f1 || c.plate || c.licensePlate) === normalizePlateText(newItem.f1));
                    if(duplicatePlate) return alert('Biển số xe này đã được khách hàng khác đăng ký. Không thể lưu trùng biển số.');

                    const customer = findCustomerByNameOrAccount(newItem.f2);
                    if(!customer) return alert('Không tìm thấy khách hàng để gán xe. Hãy thêm khách hàng trước.');
                    const customerId = customer.apiCustomerId || customer.id;
                    if(!customerId || !String(customerId).match(/^\d+$/)) return alert('Khách hàng chưa có mã SQL hợp lệ. Hãy lưu khách hàng lên SQL trước.');

                    const parts = String(newItem.f3 || '').trim().split(/\s+/);
                    const body = {
                        licensePlate: newItem.f1,
                        brand: parts[0] || 'Chưa cập nhật',
                        model: parts.slice(1, -1).join(' ') || parts.slice(1).join(' ') || 'Chưa cập nhật',
                        year: Number(parts[parts.length - 1]) || new Date().getFullYear(),
                        customerId: Number(customerId)
                    };
                    const apiId = newItem.apiCarId || (String(id).match(/^\d+$/) ? id : null);
                    const res = await fetch(apiId ? `/api/Cars/${apiId}` : '/api/Cars', {
                        method: apiId ? 'PUT' : 'POST',
                        headers:{'Content-Type':'application/json'},
                        body:JSON.stringify(body)
                    });
                    const json = await res.json().catch(()=>({}));
                    if(!res.ok || json.success === false) throw new Error(json.message || 'Không lưu được xe lên SQL');
                    const saved = json.data || json;
                    newItem.apiCarId = saved.carId || saved.CarId || saved.id || saved.Id || apiId;
                    newItem.id = newItem.apiCarId || newItem.id;
                    newItem.customerAccount = customer.f2 || customer.customerAccount || '';
                    newItem.ownerUser = normalizeUserKey(newItem.customerAccount || customer.f1);
                    newItem.customerName = customer.f1;
                }

                if(type === 'booking') {
                    if(newItem.status === 'Đã từ chối' && !String(newItem.rejectionReason || '').trim()) {
                        return alert('Lịch hẹn bị từ chối cần có lý do từ chối.');
                    }
                    if(!id) {
                        const customer = findCustomerByNameOrAccount(newItem.customerName || newItem.f2 || '');
                        const customerId = customer && (customer.apiCustomerId || customer.id);
                        if(customerId && String(customerId).match(/^\d+$/)) {
                            const date = new Date(newItem.date || new Date(Date.now()+86400000)).toISOString();
                            const res = await fetch('/api/Appointments', { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ customerId:Number(customerId), appointmentDate:date, note:newItem.type || '' }) });
                            const json = await res.json().catch(()=>({}));
                            if(!res.ok || json.success === false) throw new Error(json.message || 'Không lưu được lịch hẹn lên SQL');
                            const saved = json.data || json;
                            newItem.appointmentId = saved.appointmentId || saved.AppointmentId;
                            newItem.id = 'SQL_APP_' + newItem.appointmentId;
                        }
                    }
                }
            } catch(e) {
                alert(e.message || 'Không lưu được dữ liệu lên SQL/API.');
                return;
            }

            if(type === 'order') {
                const customer = findCustomerByNameOrAccount(newItem.customerAccount || newItem.f2);
                if(customer) {
                    newItem.f2 = customer.f1 || newItem.f2;
                    newItem.customerAccount = cleanCustomerAccount(customer.f2 || customer.customerAccount || newItem.customerAccount);
                    newItem.ownerUser = normalizeUserKey(newItem.customerAccount || newItem.f2);
                    newItem.customerName = newItem.f2;
                } else if(newItem.customerAccount) {
                    newItem.customerAccount = cleanCustomerAccount(newItem.customerAccount);
                    newItem.ownerUser = normalizeUserKey(newItem.customerAccount);
                    newItem.customerName = newItem.f2;
                } else {
                    alert('Lưu ý: Hóa đơn chưa có SĐT/Tài khoản khách hàng nên khách có thể không nhìn thấy trong mục Hóa đơn của tôi.');
                }
            }

            if (id) {
                let index = db[type].findIndex(x => String(x.id) === String(id));
                if(index >= 0) db[type][index] = newItem;
                else db[type].unshift(newItem);
            } else {
                db[type].unshift(newItem); 
            }

            saveDB();
            await initData();
            closeModal();
        }

        async function deleteData(type, id) {
            if (!confirm("Xác nhận xóa dữ liệu này vĩnh viễn?")) return;

            try {
                if(type === 'car') {
                    const item = db.car.find(x => String(x.id) === String(id));
                    const apiId = item && (item.apiCarId || (String(id).match(/^\d+$/) ? id : null));
                    if(apiId) {
                        const res = await fetch(`/api/Cars/${apiId}`, { method:'DELETE' });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không thể xóa xe trên SQL');
                    }
                }
                if(type === 'service') {
                    const item = db.service.find(x => String(x.id) === String(id));
                    const apiId = item && (item.apiServiceId || (String(id).match(/^\d+$/) ? id : null));
                    if(apiId) {
                        const res = await fetch(`/api/Services/${apiId}`, { method:'DELETE' });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không thể xóa dịch vụ trên SQL');
                    }
                }
                if(type === 'customer') {
                    if(String(id).match(/^\d+$/)) {
                        const res = await fetch(`/api/Customers/${id}`, { method:'DELETE' });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không thể xóa khách hàng trên SQL');
                    }
                }
                if(type === 'booking') {
                    const item = db.booking.find(x => String(x.id) === String(id));
                    const apiId = item && (item.appointmentId || String(id).replace('SQL_APP_',''));
                    if(apiId && String(apiId).match(/^\d+$/)) {
                        const res = await fetch(`/api/Appointments/${apiId}`, { method:'DELETE' });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không thể hủy lịch hẹn trên SQL');
                    }
                }
            } catch(e) {
                alert(e.message || 'Không thể xóa dữ liệu do ràng buộc SQL.');
                return;
            }

            db[type] = db[type].filter(x => String(x.id) !== String(id));
            saveDB();
            await initData();
        }

        function extractMoneyFromText(text) {
            const raw = String(text || '');
            const matches = raw.match(/[0-9][0-9\.\,]*/g);
            if(!matches || matches.length === 0) return 0;
            const last = matches[matches.length - 1].replace(/[^0-9]/g, '');
            return Number(last) || 0;
        }

        async function createInvoiceFromBooking(booking) {
            db.order = db.order || [];
            const existed = db.order.find(o => String(o.bookingId || '') === String(booking.id));
            if(existed) return existed;

            const estimate = Number(booking.estimatedAmount || booking.f3 || 0) || extractMoneyFromText(booking.note);
            const amount = estimate > 0 ? estimate : 0;
            const serviceName = booking.carService || booking.serviceName || booking.type || 'Dịch vụ sửa chữa';
            const customerName = booking.customerName || booking.f2 || 'Khách hàng';

            // Gọi API lưu hóa đơn lên SQL
            const invoiceBody = {
                serviceName: 'Hóa đơn tạm tính: ' + serviceName,
                customerName: customerName,
                customerEmail: booking.customerEmail || '',
                customerPhone: cleanCustomerAccount(booking.customerAccount || ''),
                totalAmount: amount,
                appointmentId: booking.appointmentId ? Number(booking.appointmentId) : undefined,
                status: 'Chưa thanh toán',
                note: 'Tạo tự động từ lịch hẹn đã xác nhận'
            };

            let savedId = 'HD' + Math.floor(Math.random() * 1000000);
            try {
                const res = await fetch('/api/Invoices', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(invoiceBody)
                });
                const json = await res.json().catch(() => ({}));
                if (res.ok && json.success !== false) {
                    const saved = json.data || json;
                    savedId = saved.invoiceId || saved.InvoiceId || saved.id || savedId;
                } else {
                    console.warn('Không tạo được hóa đơn SQL tự động:', json.message || res.status);
                }
            } catch(e) {
                console.warn('Lỗi tạo hóa đơn tự động:', e);
            }

            const invoice = {
                id: savedId,
                invoiceId: savedId,
                bookingId: booking.id,
                ownerUser: booking.ownerUser || booking.customerAccount || booking.customerEmail || '',
                customerAccount: cleanCustomerAccount(booking.customerAccount || booking.customerEmail || booking.ownerUser || ''),
                customerName: customerName,
                f1: 'Hóa đơn tạm tính: ' + serviceName,
                f2: customerName,
                f3: amount,
                f4: new Date().toLocaleString('vi-VN'),
                paymentStatus: 'Chưa thanh toán',
                status: 'Chưa thanh toán',
                createdFrom: 'Tạo tự động từ lịch hẹn đã xác nhận'
            };
            db.order.unshift(invoice);
            return invoice;
        }

        async function approveItem(type, id) {
            let item = db[type].find(x => String(x.id) === String(id));
            if(!item) return;

            try {
                if(type === 'order') {
                    const paymentId = item.paymentId || item.latestPaymentId || item.LatestPaymentId;
                    const invoiceId = item.invoiceId || item.InvoiceId;
                    if(paymentId) {
                        const res = await fetch(`/api/Payments/${paymentId}/confirm`, {
                            method:'PUT',
                            headers:{'Content-Type':'application/json'},
                            body:JSON.stringify({ confirmedBy:'Admin', note:'Admin xác nhận thanh toán QR' })
                        });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không xác nhận được thanh toán');
                    } else if(invoiceId) {
                        const res = await fetch(`/api/Invoices/${invoiceId}/confirm-payment`, { method:'PUT' });
                        const json = await res.json().catch(()=>({}));
                        if(!res.ok || json.success === false) throw new Error(json.message || 'Không xác nhận được hóa đơn');
                    }
                    item.status = "Đã xác nhận / Hoàn tất";
                    item.paymentStatus = 'Đã được Admin/Nhân viên xác nhận thanh toán';
                    item.paidAt = new Date().toLocaleString('vi-VN');
                } else if(type === 'booking') {
                    const appointmentId = await ensureSqlAppointmentForBooking(item);
                    const res = await fetch(`/api/Appointments/${appointmentId}/approve`, { method:'PUT' });
                    const json = await res.json().catch(()=>({}));
                    if(!res.ok || json.success === false) throw new Error(json.message || 'Không xác nhận được lịch hẹn');

                    item.status = "Đã xác nhận / Hoàn tất";
                    item.rejectionReason = '';
                    item.approvedAt = new Date().toLocaleString('vi-VN');
                    item.appointmentId = appointmentId;
                    item.id = 'SQL_APP_' + appointmentId;
                    item.sqlSynced = true;
                    await createInvoiceFromBooking(item);
                    showToast('Đã xác nhận lịch hẹn. Khách hàng sẽ thấy trạng thái cập nhật.', 'success');
                } else {
                    item.status = "Đã xác nhận / Hoàn tất";
                }

                saveDB();
                await initData();
            } catch(e) {
                alert(e.message || 'Không thể xác nhận dữ liệu trên SQL/API.');
            }
        }

        async function rejectBooking(id) {
            const item = db.booking.find(x => String(x.id) === String(id));
            if(!item) return alert('Không tìm thấy lịch hẹn cần từ chối!');
            const reason = prompt('Nhập lý do từ chối lịch hẹn này:');
            if(reason === null) return;
            const cleanReason = reason.trim();
            if(!cleanReason) return alert('Bạn phải nhập lý do từ chối để khách hàng biết nguyên nhân.');

            try {
                const appointmentId = await ensureSqlAppointmentForBooking(item);
                const res = await fetch(`/api/Appointments/${appointmentId}/reject`, {
                    method:'PUT',
                    headers:{'Content-Type':'application/json'},
                    body:JSON.stringify({ reason: cleanReason, confirmedBy:'Admin' })
                });
                const json = await res.json().catch(()=>({}));
                if(!res.ok || json.success === false) throw new Error(json.message || 'Không từ chối được lịch hẹn');

                item.appointmentId = appointmentId;
                item.id = 'SQL_APP_' + appointmentId;
                item.sqlSynced = true;
                item.status = 'Đã từ chối';
                item.rejectionReason = cleanReason;
                item.rejectedAt = new Date().toLocaleString('vi-VN');
                saveDB();
                await initData();
                alert('Đã từ chối lịch hẹn và lưu lý do cho khách hàng.');
            } catch(e) {
                alert(e.message || 'Không thể từ chối lịch hẹn trên SQL/API.');
            }
        }

        async function rejectOrder(id) {
            const item = db.order.find(x => String(x.id) === String(id));
            if(!item) return alert('Không tìm thấy hóa đơn cần từ chối!');
            const reason = prompt('Nhập lý do từ chối thanh toán QR:');
            if(reason === null) return;
            const cleanReason = reason.trim();
            if(!cleanReason) return alert('Bạn phải nhập lý do từ chối để khách hàng biết nguyên nhân.');

            try {
                const paymentId = item.paymentId || item.latestPaymentId || item.LatestPaymentId;
                const invoiceId = item.invoiceId || item.InvoiceId;
                if(paymentId) {
                    const res = await fetch(`/api/Payments/${paymentId}/reject`, {
                        method:'PUT',
                        headers:{'Content-Type':'application/json'},
                        body:JSON.stringify({ reason: cleanReason, confirmedBy:'Admin' })
                    });
                    const json = await res.json().catch(()=>({}));
                    if(!res.ok || json.success === false) throw new Error(json.message || 'Không từ chối được thanh toán');
                } else if(invoiceId) {
                    const res = await fetch(`/api/Invoices/${invoiceId}/reject-payment`, {
                        method:'PUT',
                        headers:{'Content-Type':'application/json'},
                        body:JSON.stringify({ reason: cleanReason, confirmedBy:'Admin' })
                    });
                    const json = await res.json().catch(()=>({}));
                    if(!res.ok || json.success === false) throw new Error(json.message || 'Không từ chối được hóa đơn');
                }

                item.status = 'Đã từ chối';
                item.paymentStatus = 'Admin/Nhân viên từ chối thanh toán QR';
                item.rejectionReason = cleanReason;
                item.rejectedAt = new Date().toLocaleString('vi-VN');
                saveDB();
                await initData();
                alert('Đã từ chối thanh toán QR và lưu lý do cho khách hàng.');
            } catch(e) {
                alert(e.message || 'Không thể từ chối thanh toán trên SQL/API.');
            }
        }

        // BÁN HÀNG POS
        let cart = []; let totalCart = 0;
        function addToCart(name, price) {
            let numPrice = Number(price) || 0;
            cart.push({ name, price: numPrice });
            totalCart += numPrice;
            renderCart();
        }
        function renderCart() {
            let list = document.getElementById('cart-list');
            if(cart.length === 0) { list.innerHTML = `<div style="text-align:center; color:var(--muted); padding:30px 0;">Chưa chọn dịch vụ</div>`; }
            else {
                list.innerHTML = cart.map((item, idx) => `
                    <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:12px; border-bottom:1px solid var(--border); padding-bottom:8px;">
                        <span style="font-size:14px; font-weight:500;">${item.name}</span>
                        <div><strong style="color:var(--blue);">${formatMoney(item.price)}</strong> <i class="fa-solid fa-circle-xmark" style="color:var(--red); cursor:pointer; margin-left:12px;" onclick="removeFromCart(${idx})"></i></div>
                    </div>
                `).join('');
            }
            document.getElementById('cart-total').innerText = formatMoney(totalCart);
        }
        function removeFromCart(idx) { totalCart -= cart[idx].price; cart.splice(idx, 1); renderCart(); }
        
        async function checkout() {
            if(cart.length === 0) return showToast('Chưa chọn dịch vụ để thanh toán!', 'warning');
            let cusName = document.getElementById('pos-cus-name').value.trim() || 'Khách lẻ';
            let customer = findCustomerByNameOrAccount(cusName);
            let itemNames = cart.map(c => c.name).join(', ');
            const invoiceBody = {
                serviceName: 'Thanh toán tại quầy: ' + itemNames,
                customerName: customer ? customer.f1 : cusName,
                customerEmail: customer ? (customer.email || '') : '',
                customerPhone: customer ? displayCustomerAccount(customer.f2 || '') : '',
                totalAmount: totalCart,
                status: 'Đã hoàn tất',
                paymentMethod: 'Tiền mặt',
                note: 'POS - Thanh toán tại quầy'
            };
            showLoading('Đang lưu hóa đơn...');
            try {
                const res = await fetch('/api/Invoices', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(invoiceBody)
                });
                const json = await res.json().catch(() => ({}));
                if (!res.ok || json.success === false) throw new Error(json.message || 'Không lưu được hóa đơn lên SQL');
                const saved = json.data || json;
                const savedId = saved.invoiceId || saved.InvoiceId || saved.id || ('POS' + Date.now());
                // Cập nhật local db để hiển thị ngay không cần reload
                db.order.unshift({
                    id: savedId,
                    invoiceId: savedId,
                    f1: invoiceBody.serviceName,
                    f2: invoiceBody.customerName,
                    customerAccount: invoiceBody.customerPhone || invoiceBody.customerEmail || cusName,
                    ownerUser: normalizeUserKey(invoiceBody.customerPhone || invoiceBody.customerEmail || cusName),
                    customerName: invoiceBody.customerName,
                    f3: totalCart,
                    f4: new Date().toLocaleString('vi-VN'),
                    status: 'Đã hoàn tất'
                });
                renderApp();
                showToast(`Thanh toán thành công! Tổng thu: ${formatMoney(totalCart)}`, 'success', 4000);
                cart = []; totalCart = 0; document.getElementById('pos-cus-name').value = ''; renderCart();
                switchTab('order');
            } catch(e) {
                showToast(e.message || 'Không thể lưu hóa đơn. Kiểm tra kết nối backend.', 'error', 5000);
            } finally {
                hideLoading();
            }
        }

        function switchTab(tabId) {
            document.querySelectorAll('.page-section').forEach(el => el.classList.remove('active'));
            document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
            document.getElementById(tabId).classList.add('active');
            document.querySelector(`[data-tab="${tabId}"]`).classList.add('active');
            renderApp(); // Cập nhật lại UI khi đổi tab
        }
        
        function resetSystem() {
            if(confirm("Khôi phục tất cả dữ liệu về mặc định? Toàn bộ danh sách Khách Hàng, Hóa đơn sẽ bị xóa!")) { 
                sessionStorage.clear(); location.href='login.html'; 
            }
        }

        function logout() {
            if (confirm("Đăng xuất khỏi trang Quản trị?")) {
                fetch('/api/auth/logout', { method: 'POST' }).catch(e => {});
                sessionStorage.clear();
                window.location.href = 'login.html';
            }
        }


        /* =========================================================
           FIX CRUD SQL: KHO - NHÂN SỰ - BẢO HÀNH - NHÀ CUNG CẤP
           Giữ code cũ, chỉ vá 4 phần này để sửa/xóa đúng SQL.
        ========================================================= */

        function normalizeCrudNumber(value) {
            const n = Number(String(value || '').replace(/[^0-9.-]/g, ''));
            return isNaN(n) ? 0 : n;
        }

        async function crudFetchJson(url, options = {}) {
            const res = await fetch(url, {
                cache: 'no-store',
                ...options,
                headers: {
                    'Content-Type': 'application/json',
                    ...(options.headers || {})
                }
            });

            const json = await res.json().catch(() => ({}));

            if (!res.ok || json.success === false) {
                throw new Error(json.message || json.error || ('API lỗi: ' + url));
            }

            return json.data || json;
        }

        function getSqlId(item, type) {
            if (!item) return null;

            if (type === 'inventory') return item.apiSparePartId || item.sparePartId || item.SparePartId || item.id || item.Id;
            if (type === 'staff') return item.apiEmployeeId || item.employeeId || item.EmployeeId || item.id || item.Id;
            if (type === 'supplier') return item.apiSupplierId || item.supplierId || item.SupplierId || item.id || item.Id;
            if (type === 'warranty') return item.apiWarrantyId || item.warrantyId || item.WarrantyId || item.id || item.Id;

            return item.id || item.Id;
        }

        function mapCrudSparePart(p) {
            const id = p.id || p.Id || p.sparePartId || p.SparePartId;
            return {
                id: id,
                apiSparePartId: id,
                f1: p.name || p.Name || 'Phụ tùng',
                f2: p.code || p.Code || '',
                f3: p.stockQuantity ?? p.StockQuantity ?? p.quantity ?? p.Quantity ?? 0,
                f4: p.location || p.Location || '',
                f5: p.price ?? p.Price ?? 0
            };
        }

        function mapCrudEmployee(e) {
            const id = e.id || e.Id || e.employeeId || e.EmployeeId;
            return {
                id: id,
                apiEmployeeId: id,
                f1: e.name || e.Name || e.fullName || e.FullName || 'Nhân viên',
                f2: e.position || e.Position || 'Nhân viên',
                f3: e.phone || e.Phone || e.phoneNumber || e.PhoneNumber || '',
                f4: e.salary ?? e.Salary ?? 0
            };
        }

        function mapCrudSupplier(s) {
            const id = s.id || s.Id || s.supplierId || s.SupplierId;
            return {
                id: id,
                apiSupplierId: id,
                f1: s.name || s.Name || 'Nhà cung cấp',
                f2: s.phone || s.Phone || '',
                f3: s.address || s.Address || '',
                f4: s.status || s.Status || 'Hoạt động'
            };
        }

        function mapCrudWarranty(w) {
            const id = w.id || w.Id || w.warrantyId || w.WarrantyId;
            return {
                id: id,
                apiWarrantyId: id,
                f1: w.serviceName || w.ServiceName || w.name || w.Name || 'Dịch vụ bảo hành',
                f2: w.customerName || w.CustomerName || '',
                f3: w.purchaseDate || w.PurchaseDate || '',
                f4: w.expiryDate || w.ExpiryDate || ''
            };
        }

        async function loadCrudSqlSections() {
            // Cho giao diện kho khớp SQL hơn: thêm cột Giá
            if (schema.inventory && !schema.inventory.fields.includes('f5')) {
                schema.inventory.cols = ['Tên Phụ Tùng', 'Mã SP', 'Giá', 'Tồn kho', 'Vị trí'];
                schema.inventory.fields = ['f1', 'f2', 'f5', 'f3', 'f4'];
            }

            try {
                const data = await crudFetchJson('/api/SpareParts');
                const list = Array.isArray(data) ? data : (Array.isArray(data.data) ? data.data : []);
                db.inventory = list.map(mapCrudSparePart);
            } catch (e) {
                console.warn('Không đọc được kho từ SQL:', e);
            }

            try {
                const data = await crudFetchJson('/api/Employees');
                const list = Array.isArray(data) ? data : (Array.isArray(data.data) ? data.data : []);
                db.staff = list.map(mapCrudEmployee);
            } catch (e) {
                console.warn('Không đọc được nhân sự từ SQL:', e);
            }

            try {
                const data = await crudFetchJson('/api/Suppliers');
                const list = Array.isArray(data) ? data : (Array.isArray(data.data) ? data.data : []);
                db.supplier = list.map(mapCrudSupplier);
            } catch (e) {
                console.warn('Không đọc được nhà cung cấp từ SQL:', e);
            }

            try {
                const data = await crudFetchJson('/api/Warranties');
                const list = Array.isArray(data) ? data : (Array.isArray(data.data) ? data.data : []);
                db.warranty = list.map(mapCrudWarranty);
            } catch (e) {
                console.warn('Không đọc được bảo hành từ SQL:', e);
            }
        }

        function buildCrudBody(type, newItem) {
            if (type === 'inventory') {
                return {
                    name: newItem.f1 || '',
                    code: newItem.f2 || '',
                    price: normalizeCrudNumber(newItem.f5),
                    stockQuantity: normalizeCrudNumber(newItem.f3),
                    location: newItem.f4 || ''
                };
            }

            if (type === 'staff') {
                return {
                    name: newItem.f1 || '',
                    position: newItem.f2 || 'Nhân viên',
                    phone: newItem.f3 || '',
                    salary: normalizeCrudNumber(newItem.f4)
                };
            }

            if (type === 'supplier') {
                return {
                    name: newItem.f1 || '',
                    phone: newItem.f2 || '',
                    address: newItem.f3 || '',
                    status: newItem.f4 || 'Hoạt động'
                };
            }

            if (type === 'warranty') {
                return {
                    serviceName: newItem.f1 || '',
                    customerName: newItem.f2 || '',
                    purchaseDate: newItem.f3 || '',
                    expiryDate: newItem.f4 || ''
                };
            }

            return {};
        }

        function getCrudEndpoint(type) {
            if (type === 'inventory') return '/api/SpareParts';
            if (type === 'staff') return '/api/Employees';
            if (type === 'supplier') return '/api/Suppliers';
            if (type === 'warranty') return '/api/Warranties';
            return '';
        }

        function getCrudMapper(type) {
            if (type === 'inventory') return mapCrudSparePart;
            if (type === 'staff') return mapCrudEmployee;
            if (type === 'supplier') return mapCrudSupplier;
            if (type === 'warranty') return mapCrudWarranty;
            return x => x;
        }

        const __oldInitDataBeforeCrudFix = initData;
        initData = async function() {
            await __oldInitDataBeforeCrudFix();
            await loadCrudSqlSections();
            renderApp();
        };

        const __oldSaveDataBeforeCrudFix = saveData;
        saveData = async function() {
            const type = document.getElementById('entityType').value;

            if (!['inventory', 'staff', 'supplier', 'warranty'].includes(type)) {
                return __oldSaveDataBeforeCrudFix();
            }

            const id = document.getElementById('entityId').value;
            const sc = schema[type];
            const oldItem = id ? (db[type] || []).find(x => String(x.id) === String(id)) : null;

            let newItem = oldItem ? { ...oldItem } : { id: 'ID_' + Date.now() };

            const firstInput = document.getElementById('inp-' + sc.fields[0])?.value || '';
            if (!firstInput.trim()) {
                alert('Vui lòng nhập thông tin cho trường đầu tiên!');
                return;
            }

            sc.fields.forEach(fieldKey => {
                const el = document.getElementById('inp-' + fieldKey);
                if (el) newItem[fieldKey] = el.value;
            });

            const endpoint = getCrudEndpoint(type);
            const body = buildCrudBody(type, newItem);
            const apiId = oldItem ? getSqlId(oldItem, type) : null;

            try {
                const saved = await crudFetchJson(apiId ? `${endpoint}/${apiId}` : endpoint, {
                    method: apiId ? 'PUT' : 'POST',
                    body: JSON.stringify(body)
                });

                const mapper = getCrudMapper(type);
                const mapped = mapper(saved);

                if (apiId) {
                    const idx = db[type].findIndex(x => String(x.id) === String(id));
                    if (idx >= 0) db[type][idx] = mapped;
                } else {
                    db[type].unshift(mapped);
                }

                await loadCrudSqlSections();
                renderApp();
                closeModal();
            } catch (e) {
                alert(e.message || 'Không lưu được dữ liệu lên SQL/API.');
            }
        };

        const __oldDeleteDataBeforeCrudFix = deleteData;
        deleteData = async function(type, id) {
            if (!['inventory', 'staff', 'supplier', 'warranty'].includes(type)) {
                return __oldDeleteDataBeforeCrudFix(type, id);
            }

            if (!confirm('Xác nhận xóa dữ liệu này vĩnh viễn?')) return;

            const item = (db[type] || []).find(x => String(x.id) === String(id));
            const apiId = getSqlId(item, type);

            if (!apiId || !String(apiId).match(/^\d+$/)) {
                alert('Không tìm thấy ID SQL để xóa. Hãy bấm Ctrl + F5 rồi thử lại.');
                return;
            }

            const endpoint = getCrudEndpoint(type);

            try {
                await crudFetchJson(`${endpoint}/${apiId}`, { method: 'DELETE' });
                await loadCrudSqlSections();
                renderApp();
            } catch (e) {
                alert(e.message || 'Không thể xóa dữ liệu do ràng buộc SQL.');
            }
        };


        // Khởi động chạy hàm ĐỒNG BỘ SIÊU CẤP 1 lần duy nhất, sau đó mới vẽ UI
        window.onload = () => {
            initData();
            renderCart();
        };


/* =========================================================
   SQL/API HOTFIX - GIỮ CODE CŨ, CHỈ VÁ LOCALSTORAGE DB_* SANG SQL
   ========================================================= */

// admin-sql-hotfix.js
// Vá tối thiểu cho index admin: bỏ dữ liệu trình duyệt, lấy dữ liệu từ SQL/API, giữ giao diện/hàm cũ.

(function () {
    function getAdminToken() {
        return sessionStorage.getItem('token')
            || sessionStorage.getItem('JWT_TOKEN')
            || sessionStorage.getItem('AUTH_TOKEN')
            || '';
    }

    function getAdminUser() {
        try {
            return JSON.parse(sessionStorage.getItem('KKTH_ACTIVE_USER') || sessionStorage.getItem('user') || 'null');
        } catch {
            return null;
        }
    }

    const user = getAdminUser();
    const token = getAdminToken();

    if (!token || !user || String(user.role || '').toLowerCase() !== 'admin') {
        window.location.href = 'login.html';
        return;
    }

    function authHeaders(extra = {}) {
        return {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + getAdminToken(),
            ...extra
        };
    }

    const nativeFetch = window.fetch.bind(window);
    window.fetch = function (url, options = {}) {
        const headers = new Headers(options.headers || {});
        const isApi = typeof url === 'string' && url.toLowerCase().startsWith('/api/');
        if (isApi && getAdminToken() && !headers.has('Authorization')) {
            headers.set('Authorization', 'Bearer ' + getAdminToken());
        }
        return nativeFetch(url, { ...options, headers });
    };

    async function apiList(url) {
        const res = await fetch(url, { cache: 'no-store', headers: authHeaders() });
        const json = await res.json().catch(() => ({}));

        if (res.status === 401 || res.status === 403) {
            sessionStorage.clear();
            alert('Phiên đăng nhập hết hạn hoặc không có quyền Admin. Vui lòng đăng nhập lại.');
            window.location.href = 'login.html';
            return [];
        }

        if (!res.ok || json.success === false) {
            console.warn('API lỗi:', url, json.message || res.status);
            return [];
        }

        const data = json.data || json.value || json;
        return Array.isArray(data) ? data : [];
    }

    window.saveDB = function () {
        // SQL only: không ghi dữ liệu cũ vào sessionStorage nữa
    };

    function displayAccount(value) {
        if (typeof window.displayCustomerAccount === 'function') return window.displayCustomerAccount(value);
        const raw = String(value || '').trim();
        const fakeEmail = raw.match(/^(0\d{9,10})@khachhang\.com$/i);
        return fakeEmail ? fakeEmail[1] : raw;
    }

    function plate(value) {
        if (typeof window.normalizePlateText === 'function') return window.normalizePlateText(value);
        return String(value || '').trim().toUpperCase();
    }

    function userKey(value) {
        if (typeof window.normalizeUserKey === 'function') return window.normalizeUserKey(value);
        return String(value || '').trim().toLowerCase().replace(/[^a-z0-9@._-]/g, '_');
    }

    function mapCustomer(c) {
        const id = c.customerId || c.CustomerId || c.id || c.Id;
        return {
            id,
            apiCustomerId: id,
            f1: c.fullName || c.FullName || c.name || c.Name || 'Khách hàng',
            f2: c.phoneNumber || c.PhoneNumber || c.phone || c.Phone || '',
            email: c.email || c.Email || '',
            f3: 'Chưa có xe',
            f4: '',
            address: c.address || c.Address || ''
        };
    }

    function mapCar(c) {
        const id = c.carId || c.CarId || c.id || c.Id;
        const customerAccount = displayAccount(c.customerPhone || c.CustomerPhone || c.customerEmail || c.CustomerEmail || '');
        return {
            id,
            apiCarId: id,
            f1: plate(c.licensePlate || c.LicensePlate || c.plate || c.Plate || ''),
            f2: c.customerName || c.CustomerName || 'Khách hàng',
            f3: `${c.brand || c.Brand || ''} ${c.model || c.Model || ''} ${c.year || c.Year || ''}`.trim(),
            status: c.status || c.Status || 'Đang hoạt động',
            customerAccount,
            ownerUser: userKey(customerAccount || c.customerName || c.CustomerName || ''),
            customerName: c.customerName || c.CustomerName || '',
            customerId: c.customerId || c.CustomerId
        };
    }

    function mapService(s) {
        const id = s.serviceId || s.ServiceId || s.id || s.Id;
        return {
            id,
            apiServiceId: id,
            f1: s.serviceName || s.ServiceName || s.name || s.Name || 'Dịch vụ',
            f2: s.serviceCode || s.ServiceCode || s.code || s.Code || '',
            f3: s.price ?? s.Price ?? 0,
            f4: s.description || s.Description || ''
        };
    }

    function mapPart(p) {
        const id = p.sparePartId || p.SparePartId || p.id || p.Id;
        return {
            id,
            apiSparePartId: id,
            f1: p.name || p.Name || 'Phụ tùng',
            f2: p.code || p.Code || '',
            f3: p.stockQuantity ?? p.StockQuantity ?? p.quantity ?? p.Quantity ?? 0,
            f4: p.location || p.Location || '',
            f5: p.price ?? p.Price ?? 0
        };
    }

    function mapEmployee(e) {
        const id = e.employeeId || e.EmployeeId || e.id || e.Id;
        return {
            id,
            apiEmployeeId: id,
            f1: e.name || e.Name || e.fullName || e.FullName || 'Nhân viên',
            f2: e.position || e.Position || 'Nhân viên',
            f3: e.phone || e.Phone || e.phoneNumber || e.PhoneNumber || '',
            f4: e.salary ?? e.Salary ?? 0
        };
    }

    function mapSupplier(s) {
        const id = s.supplierId || s.SupplierId || s.id || s.Id;
        return {
            id,
            apiSupplierId: id,
            f1: s.name || s.Name || 'Nhà cung cấp',
            f2: s.phone || s.Phone || '',
            f3: s.address || s.Address || '',
            f4: s.status || s.Status || 'Hoạt động'
        };
    }

    function mapWarranty(w) {
        const id = w.warrantyId || w.WarrantyId || w.id || w.Id;
        return {
            id,
            apiWarrantyId: id,
            f1: w.serviceName || w.ServiceName || w.name || w.Name || 'Dịch vụ bảo hành',
            f2: w.customerName || w.CustomerName || '',
            f3: w.purchaseDate || w.PurchaseDate || '',
            f4: w.expiryDate || w.ExpiryDate || ''
        };
    }

    function mapInvoice(i) {
        const id = i.invoiceId || i.InvoiceId || i.id || i.Id;
        return {
            id,
            invoiceId: id,
            paymentId: i.latestPaymentId || i.LatestPaymentId || i.paymentId || i.PaymentId,
            f1: i.serviceName || i.ServiceName || i.description || i.Description || 'Hóa đơn sửa chữa',
            f2: i.customerName || i.CustomerName || 'Khách hàng',
            customerAccount: displayAccount(i.customerPhone || i.CustomerPhone || i.customerEmail || i.CustomerEmail || ''),
            f3: i.totalAmount ?? i.TotalAmount ?? i.amount ?? i.Amount ?? 0,
            status: i.status || i.Status || i.paymentStatus || i.PaymentStatus || 'Chưa thanh toán',
            paymentStatus: i.paymentStatus || i.PaymentStatus || ''
        };
    }

    function mapAppointment(a) {
        if (typeof window.makeBookingFromApiAppointment === 'function') return window.makeBookingFromApiAppointment(a);
        const id = a.appointmentId || a.AppointmentId || a.id || a.Id;
        return {
            id: 'SQL_APP_' + id,
            appointmentId: id,
            customerName: a.customerName || a.CustomerName || 'Khách hàng',
            customerAccount: displayAccount(a.customerPhone || a.CustomerPhone || a.customerEmail || a.CustomerEmail || ''),
            type: a.serviceName || a.ServiceName || a.type || a.Type || 'Lịch hẹn',
            date: a.appointmentDate || a.AppointmentDate || a.date || a.Date || '',
            status: a.status || a.Status || 'Chờ xác nhận',
            rejectionReason: a.rejectionReason || a.RejectionReason || '',
            rejectedAt: a.rejectedAt || a.RejectedAt || ''
        };
    }

    window.initData = async function () {
        try {
            const [customers, cars, services, parts, appointments, invoices, employees, suppliers, warranties] = await Promise.all([
                apiList('/api/Customers'),
                apiList('/api/Cars'),
                apiList('/api/Services'),
                apiList('/api/SpareParts'),
                apiList('/api/Appointments'),
                apiList('/api/Invoices'),
                apiList('/api/Employees'),
                apiList('/api/Suppliers'),
                apiList('/api/Warranties')
            ]);

            window.db = window.db || {};
            db.customer = customers.map(mapCustomer);
            db.car = cars.map(mapCar);
            db.service = services.map(mapService);
            db.inventory = parts.map(mapPart);
            db.booking = appointments.map(mapAppointment);
            db.order = invoices.map(mapInvoice);
            db.staff = employees.map(mapEmployee);
            db.supplier = suppliers.map(mapSupplier);
            db.warranty = warranties.map(mapWarranty);

            if (typeof window.renderApp === 'function') window.renderApp();
        } catch (e) {
            console.error('Không thể tải dữ liệu SQL/API:', e);
            alert('Không thể tải dữ liệu từ SQL Server/API. Hãy kiểm tra backend đang chạy và đăng nhập lại Admin.');
        }
    };

    window.resetSystem = function () {
        if (confirm('Đăng xuất khỏi trang Admin? Dữ liệu SQL sẽ không bị xóa.')) {
            sessionStorage.clear();
            window.location.href = 'login.html';
        }
    };

    const oldLogout = window.logout;
    window.logout = function () {
        if (confirm('Đăng xuất khỏi trang Quản trị?')) {
            fetch('/api/auth/logout', { method: 'POST' }).catch(() => {});
            sessionStorage.clear();
            window.location.href = 'login.html';
        }
    };

    window.onload = () => {
        window.initData();
        if (typeof window.renderCart === 'function') window.renderCart();
    };
})();