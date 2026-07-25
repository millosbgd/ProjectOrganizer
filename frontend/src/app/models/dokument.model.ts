export interface Dokument {
  id: number;
  projekatId?: number | null;
  entity: string;
  entityId: number;
  nazivFajla: string;
  tipFajla: string;
  blobUrl: string;
  velicina: number;
  createdAt: string;
  updatedAt: string;
}
