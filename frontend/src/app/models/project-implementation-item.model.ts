export interface ProjectImplementationItem {
  id: number;
  projectId: number;
  implementationModelId: number;
  implementationItemId: number;
  implementationItemNaziv?: string; // From joined data
  napomena?: string;
  zavrseno: boolean;
  zavrsenoDatum?: Date;
  klijentPotvrdio: boolean;
  klijentPotvrdioDatum?: Date;
  checkLists?: ProjectImplementationCheckListItem[];
}

export interface ProjectImplementationCheckListItem {
  id: number;
  checkListItemId: number;
  checkListItemOpis?: string;
  checkListItemKompleksnost?: number | null;
  procenat?: number | null;
  zavrsen: boolean;
  zavrsenDatum?: Date;
  klijentPotvrdio: boolean;
  klijentPotvrdioDatum?: Date;
}
