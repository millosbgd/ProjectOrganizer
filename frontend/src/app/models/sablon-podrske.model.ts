export interface SablonPodrske {
  id: number;
  klijentId?: number | null;
  naslov: string;
  opisZahteva: string;
  opisResenja: string;
  odgovorKlijentu: string;
  kreirao?: number | null;
  promenio?: number | null;
  vremeKreiranja?: string;
  vremePromene?: string;
  klijent?: {
    id: number;
    naziv: string;
  };
  kreiraoUser?: {
    id: number;
    name: string;
    email: string;
  };
  promenioUser?: {
    id: number;
    name: string;
    email: string;
  };
}
