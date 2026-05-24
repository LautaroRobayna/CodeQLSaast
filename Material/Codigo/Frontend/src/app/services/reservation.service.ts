import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ReservationRequest, ReservationResponse } from '../interfaces/reservation';
import { CommonService } from './CommonService';
import { StorageManager } from '../utils/storage-manager';

@Injectable({ providedIn: 'root' })
export class ReservationService {

  lastErrorMessage: string = '';
  private url = environment.apiUrl + '/api/reservation';

  constructor(
    private http: HttpClient,
    private commonService: CommonService,
    private storageManager: StorageManager) { }

  private getHttpHeaders(): HttpHeaders {
    let login = JSON.parse(this.storageManager.getLogin());
    let token = login ? login.token : '';
    return new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Authorization', token);
  }

  getByPublicKey(publicKey: string): Observable<ReservationResponse> {
    return this.http.get<ReservationResponse>(`${this.url}?publicKey=${encodeURIComponent(publicKey)}`, { headers: this.getHttpHeaders() })
      .pipe(
        tap((res: ReservationResponse) => console.log(`Got reservation w/ code=${res.code}`)),
        catchError(this.handleError<ReservationResponse>('Get Reservation By Public Key'))
      );
  }

  createReservation(reservation: ReservationRequest): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(this.url, reservation, {headers: this.getHttpHeaders()})
      .pipe(
        tap((newReservation: ReservationResponse) => console.log(`Created reservation w/ code=${newReservation.code}`)),
        catchError(this.handleError<ReservationResponse>('Create Reservation'))
      );
  }

  uploadPrescription(publicKey: string, prescriptionBase64: string, prescriptionFileName: string): Observable<any> {
    return this.http.patch<any>(`${this.url}?publicKey=${encodeURIComponent(publicKey)}`,
      { prescriptionBase64, prescriptionFileName }, { headers: this.getHttpHeaders() })
      .pipe(
        tap(() => console.log('Prescription uploaded')),
        catchError(this.handleError<any>('Upload Prescription'))
      );
  }

  getAllPending(): Observable<ReservationResponse[]> {
    return this.http.get<ReservationResponse[]>(`${this.url}/pending`, {headers: this.getHttpHeaders()})
      .pipe(
        tap(() => console.log('Fetched pending reservations')),
        catchError(this.handleError<ReservationResponse[]>('Get All Pending'))
      );
  }

  confirmReservation(code: string): Observable<ReservationResponse> {
    return this.http.put<ReservationResponse>(`${this.url}/${code}/confirm`, null, {headers: this.getHttpHeaders()})
      .pipe(
        tap((confirmed: ReservationResponse) => console.log(`Confirmed reservation code=${confirmed.code}`)),
        catchError(this.handleError<ReservationResponse>('Confirm Reservation'))
      );
  }

  rejectReservation(code: string): Observable<ReservationResponse> {
    return this.http.put<ReservationResponse>(`${this.url}/${code}/reject`, null, {headers: this.getHttpHeaders()})
      .pipe(
        tap((rejected: ReservationResponse) => console.log(`Rejected reservation code=${rejected.code}`)),
        catchError(this.handleError<ReservationResponse>('Reject Reservation'))
      );
  }

  private handleError<T>(operation = 'operation', result?: T) {
    return (error: any): Observable<T> => {
      this.lastErrorMessage = error.error?.message || '';
      this.log(`${operation} failed: ${error.error.message}`);
      return of(result as T);
    };
  }

  private log(message: string) {
    this.commonService.updateToastData(message, "danger", "Error");
  }
}
