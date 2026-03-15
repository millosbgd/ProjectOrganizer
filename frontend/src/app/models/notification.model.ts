export interface Notification {
  id: number;
  userId: number;
  projekatId?: number;
  aktivnostId?: number;
  type: 'StatusChange' | 'Blocked' | 'Inactive' | 'DeadlineApproaching' | 'AiReminder' | string;
  message: string;
  isRead: boolean;
  dismissed: boolean;
  referenceKey?: string;
  createdAt: string;
}
