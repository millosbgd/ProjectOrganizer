export interface Notification {
  id: number;
  userId: number;
  projekatId?: number;
  type: 'StatusChange' | 'Blocked' | 'Inactive' | 'DeadlineApproaching' | string;
  message: string;
  isRead: boolean;
  dismissed: boolean;
  referenceKey?: string;
  createdAt: string;
}
