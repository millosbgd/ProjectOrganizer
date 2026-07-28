export interface StatusHistoryEntry {
  status: string;
  assignedTo?: string;
  roleName?: string;
  totalDurationMinutes?: number;
  isActive: boolean;
  startedAt?: string;
  endedAt?: string;
  lastStartedAt?: string;
}

export interface DevOpsTasksCandidate {
  id: number;
  aktivnostId: number;
  userId: number;
  title: string;
  description?: string;
  acceptanceCriteria?: string;
  priority?: string;
  estimation?: string;
  orderIndex: number;
  status: string;
  createdAt: Date;
  devOpsWorkItemId?: number;
  devOpsUrl?: string;
  devOpsState?: string;
  devOpsAssignedTo?: string;
  devOpsChangedDate?: string;
  lastDevOpsSyncAt?: string;
  lastDevOpsSyncStatus?: string;
  lastDevOpsSyncError?: string;
  statusHistory?: StatusHistoryEntry[];
  aktivnost?: {
    id: number;
    opis: string;
    datum: Date | string;
    status: string;
    vrsta: string;
  };
  user?: {
    id: number;
    email: string;
    name?: string;
  };
}

export interface FetchedDevOpsTask {
  devOpsWorkItemId: number;
  devOpsUrl?: string;
  title?: string;
  description?: string;
  acceptanceCriteria?: string;
  priority?: string;
  estimation?: string;
  workItemType?: string;
  devOpsState?: string;
  statusHistory?: StatusHistoryEntry[];
}
