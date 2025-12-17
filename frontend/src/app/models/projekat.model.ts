import { Aktivnost } from './aktivnost.model';
import { Klijent } from './klijent.model';

export interface Projekat {
  id: number;
  brojProjekta: string;
  datum: Date;
  naziv: string;
  aktivan: boolean;
  status: string;
  klijentId: number;
  klijent?: Klijent;
  aktivnosti?: Aktivnost[];
}
