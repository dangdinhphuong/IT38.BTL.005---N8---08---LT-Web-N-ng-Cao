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
})();
