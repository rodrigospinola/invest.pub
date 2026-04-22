import axios from 'axios';

// Access token em memória — nunca em localStorage/sessionStorage (vulnerável a XSS)
// O refresh token viaja apenas em httpOnly cookie (gerenciado pelo browser)
let _accessToken: string | null = null;

export const tokenStore = {
  set: (token: string) => { _accessToken = token; },
  get: () => _accessToken,
  clear: () => { _accessToken = null; },
};

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true, // envia o cookie refresh_token automaticamente
});

api.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let _isRefreshing = false;
let _refreshSubscribers: ((token: string) => void)[] = [];

function subscribeRefresh(cb: (token: string) => void) {
  _refreshSubscribers.push(cb);
}

function notifyRefreshed(token: string) {
  _refreshSubscribers.forEach((cb) => cb(token));
  _refreshSubscribers = [];
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Não tenta refresh se for uma rota de auth ou se for o próprio refresh
    const isAuthRequest = originalRequest.url?.includes('/auth/login') || originalRequest.url?.includes('/auth/refresh');

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthRequest) {
      if (_isRefreshing) {
        return new Promise((resolve) => {
          subscribeRefresh((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            resolve(api.request(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      _isRefreshing = true;

      try {
        const { data } = await axios.post(
          `${import.meta.env.VITE_API_URL || 'http://localhost:5000'}/auth/refresh`,
          {},
          { withCredentials: true }
        );
        tokenStore.set(data.accessToken);
        notifyRefreshed(data.accessToken);
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        return api.request(originalRequest);
      } catch {
        tokenStore.clear();
        // Só redireciona se não estiver já na página de login para evitar loop
        if (window.location.pathname !== '/auth/login' && window.location.pathname !== '/auth/register') {
          window.location.href = '/auth/login';
        }
      } finally {
        _isRefreshing = false;
      }
    }
    return Promise.reject(error);
  }
);

export default api;
