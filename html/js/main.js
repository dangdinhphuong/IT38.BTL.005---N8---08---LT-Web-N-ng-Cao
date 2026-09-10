/* ==========================================================================
   COUPPA CORE APP ENGINE (Phase 11 — tích hợp API thật)
   Specification: docs/test/SRS.md
   ========================================================================== */

/**
 * Global App Helpers (nền tảng dùng chung mọi trang).
 * Không còn mock data (CouppaApp.db) / auth localStorage - mọi trạng thái đăng nhập
 * và giỏ hàng lấy trực tiếp từ API (session cookie couppa.session / couppa.auth do
 * server quản lý qua credentials:'include').
 */
const CouppaApp = {
  // Helper để tính đường dẫn tương đối theo vị trí trang hiện tại.
  getBasePath() {
    const isSubDir = window.location.pathname.includes('/admin/') || window.location.href.includes('/admin/');
    return isSubDir ? '../' : './';
  },

  getAdminPath() {
    const isSubDir = window.location.pathname.includes('/admin/') || window.location.href.includes('/admin/');
    return isSubDir ? './' : 'admin/';
  },

  /**
   * Helper: Format Currency VNĐ
   */
  formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  },

  /**
   * Escape các ký tự đặc biệt HTML (& < > " ') trước khi chèn vào template
   * literal dùng cho innerHTML - chống XSS khi nội dung đến từ API (free-text
   * hoặc field tưởng chừng an toàn như role/slug/email). Escape & trước tiên
   * để tránh double-escape các entity còn lại.
   */
  escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  },

  /**
   * Toast Notification System
   */
  showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
      container = document.createElement('div');
      container.id = 'toast-container';
      container.className = 'toast-container';
      document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    let iconSvg = '';
    if (type === 'success') {
      iconSvg = `<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M5 13l4 4L19 7"></path></svg>`;
    } else if (type === 'error') {
      iconSvg = `<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"></circle><path d="M15 9l-6 6M9 9l6 6"></path></svg>`;
    } else {
      iconSvg = `<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>`;
    }

    toast.innerHTML = `${iconSvg} <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      toast.style.transition = 'all 0.3s ease';
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  },

  /**
   * Đọc message hiển thị được từ lỗi ApiClient (ApiError nghiệp vụ hoặc ApiNetworkError hạ tầng).
   */
  getErrorMessage(err) {
    if (err && err.name === 'ApiError') return err.message;
    if (err && err.name === 'ApiNetworkError') return err.message;
    return 'Đã xảy ra lỗi không xác định. Vui lòng thử lại.';
  },

  /**
   * Cart Badge - lấy tổng số lượng thật từ server (CartResponse.totalQuantity).
   */
  async updateCartBadge() {
    const badges = document.querySelectorAll('.cart-badge');
    if (badges.length === 0) return;
    try {
      const cart = await ApiClient.get('/cart');
      const count = cart.totalQuantity || 0;
      badges.forEach(b => {
        b.textContent = count;
        b.style.display = count > 0 ? 'flex' : 'none';
      });
    } catch (err) {
      // Guest chưa có giỏ hàng / lỗi mạng: hiển thị badge rỗng, không chặn trang.
      badges.forEach(b => {
        b.textContent = '0';
        b.style.display = 'none';
      });
    }
  },

  /**
   * Thêm sản phẩm vào giỏ - dùng chung cho mọi trang (index/products/product-detail).
   * Trả về true/false để trang gọi biết có nên cập nhật UI cục bộ hay không.
   */
  async addToCart(productId, quantity = 1) {
    try {
      await ApiClient.post('/cart/items', { productId, quantity });
      await this.updateCartBadge();
      this.showToast('Đã thêm sản phẩm vào giỏ hàng!', 'success');
      return true;
    } catch (err) {
      this.showToast(this.getErrorMessage(err), 'error');
      return false;
    }
  },

  /**
   * Modal Management
   */
  openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.add('active');
    }
  },

  closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.remove('active');
    }
  },

  /**
   * Đăng xuất: gọi API thật rồi redirect về trang chủ.
   */
  async logout() {
    try {
      await ApiClient.post('/auth/logout');
    } catch (err) {
      // Kể cả lỗi logout API, vẫn điều hướng về trang chủ - cookie phía server
      // hết hạn hoặc đã bị xoá không nên chặn người dùng ở lại trang.
    }
    this.showToast('Đã đăng xuất tài khoản', 'info');
    const base = this.getBasePath();
    setTimeout(() => {
      window.location.href = `${base}index.html`;
    }, 600);
  },

  /**
   * Header Auth UI Update - gọi GET /api/users/me để xác định trạng thái đăng nhập.
   * 200 -> render tên user + logout. 401 -> render nút Login/Register.
   */
  async renderHeaderAuth() {
    const container = document.getElementById('header-auth-area');
    if (!container) return;

    const base = this.getBasePath();
    const admin = this.getAdminPath();

    try {
      const user = await ApiClient.get('/users/me');
      const isAdmin = user.role === 'Admin';
      const targetUrl = isAdmin ? `${admin}dashboard.html` : `${base}profile.html`;
      // SEC-08: user.fullName do chính user nhập lúc đăng ký/sửa profile -> phải escape trước khi chèn vào innerHTML.
      container.innerHTML = `
        <div class="flex items-center gap-3">
          <a href="${targetUrl}" class="btn btn-outline btn-sm">
            <svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path></svg>
            ${this.escapeHtml(user.fullName)} ${isAdmin ? '<span class="badge badge-info" style="margin-left:4px">Admin</span>' : ''}
          </a>
          <button onclick="CouppaApp.logout()" class="btn btn-outline btn-sm" title="Đăng xuất">
            <svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path></svg>
          </button>
        </div>
      `;
    } catch (err) {
      // 401 (chưa đăng nhập) hoặc lỗi mạng: hiển thị nút Login/Register mặc định.
      container.innerHTML = `
        <a href="${base}login.html" class="btn btn-outline btn-sm">Đăng nhập</a>
        <a href="${base}register.html" class="btn btn-primary btn-sm">Đăng ký</a>
      `;
    }
  },

  /**
   * SEC-07: đảm bảo cookie CSRF (couppa.csrf) luôn tồn tại trước khi user thao tác bất kỳ
   * hành động thay đổi trạng thái nào (add to cart, login, CRUD admin...). Gọi 1 lần khi
   * trang load - không cần đọc response, chỉ cần server set cookie qua Set-Cookie.
   */
  async ensureCsrfToken() {
    try {
      await ApiClient.get('/auth/csrf-token');
    } catch (err) {
      // Lỗi mạng ở bước này không nên chặn trang - các request đổi trạng thái sau đó
      // sẽ tự thất bại với lỗi CSRF rõ ràng nếu cookie thực sự chưa có.
    }
  },

  /**
   * Global Initialization
   */
  init() {
    this.ensureCsrfToken();
    this.updateCartBadge();
    this.renderHeaderAuth();
  }
};

document.addEventListener('DOMContentLoaded', () => {
  CouppaApp.init();
});
