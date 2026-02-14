export interface ImplementationItem {
  id: number;
  implementationModelId: number;
  naziv: string;
  detalji?: string;
}

export interface ImplementationModel {
  id: number;
  naziv?: string;
  opis?: string;
  aktivan: boolean;
  items: ImplementationItem[];
}
