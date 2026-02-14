import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ImplementationModel } from '../models/implementation-model.model';

@Injectable({
  providedIn: 'root'
})
export class ImplementationModelService {
  private apiUrl = `${environment.apiUrl}/implementationmodels`;

  constructor(private http: HttpClient) { }

  getAll(aktivan?: boolean): Observable<ImplementationModel[]> {
    let url = this.apiUrl;
    if (aktivan !== undefined) {
      url += `?aktivan=${aktivan}`;
    }
    return this.http.get<ImplementationModel[]>(url);
  }

  getById(id: number): Observable<ImplementationModel> {
    return this.http.get<ImplementationModel>(`${this.apiUrl}/${id}`);
  }

  create(model: ImplementationModel): Observable<ImplementationModel> {
    return this.http.post<ImplementationModel>(this.apiUrl, model);
  }

  update(id: number, model: ImplementationModel): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, model);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
