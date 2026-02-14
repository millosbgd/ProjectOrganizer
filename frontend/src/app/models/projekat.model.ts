import { Aktivnost } from './aktivnost.model';
import { Klijent } from './klijent.model';
import { User } from './user.model';

export interface Projekat {
  id: number;
  brojProjekta: string;
  datum: Date;
  naziv: string;
  aktivan: boolean;
  status: string;
  klijentId: number;
  createdBy?: number;
  createdByUser?: User;
  devOpsOrganization?: string;
  devOpsProject?: string;
  devOpsAreaPath?: string;
  devOpsIterationPath?: string;
  klijent?: Klijent;
  aktivnosti?: Aktivnost[];
}
