export interface CheckListItem {
  id: number;
  opis: string;
  linkId?: number; // ID in junction table for deletion
}

export interface AddCheckListItemsDto {
  checkListItemIds: number[];
}
