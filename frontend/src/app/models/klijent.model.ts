export interface Klijent {
  id: number;
  naziv: string;
  pib?: string;
  maticniBroj?: string;
  adresa: string;
  grad: string;
  zemlja: string;
  pdvStatus?: string;
  pdvRegistrationDate?: string;
}
