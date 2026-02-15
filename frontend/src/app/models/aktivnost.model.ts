export interface Aktivnost {
  id: number;
  opis: string;
  detalji: string;
  datum: Date;
  startUtc?: string;
  endUtc?: string;
  status: string;
  vrsta: string;
  projekatId: number;
  projectImplementationItemId?: number;
}
