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
  user?: {
    id: number;
    email: string;
    name?: string;
  };
}
