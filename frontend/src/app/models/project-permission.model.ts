export interface ProjectPermission {
  id: number;
  userId: number;
  projekatId: number;
  permissionLevel: string;
  createdAt: Date;
  createdBy?: number;
  user?: {
    id: number;
    email: string;
    name?: string;
  };
}
