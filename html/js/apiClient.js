/* ==========================================================================
   COUPPA API CLIENT
   Module trung tâm gọi Fetch tới ASP.NET Core Web API backend thật.
   Specification: docs/test/SRS.md
   ========================================================================== */

const API_BASE_URL = 'https://localhost:5001/api';

/**
 * Lỗi nghiệp vụ do server trả về success:false (ApiResponse<T>.Error).
 * Có .code / .message / .details lấy từ ApiError để caller phân biệt xử lý (409/401/422...).
 */
class ApiError extends Error {
  constructor(code, message, details) {
    super(message);
    this.name = 'ApiError';
    this.code = code;
    this.message = message;
    this.details = details;
  }
}

/**
 * Lỗi hạ tầng: network fail (fetch reject) hoặc response không phải JSON hợp lệ.
 * Tách riêng khỏi ApiError để UI phân biệt được "lỗi nghiệp vụ" (có message rõ ràng
 * từ server) với "lỗi kết nối/parse" (không có message nghiệp vụ để hiển thị).
 */
class ApiNetworkError extends Error {
  constructor(message, cause) {
    super(message);
    this.name = 'ApiNetworkError';
    this.cause = cause;
  }
}

const CSRF_COOKIE_NAME = 'couppa.csrf';
const CSRF_HEADER_NAME = 'X-CSRF-TOKEN';
const STATE_CHANGING_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

/**
 * Đọc giá trị cookie CSRF (couppa.csrf) từ document.cookie. Cookie này KHÔNG HttpOnly
 * (cấu hình ở backend Program.cs) vì JS bắt buộc phải đọc được để đính vào header
 * X-CSRF-TOKEN - bảo mật CSRF nằm ở việc backend so khớp header với cookie, không phải
 * ở việc giấu giá trị này với JS cùng origin.
 */
function readCsrfCookie() {
  const match = document.cookie.match(new RegExp(`(?:^|; )${CSRF_COOKIE_NAME}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

const ApiClient = (() => {
  /**
   * Gọi fetch chung, parse theo format { success, data } | { success:false, error }.
   * SEC-07: tự động đính header X-CSRF-TOKEN (đọc từ cookie couppa.csrf) cho mọi request
   * POST/PUT/PATCH/DELETE - GET không đổi trạng thái nên không cần. Backend so khớp header
   * này với cookie qua CsrfValidationMiddleware (xem Program.cs).
   */
  async function request(method, path, body) {
    const headers = { 'Content-Type': 'application/json' };
    if (STATE_CHANGING_METHODS.has(method.toUpperCase())) {
      const csrfToken = readCsrfCookie();
      if (csrfToken) {
        headers[CSRF_HEADER_NAME] = csrfToken;
      }
    }

    let response;
    try {
      response = await fetch(`${API_BASE_URL}${path}`, {
        method,
        credentials: 'include',
        headers,
        body: body !== undefined ? JSON.stringify(body) : undefined
      });
    } catch (networkErr) {
      throw new ApiNetworkError('Không thể kết nối tới máy chủ. Vui lòng kiểm tra kết nối mạng.', networkErr);
    }

    let payload;
    try {
      // Một số response (vd 204 No Content) có thể rỗng thân - vẫn cố parse JSON,
      // nếu rỗng thì text() trả '' và JSON.parse('') sẽ throw -> rơi vào catch bên dưới.
      const text = await response.text();
      payload = text ? JSON.parse(text) : { success: response.ok, data: null, error: null };
    } catch (parseErr) {
      throw new ApiNetworkError('Phản hồi từ máy chủ không hợp lệ (không phải JSON).', parseErr);
    }

    if (!payload || payload.success !== true) {
      const err = payload && payload.error
        ? payload.error
        : { code: 'UNKNOWN_ERROR', message: `Yêu cầu thất bại (HTTP ${response.status})`, details: null };
      throw new ApiError(err.code, err.message, err.details);
    }

    return payload.data;
  }

  return {
    get(path) {
      return request('GET', path);
    },
    post(path, body) {
      return request('POST', path, body);
    },
    put(path, body) {
      return request('PUT', path, body);
    },
    patch(path, body) {
      return request('PATCH', path, body);
    },
    // Đặt tên "del" thay vì "delete" vì delete là từ khóa JS.
    del(path) {
      return request('DELETE', path);
    }
  };
})();
