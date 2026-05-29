export interface CalendarActivity {
  id: number;
  title: string;
  start: string; // ISO string (UTC)
  end: string; // ISO string (UTC)
  type: string;
  projectName: string;
  projectId: number | null;
  bau: boolean;
  bauTipAktivnosti?: string | null;
}

export interface UpdateActivityTimeDto {
  startUtc: string; // ISO string (UTC)
  endUtc: string; // ISO string (UTC)
}
