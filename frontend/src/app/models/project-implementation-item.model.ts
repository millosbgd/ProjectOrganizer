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
}
