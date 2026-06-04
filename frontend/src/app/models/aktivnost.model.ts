export interface Aktivnost {
  id: number;
  opis: string;
  opisZaIzvestaj?: string;
  detalji: string;
  datum: Date;
  startUtc?: string;
  endUtc?: string;
  status: string;
  vrsta: string;
  bau?: boolean;
  klijentId?: number | null;
  bauTipAktivnosti?: string | null;
  bauTrajanjeMinuta?: number | null;
  projekatId: number | null;
  projectImplementationItemId?: number;
  projekat?: {
    id: number;
    naziv: string;
    brojProjekta: string;
  };
  createdBy?: number;
  createdByUser?: {
    id: number;
    name: string;
    email: string;
  };
  devOpsTasksTotalCount?: number;
  devOpsTasksReadyCount?: number;
}
