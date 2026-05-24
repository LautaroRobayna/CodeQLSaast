import { Component } from '@angular/core';
import { ReservationResponse } from '../../interfaces/reservation';
import { ReservationService } from '../../services/reservation.service';

@Component({
  selector: 'app-reservation-manage',
  templateUrl: './reservation-manage.component.html',
  styleUrls: ['./reservation-manage.component.css']
})
export class ReservationManageComponent {
  publicKey: string = '';
  reservation: ReservationResponse | null = null;
  errorMessage: string = '';

  constructor(private reservationService: ReservationService) {}

  searchReservation(): void {
    if (!this.publicKey.trim()) return;
    this.reservation = null;
    this.errorMessage = '';
    this.reservationService.getByPublicKey(this.publicKey).subscribe(res => {
      if (res) {
        this.reservation = res;
      } else {
        this.errorMessage = 'Reserva no encontrada.';
      }
    });
  }
}
