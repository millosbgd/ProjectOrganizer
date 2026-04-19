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
}
