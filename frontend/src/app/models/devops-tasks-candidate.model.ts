export interface DevOpsTasksCandidate {
  id: number;
  aktivnostId: number;
  generatedContent: string;
  status: string;
  createdAt: Date;
  user?: {
    id: number;
    email: string;
    name?: string;
  };
}
