import { CheckListItem } from './checklist-item.model';

export interface ImplementationItem {
  id: number;
  implementationModelId: number;
  naziv: string;
  detalji?: string;
  checkListItems?: CheckListItem[];
}

export interface ImplementationModel {
  id: number;
  naziv?: string;
  opis?: string;
  aktivan: boolean;
  items: ImplementationItem[];
}
