import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ClientVisit, FuelPurchase } from '../models/posete-gorivo.model';

@Injectable({
  providedIn: 'root'
})
export class PoseteGorivoService {
  private apiUrl = `${environment.apiUrl}/posetegorivo`;

  constructor(private http: HttpClient) {}

  getVisits(): Observable<ClientVisit[]> {
    return this.http.get<ClientVisit[]>(`${this.apiUrl}/visits`);
  }

  createVisit(visit: ClientVisit): Observable<ClientVisit> {
    return this.http.post<ClientVisit>(`${this.apiUrl}/visits`, visit);
  }

  updateVisit(id: number, visit: ClientVisit): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/visits/${id}`, visit);
  }

  deleteVisit(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/visits/${id}`);
  }

  getFuelPurchases(): Observable<FuelPurchase[]> {
    return this.http.get<FuelPurchase[]>(`${this.apiUrl}/fuel-purchases`);
  }

  createFuelPurchase(fuelPurchase: FuelPurchase): Observable<FuelPurchase> {
    return this.http.post<FuelPurchase>(`${this.apiUrl}/fuel-purchases`, fuelPurchase);
  }

  updateFuelPurchase(id: number, fuelPurchase: FuelPurchase): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/fuel-purchases/${id}`, fuelPurchase);
  }

  deleteFuelPurchase(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/fuel-purchases/${id}`);
  }
}
