import axios from 'axios';
import type {
  AuthResponse,
  DiningTable,
  KitchenAccessResponse,
  Member,
  MenuItem,
  Order,
  OrderItemStatus,
  OrderStatus,
  Restaurant,
  StaffRole,
  User,
} from '../types';

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5139/api';

const api = axios.create({
  baseURL,
});

const TOKEN_KEY = 'kd_token';

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

// Attach bearer token to every request.
api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// On 401, clear the stored token so the app can redirect to login.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      setToken(null);
    }
    return Promise.reject(error);
  },
);

export const authApi = {
  register: (email: string, password: string, displayName: string) =>
    api.post<AuthResponse>('/auth/register', { email, password, displayName }).then((r) => r.data),
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { email, password }).then((r) => r.data),
  me: () => api.get<User>('/auth/me').then((r) => r.data),
};

export const restaurantApi = {
  list: () => api.get<Restaurant[]>('/restaurants').then((r) => r.data),
  get: (id: string) => api.get<Restaurant>(`/restaurants/${id}`).then((r) => r.data),
  create: (name: string, kitchenPasscode?: string) =>
    api.post<Restaurant>('/restaurants', { name, kitchenPasscode }).then((r) => r.data),
  members: (id: string) => api.get<Member[]>(`/restaurants/${id}/members`).then((r) => r.data),
  addMember: (id: string, email: string, roles: StaffRole[]) =>
    api.post<Member>(`/restaurants/${id}/members`, { email, roles }).then((r) => r.data),
  updateRoles: (id: string, userId: string, roles: StaffRole[]) =>
    api.put(`/restaurants/${id}/members/${userId}/roles`, { roles }),
  removeMember: (id: string, userId: string) =>
    api.delete(`/restaurants/${id}/members/${userId}`),
  setKitchenPasscode: (id: string, passcode: string) =>
    api.put(`/restaurants/${id}/kitchen-passcode`, { passcode }),
};

export type MenuItemInput = {
  name: string;
  description?: string | null;
  category?: string | null;
  price: number;
  isAvailable: boolean;
};

export const menuApi = {
  list: (restaurantId: string) =>
    api.get<MenuItem[]>(`/restaurants/${restaurantId}/menu`).then((r) => r.data),
  create: (restaurantId: string, input: MenuItemInput) =>
    api.post<MenuItem>(`/restaurants/${restaurantId}/menu`, input).then((r) => r.data),
  update: (restaurantId: string, itemId: string, input: MenuItemInput) =>
    api.put<MenuItem>(`/restaurants/${restaurantId}/menu/${itemId}`, input).then((r) => r.data),
  remove: (restaurantId: string, itemId: string) =>
    api.delete(`/restaurants/${restaurantId}/menu/${itemId}`),
};

export type TableInput = {
  number: number;
  label?: string | null;
  seats: number;
};

export const tableApi = {
  list: (restaurantId: string) =>
    api.get<DiningTable[]>(`/restaurants/${restaurantId}/tables`).then((r) => r.data),
  create: (restaurantId: string, input: TableInput) =>
    api.post<DiningTable>(`/restaurants/${restaurantId}/tables`, input).then((r) => r.data),
  update: (restaurantId: string, tableId: string, input: TableInput) =>
    api.put<DiningTable>(`/restaurants/${restaurantId}/tables/${tableId}`, input).then((r) => r.data),
  remove: (restaurantId: string, tableId: string) =>
    api.delete(`/restaurants/${restaurantId}/tables/${tableId}`),
};

export type OrderLineInput = {
  menuItemId: string;
  quantity: number;
  notes?: string | null;
};

export const orderApi = {
  list: (restaurantId: string, activeOnly = true) =>
    api
      .get<Order[]>(`/restaurants/${restaurantId}/orders`, { params: { activeOnly } })
      .then((r) => r.data),
  get: (restaurantId: string, orderId: string) =>
    api.get<Order>(`/restaurants/${restaurantId}/orders/${orderId}`).then((r) => r.data),
  create: (restaurantId: string, tableId: string, items: OrderLineInput[]) =>
    api
      .post<Order>(`/restaurants/${restaurantId}/orders`, { tableId, items })
      .then((r) => r.data),
  addLines: (restaurantId: string, orderId: string, items: OrderLineInput[]) =>
    api
      .post<Order>(`/restaurants/${restaurantId}/orders/${orderId}/items`, { items })
      .then((r) => r.data),
  updateLine: (restaurantId: string, orderId: string, lineId: string, quantity: number, notes?: string | null) =>
    api
      .put<Order>(`/restaurants/${restaurantId}/orders/${orderId}/items/${lineId}`, { quantity, notes })
      .then((r) => r.data),
  removeLine: (restaurantId: string, orderId: string, lineId: string) =>
    api
      .delete<Order>(`/restaurants/${restaurantId}/orders/${orderId}/items/${lineId}`)
      .then((r) => r.data),
  setLineStatus: (restaurantId: string, orderId: string, lineId: string, status: OrderItemStatus) =>
    api
      .put<Order>(`/restaurants/${restaurantId}/orders/${orderId}/items/${lineId}/status`, { status })
      .then((r) => r.data),
  setStatus: (restaurantId: string, orderId: string, status: OrderStatus) =>
    api
      .put<Order>(`/restaurants/${restaurantId}/orders/${orderId}/status`, { status })
      .then((r) => r.data),
};

// The kitchen window uses its own client with an explicit kitchen token, so the
// logged-in user's bearer token (if any) does not override the kitchen scope.
const kitchenClient = axios.create({ baseURL });

export const kitchenApi = {
  access: (restaurantId: string, passcode: string) =>
    axios
      .post<KitchenAccessResponse>(`${baseURL}/restaurants/${restaurantId}/kitchen/access`, { passcode })
      .then((r) => r.data),
  listOrders: (restaurantId: string, token: string) =>
    kitchenClient
      .get<Order[]>(`/restaurants/${restaurantId}/kitchen/orders`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      .then((r) => r.data),
};

export default api;
