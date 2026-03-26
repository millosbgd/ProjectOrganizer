export interface DailyTask {
  id: number;
  datum: string;       // ISO date string, e.g. "2026-03-26"
  opis: string;
  korisnikKreirao: number;
  solved: boolean;
  pinned: boolean;
}
