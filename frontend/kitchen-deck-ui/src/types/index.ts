export type StaffRole = 'Admin' | 'Cook' | 'Waiter' | 'Guard' | 'CleaningStaff';

export const ALL_ROLES: StaffRole[] = ['Admin', 'Cook', 'Waiter', 'Guard', 'CleaningStaff'];

export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface Restaurant {
  id: string;
  name: string;
  ownerUserId: string;
  isOwner: boolean;
  myRoles: StaffRole[];
  hasKitchenPasscode: boolean;
}

export interface Member {
  userId: string;
  displayName: string;
  email: string;
  roles: StaffRole[];
}

export interface MenuItem {
  id: string;
  restaurantId: string;
  name: string;
  description?: string | null;
  category?: string | null;
  price: number;
  isAvailable: boolean;
}

export interface DiningTable {
  id: string;
  restaurantId: string;
  number: number;
  label?: string | null;
  seats: number;
}

export type OrderStatus = 'Open' | 'Preparing' | 'Served' | 'Closed' | 'Cancelled';
export type OrderItemStatus = 'Pending' | 'Preparing' | 'Served';

export interface OrderItem {
  id: string;
  menuItemId: string;
  name: string;
  unitPrice: number;
  quantity: number;
  notes?: string | null;
  status: OrderItemStatus;
}

export interface Order {
  id: string;
  restaurantId: string;
  tableId: string;
  tableNumber: number;
  waiterUserId: string;
  status: OrderStatus;
  items: OrderItem[];
  createdAt: string;
  updatedAt: string;
}

export interface KitchenAccessResponse {
  token: string;
  restaurantId: string;
  restaurantName: string;
  expiresInMinutes: number;
}
