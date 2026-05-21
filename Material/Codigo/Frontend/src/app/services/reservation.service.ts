import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ReservationRequest, ReservationResponse } from '../interfaces/reservation';
import { CommonService } from './CommonService';

@Injectable({ providedIn: 'root' })
export class ReservationService {

  lastErrorMessage: string = '';
  private url = environment.apiUrl + '/api/reservation';

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
  };

  constructor(
    private http: HttpClient,
    private commonService: CommonService) { }

  createReservation(reservation: ReservationRequest): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(this.url, reservation, this.httpOptions)
      .pipe(
        tap((newReservation: ReservationResponse) => console.log(`Created reservation w/ code=${newReservation.code}`)),
        catchError(this.handleError<ReservationResponse>('Create Reservation'))
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
