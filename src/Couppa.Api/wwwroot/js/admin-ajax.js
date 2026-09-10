// Helper AJAX dùng chung cho các trang Admin (SRS Chương 12) — CRUD qua JsonResult, gửi kèm
// Anti-forgery token qua header RequestVerificationToken (SEC-07).
(function () {
    let antiForgeryToken = null;

    async function ensureToken() {
        if (antiForgeryToken) return antiForgeryToken;
        const res = await fetch('/Account/AntiForgeryToken', { credentials: 'include' });
        const body = await res.json();
        antiForgeryToken = body.data.token;
        return antiForgeryToken;
    }

    async function postJson(url, payload) {
        const token = await ensureToken();
        const res = await fetch(url, {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: payload === null ? null : JSON.stringify(payload)
        });
        return res.json();
    }

    async function postForm(url, formData) {
        const token = await ensureToken();
        const res = await fetch(url, {
            method: 'POST',
            credentials: 'include',
            headers: {
                // Do not set Content-Type here. The browser adds the multipart
                // boundary required by FormData automatically.
                'RequestVerificationToken': token
            },
            body: formData
        });
        return res.json();
    }

    window.couppaAdminAjax = { postJson, postForm };
})();
