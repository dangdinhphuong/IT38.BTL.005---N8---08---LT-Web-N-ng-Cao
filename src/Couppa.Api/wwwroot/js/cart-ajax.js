// AJAX cho các thao tác giỏ hàng không reload trang (SRS Chương 12).
// Gửi Anti-forgery token qua header RequestVerificationToken (SEC-07) — lấy 1 lần từ
// GET /Account/AntiForgeryToken khi trang load.
(function () {
    let antiForgeryToken = null;

    async function ensureToken() {
        if (antiForgeryToken) return antiForgeryToken;
        const res = await fetch('/Account/AntiForgeryToken', { credentials: 'include' });
        const body = await res.json();
        antiForgeryToken = body.data.token;
        return antiForgeryToken;
    }

    async function addToCart(productId, quantity) {
        const token = await ensureToken();
        const res = await fetch('/Cart/AddItem', {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ productId: Number(productId), quantity: Number(quantity) })
        });
        const body = await res.json();
        if (!res.ok || body.success === false) {
            alert(body.error?.message || 'Không thể thêm sản phẩm vào giỏ hàng.');
            return;
        }
        alert('Đã thêm sản phẩm vào giỏ hàng!');
    }

    async function post(url, payload) {
        const token = await ensureToken();
        const res = await fetch(url, {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: payload === undefined ? null : JSON.stringify(payload)
        });
        const body = await res.json();
        if (!res.ok || body.success === false) {
            alert(body.error?.message || 'Không thể cập nhật giỏ hàng.');
            return null;
        }
        return body;
    }

    function renderCart(cart) {
        if (!cart) return;
        const total = document.getElementById('cart-total-items');
        const subtotal = document.getElementById('cart-subtotal');
        if (total) total.textContent = cart.data.totalQuantity;
        if (subtotal) subtotal.textContent =
            new Intl.NumberFormat('vi-VN').format(cart.data.subtotal) + ' đ';
        for (const item of cart.data.items) {
            const input = document.querySelector(`.js-cart-quantity[data-item-id="${item.id}"]`);
            if (input) input.value = item.quantity;
            const row = document.querySelector(`[data-cart-item="${item.id}"]`);
            if (row) {
                const cells = row.querySelectorAll('td');
                if (cells.length >= 4) cells[3].textContent =
                    new Intl.NumberFormat('vi-VN').format(item.subtotal) + ' đ';
            }
        }
    }

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.js-add-to-cart');
        if (!btn) return;

        const productId = btn.dataset.productId;
        const quantityInputSelector = btn.dataset.quantityInput;
        const quantity = quantityInputSelector
            ? document.querySelector(quantityInputSelector)?.value || 1
            : 1;

        addToCart(productId, quantity);
    });

    window.couppaCartAjax = {
        add: addToCart,
        updateItem: async (id, quantity) => {
            const result = await post(`/Cart/UpdateItem/${id}`, { quantity });
            if (result) renderCart(result);
        },
        removeItem: async (id) => {
            const result = await post(`/Cart/RemoveItem/${id}`);
            if (result) {
                document.querySelector(`[data-cart-item="${id}"]`)?.remove();
                renderCart(result);
            }
        },
        clear: async () => {
            const result = await post('/Cart/Clear');
            if (result) {
                document.querySelectorAll('[data-cart-item]').forEach(row => row.remove());
                const total = document.getElementById('cart-total-items');
                const subtotal = document.getElementById('cart-subtotal');
                if (total) total.textContent = '0';
                if (subtotal) subtotal.textContent = '0 đ';
            }
        }
    };
})();
