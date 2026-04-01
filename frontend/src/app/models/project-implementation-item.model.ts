export interface ProjectImplementationItem {
  id: number;
  projectId: number;
  implementationModelId: number;
  implementationItemId: number;
  implementationItemNaziv?: string;
  napomena?: string;
  zavrseno: boolean;
  zavrsenoDatum?: string;
  klijentPotvrdio: boolean;
  klijentPotvrdioDatum?: string;
  checkLists?: ProjectImplementationCheckListItem[];
}

export interface ProjectImplementationCheckListItem {
  id: number;
  checkListItemId: number;
  checkListItemOpis?: string;
  checkListItemKompleksnost?: number | null;
  procenat?: number | null;
  zavrsen: boolean;
  zavrsenDatum?: string;
  klijentPotvrdio: boolean;
  klijentPotvrdioDatum?: string;
}
