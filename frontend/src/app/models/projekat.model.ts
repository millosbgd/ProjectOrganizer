import { Aktivnost } from './aktivnost.model';
import { Klijent } from './klijent.model';
import { User } from './user.model';

export interface ProjectCompletionStats {
  lastWeekCompletion: number;
  currentCompletion: number;
  completionDelta: number;
  hasImplementationPlan: boolean;
}

export interface Projekat {
  id: number;
  brojProjekta: string;
  datum: Date | string;
  naziv: string;
  aktivan: boolean;
  status: string;
  klijentId: number;
  createdBy?: number;
  createdByUser?: User;
  implementationModelId?: number;
  devOpsOrganization?: string;
  devOpsProject?: string;
  devOpsAreaPath?: string;
  devOpsIterationPath?: string;
  klijent?: Klijent;
  aktivnosti?: Aktivnost[];
  completionStats?: ProjectCompletionStats;
}

