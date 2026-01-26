export interface User {
  id: number;
  auth0Id: string;
  email: string;
  name?: string;
  role: string;
  isActive: boolean;
  lastLogin?: Date;
  createdAt: Date;
  updatedAt: Date;
}
