import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ReservationRequest, ReservationResponse } from '../interfaces/reservation';
import { CommonService } from './CommonService';

@Injectable({ providedIn: 'root' })
export class ReservationService {

  private url = environment.apiUrl + '/api/reservation';

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
  };

  constructor(
    private http: HttpClient,
    private commonService: CommonService) { }

  getByPublicKey(publicKey: string): Observable<ReservationResponse> {
    return this.http.get<ReservationResponse>(`${this.url}?publicKey=${encodeURIComponent(publicKey)}`, this.httpOptions)
      .pipe(
        tap((res: ReservationResponse) => console.log(`Got reservation w/ code=${res.code}`)),
        catchError(this.handleError<ReservationResponse>('Get Reservation By Public Key'))
      );
  }

  createReservation(reservation: ReservationRequest): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(this.url, reservation, this.httpOptions)
      .pipe(
        tap((newReservation: ReservationResponse) => console.log(`Created reservation w/ code=${newReservation.code}`)),
        catchError(this.handleError<ReservationResponse>('Create Reservation'))
      );
  }

  uploadPrescription(publicKey: string, prescriptionBase64: string, prescriptionFileName: string): Observable<any> {
    return this.http.patch<any>(`${this.url}?publicKey=${encodeURIComponent(publicKey)}`,
      { prescriptionBase64, prescriptionFileName }, this.httpOptions)
      .pipe(
        tap(() => console.log('Prescription uploaded')),
        catchError(this.handleError<any>('Upload Prescription'))
      );
  }

  private handleError<T>(operation = 'operation', result?: T) {
    return (error: any): Observable<T> => {
      this.log(`${operation} failed: ${error.error.message}`);
      return of(result as T);
    };
  }

  private log(message: string) {
    this.commonService.updateToastData(message, "danger", "Error");
  }
}
