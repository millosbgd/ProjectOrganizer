import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CompanyInfo {
  naziv: string;
  pib: string;
  maticniBroj: string;
  adresa: string;
  grad: string;
  zemlja: string;
  isActive: boolean;
  pdvStatus: string;
  pdvRegistrationDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class NbsService {
  private apiUrl = `${environment.apiUrl}/nbs`;

  constructor(private http: HttpClient) {}

  getCompanyInfo(pib: string): Observable<CompanyInfo> {
    return this.http.get<CompanyInfo>(`${this.apiUrl}/company/${pib}`);
  }
}
