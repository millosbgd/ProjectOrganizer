import { Aktivnost } from './aktivnost.model';
import { Klijent } from './klijent.model';
import { User } from './user.model';

export interface ClientVisit {
  id: number;
  klijentId: number;
  aktivnostId: number;
  grad: string;
  kilometraza: number;
  gorivoLitara: number;
  createdBy?: number;
  createdAt?: Date;
  updatedAt?: Date;
  klijent?: Klijent;
  aktivnost?: Aktivnost;
  createdByUser?: User;
}

export interface FuelPurchase {
  id: number;
  datum: Date | string;
  kolicina: number;
  jedinicnaCena: number;
  ukupnaCena: number;
  createdBy?: number;
  createdAt?: Date;
  updatedAt?: Date;
  createdByUser?: User;
}
