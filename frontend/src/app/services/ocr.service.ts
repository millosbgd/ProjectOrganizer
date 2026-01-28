import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcrService {
  private apiUrl = `${environment.apiUrl}/ocr`;

  constructor(private http: HttpClient) {}

  extractText(imageBase64: string): Observable<ExtractTextResponse> {
    return this.http.post<ExtractTextResponse>(`${this.apiUrl}/extract-text`, {
      imageBase64: imageBase64
    });
  }
}

export interface ExtractTextResponse {
  text: string;
}
