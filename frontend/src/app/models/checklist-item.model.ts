export interface CheckListItem {
  id: number;
  opis: string;
  kompleksnost?: number | null;
  linkId?: number; // ID in junction table for deletion
}

export interface AddCheckListItemsDto {
  checkListItemIds: number[];
}

export interface CreateCheckListItemDto {
  opis: string;
  kompleksnost?: number | null;
}
